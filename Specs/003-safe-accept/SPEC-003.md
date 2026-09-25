# LOT-003 — V0.2 — Safe Accept pipeline

> Before working on this lot, read the repository root README.md.

Parent: None
Created: 2026-09-25T23:53:23

## Objective

Prove replacement in the source application without AI.

## Context

Source milestone: `docs/TextAid specification.md`, section 74; source-to-lot mapping: `docs/SOURCE-MAP.md`. This lot is part of the ordered V1 plan.

## In scope

Use a temporary uppercase transformation. MAF work waits for this lot's closure.

## Out of scope

Post-V1 capabilities in source section 91, except where a narrower exclusion is stated above. Do not add features beyond this lot's requirements.

## Requirements

### REQ-003-001 — Transaction outcomes

The session MUST end through Accept or Cancel only. X, Escape, and Alt+F4 mean Cancel. Cancel MUST leave the source unchanged and destroy the current session.

Source: `docs/TextAid specification.md`, sections 19–22, 74.

### REQ-003-002 — Captured target

Accept MUST use the source HWND captured at invocation, never a newly sampled foreground window. Verify IsWindow and confirmed restoration/focus before synthetic paste.

Source: `docs/TextAid specification.md`, sections 28–30, 74.

### REQ-003-003 — Clipboard and paste

Place the complete result in the clipboard, wait briefly for all Ctrl/Alt/Shift/Win keys to be released, and send Ctrl+V via SendInput only when safe. Never use SendKeys.SendWait.

Source: `docs/TextAid specification.md`, sections 29–32, 74.

### REQ-003-004 — Safe failure

If target validation, restoration, modifier release, or paste fails, do not paste into another window; retain the result in the clipboard and show an understandable error. Document elevated-application limitations.

Source: `docs/TextAid specification.md`, sections 30–33, 74.

### REQ-003-005 — Windows test target

Create the non-distributed WPF TextAid.TestTarget with single-line and multiline TextBox, RichTextBox, focus-change button, and event display for repeatable selection, focus, Unicode, and paste checks.

Source: `docs/TextAid specification.md`, sections 74, 82.

## Important cases

Validate success and failure paths described in the active requirements; use the source specification for examples and exact user-facing messages where given.

## Dependencies

LOT-002 must be closed before this lot starts. R-001 through R-010 apply throughout.

## Known constraints

The source document is retained for traceability. If it conflicts with an active Pro-Spec requirement or rule, record a finding and obtain the required human decision before changing active intent.
