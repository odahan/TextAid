# Gates — LOT-003

> Before working on this lot, read the repository root README.md.

All gates below are active and unevaluated. Identify each evaluated result precisely. Preserve prior evaluations in each gate's `Test history` before reevaluation.

## G-003-001 — Build and platform tests

Type: AUTO
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
Solution builds and deterministic platform tests pass.

Method:
Run dotnet build -m:1 and dotnet test -m:1.

Tested at: —
Evaluated result: —
Result: —
Evidence: —

### Test history

No evaluations yet.

## G-003-002 — Replacement safety walkthrough

Type: HUMAN
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
Capture → uppercase preview → Accept replaces the intended selection in Notepad, TestTarget, a Chromium browser, and Visual Studio or VS Code; Cancel and failure never paste elsewhere.

Method:
Run the application matrix manually on Windows, including lost source window and modifier timeout.

Validated at: —
Validated by: —
Evaluated result: —
Comment: —

### Test history

No evaluations yet.
