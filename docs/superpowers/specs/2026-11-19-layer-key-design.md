# Layer key (hold for digits, tap to lock) — design

Status: approved 2026-11-19. Target release: 1.1.0.

## Goal

Automatic detection cannot be right everywhere. Give the user a manual way that is faster
than the override hotkey: hold one key and the left-hand block types digits anywhere,
release it and letters are back.

## Behaviour

| Input | Result |
|---|---|
| Hold layer key + `q w e` … | numpad digits, in any field, even when mode is *Forced off* |
| Release layer key | back to whatever the current mode says |
| Tap layer key (down→up within 300 ms, no other key pressed meanwhile) | toggles numpad lock: *Auto* → *Forced on*, anything else → *Auto* |
| `Shift` + layer key | passes through: real Caps Lock |
| `Shift` + letter while layer held | letter (existing bypass rule) |

The lock does **not** teach the learned-field store; it is meant for typing a list, unlike
`Ctrl`+`Alt`+`N`, which teaches.

The layer key itself is always swallowed (except with `Shift`), so the system Caps Lock
state never changes by accident. Auto-repeat key-downs while held are swallowed too.

Layer key is configurable: **CapsLock** (default), **Right Ctrl**, or **off**. Right Alt is
deliberately not offered — it is AltGr on Turkish Q layouts.

## Components

- `QweNumpad.Core.LayerKeyTracker` — pure state machine. `OnLayerKey(isDown, modifiers, nowMs)`
  returns `LayerKeyResult { Swallow, Tapped }`; `OnOtherKeyDown()` marks the press as a hold;
  `IsHeld`. Time is passed in, so tests need no clock.
- `ModeState.SetLayerHeld(bool)`; `IsActive = LayerHeld || <existing rule>`. `Changed` fires
  on flips as before.
- `AppSettings.LayerKey` (VK code; 0 = off; default `0x14` CapsLock).
- `KeystrokeRouter` routes layer-key events to the tracker, updates `ModeState`, raises
  `LockToggled` on tap. Existing down/up pairing covers releasing the layer key before the
  letter.
- Settings form: "Katman tuşu" combo. Welcome window, README, tray menu text mention it.

## Testing

- Unit: tracker hold / tap / slow press / other key during hold / auto-repeat / Shift
  passthrough; ModeState layer overrides Forced off and raises Changed.
- E2E (`tools/smoke-test.ps1`): in the *text* test field, CapsLock held + `qwe` → `789`;
  tap, `qwe` → `789`; tap again, `qwe` → `qwe`; system Caps Lock state unchanged.
