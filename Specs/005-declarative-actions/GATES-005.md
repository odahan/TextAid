# Gates — LOT-005

> Before working on this lot, read the repository root README.md.

All gates below are active and unevaluated. Identify each evaluated result precisely. Preserve prior evaluations in each gate's `Test history` before reevaluation.

## G-005-001 — Action and template tests

Type: AUTO
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
All eight built-in actions validate, and template tests cover variables, missing parameters, Unicode, literal braces, instruction-bearing input, and Translate with an explicit destination language and unchanged original input.

Method:
Run dotnet test -m:1 for Core and AI tests.

Tested at: —
Evaluated result: —
Result: —
Evidence: —

### Test history

No evaluations yet.

## G-005-002 — Action extensibility

Type: HUMAN
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
Adding a JSON action and reloading it exposes it for use without recompiling TextAid.

Method:
Add a disposable sample action and exercise it in the running application.

Validated at: —
Validated by: —
Evaluated result: —
Comment: —

### Test history

No evaluations yet.
