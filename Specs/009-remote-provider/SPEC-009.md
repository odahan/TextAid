# LOT-009 — V0.8 — Optional remote provider

> Before working on this lot, read the repository root README.md.

Parent: None
Created: 2026-09-25T23:53:23

## Objective

Demonstrate an explicitly configured remote provider without weakening local defaults.

## Context

Source milestone: `docs/TextAid specification.md`, section 80; source-to-lot mapping: `docs/SOURCE-MAP.md`. This lot is part of the ordered V1 plan.

## In scope

Depends on LOT-008.

## Out of scope

Post-V1 capabilities in source section 91, except where a narrower exclusion is stated above. Do not add features beyond this lot's requirements.

## Requirements

### REQ-009-001 — Remote IChatClient

Integrate at least one remotely hosted provider through IChatClient and the same MAF path; no direct functional provider call or duplicate AI pipeline is allowed.

Source: `docs/TextAid specification.md`, sections 4–6, 47, 80.

### REQ-009-002 — Authentication

Support None and BearerFromEnvironment, resolving the named environment variable at use time. Do not store an API key in clear text in config.json; Windows Credential Manager is outside V1.

Source: `docs/TextAid specification.md`, sections 48, 80.

### REQ-009-003 — Strict Local conflict

Reject a remote endpoint while Strict Local is enabled; any use of a remote provider MUST require deliberate configuration and disabling Strict Local.

Source: `docs/TextAid specification.md`, sections 4, 47–48, 80.

## Important cases

Validate success and failure paths described in the active requirements; use the source specification for examples and exact user-facing messages where given.

## Dependencies

LOT-008 must be closed before this lot starts. Active R-001 through R-005, R-007 through R-012, and R-015 through R-018 apply according to their stated scopes; obsolete R-006, R-013, and R-014 remain part of closed or superseded history.

## Known constraints

The source document is retained for traceability. If it conflicts with an active Pro-Spec requirement or rule, record a finding and obtain the required human decision before changing active intent.
