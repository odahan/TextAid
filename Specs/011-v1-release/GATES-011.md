# Gates — LOT-011

> Before working on this lot, read the repository root README.md.

All gates below are active and unevaluated. Identify each evaluated result precisely. Preserve prior evaluations in each gate's `Test history` before reevaluation.

## G-011-001 — Release build and tests

Type: AUTO
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
Release candidate builds, non-integration tests pass, and a single self-contained TextAid.exe is produced.

Method:
Run dotnet build -m:1, dotnet test -m:1, and the Release publish; identify the candidate result.

Tested at: —
Evaluated result: —
Result: —
Evidence: —

### Test history

No evaluations yet.

## G-011-002 — V1 acceptance

Type: HUMAN
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
The named human accepts the candidate against each formal V1 release criterion, including the documented compatibility limits.

Method:
Review release evidence and exercise the candidate; record named, dated validation.

Validated at: —
Validated by: —
Evaluated result: —
Comment: —

### Test history

No evaluations yet.
