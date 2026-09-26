# Durable decisions

## D-001 — Canonical Release publish location and script

Date: 2026-09-26T03:23:39+02:00
Decided by: Olivier
Source: Olivier's instruction in the LOT-002 conversation
Related: R-008, R-011, LOT-002, REQ-002-007, REQ-002-008, G-002-001, G-002-004

Decision:
For LOT-002 and every future TextAid release, the final single-file executable MUST be placed in `E:\TextAid\src\TextAid.App\bin\Publish\TextAid.exe`. The project root MUST contain `publish.ps1`, which produces the self-contained single-file Release executable in that directory. The project must keep that directory as the canonical location for the final EXE.

Reason:
Olivier requested one stable location and a repeatable publication command for subsequent builds.

Consequences:
Add global rule R-011, LOT-002 requirement REQ-002-008, and AUTO gate G-002-004; retain the original requirements and gates. Update the project publish settings, script, walkthrough, findings, gate evidence, and recovery references. Reevaluate G-002-001 on the newly published result. G-002-002 remains valid because the keyboard detector and its tests are unchanged. G-002-003 has no prior human validation and remains TO TEST. LOT-001 is closed and its visual baseline is unaffected. Planned LOT-003 through LOT-011 will follow R-011 when they publish; no current gate result exists in those lots to invalidate.

Replaces: None
Replaced by: None

## D-002 — Separate-machine verification is deferred when unavailable

Date: 2026-09-26T04:32:06+02:00
Decided by: Olivier
Source: Olivier's instruction in the LOT-002 conversation
Related: R-008, R-012, LOT-002, REQ-002-007, G-002-003, LOT-003 through LOT-011

Decision:
For each final TextAid executable, verification on a separate clean Windows x64 machine should be performed when such a machine is available. Its temporary unavailability MUST NOT by itself block closing a lot. An unavailable check MUST be recorded as deferred, not cancelled, and revisited when a second machine becomes available, including during later releases. The actual outcome must be recorded against the executable tested. A later executable may satisfy the recurring verification, but its result must not be presented as a test of an earlier binary.

Reason:
Olivier currently has no second Windows x64 machine running and considers the check valuable but not always feasible for every lot.

Consequences:
Add R-012 and a durable portability-check register. Clarify G-002-003 and its walkthrough so separate-machine testing is tracked outside the blocking HUMAN condition, while REQ-002-007 and R-008 still require a self-contained single-file executable. Preserve the original gate condition and method in its definition history. The current G-002-003 remains TO TEST for its other human checks; G-002-001, G-002-002, and G-002-004 remain PASS because the binary and their conditions are unchanged. Apply R-012 to planned LOT-003 through LOT-011. No source code or published executable changes.

Replaces: None
Replaced by: None

## D-003 — Keep invocation without selected text

Date: 2026-09-26T04:51:52+02:00
Decided by: Olivier
Source: Olivier's validation in the LOT-002 conversation
Related: R-013, LOT-002, REQ-002-004, REQ-002-005, G-002-003, LOT-003 through LOT-011

Decision:
Ctrl+C+C MUST still open the TextAid session when no text is selected. This observed behavior is accepted and must be preserved in subsequent versions; lack of a selection is not a reason to suppress invocation. A later version may use the open session for manually entered text to transform or translate. This decision does not require manual text entry in V0.1, whose captured-text view remains read-only.

Reason:
Olivier wants TextAid to remain callable without first selecting source text, enabling a future direct-entry workflow.

Consequences:
Add R-013 and an explicit no-selection case to the LOT-002 walkthrough and human validation record. Apply R-013 to planned LOT-003 through LOT-011. REQ-002-004 and REQ-002-005 retain their current meanings; the existing V0.1 session already opens with empty captured text. No code or published EXE changes. Previous AUTO gate results remain applicable; G-002-003 remains TO TEST for window placement and instance behavior not yet accepted by Olivier on the current candidate.

Replaces: None
Replaced by: None

## D-004 — Immediate translation on Ctrl+C+T

Date: 2026-09-26T05:12:02+02:00
Decided by: Olivier
Source: Olivier's quick-translation instruction and follow-up answers after LOT-002 closure
Related: R-001, R-004, R-006, R-014, LOT-005, LOT-006, LOT-008, LOT-010, LOT-011

Decision:
Keep the existing `Ctrl+C+C` action-choice flow. Add `Ctrl+C+T` as a separate entry point that captures the source and starts Translate immediately, without asking the user to choose an action or destination first. If the detected source language is the configured user language, translate to the configured preferred translation language. If it is the preferred translation language, translate to the user language. If it is neither, or detection is uncertain, use the user language as the destination. The user may choose a different destination in the open session, which starts another translation of the original captured text. The safe replacement action becomes available after the current translation succeeds, under the existing `Accept` label. These preferences are distinct, valid BCP-47 languages in Settings.

Reason:
Olivier wants a direct translation gesture while retaining the flexible `Ctrl+C+C` workflow and the established safe source replacement behavior.

