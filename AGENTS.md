# mp-poker: instructions for agents and contributors

This file is the single source of project instructions for any AI coding agent (Claude Code reads it through
`CLAUDE.md`; Codex, Cursor, Copilot, Gemini and others read it directly) and for people. Where a personal or
user-level instruction disagrees with this file or `.claude/rules/`, follow these.

## What this is

An educational multiplayer No-Limit Texas Hold'em: a light Unity client and a light authoritative .NET server for one
table (2 to 6 seats, bots, sit/stand, buy-in, turn timer, session resume). The goal is a small, readable codebase that
shows every layer between a tap on "Call" and chips moving on every screen. The design docs in [docs/](docs/README.md)
describe the system as it is and are the contract.

## Layout

| Path | What |
|---|---|
| `shared/Poker.Shared` | netstandard2.1, no UnityEngine: protocol (`Infrastructure/Protocol`), rules (`Rules/`), messages per slice (`Features/<Slice>`) |
| `server/Poker.Server` | .NET 8, Kestrel WebSockets: `Infrastructure/` (Hosting, Messaging, Session, Time, Transport) + `Features/<Slice>` |
| `tests/Poker.Tests` | xUnit: `Features/<Slice>`, `Infrastructure/`, `Shared/`, `Integration/`, `Architecture/`; doubles in `TestDoubles/` |
| `client/` | Unity 6000.0.79f1, VContainer, UniTask, DOTween, Addressables: `Assets/Infrastructure/` (Core, Network, UI, Diagnostics), `Assets/Features/<Slice>`, `Assets/App/` (composition root; `App/Editor/` builders), `Assets/Tests/` |
| `tools/` | PowerShell scripts (Windows PowerShell 5.1 or PowerShell 7 on any OS) |
| `docs/` | Current-state design docs |

Slices: `Table` (kernel), `Lobby`, `Seating`, `Hand`, `HandHistory` (read model), `TurnTimer`, `Wallet`.

## Commands (repo root)

```powershell
./tools/setup.ps1                              # fresh clone: finds the SDK and Unity, builds Poker.Shared.dll into client/
./tools/test.ps1 [-Filter <expr>]              # build with warnings as errors, run every .NET test (incl. architecture)
./tools/server.ps1 [-Background] [-Port 5080]  # ws://localhost:5080/ws; ./tools/server.ps1 -Stop stops it
./tools/unity-tests.ps1 -Platform EditMode     # headless Test Runner; PlayMode needs a server or -StartServer
./tools/open-unity.ps1                         # open client/ in the matching Unity editor
```

