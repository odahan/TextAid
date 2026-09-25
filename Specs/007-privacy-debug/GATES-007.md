# Gates — LOT-007

> Before working on this lot, read the repository root README.md.

All gates below are active and unevaluated. Identify each evaluated result precisely. Preserve prior evaluations in each gate's `Test history` before reevaluation.

## G-007-001 — Privacy and policy tests

Type: AUTO
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
Tests prove endpoint classification, Strict Local rejection, Debug-off file absence, and redaction when Debug is on.

Method:
Run dotnet test -m:1 with representative secret and text sentinels.

Tested at: —
Evaluated result: —
Result: —
Evidence: —

### Test history

No evaluations yet.

## G-007-002 — Privacy behavior

Type: HUMAN
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
The user sees an understandable local/remote connection indication and errors; Debug controls and file behavior match the documented privacy rules.

Method:
Inspect the running app and log from an opt-in session.

Validated at: —
Validated by: —
Evaluated result: —
Comment: —

### Test history

No evaluations yet.
