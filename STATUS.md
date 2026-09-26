# Lot status

`STATUS.md` is the authority for current lot states. Each lot's spec, findings, and gates live in its `Specs/` directory.

| Lot reference | Parent | Title | Status | Replaced by | Comment |
|---|---|---|---|---|---|
| LOT-001 | — | [Pré-V0.1 — Visual baseline](Specs/001-visual-baseline/SPEC-001.md) | Closed | — | Candidate 2 accepted by Olivier; both gates PASS; TOTAL convergence recorded in `CONVERGENCE-001.md`. |
| LOT-002 | — | [V0.1 — Windows shell and foundations](Specs/002-windows-shell/SPEC-002.md) | Closed | — | Olivier accepted TOTAL convergence on the V0.1 EXE; all four gates PASS and all findings are terminal. The separate-machine portability check passed after closure for the final EXE; see `PORTABILITY-CHECKS.md`. |
| LOT-003 | — | [V0.2 — Safe result actions](Specs/003-safe-accept/SPEC-003.md) | Planned | — | No result yet. Start after LOT-002 closes. |
| LOT-004 | — | [V0.3 — MAF and local Ollama](Specs/004-ollama-maf/SPEC-004.md) | Planned | — | No result yet. Start after LOT-003 closes. |
| LOT-005 | — | [V0.4 — Declarative actions](Specs/005-declarative-actions/SPEC-005.md) | Planned | — | No result yet. Start after LOT-004 closes. |
| LOT-006 | — | [V0.5 — Connections and profiles](Specs/006-connections-profiles/SPEC-006.md) | Planned | — | No result yet. Start after LOT-005 closes. |
| LOT-007 | — | [V0.6 — Local-first privacy and debug](Specs/007-privacy-debug/SPEC-007.md) | Planned | — | No result yet. Start after LOT-006 closes. |
| LOT-008 | — | [V0.7 — Settings and localization](Specs/008-settings-localization/SPEC-008.md) | Planned | — | No result yet. Start after LOT-007 closes. |
| LOT-009 | — | [V0.8 — Optional remote provider](Specs/009-remote-provider/SPEC-009.md) | Planned | — | No result yet. Start after LOT-008 closes. |
| LOT-010 | — | [V0.9 — Hardening and compatibility](Specs/010-hardening/SPEC-010.md) | Planned | — | No result yet. Start after LOT-009 closes. |
| LOT-011 | — | [V1.0 — Release qualification](Specs/011-v1-release/SPEC-011.md) | Planned | — | No result yet. Start after LOT-010 closes. |

## Recovery summary

LOT-001 and LOT-002 are Closed with TOTAL convergence; their accepted results are identified in `Specs/001-visual-baseline/CONVERGENCE-001.md` and `Specs/002-windows-shell/CONVERGENCE-002.md`. The V0.1 EXE remains at the D-001 canonical path `src/TextAid.App/bin/Publish/TextAid.exe`, produced by root `publish.ps1`; its SHA-256 is in G-002-001 and the finalized convergence. Olivier accepted closure after all four active gates passed and all ten LOT-002 findings received terminal local dispositions. Under D-002/R-012, the separate-machine Windows x64 check was DEFERRED at closure and was later confirmed PASS by Olivier for the final EXE copied alone, launched, and checked in About on a second Windows x64 machine; `PORTABILITY-CHECKS.md` records the result. The superseded earlier EXE remains untested. D-003/R-013 preserve the closed V0.1 Ctrl+C+C behavior. D-004 through D-007 and active R-015 through R-018 govern the planned two-shortcut, direct-translation, Replace/Copy behavior. The default mnemonics are Choose (`Ctrl+C`, then `C`) and Translate (`Ctrl+C`, then `T`); R-006, R-013, and R-014 remain in the historical record. LOT-003 now plans Replace/Copy/Cancel, LOT-005 Translate parameters, LOT-006 language preferences and shortcut storage, LOT-008 the quick path and Settings UI, and LOT-010/011 validation. All later lots remain Planned; no gate was reevaluated and the closed EXE is unchanged. In restricted workspaces, use the project-local `NUGET_SCRATCH` procedure in `AGENTS.md` and `README.md` before restore/build/test; `publish.ps1` applies it automatically. Next action: Olivier may decide to start LOT-003; it remains Planned until that separate human decision.