- The scripts find dotnet (PATH, `DOTNET_ROOT`, per-user installs; must satisfy `global.json`) and Unity
  (`UNITY_EDITOR`, then Unity Hub's folders) on their own. Never hardcode machine paths.
- Exit code 3 means the environment refused: a running server locks the DLLs (stop it first), or an open Unity Editor
  holds the project (batch mode cannot share it; never kill the Editor yourself).
- If only `dotnet` is available, `dotnet test mp-poker.sln` is equivalent to `./tools/test.ps1`.
- Client compile check without the Editor: once Unity has generated `client/*.csproj` (it does on open, with the Rider
  package), `dotnet build client/Tests.EditMode.csproj` compiles every client assembly the Edit Mode tests use. The
  project files list sources explicitly, so files added since the last Unity refresh are not in them yet.

## Architecture rules (enforced by `tests/Poker.Tests/Architecture`)

1. A feature may use infrastructure, the `Table` kernel and `Poker.Shared`; never another feature's implementation.
   Shared message contracts of other slices may be read; their server or client code may not.
2. Features react to each other only through events: client bus events, server table events and
   `ITableEventListener`.
3. Infrastructure never references features.
4. `Poker.Shared` references nothing outside the BCL subset Unity supports; no `UnityEngine`.
5. Rules both sides need live in `shared/`; server-only decisions (shuffle, deadlines) in `server/Features/<Slice>`.
6. Only `ServerHost` (server) and `App.Root` (client, plus `App.Editor` and test assemblies) know all features.
7. A duplicate across slices moves down to the kernel or infrastructure.

Placement guide and diagrams: [docs/architecture.md](docs/architecture.md).

## Authority and determinism

- The server is authoritative for cards, turns, chips and timers. The client sends intents and animates facts; it never
  moves chips before the server's event.
- Hidden information is addressed per receiver (`TableEvent(message, recipientPlayerId)`); hole cards go only to their
  owner until showdown.
- Every move carries the server's `TurnId`; stale ids are rejected.
- Game state changes only inside the table actor (post work through `TableDirectory.PostOrRejectAsync`).
- Server logic reads the injected `IClock` and `IDeckFactory`: no `DateTime.UtcNow`, `Task.Delay`, `System.Random` or
  timers in rules. Tests use `ManualClock`, `ScriptedDeck`, `SeededDeckFactory`.

## Style rules (read the matching file before editing)

| Editing | Read |
|---|---|
| any `*.cs` | [.claude/rules/csharp-style.md](.claude/rules/csharp-style.md) |
| `shared/**` | [.claude/rules/shared.md](.claude/rules/shared.md) |
| `server/**` | [.claude/rules/server.md](.claude/rules/server.md) |
| `client/**` | [.claude/rules/client.md](.claude/rules/client.md) |
| `tests/**`, `client/Assets/Tests/**` | [.claude/rules/tests.md](.claude/rules/tests.md) |
| `docs/**` | [.claude/rules/docs.md](.claude/rules/docs.md) |

The essentials: tabs, Allman braces, block-scoped namespaces that mirror folders, at most 8 `.cs` files per folder
(group by responsibility into subfolders before that), no `var`, `sealed` by default, `_camelCase` fields, LF line endings, no trailing newline at end of file, no em or en dashes anywhere. The source
hygiene test fails on CRLF, trailing newlines, dashes, namespace/folder mismatches and broken relative links.

## Unity specifics

- Never create or edit `.meta` files by hand. After adding files under `client/Assets/`, the Unity Editor must import
  them (ask the user to focus it); do not commit assets without their `.meta`.
- Scenes and prefabs are generated by editor builders (`MainSceneBuilder`, `UiPrefabBuilder`, `UiSkinBuilder`,
  `SpriteAtlasBuilder`, `FontSetup`, menu `mp-poker`); change the builder and rerun it, never hand-edit the YAML.
- `Poker.Shared.dll` is built by the .NET solution into `client/Assets/Plugins/Poker.Shared/` and is not committed;
  rebuild after changing `shared/`.
- Third-party code under `client/Assets/Plugins/` and `client/Assets/TextMesh Pro/` is never edited.

## Workflow

A human-readable tour of this setup is in [docs/agentic-workflow.md](docs/agentic-workflow.md).

- A non-trivial feature starts as a design in `docs/features/<name>/design.md`, reviewed and planned before coding
  (`/plan-feature`; by hand: write `review.md` and `plan.md` next to it, see [docs/features](docs/features/README.md)).
  Important decisions belong to the user: ask, record them in `plan.md`, and do not start coding in the plan's scope
  while it is a draft or has open decisions.
- Work a feature in layers ([docs/development.md](docs/development.md)): shared messages and rules with tests, server
  handlers through the actor (manual clock, scripted deck), client domain with Edit Mode tests and fake network events,
  then presenters and views.
- Missing infrastructure is built minimally in infrastructure with its own tests, never inside a slice.
- Exit test first or alongside: shown red against the defect, then green. Never weaken an assertion to pass.
- Update the docs that describe the changed area in the same change, in current-state wording.
- Create and change source files with your edit tools, not through shell commands (`sed -i`, redirects,
  `Set-Content`): edits are what the checks and the user review. Shell writes are for generated output only.
- Done means `./tools/test.ps1` is green (and Unity Edit Mode tests when the client changed).

## Commits

- One past-tense plain sentence, no prefix, no trailing period, for example "Kept outgoing packets in order under
  simulated latency".
- One logical change per commit, docs included. Commit or push only when asked.

## Ground rules

- Original project: generic Hold'em with its own names, messages, protocol and assets.
- Keep it small and readable: prefer the simplest design that keeps the rules above; add abstractions only when a
  second user exists.
- No secrets, machine paths or personal data in the repository.