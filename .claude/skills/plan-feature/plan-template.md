# Plan: <name>

Status: draft
Scope: shared/Poker.Shared/Features/<Name>/, server/Poker.Server/Features/<Name>/, client/Assets/Features/<Name>/

Design: [design.md](design.md). Review: [review.md](review.md).

## Decisions

- [x] D1. <question>: <answer> (decided by the user)
- [ ] D2. <question> (options: <recommended>, <other>)

## Placement

Slice, kernel or infrastructure for each part, and the extension points used instead of cross-feature references.

## Implementation steps

1. **Shared.** Messages (`/add-message`), wire names, registration, error codes, shared rules. Files: ...
2. **Server.** Handlers, work items, listeners, options, `<Name>Feature.Install`. Files: ...
3. **Client domain and application.** Model, flows, subscriptions, installer. Files: ...
4. **Presenters and views.** Presenters, visual commands, view interfaces, builder changes. Files: ...
5. **Docs.** Reference docs to update when it ships: ...

## Test plan

| Behaviour | Test (level, name) | Mutation that turns it red |
|---|---|---|
| ... | `tests/Poker.Tests/Features/<Name>/...`: `Subject_Scenario_Outcome` | ... |

## Risks

| Risk | Containment |
|---|---|
| ... | ... |

## Commits

1. "..."