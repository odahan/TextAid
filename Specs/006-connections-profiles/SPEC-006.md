# LOT-006 — V0.5 — Connections and profiles

> Before working on this lot, read the repository root README.md.

Parent: None
Created: 2026-09-25T23:53:23

## Objective

Separate action choice from model configuration and provider connection.

## Context

Source milestone: `docs/TextAid specification.md`, section 77; source-to-lot mapping: `docs/SOURCE-MAP.md`. This lot is part of the ordered V1 plan.

## In scope

Depends on LOT-005.

## Out of scope

Post-V1 capabilities in source section 91, except where a narrower exclusion is stated above. Do not add features beyond this lot's requirements.

## Requirements

### REQ-006-001 — Typed configuration

Implement ConnectionDefinition, ModelProfile, AiClientFactory, and ProfileResolver. Resolve Action → ModelProfile → Connection → IChatClient → MAF; keep provider-specific options opaque to Core. Action `profileId` determines the selected ModelProfile.

Source: `docs/TextAid specification.md`, sections 39–43, 77.

### REQ-006-007 — Action-profile validation

Validate every action `profileId` and resolve it before an invocation starts. Preserve a visible local default profile, and do not silently substitute a different profile when the referenced profile is unavailable or invalid.

Source: D-009, D-015, R-022.

### REQ-006-008 — Concurrent connection categories

Persist independent This device only, On-premises, and External connection definitions concurrently. A ModelProfile chooses one connection; no global mode may replace the selected profile for unrelated actions.

Source: D-011, D-015, R-005, R-022.

### REQ-006-009 — Action generation overrides

Resolve an action's selected profile and optional temperature override for each invocation. Reject an action whose profile is unavailable or whose override is out of the supported range; do not silently substitute another profile or temperature.

Source: D-012, D-015, R-022.

### REQ-006-010 — Action-language validation

Validate each action output-language default against the supported language catalog, permitting the explicit **Unchanged** value. Invalid action-language data must not replace the last valid configuration.

Source: D-013, R-020.

### REQ-006-002 — Default configuration

Create schemaVersion 1 configuration on first launch under the user profile. Standard action files are separately created and maintained in the unversioned `actions` subdirectory beside the executable. Default to This device only, Debug off, Ollama endpoint http://127.0.0.1:11434, and no arbitrary preselected model. Ollama absence MUST NOT prevent startup.

Source: `docs/TextAid specification.md`, sections 43–45, 77.

### REQ-006-003 — Model discovery

Query locally available Ollama models through OllamaSharp and allow the user to select a model; no selected model MUST block invocation with an explicit message.

Source: `docs/TextAid specification.md`, sections 46, 64, 77.

### REQ-006-004 — Configuration validation

Validate duplicate IDs, unknown action/profile/connection/provider, malformed endpoints, and missing models. Configuration reload MUST validate a new configuration completely before replacing the active one. Action JSON has no schema-version field; action validation remains LOT-005 work.

Source: `docs/TextAid specification.md`, sections 39–46, 69, 85.

### REQ-006-005 — Translation language preferences

Persist and validate a user language and a preferred translation language in versioned configuration, using distinct BCP-47 identifiers from the application language catalog. These values MUST be available to the later `Ctrl+C+T` direction decision and editable in LOT-008 Settings. Configuration reload MUST retain the previous valid values if the new language preferences are invalid. This lot does not yet activate the quick-translation shortcut.

Source: D-004, D-006, R-018; Olivier's quick-translation instruction.

### REQ-006-006 — Shortcut assignments

Persist separate normal-action and quick-translation shortcut assignments, defaulting to `Ctrl+C+C` and `Ctrl+C+T`. Validate that both complete sequences are supported by the keyboard recognizer and do not conflict with each other. Invalid reload MUST preserve the previous valid assignments. LOT-008 exposes reassignment in Settings and activates the quick-translation binding.

Source: D-005, R-015; Olivier's shortcut-reassignment instruction.

## Important cases

Validate success and failure paths described in the active requirements; use the source specification for examples and exact user-facing messages where given.

## Dependencies

LOT-005 must be closed before this lot starts. Active R-001 through R-005, R-007 through R-012, R-015 through R-018, and R-020 through R-022 apply according to their stated scopes; obsolete R-006, R-013, R-014, and R-019 remain part of closed or superseded history.

## Known constraints

The source document is retained for traceability. If it conflicts with an active Pro-Spec requirement or rule, record a finding and obtain the required human decision before changing active intent.
