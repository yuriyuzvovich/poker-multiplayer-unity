---
name: test
description: Build and run the mp-poker .NET test suite (Poker.Shared, Poker.Server, Poker.Tests) through tools/test.ps1, handling the running-server DLL lock, and summarize failures. Use when asked to run tests, check the build, or verify a server/shared change.
argument-hint: "[dotnet test --filter expression, e.g. FullyQualifiedName~HandEngine]"
---

# Run the .NET tests

`tools/test.ps1` finds a dotnet that satisfies `global.json` (see `tools/Common.ps1`), so never hardcode an SDK path.

## Steps

1. **Run** from the repo root (PowerShell tool, or `pwsh`/`powershell -File` from Bash):
   ```powershell
   ./tools/test.ps1                      # whole suite
   ./tools/test.ps1 -Filter "<argument>" # with an argument
   ```
   The build also copies `Poker.Shared.dll` into `client/Assets/Plugins/Poker.Shared/`, which Unity picks up on its next
   refresh. Use `-Verbosity minimal` when you need the build output.
2. **Exit code 3: a server is running.** It locks `Poker.Server.dll` and `Poker.Shared.dll`. Tell the user and ask before
   stopping it with `./tools/server.ps1 -Stop`; it may be serving a Unity Play session. Then run again.
3. **Report.**
   - Green: one line with the passed/total count per project.
   - Build errors: list each as `path:line` with the compiler message. Warnings are errors here
     (`TreatWarningsAsErrors`), so a warning is a real failure.
   - Test failures: for each failing test give the name, the assertion message and the `path:line` from the stack frame
     inside `tests/` or the code under test. Do not paste whole stack traces.
4. Integration tests run one class at a time in their own collection and are expected to be stable. A failing or timing
   out integration test is a real finding: report it with its message; do not rerun until green. If a new integration
   test class times out, check that it carries `[Collection(IntegrationCollection.Name)]`.
5. Do not "fix" a failing test by weakening its assertion. Find out whether the code or the test is wrong and say which.