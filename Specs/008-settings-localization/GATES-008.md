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

- 2026-09-28 — Automated regression: `TextAid.Core.Tests` passed (59/59), `TextAid.AI.Tests` passed (6/6), and `TextAid.Platform.Windows.Tests` passed (16/16). The English source catalog and `Strings.xaml` key sets were also compared exactly (158/158). The Core suite proves that a stale generation cannot publish output, so it cannot enable either `Copy` or `Replace`; it also covers unavailable catalog-generation providers, invalid generated catalogs that retain the prior validated cache, and a locked unreadable cache that returns English. The remaining evidence for G-008-001 is the existing binding behavior exercised through the Windows walkthrough in G-008-002.

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
