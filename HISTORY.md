# Project history

This append-only record uses concise entries with timestamp, actor, operation, affected artifacts, and outcome. It is not a substitute for `STATUS.md`, `LEDGER.md`, or gate evidence.

## 2026-09-25 — Pro-Spec project initialized

Actor: Codex
Operation: Decomposed the retained TextAid V1 source specification into planned Pro-Spec lots; initialized the global artifacts and copied the normative Pro-Spec 3 reference locally.
Affected artifacts: `README.md`, `PROJECT.md`, `RULES.md`, `STATUS.md`, `LEDGER.md`, `HISTORY.md`, `Specs/`, `docs/PROSPEC-3-SPECIFICATION.md`.
Outcome: Planning baseline created. No implementation, gate validation, lot start, or human closure is claimed.

## 2026-09-26 — LOT-001 visual candidate prepared

Actor: Codex
Operation: Started LOT-001 on the user's instruction; documented palette, typography, dimensions, control states, and native dark chrome; prepared a reference screen and application icon; evaluated AUTO gate G-001-002.
Affected artifacts: `STATUS.md`, `Specs/001-visual-baseline/`, `assets/TextAid-icon.svg`, `assets/TextAid-icon.png`, `assets/TextAid.ico`, `assets/render_icon.py`.
Outcome: Candidate 1 is available for review. G-001-002 is PASS; G-001-001 awaits named human validation. LOT-001 remains In-progress and no closure is claimed.

## 2026-09-26 — LOT-001 brand asset correction pending

Actor: User and Codex
Operation: The user reviewed candidate 1, accepted the rest of the visual baseline, and reported anomalies in the logo. The user will revise the SVG before PNG and ICO regeneration.
Affected artifacts: `Specs/001-visual-baseline/FINDINGS-001.md`, `Specs/001-visual-baseline/GATES-001.md`, `Specs/001-visual-baseline/CONVERGENCE-001.md`, `STATUS.md`.
Outcome: G-001-001 remains TO TEST. Candidate 1's AUTO evaluation remains recorded; the next result will be reevaluated after asset changes. LOT-001 remains In-progress.

## 2026-09-26 — LOT-001 icon regenerated from corrected source

Actor: User and Codex
Operation: The user corrected `TextAid-icon.svg` and `TextAid-icon.png`; Codex changed the icon exporter to preserve those sources, regenerated `TextAid.ico`, and reevaluated G-001-002.
Affected artifacts: `assets/TextAid-icon.svg`, `assets/TextAid-icon.png`, `assets/TextAid.ico`, `assets/render_icon.py`, `Specs/001-visual-baseline/FINDINGS-001.md`, `Specs/001-visual-baseline/GATES-001.md`, `Specs/001-visual-baseline/CONVERGENCE-001.md`, `STATUS.md`.
Outcome: Candidate 2 is available. The PNG checksum remained unchanged during ICO generation; G-001-002 is PASS on candidate 2. G-001-001 awaits human validation, and LOT-001 remains In-progress.

## 2026-09-26 — LOT-001 closed with total convergence

Actor: Olivier and Codex
Operation: Olivier accepted candidate 2 and authorized lot closure; Codex recorded G-001-001 PASS, confirmed G-001-002 PASS and terminal findings, and finalized `CONVERGENCE-001.md`.
Affected artifacts: `Specs/001-visual-baseline/GATES-001.md`, `Specs/001-visual-baseline/FINDINGS-001.md`, `Specs/001-visual-baseline/CONVERGENCE-001.md`, `STATUS.md`.
Outcome: LOT-001 closed with TOTAL convergence. LOT-002 remains Planned and awaits a separate human start decision.

## 2026-09-26 — LOT-002 V0.1 candidate prepared

Actor: User and Codex
Operation: The user instructed Codex to perform the next lot. Codex started LOT-002, created the .NET 10 solution and project boundaries, implemented the Windows resident shell and dark WPF views, added deterministic keyboard tests, built the solution, and published a self-contained win-x64 single executable.
Affected artifacts: `STATUS.md`, `Specs/002-windows-shell/`, `TextAid.sln`, `Directory.Build.props`, `src/`, `tests/`, `tools/`, `.gitignore`, `artifacts/publish-v01-final/TextAid.exe`.
Outcome: G-002-001 and G-002-002 are PASS. The final EXE starts resident and creates user configuration in an authorized Windows run. G-002-003 remains TO TEST pending Olivier's desktop walkthrough; LOT-002 remains In-progress and no closure is claimed.

## 2026-09-26 — D-001 canonical publication applied

Actor: Olivier and Codex
Operation: Olivier decided that all future final EXEs belong in `src/TextAid.App/bin/Publish/` and requested a root `publish.ps1`. Codex recorded D-001 and R-011, added REQ-002-008 and G-002-004, configured the Release publish directory, and created and ran the script twice. G-002-001's previous evaluation was preserved before revalidation.
Affected artifacts: `LEDGER.md`, `RULES.md`, `README.md`, `Specs/002-windows-shell/SPEC-002.md`, `Specs/002-windows-shell/GATES-002.md`, `Specs/002-windows-shell/FINDINGS-002.md`, `Specs/002-windows-shell/WALKTHROUGH-002.md`, `src/TextAid.App/TextAid.App.csproj`, `publish.ps1`, `STATUS.md`, `src/TextAid.App/bin/Publish/TextAid.exe`.
Outcome: The canonical Publish directory contains only `TextAid.exe`; G-002-001 and G-002-004 PASS on that result. G-002-002 retains its PASS after impact review. G-002-003 remains TO TEST and LOT-002 remains In-progress.

## 2026-09-26 — D-001 aligned with planned lots

