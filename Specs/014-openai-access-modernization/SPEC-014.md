# LOT-014 — Modernisation accès OpenAI

> Before working on this lot, read the repository root README.md.

Parent: None
Created: 2026-10-18T00:00:00+02:00
Source: Olivier's 2026-10-18 specification request and routing/transfer-language clarification; D-046.

## Objective

Add a fourth connection type, **Sign in with OpenAI**, alongside local Ollama, network Ollama, and the generic OpenAI-compatible API-key connection. Support explicit reuse of a completed result as the next input and a temporary model selection for the next action.

## Context

TextAid currently distinguishes three deployment categories and resolves Action → ModelProfile → Connection → IChatClient → MAF. The fourth connection is a separate provider/authentication choice within **External**, not a fourth privacy category. The existing generic connection must continue to support arbitrary OpenAI-compatible providers.

SmallBazar's `docs/Spec-001-Etape-08BIS.md` documents successful Windows and Android synthetic sign-in/inference tests. Its `Services/ChatGpt` code provides a useful protocol reference, but its direct HTTP business integration must be adapted to TextAid's MAF boundary. Its validation does not validate TextAid. The customer-facing feature name here is **Sign in with OpenAI**; the referenced contract calls the account/plan authorization **Sign in with ChatGPT**.

D-046 authorizes this post-V1 scope and its narrow exceptions to historical three-connection routing and one-transformation wording. Each processing operation remains a separate explicit transformation; no autonomous chain or conversation is introduced.

## In scope

- A separate OpenAI sign-in Settings entry, independently persisted Active state, protected renewable session, account status, model discovery/selection, explicit test and sign-out.
- Integration with every AI selection path, External precedence, transient model override, and an identifiable connection indicator.
- A filled left-pointing transfer triangle between the input and result panes, allowing successive user-triggered operations on one working text.
- Configuration compatibility, cancellation, errors, localization, regression tests and Windows user validation.

## Out of scope

Replacing or removing API-key access; MAUI or Android support in TextAid; multi-account management; importing SmallBazar/Codex/browser credentials; private ChatGPT endpoints or browser automation; chat, autonomous workflows, undo/history or persistent transformation chains; rich-text transfer; bypassing plan limits; automatic retries or switching billing after inference failure.

## Requirements

### REQ-014-001 — Fourth independently configured connection

Settings MUST retain the three existing connection types and add a clearly distinct **Sign in with OpenAI** entry. It MUST have its own connection identity, Active flag, configured model/profile and session reference. It MUST use the External deployment boundary with deliberate user activation, fixed official service origins and no API-key field. Activating, deactivating, signing out or changing its model MUST NOT overwrite another connection or its key. Existing configurations and action references MUST load without signing in, activating External, or changing their saved choices. The new connection starts inactive and disconnected on first creation.

### REQ-014-002 — Browser sign-in and verified authorization

Sign-in MUST use the system browser and TextAid's own public-client registration under the applicable OpenAI contract, with no embedded client secret or credentials borrowed from another application. A cancellable loopback callback listener MUST start before opening the browser and bind only to loopback on an available port. The exact redirect URI MUST be bound to the attempt and reused for code exchange.

Use authorization code with PKCE S256, unpredictable per-attempt state and nonce. Reject wrong state/path/client, duplicate parameters, fragments, missing code, replayed or obsolete callbacks, and authorization denial. Validate the signed identity token against trusted OpenAI signing keys, issuer, audience/authorized party, expiry and nonce before accepting the session. Identity success alone MUST NOT enable inference: verify the separate plan/inference permission. Browser callback receipt MUST NOT be presented as completed sign-in.

The UI MUST remain responsive, show safe progress, distinguish user cancellation from timeout, allow up to fifteen minutes for browser/MFA completion, and dispose the listener on every terminal path. An unsuccessful replacement sign-in MUST preserve the previously valid session. Remote protocol values MUST be checked against current official documentation during implementation; the SmallBazar snapshot is evidence, not a guarantee of continued eligibility.

### REQ-014-003 — Protected session lifecycle

