---
name: arch-review
description: Review the current mp-poker change (working tree and branch against main) for architecture, layering, authority and project style violations, run the architecture tests, and report findings with file:line. Use before committing or opening a pull request, or when asked whether a change follows the project's rules.
argument-hint: "[git range, default: working tree + branch vs main]"
---

# Review a change against the project rules

This is a project-rules review. For general bug hunting the built-in `/code-review` exists; this skill checks what is
specific to mp-poker. Do not edit files unless the user asks for fixes afterwards.

## 1. Collect the change

- Range: the argument, or `git diff main...HEAD` plus `git diff HEAD` (staged and unstaged). `git diff --stat` first.
- Read `AGENTS.md`, then the `.claude/rules/*.md` files whose paths the change touches.

## 2. Run the enforced rules

```powershell
./tools/test.ps1 -Filter "FullyQualifiedName~Architecture"
```

Any failure is a finding (cross-feature reference, Unity in shared code, missing asmdef or `Install` call, CRLF,
trailing newline, dash, namespace not mirroring its folder). If the change touches behaviour, also run the full
`./tools/test.ps1`.

## 3. Check what tests cannot see

1. **Placement.** Is each new type in the right slice, kernel or infrastructure ("When a new piece of code arrives" in
   `docs/architecture.md`)? Duplicates across slices should move down.
2. **Layering inside a client slice.** Views render and raise input only; presenters do not mutate domain state
   directly; application code touches no Unity UI types; domain knows no network or UI.
3. **Client rules.** UniTask only (no coroutines, no `async void`), `CancellationToken` last; animations that react to
   the network go through the `PresentationQueue` as `IVisualCommand`s carrying absolute values; money on screen
   waits for its animation (`DisplayLock`); subscriptions disposed with their owner; views through `ViewLoader` or
   `LazyView`; no hand-written `.meta`, no hand-edited scene or prefab YAML.
4. **Server rules.** Thin handlers posting work through `TableDirectory.PostOrRejectAsync`; state changed only inside
   the actor; `IClock` and `IDeckFactory` instead of wall clock and randomness; cross-slice reactions through
   `ITableEventListener` or `ITableTickWork`.
5. **Authority.** If the change touches `server/`, `shared/` or client network code, run the `authority-reviewer`
   agent on the same range and merge its findings.
6. **Style beyond the hygiene test.** `.claude/rules/csharp-style.md`: explicit types, `sealed`, `_camelCase` fields,
   null checks with `?? throw new ArgumentNullException(nameof(x))`, `""` over `string.Empty`, no LINQ in per-message
   or per-frame code, comments that explain why.
7. **Tests.** Behaviour changed without a test that would fail against the defect; tests that use `Task.Delay`, the
   wall clock or real randomness.
8. **Docs.** Statements the change made stale (use the `docs-sync` agent when the change is large); history wording in
   docs.

## 4. Report

Findings ranked by severity: **Broken rule** (architecture, authority, determinism), **Correctness**,
**Missing test**, **Docs**, **Style**. Each: `path:line`, the rule it breaks (cite the rules file or doc), one sentence
on why it matters, and the concrete fix. End with the architecture test result and a one-line verdict. No em or en
dashes.