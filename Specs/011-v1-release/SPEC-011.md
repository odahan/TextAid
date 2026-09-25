# LOT-011 — V1.0 — Release qualification

> Before working on this lot, read the repository root README.md.

Parent: None
Created: 2026-09-25T23:53:23

## Objective

Release the stabilized V0.9 candidate only when the complete V1 criteria are met.

## Context

Source milestone: `docs/TextAid specification.md`, section 89; source-to-lot mapping: `docs/SOURCE-MAP.md`. This lot is part of the ordered V1 plan.

## In scope

Depends on LOT-010. This lot adds documentation and release evidence, not a final feature wave.

## Out of scope

Post-V1 capabilities in source section 91, except where a narrower exclusion is stated above. Do not add features beyond this lot's requirements.

## Requirements

### REQ-011-001 — End-to-end function

Selection → Ctrl+C+C → one of eight actions → preview → Accept → correct replacement MUST work; Cancel MUST leave source unchanged.

Source: `docs/TextAid specification.md`, sections 89–90.

### REQ-011-002 — Interface and AI architecture

Verify complete dark non-minimizable transaction UI, dark chrome, correct centering, only Accept/Cancel outcomes, all model calls through MAF/IChatClient, OllamaSharp default, and declarative actions without rebuild.

Source: `docs/TextAid specification.md`, sections 89–90.

### REQ-011-003 — Provider, privacy, resilience

Verify optional remote provider, Strict Local loopback enforcement, no telemetry/log when Debug is off, no sensitive debug content when on, clean failure when Ollama is unavailable, and no paste on failed source restoration.

Source: `docs/TextAid specification.md`, sections 89–90.

### REQ-011-004 — Localization and distribution

Verify English without AI, translated-catalog failure fallback, documented product behavior, and standalone self-contained TextAid.exe as the official distribution.

Source: `docs/TextAid specification.md`, sections 89–90.

### REQ-011-005 — Scope integrity

Keep V1 inside the one-transformation product definition. Treat source section 92 ideas as uncommitted post-V1 possibilities.

Source: `docs/TextAid specification.md`, sections 91–96.

## Important cases

Validate success and failure paths described in the active requirements; use the source specification for examples and exact user-facing messages where given.

## Dependencies

LOT-010 must be closed before this lot starts. R-001 through R-010 apply throughout.

## Known constraints

The source document is retained for traceability. If it conflicts with an active Pro-Spec requirement or rule, record a finding and obtain the required human decision before changing active intent.