Actor: Codex
Operation: Updated the dependency sections of planned LOT-003 through LOT-011 to include new global rule R-011, preserving their existing planned requirements and gates.
Affected artifacts: `Specs/003-safe-accept/SPEC-003.md` through `Specs/011-v1-release/SPEC-011.md`.
Outcome: Later lots explicitly inherit the canonical Release publish location decided by Olivier in D-001.

## 2026-09-26 — NuGet sandbox procedure recorded

Actor: Olivier and Codex
Operation: At Olivier's request, Codex investigated recurring NuGet lock errors. The reported lock file was absent, normal-access restore succeeded, and the restricted-workspace restore, build, and tests succeeded using a project-local `NUGET_SCRATCH`. Codex documented that procedure for future work.
Affected artifacts: `AGENTS.md`, `README.md`, `Specs/002-windows-shell/FINDINGS-002.md`, `STATUS.md`.
Outcome: The working restore/build/test sequence is verified, including seven passing keyboard tests. The issue is restricted cache access in the sandbox; no product result or gate condition changed.

## 2026-09-26 — LOT-002 desktop validation corrections

Actor: Olivier and Codex
Operation: Olivier reported white tray separator gutters, a white About window, and no visible response to Ctrl+C+C. Codex preserved his failed evaluation, corrected the tray templates and explicit window backgrounds, tracked Ctrl transitions in the hook, and fixed the read-only session text binding that prevented the window from opening. The clipboard reader now waits for Unicode text during its bounded retries, and unexpected capture failures display tray feedback.
Affected artifacts: `Specs/002-windows-shell/FINDINGS-002.md`, `Specs/002-windows-shell/GATES-002.md`, `Specs/002-windows-shell/WALKTHROUGH-002.md`, `src/TextAid.App/`, `src/TextAid.Platform.Windows/`, `tests/TextAid.Platform.Windows.Tests/`, `STATUS.md`, `src/TextAid.App/bin/Publish/TextAid.exe`.
Outcome: The solution builds with zero warnings/errors, nine keyboard tests pass, and `publish.ps1` leaves one corrected EXE (SHA-256 `61823D3AE5797606B356CB781490F532D8FC2FCB3807A3F96D6DD93102B453F0`). Render probes show dark tray and About surfaces; a Windows launch plus synthetic Ctrl+C+C opened a visible session. AUTO gates G-002-001, G-002-002, and G-002-004 are PASS. G-002-003 is TO TEST pending Olivier's complete walkthrough; LOT-002 stays In-progress.

## 2026-09-26 — LOT-002 corrected candidate partly validated

Actor: Olivier and Codex
Operation: Olivier validated the corrected tray menu, screens and theme, ordinary Ctrl+C, the Ctrl+C+C invocation, and the selected text displayed in TextAid. Codex recorded that human evidence against the corrected published EXE and updated the three reported findings.
Affected artifacts: `Specs/002-windows-shell/FINDINGS-002.md`, `Specs/002-windows-shell/GATES-002.md`, `STATUS.md`.
Outcome: F-002-004 through F-002-006 are confirmed corrected by Olivier. The full G-002-003 walkthrough remains TO TEST for its other stated conditions; LOT-002 remains In-progress. No closure decision was made.

## 2026-09-26 — LOT-002 tray and About validation continued

Actor: Olivier and Codex
Operation: Olivier confirmed tray Enable/Disable and Exit, successful launch and stop of the EXE on his current Windows machine, and the About details except an unwanted version suffix. He stated that a second Windows x64 machine is not currently available for the standalone distribution test. Codex identified the suffix as .NET Git metadata in the assembly informational version, changed About to display only the product version, and republished the canonical EXE.
Affected artifacts: `src/TextAid.App/AboutWindow.xaml.cs`, `Specs/002-windows-shell/FINDINGS-002.md`, `Specs/002-windows-shell/GATES-002.md`, `Specs/002-windows-shell/WALKTHROUGH-002.md`, `STATUS.md`, `src/TextAid.App/bin/Publish/TextAid.exe`.
Outcome: The solution builds without warnings/errors; `publish.ps1` leaves one EXE with SHA-256 `59204A2BCC32D46C7E37742FB818CA6FE3E3A20BC7641D4EC3B574001D224D0D`. The earlier keyboard test PASS remains valid because its code did not change. The full HUMAN gate G-002-003 remains TO TEST for the updated About display and outstanding walkthrough checks, including clean-machine distribution. LOT-002 remains In-progress.

## 2026-09-26 — LOT-002 About version display validated

Actor: Olivier and Codex
Operation: Olivier confirmed that the corrected About version display is right on the current published EXE. Codex recorded this validation in F-002-007 and G-002-003.
Affected artifacts: `Specs/002-windows-shell/FINDINGS-002.md`, `Specs/002-windows-shell/GATES-002.md`, `Specs/002-windows-shell/WALKTHROUGH-002.md`, `STATUS.md`.
Outcome: The About version correction is accepted. G-002-003 remains TO TEST for other walkthrough conditions, including the deferred second-machine distribution check; LOT-002 remains In-progress.

## 2026-09-26 — D-002 separate-machine check made nonblocking

Actor: Olivier and Codex
Operation: Olivier decided that testing each final EXE on a separate clean Windows x64 machine remains a recurring verification but its temporary unavailability cannot alone block lot closure. Codex recorded D-002, added R-012 and `PORTABILITY-CHECKS.md`, preserved the former G-002-003 condition and method, clarified its active condition, updated the walkthrough, and applied the rule to planned LOT-003 through LOT-011.
Affected artifacts: `LEDGER.md`, `RULES.md`, `README.md`, `PORTABILITY-CHECKS.md`, `Specs/002-windows-shell/SPEC-002.md`, `Specs/002-windows-shell/GATES-002.md`, `Specs/002-windows-shell/WALKTHROUGH-002.md`, `Specs/003-safe-accept/SPEC-003.md` through `Specs/011-v1-release/SPEC-011.md`, `STATUS.md`.
Outcome: The current EXE's separate-machine check is DEFERRED, not cancelled. G-002-001, G-002-002, and G-002-004 retain PASS after impact review; G-002-003 stays TO TEST for current-machine walkthrough items that Olivier has not explicitly confirmed. No binary changed and LOT-002 remains In-progress.

