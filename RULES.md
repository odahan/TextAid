# Project rules

These rules apply to all V1 lots unless a rule names a narrower phase. They originate in the retained `docs/TextAid specification.md` and are initial rules, so no ledger decision is required. Later changes follow Pro-Spec section 12.

## R-001 — Product identity and scope

Status: ACTIVE
Source: Initial project rule; source sections 1–3, 91, 94, 96
Rule: The product MUST be named TextAid everywhere, use the exact subtitle “Local-first text transformer & translator”, and remain a one-text/one-transformation utility. Post-V1 features listed in source section 91 MUST NOT enter V1 lots.
Replaces: None
Replaced by: None

## R-002 — Platform and architecture

Status: ACTIVE
Source: Initial project rule; source sections 7–11, 94
Rule: The product MUST target .NET 10, C#, WPF, and Windows x64. The target solution has `TextAid.App`, `TextAid.Core`, `TextAid.AI`, `TextAid.Platform.Windows`, the three matching test projects, and `TextAid.TestTarget`. Core MUST have no WPF, Win32, or OllamaSharp dependency; Windows integration MUST remain in Platform.Windows. The WPF UI MUST use MVVM with CommunityToolkit.Mvvm; code-behind is limited to view and Win32 concerns.
Replaces: None
Replaced by: None

## R-003 — Dependency discipline

Status: ACTIVE
Source: Initial project rule; source sections 8, 37, 56, 94
Rule: Dependencies MUST stay few. CommunityToolkit.Mvvm is the only MVVM framework; do not add a logging, localization, theming, configuration, mediator, event-bus, or prompt-template framework without demonstrated need. Do not create an internal framework for simple behavior.
Replaces: None
Replaced by: None

## R-004 — AI access boundary

Status: ACTIVE
Source: Initial project rule; source sections 4–6, 42, 47, 94
Rule: All application AI calls MUST pass through MAF and `Microsoft.Extensions.AI.IChatClient`. The default Ollama connection MUST use OllamaSharp, without a custom Ollama HTTP client. Functional code MUST NOT call providers directly or introduce a competing `ILlmProvider`. MAF MUST be used for a single transformation call, without agent tools, memory, or autonomous behavior.
Replaces: None
Replaced by: None

## R-005 — Local privacy boundary

Status: ACTIVE
Source: Initial project rule; source sections 4, 48, 55–59, 90
Rule: The initial configuration MUST enable Strict Local. In that mode only `localhost`, `127.0.0.1`, and `::1` are automatically local; private LAN addresses are remote. No remote AI call, cloud fallback, telemetry, or user-data egress is allowed. API keys MUST NOT be stored in clear text in `config.json`. With Debug off, no log or persistent trace is created; with Debug on, no secret, user text, result, or prompt containing user text is logged.
Replaces: None
Replaced by: None

## [OBSOLETE 2026-09-26] R-006 — Safe source replacement

Status: OBSOLETE
Source: Initial project rule; source sections 20–33, 90
Rule: A session MUST have only `Accept` or `Cancel`, with at most one active session. Cancel MUST leave the source unchanged. Before `SendInput(Ctrl+V)`, the captured source HWND MUST still be valid and confirmed restored, and modifier keys MUST be released. Failure MUST prevent paste to an arbitrary window and leave the generated result in the clipboard. Wrong-window paste is a release blocker.
Replaces: None
Replaced by: R-017
Decision: D-006

## R-007 — Visual and language foundation

Status: ACTIVE
Source: Initial project rule; source sections 12–16, 49–54, 72–73
Rule: Fix the palette, typography, layout dimensions, logo, and icon before visible V0.1 UI implementation. All windows, title bars, and controls MUST use a complete dark theme with centralized tokens and no hard-coded view colors. English is the source UI language; visible UI strings MUST use resources, and translated catalogs MUST fall back to English on failure.
Replaces: None
Replaced by: None

## R-008 — Single-file distribution

Status: ACTIVE
Source: Initial project rule; source sections 9, 73, 90
Rule: Starting at V0.1, Release publishing MUST produce a self-contained single `TextAid.exe` for win-x64 with trimming disabled; no adjacent configuration or runtime file may be required for distribution. User configuration is created in the user profile at first run.
Replaces: None
Replaced by: None

## R-009 — Incremental quality

Status: ACTIVE
Source: Initial project rule; source sections 72–90, 93–94
Rule: Follow the source milestone order from design gate through V1. Each V0.x MUST compile, remain testable, and preserve validated behavior. Validate the Windows capture/Accept pipeline before adding MAF. Unit AI tests MUST use a fake `IChatClient`; live Ollama integration tests are optional. For any `dotnet build` or `dotnet test`, explicitly pass `-m:1`.
Replaces: None
Replaced by: None

## R-010 — Code conventions

Status: ACTIVE
Source: Initial project rule; project-wide AGENTS.md instruction
Rule: Application code and UI MUST be in English, formatted for human maintenance. Code comments MUST be in English and documentation comments MUST use XML style. Use MVVM Toolkit by default.
Replaces: None
Replaced by: None

## R-011 — Canonical Release output

Status: ACTIVE
Source: D-001; Olivier's 2026-09-26 decision
Scope: LOT-002 and all later TextAid releases; no retroactive change to closed LOT-001.
Rule: Release publishing MUST place the final single-file `TextAid.exe` in `src/TextAid.App/bin/Publish/`. The root `publish.ps1` MUST produce that executable there. This location is the canonical final build output.
Replaces: None
Replaced by: None

