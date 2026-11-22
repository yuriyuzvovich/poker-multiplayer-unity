# Testing

Purpose: how each layer is proven, from pure rules to resume under a dropped socket, and how the architecture rules are
enforced. Back to [README](README.md).

## Test map

| Level | Where | What | Tools |
|---|---|---|---|
| Rules | `tests/Poker.Tests/Shared/Rules` | Hand evaluator vectors (wheel, kickers, seven-card choice, ties), side pots | xUnit |
| Protocol | `tests/Poker.Tests/Shared/Infrastructure/Protocol` | Header byte layout and round trip, FNV-1a vectors, codec, registry collisions, `ReliableChannel` (numbering, duplicates, gaps, ack trimming, resend order, wrap-around) | xUnit |
| Table and slices | `tests/Poker.Tests/Features/<Slice>` | `PokerTable` rules, the actor, `PokerHand`, `HandEngine`, seating, turn timer, chip bank, hand history | `ManualClock`, `ScriptedDeck`, `SeededDeckFactory`, `RecordingConnection`, `RecordingChipBank` |
| Simulation | `Features/Hand/HandSimulationTests` | A thousand seeded bot hands through the real engine keep every invariant (below); the same seeds play the same game | Seeded decks and bots |
| Infrastructure | `tests/Poker.Tests/Infrastructure/{Session,Messaging}` | `ClientSession` (outbox, close, stalled socket), `SessionManager`, `HelloValidator`, `MessageDispatcher`, serializer | `StalledWebSocket`, fakes |
| Integration | `tests/Poker.Tests/Integration` | A real Kestrel server on a random port (`TestServer`) and raw WebSocket clients (`TestClient`): handshake, lobby, join, a dealt hand with hole cards only to their owner, resume, sign-in takeover. One class at a time (`IntegrationCollection`): several in-process servers in parallel made connects time out | Real sockets, short `ConnectionOptions` |
| Architecture | `tests/Poker.Tests/Architecture` | Dependency rules on server namespaces, shared code and client asmdefs; source hygiene | Reads the source tree |
| Client logic | `client/Assets/Tests/EditMode/<Area>` (one folder per infrastructure area or slice: `Core`, `Network`, `UI`, `Diagnostics`, `Table`, `Lobby`, `Seating`, `Hand`, `HandHistory`, `TurnTimer`, `Wallet`) | App state machine, commands, message bus, router, pending requests, reliable link, link conditioner, clock, presentation queue, display lock, popups, view pool, every slice's model and presenters with fake views, hand replay | NUnit, `FakeServerConnection` (responses and failures per request type), `FakeServerClock`, `Fixtures/` |
| End to end | `client/Assets/Tests/PlayMode` | The real app against a running server: lobby and join, sit and buy in, play a hand, reconnect after short and long drops, sign-in takeover, history, wallet, dev panel | Needs `./tools/server.ps1` (or `-StartServer`) |

Run them with `./tools/test.ps1` and `./tools/unity-tests.ps1` (or `/test` and `/unity-tests`).

## Deterministic tools

- `ManualClock` (`IClock`): tests advance time explicitly; no `Task.Delay`, no wall clock, no real randomness.
- `ScriptedDeck`: exact cards for exact scenarios (split pot, three-way all-in, kicker decides).
- `SeededDeckFactory`: a repeatable shuffle for simulations and recordings.
- `RecordingConnection` (`IClientConnection`): captures what a player was sent, as `SentMessage` values.
- `RecordingChipBank` (`IChipBank`): records wallet movements.
- `StalledWebSocket`: a socket that never completes a send, to prove a slow client cannot stall a table.
- Client: `FakeServerConnection`, `FakeServerClock`, fake views implementing the view interfaces.

## Invariants

`HandSimulationTests` checks after every table event:
1. Sum of stacks plus pots is constant within a hand.
2. Hole cards are addressed to their owner, two cards each; every other hand event is public.
3. The seat to act is in the hand, and `turnId` strictly increases.
4. No stack is negative.

## Transcript replay

| Piece | Where | Job |
|---|---|---|
| Recorder | `tests/Poker.Tests/Features/Hand/HandTranscriptTests.cs` | Plays one seeded heads-up hand through the real table actor (manual clock, seeded deck and bot) and records every message the human received, as wire JSON |
| Fixture | `client/Assets/Tests/EditMode/Fixtures/heads-up-hand.json` | The recording, committed |
| Golden check | Same .NET test | Fails when the recording and the fixture differ, so a protocol change cannot leave the client replaying a stale format. Regenerate with `POKER_UPDATE_FIXTURES=1` set while running `./tools/test.ps1`, then commit the fixture |
| Coverage check | Same .NET test | The recorded hand contains hole cards, a turn for the human, a raise, all three board rounds and a showdown, so a seed change cannot silently shrink what the replay exercises |
| Replay | `client/Assets/Tests/EditMode/Hand/HandReplayTests.cs` | Decodes the fixture with the client serializer, feeds it through a fake connection into the real Hand presenter, queue and scene, and asserts the final view and table model |

Bug reports close the loop: `transcript.json` in a bug report (`BugReportWriter`) has the same format as the fixture,
so a failing report becomes a regression test by copying it into `Fixtures/` and pointing `RecordedHand.LoadFrom` at
it. Release builds record no transcript.

## Link conditioner

`LinkConditioner` inside the client's `ServerConnection` delays every outgoing send and every incoming packet by half
the configured round trip, keeping order: an outgoing packet gets its due time when sent, then waits on the send gate
until that time. Set it with the root scope's `_simulatedRoundTripMs` or the environment variable
`POKER_SIMULATED_RTT_MS` for a batch run. `ServerConnection.SimulateDrop(blackout)` aborts the socket, discards what is
in flight in both directions and refuses to connect until the blackout ends; the dev panel has short (3 s) and long
(40 s) drops, and PlayMode tests use it to prove resume and full reconnect.

## Architecture tests

`tests/Poker.Tests/Architecture` turns the rules in [architecture](architecture.md) into failing tests, so an agent or a
reviewer gets a red build instead of a style comment:
- Server: a feature namespace uses only itself, the `Table` kernel, infrastructure and `Poker.Shared`; infrastructure
  uses no feature; only `ServerHost` uses them all; infrastructure areas are layered (Time, Transport, Messaging,
  Session, Hosting).
- Shared: no `UnityEngine`.
- Client: a `Features.*` asmdef references no other feature except `Features.Table`; `Infrastructure.*` references no
  feature; only `App.Root`, `App.Editor` and the test assemblies reference several features.
- Folders: at most 8 `.cs` files per folder, so modules stay grouped by responsibility (`FolderSizeTests`).
- Source hygiene: LF line endings, no trailing newline, no em or en dashes, namespaces that mirror folders, and
  relative markdown links that resolve.

## Definition of done for a test

Each new test is shown red once against the defect it guards, then green.

Trap: with warnings treated as errors, a constant mutation such as `if (false)` makes the code after it unreachable
(CS0162), the build fails and no test runs, which looks like a pass in a filtered log. Break the rule with a
non-constant condition instead, and check that tests ran.

Trap: two mutations in one run can hide each other. When one removes the precondition another one's test needs, only
one test goes red and the second mutation looks uncaught. Run a mutation alone whenever its test depends on state
another mutation touches.

## Extension ideas

- A load test: N bot clients over real WebSockets, measuring hands per second, event latency and resumes.
- Jitter and packet-level loss in the link conditioner.