# Multiplayer models: which one fits poker

Purpose: survey the main client-server and peer models used in games, show where each fits, and justify the model
mp-poker uses. Back to [README](../README.md).

## The models

| Model | How it works | Typical games | Cheating resistance | Bandwidth | Latency feel |
|---|---|---|---|---|---|
| **Server-authoritative, turn-based, snapshot + event stream** | Client sends intents; server validates, applies, broadcasts events; joiners get a snapshot | Card games, board games, strategy, casual social | High: client never owns state, hidden data never sent | Tiny | Round trip per action, hidden by animation |
| Deterministic lockstep | Every peer runs the same simulation; only inputs are exchanged per tick | RTS, fighting games (with rollback) | Medium: full state on every machine (maphacks) | Tiny | Input delay of one round trip |
| Rollback netcode | Lockstep that predicts remote inputs, then rewinds and resimulates on mismatch | Fighting games | Medium | Tiny | Feels local |
| Client prediction + server reconciliation | Client simulates its own input at once; server is authoritative; client corrects on mismatch | Shooters, action games | High for state, aim assists still possible | Medium | Own actions instant |
| Snapshot interpolation | Server sends world snapshots at a fixed rate; clients render slightly in the past and interpolate | Shooters (remote entities), racing | High | High | Remote entities about 100 ms behind |
| Lag compensation | Server rewinds world to the shooter's view time to resolve hits | Shooters | High | n/a | Hits match what the shooter saw |
| Relay / P2P with host | One player hosts or a relay forwards messages | Co-op, small sessions | Low (host can cheat) | Low | Depends on host |

## Why poker uses the first model

- **Hidden information.** A peer model or any model that sends full state leaks hole cards. Only a server that sends
  each player their own view is safe.
- **Turn-based.** Nothing needs to be predicted: the next state is the server's decision, and 100 to 200 ms of round
  trip is hidden by animations.
- **Money-like state.** Chips must never be applied twice or lost; that demands one authority, idempotent intents and
  reliable ordered delivery.
- **Joining mid-hand.** A snapshot of the visible state plus the ordered stream from that point is enough to render
  everything, including the running turn timer.

## What poker still borrows from real-time models

| Real-time idea | Poker version |
|---|---|
| Clock synchronization | Server deadlines; the client converts them with a measured clock offset |
| Input delay / leeway | The visible turn timer is shorter than the server deadline |
| Interpolation (render in the past) | The presentation queue plays events behind the network by the length of animations |
| Reconciliation | On resync the client drops queued animations and jumps to the authoritative state |
| Prediction | Only cosmetic: highlight the pressed button, never move chips before the server event |

## Optional contrast exercise

Add one tiny real-time feature to feel the difference, for example a shared "throw a chip at a
player" emote that flies across the table:
1. Naive: send the throw, animate when the server echoes it (feels laggy for the thrower).
2. Predicted: animate locally at once, ignore the echo for your own throws, correct if the server rejects it.
Measure both with the link conditioner from [testing](../testing.md) at 150 ms latency.

## Summary

mp-poker is server-authoritative, turn-based, with snapshot plus ordered event stream, intents with turn identity, and
client-side presentation buffering. Reliability and resume come from our own layer above the WebSocket
([reliability](../reliability.md)).