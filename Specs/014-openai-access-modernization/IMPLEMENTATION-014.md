# Implementation and validation — LOT-014

Implementation authorized by Olivier's explicit request on 2026-10-18: read the bootstrap and implement `014-openai-access-modernization`. This is an implementation candidate, not human acceptance or lot closure.

Initial 1.3.0 published result: `src/TextAid.App/bin/Publish/TextAid.exe`, FileVersion **1.3.0.0**, SHA-256 **DBA9E61A8D2E9F07BB5E2BD676720C0F36C8981205318C01BDC656112D794FB7**. Source identity: base commit `93d91da0ab383eb9642bf2507dab3f2e0c64d468` plus the uncommitted sources listed in `validation/SOURCE-014.sha256` (manifest SHA-256 `F10B70786A6364BB361D8CB293F2A5BC0A20B55750D0626BC9EF5E9440E198A6`). Root `publish.ps1` succeeded after stopping the idle 1.2.13 instance that locked the old executable. Root `start.ps1` requested launch through the desktop shell and the new resident process was verified. Olivier subsequently reported TextAid closed; a process check confirmed it was stopped. It was left stopped.

## Implemented behavior

- REQ-014-001–004: independent `openai-account` External connection and `openai-account-default` profile, inactive after migration. Settings has a distinct Sign in with OpenAI tab and the official Continue with ChatGPT browser action. TextAid generates its own host identity and dynamic registration. PKCE, callback state/client/path validation, signed RSA identity validation and separate plan scope precede protected session persistence. CurrentUser DPAPI writes are atomic and verified. Refresh is serialized, rotated credentials are saved before use, and failed replacement sign-in preserves the previous valid record. Sign-out attempts official revocation and clears local credentials even after remote failure.
- REQ-014-004: `OpenAiAccountChatClient` implements `IChatClient`; functional transformations, language identification, explicit synthetic tests and locale generation use the existing MAF service. Fixed public API origins, redirects disabled, `store:false`, `stream:true`, system/developer instructions mapping, account-specific offered model discovery, strict terminal stream success and cancellation. Unsupported action context/thinking options produce guidance. Account tokens cannot be consumed by the generic API-key factory. Permission rejection disables the rejected session; quota failure retains credentials. No automatic generation retry or key substitution is added.
- REQ-014-005: automatic External selection prefers the account profile, including the two-cloud-only setup. Existing valid local/network routes keep their category. An explicit profile bypasses preference and category promotion, with no provider substitution or automatically generated recovery guidance on another profile.
- REQ-014-006: the actual request/result retains its connection and model identity. Account symbol: `✦`; centralized `OpenAiAccount` color: `#B6A0FF`, with `OpenAiAccountBrush`. Existing local `⌂`, network `⌁`, and API-key `◎` remain recognizable. Accessible label and tooltip include connection type and model.
- REQ-014-007–008: standalone filled left-pointing path between panes, 40 × 48 hit target, theme hover/focus colors and keyboard Button semantics. `Transfer` = `#AAB6C5`; `TransferHover` = `#5CCDF2`. Only an authoritative completed nonempty result can transfer. It copies the exact raw string, carries effective language, invalidates old output, cancels obsolete classification and retains the captured source HWND. It never calls clipboard/focus/paste/processing services. Automatic translation destination is recomputed during the next explicit action; an explicit destination survives.
- REQ-014-009–010: configured profile choices with distinct connection/model/profile labels, visible only above one choice, with neutral localized Default. The running profile is snapshotted independently from the next choice. All main-session entry paths converge on `StartTransformation`: Process, presets, destination/Markdown reruns, quick Translate and Free. Selector changes do not generate text; completed detection is reused. Success, failure, cancellation, instructions cancellation and close clear the unused override without erasing a successful result or its actual-provider indicator. Configuration refresh removes ineligible choices and cancels a running removed pin.
- REQ-014-011: English catalog and XAML resources updated together, retaining automatic source-fingerprint invalidation. MVVM Toolkit commands and observable ViewModels remain the UI boundary. Existing action files, API keys and replacement guards are preserved.

## Official contract review

Reviewed on 2026-10-18:

- [Registration and sign-in](https://developers.openai.com/siwc/token-sharing-open-source/sign-in): TextAid-owned dynamic registration, persistent host ID, loopback PKCE, issued client ID, signed identity and plan scope validation.
- [Accounts and sessions](https://developers.openai.com/siwc/token-sharing-open-source/profiles-and-sessions): returning registration, refresh and revocation lifecycle. TextAid's specification deliberately supports one registration.
- [Models and inference](https://developers.openai.com/siwc/token-sharing-open-source/models-and-inference): account catalogue uses offered display names/slugs; inference uses the public Responses endpoint.
- [Preview limitations](https://developers.openai.com/siwc/token-sharing-open-source/preview-limitations): non-persistent streaming; omit unsupported fields including temperature and maximum output tokens.
- [Quickstart](https://developers.openai.com/siwc/quickstart): eligibility depends on the applicable OpenAI account/application contract. Olivier identifies TextAid as an open-source application; no TextAid account authorization or distribution approval has been empirically verified here. The repository's CC BY-NC license is unchanged. G-014-004 retains the live contract/distribution check.

SmallBazar supplied the protocol and loopback reference; the account lifecycle and MAF adapter were implemented for TextAid. No SmallBazar/Codex/browser credentials were read or imported. No real account was used in automated tests.

## Automated verification

Release solution build: zero warnings, zero errors. Release tests: **232 passed** (98 AI, 103 Core, 31 Windows). The original 173 tests remain in the suite.

Focused new coverage is in `OpenAiAccountTests`, `ModernizationTests` and `ModernizationViewModelTests`. It covers synthetic signed tokens, malformed/ambiguous callbacks, cancellation and listener disposal, failed replacement authorization, isolated DPAPI, save/read-back rollback, rotating refresh serialization, terminal versus transient failures, local sign-out, diagnostic exclusion, offered catalogue filtering, MAF Responses mapping, missing Content-Type, delta-only terminal completion, refusal/interruption/malformed/conflicting streams, HTTP permission/quota failures, timeout/no retry, endpoint and credential isolation, migration, routing preference/exact pins, selector state, raw transfer/language inheritance and late response rejection.

The restricted workspace prevented VSTest from connecting to testhost. Tests succeeded after allowing the test runner's local process/network communication. The initial closed-port assertion was corrected to accept either socket rejection or cancellation without a successful connection on Windows. Final logs and a content-hash source manifest are retained in `validation/`.

## Human walkthrough still required

G-014-004 and G-014-005 remain TO TEST. On the identified published candidate:

1. In Settings, open Sign in with OpenAI, choose Continue with ChatGPT, complete browser/MFA, and confirm the final verified/persisted status. Cancellation must preserve an existing connection.
2. Load models, explicitly select an offered model, enable Active and save. Explicit Test connection generates synthetic text only. Check restart, sign-out/reconnect, local-only sign-out guidance and continued generic API-key access.
3. With multiple configured profiles, check Default, an explicit local/key/account choice, one-action reset and actual-provider tooltip. Confirm inactive/incomplete choices are absent.
4. Generate plain text and Markdown, activate the left triangle, check the exact intermediate input and inherited language, then explicitly choose another action and Process. Confirm no automatic generation, clipboard change or source focus on transfer. Test both quick Translate and source-less sessions.
5. Accept triangle placement, hover, focus, resize/accessibility behavior and the account symbol/color. Exercise final Copy/Replace/Cancel in a disposable editor, retaining the 1.2.13 focus protections.

A separate clean Windows x64 machine remains unavailable; portability is DEFERRED under R-012. No real-account, visual-acceptance, portability or lot-closure PASS is claimed.

## Cosmetic follow-up — 1.3.1

Olivier reported functional tests passing and supplied the Default selector screenshot on 2026-10-18. MainWindow now uses an explicit Label DataTemplate instead of DisplayMemberPath, so the selected entry displays only its label. Routing and session behavior are unchanged. Release build: zero warnings/errors. Root publish.ps1 produced FileVersion 1.3.1.0, SHA-256 622E13C1A0FC1C6384ABCC5681225DAF7109E41F5AF97EC82ACC16B0768ABD6A. Source manifest: validation/SOURCE-014-1.3.1.sha256. Logs: validation/build-014-1.3.1.log and validation/publish-014-1.3.1.log. The previous 232-test result remains applicable to unchanged functional behavior; tests were not rerun for this XAML-only correction. No new human gate verdict is inferred.

## 2026-10-18 — Selected account model display correction (1.3.2)

Settings / Sign in with OpenAI now uses an explicit DisplayName DataTemplate for the account model ComboBox, including its selected resting display. SelectedValuePath remains Slug, preserving the inference model identity. Release app build: zero warnings/errors. Root publish.ps1 produced FileVersion 1.3.2.0, SHA-256 DA7AF13F2DB14C5B5C1D7EAE19A9BDA71D4FA18806C677C99A2B331A2616ED35. No functional changes or new tests; prior 232-test evidence remains applicable. Build/publication logs and source manifest are retained in validation/ with the 1.3.2 suffix. App left stopped.
