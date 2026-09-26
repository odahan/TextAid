# Gates — LOT-004

> Before working on this lot, read the repository root README.md.

All gates below are active and unevaluated. Identify each evaluated result precisely. Preserve prior evaluations in each gate's `Test history` before reevaluation.

## G-004-001 — Build and fake-client tests

Type: AUTO
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
Solution builds; AI unit tests pass without Ollama running.

Method:
Run dotnet build -m:1 and dotnet test -m:1 with live integration tests excluded.

Tested at: —
Evaluated result: —
Result: —
Evidence: —

### Test history

No evaluations yet.

## G-004-002 — Local transformation walkthrough

Type: HUMAN
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
The local Ollama Rewrite result appears and can be safely replaced into the source or copied without replacing it; stopped Ollama produces a usable error without crashing.

Method:
Run on Windows with a configured local model, then stop Ollama.

Validated at: —
Validated by: —
Evaluated result: —
Comment: —

### Test history

No evaluations yet.
