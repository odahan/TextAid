# Gates — LOT-014

> Before working on this lot, read the repository root README.md.

The gates were initially unevaluated at specification preparation. The implementation evaluations below identify the candidate; human gates remain pending. Before recording PASS, identify the evaluated source revision and, for release walkthroughs, executable version and SHA-256. Preserve previous evaluations in Test history before replacing them.

## G-014-001 — Authentication, transport and credential regression

Type: AUTO
Status: PASS
Defined at: 2026-10-18T00:00:00+02:00
Requirements: REQ-014-001 through REQ-014-004, REQ-014-011.

Condition:
Deterministic tests cover configuration compatibility/independent activation; PKCE; callback validation and cancellation; signed identity and plan permission; failed replacement sign-in preserving the previous session; protected save/read-back failure; serialized rotating refresh; terminal revocation; local sign-out despite remote failure; and credential exclusion from both diagnostic modes. Verify the IChatClient integration with fake HTTP/account services: offered model handling, supported request mapping, complete SSE text, missing Content-Type with valid SSE, delta-only completion, refusal, interruption, malformed responses, timeouts and no automatic retries or key substitution.

Method:
Use fake IChatClient/HTTP services and synthetic signed tokens; never use a real account in deterministic tests. Include Windows protected-storage coverage with isolated test data, avoiding the user's vault. Run applicable tests under the build procedure in G-014-006. Inspect that functional inference uses the MAF path and account credentials cannot reach an arbitrary endpoint.

Tested at: 2026-10-18 (Europe/Paris)
Evaluated result: Release 1.3.0; base commit 93d91da0ab383eb9642bf2507dab3f2e0c64d468 plus the working-tree sources in validation/SOURCE-014.sha256 (manifest SHA-256 F10B70786A6364BB361D8CB293F2A5BC0A20B55750D0626BC9EF5E9440E198A6).
Result: PASS.
Evidence: 98 AI tests pass. Synthetic OAuth signatures/callbacks, failed replacement authorization, protected read-back, rotating refresh, revocation, isolated DPAPI, safe diagnostics, offered models, MAF Responses mapping, strict streams, timeout/no retry and account-key/endpoint isolation are covered. See IMPLEMENTATION-014.md and validation/build-014-release.log, validation/test-014-candidate.log, validation/publish-014.log.

Test history: None.

## G-014-002 — Routing and temporary override matrix

Type: AUTO
Status: PASS
Defined at: 2026-10-18T00:00:00+02:00
Requirements: REQ-014-005, REQ-014-009, REQ-014-010.

Condition:
Deterministic resolution/session tests prove every routing/selector case in SPEC-014's Important cases, including local/network preservation, both-External cloud-only preference, missing/inactive/invalid sign-in distinctions, exact explicit API-key override over sign-in preference, explicit local override of an External action, zero/one/multiple configured profiles, configuration refresh, Default neutrality, no selector-triggered inference and all main-session execution paths. Required preliminary classification follows the action snapshot; obsolete requests cannot change it. Every success/error/timeout/cancel/instructions-cancel path resets selection without changing the running provider, erasing a successful result, modifying action files or triggering fallback. Runtime failure cannot silently switch to the API key.

Method:
Use table-driven fake-provider tests with recorded connection/model identities and request counts. Include preset processing, quick Translate reruns/destination changes and selection/configuration changes during preliminary work.

Tested at: 2026-10-18 (Europe/Paris)
Evaluated result: Release 1.3.0; base commit 93d91da0ab383eb9642bf2507dab3f2e0c64d468 plus the working-tree sources in validation/SOURCE-014.sha256 (manifest SHA-256 F10B70786A6364BB361D8CB293F2A5BC0A20B55750D0626BC9EF5E9440E198A6).
Result: PASS.
Evidence: 103 Core and 31 Windows tests pass. Routing preference/exact pins, cloud-only selection, inactive/invalid account, selector visibility/reset/running snapshot and no selector-triggered inference are covered. Source review verifies Process, presets, quick Translate, destination/Markdown reruns and Free converge on StartTransformation; pinned errors do not run recovery inference elsewhere. See IMPLEMENTATION-014.md and validation/build-014-release.log, validation/test-014-candidate.log, validation/publish-014.log.

