# Feature: <name>

## Goal

What the player can do that they cannot do today, in two or three sentences.

## Player experience

The flow from the player's side, step by step, including errors and edge cases (leaving the table, reconnecting,
watching without a seat).

## Rules

The game or business rules: who may do what, limits, timings, amounts.

## Server

What the server decides and stores, which events it sends and to whom (everyone, one player, everyone but one).

## Client

Screens, popups, buttons, animations; what waits for an animation.

## Messages (draft)

| Wire name | Kind | Payload |
|---|---|---|
| `area.verb` | request / response / event | `{ ... }` |

## Out of scope

What this feature deliberately does not do.

## Open questions

Anything the author is unsure about.