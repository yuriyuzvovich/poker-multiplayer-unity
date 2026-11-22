@AGENTS.md

## Claude Code

Everything above applies. Claude-specific tooling in `.claude/`:

- **Rules** (`.claude/rules/*.md`) load automatically for the paths they name.
- **Skills:**
  - `/test`: .NET tests with a failure summary.
  - `/unity-tests`: EditMode or PlayMode, headless.
  - `/run-local`: server plus Unity or PlayMode.
  - `/add-message`: one protocol message end to end.
  - `/plan-feature`: review a feature design, settle decisions with the user, write the review and the plan.
  - `/new-slice`: scaffold a feature slice across all layers.
  - `/arch-review`: check a change against the architecture and style rules.
  - `/update-docs`: bring the docs in line with a change.
- **Agents:**
  - `authority-reviewer`: hidden-card leaks, client-side decisions, `TurnId`, determinism, chip invariants.
  - `docs-sync`: doc statements a change made stale.
- **Hooks:**
  - Block hand-written `.meta` files under `client/Assets`.
  - Feature gate: ask the user before editing files in a feature plan's `Scope` while that plan is a draft or has open
    decisions (`docs/features/<name>/plan.md`).
  - Shell guard: ask the user when a Bash or PowerShell command looks like it writes a `.meta` file or a file in an
    unsettled plan's scope, so the shell cannot bypass the two guards above.
  - Flag CRLF, trailing newlines and em/en dashes in edited repo files.
  - In cloud sessions, install the pinned .NET SDK and PowerShell at session start.
- **Personal overrides** go in `.claude/settings.local.json` and `CLAUDE.local.md` (both gitignored).