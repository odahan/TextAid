# LOT-008 — V0.7 — Settings and localization

> Before working on this lot, read the repository root README.md.

Parent: None
Created: 2026-09-25T23:53:23

## Objective

Make V1 configuration accessible in the UI and localize from an English source catalog.

## Context

Source milestone: `docs/TextAid specification.md`, section 79; source-to-lot mapping: `docs/SOURCE-MAP.md`. This lot is part of the ordered V1 plan.

## In scope

Depends on LOT-007.

## Out of scope

Post-V1 capabilities in source section 91, except where a narrower exclusion is stated above. Do not add features beyond this lot's requirements.

## Requirements

### REQ-008-001 — Settings sections

Provide dark-themed General, AI Connections, Models/Profiles, Actions, Language, and Debug sections. AI Connections contains three independent tabs: This device only, On-premises, and External. Include enable/disable, double-copy delay, endpoint/authentication/test, model settings, action list/open folder/reload, language selection, and debug log controls. The tabs may all be configured simultaneously; an action profile selects the active connection for that action.

Source: `docs/TextAid specification.md`, sections 61–68, 79.

### REQ-008-002 — English source resources

Move every visible string in existing Views to an English source catalog; Views MUST reference resource keys rather than literal English text.

Source: `docs/TextAid specification.md`, sections 49, 79.

### REQ-008-003 — Language catalog and translation

Provide BCP-47 language entries, English/native names, and availability; reuse relevant languages for Translate. Generate non-English UI values through the current MAF provider, cache them locally, and allow user action or selection to initiate generation with progress.

Source: `docs/TextAid specification.md`, sections 50–54, 79.

### REQ-008-004 — Catalog integrity and fallback

Validate translated JSON has exactly the source keys and preserved placeholders. On invalid catalog or provider failure, keep/revert to English without preventing the application from working. User-defined action names need not be translated.

Source: `docs/TextAid specification.md`, sections 52–54, 79, 87.

### REQ-008-005 — Reload behavior

Provide explicit Reload configuration and Reload actions commands; validate completely and replace active state atomically, retaining the previous valid state on failure. A FileSystemWatcher is not required.

Source: `docs/TextAid specification.md`, sections 69, 79.

### REQ-008-006 — Immediate translation shortcut

The quick-translation binding, default `Ctrl+C+T`, MUST capture the original text and open one session with Translate selected and running immediately. It MUST NOT ask for an action or destination before starting. Determine the destination from the detected source language and the two Settings preferences: user language to preferred translation language; preferred translation language to user language; any other or uncertain detection to user language. Keep the normal-action binding, default `Ctrl+C+C`, on its existing action-choice path. Use the existing MAF/IChatClient transformation path and safe Replace/Copy transaction.

Source: D-004, D-006, R-017, R-018; Olivier's quick-translation instruction.

### REQ-008-007 — Translation destination and replacement state

Show the selected destination in the quick-translation session and allow the user to choose another available language. A destination change MUST start a new translation of the original captured text, cancel or supersede pending work, and prevent stale output from replacing or copying the newer result. `Replace` and `Copy` MUST be unavailable while translation is pending, after a destination change, or after failure; enable them only for the current successful translation. `Copy` leaves the source unchanged and closes the session. `Cancel` and window close retain their existing behavior.

Source: D-004, D-006, R-017, R-018; Olivier's quick-translation and result-action decisions.

### REQ-008-008 — Language and shortcut Settings

Expose the user language and preferred translation language as distinct, valid BCP-47 choices in Settings. Label the two shortcut controls Choose and Translate, explaining the defaults as `Ctrl+C`, then `C` for Choose, and `Ctrl+C`, then `T` for Translate. Let the user reassign either complete sequence, including its prefix, when it conflicts with another application. Apply valid changes to the keyboard recognizer safely and persist them. Reject unsupported or internally conflicting bindings with a clear explanation while retaining the active assignments. Settings MUST not misrepresent a shortcut as guaranteed to override a binding consumed by another application.

Source: D-004 through D-007, R-015, R-018; Olivier's language-routing and shortcut instructions.

### REQ-008-009 — Actions management

Provide an Actions Settings page that lists every action, including TextAid-supplied actions, and permits creation and editing. The editor exposes name, model profile, temperature, prompt template, and output-language default (Unchanged or a supported language). Profile choices visibly identify their This device only, On-premises, or External connection category. Validation errors must retain the last valid action configuration.

Source: D-012, R-019.

### REQ-008-010 — Main-session output-language override

The main TextAid session MUST display a combo of available output languages for an action invocation. It initializes from the action default, but a user-selected language overrides that value for the current generation only. A changed language starts a new one-shot transformation from the original captured text and prevents an older result from being copied or replaced.

Source: D-013, R-020.

### REQ-008-011 — User-instructions editor and session control

The Actions editor MUST expose an English checkbox labelled **Ask for user instructions** bound to `askForUserInstructions`. The normal Choose session MUST include an **Instructions** control that opens the supplementary-instructions dialog for the currently selected action. If an action requires instructions and none were supplied through that control, Process MUST open the same dialog before that model request. The dialog is modal, themed, editable, and its value applies only to the current invocation. This control and requirement MUST NOT delay the immediate `Ctrl+C+T` quick Translate invocation.

Source: D-014, R-021.

## Important cases

Validate success and failure paths described in the active requirements; use the source specification for examples and exact user-facing messages where given.

## Dependencies

LOT-007 must be closed before this lot starts. Active R-001 through R-005, R-007 through R-012, and R-015 through R-018 apply according to their stated scopes; obsolete R-006, R-013, and R-014 remain part of closed or superseded history.

## Known constraints

The source document is retained for traceability. If it conflicts with an active Pro-Spec requirement or rule, record a finding and obtain the required human decision before changing active intent.