## 2026-09-26 — LOT-002 instance and placement corrections

Actor: Olivier and Codex
Operation: Olivier reported that the session appeared in the upper-left area, repeated launches created multiple tray instances, and Ctrl+C+C later stopped responding, preventing Cancel/close testing. Codex recorded this failed walkthrough, added a named mutex before hook and tray initialization, reapplied placement after WPF window loading, rebuilt and republished the EXE, and ran an interactive Windows probe on the second monitor.
Affected artifacts: `src/TextAid.App/App.xaml.cs`, `src/TextAid.App/MainWindow.xaml.cs`, `Specs/002-windows-shell/FINDINGS-002.md`, `Specs/002-windows-shell/GATES-002.md`, `Specs/002-windows-shell/WALKTHROUGH-002.md`, `Specs/002-windows-shell/evidence/shell-integration-corrected.txt`, `PORTABILITY-CHECKS.md`, `STATUS.md`, `src/TextAid.App/bin/Publish/TextAid.exe`.
Outcome: The build passed with zero warnings/errors and nine keyboard tests passed. `publish.ps1` left one EXE, SHA-256 `D9B7890D45B4CA458DEF74A533D940909586062EACF4477C7F4D6FC20DD19A4B`. The probe showed a second process exiting, one session centered with zero offset in the second monitor's work area, no duplicate session, successful window close, and a new session after retriggering. AUTO gates G-002-001, G-002-002, and G-002-004 are PASS; HUMAN G-002-003 is TO TEST on this candidate. The separate-machine portability check remains deferred under R-012.

## 2026-09-26 — D-003 no-selection invocation preserved

Actor: Olivier and Codex
Operation: Olivier validated that Ctrl+C+C opens the current EXE's session without selected text and that Cancel and the Windows close button close the window. He decided that no-selection invocation must remain available for future direct text entry. Codex recorded D-003, added R-013, updated the LOT-002 walkthrough and validation evidence, and applied the rule to planned LOT-003 through LOT-011.
Affected artifacts: `LEDGER.md`, `RULES.md`, `Specs/002-windows-shell/SPEC-002.md`, `Specs/002-windows-shell/FINDINGS-002.md`, `Specs/002-windows-shell/GATES-002.md`, `Specs/002-windows-shell/WALKTHROUGH-002.md`, `Specs/003-safe-accept/SPEC-003.md` through `Specs/011-v1-release/SPEC-011.md`, `STATUS.md`.
Outcome: The current V0.1 EXE is unchanged; it continues to open on Ctrl+C+C without a selection. Manual input is a possible later workflow, not a V0.1 feature. The existing AUTO gate results remain valid. G-002-003 stays TO TEST pending Olivier's confirmation of placement, instance handling, and window constraints on the current EXE.

## 2026-09-26 — LOT-002 prepared for closure

Actor: Olivier and Codex
Operation: Olivier confirmed the current candidate's centering and second-instance tests. Codex recorded G-002-003 HUMAN PASS, confirmed the other three AUTO gates remain valid on the same EXE, reviewed all eight active requirements and ten terminal findings, and prepared `CONVERGENCE-002.md` as a TOTAL closure proposal.
Affected artifacts: `Specs/002-windows-shell/GATES-002.md`, `Specs/002-windows-shell/FINDINGS-002.md`, `Specs/002-windows-shell/WALKTHROUGH-002.md`, `Specs/002-windows-shell/CONVERGENCE-002.md`, `STATUS.md`.
Outcome: All four active gates are PASS for the V0.1 EXE, SHA-256 `D9B7890D45B4CA458DEF74A533D940909586062EACF4477C7F4D6FC20DD19A4B`. No finding remains OPEN. The separate-machine check stays DEFERRED under D-002/R-012 and does not block closure. LOT-002 moves to Ready-to-close and awaits Olivier's explicit acceptance of the proposed convergence and closure.

## 2026-09-26 — LOT-002 closed with total convergence

Actor: Olivier and Codex
Operation: Olivier explicitly accepted closure of LOT-002. Codex finalized `CONVERGENCE-002.md` for the identified V0.1 EXE, confirmed all four gates PASS and all ten findings terminal, and moved the lot from Ready-to-close to Closed.
Affected artifacts: `Specs/002-windows-shell/CONVERGENCE-002.md`, `STATUS.md`.
Outcome: LOT-002 is Closed with TOTAL convergence. The separate-machine Windows x64 check remains DEFERRED in `PORTABILITY-CHECKS.md` under D-002/R-012; it was neither performed nor cancelled. LOT-003 remains Planned until a separate human start decision.

## 2026-09-26 — Future quick translation and configurable shortcuts specified

Actor: Olivier and Codex
Operation: After LOT-002 closure, Olivier specified a direct translation path on `Ctrl+C+T`, automatic target selection from two language preferences, destination changes that rerun translation, safe replacement availability after completion, and Settings reassignment of the invocation shortcuts. He clarified the fallback to the user language for third or uncertain source languages and retained the `Accept` button label. Codex recorded D-004/D-005, added R-014/R-015/R-016, preserved R-013 as the historical rule for the closed V0.1 lot, and refined affected planned specs and gates.
Affected artifacts: `PROJECT.md`, `LEDGER.md`, `RULES.md`, `Specs/003-safe-accept/SPEC-003.md` through `Specs/011-v1-release/SPEC-011.md`, `Specs/005-declarative-actions/GATES-005.md`, `Specs/006-connections-profiles/GATES-006.md`, `Specs/008-settings-localization/GATES-008.md`, `Specs/010-hardening/GATES-010.md`, `Specs/011-v1-release/GATES-011.md`, `STATUS.md`.
Outcome: The default normal-action shortcut remains `Ctrl+C+C`; quick translation defaults to `Ctrl+C+T`. LOT-005 prepares Translate parameters, LOT-006 persists preferences and bindings, LOT-008 activates the quick workflow and Settings controls, and LOT-010/011 validate them. LOT-002 and its accepted EXE remain unchanged. All later lots remain Planned; no implementation or gate evaluation is claimed.