All access/refresh/identity tokens and sensitive registration/session material MUST be protected under TextAid's own current-user DPAPI storage, separate from config.json, action files, executable and logs. Configuration MUST contain only opaque references where secrets are involved. Saving a connected state MUST include successful protected persistence and read-back. No credential, authorization code, verifier, callback URL or raw authentication response may enter Safe Debug or Full log.

Refresh MUST be serialized for a session, preserve rotated tokens atomically and honor cancellation. Terminal expiry/revocation or lost permission MUST disable inference and require a visible reconnect; transient errors MUST NOT destroy a valid saved session. Sign-out MUST attempt official remote revocation, always clear local usable tokens, and distinguish confirmed remote revocation from local-only sign-out with guidance. Deactivation retains the session but excludes the connection from routing.

### REQ-014-004 — Models and TextAid AI boundary

Load models available to the authenticated authorized account and allow explicit selection of an offered model. Do not hard-code a universally available model or silently replace one that becomes unavailable. No model, missing permission or unreadable credentials MUST yield explicit configuration guidance. Settings opening and model-list loading MUST NOT run a test generation. An explicit connection test MUST use synthetic text.

All functional language identification, transformations and locale generation MUST continue through MAF and `Microsoft.Extensions.AI.IChatClient`. Implement/adapt the OpenAI account transport in TextAid.AI behind that boundary, preserving provider-independent Core and WPF MVVM. Authentication/model discovery may use dedicated infrastructure services. The account transport MUST obey the applicable public Responses contract, including streaming, non-persistent requests and supported message/options mapping. Do not copy SmallBazar's direct business HTTP call path or route account tokens to an arbitrary compatible endpoint.

A complete result requires terminal success; partial text may appear only as the existing incomplete preview and MUST NOT enable transfer, Copy or Replace. Refusal, incomplete/malformed streams, quota/permission errors and network failures remain failures. No automatic generation retry, API-key substitution or billing switch is permitted. Honor invocation cancellation/timeouts, reject late responses, and report unsupported action options clearly instead of forwarding incompatible Ollama-specific fields.

### REQ-014-005 — External routing and OpenAI preference

Both cloud types MUST occupy the same External rank in category selection: External → On-premises → This device only. Whenever automatic/default resolution chooses an External candidate and both cloud types are active, configured, authorized and eligible, prefer **Sign in with OpenAI**, using its own selected model/profile. This is the cost-preference rule for every automatic cloud selection; most users are expected to configure only one of the two access methods, and coexistence is an edge case. This preference applies even when the action's saved External profile points to the API-key connection; the explicit override in REQ-014-010 takes precedence. Show the actual selected connection/model in status.

The fourth type MUST participate in eligibility, sole-configuration promotion, missing-category downgrade, action and reserved Translate resolution, preliminary language identification, retry/downgrade proposals, Settings tests and locale-generation choices. Valid requested local/network routes retain their rank and behavior. When only the two External types are eligible, resolve their tie to sign-in rather than treating their coexistence as an unresolved cloud choice; this permits intentionally cloud-only setups. Multiple categories MUST NOT cause an otherwise forbidden upward promotion. In explicit Settings operations, the specifically selected connection/profile remains authoritative.

An inactive or never-configured sign-in connection is ignored. A configured sign-in connection with invalid credentials/permission MUST report that configuration error rather than silently using its API-key peer. A runtime failure MUST report the failure and retain R-027's user-controlled lower-category recovery. A same-rank API-key alternative may be chosen explicitly by the user, never silently after failure. The override's stricter behavior is defined below.

### REQ-014-006 — Distinct connection indicator

The existing connection indicator MUST distinguish sign-in from API-key External with a new symbol and a new centralized theme color. It MUST reflect the connection actually executing/producing the result, including explicit override and recovery, rather than derive identity solely from the External category. The tooltip/accessibility label MUST name the type and selected model without exposing credentials. Color MUST NOT be the only differentiator. Preserve recognizable existing local, network and API-key indicators. Record the selected symbol/color tokens and obtain visual acceptance through G-014-005.

### REQ-014-007 — Result-to-input triangle

