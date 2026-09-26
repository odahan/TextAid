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