## 2026-09-26 — Result actions clarified as Replace and Copy

Actor: Olivier and Codex
Operation: Olivier replaced the earlier `Accept` label decision with an explicit `Replace` action and added `Copy` for placing the result in the clipboard without modifying the selected source. He confirmed that successful Copy closes the session. Codex recorded D-006, preserved and superseded R-006/R-014 with R-017/R-018, and refined planned lots and gates from LOT-003 through V1.
Affected artifacts: `PROJECT.md`, `LEDGER.md`, `RULES.md`, `Specs/003-safe-accept/SPEC-003.md`, `Specs/003-safe-accept/GATES-003.md`, `Specs/004-ollama-maf/SPEC-004.md`, `Specs/004-ollama-maf/GATES-004.md`, `Specs/005-declarative-actions/SPEC-005.md` through `Specs/011-v1-release/SPEC-011.md`, `Specs/008-settings-localization/GATES-008.md`, `Specs/011-v1-release/GATES-011.md`, `STATUS.md`.
Outcome: Planned LOT-003 now introduces Replace/Copy/Cancel before AI. Quick translation enables Replace and Copy only for its current successful result. Closed LOT-002 and its V0.1 executable remain unchanged. No planned lot was started or evaluated.

## 2026-09-26 — Default shortcut mnemonics named

Actor: Olivier and Codex
Operation: Olivier specified the memory aid for the default gestures and confirmed the English spelling **Choose**. Codex recorded D-007 and aligned the project description and planned Settings and V1 requirements with `Ctrl+C`, then `C` for Choose or `T` for Translate.
Affected artifacts: `LEDGER.md`, `PROJECT.md`, `Specs/008-settings-localization/SPEC-008.md`, `Specs/011-v1-release/SPEC-011.md`, `STATUS.md`.
Outcome: Shortcut behavior, reassignment rules, closed LOT-002, and the accepted executable remain unchanged. Later lots remain Planned; no gate result is claimed.

## 2026-09-26 — LOT-002 separate-machine check completed after closure

Actor: Olivier and Codex
Operation: Olivier reported a successful test on a second Windows x64 machine and confirmed that the final LOT-002 EXE was copied alone, launched, and displayed About. Codex matched the still-published canonical EXE to SHA-256 `D9B7890D45B4CA458DEF74A533D940909586062EACF4477C7F4D6FC20DD19A4B` and updated the recurring portability register.
Affected artifacts: `PORTABILITY-CHECKS.md`, `HISTORY.md`, `STATUS.md`.
Outcome: The previously deferred check is PASS for the final LOT-002 EXE. The superseded older candidate remains untested. LOT-002 remains Closed; its original convergence and gate evaluations remain historical records of the closure state. No product code, publication, or later lot state changed.

## 2026-09-26 — LOT-003 started

Actor: Olivier and Codex
Operation: Olivier instructed Codex to perform LOT-003 after the completed bootstrap and with no other active lot.
Affected artifacts: `STATUS.md`, `HISTORY.md`.
Outcome: LOT-003 moved from Planned to In-progress. Its active work is the explicit Replace, Copy, and Cancel transaction behavior, including safe use of the captured source window and the non-distributed Windows test target.

## 2026-09-26 — LOT-003 V0.2 candidate prepared

Actor: Codex
Operation: Implemented the temporary uppercase preview, Replace/Copy/Cancel result actions, guarded native replacement using the invocation-time HWND, and the WPF test target. Replaced the former Windows Forms test target with the specified single-line and multiline TextBox, RichTextBox, focus-change control, and visible focus event display.
Affected artifacts: `src/TextAid.App/`, `src/TextAid.Core/InvocationSession.cs`, `src/TextAid.Platform.Windows/ResultActions.cs`, `tests/TextAid.Platform.Windows.Tests/ResultActionsTests.cs`, `tools/TextAid.TestTarget/`, `Specs/003-safe-accept/`.
Outcome: `dotnet build TextAid.sln -m:1 --no-restore -p:NuGetAudit=false` passed with zero warnings/errors and `dotnet test TextAid.sln -m:1 --no-build -p:NuGetAudit=false` passed 13 platform tests. `publish.ps1` produced the sole V0.2 `TextAid.exe` (SHA-256 `0AFC77FACD4C50B002DC1AFF44A58EFE421F1B174B066CBF6C056CC9DB6765A0`) in the canonical directory. G-003-001 is PASS; the required Olivier Windows safety walkthrough, G-003-002, remains TO TEST.

## 2026-09-26 — LOT-003 modifier safety coverage strengthened

Actor: Codex
Operation: Added a deterministic pressed-modifier timeout test and expanded the Win32 modifier check to include generic Ctrl, Alt, and Shift virtual keys as well as their left/right variants and Windows keys. Rebuilt, retested, and republished the candidate.
Affected artifacts: `src/TextAid.Platform.Windows/ResultActions.cs`, `tests/TextAid.Platform.Windows.Tests/ResultActionsTests.cs`, `Specs/003-safe-accept/GATES-003.md`, `HISTORY.md`.
Outcome: The solution rebuilt with zero warnings/errors and all 14 platform tests passed. The superseding V0.2 EXE is the sole 173,662,859-byte file at `src/TextAid.App/bin/Publish/TextAid.exe`, SHA-256 `A38B39129C90093E99AF5EE6AD89BD8B3F782B20CAB8A60C024BCDC38B3C0B6E`. G-003-001 remains PASS; G-003-002 remains TO TEST pending Olivier's Windows walkthrough.