Between the captured-text pane on the left and result pane on the right, display a readable **filled left-pointing triangle**. It MUST appear as a standalone symbol with no visible button chrome, border or enclosing button surface, and MUST change color on mouse hover using theme tokens. Provide a usable hit target, tooltip, accessible action name and keyboard activation/focus indication without changing that visual form. It MUST remain visible and correctly positioned when the split window resizes.

Activation MUST be available only for the latest complete, successful, nonempty result, and unavailable during processing, failure, cancellation or an incomplete stream. Activation MUST copy the exact authoritative result string into the working captured/input field, replacing its entire prior contents. For Markdown results this is the original Markdown string, not text extracted from the rendered document. This action MUST NOT write the clipboard, paste into/focus the source application, close the session, or start a transformation.

### REQ-014-008 — Safe sequential processing

Transfer MUST snapshot both the authoritative result text and its known output language before invalidating and clearing the old result, disabling its transfer/Copy/Replace actions, cancelling obsolete work and rejecting late updates. The transferred text becomes the current editable input and its source language MUST be the known language of that result, without a new language-identification call. Record the effective language with each successful result: the explicit target language for a targeted output, or the known input language for an unchanged-language action. An old input-language detection response MUST NOT overwrite the transferred language. Recalculate automatic translation destinations from this inherited language; retain an explicitly chosen destination. Only when the result language is genuinely unknown, or after a subsequent user edit invalidates that knowledge, use the existing asynchronous detection path. Transfer MUST NOT automatically translate or transform even in a quick-translation session. The user may choose another action and explicitly process again, repeating this sequence.

Preserve the original captured source HWND and existing guarded Replace transaction across transfers. Only the latest successful transformation may replace the original selection. A manual/free session with no replacement target MUST NOT acquire one from transfer. Cancel leaves the original application unchanged. No history, hidden chain or conversation state is added; any further action uses only the current input and its invocation settings.

### REQ-014-009 — Conditional selector and neutral Default

Show the main-session model selector if and only if more than one active configured model profile is available on enabled connections. Each item MUST identify the connection type and model/profile, so identical model names on different connections remain distinct. List configured selectable profiles, not every model in a remote catalogue. Inactive, incomplete or structurally invalid configurations MUST NOT be selectable; a temporarily unreachable configured provider MUST NOT disappear merely because a network probe failed. With zero or one choice, hide the selector and remove any stale override.

Each new session and each transition from hidden to visible MUST start on **Default** (English source text, localized through resources). Default is a neutral extra item, not a connection and not a global saved model. It MUST leave ordinary routing and REQ-014-005's External preference unchanged. Refresh the choices after valid configuration changes without persisting the transient selection into Settings or action files.

### REQ-014-010 — One-action override and systematic reset

Selecting a concrete item MUST pin its exact profile, connection and model for the next action, overriding the action profile, automatic category deductions, sole-choice promotion and sign-in-over-key preference. Selecting an API-key item explicitly MUST therefore use that item even when OpenAI sign-in is available. This does not waive validation, authorization, privacy boundaries, supported options or the action's non-provider settings.

Snapshot the selection when an action starts, for Process, immediate action presets, quick-translation reruns/destination changes and any other main-session action path. All new AI work belonging to that action, including required preliminary language identification, MUST honor that snapshot. If selection changes while preliminary identification is pending, cancel/ignore obsolete work; reuse already completed identification for unchanged input without silently reissuing it. Changing the selector alone MUST NOT launch inference. The initial shortcut-triggered translation uses Default because no prior session choice exists.

If the pinned choice is removed, disabled, invalid or fails, report that exact failure; do not substitute any other provider in that attempt. Recovery requires a new explicit user choice/action. Selection MUST reset to Default on every terminal action path, including success, error, timeout, cancellation and cancellation of a required instructions dialog. The reset MUST occur without invalidating the action's result or changing its actual-provider indicator. A selection made but not yet used is cleared on session close or loss of eligibility. Prevent concurrent selection changes from altering an already running snapshot. Subsequent actions use Default unless the user chooses again.

### REQ-014-011 — Compatibility, localization and delivery

