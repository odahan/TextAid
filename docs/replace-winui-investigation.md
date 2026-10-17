# Replace compatibility investigation — MAUI/WinUI

Date: 2026-10-17

## Report and current state

Olivier reports that Replace closes TextAid without changing the selected text in smalBazar. Replace works in the WPF applications and other applications tested so far. The active process was identified as `SmallBazar.exe`, MSIX package `com.enaxos.smallbazar_1.6.13.0_x64__24t49ffww27ep`, installed under `C:\Program Files\WindowsApps\`. The application uses .NET MAUI and is understood to use WinUI on Windows.

A concrete focus defect has been reproduced in an isolated native Windows test and corrected. After publication and restart of TextAid 1.2.13, Olivier retested smalBazar and confirmed: "Le remplacement fonctionne maintenant". The reported application failure is resolved by this user validation. The Windows Computer Use runtime could not start: `failed to prepare MXC sandbox: permission path cannot be represented losslessly`, including after a kernel reset; no automated inspection of the actual smalBazar control was performed.

## Mechanism

Previously, `Win32ResultActions.RestoreAndFocus` called `ShowWindow(SW_RESTORE)`, activated the captured top-level source window, and then unconditionally called `SetFocus` on that top-level HWND. It verified only that the source window was foreground before sending Ctrl+V. A successful `SendInput` return was treated as a completed replacement; it only proves that Windows accepted the input events, not that a text control applied the paste.

WinUI distinguishes the top-level host from the child window receiving keyboard input for its XAML content. Activating a WinUI desktop window normally restores focus to its content island. Explicitly focusing the top-level host afterward can take keyboard focus away from that island. This explains a plausible route to accepted Ctrl+V events with no edit. WPF has different focus restoration behavior, so its reported success does not rule out this defect.

Microsoft's [WinUI focus design notes](https://github.com/microsoft/microsoft-ui-xaml/blob/main/docs/design-notes/focus.md) describe activation restoring the content-island focus and distinguish Win32 focus from XAML focus. The [SetFocus documentation](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setfocus) describes assigning keyboard focus to the specified HWND. The [SendInput documentation](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput) defines the return value as the number of inserted input events and documents UIPI restrictions, which remain a separate possible failure mode.

## Candidate 1.2.13

- Activate the captured source window without forcing focus onto its top-level host, allowing the application to restore its own editing focus.
- Call `ShowWindow(SW_RESTORE)` only when the source is minimized, preserving normal/maximized window state.
- Use `GetGUIThreadInfo` to verify that keyboard focus belongs to the source HWND or one of its descendants, in addition to foreground-window validation.
- Recheck source validity and focus after waiting for released modifiers, and guard the native input-injection entry point against a changed target.
- Add opt-in Safe Debug events with numeric HWND values, focus ownership, inserted-input count, native error code, and outcome code. No window titles or text are added to Safe Debug.

The correction does not establish that the original selection still exists or that the destination control consumed a paste. It retains the clipboard fallback when guarded replacement fails. No repeated paste or alternative destructive replacement mechanism is attempted.

## Validation evidence

`NativeResultFocusTests.RestoringAnActiveWindowPreservesItsFocusedChild` creates its own top-level window and native child editor, focuses that editor, and exercises native activation. Restoring the old `SetFocus(topLevel)` call makes the test fail because the focused HWND becomes the host. Removing that call makes it pass. The test sends no paste and does not modify an existing user document.

Additional guarded-replacement tests cover focus loss during modifier checks, a target closed during those checks, and failed input injection. The full Release suite passes: 55 AI, 89 Core, 29 Windows, 173 total. Tests were run outside the restricted workspace sandbox because its testhost connection timed out. All build/test invocations used `-m:1`, the project-local NuGet scratch directory, and `-p:NuGetAudit=false`.

Root `publish.ps1` produced `src/TextAid.App/bin/Publish/TextAid.exe`, FileVersion `1.2.13.0`, SHA-256 `5BB0574543E013FFA80FBB261F9647E30409BD5F0400856EFBEB85AF2710EF07`. The idle resident instance was stopped and the candidate launch was requested through root `start.ps1` in the Windows desktop context.

## Application validation and remaining coverage

1. Completed: Olivier confirmed that Replace works in smalBazar with the restarted TextAid 1.2.13 on 2026-10-17.
   Olivier also confirmed on the same date that Replace still works in the Codex input area, validating non-regression in that previously working target. The control's UI technology was not identified by this test.
2. Verify the same operation in a previously working WPF application and, if available, a second WinUI application. The automated native focus test does not substitute for these application checks.
3. If a similar failure recurs, enable Debug without Full log, reproduce once, and inspect `replace-before-activation`, `replace-after-activation`, `replace-input-sent` or `replace-paste-blocked`, and `replace-completed`. Four inserted events confirm injection only. Outcome codes are 0 Replaced, 1 InvalidTarget, 2 FocusFailed, 3 ModifiersStillPressed, 4 PasteFailed.
4. Compare foreground/focused HWNDs and focus ownership; investigate the actual control (including a possible WebView), selection restoration, elevation/UIPI, and clipboard consumption as the evidence warrants.
