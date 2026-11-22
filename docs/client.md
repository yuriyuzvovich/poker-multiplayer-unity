# Client design (Unity 6, UniTask, VContainer)

Purpose: the client internals: composition, startup, connection, messaging, the presentation queue, time, views and
assets, effects, and diagnostics. Back to [README](README.md).

## Composition root

- One `RootLifetimeScope` (VContainer, in `Main.unity`) registers everything. It registers settings, the message
  registry, `UiLayers`, `ViewLoader`, `PopupService` and the `TranscriptRecorder` itself, then calls the installers:
  `CoreInstaller`, `NetworkInstaller` and `DiagnosticsInstaller` for infrastructure, and `TableInstaller`,
  `LobbyInstaller`, `SeatingInstaller`, `HandInstaller`, `HandHistoryInstaller`, `TurnTimerInstaller` and
  `WalletInstaller` for features. Each is a static `Install(IContainerBuilder)` inside its own assembly.
- Constructor injection only; no service locator, no static state. That is what makes a scene reload a clean restart.
- Inspector fields on the root: server URL, request timeout, player name, simulated round trip.
- `AppEntryPoint` (`IAsyncStartable`) runs the startup commands and sets `Application.targetFrameRate = 60`.

## Feature slices on the client

Per [architecture](architecture.md): each feature is one assembly under `client/Assets/Features/<Name>/` with
`Domain`, `Application`, `Presentation` and `Views`, and an installer.

| Rule | Why |
|---|---|
| A feature references infrastructure, the `Table` kernel and `Poker.Shared`, never another feature | Slices stay independent; deleting one still compiles |
| Cross-feature reactions go through bus events (`IMessageBus`) or kernel state | The table reacts to seating without referencing `Seating` |
| A slice registers its own subscriptions, commands and presenters in its installer | Adding a feature touches one folder plus one line in `RootLifetimeScope` |
| Shared visuals (seat widget, chip stacks, words such as `HandText`) live in the `Table` kernel | Several slices draw on the same seat |
| Features add UI to shared surfaces through kernel or infrastructure extension points (`TableToolbar`, `TableView.Decorate`, `DevPanelActions`) | The kernel and infrastructure stay feature-agnostic |

## Startup and reload

Startup is a list of commands composed with `Infrastructure/Core/Commands` (`ICommand`, `SequenceCommand`,
`ParallelCommand`, `RetryCommand`, `TimedCommand`): connect (retried), hello, then clock sync beside the lobby load,
then the return to a held seat (`ConnectCommand`, `HelloCommand`, `SyncClockCommand`, `LoadLobbyCommand`,
`ReturnToTableCommand`).

Reload: `AppReloader.Reload(reason)` loads the Main scene again. Unity destroys the old root, its container disposes
every service in reverse order (that is the teardown), and the new root runs startup again. The user is
`4007 DuplicateLogin`: `ReplacedSessionFlow` shows a `NoticePopup` ("Signed in elsewhere") whose "Play here" reloads
the app, which signs in again and replaces the other device in turn. A new session after a long drop
(`SessionRestarted`) reloads only the table.

`AppStateMachine` tracks `AppState` (`Booting`, `Connecting`, `Connected`, `Disconnected`, `Failed`);
`ConnectionSupervisor` drives reconnects ([reliability](reliability.md)).

## Connection and messaging

- Features depend on `IServerConnection` only: `Subscribe<T>(handler)` and `RequestAsync<TRequest, TResponse>`. A
  rejected request throws `RequestRejectedException` with the server's error code.
- Inside, `ServerConnection` owns the socket and the order of writes. It composes:
  - `WebSocketConnection`, the socket adapter.
  - `ReliableLink`: numbering, acks, and the start, resume and restart welcomes (`WelcomeKind`).
  - `PendingRequests`: request ids, timeouts and cancellation.
  - `MessageRouter`: delivery by type.
  - `LinkConditioner`: development latency and blackouts.
  - `IPacketTap`s for debug capture.

  A subscriber that throws is logged and skipped, so one presenter bug can neither starve the others nor stop the
  receive loop. If the receive loop still fails, it closes the connection visibly instead of leaving a silent
  half-open link.
