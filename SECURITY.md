# Security policy

QweNumpad installs a system-wide low-level keyboard hook — the same Windows API a keylogger
uses. That makes security reports about it especially welcome.

## Reporting a vulnerability

**Please do not open a public issue.** Report privately through GitHub:
[**Report a vulnerability**](https://github.com/Muratmd/QweNumpad/security/advisories/new).

You can expect an acknowledgement within a few days. Fixes for confirmed issues are released as
soon as possible, and you are credited in the release notes unless you prefer otherwise.

## What counts

Anything that breaks one of the project's promises:

- a keystroke's content ending up anywhere — logs, files, memory that outlives the decision, the network
- any network activity at all
- files written outside `%APPDATA%\QweNumpad\`
- a way for another process to influence what QweNumpad injects, or to make it inject keystrokes
- a password field being remapped
- the release binary not matching what this source code builds

## Supported versions

Only the latest release receives fixes.
