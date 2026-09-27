# Gates — LOT-006

> Before working on this lot, read the repository root README.md.

All gates below are active and unevaluated. Identify each evaluated result precisely. Preserve prior evaluations in each gate's `Test history` before reevaluation.

## G-006-001 — Configuration tests

Type: AUTO
Status: PASS
Defined at: 2026-09-25T23:53:23

Condition:
Connection/profile resolution, the automatic External → On-premises → This device only and On-premises → This device only downgrade paths with status messages, the error-then-user-offered downgrade paths for invalid configured connections and non-responsive providers, and CurrentUser-DPAPI secret-vault persistence without clear-text configuration secrets are covered by passing deterministic tests, together with distinct persisted translation language preferences, default and reassigned shortcut persistence, shortcut conflicts, and listed invalid configurations.

Method:
Run dotnet test -m:1 for Core and AI tests.

Tested at: 2026-09-27
Evaluated result: PASS on the current LOT-006 candidate.
Result: PASS
Evidence: `dotnet build TextAid.sln -m:1 --no-restore -p:NuGetAudit=false` completed with 0 warnings and 0 errors; `dotnet test TextAid.sln -m:1 --no-build -p:NuGetAudit=false` passed 6 AI, 15 Core, and 14 Windows tests.

### Test history

- 2026-09-27 — PASS: `dotnet test TextAid.sln -m:1 --no-build -p:NuGetAudit=false` passed 6 AI, 15 Core, and 14 Windows tests after the configuration, secret-vault, downgrade, shortcut, language-preference, and preset changes.

## G-006-002 — Profile selection

Type: HUMAN
Status: PASS
Defined at: 2026-09-25T23:53:23

Condition:
The user can discover and select an Ollama model, and actions resolve through the selected profile/connection.

Method:
Publish an identifiable Release candidate with root `publish.ps1`, then exercise model discovery and one transformation in that running candidate. Candidate publication is required to make this human gate testable; it does not mark the lot closed or satisfy the gate by itself.

Validated at: 2026-09-27
Validated by: Olivier
Evaluated result: PASS on the published candidate SHA-256 `965D3EECCC4B1A77508AE5849A325F66CFC7141EAB82A2E7D7C4CE6802F6CABA`.
Comment: Olivier confirmed that the local Ollama discovery and transformation path, the connection settings, and action presets work. The current action-language behavior is explicitly deferred to the planned language workflow.

### Test history

- 2026-09-27 — Release candidate published for the required human walkthrough: `src/TextAid.App/bin/Publish/TextAid.exe`, 178,433,455 bytes, SHA-256 `C7AFF2761FA016E6FBAD07ECB72FFF95269660093C057771B4124DB5DC074612`. Publication alone does not evaluate this HUMAN gate; status remains TO TEST pending Olivier's walkthrough.
- 2026-09-27 — PASS: Olivier confirmed the local Ollama discovery and transformation path works on the published candidate.
- 2026-09-27 — PASS: Olivier confirmed all implemented LOT-006 behavior works on the current published candidate.
