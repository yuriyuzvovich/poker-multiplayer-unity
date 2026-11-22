# Reading list

Purpose: sources to deepen each part of the project, grouped by topic. Back to [README](../README.md).

## Multiplayer models and netcode

| Source | Why |
|---|---|
| Glenn Fiedler, "Networking for Game Programmers" and "Networked Physics" series, gafferongames.com | Reliability over unreliable transports, sequence and ack bitfields, snapshot interpolation |
| Gabriel Gambetta, "Fast-Paced Multiplayer", gabrielgambetta.com/client-server-game-architecture.html | Client prediction, server reconciliation, entity interpolation, lag compensation, clearly illustrated |
| Valve, "Source Multiplayer Networking", developer.valvesoftware.com | Tick rate, interpolation delay, lag compensation in a shipped engine |
| Timothy Ford, "Overwatch Gameplay Architecture and Netcode", GDC 2017 | ECS, prediction and rollback at scale |
| GGPO documentation, ggpo.net | Rollback netcode explained by its authors |

## Server side (.NET)

| Source | Why |
|---|---|
| Microsoft Learn, "WebSockets support in ASP.NET Core" | Endpoint, keep-alive, close handshake |
| Microsoft Learn, "System.Threading.Channels" | Mailbox for the table actor |
| Microsoft Learn, "System.Text.Json source generation" | Reflection-free serialization usable from IL2CPP |
| MessagePack-CSharp README (github.com/MessagePack-CSharp) | Binary format with generated formatters (an option behind the `BinaryPayload` flag) |
| Microsoft Orleans documentation | Virtual actors, to compare with the table actor and for scaling out |

## Client side (Unity)

| Source | Why |
|---|---|
| VContainer documentation (vcontainer.hadashikick.jp) | Lifetime scopes, entry points, source generator |
| UniTask README (github.com/Cysharp/UniTask) | Cancellation, player loop, `ToUniTask` for tweens |
| Unity Manual, "Scripting restrictions" (IL2CPP, AOT) | What `Poker.Shared` may use |

## Architecture and design patterns

| Source | Why |
|---|---|
| Martin Fowler, "Event Sourcing" and "Domain Event" (martinfowler.com) | Snapshot plus event stream as a design |
| Eric Evans and Martin Fowler, "Specifications" (martinfowler.com/apsupp/spec.pdf) | Condition objects for popup rules |
| Robert Nystrom, "Game Programming Patterns" (gameprogrammingpatterns.com): Command, State, Observer, Event Queue, Object Pool | Short, game-focused explanations of the client patterns |
| Vaughn Vernon, "Effective Aggregate Design" | Aggregate boundaries for `TableModel` |
| NCalc project (github.com/ncalc) | Small expression language, for data-driven rules such as popup conditions |

## Poker specifics

| Source | Why |
|---|---|
| Robert's Rules of Poker (free PDF) | Betting, all-in and side-pot rules to encode and test |
| "Cactus Kev" poker hand evaluator write-up and the "Two Plus Two" evaluator | Fast hand evaluation approaches |