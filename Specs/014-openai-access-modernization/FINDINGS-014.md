# Findings — LOT-014

> Before working on this lot, read the repository root README.md.

## F-014-007 — Explicit selector item template

Status: RESOLVED LOCALLY
Found at: 2026-10-18

Finding:
Olivier reports that the functional tests pass and provides a screenshot showing the selected neutral choice rendered as `SessionModelChoice { ProfileId = , Label = Default }`. The custom ComboBox selection presentation does not use the generated DisplayMemberPath template as intended.

Resolution:
MainWindow now supplies an explicit DataTemplate with a TextBlock bound to Label, following the existing action and output-language selectors. Both the selected value and dropdown use their display label. Default remains neutral; routing and selection state are unchanged. Candidate version is 1.3.1. This cosmetic change does not invalidate the previous protocol/routing/transfer test results; no new deterministic test was added for the XAML binding. Release build/publication evidence is recorded separately.

Destinations:
- Local: REQ-014-009; G-014-005 and G-014-006; IMPLEMENTATION-014.md.

## F-014-005 — Current public contract adapted to TextAid

Status: RESOLVED LOCALLY
Found at: 2026-10-18

Finding:
The current official OpenAI registration, account/session, catalogue/inference and preview-limitations pages confirm the dynamic public-client flow. The Responses account route requires non-persistent streaming and omits temperature and maximum output tokens. TextAid now owns its protected host/registration/session and exposes account inference only through IChatClient and MAF. Existing generic compatible access remains independently configured. Account session references are rejected by the API-key transport, and production account clients disable redirects and fix official service origins.

Evidence:
See the dated official references, requirement mapping, 232-test Release result and identified 1.3.0 candidate in IMPLEMENTATION-014.md. Tests use synthetic credentials and isolated DPAPI storage. G-014-001/002/003/006 are PASS.

Impact:
Transport and protocol behavior have deterministic TextAid-specific evidence; SmallBazar validation is not being substituted for TextAid validation. The account route rejects unsupported action options with localized guidance instead of forwarding Ollama fields.

Destinations:
- Local: REQ-014-001 through REQ-014-005 and REQ-014-011; G-014-001 and G-014-006.

## F-014-006 — Real-account and visual acceptance remain pending

Status: OPEN
Found at: 2026-10-18

Finding:
Olivier identifies TextAid as an open-source application. The supported public integration has been implemented without importing credentials, changing the license or using private endpoints. No real TextAid browser/MFA/plan/inference journey or named visual acceptance was performed by the agent. Successful synthetic OAuth/MAF tests, catalogue parsing and resident startup do not establish these human outcomes.

Evidence:
G-014-004 and G-014-005 remain TO TEST on the identified 1.3.0 candidate. The walkthrough and exact symbol/theme tokens are in IMPLEMENTATION-014.md. Separate-machine portability is DEFERRED in PORTABILITY-CHECKS.md.

Impact:
Keep LOT-014 In-progress. Next action is Olivier's synthetic-text walkthrough of account authorization and interface behavior, including restart, transfer, selector, Copy/Replace/Cancel and the applicable distribution contract. No HUMAN verdict or closure is inferred.

Destinations:
- Local: G-014-004 and G-014-005; STATUS.md.

## F-014-001 — SmallBazar provides a validated reference, not a TextAid integration

Status: RESOLVED LOCALLY
Found at: 2026-10-18T00:00:00+02:00

Finding:
SmallBazar's step 08 BIS records successful synthetic sign-in, account model listing and terminal Responses generation on Windows and Android on 2026-10-17. Its protocol uses a public dynamic registration, persistent host identity, authorization code/PKCE S256, state and nonce, loopback callback, signed OIDC validation and a separate plan-use permission. The snapshot names `dynamic_agent_client`, `chatgpt.tokens.use.direct`, `https://auth.openai.com` and public `/v1/models` and `/v1/responses`. These are implementation references to reverify, not claims that any account or distribution is eligible.

Evidence:
`E:\source\SmallBazarSolution\SmallBazar\docs\Spec-001-Etape-08BIS.md`; `README.md`; `Services/ChatGpt/ChatGptProtocol.cs`, `ChatGptSession.cs`, `ChatGptInference.cs`.

Impact:
TextAid must register independently, protect its own session with CurrentUser DPAPI and adapt inference behind IChatClient/MAF. No real TextAid sign-in or inference has been performed during specification drafting. SmallBazar's transport must not be copied into TextAid functional code as a second AI pipeline.