## 2026-09-26 — LOT-003 corrections prepared from Olivier's walkthrough

Actor: Olivier and Codex
Operation: Olivier confirmed Cancel, close, capture, and uppercase transformation but reported failed Replace and an overlapping preview/status display. He decided on an elastic 50/50 review layout (D-008). Codex separated the status row, made the session resizable with equal-width panels, and strengthened native source-focus restoration before paste.
Affected artifacts: `LEDGER.md`, `Specs/003-safe-accept/`, `src/TextAid.App/MainWindow.xaml`, `src/TextAid.Platform.Windows/ResultActions.cs`.
Outcome: The corrected source builds with zero warnings/errors and the 14 platform tests pass. The prior published EXE is running (PID 43160), so it locks the canonical Publish file and prevents final publication. G-003-001 returns to TO TEST until the corrected EXE is published and identified; G-003-002 remains TO TEST.

## 2026-09-26 — LOT-003 corrected candidate published

Actor: Codex
Operation: After Olivier closed the resident previous candidate, published the focus-restoration and elastic-layout correction with the canonical root script.
Affected artifacts: `src/TextAid.App/bin/Publish/TextAid.exe`, `Specs/003-safe-accept/GATES-003.md`, `HISTORY.md`.
Outcome: The canonical directory contains exactly one 173,663,371-byte V0.2 EXE, SHA-256 `6DB09596045AD8824E3865F4DD7583E06285401DA51416B8FA7B0DBEE8E8F0A0`. G-003-001 is again PASS. F-003-002 and G-003-002 remain open for Olivier's retest of Replace; the visual correction is locally resolved and also awaits human review.

## 2026-09-26 — LOT-003 native paste and foreground correction prepared

Actor: Codex
Operation: Diagnosed Olivier's “could not paste safely” message as an undersized x64 native `INPUT` interop layout passed to `SendInput`. Added the full union layout and explicit session activation/temporary topmost behavior to prevent the session from opening behind other windows.
Affected artifacts: `src/TextAid.Platform.Windows/ResultActions.cs`, `src/TextAid.App/App.xaml.cs`, `src/TextAid.App/MainWindow.xaml.cs`, `Specs/003-safe-accept/`.
Outcome: The corrected source builds with zero warnings/errors and the 14 platform tests pass. The prior EXE remains running (PID 12540) and locks the Publish output, so G-003-001 is TO TEST until the new EXE is published. F-003-002 is resolved locally and awaits Olivier's retest; G-003-002 remains TO TEST.

## 2026-09-26 — LOT-003 native paste correction published

Actor: Codex
Operation: Published the final native-interoperability and foreground correction after Olivier closed the previous resident EXE.
Affected artifacts: `src/TextAid.App/bin/Publish/TextAid.exe`, `Specs/003-safe-accept/GATES-003.md`, `HISTORY.md`.
Outcome: The canonical directory contains exactly one 173,663,371-byte V0.2 EXE, SHA-256 `55D5C9C4E6C859204991AC20D2587A0CA6C3A0866765AD063FE742D8AA58A52D`. G-003-001 is PASS. Olivier's retest of Replace and session foreground visibility remains required for G-003-002.

## 2026-09-26 — LOT-003 ready for closure

Actor: Olivier and Codex
Operation: Olivier confirmed all remaining Windows walkthrough cases. Codex recorded G-003-002 HUMAN PASS, reviewed all six active requirements and four terminal findings, prepared the proposed TOTAL convergence, and moved LOT-003 to Ready-to-close.
Affected artifacts: `Specs/003-safe-accept/GATES-003.md`, `Specs/003-safe-accept/FINDINGS-003.md`, `Specs/003-safe-accept/CONVERGENCE-003.md`, `STATUS.md`, `HISTORY.md`.
Outcome: G-003-001 and G-003-002 PASS on V0.2 EXE SHA-256 `55D5C9C4E6C859204991AC20D2587A0CA6C3A0866765AD063FE742D8AA58A52D`; no active requirement has an unresolved deviation and no finding remains OPEN. LOT-003 awaits Olivier's explicit acceptance or refusal of the proposed TOTAL convergence and closure.

## 2026-09-26 — LOT-003 closed with total convergence

Actor: Olivier and Codex
Operation: Olivier explicitly accepted the proposed TOTAL convergence and closure of LOT-003.
Affected artifacts: `Specs/003-safe-accept/CONVERGENCE-003.md`, `STATUS.md`, `HISTORY.md`.
Outcome: LOT-003 is Closed. The identified V0.2 EXE has both active gates PASS, all six active requirements satisfied, and all four findings resolved locally. LOT-004 remains Planned and requires a separate human start decision.

## 2026-09-26 — LOT-004 started

Operation: Olivier explicitly started LOT-004, V0.3 — MAF and local Ollama.

Outcome: `STATUS.md` records LOT-004 as In-progress. Implementation and gate evaluation are pending; no acceptance or closure is implied.

## 2026-09-26 — First LOT-004 implementation candidate

Operation: Added the non-streaming Rewrite path through `MafTextTransformationService`, Microsoft Agent Framework's `ChatClientAgent`, the `IChatClient` boundary, and an `OllamaSharp.OllamaApiClient` factory. The app reads the configured local endpoint, model, temperature, and timeout; it displays explicit no-model, timeout, and provider-failure messages. A visible indeterminate local-model progress bar is present while the call is pending and result actions stay disabled. Added five deterministic fake-client tests for request/response, cancellation, provider error, empty output, and timeout.

