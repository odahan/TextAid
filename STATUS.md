# Lot status

`STATUS.md` is the authority for current lot states. Each lot's spec, findings, and gates live in its `Specs/` directory.

| Lot reference | Parent | Title | Status | Replaced by | Comment |
|---|---|---|---|---|---|
| LOT-001 | — | [Pre-V0.1 — Visual baseline](Specs/001-visual-baseline/SPEC-001.md) | Closed | — | Candidate 2 accepted by Olivier; both gates PASS; TOTAL convergence recorded in `CONVERGENCE-001.md`. |
| LOT-002 | — | [V0.1 — Windows shell and foundations](Specs/002-windows-shell/SPEC-002.md) | Closed | — | Olivier accepted TOTAL convergence on the V0.1 EXE; all four gates PASS and all findings are terminal. The separate-machine portability check passed after closure for the final EXE; see `PORTABILITY-CHECKS.md`. |
| LOT-003 | — | [V0.2 — Safe result actions](Specs/003-safe-accept/SPEC-003.md) | Closed | — | Olivier accepted TOTAL convergence; both gates PASS and all four findings are terminal. See `CONVERGENCE-003.md`. |
| LOT-004 | — | [V0.3 — MAF and local Ollama](Specs/004-ollama-maf/SPEC-004.md) | Closed | — | Olivier accepted TOTAL convergence on the V0.3 EXE; both gates PASS and no findings were recorded. See `CONVERGENCE-004.md`. |
| LOT-005 | — | [V0.4 — Declarative actions](Specs/005-declarative-actions/SPEC-005.md) | Closed | — | Olivier accepted TOTAL convergence; both gates PASS. |
| LOT-006 | — | [V0.5 — Connections and profiles](Specs/006-connections-profiles/SPEC-006.md) | Closed | — | Olivier confirmed the contained transform-session branding, status-guidance, and shared-menu corrections are complete and authorized reclosure on 2026-09-27. Both gates remain PASS; see `CONVERGENCE-006.md`. |
| LOT-007 | — | [V0.6 — Local-first privacy and debug](Specs/007-privacy-debug/SPEC-007.md) | Closed | — | Olivier accepted TOTAL convergence on 2026-09-27. Both gates PASS; safe and Full log diagnostics, credential redaction, and the visible Full log indicator are complete. See `CONVERGENCE-007.md`. |
| LOT-008 | — | [V0.7 — Settings and localization](Specs/008-settings-localization/SPEC-008.md) | Closed | — | Olivier confirmed complete validation and closure on 2026-09-28; both gates PASS and TOTAL convergence is recorded in `CONVERGENCE-008.md`. |
| LOT-009 | — | [V0.8 — Optional remote provider](Specs/009-remote-provider/SPEC-009.md) | Closed | — | Olivier confirmed both gates and authorized TOTAL closure on 2026-09-28. The V0.8 OpenAI-compatible provider uses CurrentUser DPAPI credentials; see `CONVERGENCE-009.md`. |
| LOT-010 | — | [V0.9 — Hardening and compatibility](Specs/010-hardening/SPEC-010.md) | Closed | — | Olivier accepted PARTIAL convergence on published candidate SHA-256 `1FBB9C060A723212EDD40DE9A7D5DDA833225A968BAFA1B419DEA8AA3FB025BB`; G-010-001 and G-010-003 PASS, G-010-002 N/A under D-041. Final convergence is finalized; deferred VS Code and DPI/multi-monitor checks remain explicit residual work in `CONVERGENCE-010.md`. |
| LOT-011 | — | [V1.0 — Release qualification](Specs/011-v1-release/SPEC-011.md) | Closed | — | Olivier accepted TOTAL convergence on published V1 candidate SHA-256 `48BDA1471C6971242A60FB2EA1D1DE04AF930D5A2CFAC7A4E9733D70C3D67F8E`; both gates PASS under D-043. The separate-machine check remains deferred under R-012. |
| LOT-012 | — | [Post-V1 — Language corrections and packs](Specs/012-language-corrections-packs/SPEC-012.md) | Closed | — | Olivier accepted TOTAL convergence on the published 1.1.0 candidate; both gates PASS. See `CONVERGENCE-012.md`. |
| LOT-013 | — | [Post-V1 — Rendered Markdown preview](Specs/013-markdown-preview/SPEC-013.md) | Closed | — | Olivier accepted TOTAL convergence on the published 1.1.0 candidate; both gates PASS. See `CONVERGENCE-013.md`. |
| LOT-014 | — | [Modernisation accès OpenAI](Specs/014-openai-access-modernization/SPEC-014.md) | In-progress | — | Implementation started on Olivier's explicit 2026-10-18 request; automated and human gates pending. |

