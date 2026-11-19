# Contributing

Thanks for helping. Issues and pull requests are welcome — in English or Turkish.

## The quickest useful contribution

**Report a field that is detected wrong.** Open the tray menu → **Tanılama…**, click into the field,
press **Panoya kopyala**, and paste it into a
[Wrong detection](https://github.com/Muratmd/QweNumpad/issues/new?template=detection.yml) issue.
Each one of those can turn into a rule that fixes the field for everybody.

## Good first pull requests

- **English UI strings.** All user-facing text is Turkish and hardcoded today. Moving it to a
  resource file with Turkish and English is the most valuable change open right now.
- **Keywords in more languages.** `FieldHeuristics.LongKeywords` covers Turkish and English. Add
  your language, with a positive *and* a negative test case in `FieldHeuristicsTests`.
- **A smaller binary.** The single-file exe is ~63 MB because `System.Windows.Automation`
  pulls in WPF. Replacing it with direct `UIAutomationCore` COM interop would cut it a lot.

## Building

```powershell
dotnet test
dotnet build
src\QweNumpad.App\bin\Debug\net8.0-windows\QweNumpad.exe --diagnostics
```

Requires Windows and the .NET 8 SDK or newer.

## Ground rules for code

1. **Logic goes in `QweNumpad.Core`, with a test.** Core deliberately has no Win32 or WinForms
   dependency so everything in it can be unit-tested. `QweNumpad.App` should stay thin.
2. **Write the test first and watch it fail.** Then make it pass.
3. **The keyboard hook does no blocking work.** Its callback holds up keyboard input for the whole
   system until it returns. No UI Automation, no file or registry access, no locks, no allocation
   you can avoid. If you need information about the focused field, the focus watcher already has it.
4. **Key-down and key-up stay paired.** If you change `KeystrokeRouter`, a translated key must
   always be released as the same translated key.
5. **Never touch keystroke content.** No logging of keys, no persistence of typed text, no network.
   The PR template has a checklist; reviews will hold it.

## Before you open the PR

```powershell
dotnet test
powershell -ExecutionPolicy Bypass -File tools\smoke-test.ps1
powershell -ExecutionPolicy Bypass -File tools\browser-test.ps1   # if you touched detection
```

The two scripts inject real keystrokes for ~20 seconds each — don't type while they run.
They need a real desktop session, which is why CI does not run them.

## Releasing (maintainers)

1. Bump `<Version>` in `Directory.Build.props`
2. Commit, then tag: `git tag v1.2.3 && git push origin v1.2.3`
3. The `release` workflow tests, builds and publishes `QweNumpad.exe` to the release.