- `IMessageBus` (`Infrastructure.Core`): typed publish/subscribe for client events (for example `TableJoined`,
  `SitRequested`), synchronous, main thread. A throwing subscriber is logged and skipped; subscribing swaps in a new
  handler array, so a handler may unsubscribe during delivery.
- Presenters keep their subscriptions in a list and dispose them in `Dispose`, so nothing dangles after the container
  goes.
- Identity: `DeviceKey.LoadOrCreate()` saves a random key on first launch; `PlayerSession` holds the welcome's player
  and session ids.

## Presentation queue

```
ServerConnection -> presenter -> model updated now (the truth)
  -> IVisualCommand created with the event -> PresentationQueue
  -> command plays later: scene (presented state) -> view interface (passive)
```

| Piece | Assembly | Notes |
|---|---|---|
| `IVisualCommand` | `Infrastructure.Core` | `PlayAsync(token)` animates, `Finish()` applies the end state instantly, `RunsOutOfOrder` |
| `PresentationQueue` | `Infrastructure.Core` | `Enqueue` (plays in arrival order, one at a time), `FastForward` (cancels the playing command, then finishes it and every queued one in order), `Clear` (drops without finishing; leaving the table). A failing command is logged and the queue moves on |
| `HandModel` | `Features.Hand` | Updated at network time: current turn (the action panel sends only when the model says it is my turn), board, my cards, the chosen `PreAction` |
| `HandScene` | `Features.Hand` | What the player currently sees: seat lines, board, pot, status. Commands write here, never to the model. It is the only holder of the shown state (`HandShownState`) and fills a newly loaded view through `Replay()`. The countdown is `TurnClock`, card reading `HandReader`, the words `HandSceneText` |
| `HandStepCommand` | `Features.Hand` | Template method: `Apply()` plus a hold time from `HandTimings`; one subclass per hand event (see [sync flows](sync-flows.md)) |

Rules:
- **Absolute values only.** Every command carries absolute values from its event (stack, committed, pot total, the
  full board), never deltas. `Finish()` after a half-played `PlayAsync()` lands on the same state as a full play, and
  finishing every queued command lands on the network's latest state.
- **Out of order.** A command with `RunsOutOfOrder` is finished the moment it is enqueued, without waiting for or
  skipping the playing animation. Example: `SitOutChangedCommand` (TurnTimer) sets the kernel seat flag at once, so the
  "I am back" banner does not wait behind a showdown; the banner and the seat's "away" both render from that flag.
- **Fast-forward on my turn.** When `turn.started` arrives for my seat, the presenter fast-forwards the queue before
  enqueuing the turn, so the action panel and countdown appear at once. The last moves before my turn snap into place
  instead of animating.
- **Money waits for its animation.** `DisplayLock` (`Infrastructure.Core`) is a counted lock for values that must not
  move on screen. The Hand presenter takes a `DisplayHold` at `hand.started` for a hand I am in and gives it to that
  hand's `HandEndedCommand`, which releases it when `hand.ended` is shown. The wallet presenter keeps the true balance
  in its model but redraws only while the lock is free. An ordered command would not work here: the server sends
  `wallet.balance_changed` before the `hand.ended` broadcast. Holds owned by a command that `Clear` drops are released
  by the presenter on leaving the table or a new session.

## Time on the client

| Piece | Assembly | Notes |
|---|---|---|
| `ServerClock` (`IServerClock`) | `Infrastructure.Network` | `NowUnixMs` = local time plus offset. Each sample assumes the server read its clock halfway through the round trip; the lowest round trip wins (its error bound is smallest), taken from the last 6 samples so drift and network changes are corrected |
| Samples | `App` | The hello round trip, five pings in `SyncClockCommand`, then every heartbeat ping |
| Countdown | `Features.Hand` | `HandPresenter` is an `ITickable`; the turn ring redraws only when the whole second changes and at zero on my turn hides the panel ("Time is up"). `HandModel.IsInputOpen` refuses a click after the visible end, leaving the leeway to the network |

## Views and assets