Destinations:
- Local: REQ-014-001 through REQ-014-004; G-014-001 and G-014-004 require TextAid-specific validation and contract review.

## F-014-002 — Stream completion and sign-in progress need separate success boundaries

Status: RESOLVED LOCALLY
Found at: 2026-10-18T00:00:00+02:00

Finding:
SmallBazar's recorded investigations distinguish callback receipt from exchanged, validated and persisted credentials, and distinguish identity from plan authorization. Its Windows stream arrived without Content-Type and with text deltas absent from the terminal summary. Its corrected reader accepts a missing Content-Type only subject to strict SSE/terminal validation, assembles deltas, rejects conflicting final text/refusal/incomplete streams, and does not retry generation. Usage-limit failure was observed independently of successful sign-in/model discovery. Cleanup must not mask the original stream outcome.

Evidence:
Step 08 BIS sections on Windows callback diagnostics, missing Content-Type, delta assembly and Android usage limits; `Services/ChatGpt/ChatGptInference.cs`.

Impact:
Callback receipt, HTTP 200, model listing and streamed fragments cannot independently enable result actions. Diagnostics must use fixed safe messages/recognized error codes and never expose OAuth values or raw response bodies. Current TextAid streamed-preview safety must remain intact.

Destinations:
- Local: REQ-014-002 through REQ-014-004, REQ-014-007; G-014-001, G-014-003 and G-014-004.

## F-014-003 — A fourth connection is not a fourth deployment category

Status: RESOLVED LOCALLY
Found at: 2026-10-18T00:00:00+02:00

Finding:
`ConnectionCategory` has ThisDeviceOnly, OnPremises and External; the current indicator derives its symbol from that category. `ProfileResolver` enumerates eligible connections and normally treats multiple configurations as distinct candidates. Merely adding authentication cannot express both the same-rank cloud preference and a fourth indicator identity.

Evidence:
`src/TextAid.Core/ConnectionDefinition.cs`, `ProfileResolver.cs`; `src/TextAid.App/ViewModels/MainViewModel.cs` (`ActiveConnectionSymbol`); `src/TextAid.App/Views/MainWindow.xaml`; R-027/R-029.

Impact:
Routing must preserve the three privacy ranks, distinguish authentication/provider identity and implement deterministic sign-in preference within External. Explicit profile override must win over this preference. D-046 captures the prospective scope exception without rewriting historical acceptance evidence.

Destinations:
- Local: REQ-014-005, REQ-014-006, REQ-014-009, REQ-014-010.
- Ledger: D-046.

## F-014-004 — Transferring output must use input invalidation without losing the source target

Status: RESOLVED LOCALLY
Found at: 2026-10-18T00:00:00+02:00

Finding:
`InvocationSession.SetInputText` invalidates an old result and input-scoped language identification. `ResetForNewInput` also clears the captured source HWND, which would break later replacement of the original selection. MdXaml output is presentation-only; OutputText remains the raw authoritative result.

Evidence:
`src/TextAid.Core/InvocationSession.cs`; REQ-013-003; D-044/R-032; `docs/replace-winui-investigation.md`.

Impact:
Transfer must snapshot raw output and its known output language before invalidation, preserve the existing source target, clear old result actions and await explicit user processing. Olivier clarified that the result language becomes the transferred input language; a known result language must not trigger redundant AI detection. Preserve that metadata independently of clearing OutputText; detect again only if unknown or invalidated by a later text edit. Automatic selector reset must not use request-edit invalidation to erase a successful result. No implementation is claimed by this finding.

Destinations:
- Local: REQ-014-007 through REQ-014-010; G-014-003 and G-014-005.

## F-014-008 — Account model selected display

Status: RESOLVED LOCALLY
Found at: 2026-10-18

Olivier supplied a screenshot of OpenAiAccountModel rendered as an object while the dropdown names were correct. Settings / Sign in with OpenAI now uses an explicit DisplayName DataTemplate for the account model ComboBox, including its selected resting display. SelectedValuePath remains Slug, preserving the inference model identity. Release app build: zero warnings/errors. Root publish.ps1 produced FileVersion 1.3.2.0, SHA-256 DA7AF13F2DB14C5B5C1D7EAE19A9BDA71D4FA18806C677C99A2B331A2616ED35. No functional changes or new tests; prior 232-test evidence remains applicable. Build/publication logs and source manifest are retained in validation/ with the 1.3.2 suffix. App left stopped.

Destinations: REQ-014-004, G-014-005 and G-014-006.
