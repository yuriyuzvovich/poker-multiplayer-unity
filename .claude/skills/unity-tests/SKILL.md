---
name: unity-tests
description: Run the mp-poker Unity Test Runner suites (EditMode or PlayMode) headless through tools/unity-tests.ps1 and summarize the NUnit results. Use when asked to run Unity, client, EditMode or PlayMode tests.
argument-hint: "<EditMode|PlayMode> [test filter, e.g. PresentationQueue]"
---

# Run Unity tests in batch mode

`tools/unity-tests.ps1` finds the editor for `client/ProjectSettings/ProjectVersion.txt` (`UNITY_EDITOR`, then Unity
Hub's install folders; see `tools/Common.ps1`), runs the Test Runner in batch mode and prints a summary. Never hardcode
an editor path.

## Steps

1. **Fresh Shared DLL.** If `shared/` changed since the last build, run `/test` first so
   `client/Assets/Plugins/Poker.Shared/Poker.Shared.dll` is current.
2. **Run** from the repo root in the foreground with a 10 minute timeout (the first import takes a few minutes):
   ```powershell
   ./tools/unity-tests.ps1 -Platform EditMode [-Filter "<filter>"]
   ./tools/unity-tests.ps1 -Platform PlayMode -StartServer [-Filter "<filter>"]
   ```
   PlayMode (`WalkingSkeletonTests`) needs the server on `ws://localhost:5080/ws`. `-StartServer` starts one for the run
   and stops it afterwards; if a server is already running, the script uses it.
3. **Exit codes.** 0 all passed, 2 test failures, 1 the run itself failed (compile errors or a crash; the script prints
   the `error CS` and exception lines from the editor log), 3 refused:
   - An Editor has `client/` open: batch mode cannot share the project. Tell the user to close it or to run the tests in
     its Test Runner window (Window > General > Test Runner). Never kill the Editor yourself.
   - `Poker.Shared.dll` missing: run `./tools/setup.ps1` or `/test` first.
   - Unity not found: the message lists where it looked; the user can set `UNITY_EDITOR`.
4. **Report.** The script prints `passed, failed, skipped of total`, then each failed test with its message and first
   stack line under `client/Assets`, and the path of the results XML. Relay that; read the XML (NUnit 3,
   `test-case[@result='Failed']`) only when you need more detail.