Test history: None.

## G-014-003 — Transfer and replacement state safety

Type: AUTO
Status: PASS
Defined at: 2026-10-18T00:00:00+02:00
Requirements: REQ-014-006 through REQ-014-008, REQ-014-010, REQ-014-011.

Condition:
Deterministic tests prove exact raw-text/Markdown transfer, clearing old output/result actions, no transformation/clipboard/paste/focus operation on transfer, preserved captured target, source-less session safety, inheritance of the known result language without another detection call, detection only for unknown/later-edited input and explicit-destination retention, late-response rejection and unavailability for partial/failed results. Two successive explicit transformations consume the transferred intermediate text. The last successful output alone can be copied/replaced under existing native focus guards. Provider indicator identity remains tied to the actual request/result despite selector reset.

Method:
Exercise Core and ViewModel commands with fake AI, clipboard and result-action services; retain existing Markdown and Windows guarded-replacement regression coverage. No test may paste into an existing user document.

Tested at: 2026-10-18 (Europe/Paris)
Evaluated result: Release 1.3.0; base commit 93d91da0ab383eb9642bf2507dab3f2e0c64d468 plus the working-tree sources in validation/SOURCE-014.sha256 (manifest SHA-256 F10B70786A6364BB361D8CB293F2A5BC0A20B55750D0626BC9EF5E9440E198A6).
Result: PASS.
Evidence: ModernizationTests and ModernizationViewModelTests verify raw transfer, inherited language without redundant detection, successive generations, preserved targets, obsolete callbacks, result gating and actual-provider identity. Existing guarded Replace and inert Markdown tests pass. See IMPLEMENTATION-014.md and validation/build-014-release.log, validation/test-014-candidate.log, validation/publish-014.log.

Test history: None.

## G-014-004 — Real Windows OpenAI account journey

Type: HUMAN
Status: TO TEST
Defined at: 2026-10-18T00:00:00+02:00
Requirements: REQ-014-001 through REQ-014-005, REQ-014-011.

Condition:
On the identified published TextAid candidate, a named user accepts the independently activated fourth Settings connection, browser/MFA return, accurate progress and account permission state, offered model selection and one explicitly triggered synthetic transformation through TextAid's production MAF path. Restart retains the protected session/model. Cancellation, sign-out/reconnect and actionable failure guidance are reviewed; expiry/permission/quota behavior may be simulated and must be identified as such. Generic compatible API-key access still works and its configuration remains intact. Review the current official contract and TextAid distribution eligibility; record any unresolved incompatibility as a finding rather than claiming live success.

Method:
Use synthetic text only. Record contract references/date, the tested app identity and the user's named, dated acceptance without tokens, callbacks or private account data. SmallBazar success or model listing alone cannot satisfy this gate.

Validated at:
Validated by:
Evaluated result:
Comment:

Test history: None.

## G-014-005 — Windows transfer, selector and indicator walkthrough

Type: HUMAN
Status: TO TEST
Defined at: 2026-10-18T00:00:00+02:00
Requirements: REQ-014-005 through REQ-014-011.

Condition:
A named user accepts the filled left-pointing triangle between the panes, hover color, lack of visible button chrome, readability, resize behavior and keyboard/accessibility operation. With a successful plain-text and Markdown output, transfer then run a different action; confirm correct intermediate text, inherited result language without another detection call, no automatic transformation, and safe final Replace/Copy/Cancel. Repeat from quick Translate and a session without a captured target. Accept the new distinguishable sign-in color/symbol and actual-provider tooltip.

Verify selector hidden with zero/one profile, visible on Default with multiple profiles, unambiguous connection/model labels, sign-in preference on Default, exact key/local override and systematic reset after success and failure/cancellation. Confirm result/indicator survive reset and English/localized strings remain coherent with stale-cache fallback.

