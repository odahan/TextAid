# Gates — LOT-009

> Before working on this lot, read the repository root README.md.

All gates below are active and unevaluated. Identify each evaluated result precisely. Preserve prior evaluations in each gate's `Test history` before reevaluation.

## G-009-001 — Provider and policy tests

Type: AUTO
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
Provider factory, environment authentication, missing credentials, and deployment-mode classification tests pass without needing live remote credentials.

Method:
Run dotnet test -m:1 with a fake IChatClient or local test endpoint.

Tested at: —
Evaluated result: —
Result: —
Evidence: —

### Test history

No evaluations yet.

## G-009-002 — Remote choice walkthrough

Type: HUMAN
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
A deliberately configured External profile works only in External mode, and the selected deployment mode is clear to the user.

Method:
Exercise with an authorized test account and inspect configuration for absence of clear-text secrets.

Validated at: —
Validated by: —
Evaluated result: —
Comment: —

### Test history

No evaluations yet.
