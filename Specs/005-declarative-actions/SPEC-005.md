# LOT-005 — V0.4 — Declarative actions

> Before working on this lot, read the repository root README.md.

Parent: None
Created: 2026-09-25T23:53:23

## Objective

Move transformations out of C# and make the V1 action set configurable.

## Context

Source milestone: `docs/TextAid specification.md`, section 76; source-to-lot mapping: `docs/SOURCE-MAP.md`. This lot is part of the ordered V1 plan.

## In scope

Depends on LOT-004.

## Out of scope

Post-V1 capabilities in source section 91, except where a narrower exclusion is stated above. Do not add features beyond this lot's requirements.

## Requirements

### REQ-005-001 — Action data

Implement ActionDefinition, ActionLoader, and ActionValidator using versioned JSON. Migrate Rewrite and supply Translate, Correct, Rewrite, Shorten, Expand, Simplify, Change tone, and Summarize as built-in action data. Built-in and user-created actions share the same editable format and no built-in action has a dedicated C# execution path.

Source: `docs/TextAid specification.md`, sections 34–36, 76.

### REQ-005-002 — No recompilation

Loading an added valid action MUST NOT require a new ViewModel, dedicated button, service, or application build. Actions can be enabled or disabled and use localizable names.

Source: `docs/TextAid specification.md`, sections 34, 76, 90.

### REQ-005-003 — Parameters and templates

Support only choice and text parameters. TemplateRenderer MUST resolve {{text}} and named parameters, reject missing/unknown variables, and never parse injected user text a second time.

Source: `docs/TextAid specification.md`, sections 36–37, 76, 84.

### REQ-005-004 — Untrusted input

Standard prompts MUST delimit selected text as input data and direct the model to ignore instructions within it, while recognizing this is not a complete prompt-injection defense.

Source: `docs/TextAid specification.md`, sections 38, 84.

### REQ-005-005 — Translation parameters for quick invocation

The built-in Translate action MUST accept an explicit destination language and preserve the original captured text as its input. Its action data and rendering path MUST support the later `Ctrl+C+T` entry point without adding a separate hard-coded translation implementation. Changing the destination in a session will mean a new single transformation of the original text, never translation of an earlier translated result. This lot does not yet activate `Ctrl+C+T`; the full shortcut and language-preference workflow belongs to LOT-008.

Source: D-004, D-006, R-018; Olivier's quick-translation instruction.

### REQ-005-006 — Action profile reference

Each ActionDefinition MUST declare a `profileId` that identifies the model profile to use. Built-in action data may select the local default profile; the remote profile implementation remains LOT-009 work. An unknown profile is invalid action data.

Source: D-009, R-019.

### REQ-005-007 — Editable action fields

Each ActionDefinition MUST include a user-visible name, a prompt template, a `profileId`, and an optional temperature override. The profile reference represents the selected This device only, On-premises, or External model profile; action data must not embed a provider endpoint or credential. The same schema applies to every built-in and user-created action.

Source: D-012, R-019.

### REQ-005-008 — Output-language default

Each ActionDefinition MUST define an output-language default as either **Unchanged** or a BCP-47 language identifier. This is data, not a separate translation action implementation, and it becomes the initial value of the later session language selector.

Source: D-013, R-020.

### REQ-005-009 — User-instructions flag

Each ActionDefinition MUST include an `askForUserInstructions` boolean. When true, TextAid MUST collect additional user instructions in a dialog before it begins that action's model call. Cancelling that dialog MUST leave the session unchanged and MUST NOT call a provider. The supplementary instruction is invocation-only data and MUST NOT modify the action definition.

Source: D-014, R-021.

### REQ-005-010 — Editable session and action choice

The normal TextAid session MUST keep its input text editable regardless of whether it originated from a selection, manual typing, or a paste. It MUST let the user select an available action, optionally enter supplementary instructions, and explicitly start one Process operation. The request must use a snapshot of the editable text, selected action, and supplementary instruction at the moment Process starts.

Source: D-014, R-021.

## Important cases

Validate success and failure paths described in the active requirements; use the source specification for examples and exact user-facing messages where given.

## Dependencies

LOT-004 must be closed before this lot starts. Active R-001 through R-005, R-007 through R-012, and R-015 through R-018 apply according to their stated scopes; obsolete R-006, R-013, and R-014 remain part of closed or superseded history.

## Known constraints

The source document is retained for traceability. If it conflicts with an active Pro-Spec requirement or rule, record a finding and obtain the required human decision before changing active intent.
