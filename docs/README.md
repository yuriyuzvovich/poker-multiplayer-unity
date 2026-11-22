# mp-poker docs

mp-poker is an educational project: a light Unity poker client and a light authoritative .NET server for one
No-Limit Texas Hold'em table (2 to 6 seats, bots, sit and stand, buy-in, turn timer, session resume). It shows every
layer between a tap on "Call" and the chips moving on every screen at the table, using well-known approaches (a
layered protocol, command pipelines, state machines, a presentation queue, session resume) in a small, readable form.

These docs describe the system as it is. They are the contract: a change that makes a statement here wrong updates it
in the same change ([development](development.md)).

## What the project teaches

1. Server authority: why the server owns cards, turns, chips and timers, and what the client is still responsible for.
2. Snapshot plus ordered event stream: how a client joins mid-hand and stays in sync afterwards.
3. A layered protocol: transport, packet header, system messages, typed messages, request/response ids.
4. Reliability above TCP: sequence numbers, acks, resend, session resume, full reconnect.
5. Intents vs facts: the client asks, the server decides, everybody animates the decision.
6. Presentation decoupled from network timing: a command queue that plays events at animation speed.
7. Time: server deadlines, client timers with leeway, clock offset.
8. Deterministic, testable game logic: fake clocks, seeded decks, replayed transcripts.
9. Client architecture: composition root, startup commands, models, message bus, MVP, Addressables, pooling.

## Key decisions

| Topic | Decision | Doc |
|---|---|---|
| Server stack | .NET 8 process with Kestrel WebSockets | [server stack](background/server-stack.md) |
| Network model | Server-authoritative, snapshot plus ordered events | [multiplayer models](background/multiplayer-models.md) |
| Shared code | `Poker.Shared` (netstandard2.1): protocol, rules, messages, used by client and server | [architecture](architecture.md) |
| Code organization | Horizontal infrastructure, vertical feature slices, a `Table` kernel | [architecture](architecture.md) |
| Client | Unity 6, VContainer, UniTask, DOTween, Addressables | [client](client.md) |
| Serialization | JSON in a 14-byte binary header; System.Text.Json on the server, Newtonsoft on Unity | [protocol](protocol.md) |
| Concurrency | One actor (mailbox, single reader) per table | [server](server.md) |

## Reading order

| Doc | What it answers |
|---|---|
| [Architecture](architecture.md) | Projects, slices, layers and dependency rules |
| [Protocol](protocol.md) | Bytes on the wire, system messages, the message catalog, error codes |
| [Reliability](reliability.md) | Seq/ack/resend, resume, reconnect |
| [Game authority](game-authority.md) | Who owns what, the hand lifecycle, timers, hidden information, RNG, bots, chips |
| [Sync flows](sync-flows.md) | Sequence diagrams for every important flow |
| [Client](client.md) | Unity client internals |
| [Server](server.md) | Server internals |
| [Testing](testing.md) | How each layer is proven, and the architecture tests |
| [Development](development.md) | Setup, adding a slice or a message, definition of done, commits, CI |
| [Working with AI agents](agentic-workflow.md) | The agent setup, the safety net, a feature step by step |

Background: [server stack](background/server-stack.md) (why .NET), [multiplayer models](background/multiplayer-models.md)
(why this network model), [reading list](background/reading-list.md).

## Repository layout

```
mp-poker/
  AGENTS.md   instructions for AI agents and contributors (CLAUDE.md imports it)
  .claude/    path-scoped rules, skills, review agents, hooks
  .github/    CI
  tools/      PowerShell scripts: setup, server, tests, Unity tests, open Unity
  docs/       these docs; docs/features/ holds designs and plans of features not built yet
  shared/     Poker.Shared (netstandard2.1): Infrastructure/Protocol, Rules, Features/<Slice>; no UnityEngine
  server/     Poker.Server (.NET 8): Infrastructure/ + Features/<Slice>
  tests/      Poker.Tests (xUnit): per slice, infrastructure, shared, integration, architecture
  client/     Unity 6 project: Assets/Infrastructure, Assets/Features/<Slice>, Assets/App (with App/Editor)
```

Feature slices: `Table` (kernel), `Lobby`, `Seating`, `Hand`, `HandHistory`, `TurnTimer`, `Wallet`.

## Glossary

| Term | Meaning here |
|---|---|
| Authority | The side whose decision is final. In mp-poker the server is authoritative for all game state. |
| Intent | A client request ("I want to call"). It can be rejected. |
| Event | A server fact ("Seat 3 called 200"). Every client applies it. |
| Snapshot | The full visible state of a table for one player, sent on join or after a long gap. |
| Event stream | The ordered events after a snapshot. Snapshot plus stream equals current state. |
| Turn id | The server's identity of one turn; a move must carry it, which makes moves idempotent. |
| Seq / ack | Per-direction packet sequence number and the last sequence number received from the peer. |
| Resume | Continuing the same session on a new socket, with unacknowledged packets resent. |
| Reconnect | Starting a new session and rebuilding state from a fresh snapshot. |
| Presentation queue | Client queue that turns events into animations played one after another. |
| Kernel | The `Table` slice: the shared game state other slices build on. |
| Slice | One feature cut through every layer (shared, server, client, tests). |

## Doc rules

- Each doc opens with a title and a `Purpose:` paragraph ending with a link back here.
- Current-state wording only: no milestones, "will", "later" or build diary. Unbuilt ideas go in a short "Extension
  ideas" list at the end of the doc that owns them.
- Name real types and files; a renamed type is renamed here in the same change.