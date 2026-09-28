# Lot status

`STATUS.md` is the authority for current lot states. Each lot's spec, findings, and gates live in its `Specs/` directory.

| Lot reference | Parent | Title | Status | Replaced by | Comment |
|---|---|---|---|---|---|
| LOT-001 | — | [Pré-V0.1 — Visual baseline](Specs/001-visual-baseline/SPEC-001.md) | Closed | — | Candidate 2 accepted by Olivier; both gates PASS; TOTAL convergence recorded in `CONVERGENCE-001.md`. |
| LOT-002 | — | [V0.1 — Windows shell and foundations](Specs/002-windows-shell/SPEC-002.md) | Closed | — | Olivier accepted TOTAL convergence on the V0.1 EXE; all four gates PASS and all findings are terminal. The separate-machine portability check passed after closure for the final EXE; see `PORTABILITY-CHECKS.md`. |
| LOT-003 | — | [V0.2 — Safe result actions](Specs/003-safe-accept/SPEC-003.md) | Closed | — | Olivier accepted TOTAL convergence; both gates PASS and all four findings are terminal. See `CONVERGENCE-003.md`. |
| LOT-004 | — | [V0.3 — MAF and local Ollama](Specs/004-ollama-maf/SPEC-004.md) | Closed | — | Olivier accepted TOTAL convergence on the V0.3 EXE; both gates PASS and no findings were recorded. See `CONVERGENCE-004.md`. |
| LOT-005 | — | [V0.4 — Declarative actions](Specs/005-declarative-actions/SPEC-005.md) | Closed | — | Olivier accepted TOTAL convergence; both gates PASS. |
| LOT-006 | — | [V0.5 — Connections and profiles](Specs/006-connections-profiles/SPEC-006.md) | Closed | — | Olivier confirmed the contained transform-session branding, status-guidance, and shared-menu corrections are complete and authorized reclosure on 2026-09-27. Both gates remain PASS; see `CONVERGENCE-006.md`. |
| LOT-007 | — | [V0.6 — Local-first privacy and debug](Specs/007-privacy-debug/SPEC-007.md) | Closed | — | Olivier accepted TOTAL convergence on 2026-09-27. Both gates PASS; safe and Full log diagnostics, credential redaction, and the visible Full log indicator are complete. See `CONVERGENCE-007.md`. |
| LOT-008 | — | [V0.7 — Settings and localization](Specs/008-settings-localization/SPEC-008.md) | Closed | — | Olivier confirmed complete validation and closure on 2026-09-28; both gates PASS and TOTAL convergence is recorded in `CONVERGENCE-008.md`. |
| LOT-009 | — | [V0.8 — Optional remote provider](Specs/009-remote-provider/SPEC-009.md) | Closed | — | Olivier confirmed both gates and authorized TOTAL closure on 2026-09-28. The V0.8 OpenAI-compatible provider uses CurrentUser DPAPI credentials; see `CONVERGENCE-009.md`. |
| LOT-010 | — | [V0.9 — Hardening and compatibility](Specs/010-hardening/SPEC-010.md) | Planned | — | No result yet. LOT-009 is closed, so this lot may start when authorized. |
| LOT-011 | — | [V1.0 — Release qualification](Specs/011-v1-release/SPEC-011.md) | Planned | — | No result yet. Start after LOT-010 closes. |
| LOT-012 | — | [Post-V1 — Language corrections and packs](Specs/012-language-corrections-packs/SPEC-012.md) | Planned | — | User correction layer, import/export, and community language-pack workflow; starts after LOT-011. |

## Recovery summary

LOT-001 through LOT-009 are Closed with TOTAL convergence; their accepted results are identified by their respective `CONVERGENCE-xxx.md` artifacts. The V0.1 EXE remains at the D-001 canonical path `src/TextAid.App/bin/Publish/TextAid.exe`, produced by root `publish.ps1`; its SHA-256 is in G-002-001 and the finalized convergence. Under D-002/R-012, the separate-machine Windows x64 check was later confirmed PASS for the final V0.1 EXE. D-003/R-013 preserve the closed V0.1 Ctrl+C+C behavior. The active V1 behavior includes Choose (`Ctrl+C`, then `C`), Translate (`Ctrl+C`, then `T`), safe Replace/Copy, configurable actions, profiles, Settings, localization, and the optional OpenAI-compatible External provider. LOT-010 through LOT-012 remain Planned. In restricted workspaces, use the project-local `NUGET_SCRATCH` procedure in `AGENTS.md` and `README.md` before restore/build/test; `publish.ps1` applies it automatically.

Current state: LOT-009 is Closed with TOTAL convergence following Olivier's complete validation and explicit closure authorization on 2026-09-28. F-006-002 remains resolved by the completed language workflow, and F-009-001 is resolved by the DPAPI-first External authentication route. LOT-010 is the next planned lot.
