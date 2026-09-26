# Gates — LOT-010

> Before working on this lot, read the repository root README.md.

All gates below are active and unevaluated. Identify each evaluated result precisely. Preserve prior evaluations in each gate's `Test history` before reevaluation.

## G-010-001 — Automated regression

Type: AUTO
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
Build and non-integration test suites pass on the identified candidate result.

Method:
Run dotnet build -m:1 and dotnet test -m:1; record candidate identity and evidence.

Tested at: —
Evaluated result: —
Result: —
Evidence: —

### Test history

No evaluations yet.

## G-010-002 — Windows compatibility matrix

Type: HUMAN
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
The required application, DPI, monitor, failure, text, both invocation paths, translation-direction, destination-change, and shortcut-reassignment cases are exercised and results recorded with named human validation.

Method:
Run the V0.9 matrix on Windows and record each required and exploratory outcome.

Validated at: —
Validated by: —
Evaluated result: —
Comment: —

### Test history

No evaluations yet.

## G-010-003 — Privacy and paste release blockers

Type: HUMAN
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
No wrong-window paste, prohibited log content, or deployment-mode boundary escape remains in the candidate.

Method:
Review failure reproductions and evidence from LOT-003, LOT-007, and current regression.

Validated at: —
Validated by: —
Evaluated result: —
Comment: —

### Test history

No evaluations yet.
