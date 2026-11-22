# Feature designs in progress

Purpose: the working area for features that are designed but not yet built. Unlike the rest of `docs/`, these files
describe the future and are temporary. Back to [README](../README.md).

## How it works

1. Write the design as `docs/features/<name>/design.md`, where `<name>` is lower-case kebab case such as `table-chat`.
   `/plan-feature <name>` creates it from a template if it does not exist.
2. Run `/plan-feature <name>`. It reviews the design against the architecture, authority rules, protocol and code, asks
   you to settle the important decisions, and writes `review.md` (inconsistencies and gaps) and `plan.md`
   (decisions, implementation steps, test plan, risks, commits).
3. While `plan.md` says `Status: draft` or lists an open decision (`- [ ] D<n>. ...`), the `feature-gate` hook asks
   for your confirmation before Claude edits any file in the plan's `Scope`. Approve the plan, or answer the
   decisions, to lift the gate.
4. Build the feature following the plan. A new decision found on the way goes back into `plan.md` as an open line.
5. When it ships, the lasting facts move into the reference docs (`/update-docs`), and the feature's folder is deleted
   in the same change.

Other agents can follow the same steps by hand: the plan is plain markdown, and [development](../development.md) has
the build order.