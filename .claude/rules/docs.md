---
paths:
  - "docs/**"
  - "AGENTS.md"
  - "README.md"
---

# Docs rules

- The docs describe the system as it is now; they are the contract. A change that makes a statement wrong updates it
  in the same change (`/update-docs`, or the `docs-sync` agent to find stale statements).
- Each doc opens with `# Title`, then a `Purpose:` paragraph ending with `Back to [README](README.md).` (`../README.md`
  under `docs/background/`).
- Exception: `docs/features/<name>/` holds designs and plans of features not built yet (`/plan-feature`). Future tense
  is fine there, and the folder is deleted when the feature ships.
- Current-state wording only: no milestones, phases, "will", "later", "v1", test counts or build diary. Unbuilt ideas
  go in a short "Extension ideas" list at the end of the doc that owns them, phrased as options.
- Name real types, files and messages, and check them with Grep before writing; a rename in code is a rename here.
- Where each area is documented: the map in `docs/development.md` (Definition of done).
- Plain English, short sentences, tables for catalogs, mermaid for flows. No em or en dashes; use commas, colons or
  parentheses. Links are relative and must resolve.