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

Provide dark-themed General, AI Connections, Models/Profiles, Actions, Language, and Debug sections. Include enable/disable, double-copy delay, Strict Local, endpoint/authentication/test, model settings, action list/open folder/reload, language selection, and debug log controls.

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

## Important cases

Validate success and failure paths described in the active requirements; use the source specification for examples and exact user-facing messages where given.

## Dependencies

LOT-007 must be closed before this lot starts. R-001 through R-010 apply throughout.

## Known constraints

The source document is retained for traceability. If it conflicts with an active Pro-Spec requirement or rule, record a finding and obtain the required human decision before changing active intent.