| Piece | Where | Job |
|---|---|---|
| `ViewLoader` | `Infrastructure.UI` | `LoadAsync<T>(address, parent, ct)` instantiates an Addressable prefab; `Release(view)` hands it back. A load cancelled midway releases what arrived; `LoadedCount` makes leaks testable. Opens and closes log `[UI] Opened <address> in <ms> ms` / `[UI] Closed <name>` |
| `LazyView<TView>` | `Infrastructure.UI` | A view handle: `Open(parent)` loads, `Close()` releases, `Opened` / `Closing` events, safe against close-while-loading |
| Screen proxies | `LobbyScreen`, `TableScreen`, `HandLayer` | Implement the presenter's view interface (`ILobbyView`, `ITableView`, `IHandView`) and fill the view when it loads; `SetVisible` means load and release. `HandLayer` caches nothing: it raises `IHandView.Attached`, and the presenter has `HandScene.Replay()` push the shown state |
| `ViewPool<T>` | `Infrastructure.UI` | Inactive instances under a storage transform; card views are reused, so a session creates only as many cards as one frame shows |
| `PopupService`, `PopupQueue` | `Infrastructure.UI` | `ShowAsync(popup, priority)`: one popup at a time, waiting ones open in order, a `Critical` one opens at once on top |
| `UiLayers` | `Infrastructure.UI` | The only always-loaded UI: a screen layer and a popup layer, 1080x1920 portrait, matched to width |
| `SafeArea`, `SafeAreaInset` | `Infrastructure.UI` | Grow the top and action bars by the notch and home-indicator strips |

Lifetimes: the lobby loads at start and is released while you sit at a table; the table screen loads on join and is
released on leaving; the hand layer loads into the table screen's slot and is released before the screen goes (its
`Closing` event), so no instance is destroyed behind Addressables' back; popups live only while open.

Table layout: seats sit at fixed slots on an oval by seat index (`SeatLayout`), so every client draws seat N in the
same place. The kernel's `SeatWidget` owns every seat visual (avatar, name, stack, badge, bubble, note, bet chips,
cards slot, action button); the Hand layer decorates seats through `TableView.Decorate` and puts its pooled cards into
the cards slot. `HandView.SetSeats` changes only what differs, and `SeatWidget` skips a decoration equal to the one it
shows. The seat area has the table image's size, so seats stay on the rim on tall screens. Seats, the board, the turn
ring, the dealer button, the fly layer and the action bar each have their own nested `Canvas`, so a ring that changes
every frame rebuilds only its own small canvas.

Generated assets (never hand-edit the YAML; change the builder and rerun it from the `mp-poker` menu):

| Builder (`client/Assets/App/Editor`) | Produces |
|---|---|
| `MainSceneBuilder` | `Assets/Scenes/Main.unity` with the root scope |
| `UiPrefabBuilder` | The screen, layer and popup prefabs in `Assets/Content/UI`, wired and registered in the Addressables group `UI` (one bundle per prefab, so table art leaves memory with the table) |
| `UiSkinBuilder` | Code-drawn 9-slice sprites (pill, panel, ring) in `Assets/Content/Art/Skin` |
| `SpriteAtlasBuilder` | Sprite Atlas v2 assets `Skin`, `Table`, `Cards` in `Assets/Content/Atlases`, each Addressable in its own bundle, so shared sprites load once |
| `FontSetup` | Static SDF font assets for Montserrat (SemiBold text, Bold names and numbers), ASCII plus a few symbols |
| `ArtImportPostprocessor`, `MobileTextureSettings` | Sprite import without mipmaps; ASTC on Android, ASTC 8x8 for the soft backgrounds |
| `UiSnapshot` | Renders the lobby, a table with a hand, the buy-in popup and a win frame to PNGs (`Library/UiSnapshots` or `POKER_SNAPSHOT_DIR`), so layout can be checked without Play Mode |
| `AddressablesReport` | Builds Addressables content for the active target and lists bundle sizes, duplicate dependencies, atlas fill and texture formats |
| `AndroidBuild` | Addressables content plus a development APK (IL2CPP, ARM64); reach a local server from a phone with `adb reverse tcp:5080 tcp:5080` |