Method:
Walk through the published EXE with synthetic text in a disposable source editor. Record the glyph/theme tokens and screenshots or concise observations. Retain the 1.2.13 focus correction; do not infer compatibility with every application from this walkthrough. Separate-machine portability remains recorded under R-012.

Validated at:
Validated by:
Evaluated result:
Comment:

Test history: None.

## G-014-006 — Regression build and canonical publication

Type: AUTO
Status: PASS
Defined at: 2026-10-18T00:00:00+02:00
Requirements: REQ-014-011; all requirements' regression baseline.

Condition:
Solution builds and applicable regression tests pass. Root publish.ps1 produces the sole self-contained Windows x64 TextAid.exe at the canonical location, with identifiable version/hash and embedded required dependencies. No new plaintext credentials, untracked UI literals or bypass of the AI boundary is introduced.

Method:
Before the first restore/build/test, create `src/TextAid.App/obj/NuGetScratch` and set NUGET_SCRATCH to its absolute path. Run `dotnet build TextAid.sln -m:1 -p:NuGetAudit=false` and `dotnet test TextAid.sln -m:1 -p:NuGetAudit=false` in the appropriate configuration, then root `publish.ps1`. Record source identity, summaries, executable version and SHA-256. Perform focused source/resource review for localization, secret handling and architectural boundaries. Use root start.ps1 for any launch from Codex.

Tested at: 2026-10-18 (Europe/Paris)
Evaluated result: Cosmetic 1.3.2 source identified by validation/SOURCE-014-1.3.2.sha256.
Result: PASS.
Evidence: Settings / Sign in with OpenAI now uses an explicit DisplayName DataTemplate for the account model ComboBox, including its selected resting display. SelectedValuePath remains Slug, preserving the inference model identity. Release app build: zero warnings/errors. Root publish.ps1 produced FileVersion 1.3.2.0, SHA-256 DA7AF13F2DB14C5B5C1D7EAE19A9BDA71D4FA18806C677C99A2B331A2616ED35. No functional changes or new tests; prior 232-test evidence remains applicable. Build/publication logs and source manifest are retained in validation/ with the 1.3.2 suffix. App left stopped.

Test history:

### Superseded 1.3.1 evaluation

Tested at: 2026-10-18 (Europe/Paris)
Evaluated result: Cosmetic 1.3.1 source identified by validation/SOURCE-014-1.3.1.sha256 (manifest SHA-256 F9EE90ED59D48BF7BD526C483BF9D730D72B3FE9D21C750CF14B50462CF7EC2F).
Result: PASS.
Evidence: Release build zero warnings/errors; root publish.ps1 succeeded. Sole published executable FileVersion 1.3.1.0, SHA-256 622E13C1A0FC1C6384ABCC5681225DAF7109E41F5AF97EC82ACC16B0768ABD6A. See validation/build-014-1.3.1.log and validation/publish-014-1.3.1.log. Previous 232-test Release result remains applicable to unchanged functional code; tests were not rerun for the cosmetic XAML binding correction. Previous runtime startup and separate-machine deferral remain recorded for 1.3.0; 1.3.1 was left stopped.


### Superseded 1.3.0 evaluation

Tested at: 2026-10-18 (Europe/Paris)
Evaluated result: Cosmetic 1.3.2 source identified by validation/SOURCE-014-1.3.2.sha256.
Result: PASS.
Evidence: Settings / Sign in with OpenAI now uses an explicit DisplayName DataTemplate for the account model ComboBox, including its selected resting display. SelectedValuePath remains Slug, preserving the inference model identity. Release app build: zero warnings/errors. Root publish.ps1 produced FileVersion 1.3.2.0, SHA-256 DA7AF13F2DB14C5B5C1D7EAE19A9BDA71D4FA18806C677C99A2B331A2616ED35. No functional changes or new tests; prior 232-test evidence remains applicable. Build/publication logs and source manifest are retained in validation/ with the 1.3.2 suffix. App left stopped.
