# Protocol

Purpose: the bytes on the wire, layer by layer, and the full message catalog. Back to [README](README.md).

## Layers

| Layer | Owner | Job |
|---|---|---|
| L1 Transport | WebSocket (plain `ws://` locally; TLS belongs in front of it in a deployment) | Deliver binary frames over one TCP connection |
| L2 Packet | `PacketHeader` + payload (`PacketCodec`) | Sequence, ack, message type, request id, channel, flags |
| L3 System | `sys.*` messages, `ReliableChannel`, sessions | Hello/welcome, heartbeat, acks, resume |
| L4 Channels | Header field | Reserved for logical streams; everything travels on channel `0` today |
| L5 Messages | `MessageRegistry` + `IPayloadSerializer` | Typed requests, responses and events |
| L6 Facade | Client `IServerConnection`, server `MessageHandler<T>` | Send, request/response with cancellation, subscribe by type |

One WebSocket binary frame carries exactly one packet. A text frame is a protocol violation: the server closes with
status 1003 (`InvalidMessageType`, "Binary packets only.") without waiting for the peer.

## L2 packet header (14 bytes, little endian)

| Field | Type | Meaning |
|---|---|---|
| `seq` | u16 | Sender's packet number; `0` for unnumbered `sys.*` packets; wraps around |
| `ack` | u16 | Last `seq` received in order from the peer (piggy-backed ack) |
| `type` | u32 | Message type id: FNV-1a 32-bit hash of the message name (`MessageTypeId`) |
| `requestId` | u16 | Non-zero for a request and its response; `0` for events |
| `channel` | u16 | Logical stream id, `0` today |
| `flags` | u16 | `PacketFlags`: bit 0 binary payload (unused, JSON only), bit 1 response expected, bit 2 error response |

The payload follows the header: UTF-8 JSON with camelCase names. The message name is not repeated in the payload. The
codec (`PacketHeader`, `PacketCodec`, `MessageRegistry`, `MessageCodec`) lives in `Poker.Shared`; each side plugs in
its own `IPayloadSerializer`: `JsonPayloadSerializer` (System.Text.Json) on the server, `NewtonsoftPayloadSerializer`
on Unity (IL2CPP-safe through `link.xml`).

Why a custom header over WebSocket: TCP orders bytes within one connection, but mobile connections get replaced. The
header's seq/ack let both sides resend what the peer never received and continue on a new socket
([reliability](reliability.md)).

## L3 system messages

`ProtocolInfo.Version` is 2. Packets whose name starts with `sys.` travel unnumbered (`seq = 0`) but always carry
`ack`. Every other packet is numbered once `sys.welcome` arrived; the server drops an unnumbered game message after the
handshake.

| Message | Direction | Payload | Notes |
|---|---|---|---|
| `sys.hello` | C -> S request | `{ protocolVersion, playerName, resumeKey, deviceKey }` | First request; until it succeeds every other request is rejected with `session.required`. The header `ack` is the client's last received seq |
| `sys.welcome` | S -> C response | `{ sessionId, playerId, playerName, serverTimeUnixMs, resumeKey, resumed, heartbeatMs, zombieMs, maxUnacked, seatedTableId }` | The header `ack` is the server's last received seq; the resend follows it |
| `sys.ping` / `sys.pong` | C -> S request / response | `{ clientTimeUnixMs }` / `{ clientTimeUnixMs, serverTimeUnixMs }` | Heartbeat; also feeds the clock offset |
| `sys.ack` | both | none | Sent when `maxUnacked` packets arrived and nothing else is going out |
| `error` | response | `{ code, message }` | Any rejected request, registered by the protocol, not by a slice |

Hello rules:
- `resumeKey` empty means a new session. An unknown or expired key is not a close: the server answers
  `resumed: false` with a new session and the client resynchronises.
- `deviceKey` (optional, at most `ProtocolInfo.MaxDeviceKeyLength` = 64 characters) is a random id the app saves on
  first launch. The same key is the same player while the server runs, so an app restart keeps the player id and the
  wallet. No key means a new player.
