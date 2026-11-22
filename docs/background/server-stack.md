# Server stack: options and decision

Purpose: compare server technologies for a light, educational, authoritative poker server and record the choice.
Back to [README](../README.md).

## Requirements

| # | Requirement | Why |
|---|---|---|
| R1 | Server-authoritative game logic | Poker has hidden information and money-like state |
| R2 | Persistent bidirectional connection | Server pushes events; client sends intents |
| R3 | Rules written once | Client needs hand ranks, pot math and validation for UI; server needs them for truth |
| R4 | Deterministic, testable table logic | Fake clock, seeded deck, replayed transcripts |
| R5 | Every layer visible | The goal is learning the socket, packet and reliability layers, not hiding them |
| R6 | Cheap to run locally | One machine, no cloud account required |
| R7 | Concurrency model that is easy to reason about | Many tables later, no locks in game logic |

## Options

| Option | Shared rules (R3) | Layers visible (R5) | Concurrency (R7) | Notes |
|---|---|---|---|---|
| **.NET 8 console + Kestrel WebSockets** | Yes: one C# library for Unity and server | Yes: raw `WebSocket`, own framing | `Channel<T>` mailbox per table, async/await | Same language as the client; xUnit; first-class WebSocket support in ASP.NET Core |
| Go | No: rules twice (Go and C#) | Yes | Goroutine per table, channels | Great actor model; learning a second ecosystem is a bonus but rules drift is a risk |
| Node.js / TypeScript | No: rules in TS and C# | Yes | Single thread, event loop | Fastest prototype; weaker typing at the boundary |
| Unity headless server (Netcode for GameObjects) | Yes | No: transport and reliability hidden | Unity main thread | Good for real-time action games, wrong tool for learning the protocol |
| Nakama / Colyseus / Photon | Partly (server modules in Go/Lua/TS or vendor SDK) | Partly | Framework-defined | Production features for free, but the interesting parts are the framework's |
| Erlang / Elixir | No | Yes | Best-in-class actors and supervision | Excellent for tables-as-processes, far from Unity |

## Decision

**.NET 8 console server hosting Kestrel WebSockets, with a shared netstandard2.1 library.**

- R3 is the deciding factor: `Poker.Shared` (cards, hand evaluator, betting rules, protocol messages, serializers)
  compiles into both Unity and the server. A rule is fixed once and tested once.
- R5: the server reads and writes raw WebSocket frames; packet header, sequence numbers and resume are ours to write.
- R7: each table is a single-threaded actor reading a `System.Threading.Channels.Channel<T>` mailbox. Table code needs
  no locks; concurrency lives only at the session and routing layers.
- R4: the table actor depends on `IClock` and `IDeckFactory`, both replaceable in tests.

## Constraints that follow

- `Poker.Shared` targets netstandard2.1 and uses only APIs Unity's IL2CPP supports. Serialization is plugged in per side:
  System.Text.Json on the server and Newtonsoft (with `link.xml`) on Unity behind one `IPayloadSerializer`; MessagePack
  or source generation are options.
- No `UnityEngine` references in `Poker.Shared`.
- The server is a plain process (`dotnet run`, or `./tools/server.ps1`); a container is optional.

## What we give up

- No built-in matchmaking, accounts, leaderboards or storage (we build minimal versions or stub them).
- Horizontal scaling is out of scope; the design keeps a seam for it (tables addressed by id, sessions separate from
  tables), so a second process can be added.