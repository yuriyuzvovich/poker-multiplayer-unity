---
name: plan-feature
description: Read a feature design document for mp-poker (docs/features/<name>/design.md or a given path), review it against the architecture, authority rules, protocol conventions, docs and existing code, list inconsistencies and gaps, ask the user to settle the important decisions, and write a review plus an implementation and test plan that gates coding until approved. Use when the user has a feature design, spec or idea document to assess or plan before implementation.
argument-hint: "<feature-name | path/to/design.md>"
---

# Review a feature design and plan it

Output lives in `docs/features/<name>/`: the user's `design.md`, and the `review.md` and `plan.md` this skill writes.
While `plan.md` is not approved, or has open decisions, the `feature-gate` hook makes Claude Code ask the user before
any edit inside the plan's `Scope`. That is how important decisions reach the user before code is written. These
files are working documents: future tense is fine here, and the folder is deleted when the feature ships.

Do not write production code in this skill. Write all files with LF line endings, no trailing newline, no em or en
dashes.

## 1. Find the design

- The argument is a feature name: read `docs/features/<name>/design.md`. If it is a path elsewhere, copy it to
  `docs/features/<name>/design.md`, where `<name>` is lower-case kebab case, and tell the user.
- If there is no design yet, create `design.md` from [design-template.md](design-template.md), fill what the user
  told you, and ask them to complete it. Stop there.

## 2. Load the context

- Read `AGENTS.md` and `docs/architecture.md`.
- Read the docs the feature touches: `docs/protocol.md` for messages, `docs/game-authority.md` for game state, chips,
  timers or hidden cards, `docs/sync-flows.md` and `docs/reliability.md` for join, reconnect or ordering,
  `docs/client.md` and `docs/server.md` for internals, and `docs/testing.md`.
- Grep the code for every name, message and slice the design mentions, and for existing code that already does part of
  it.

## 3. Review

Check the design against each list and record every finding with a severity. **Blocker**: it contradicts a rule or
cannot work. **Gap**: it is missing something the implementation needs. **Risk**: it can work, but is fragile or
costly. **Nit**.

- **Architecture.** Is it a new slice, a use case of an existing slice, kernel state or infrastructure ("When a new
  piece of code arrives")? Would any part need a feature to reference another feature? Name the
  `ITableEventListener`, `ITableTickWork`, `ISnapshotContributor`, bus event or kernel extension point that replaces
  that reference.
- **Authority and fairness.** The server decides everything that matters. Hidden information is addressed per
  receiver. Moves carry `TurnId` where turns are involved. Chips are conserved (`IChipBank`, display lock). There are no
  client-side outcomes.
- **Protocol.** Wire names follow `area.verb` and do not clash with existing ones. Every intent is a request with an
  accepted response and an authoritative event. Error codes are defined. Payloads carry absolute values. Unknown fields
  stay backward compatible.
- **Sync and reliability.** What a viewer joining mid-way sees (snapshot contribution), what happens on resume and on
  full reconnect, and the ordering against existing events (responses can arrive after related events).
- **Time and determinism.** Deadlines through `IClock` and ticks. Testable with `ManualClock`, `ScriptedDeck` or
  `SeededDeckFactory`.
- **Client.** Which presenter owns it, which animations go through the `PresentationQueue`, what waits for an
  animation (`DisplayLock`), which views and popups are involved, and which builder produces the prefabs.
- **Consistency.** Contradictions inside the design; conflicts with the docs or the code as they are; undefined terms.
- **Scope.** Parts that could ship later, and parts the design forgot (errors, limits, empty states, leaving the table,
  bots).

## 4. Decision checkpoints

Collect the questions only the user can answer: scope cuts, slice placement when it is debatable, protocol shape,
authority trade-offs, persistence, behaviour on reconnect, limits and numbers, anything that changes an existing rule
or message. Ask them with the AskUserQuestion tool, at most 4 per call and more rounds if needed.
- Each question stands on its own and names the feature.
- Options are concrete, with the recommended one first and marked "(Recommended)".
- Each option's description says the consequence.

Do not ask about what the rules already decide; state those as findings instead. If the user defers a decision, keep
it open.

A decision that would change a project rule (`AGENTS.md`, the architecture tests) is always a question, never your
call.

## 5. Write `review.md`

From [review-template.md](review-template.md): summary verdict, findings grouped by severity (each with what the design
says, the rule, doc or code it conflicts with as `path:line`, and the proposed resolution), and what the design does
well.

## 6. Write `plan.md`

From [plan-template.md](plan-template.md). The hook reads three fields, so keep their format exact:
- `Status: draft` until the user approves the whole plan, then `Status: approved`.
- `Scope:` the comma-separated repo paths the implementation will create or edit, for example
  `shared/Poker.Shared/Features/Chat/, server/Poker.Server/Features/Chat/, client/Assets/Features/Chat/,
  shared/Poker.Shared/MessageCatalog.cs`.
- Decisions as `- [x] D1. <question>: <answer>` when settled and `- [ ] D2. <question> (options: ...)` when open.

Then the body:
- Implementation steps in layer order (shared, server, client domain, presenters and views, docs). Each step lists
  files, types and messages, and the skill to use (`/new-slice`, `/add-message`).
- Test plan: the exit test of each behaviour, named the way the test rules say, at the right level (rules, actor with
  `ManualClock`, integration, Edit Mode, PlayMode). Add the mutation that proves each test would go red.
- The reference docs to update, risks and how each is contained, and the commit split.

## 7. Approval

Show the user a short summary: the verdict, the blocker count, the open decisions and the plan outline. Ask with
AskUserQuestion whether to approve the plan, change it, or keep it as a draft.
- On approval with no open decisions, set `Status: approved`.
- While decisions are open, keep `draft`; the gate will ask before edits in scope.

## Afterwards

- Implement with the plan (`/new-slice`, `/add-message`, `/test`, `/arch-review`).
- If implementation reveals a new decision, add it as an open `D` line. The gate then asks again until it is
  resolved.
- When the feature ships, `/update-docs` moves the lasting facts into the reference docs; then delete
  `docs/features/<name>/` in the same change, so no build history stays in the docs.