## R-012 — Separate-machine portability check

Status: ACTIVE
Source: D-002; Olivier's 2026-09-26 decision
Scope: LOT-002 and every later TextAid release candidate; no retroactive change to closed LOT-001.
Rule: For each final published executable, plan a launch and About check using that EXE alone on a separate clean Windows x64 machine when available. Record the tested EXE identity and outcome. If no such machine is available, record the check as deferred in `PORTABILITY-CHECKS.md` and revisit it at the next opportunity, including later releases; do not erase the untested history of a superseded binary. Lack of this separate-machine check alone MUST NOT block lot closure. Do not claim empirical portability validation for an EXE that was not tested. R-008's single-file distribution requirement and applicable build/publish gates remain in force.
Replaces: None
Replaced by: None

## [OBSOLETE 2026-09-26] R-013 — Invocation without a selection

Status: OBSOLETE
Source: D-003; Olivier's 2026-09-26 decision
Scope: LOT-002 and later TextAid versions; no retroactive change to closed LOT-001.
Rule: Ctrl+C+C MUST open an invocation session even when no text is selected or available to capture. The absence of selected text MUST NOT suppress the invocation. A manual-text-entry workflow MAY be added in a later lot; R-013 does not require it in V0.1 or change the current captured-text field into an editor.
Replaces: None
Replaced by: R-016
Decision: D-005

## [OBSOLETE 2026-09-26] R-014 — Quick translation invocation

Status: OBSOLETE
Source: D-004; Olivier's 2026-09-26 decision
Scope: Planned LOT-005 through LOT-011; the `Ctrl+C+T` entry point becomes active in LOT-008.
Rule: The quick-translation shortcut (default `Ctrl+C+T`) MUST start Translate immediately without an action or destination question. Detect the language of the original captured text: user language → preferred translation language; preferred translation language → user language; other or uncertain language → user language. The user MAY choose another destination in the session; this starts a new translation of the original text and supersedes any pending result. `Accept` MUST remain unavailable until the current translation succeeds, then offer the existing safe replacement action. The normal-action shortcut (default `Ctrl+C+C`) MUST retain its action-choice behavior. The two configured language preferences MUST be valid and distinct.
Replaces: None
Replaced by: R-018
Decision: D-006

## R-015 — Configurable invocation shortcuts

Status: ACTIVE
Source: D-005; Olivier's 2026-09-26 decision
Scope: LOT-006 configuration and LOT-008 Settings onward; defaults do not change the closed V0.1 result.
Rule: The default normal-action and quick-translation shortcuts MUST be `Ctrl+C+C` and `Ctrl+C+T`, respectively. Settings MUST let the user reassign each full keyboard sequence independently when it conflicts with another application. Persist and validate the assignments; reject identical, invalid, or internally conflicting assignments without replacing active shortcuts. Invocation behavior MUST follow the configured assignments. The default sequences begin with `Ctrl+C`; a reassigned sequence need not keep that prefix.
Replaces: None
Replaced by: None

## R-016 — Normal-action invocation without a selection

Status: ACTIVE
Source: D-005; Olivier's 2026-09-26 decision
Scope: Planned LOT-003 through LOT-011; R-013 remains the historical rule for the closed LOT-002 result.
Rule: The normal-action shortcut (default `Ctrl+C+C`) MUST open an invocation session even when no text is selected or available to capture. The absence of selected text MUST NOT suppress the invocation. A manual-text-entry workflow MAY be added in a later lot; this rule does not require it in V0.1 or change the closed candidate's captured-text field into an editor. From LOT-008, the behavior follows the user's configured shortcut if they reassign the default.
Replaces: R-013
Replaced by: None

## R-017 — Explicit result outcomes and safe replacement

Status: ACTIVE
Source: D-006; Olivier's 2026-09-26 decision
Scope: Planned LOT-003 through LOT-011; R-006 remains the historical rule for closed LOT-002.
Rule: A session MUST offer `Replace`, `Copy`, and `Cancel` for a successful result, with at most one active session. `Replace` MUST copy the result to the clipboard and paste it only into the captured source HWND after confirming that window is valid and restored and all modifier keys are released. Failure MUST prevent paste to an arbitrary window and leave the result in the clipboard. `Copy` MUST copy the result to the clipboard without restoring/focusing the source or sending paste, then close the session. `Cancel` MUST leave the source unchanged. Window close, Escape, and Alt+F4 mean `Cancel`. Wrong-window paste is a release blocker.
Replaces: R-006
Replaced by: None

## R-018 — Quick translation invocation and result actions

Status: ACTIVE
Source: D-006; Olivier's 2026-09-26 decision, retaining D-004's language routing
Scope: Planned LOT-005 through LOT-011; the quick-translation entry point becomes active in LOT-008.
Rule: The quick-translation shortcut (default `Ctrl+C+T`) MUST start Translate immediately without an action or destination question. Detect the language of the original captured text: user language → preferred translation language; preferred translation language → user language; other or uncertain language → user language. The user MAY choose another destination in the session; this starts a new translation of the original text and supersedes any pending result. `Replace` and `Copy` MUST remain unavailable until the current translation succeeds, then perform their distinct R-017 actions. The normal-action shortcut (default `Ctrl+C+C`) MUST retain its action-choice behavior. The two configured language preferences MUST be valid and distinct.
Replaces: R-014
Replaced by: None
