---
name: run-local
description: Run mp-poker locally to see a change working - start the .NET server, then open the Unity client on it or run the PlayMode end-to-end tests headless - and stop the server afterwards. Use when asked to run, start, launch or try the game, server or client.
argument-hint: "[server | unity | playmode] (default: server + unity)"
---

# Run the game locally

## Steps

1. **Server.** Check whether one already runs (`Get-Process -Name Poker.Server`). If not:
   ```powershell
   ./tools/server.ps1 -Background      # builds, starts, waits for ws://localhost:5080/ws, logs to the temp folder
   ```
   Exit code 3: the port is taken or a server already runs; report which. Exit code 1: show the log tail it printed.
2. **Client**, by argument:
   - `unity` (default): `./tools/open-unity.ps1` opens `client/` in the editor that matches `ProjectVersion.txt`
     (exit code 3 means it is already open). Tell the user to open `Assets/Scenes/Main.unity` and press Play; the app
     connects to `ws://localhost:5080/ws`. The first open on a fresh clone imports for several minutes and needs
     `Poker.Shared.dll` (`./tools/setup.ps1`).
   - `playmode`: with the Editor closed, run `/unity-tests PlayMode` (it reuses the running server).
   - `server`: stop after step 1 and give the URL.
3. **Simulated network.** For latency, set `POKER_SIMULATED_RTT_MS` before launching Unity, or the root scope's
   `_simulatedRoundTripMs`; the in-game Dev panel has short (3 s) and long (40 s) drops and a bug report button.
4. **Stop.** When the user is done (or after a PlayMode run you started the server for), stop the server with
   `./tools/server.ps1 -Stop`. Never stop a server or Unity Editor you did not start without asking.

## Notes

- Bots fill the practice table, so one client is enough to play a hand.
- A phone reaches the local server through `adb reverse tcp:5080 tcp:5080` after an Android development build
  (`mp-poker > Build Android Development APK`).
- Cloud sessions have no Unity: run the server and the .NET tests only.