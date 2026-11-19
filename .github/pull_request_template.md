## What this changes

<!-- One or two sentences. Link the issue if there is one: "Fixes #12". -->

## How I checked it

- [ ] `dotnet test` passes
- [ ] New logic lives in `QweNumpad.Core` and has a test
- [ ] If this touches the keyboard hook, detection, or key output: I ran `tools/smoke-test.ps1`
      (and `tools/browser-test.ps1` if it affects web fields)

## Security checklist

QweNumpad runs a system-wide keyboard hook, so these are hard rules:

- [ ] No keystroke content is logged, stored, or sent anywhere
- [ ] No networking code added
- [ ] Nothing new is written to disk outside `%APPDATA%\QweNumpad\`
- [ ] The hook callback still does no blocking work (no UI Automation, I/O, or locks)