Consequences:
Add R-014. LOT-005 prepares the Translate action for explicit destination input; LOT-006 stores and validates the two language preferences; LOT-008 activates the shortcut, automatic direction, Settings controls, destination changes, and preview state; LOT-010 covers regression and compatibility; LOT-011 adds V1 acceptance. Each destination change supersedes any pending translation and only the latest successful result can be accepted. No existing Closed lot, V0.1 result, or published executable changes. LOT-003 and LOT-004 keep their planned milestone scope and inherit R-014 only as a future behavior constraint. No current gate result is invalidated because the affected later lots have not started.

Replaces: None
Replaced by: D-006 for button label and result outcomes only; language routing remains active.

## D-005 — User-reassignable invocation shortcuts

Date: 2026-09-26T05:14:00+02:00
Decided by: Olivier
Source: Olivier's follow-up shortcut instruction after LOT-002 closure
Related: R-013, R-014, R-015, R-016, LOT-003 through LOT-011

Decision:
The normal-action and quick-translation shortcuts default to `Ctrl+C+C` and `Ctrl+C+T`; each sequence can be reassigned by the user in Settings to avoid conflicts with other applications. The complete gesture may be changed, including its `Ctrl+C` prefix. The two bindings must remain distinguishable and valid. The normal-action and quick-translation behavior follows the assigned gesture.

Reason:
Olivier expects shortcut conflicts with other applications and wants the user to resolve them without changing TextAid code.

Consequences:
Add R-015 and replace R-013 with R-016 for planned lots while preserving R-013's original text and its historical applicability to closed LOT-002. R-014 uses the configured quick-translation binding from LOT-008. LOT-003 through LOT-005 need no immediate shortcut implementation change; LOT-006 persists/validates the assignments; LOT-008 exposes Settings and switches the keyboard recognizer safely; LOT-010 and LOT-011 verify defaults, reassignment, conflicts, and persistence. Closed LOT-002 remains unchanged: its V0.1 shortcut is `Ctrl+C+C`, and no user reassignment is required for that closed candidate. No current gate result is invalidated; affected future lots remain Planned.

Replaces: None
Replaced by: None

## D-006 — Separate Replace and Copy outcomes

Date: 2026-09-26T05:20:00+02:00
Decided by: Olivier
Source: Olivier's clarification after the quick-translation specification, including the Copy-window answer
Related: D-004, R-006, R-014, R-017, R-018, LOT-003 through LOT-011

Decision:
The action previously labelled `Accept` is labelled `Replace` because it replaces the selected original text. A separate `Copy` action places the completed transformation or translation in the clipboard without pasting it into the source, allowing the user to paste elsewhere. `Copy` closes the TextAid session after successful clipboard copy. `Cancel` remains available and leaves the source unchanged. `Replace` and `Copy` become available only when the current result is complete and usable. This supersedes D-004's earlier button-label clarification but retains its quick-translation and language-routing decisions.

Reason:
`Accept` does not tell the user whether they are accepting the generated result or authorizing replacement of the original selection. Olivier wants the two intentions to be explicit.

Consequences:
Preserve and mark R-006 and R-014 obsolete; replace them with R-017 and R-018 for planned lots. R-009's historical phrase "capture/Accept pipeline" refers to the same safety work, now presented as Replace/Copy. LOT-003 introduces the distinct safe outcomes before AI; later transformation lots use them, and LOT-008 applies them to quick translation. LOT-010 and LOT-011 verify both operations, including unchanged source text after Copy. The closed LOT-002 V0.1 candidate retains its existing UI and evidence. No current gate result is invalidated; future lots remain Planned.

Replaces: D-004 for button label and result outcomes only
Replaced by: None

## D-007 — Shortcut mnemonic names

Date: 2026-09-26T05:22:34+02:00
Decided by: Olivier
Source: Olivier's clarification of the default shortcut mnemonics after D-005, including the spelling confirmation
Related: D-005, R-015, LOT-008, LOT-011

Decision:
Present the default `Ctrl+C+C` sequence as `Ctrl+C` followed by `C` for **Choose**, and `Ctrl+C+T` as `Ctrl+C` followed by `T` for **Translate**. These names describe the two functions and their default gestures; user-reassigned gestures remain permitted under D-005.

Reason:
The second key communicates which path will open and helps users remember the defaults.

Consequences:
Use Choose and Translate consistently in Settings shortcut labels, help/documentation, and V1 acceptance. The existing default sequences and behavior do not change. No code, published executable, closed lot, or gate result changes.

Replaces: None
Replaced by: None

## D-008 — Resizable split result review

Date: 2026-09-26T19:42:00+02:00
Decided by: Olivier
Source: Olivier's LOT-003 visual-review instruction
Related: LOT-003, REQ-003-006, G-003-002

Decision:
The LOT-003 session MUST present the captured text and transformed text in a 50/50 side-by-side layout. The window MUST be resizable, and the two panels MUST continue to divide the available width equally as it is resized. The status message MUST occupy its own non-overlapping row.

Reason:
The vertical layout mixes the preview and status content and does not provide enough usable space to compare source and transformed text.

Consequences:
Add REQ-003-006 and refine the active HUMAN walkthrough gate to include readable, elastic 50/50 review. Preserve the prior G-003-002 definition in its history. This decision applies only to the active LOT-003 result; no closed result or global rule changes.

Replaces: None
Replaced by: None
