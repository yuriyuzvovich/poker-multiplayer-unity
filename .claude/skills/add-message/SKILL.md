---
name: add-message
description: Add a new protocol message to mp-poker end to end, across Poker.Shared (message class, wire name, registration), Poker.Server (handler and feature install for requests), the Unity client (subscription or request call) and tests. Use when a slice needs a new request, response or event.
argument-hint: "<Slice> <Name> <request|response|event> [wire name]"
---

# Add a protocol message

Arguments: slice (an existing folder under `shared/Poker.Shared/Features/`, or run `/new-slice` first), base name, kind, and
optionally the wire name. If the user gave no wire name, propose one in the slice's dotted lower snake case
(`"turn.act"`, `"hand.hole_cards"`) and confirm it. Before writing, read the slice's existing files and copy their shape
exactly. Follow `.claude/rules/csharp-style.md`.

## 1. Shared (`shared/Poker.Shared/Features/<Slice>/`)

1. `<Name><Kind>.cs`: kind suffix `Request`, `Response` or `Event`, in `Requests/` (a request and its response) or
   `Events/`, or in the slice's topic folder if it has one (`Hand/Turns`). A DTO or enum the message carries goes in
   `Models/`. A `public sealed class` in the folder's namespace (`Poker.Shared.Features.<Slice>.Requests`), with public `{ get; set; }` properties only; strings `= ""`, lists `= new()`.
   Requests that target a table carry `TableId`; moves carry `TurnId`.
2. `<Slice>MessageNames.cs`: add `public const string <Name> = "<wire.name>";`
3. `<Slice>Messages.cs`: add `registry.Register<<Name><Kind>>(<Slice>MessageNames.<Name>);` next to its siblings.
4. A new slice also needs `<Slice>Messages.Register` added to `MessageCatalog.CreateRegistry`.

## 2. Server (requests only, `server/Poker.Server/Features/<Slice>/`)

1. `<Name>Handler.cs`: `public sealed class <Name>Handler : MessageHandler<<Name>Request>`. Inject dependencies with
   `?? throw new ArgumentNullException(nameof(x))` checks. If the request touches table state, post a work item through
   `TableDirectory.PostOrRejectAsync` (see `TurnActHandler.cs`, `BuyInHandler.cs`); do not mutate table state in the
   handler.
2. `<Slice>Feature.cs`: add `services.AddSingleton<IMessageHandler, <Name>Handler>();`
3. Events are sent from the actor or work item through the slice's existing broadcast/outbox path; respect per-receiver
   visibility for anything hidden.

## 3. Client (`client/Assets/Features/<Slice>/`)

- Event: subscribe in the presenter that owns the reaction:
  `_subscriptions.Add(_connection.Subscribe<<Name>Event>(On<Name>));` and, if it animates, enqueue an
  `IVisualCommand` in the `PresentationQueue` instead of animating in the handler.
- Request: call `_connection.RequestAsync<<Name>Request, <Name>Response>(request, cancellationToken)` from the
  flow/presenter that owns the intent.
- Only wire the client if the user wants it now; otherwise list where it will go.

## 4. Tests

- `tests/Poker.Tests/Features/<Slice>/`: a serialization round trip through `JsonPayloadSerializer` (pattern:
  `tests/Poker.Tests/Infrastructure/Messaging/JsonPayloadSerializerTests.cs`), plus a handler or actor test with `ManualClock`
  and a scripted deck when there is behaviour.
- Registry collisions are covered by the existing catalog test; run `/test` to confirm.

## 5. Finish

1. Run `/test`.
2. If files were added under `client/Assets/`, remind the user to focus Unity so it generates the `.meta` files.
3. Add the message (and any new error code) to the catalog in `docs/protocol.md`, and to `docs/sync-flows.md` if it
   changes a flow, in the same change.