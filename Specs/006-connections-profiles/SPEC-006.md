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

Implement ConnectionDefinition, ModelProfile, AiClientFactory, and ProfileResolver. Resolve Action → ModelProfile → Connection → IChatClient → MAF; keep provider-specific options opaque to Core.

Source: `docs/TextAid specification.md`, sections 39–43, 77.

### REQ-006-002 — Default configuration

Create schemaVersion 1 configuration and standard action files on first launch under the user profile. Default to Strict Local, Debug off, Ollama endpoint http://127.0.0.1:11434, and no arbitrary preselected model. Ollama absence MUST NOT prevent startup.

Source: `docs/TextAid specification.md`, sections 43–45, 77.

### REQ-006-003 — Model discovery

Query locally available Ollama models through OllamaSharp and allow the user to select a model; no selected model MUST block invocation with an explicit message.

Source: `docs/TextAid specification.md`, sections 46, 64, 77.

### REQ-006-004 — Configuration validation

Validate duplicate IDs, unknown action/profile/connection/schema/provider, malformed endpoints, and missing models. Reload MUST validate a new configuration completely before replacing the active one.

Source: `docs/TextAid specification.md`, sections 39–46, 69, 85.

## Important cases

Validate success and failure paths described in the active requirements; use the source specification for examples and exact user-facing messages where given.

## Dependencies

LOT-005 must be closed before this lot starts. R-001 through R-010 apply throughout.

## Known constraints

The source document is retained for traceability. If it conflicts with an active Pro-Spec requirement or rule, record a finding and obtain the required human decision before changing active intent.
