# Gates — LOT-008

> Before working on this lot, read the repository root README.md.

All gates below are active and unevaluated. Identify each evaluated result precisely. Preserve prior evaluations in each gate's `Test history` before reevaluation.

## G-008-001 — Localization tests

Type: AUTO
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
Tests cover complete, missing/extra-key, invalid JSON, placeholder loss, unavailable provider, valid/stale cache, English fallback, all quick-translation routing cases (including third/uncertain language), destination-change result supersession for both Replace and Copy, and shortcut defaults/reassignment/conflicts.

Method:
Run dotnet test -m:1 for Core, AI, and Windows keyboard tests; use fake IChatClient results for deterministic routing and supersession cases.

Tested at: —
Evaluated result: —
Result: —
Evidence: —

### Test history

No evaluations yet.

## G-008-002 — Settings and locale walkthrough

Type: HUMAN
Status: TO TEST
Defined at: 2026-09-25T23:53:23

Condition:
The six Settings sections work in the dark theme, changes apply safely, progress is visible, and an unavailable translation returns to English. The user validates both default shortcuts, direct `Ctrl+C+T` translation without a prior choice, language direction in both configured directions, a different destination restarting translation, `Replace` and `Copy` availability only for the latest completed result, Copy leaving the source unchanged, and shortcut reassignment and persistence.

Method:
Exercise Settings and language switching on Windows.

Validated at: —
Validated by: —
Evaluated result: —
Comment: —

### Test history

No evaluations yet.
