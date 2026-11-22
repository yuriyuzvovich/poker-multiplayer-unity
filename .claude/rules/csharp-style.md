---
paths:
  - "**/*.cs"
---

# C# style (mp-poker)

Taken from the existing code. When in doubt, match the nearest sibling file.

## Formatting

- Tabs, width 4. Allman braces. LF line endings. No trailing newline at the end of a file (`.editorconfig`).
- Block-scoped namespaces (`namespace X { ... }`), never file-scoped. Namespace mirrors the folder:
  `Poker.Shared.Features.Hand`, `Poker.Server.Features.Table.Runtime`, `Poker.Client.Features.Hand.Presentation`.
- `using` order: `System*` first, then third-party, then `Poker.*`. Remove unused usings.
- Guard clauses on one line without braces: `if (handle.Status != AsyncOperationStatus.Succeeded) throw ...;`
- Expression bodies (`=>`) for one-statement members; statement bodies for anything longer.
- Do not reflow, reorder or reformat lines unrelated to the change.

## Types and declarations

- Never `var`; always the explicit type, including `out int value`.
- Target-typed `new()` whenever the type is on the left, on every side (C# 9 in Unity too): locals, fields, `using`
  declarations and property initializers, for example `JsonPayloadSerializer serializer = new();`
- Classes are `sealed` unless designed for inheritance. Small immutable values may be `readonly struct` (client) or
  `record` (server domain).
- Explicit access modifiers on every member.
- Private fields `_camelCase`, `readonly` whenever possible. Locals and parameters `camelCase`. Everything else
  PascalCase, including constants.
- Constructor null checks: `_engine = engine ?? throw new ArgumentNullException(nameof(engine));`
- `""` instead of `string.Empty`. In the .NET projects nullable reference types are on and warnings
  are errors (`Directory.Build.props`); `client/Directory.Build.props` keeps that out of the Unity projects.

## Code hygiene

- No LINQ in hot paths (per-message, per-frame, per-tick); plain loops.
- Delete dead code and unused members instead of commenting them out.
- Comments explain why, not what. No em or en dashes in code or comments.
- One type per file, file name equals type name.

## Files and folders

- At most 8 `.cs` files in one folder (enforced by `FolderSizeTests`). Once a folder passes about 6, group its files by
  responsibility into subfolders; do not wait for the test.
- A subfolder is named after the concern its files share, as a noun: `Requests`, `Events`, `Models`, `Popups`,
  `Connection`, `Seats`. The module's entry point (installer, root model, `<Slice>Messages`) stays at the module root.
- The namespace mirrors the folder, so a new subfolder is a new namespace; add the `using` where its types are used.
- Never name a folder after a type it contains (`HandScene/HandScene.cs` makes `HandScene` ambiguous) or after a common
  type or namespace its code uses (`Time`, `Text`, `Directory`, `System`): inside that namespace the folder name
  would shadow it.
- Unity: move a file together with its `.meta` (`git mv` both); a new folder gets its `.meta` from the Editor.