---
paths:
  - "tests/**"
  - "client/Assets/Tests/**"
---

# Test rules

## .NET (`tests/Poker.Tests`, xUnit)

- Folder and namespace mirror the code under test: `Features/Hand` for `server/.../Features/Hand`,
  `Infrastructure/Session` for `server/.../Infrastructure/Session`, `Shared/Infrastructure/Protocol` for
  `shared/.../Infrastructure/Protocol`. Cross-cutting suites have their own folders: `Integration/`, `Architecture/`.
- Test names read as a sentence in underscore-separated parts: `Subject_Scenario_Outcome`, for example
  `Act_OnTheHumansTurn_IsAccepted_AndAStaleTurnIsRejected`, `SittingOutSeat_IsNotDealtIn`.
- Arrange, act and assert separated by blank lines. One behaviour per test; `[Theory]` + `[InlineData]` for tables.
- Deterministic only: `ManualClock`, `ScriptedDeck`, `SeededDeckFactory`, `RecordingConnection`, `RecordingChipBank` from
  `TestDoubles/`. No `Task.Delay`, no wall clock, no real randomness.
- Integration tests run a real server on a random port (`TestServer`, `TestClient`); keep them few, and wait on
  messages with a timeout rather than fixed sleeps. Every integration test class carries
  `[Collection(IntegrationCollection.Name)]`, so real servers never run in parallel.
- The one allowed fixed wait proves a negative (something must not happen and there is nothing to wait for), with a
  comment saying so; see `SignInIntegrationTests`.
- `Architecture/` enforces the dependency rules and source hygiene by reading the source tree. When a rule changes on
  purpose, change the test and `AGENTS.md` together; never add an allowlist entry just to make a build pass.

## Unity (`client/Assets/Tests`)

- EditMode tests live in one folder per infrastructure area or slice (`Core`, `Network`, `UI`, `Diagnostics`, or the
  slice name), namespace `Poker.Client.Tests.EditMode.<Area>`. They cover domain, presenters, commands, presentation
  queue and network logic with fakes (`FakeServerConnection`, `FakeServerClock`, `Fixtures/`); every client slice has
  its own. PlayMode (`WalkingSkeletonTests`) needs the server running.
- Run them with `/unity-tests`; run .NET tests with `/test`.

## Definition of done

- The exit test is written first or alongside, shown red against the defect, then green.