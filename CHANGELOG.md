# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/).

## [1.1.0] - 2026-11-19

### Added
- Layer key (default `CapsLock`): hold it for digits in any field, tap it to toggle the
  numpad lock, `Shift`+`CapsLock` for the real Caps Lock. Selectable in Settings as
  CapsLock, Right Ctrl or off.

### Fixed
- Saving Settings no longer resets the "welcome shown" flag, which made the first-run
  window come back on the next start.

## [1.0.0] - 2026-11-19

First public release.

### Added
- Automatic remapping of `q w e` / `a s d` / `z x c` / `space` / `v` to numpad keys while a
  numeric field has focus, detected through UI Automation, Win32 `ES_NUMBER` and field-name
  keywords (Turkish and English).
- `Ctrl`+`Alt`+`N` override that cycles Auto → Forced on → Forced off and teaches the
  corrected field; label digits are ignored so the rule survives changing values.
- `Shift` / `Ctrl` / `Alt` / `Win` bypass, Num Lock-off fallback, password and read-only
  fields never remapped.
- Tray icon with state colours; left or right click opens the menu. On first start the
  icon is moved out of the hidden overflow flyout and a welcome window explains where it is.
- Settings (hotkey, key map, extra keywords, start with Windows) and a Diagnostics window
  with a copy button for bug reports.
- Per-user installer (no admin), portable exe, SHA-256 checksums and build provenance
  attestation on every release.

[1.1.0]: https://github.com/Muratmd/QweNumpad/releases/tag/v1.1.0
[1.0.0]: https://github.com/Muratmd/QweNumpad/releases/tag/v1.0.0
