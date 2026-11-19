# QweNumpad — design

Date: 2026-11-19

## Problem

Typing digits with the right hand means either reaching for the number row or moving to a
numpad that half of keyboards do not have. The left hand's home position — `qwe` / `asd` / `zxc` —
is already a 3x3 grid with the same shape as the numpad's digit block.

## Decision: the mapping

```
q w e -> 7 8 9
a s d -> 4 5 6
z x c -> 1 2 3
space -> 0
v     -> decimal separator
```

Spatially one-to-one with the numpad, so no new muscle memory is needed. `space` takes `0`
because the numpad's `0` is also the wide key under the thumb.

## Decision: activation is automatic

Three options were considered:

| Option | Why not chosen |
|--------|----------------|
| Hold a modifier | Safest, but the user has to hold a key for every number — it solves half the problem |
| Toggle on/off | No ambiguity about state, but "is it on right now?" becomes a constant question |
| **Automatic** | **Chosen.** Zero user action in the common case |

Automatic detection cannot be fully reliable on Windows, and that was accepted explicitly up
front rather than discovered later. Electron, Qt, Java and custom-drawn applications expose
fields as a generic `Edit` with no name, and nothing in the accessibility tree distinguishes a
phone number from a surname.

So automatic detection is the primary mechanism, with two deliberate safety nets:

1. **An override hotkey** (`Ctrl`+`Alt`+`N`) cycling `Auto → ForcedOn → ForcedOff → Auto`.
2. **Learning.** Using the override records the field's signature, so the same field is
   classified correctly from then on. Accuracy converges toward 100% for the applications a
   given user actually uses, which is the only accuracy that matters.

## Detection chain

First match wins:

1. A learned rule for this field signature
2. UI Automation `RangeValuePattern` or `ControlType.Spinner` — covers `input type=number`
3. Win32 `ES_NUMBER` window style — covers native digit-only edit controls
4. Keyword match on name / automation id / help text
5. Otherwise: off

Hard exclusions, checked before anything else: **password fields and read-only fields are never
remapped**, and only editable control types (`Edit`, `Document`, `ComboBox`, `Spinner`, `Custom`)
are considered at all, so a keyword in a button label cannot switch the mode on.

A field signature is `process name + control type + automation id + label`. It contains no typed
content, by design — the persisted file must not be able to leak what the user wrote.

Digit runs in the label are replaced by `#` when building the lookup key. Labels frequently
carry the field's current value or a row number ("Tutar: 150 TL", "Satır 3"); keeping those
digits would make a taught rule stop matching the moment the value changed, which silently
defeats the learning safety net. The automation id keeps its digits, because Win32 dialogs
put the control id there ("1001", "1002") and that is what tells two fields apart.

## Escape hatches

`Shift` and `Ctrl` / `Alt` / `Win` suppress the mapping. `Shift` matters most: it means a letter
is always reachable even when the mode is wrongly on, and it keeps `Ctrl`+`A`, `Ctrl`+`C` and
every other shortcut intact.

## Architecture

`QweNumpad.Core` holds all the logic and has no Win32 or WinForms dependency, so it is fully
unit-testable. `QweNumpad.App` holds the parts that can only be tested by running them.

```
Focus changed ──> UiaFocusWatcher (background)
                    ├─> FieldSignature
                    ├─> LearnedFieldStore ─┐
                    └─> FieldHeuristics ───┴─> ModeState.AutoDetected ──> tray icon

Key pressed ──> LowLevelKeyboardHook (reads a volatile bool only)
                    └─> MappingEngine.Decide(key, modifiers, active)
                          ├─ Passthrough → return 0
                          └─ Translate   → SendInput(numpad), return 1
```

### Two constraints that shape the code

**The hook must never call UI Automation.** A `WH_KEYBOARD_LL` callback blocks keyboard input
system-wide until it returns; UIA property reads are COM calls that can take tens of
milliseconds and can throw when the element disappears mid-read. Detection therefore runs on
the focus-change event, and the hook only reads a `volatile bool`.

**Key-down and key-up must stay paired.** `KeystrokeRouter` records which source keys it
translated on the way down, so the matching key-up is also swallowed and replaced — even if the
mode flipped in between. Without this, the original letter's key-up leaks into the application
and keys appear stuck.

**Num Lock is handled, not assumed.** `VK_NUMPAD7` produces `7` only while Num Lock is on; with
it off it means `Home`. When Num Lock is off, the top-row digit key is sent instead. Silently
toggling the user's Num Lock was rejected: it is a visible, global side effect.

## Security posture

A low-level keyboard hook is the same API a keylogger uses, so the project has to be auditable:
no keystroke is ever stored or transmitted, there is no networking code at all, the only files
written are field metadata and settings under `%APPDATA%\QweNumpad\`, and the app runs
`asInvoker` rather than elevated — accepting that it cannot remap keys in elevated windows.

## Testing

- `QweNumpad.Core.Tests` — 74 unit tests over the mapping decision, modifier bypass, mode
  state machine, heuristics (positive and negative name cases), the learned-rule store
  including corrupt-file handling, and learned-beats-heuristic precedence.
- `tools/smoke-test.ps1` — launches the app, injects real keystrokes with `SendInput`, reads
  results back through UI Automation. Covers the full 3x3 grid, `Shift` bypass, and a text
  field being left alone.
- `tools/browser-test.ps1` — the same, against a real page in the default browser. This is the
  test that covers the riskiest claim in the design: that `input type=number` and a text input
  named "Telefon Numarasi" are both detected automatically.

## Known limitations

- Cannot remap inside elevated windows (by design — see Security posture).
- Detection quality varies by toolkit; the override + learning path is the answer, not a fix.
- The single-file executable is ~63 MB because `System.Windows.Automation` pulls in the WPF
  assemblies. Dropping that for direct `UIAutomationCore` COM interop would cut it
  substantially and is a good contribution.
- UI strings are Turkish only.
