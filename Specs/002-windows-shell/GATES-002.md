# Gates — LOT-002

> Before working on this lot, read the repository root README.md.

All gates below are active and unevaluated. Identify each evaluated result precisely. Preserve prior evaluations in each gate's `Test history` before reevaluation.

## G-002-001 — Build and publish

Type: AUTO
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
Solution builds and Release publish yields one distributable TextAid.exe.

Method:
Run dotnet build -m:1 and dotnet publish for win-x64 with the specified properties; inspect publish output.

Tested at: —
Evaluated result: —
Result: —
Evidence: —

### Test history

No evaluations yet.

## G-002-002 — Keyboard state cases

Type: AUTO
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
Deterministic keyboard-trigger tests cover ordinary copy, double copy, key repeat, timeout, intervening key, triple C, and released Ctrl/C.

Method:
Run the relevant dotnet test -m:1 project.

Tested at: —
Evaluated result: —
Result: —
Evidence: —

### Test history

No evaluations yet.

## G-002-003 — Windows shell walkthrough

Type: HUMAN
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
On Windows, the trigger, source-monitor centering, capture, complete dark theme, tray, About, and standalone executable are accepted.

Method:
Exercise the V0.1 executable on a Windows desktop and record the named validator.

Validated at: —
Validated by: —
Evaluated result: —
Comment: —

### Test history

No evaluations yet.
