# Project rules

These rules apply to all V1 lots unless a rule names a narrower phase. They originate in the retained `docs/TextAid specification.md` and are initial rules, so no ledger decision is required. Later changes follow Pro-Spec section 12.

## R-001 — Product identity and scope

Status: ACTIVE
Source: Initial project rule; source sections 1–3, 91, 94, 96
Rule: The product MUST be named TextAid everywhere, use the exact subtitle “Local-first text transformer & translator”, and remain a one-text/one-transformation utility. Post-V1 features listed in source section 91 MUST NOT enter V1 lots.
Replaces: None
Replaced by: None

## R-002 — Platform and architecture

Status: ACTIVE
Source: Initial project rule; source sections 7–11, 94
Rule: The product MUST target .NET 10, C#, WPF, and Windows x64. The target solution has `TextAid.App`, `TextAid.Core`, `TextAid.AI`, `TextAid.Platform.Windows`, the three matching test projects, and `TextAid.TestTarget`. Core MUST have no WPF, Win32, or OllamaSharp dependency; Windows integration MUST remain in Platform.Windows. The WPF UI MUST use MVVM with CommunityToolkit.Mvvm; code-behind is limited to view and Win32 concerns.
Replaces: None
Replaced by: None

## R-003 — Dependency discipline

Status: ACTIVE
Source: Initial project rule; source sections 8, 37, 56, 94
Rule: Dependencies MUST stay few. CommunityToolkit.Mvvm is the only MVVM framework; do not add a logging, localization, theming, configuration, mediator, event-bus, or prompt-template framework without demonstrated need. Do not create an internal framework for simple behavior.
Replaces: None
Replaced by: None

## R-004 — AI access boundary

Status: ACTIVE
Source: Initial project rule; source sections 4–6, 42, 47, 94
Rule: All application AI calls MUST pass through MAF and `Microsoft.Extensions.AI.IChatClient`. The default Ollama connection MUST use OllamaSharp, without a custom Ollama HTTP client. Functional code MUST NOT call providers directly or introduce a competing `ILlmProvider`. MAF MUST be used for a single transformation call, without agent tools, memory, or autonomous behavior.
Replaces: None
Replaced by: None

## R-005 — Local privacy boundary

Status: ACTIVE
Source: Initial project rule; source sections 4, 48, 55–59, 90
Rule: The initial configuration MUST enable Strict Local. In that mode only `localhost`, `127.0.0.1`, and `::1` are automatically local; private LAN addresses are remote. No remote AI call, cloud fallback, telemetry, or user-data egress is allowed. API keys MUST NOT be stored in clear text in `config.json`. With Debug off, no log or persistent trace is created; with Debug on, no secret, user text, result, or prompt containing user text is logged.
Replaces: None
Replaced by: None

## R-006 — Safe source replacement

Status: ACTIVE
Source: Initial project rule; source sections 20–33, 90
Rule: A session MUST have only `Accept` or `Cancel`, with at most one active session. Cancel MUST leave the source unchanged. Before `SendInput(Ctrl+V)`, the captured source HWND MUST still be valid and confirmed restored, and modifier keys MUST be released. Failure MUST prevent paste to an arbitrary window and leave the generated result in the clipboard. Wrong-window paste is a release blocker.
Replaces: None
Replaced by: None

## R-007 — Visual and language foundation

Status: ACTIVE
Source: Initial project rule; source sections 12–16, 49–54, 72–73
Rule: Fix the palette, typography, layout dimensions, logo, and icon before visible V0.1 UI implementation. All windows, title bars, and controls MUST use a complete dark theme with centralized tokens and no hard-coded view colors. English is the source UI language; visible UI strings MUST use resources, and translated catalogs MUST fall back to English on failure.
Replaces: None
Replaced by: None

## R-008 — Single-file distribution

Status: ACTIVE
Source: Initial project rule; source sections 9, 73, 90
Rule: Starting at V0.1, Release publishing MUST produce a self-contained single `TextAid.exe` for win-x64 with trimming disabled; no adjacent configuration or runtime file may be required for distribution. User configuration is created in the user profile at first run.
Replaces: None
Replaced by: None

## R-009 — Incremental quality

Status: ACTIVE
Source: Initial project rule; source sections 72–90, 93–94
Rule: Follow the source milestone order from design gate through V1. Each V0.x MUST compile, remain testable, and preserve validated behavior. Validate the Windows capture/Accept pipeline before adding MAF. Unit AI tests MUST use a fake `IChatClient`; live Ollama integration tests are optional. For any `dotnet build` or `dotnet test`, explicitly pass `-m:1`.
Replaces: None
Replaced by: None

## R-010 — Code conventions

Status: ACTIVE
Source: Initial project rule; project-wide AGENTS.md instruction
Rule: Application code and UI MUST be in English, formatted for human maintenance. Code comments MUST be in English and documentation comments MUST use XML style. Use MVVM Toolkit by default.
Replaces: None
Replaced by: None
