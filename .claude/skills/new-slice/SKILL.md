---
name: new-slice
description: Scaffold a new mp-poker feature slice across every layer (Poker.Shared messages and registration, Poker.Server feature install, Unity client assembly, installer and folders, test folders, docs) following the existing Lobby slice. Use when asked to add a new feature, slice or module, before adding its messages with /add-message.
argument-hint: "<SliceName> [one-line purpose]"
---

# Scaffold a feature slice

A slice is one feature cut through every layer (`docs/architecture.md`). Copy the shape of the smallest existing slice,
`Lobby`, file by file; read each Lobby file before writing its counterpart. Follow `AGENTS.md` and
`.claude/rules/csharp-style.md` (tabs, Allman braces, block namespaces mirroring folders, no `var`, LF, no trailing
newline, no em or en dashes).

## 0. Check first

- The name is PascalCase, singular where natural, and not taken under `shared/Poker.Shared/Features/`.
- The feature really is separate. Code that two slices need belongs in the `Table` kernel (game state) or
  infrastructure (technical); see "When a new piece of code arrives" in `docs/architecture.md`. If the user's idea is a
  use case of an existing slice, say so and stop.
- Agree the wire-name area with the user (lower snake case, for example `chat` for `chat.send`).

## 1. Shared (`shared/Poker.Shared/Features/<Name>/`)

1. `<Name>MessageNames.cs`: `public static class` with the wire-name constants (empty until the first message).
2. `<Name>Messages.cs`: `public static class` with `public static void Register(MessageRegistry registry)` (pattern:
   `LobbyMessages.cs`).
3. `<Name>ErrorCodes.cs` only when the first error code appears.
   Message classes go in `Requests/`, `Events/` and `Models/` subfolders as they appear (`.claude/rules/shared.md`).
4. Add `using Poker.Shared.Features.<Name>;` and `<Name>Messages.Register(registry);` to
   `shared/Poker.Shared/MessageCatalog.cs`.

## 2. Server (`server/Poker.Server/Features/<Name>/`)

1. `<Name>Feature.cs`: `public static class <Name>Feature` with `public static void Install(IServiceCollection services)`
   (pattern: `LobbyFeature.cs`; a block body once it registers more than one thing).
2. Call `<Name>Feature.Install(builder.Services);` in `server/Poker.Server/Infrastructure/Hosting/ServerHost.cs` next to
   the others (the architecture test `CompositionRoot_InstallsEveryFeature` requires it).
3. Server code may use `Poker.Server.Features.Table` (kernel) and infrastructure, never another feature. Reactions to
   other slices go through `ITableEventListener`, `ITableTickWork`, `ISnapshotContributor` or
   `TableContext.GetState<T>()`.

## 3. Client (`client/Assets/Features/<Name>/`)

1. `Features.<Name>.asmdef`: copy `Features.Lobby.asmdef`, change `name` and `rootNamespace`
   (`Poker.Client.Features.<Name>`), keep only the references the slice needs: infrastructure assemblies,
   `Features.Table`, third-party packages and `Poker.Shared.dll`. Never another feature.
2. Folders as needed: `Domain/` (model), `Application/` (flows, commands), `Presentation/` (presenters, screens),
   `Views/` (view interfaces and MonoBehaviours). Do not create empty folders: Unity would need `.meta` files for them.
3. `<Name>Installer.cs`: `public static class` with `public static void Install(IContainerBuilder builder)` (pattern:
   `LobbyInstaller.cs`), called from `client/Assets/App/RootLifetimeScope.cs` next to the other installers, and add
   `Features.<Name>` to `client/Assets/App/App.Root.asmdef` references.
4. When tests come, add `Features.<Name>` to `client/Assets/Tests/EditMode/Tests.EditMode.asmdef` (and PlayMode if
   needed) and put them in `client/Assets/Tests/EditMode/<Name>/`.
5. Prefabs and scenes come from the editor builders (`client/Assets/App/Editor`); extend the builder, never hand-edit YAML.

## 4. Tests and docs

1. Server tests go in `tests/Poker.Tests/Features/<Name>/` with namespace `Poker.Tests.Features.<Name>`.
2. Docs in the same change:
   - Add the slice to the slice table in `docs/architecture.md`.
   - Add it to the server slice table in `docs/server.md` and the client notes in `docs/client.md` once it has code.
   - List the slice in `AGENTS.md` and `docs/README.md`.

## 5. Finish

1. Run `/test`. The architecture tests check the new folders: a missing asmdef, a cross-feature reference or a missing
   `Install` call fails here.
2. Tell the user to focus the Unity Editor so it imports the new folders and writes their `.meta` files, and to commit
   those `.meta` files with the slice. Never write `.meta` files yourself.
3. Continue with `/add-message` for the slice's first message, then the layer order in `docs/development.md`.