Outcome: G-004-001 is PASS for the working-tree candidate: the solution builds without warnings or errors and all 19 discovered AI and Windows tests pass. G-004-002 remains TO TEST because it requires Olivier's live local-Ollama walkthrough, including stopped-provider behavior. LOT-004 remains In-progress.

## 2026-09-26 — Local Ollama settings and action-profile direction

Operation: Replaced the placeholder Settings page with a local Ollama configuration page. It discovers installed models through OllamaSharp, stores the selected model, endpoint, temperature, context size, and timeout in the existing local profile, and validates the Strict Local loopback boundary. The transformation path passes the configured context size as Ollama's `num_ctx` option. Olivier also decided that later declarative actions select a model profile and that V1 includes an explicit OpenAI-compatible remote provider.

Outcome: The current LOT-004 candidate builds with 0 warnings and 0 errors; the five AI fake-client tests and fourteen Windows tests pass. G-004-001 remains PASS after reevaluation. D-009/R-019 refine the planned LOT-005, LOT-006, and LOT-009 work without changing LOT-004's local-only scope. G-004-002 remains TO TEST.

## 2026-09-26 — Deployment modes clarified

Operation: Olivier confirmed the replacement of “Strict Local” by This device only, On-premises, and External. Codex recorded D-010, revised R-005 and future-lot requirements, and aligned the current local Settings terminology with This device only.

Outcome: The active LOT-004 candidate retains its loopback-only behavior, now accurately labelled This device only. On-premises and External implementation is deliberately deferred to the planned profiles, privacy, settings, and remote-provider lots. No human gate result is invalidated.

## 2026-09-26 — LOT-004 published for local validation

Operation: Ran the root `publish.ps1` to create the current V0.3 candidate at the canonical single-file path.

Outcome: `src/TextAid.App/bin/Publish/` contains only `TextAid.exe`, SHA-256 `53F6B2A89464EE4979BB39EEA5A62E4E52B5A4299409633A380A5CCA74B3F383`. The separate-machine check is recorded as deferred; Olivier will perform the local Ollama human walkthrough next.

## 2026-09-26 — Concurrent provider configuration decided

Operation: Olivier decided that This device only, On-premises, and External connections are retained concurrently in separate Settings tabs. Codex recorded D-011 and aligned the privacy, profiles, and Settings specifications.

Outcome: The selected model profile remains the sole action-specific route to a provider. The new V0.3 candidate is published at the canonical path; provider-tab implementation remains assigned to LOT-006 and LOT-008.

## 2026-09-26 — Unified editable action model decided

Operation: Olivier decided that TextAid-supplied actions and user-created actions share one editable declarative schema. Codex recorded D-012 and refined the actions, profiles, and Settings requirements.

Outcome: LOT-005 will eliminate special built-in execution paths; LOT-006 resolves an action's profile and temperature override; LOT-008 supplies the management UI for all actions. LOT-004 remains focused on its first Rewrite path.

## 2026-09-26 — Action output-language default decided

Operation: Olivier decided that actions carry an Unchanged or BCP-47 output-language default, while the main session always permits a per-invocation language override. Codex recorded D-013 and refined the affected action, profile, and Settings requirements.

Outcome: The action definition supplies the initial generation language; the later main-session combo overrides it without mutating saved action data. LOT-004 remains unchanged.

## 2026-09-26 — LOT-004 local walkthrough substantially validated

Actor: Olivier and Codex
Operation: Olivier validated the configured local transformation, its displayed result, Copy, Replace, and the graceful stopped-Ollama path on the published V0.3 candidate. Olivier deferred the separate no-Ollama-machine launch check. He also reported mismatched preview typography, non-exclusive Settings/About dialogs, and a likely ineffective thinking switch.
Affected artifacts: `Specs/004-ollama-maf/GATES-004.md`, `PORTABILITY-CHECKS.md`.
Outcome: The prior V0.3 EXE SHA-256 `871F31EE5D3B8782BE1127FB1B2DF3B4462F6A9EBE3D49DF7D56A04464A06C33` has a successful core local walkthrough. A follow-up candidate corrects the reported UI and thinking issues; it awaits publication and a focused human retest.

## 2026-09-26 — LOT-004 typography, modality, and thinking correction published

Actor: Codex
Operation: Set an explicit identical 14-point text size for captured and generated text, made Settings, About, and the missing-provider notice mutually exclusive modal dialogs, and mapped the stored thinking choice to MAF's `ReasoningOptions` (`Off` maps to `None`). Added fake-client assertions for Off and High reasoning choices.
Affected artifacts: `src/TextAid.AI/MafTextTransformationService.cs`, `src/TextAid.App/App.xaml.cs`, `src/TextAid.App/MainWindow.xaml`, `src/TextAid.App/StartupNoticeWindow.xaml.cs`, `tests/TextAid.AI.Tests/MafTextTransformationServiceTests.cs`, `Specs/004-ollama-maf/GATES-004.md`, `PORTABILITY-CHECKS.md`.
Outcome: The solution built with 0 warnings and 0 errors; 6 AI fake-client tests and 14 Platform.Windows tests passed. `publish.ps1` produced the sole 178,355,119-byte V0.3 EXE at `src/TextAid.App/bin/Publish/TextAid.exe`, SHA-256 `81720DC9AF212B625B45052674A3D53A5503C0CB5C00B696D1B2DDA213820FFC`. Focused human retest remains required for this distinct binary; the clean no-Ollama-machine portability case remains deferred.

## 2026-09-26 — LOT-004 thinking correction validated