- A new session for a player who still has one closes the old socket with `4007 DuplicateLogin` ("Signed in on another
  device.") and takes over the player; the seat stays. The client does not reconnect on its own after `4007`; the player
  can choose "Play here".
- `seatedTableId` names the table where the player holds a seat (empty if none); the client opens it right after the
  lobby loads.
- Rejections: `protocol.unsupported_version`, `session.invalid_name` (name longer than
  `ProtocolInfo.MaxPlayerNameLength` = 24), `session.invalid_device_key`.

Close codes (`CloseCodes`): `4006 SequenceGap`, `4007 DuplicateLogin`. A full resend buffer aborts the socket and ends
the session; `4005 ResendOverflow` is defined but not sent, because the socket is aborted rather than closed.

## L5 message catalog

Naming: `<area>.<verb or fact>`. Requests end in an imperative verb, events in a past participle. Every message class
lives in `shared/Poker.Shared/Features/<Slice>/` under `Requests/` or `Events/` (DTOs and enums in `Models/`; `Hand`
keeps its turn messages together in `Turns/`), its wire name in `<Slice>MessageNames`, and its registration in
`<Slice>Messages.Register`, collected by `MessageCatalog.CreateRegistry`.

### Lobby and wallet

| Message | Kind | Payload |
|---|---|---|
| `lobby.list_tables` | request | `{}` -> `lobby.table_list { tables[{ tableId, name, smallBlind, bigBlind, seatCount, occupiedSeats }] }` |
| `wallet.get_balance` | request | `{}` -> `wallet.balance { balance }` (a new player starts with 5,000) |
| `wallet.add_chips` | request | `{}` -> `wallet.balance { balance }`, or `error` `wallet.not_eligible`; free chips top the balance up to 5,000 only when it is under 1,000 |
| `wallet.balance_changed` | event (to its owner) | `{ balance, delta, reason }` (`buyIn`, `cashOut`, `freeChips`); sent the moment the wallet changes, so it can arrive before the `hand.ended` that caused it |

### Table and seating

| Message | Kind | Payload |
|---|---|---|
| `table.join` | request | `{ tableId }` -> `table.snapshot` under the same request id, or `error` `table.not_found` |
| `table.snapshot` | response or event (to one player) | `{ tableId, name, smallBlind, bigBlind, minBuyIn, maxBuyIn, seats[{ index, status, playerId, playerName, stack, reservedUntilUnixMs, isSittingOut }] }` |
| `table.reserve_seat` | request | `{ tableId, seatIndex }` -> `table.seat_reservation { seatIndex, expiresAtUnixMs, minBuyIn, maxBuyIn }` |
| `table.seat_reserved` | event | `{ tableId, seat }` |
| `table.buy_in` | request | `{ tableId, amount }` -> `table.buy_in_accepted { seatIndex, stack }` |
| `table.player_seated` | event | `{ tableId, seat }` |
| `table.stand_up` | request | `{ tableId }` -> `table.stand_up_accepted { seatIndex, leavesAfterHand }`; in a running hand the seat is kept and released (and cashed out) when the hand ends |
| `table.seat_released` | event | `{ tableId, seatIndex, reason }` (`stoodUp`, `reservationExpired`, `disconnected`) |

### Hand

| Message | Kind | Payload |
|---|---|---|
| `hand.started` | event | `{ tableId, handNumber, buttonSeat, smallBlindSeat, bigBlindSeat, smallBlind, bigBlind, players[{ seatIndex, playerId, stack, committed }] }` (blinds already posted) |
| `hand.hole_cards` | event (to its owner only) | `{ tableId, handNumber, seatIndex, cards[2] }` (codes such as `"Ah"`) |
| `hand.snapshot` | event (to a viewer joining mid-hand) | `{ tableId, handNumber, street, buttonSeat, board[], pots[], players[], currentTurn?, myCards[] }`, sent right after `table.snapshot` |
| `turn.started` | event | `{ tableId, handNumber, turnId, seatIndex, deadlineUnixMs, leewayMs, toCall, canCheck, canRaise, minRaiseTo, maxRaiseTo }`; the client closes input at `deadlineUnixMs - leewayMs` |
| `turn.act` | request | `{ tableId, turnId, action (fold, check, call, raise), amount (raise-to) }` -> `turn.act_accepted { turnId }` or `error` |
| `turn.acted` | event | `{ turnId, seatIndex, action, amount (chips put in), committed, stack, potTotal, isAllIn, reason }` (`reason`: player, timeout, leftTable) |
| `round.ended` | event | `{ street, pots[{ amount, eligibleSeats[] }] }` |
| `round.started` | event | `{ street, board[] }` |
| `hand.showdown` | event | `{ board[], reveals[{ seatIndex, cards[2], category, description }] }` |
| `hand.ended` | event | `{ handNumber, awards[{ potIndex, seatIndex, amount }], stacks[{ seatIndex, stack }], nextHandInMs }` |

### Turn timer and hand history

| Message | Kind | Payload |
|---|---|---|
| `player.sat_out` | event | `{ tableId, seatIndex, playerId }`, right after the `turn.acted` timeout that caused it |
| `player.return` | request | `{ tableId }` -> `player.return_accepted { seatIndex }` or `error` |
| `player.returned` | event | `{ tableId, seatIndex, playerId }` |
| `history.get` | request | `{}` -> `history.hands { hands[] }`: the player's last 20 finished hands, newest first, each from that player's point of view (own hole cards, other cards only when shown) |

### Error codes

Each slice declares its codes in `<Slice>ErrorCodes`:

| Area | Codes |
|---|---|
| Protocol | `session.required`, `protocol.unsupported_version`, `session.invalid_name`, `session.invalid_device_key` |
| Table | `table.not_found`, `table.not_joined` |
| Seating | `seat.invalid`, `seat.taken`, `seat.already_seated`, `seat.not_reserved`, `seat.reservation_expired`, `seat.not_seated`, `buyin.out_of_range`, `buyin.insufficient_funds` |
| Hand | `hand.none`, `turn.not_your_turn`, `turn.stale` (wrong `turnId`), `turn.invalid_action`, `turn.invalid_amount` |
| Turn timer | `player.not_sitting_out` |
| Wallet | `wallet.not_eligible` |

## Intents and facts

- Every intent is a request (`requestId != 0`), resolved on the client by the pending request map with a timeout and
  cancellation. The response only says "accepted" or carries an error.
- The authoritative effect arrives as an event that everyone at the table receives (for example `turn.acted`). The
  client does not change game state on the response; it waits for the event.

## Versioning

- `protocolVersion` in `sys.hello`; the server rejects other versions with `protocol.unsupported_version`.
- New fields are optional; unknown fields are ignored by both sides.
- New message types are additive; an unknown type id decodes as `DecodeStatus.UnknownType`, is logged and dropped.

## Message registry

`Poker.Shared` declares every message type once with its name. `MessageRegistry` maps type id, CLR type and name;
building the catalog asserts there are no hash collisions (covered by a test). The `/add-message` skill walks through
adding one across all layers ([development](development.md)).

## Extension ideas

- Per-table channels (the header already carries `channel`) once a client can watch several tables.
- MessagePack payloads behind the `BinaryPayload` flag; source-generated serializers.
- Server-initiated shutdown notice and close codes for auth and resume failures.