Art: the table art in `Assets/Content/Art` is original and AI-generated (OpenAI image generation), cropped and scaled
to the size it is shown at, and contains no text. The skin sprites are drawn in code by `UiSkinBuilder`. Rank letters
are UI text, so a blank face, a back and four suit icons make every card. Text is TextMesh Pro with Montserrat (SIL OFL). Prefabs carry no
font reference: TextMesh Pro fills an empty font with its default from `Resources`, so the font is in memory once
instead of copied into every bundle.

## Effects

All tweens live in `Infrastructure.UI` (DOTween is vendored in `Assets/Plugins/Demigiant`); features call helpers and
never reference DOTween:
- `UiMotion`: `Punch`, `SlideIn`, `MoveTo` and `Flip`. Every tween is linked to its object and dies when the object is
  disabled.
- `PressFeedback`: on every button.
- `AppearMotion`: screens fade in, popups pop.
- `FlightLayer`: plays `Flight` values as one sequence of pooled images: `Arc` (chips and cards), `Burst` (sparks),
  `Glow` (halo) and `Float` ("+1,250").

The Hand slice decides who flies: `HandScene` picks the seats, `HandView` turns them into positions, and each command
flies first, then applies its end state. `HandStepCommand.Finish()` and `HandScene.Reset()` stop flights, so a flight
never outlives the state it animates. Durations live in `HandTimings`; zero switches an effect off, which Edit Mode
replays use.

Table moments:
- Dealing: card backs spin along their arc, and cards flip face up when they become known.
- Chips: a bet flies in and shows as a stack coloured by denomination (`ChipStackView`); pots punch when chips land.
- Winners: a halo, sparks, the amount floating up (`IHandView.CelebrateAsync`). After a showdown,
  `HandEvaluator.Evaluate(cards, bestFive)` picks each winner's best five, and `IHandView.HighlightCards` frames them.
- Turn: a ring drains around the acting avatar and pulses in the last five seconds. My action bar slides up; while it
  is not my turn, a pre-action bar offers "Check/Fold", "Check" and "Call any". The choice is turned into a move with
  `HandModel.TakePreAction(turn)` using the server's `CanCheck` from that turn, and is used once.
- Amounts: `ChipText.Format` is exact below 10,000, then "12.5K" and "1.2M", rounded down.

## Hand history

A "History" button (added through the kernel's `TableToolbar`) sends `history.get` and shows the answer in
`HandHistoryPopup`, newest first: the result, my cards and the board, what others showed, and actions by street.
`HandHistoryText` turns a record into lines; `HandText` (kernel) holds the words the table bubbles and the history
share.

## Async rules

- UniTask everywhere; every async method takes a `CancellationToken` last.
- No coroutines in game code, and no `async void`; fire-and-forget through `.Forget()`.
- Animations are awaited with cancellation linked to the owning object.

## Diagnostics

Assembly `Infrastructure.Diagnostics`, plus the recorder in `Infrastructure.Network`:

| Piece | Job |
|---|---|
| `LogRingBuffer` | Listens to Unity's log stream (`Application.logMessageReceivedThreaded`), so every warning, error and unobserved UniTask exception lands in it. Keeps the first 50 lines and the last 500, keeps stack traces of errors, counts errors |
| `TranscriptRecorder` | An `IPacketTap` that records every accepted game message as raw JSON, restarting at each `table.snapshot` (the composition root names that baseline, so the network layer knows no feature), up to 10,000 messages. Release builds create it with capacity 0, which records nothing |
| `BugReportWriter` | Writes `<persistentDataPath>/BugReports/bug-<utc stamp>/report.txt` (versions, player, session, socket, clock, counts, the log) and `transcript.json` (replayable, see [testing](testing.md)) |
| `NetworkStats`, `MessageTypeStats` | Bytes and packets per second each way, resends, the main-thread hop of each received frame, the message types with the most bytes, the slowest handler |
| `RenderStats` | Batches, draw calls, SetPass calls and texture memory (editor and development builds) |
| Dev panel | A "Dev" button with the live info above and buttons for a bug report and simulated drops of 3 s and 40 s. Features add buttons through `DevPanelActions.Add(label, run)`; `HandDevActions` plays single effects and toggles `HandTimings.Fast()` |

## Extension ideas

- Popup conditions (not during my turn, not during a transition) in `PopupQueue`.
- Merging queued commands (two stack updates for one seat become one).
- Landscape layouts through a resource override map.