Actor: Olivier
Operation: Olivier validated the typography and modal-dialog corrections and exercised a local transformation with thinking Off.
Affected artifacts: `Specs/004-ollama-maf/GATES-004.md`, `HISTORY.md`.
Outcome: Olivier reports an instantaneous response with thinking Off, providing live confirmation that the setting is now reaching the configured Ollama model. The no-Ollama-machine startup check remains deferred.

## 2026-09-26 — LOT-004 tray Open fallback published

Actor: Codex
Operation: Added Open to the tray menu. It starts a transformation from clipboard text without requiring Ctrl+C+C, while deliberately disabling Replace because no trustworthy source window exists; Copy stays available.
Affected artifacts: `src/TextAid.App/App.xaml.cs`, `src/TextAid.App/MainViewModel.cs`, `src/TextAid.App/Strings.xaml`, `Specs/004-ollama-maf/GATES-004.md`, `PORTABILITY-CHECKS.md`.
Outcome: The solution built with 0 warnings and 0 errors; 6 AI fake-client tests and 14 Platform.Windows tests passed. `publish.ps1` produced the sole 178,355,119-byte V0.3 EXE at `src/TextAid.App/bin/Publish/TextAid.exe`, SHA-256 `EB801C88B5BFCB930E0920B8167FE04117462A545F1D47E6E4AE86F14F9F8DBF`.

## 2026-09-26 — Per-action supplementary instructions decided

Operation: Olivier decided that declarative actions may require supplementary user instructions, while every normal session also exposes an optional Instructions control. Text remains editable, the user selects an action and optional instructions, then starts Process explicitly.
Affected artifacts: `LEDGER.md`, `RULES.md`, `PROJECT.md`, `Specs/005-declarative-actions/SPEC-005.md`, `Specs/008-settings-localization/SPEC-008.md`.
Outcome: D-014 and R-021 assign action data/runtime semantics to LOT-005 and Actions editor/session controls to LOT-008. LOT-004 remains the interim Rewrite-only implementation.

## 2026-09-26 — LOT-004 manual-entry candidate published

Actor: Codex
Operation: Revised tray Open and double-copy-without-selection to open the same focused, editable empty session rather than consume prior clipboard text. Added the Process control; its request snapshots the current editable text.
Affected artifacts: `src/TextAid.App/App.xaml.cs`, `src/TextAid.App/MainViewModel.cs`, `src/TextAid.App/MainWindow.xaml`, `src/TextAid.App/MainWindow.xaml.cs`, `src/TextAid.App/Strings.xaml`, `src/TextAid.Core/InvocationSession.cs`, `src/TextAid.Platform.Windows/KeyboardHook.cs`, `Specs/004-ollama-maf/GATES-004.md`, `PORTABILITY-CHECKS.md`.
Outcome: The solution built with 0 warnings and 0 errors; 6 AI fake-client tests and 14 Platform.Windows tests passed. `publish.ps1` produced the sole 178,355,631-byte V0.3 EXE at `src/TextAid.App/bin/Publish/TextAid.exe`, SHA-256 `7D71903867D39B77A0E94A94EF3C8A98FEE3C154840F24139F859CB08DE0AA3E`.

## 2026-09-26 — LOT-004 explicit Process flow published

Actor: Codex
Operation: Aligned valid-selection entry with the manual-entry path: every session now waits for the user to review or edit text and choose Process before a model request. This leaves a single session flow ready for upcoming action selection and supplementary-instructions controls.
Affected artifacts: `src/TextAid.App/App.xaml.cs`, `src/TextAid.App/Strings.xaml`, `Specs/004-ollama-maf/GATES-004.md`, `PORTABILITY-CHECKS.md`.
Outcome: The solution built with 0 warnings and 0 errors; 6 AI fake-client tests and 14 Platform.Windows tests passed. `publish.ps1` produced the sole 178,355,631-byte V0.3 EXE at `src/TextAid.App/bin/Publish/TextAid.exe`, SHA-256 `2280762B84063F78B89AAED76CB1E9D3752EB237C2B8D26300CF4A40F3295A72`.

## 2026-09-26 — Quick Translate immediacy reaffirmed

Operation: Olivier clarified that the explicit editable Process flow applies to normal `Ctrl+C+C` actions only. `Ctrl+C+T` must retain its immediate Translate behavior, with later optional corrections and destination-language changes.
Affected artifacts: `LEDGER.md`, `RULES.md`, `Specs/008-settings-localization/SPEC-008.md`, `HISTORY.md`.
Outcome: D-014/R-021 are constrained by the existing R-018 quick-translation rule; no LOT-004 code or published executable changes.

## 2026-09-27 — LOT-004 closed with total convergence

Actor: Olivier and Codex
Operation: Olivier explicitly validated and accepted LOT-004.
Affected artifacts: `Specs/004-ollama-maf/GATES-004.md`, `Specs/004-ollama-maf/CONVERGENCE-004.md`, `STATUS.md`, `HISTORY.md`.
Outcome: The V0.3 EXE SHA-256 `2280762B84063F78B89AAED76CB1E9D3752EB237C2B8D26300CF4A40F3295A72` has both active gates PASS. No LOT-004 finding was recorded. LOT-004 is Closed with TOTAL convergence; the separate clean-machine check remains deferred under R-012.

## 2026-09-26 — LOT-003 Replace retest passed in common applications

Actor: Olivier and Codex
Operation: Olivier retested the final corrected V0.2 EXE in Notepad, Visual Studio, Chrome, and Edge.
Affected artifacts: `Specs/003-safe-accept/GATES-003.md`, `Specs/003-safe-accept/FINDINGS-003.md`, `HISTORY.md`.
Outcome: Replace succeeds in all four reported applications, confirming the corrected `SendInput` path. G-003-002 remains TO TEST for the unreported TestTarget, Copy, controlled-failure, and layout/foreground conditions.