## Recovery summary

LOT-001 through LOT-009 and LOT-011 through LOT-013 are Closed with TOTAL convergence; LOT-010 is Closed with PARTIAL convergence under D-041. Their accepted results are identified by their respective `CONVERGENCE-xxx.md` artifacts. The accepted LOT-013 baseline EXE was `src/TextAid.App/bin/Publish/TextAid.exe`, version 1.1.0, SHA-256 `F3D9715551624D3A382DD351CFD5C1EF511F3A35BC87479DE2563D344A923E7A`. Its separate-machine check remains deferred under R-012. The active behavior includes Choose (`Ctrl+C`, then `C`), Translate (`Ctrl+C`, then `T`), safe Replace/Copy, configurable actions, profiles, Settings, localization, the optional OpenAI-compatible External provider, a per-user Windows startup option, personal UI-translation corrections, validated offline language packs, and a native inert Markdown review preview. In restricted workspaces, use the project-local `NUGET_SCRATCH` procedure in `AGENTS.md` and `README.md` before restore/build/test; `publish.ps1` applies it automatically.

Current state: LOT-014 is In-progress following Olivier's explicit implementation request on 2026-10-18. The published 1.3.2 candidate implements the fourth connection, transient profile selector and raw-result transfer. Four automatic gates PASS; G-014-004/005 require named real-account and visual human validation. Earlier accepted lots and maintenance results remain historical evidence.

## Corrective maintenance — 2026-10-04

D-044 and R-032 govern the newly published asynchronous AI language-identification implementation. Both capture shortcuts start preliminary classification off the UI thread; processing shares the same request and input-scoped result. Fixed language heuristics are removed. Uncertain input requires an explicit translation destination, and regional variants are routed by primary language.

The release target is `src/TextAid.App/bin/Publish/TextAid.exe`, version 1.2.12. Startup preload waits 30 seconds and retries unavailable Ollama every 30 seconds; retention follows the server setting. Actions, including Translate, support Thinking Off by default and an optional context override. Safe Debug includes running-model memory, context, and expiry metadata. All 169 Release tests passed (55 AI, 89 Core, 25 Windows); root publish.ps1 produced the canonical executable, with FileVersion 1.2.12.0 and SHA-256 `1AD24123A7B12C9A2101C9A706C97C6785FAC70F774C10D248BAB635F0AF33FD`. Live startup/UI and idle-latency validation remain pending, as does the separate-machine check. Olivier authorized the complete release commit, annotated tag `V1212`, and push. Cache quantization remains deferred. No accepted lot state or historical gate result is changed by this maintenance work.

Launch-context correction: direct child-process launches from Codex can read an isolated package-cache configuration. Use root start.ps1 to dispatch the published EXE through the Windows desktop shell. A desktop-context diagnostic confirmed the real user configuration is intact and has an active configured provider. The launcher preserves the normal desktop configuration context; no provider settings or secrets were modified.

Intermittent 1.2.7 Ctrl-C-T detection failures were diagnosed from the live log as action-file sharing violations before inference. Version 1.2.8 reuses the capture action snapshot for preliminary provider resolution, removing the competing background action-file reload. All 143 Release tests pass; no live repeated-capture or separate-machine PASS is claimed.

## Inference maintenance — 2026-10-06

Version 1.2.9 removes Ollama's implicit 100-second HTTP deadline while retaining the configured inference deadline, bounds warmup and model-status checks, and streams visible output into a safe incomplete preview. Diagnostic metadata reports request and server durations, token counts, and interrupted requests. All 148 Release tests pass, including transport-level Ollama and preview-state regressions. Olivier reported concurrent MyLocalAgent jobs; performance validation is deferred until the shared server is idle. No user configuration or provider secrets were changed.

## Resolved compatibility issue — Replace in smalBazar (MAUI/WinUI) — 2026-10-17

