# Development

Purpose: how to change mp-poker: setup, the daily loop, how a feature or a message is added, the definition of done,
and how docs and commits are kept. It applies to people and AI agents alike. Back to [README](README.md).

## Setup and daily loop

| Task | Command (PowerShell 5.1 or 7, repo root) |
|---|---|
| First run on a fresh clone | `./tools/setup.ps1` (finds the SDK and Unity, builds `Poker.Shared.dll` into the Unity project) |
| Build and run the .NET tests | `./tools/test.ps1 [-Filter <expr>]` |
| Run the server | `./tools/server.ps1` (foreground) or `-Background`, then `-Stop` |
| Open the Unity project | `./tools/open-unity.ps1`, or Unity Hub |
| Unity tests headless | `./tools/unity-tests.ps1 -Platform EditMode` or `-Platform PlayMode -StartServer` (Editor closed) |

The scripts share `tools/Common.ps1`. It finds a dotnet that satisfies `global.json` (PATH, then `DOTNET_ROOT`, then
per-user installs) and the editor for `client/ProjectSettings/ProjectVersion.txt` (`UNITY_EDITOR`, then Unity Hub's
custom and default install folders). A script that refuses because of the environment exits with 3: a running server
holds the DLLs, or an open Editor holds the project. Stop the server before rebuilding.

After adding files under `client/Assets/`, let the Unity Editor import them so it writes the `.meta` files; never
write a `.meta` by hand. Scenes and prefabs come from the editor builders ([client](client.md)): change the builder,
then rerun it from the `mp-poker` menu.

## How a feature slice is built

Work through the layers in this order, each with its tests:
1. **Shared.** Messages and shared rules in `shared/Poker.Shared/Features/<Slice>/`: message classes,
   `<Slice>MessageNames`, `<Slice>Messages.Register` (added to `MessageCatalog.CreateRegistry`), error codes. Unit tests
   for rules and a serialization round trip.
2. **Server.** Handlers and work items in `server/Poker.Server/Features/<Slice>/`, installed by
   `<Slice>Feature.Install` from `ServerHost`. Test through the actor with `ManualClock` and a scripted deck.
3. **Client domain and application.** `client/Assets/Features/<Slice>/` with its own asmdef (infrastructure,
   `Features.Table`, `Poker.Shared.dll`) and `<Slice>Installer`, called from `RootLifetimeScope`. Edit Mode tests with
   `FakeServerConnection` and fake network events.
4. **Presenters and views.** View interfaces in `Views/`, presenters in `Presentation/`, animations as `IVisualCommand`s
   through the `PresentationQueue`, prefabs through the editor builders. Then a manual run against the real server with
   bots, and a PlayMode test for the main path.
5. **Missing infrastructure** is built minimally in infrastructure with its own tests, never inside the slice. A
   duplicate between slices moves down to the kernel or infrastructure.

Before step 1, a non-trivial feature gets a design and a plan in `docs/features/<name>/`
([feature designs](features/README.md)): the review lists inconsistencies with these rules, the user settles the
important decisions, and the plan fixes the steps and the exit tests.

Claude Code users get this as skills ([working with AI agents](agentic-workflow.md) walks through an example):
`/plan-feature` reviews and plans a design, `/new-slice` scaffolds a slice across all layers, `/add-message` adds one
message end to end, `/arch-review` checks a change against the rules, `/update-docs` brings the docs in line.

## Adding a protocol message

1. Shared: `<Name>{Request|Response|Event}` in the slice's `Requests/` or `Events/` folder (a `sealed class` with `{ get; set; }` properties,
   strings `= ""`), the wire name in `<Slice>MessageNames` (`"area.verb"`, lower snake case), the registration in
   `<Slice>Messages.Register`.
2. Server, for a request: a `MessageHandler<T>` that posts a work item through `TableDirectory.PostOrRejectAsync` when
   it touches table state, registered in `<Slice>Feature.Install`. Events come from the actor; hidden data is addressed
   with `TableEvent(message, recipientPlayerId)`.
3. Client: `IServerConnection.Subscribe<T>` in the presenter that owns the reaction (animations through the queue), or
   `RequestAsync` from the flow that owns the intent.
4. Tests: a round trip, a handler or actor test, and the catalog collision test stays green.
5. Docs: add the message to the catalog in [protocol](protocol.md).

## Definition of done

1. The exit test is written first or alongside, shown red against the defect, then green ([testing](testing.md) has
   the mutation traps).
2. `./tools/test.ps1` is green, including the architecture and source hygiene tests; Unity Edit Mode tests are green
   when the client changed.
3. No feature references another feature's implementation; the architecture tests enforce it.
4. The docs that describe the changed area are updated in the same change, in current-state wording (no "will",
   "later", milestones or build diary). Map of areas to docs:

| Area | Doc |
|---|---|
| Messages, wire names, error codes | [protocol](protocol.md), [sync flows](sync-flows.md) |
| Seq/ack, resume, reconnect | [reliability](reliability.md) |
| Authority, timers, hidden information, chips, bots | [game authority](game-authority.md) |
| Client composition, queue, views, assets, diagnostics | [client](client.md) |
| Server host, sessions, actor, slices | [server](server.md) |
| Projects, slices, dependency rules | [architecture](architecture.md) |
| Tests, doubles, fixtures | [testing](testing.md) |
| Setup, tools, workflow | this doc, `AGENTS.md`, the root `README.md` |

## Commits

- One past-tense plain sentence, no prefix, no trailing period, for example "Kept outgoing packets in order under
  simulated latency".
- One logical change per commit; the docs it touches go in the same commit.
- No em or en dashes anywhere (code, comments, docs, commit messages); the source hygiene test checks files.

## Continuous integration

`.github/workflows/ci.yml` runs `./tools/test.ps1` on Ubuntu and Windows for every push and pull request: build with
warnings as errors, every .NET test, the architecture and hygiene tests. Unity is not run in CI (it needs a license);
the asmdef rules are covered by the architecture tests, and Unity tests run locally through `./tools/unity-tests.ps1`.