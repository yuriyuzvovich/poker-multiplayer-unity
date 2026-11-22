---
name: update-docs
description: Bring mp-poker's docs (docs/, AGENTS.md, README.md) in line with the current change, in current-state wording, as part of the same change. Use after changing behaviour, messages, structure, tools or workflow, or when asked to update or check the docs.
argument-hint: "[git range, default: working tree + branch vs main]"
---

# Update the docs for a change

The docs describe the system as it is and are the contract. They change in the same change as the code, never in a
separate history entry. Read `.claude/rules/docs.md` first.

## Steps

1. **Find what is stale.** Run the `docs-sync` agent on the range (default: working tree plus the branch against
   `main`). For a small change, do its steps yourself: list the behaviour and structure changes, map them to docs with
   the table in `docs/development.md` (Definition of done), and grep the docs for old names.
2. **Edit.** For each item, rewrite the statement so it describes the code as it is now:
   - Name the real types, messages and files, and grep each one to confirm it exists.
   - Catalogs (messages, error codes, slices, assemblies, tools) are tables; keep them complete and in the existing
     order.
   - No milestones, phases, "will", "later", "as built", test counts or narrative of how it was built. An idea that is
     not built goes in the doc's "Extension ideas" list.
   - Keep each doc's opening `Purpose:` paragraph and back link.
3. **Cross-cutting files.** A new or renamed slice, tool, command or rule also updates `AGENTS.md`, `README.md` and
   `docs/README.md`. A changed dependency rule updates `docs/architecture.md`, `AGENTS.md` and the architecture tests
   together.
4. **Verify.**
   - Every relative link resolves.
   - `./tools/test.ps1 -Filter "FullyQualifiedName~SourceHygiene"` passes (LF line endings, no trailing newline, no em
     or en dashes).
5. **Report** the docs changed, one line each, and anything left for the user to decide.