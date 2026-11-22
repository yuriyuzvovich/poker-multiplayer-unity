---
paths:
  - "shared/**"
---

# Poker.Shared rules

- Target is netstandard2.1 and the DLL is consumed by Unity: BCL only, no UnityEngine, no ASP.NET, no packages Unity
  cannot load. Async uses `Task`.
- Building the project copies `Poker.Shared.dll` into `client/Assets/Plugins/Poker.Shared/`. After changing Shared,
  rebuild before checking the client compiles.
- Folders: `Infrastructure/Protocol` (`Packets/`, `Messages/` codec and registry, `Reliability/`, `SystemMessages/`,
  with `ProtocolInfo`, `ProtocolMessages`, `ProtocolErrorCodes`, `CloseCodes` at its root), `Rules/` (`Cards/`,
  `Hands/`, `Pots/`), `Features/<Slice>` (messages and validation both sides use).
- Every slice has the same layout: `<Slice>MessageNames`, `<Slice>Messages` and `<Slice>ErrorCodes` at the slice root;
  `Requests/` for requests and their responses, `Events/` for events, `Models/` for the DTOs and enums messages carry.
  A cohesive sub-protocol may get a topic folder instead (`Hand/Turns`: the act request, its response, the turn events
  and their enums). Namespaces follow: `Poker.Shared.Features.Hand.Events`.

## Messages

- A message is a `sealed class` with public `{ get; set; }` auto-properties; strings default to `""`, lists to empty.
  No behaviour on messages. Wire names are camelCase via the serializer.
- Name by role: `XRequest`, `XResponse` (or `XAcceptedResponse`), `XEvent`.
- The wire name is a constant in `<Slice>MessageNames`, dotted lower snake case: `"turn.act"`, `"hand.hole_cards"`.
- Register it in `<Slice>Messages.Register(MessageRegistry)`; every slice's `Register` is called from
  `MessageCatalog.CreateRegistry` (`shared/Poker.Shared/MessageCatalog.cs`). The registry rejects FNV-1a collisions.
- Error codes for a slice live in `<Slice>ErrorCodes`.
- Use `/add-message` to add one end to end.