Preserve existing keys, action/profile identities, independent activation, local-only privacy, quick-translation protection, safe focus validation, inert Markdown preview and raw Copy/Replace behavior. New UI text MUST follow R-031's English-source-first resources and fingerprint invalidation. Keep theme tokens centralized and use MVVM Toolkit commands/properties. Add focused deterministic coverage for the protocol boundary, routing precedence and session transitions; existing provider tests MUST remain independent of a real account.

Publish the final implementation using root `publish.ps1` to the sole executable in `src/TextAid.App/bin/Publish/`. Identify the evaluated binary/version/hash for gates, launch from Codex through root `start.ps1`, and record separate-machine portability under R-012. No executable is produced by this specification-only task.

## Important cases

| Situation | Required outcome |
|---|---|
| Existing local/network/key configuration opens after upgrade | Preserved; sign-in inactive/disconnected; no unsolicited browser or cloud call. |
| Local requested and eligible; both cloud types configured | Local remains selected on Default. |
| External requested; sign-in and key eligible | Sign-in selected on Default; exact API-key item wins when explicitly selected. |
| Only both External types are eligible | Default resolves the External tie to sign-in, visibly, including actions whose local category is absent. |
| Sign-in disabled or never configured; key eligible | Key remains available under ordinary category routing. |
| Configured sign-in permission missing or refresh revoked | Explicit error/reconnect; no hidden API-key substitution. |
| Pinned local model but action requests External | Exact local choice used for this action; reset afterward. |
| Pinned provider errors, times out, or instructions are cancelled | No substitute; reset to Default; no stale successful result actions. |
| Successful output followed by automatic selector reset | Output remains usable; indicator still identifies the producing provider. |
| One model configured, then a second is activated | Selector becomes visible on Default; two distinct connection/model items. |
| Complete Markdown result transferred twice through different actions | Exact raw strings and known result languages inherited; no redundant detection; each transfer clears prior output; only explicit processing transforms. |
| Transfer in quick Translate or a source-less free session | No automatic transformation; original target retained if present, never invented. |
| Late detection/stream callback after transfer or close | Ignored; cannot restore old text, language, provider or result. |
| Sign-in succeeds but plan permission/catalog/generation fails | Show the particular state; callback or catalogue success alone is not inference validation. |

## Dependencies

LOT-013 must be closed before implementation starts; it is the latest closed lot and inherits LOT-006/007/008/009/012 foundations. Include subsequent maintenance through published 1.2.13 as the behavioral baseline, especially D-044/R-032 detection and the 2026-10-17 focus correction. D-045's action options also apply.

Applicable rules include R-001 through R-005, R-007, R-009 through R-012, R-015 through R-018, R-020 through R-024, and R-026 through R-032 according to their scopes, with the prospective LOT-014 exceptions recorded by D-046. Closed lots, historical rules, gate results and accepted executables remain historical evidence.

## Known constraints

The implementation MUST reverify the official sign-in, registration, eligibility, token/session, models, Responses and recovery contract for TextAid's intended distribution before live acceptance. Record findings if the snapshot no longer applies; do not silently replace this feature with an API key or a private endpoint. SmallBazar's scope and publication permissions are not evidence of TextAid eligibility.

Reference snapshot: `E:\source\SmallBazarSolution\SmallBazar\docs\Spec-001-Etape-08BIS.md`, `README.md`, `Services/ChatGpt/ChatGptProtocol.cs`, `ChatGptSession.cs`, `LoopbackCallback.cs`, `ChatGptInference.cs`, and `tests/SmallBazar.ChatGpt.Tests/ChatGptTests.cs`. Reimplement/adapt only the needed behavior for WPF and MAF; do not copy SmallBazar data/report or platform-specific behavior. Durable observations needed to implement this lot are recorded in FINDINGS-014.md so the external checkout is not the only recovery source.

This specification fixes behavior; exact new indicator glyph/color and final control placement remain implementation design choices subject to the human visual gate. GATES-014.md defines closure checks. STATUS.md alone owns lot state; implementation and closure require their separate human decisions.
