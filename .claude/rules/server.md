---
paths:
  - "server/**"
---

# Poker.Server rules

- .NET 8 with Microsoft.Extensions.DependencyInjection. Each slice exposes `<Slice>Feature.Install(IServiceCollection)`,
  called from `Infrastructure/Hosting/ServerHost.cs`. Handlers register as `IMessageHandler`.
- A handler derives from `MessageHandler<T>` and stays thin: it posts work to the table actor through
  `TableDirectory.PostOrRejectAsync(context, tableId, work, cancellationToken)`. Game state is changed only inside the
  actor (mailbox, one message at a time), never from the handler thread.
- Time: game logic reads the injected `IClock` (`Infrastructure/Time`). Never `DateTime.UtcNow`,
  `DateTimeOffset.UtcNow`, `Task.Delay` or timers inside rules, so tests can drive a `ManualClock`.
- Randomness goes through `IDeckFactory` (`SecureDeckFactory` in production, seeded or scripted in tests). The only
  other randomness is bot think time and choices in `HandEngine`, from a `Random` seeded by `HandOptions.BotSeed` when
  set (tests set it). New randomness follows one of these two patterns.
- Authority:
  - Hidden information is addressed per receiver: hole cards go only to their owner until showdown.
  - Every move carries the server's `TurnId`; stale or repeated ids are rejected, which makes moves idempotent.
  - The server validates every intent; the client's view of legality is only a hint.
- Invariants after every table message (`docs/testing.md`, checked by `HandSimulationTests`): stacks plus pots
  constant within a hand, hole cards addressed to their owner, the seat to act is in the hand, `TurnId` strictly
  increasing, no negative stack.
- Cross-slice reactions use `ITableEventListener` or `ITableTickWork`, not direct references between slices.