## 2026-09-27 — Declarative action storage and editing refined

Actor: Olivier and Codex
Operation: Olivier clarified that action JSON is visible unversioned user data beside the executable, that generic template variables are not useful in V0.4, and that editing belongs in a dedicated Actions page accessible from application menus. Codex recorded D-015, superseded R-019 with R-022, added R-023, and refined planned LOT-005, LOT-006, and LOT-008 requirements and LOT-005 gates.
Affected artifacts: `LEDGER.md`, `RULES.md`, `PROJECT.md`, `STATUS.md`, `Specs/005-declarative-actions/`, `Specs/006-connections-profiles/SPEC-006.md`, `Specs/008-settings-localization/SPEC-008.md`.
Outcome: No lot state, implementation, executable, or gate evaluation changed. LOT-005 remains Planned and awaits a separate human start decision.

## 2026-09-27 — Immediate translation action reserved

Actor: Olivier and Codex
Operation: Olivier clarified that the direct Translate shortcut must retain fixed behavior. Codex recorded D-016 and R-024, making the built-in declarative Translate action protected while retaining the shared action loading and execution path.
Affected artifacts: `LEDGER.md`, `RULES.md`, `Specs/005-declarative-actions/SPEC-005.md`, `Specs/008-settings-localization/SPEC-008.md`.
Outcome: Other built-in and user actions remain editable. No implementation, lot state, executable, or gate evaluation changed.

## 2026-09-27 — LOT-005 started

Actor: Olivier and Codex
Operation: Olivier explicitly authorized implementation of LOT-005 after the LOT-004 closure and the recorded action-model refinements.
Affected artifacts: `STATUS.md`, `HISTORY.md`.
Outcome: LOT-005 moved from Planned to In-progress. Its active work is the visible unversioned action store, declarative action execution, and normal-session action choice; no gate result is yet claimed.

## 2026-09-27 — Built-in email reply action added to LOT-005

Actor: Olivier and Codex
Operation: Olivier requested a built-in Answer this mail action that requires supplementary user context. Codex recorded D-017 and updated the active LOT-005 specification and gate.
Affected artifacts: `LEDGER.md`, `Specs/005-declarative-actions/SPEC-005.md`, `Specs/005-declarative-actions/GATES-005.md`, `HISTORY.md`.
Outcome: The active lot now supplies nine built-in actions. No gate is evaluated by this planning update.

## 2026-09-27 — LOT-005 declarative-actions candidate published

Actor: Codex
Operation: Implemented the unversioned action loader, validator, text-only template renderer, nine built-in actions, action selector, per-invocation Instructions dialog, action request snapshot, and protected declarative Translate action. Built and tested the solution, then ran the canonical publisher.
Affected artifacts: `src/TextAid.Core/`, `src/TextAid.App/`, `tests/TextAid.Core.Tests/`, `Specs/005-declarative-actions/GATES-005.md`, `STATUS.md`, `src/TextAid.App/bin/Publish/TextAid.exe`.
Outcome: G-005-001 is PASS. The only published V0.4 executable is 178,376,111 bytes, SHA-256 `461B21E646B03E9B5D3ABFE5900819E095BEC86B6D8FD28AD7053C77478B8F8A`. G-005-002 remains TO TEST pending Olivier's running-app extensibility walkthrough.

## 2026-09-27 — Single-EXE action initialization restored

Actor: Olivier and Codex
Operation: Olivier chose a single EXE with embedded default actions instead of publishing action files. He further chose a silent persisted fallback beside the settings file when the executable directory cannot accept action storage. Codex recorded D-019/R-026, embedded the action JSON files, and aligned the future Actions-page folder command.
Affected artifacts: `LEDGER.md`, `RULES.md`, `PROJECT.md`, `publish.ps1`, `src/TextAid.Core/`, `src/TextAid.App/`, `Specs/008-settings-localization/SPEC-008.md`, `Specs/010-hardening/SPEC-010.md`, `Specs/011-v1-release/`.
Outcome: The earlier direct-deployment decision is superseded before any final V0.4 publication under it. A new V0.4 EXE must be built and published through `publish.ps1`.

## 2026-09-27 — Combo interaction and accent contrast corrected

Actor: Olivier and Codex
Operation: Olivier reported a light combo scrollbar, a click target limited to the arrow, and unreadable light-button text. Codex made the full combo surface toggle its popup, added dark scrollbar templates, and recorded D-020 while applying explicit black text to every shared accent button.
Affected artifacts: `LEDGER.md`, `HISTORY.md`, `src/TextAid.App/Themes/Colors.xaml`, `src/TextAid.App/Themes/Controls.xaml`.
Outcome: The shared visual system is corrected; the updated release must be republished for desktop validation.

## 2026-09-27 — New session reset added

Actor: Olivier and Codex
Operation: Olivier requested an in-window reset for a separate manual transformation. Codex recorded D-021 and added a New command that clears the session while removing the original replacement target.
Affected artifacts: `LEDGER.md`, `HISTORY.md`, `Specs/005-declarative-actions/SPEC-005.md`, `src/TextAid.Core/InvocationSession.cs`, `src/TextAid.App/`.
Outcome: A New session is safe for manual input and cannot Replace into the source captured by the preceding session.

## 2026-09-27 — LOT-005 closed with total convergence

Actor: Olivier and Codex
Operation: Olivier confirmed the completed action workflow and visual corrections, then authorized LOT-005 closure. Codex recorded G-005-002 PASS and finalized TOTAL convergence.
Affected artifacts: `Specs/005-declarative-actions/GATES-005.md`, `Specs/005-declarative-actions/CONVERGENCE-005.md`, `STATUS.md`, `HISTORY.md`.
Outcome: LOT-005 is Closed. Both active gates PASS; LOT-006 remains Planned.