Status: Resolved in published version 1.2.13. Olivier retested the affected application and confirmed that replacement now works on 2026-10-17.

The Replace action does not work in smalBazar, a .NET MAUI application whose Windows UI is understood to use WinUI. Olivier reports that Replace works in the WPF applications and all other applications tested so far. He clarified that TextAid closes but the target text remains unchanged. The running process is `SmallBazar.exe`, installed as MSIX version 1.6.13.0. The exact target control and failure-time focus metadata remain unrecorded.

Investigation found that Replace forced `SetFocus` onto the source's top-level HWND after activation, overriding child-window focus. A native Windows regression test fails with that call restored and passes without it. Candidate 1.2.13 removes the forced focus, restores minimized windows only, validates foreground keyboard-focus ownership, and rechecks the target before input injection. Opt-in Safe Debug now records numeric focus handles, input counts, native error codes, and the replacement outcome without user text. All 173 Release tests pass (55 AI, 89 Core, 29 Windows). Root `publish.ps1` produced FileVersion 1.2.13.0, SHA-256 `5BB0574543E013FFA80FBB261F9647E30409BD5F0400856EFBEB85AF2710EF07`; the idle resident instance was restarted via root `start.ps1`. See [the investigation record](docs/replace-winui-investigation.md).

Validation and remaining compatibility coverage:

- Olivier confirmed that Replace works in smalBazar after the 1.2.13 restart.
- Olivier also confirmed on 2026-10-17 that Replace still works in the Codex input area with 1.2.13, providing a live non-regression check in a previously working application. This does not establish the target control's UI technology.
- The native regression test confirms the forced top-level focus defect; all 173 Release tests pass.
- A fresh live WPF comparison and a second WinUI application check have not been recorded. Application validation currently covers smalBazar and the Codex input area.

## Active extension — 2026-10-18

LOT-014 is In-progress. Result: src/TextAid.App/bin/Publish/TextAid.exe, FileVersion 1.3.0.0, SHA-256 DBA9E61A8D2E9F07BB5E2BD676720C0F36C8981205318C01BDC656112D794FB7. Release build: zero warnings/errors; 232 tests pass. G-014-001/002/003/006 PASS against validation/SOURCE-014.sha256; G-014-004/005 remain TO TEST. Implementation mapping, official contract review, logs and a concrete human walkthrough are in Specs/014-openai-access-modernization/IMPLEMENTATION-014.md. The EXE was launched via start.ps1 and observed resident; Olivier then reported it closed and the process check confirmed it stopped. No account credentials were imported. Remaining work: real Windows account/MFA/permission/inference/restart/sign-out journey and visual/keyboard/transfer/selector acceptance. Separate-machine portability is DEFERRED. Next action: Olivier evaluates the published candidate for the two HUMAN gates; preserve the In-progress state until those decisions and later closure acceptance. No implementation blocker is asserted.

## Cosmetic follow-up — 2026-10-18

Olivier reports the functional tests pass. Corrected the selector to display only Label, including Default. Current canonical executable: FileVersion 1.3.1.0, SHA-256 622E13C1A0FC1C6384ABCC5681225DAF7109E41F5AF97EC82ACC16B0768ABD6A; Release build zero warnings/errors and canonical publication succeeded. See F-014-007 and IMPLEMENTATION-014.md. Functional gate evidence remains applicable; G-014-006 records the replacement candidate and preserves its prior evaluation. G-014-004/005 still await an explicit named gate verdict; LOT-014 remains In-progress. The app was left stopped.

## Settings cosmetic follow-up — 2026-10-18

Settings / Sign in with OpenAI now uses an explicit DisplayName DataTemplate for the account model ComboBox, including its selected resting display. SelectedValuePath remains Slug, preserving the inference model identity. Release app build: zero warnings/errors. Root publish.ps1 produced FileVersion 1.3.2.0, SHA-256 DA7AF13F2DB14C5B5C1D7EAE19A9BDA71D4FA18806C677C99A2B331A2616ED35. No functional changes or new tests; prior 232-test evidence remains applicable. Build/publication logs and source manifest are retained in validation/ with the 1.3.2 suffix. App left stopped. LOT-014 remains In-progress; human gate verdicts and closure remain pending.
