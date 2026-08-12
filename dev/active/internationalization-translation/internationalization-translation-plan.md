<!-- ABOUTME: API/TMS-primary implementation plan for ISLAMU localization and translation support. -->
<!-- ABOUTME: Re-baselines MasterCode-keyed translation through Tolgee/Weblate with static bundles as self-host fallback. -->
# Internationalization Translation — Implementation Plan

Last Updated: 2026-07-09 Europe/Brussels

## 0. Planning Metadata
- **Request:** Correct the existing plan so the normal translation path is API-backed and TMS-connected, using Tolgee or Weblate to resolve translations by lookup `MasterCode`. Static JSON bundles remain only for self-hosters that do not connect a TMS and for graceful fallback when live providers fail.
- **Task directory:** `dev/active/internationalization-translation/`
- **Planning status:** Re-baselined after user architecture correction.
- **Matched intents:** No direct localization/TMS intent exists in `.claude/contract/intents.yaml`.
- **Fallback contract:** `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `/dev-docs`, `senior-cto-feedback`, relevant path rules, and the closest implementation intents.
- **Related implementation intents:** `add-get-endpoint`, `add-write-endpoint`, `add-cqrs-handler`, `update-repository-query`, `openapi-contract-change`, `blazor-component-affordance`, `external-infrastructure-bootstrap`.
- **Intent-derived must-read docs:** `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/BLAZOR.md`, `docs/CONFIGURATION.md`, `docs/MULTI_TENANCY.md`, `docs/OPERATIONS.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/CODEBASE_STRUCTURE.md`, `docs/LOCALIZATION.md`.
- **Relevant skills:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`, `blazor-bff-patterns`, `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `accessibility`, `error-tracking`, `aspire`, `senior-cto-feedback`.
- **Relevant rules:** `.claude/rules/api-controllers.md`, `.claude/rules/blazor-client.md`, `.claude/rules/blazor-server.md`, `.claude/rules/domain.md`, `.claude/rules/tests.md`.
- **Paths in scope:** `Explore.Domain/Common/Localization/**`, `Explore.Domain/Constants/GovernanceSettingKeys.cs`, `Explore.Domain/Enums/TranslationManagementProviderEnum.cs`, lookup entities/DTOs exposing `MasterCode`, `Explore.Application/Contracts/Infrastructure/ITranslation*.cs`, `Explore.Application/Contracts/Infrastructure/IBundleFileWriter.cs`, `Explore.Application/DTOs/Localization/**`, `Explore.Application/Features/Localization/**`, `Explore.Application/Telemetry/TranslationMetrics.cs`, `Explore.Infrastructure/Localization/**`, `Explore.API/Controllers/*Translation*.cs`, `Explore.API/Controllers/LocalizationAdminController.cs`, `Explore.API/Program.cs`, `Explore.Blazor/**`, `Explore.Blazor.Client/**`, `Explore.Infrastructure/Localization/Bundles/**`, relevant tests/docs/dev docs.
- **Minimum tests:** `dotnet build --configuration Release --verbosity quiet`, `Event.Architecture.Tests`, and targeted Domain/Application/Infrastructure/API/Blazor test projects based on changed files.
- **Docs to update during implementation:** `docs/LOCALIZATION.md`, `docs/CONFIGURATION.md`, `docs/API.md`, `docs/BLAZOR.md`, `docs/OPERATIONS.md`, `docs/DEPLOYMENT_MODES.md`, this workstream's plan/context/tasks.
- **Unique acceptance:** ISLAMU Event-hosted and connected instances resolve translations through the ISLAMU Event API and live Tolgee/Weblate provider path; lookup translations are keyed from `MasterCode` using `lookup.{entity_type}.{master_code}.{field}`; UI strings use `ui.*`; TMS credentials remain server-side; provider endpoints/auth match current Context7 docs; static bundles only serve `tms_provider=None` self-hosting and provider-failure fallback; no DB translation table and no unreleased compatibility shims.
- **Forbidden without approval:** DB translation tables, browser-visible TMS secrets, repository DTO returns, injected validators, compatibility shims for unreleased DTO/client/provider shapes, disabling tenant isolation, translating by database ID or localized label instead of `MasterCode`, generic new abstractions when existing provider/resolver/writer seams fit.
- **Primary layers touched:** Domain / Application / Infrastructure / API / Blazor / Docs / DevOps / Tests.
- **Estimated complexity:** L. The primary path crosses API, Application, Infrastructure providers, generated clients, Blazor cache, tenant config, TMS credentials, and provider-specific HTTP semantics. Static bundles still need hardening, but they are fallback/offline support rather than the main hosted runtime path.

## 1. Executive Summary
The repository is not greenfield for localization. It already has a culture registry, governance keys, translation contracts, runtime/offline/Tolgee/Weblate providers, bundle writer, public/admin controllers, Blazor translation services, a language picker, startup localization middleware, embedded bundles, metrics, and some tests. The plan is to correct and harden that existing stack around the intended runtime hierarchy.

The corrected architecture is **API/TMS-primary**. Blazor and other clients obtain translations through the ISLAMU Event API. The API routes through `ITranslationResolver` and `RuntimeTranslationProvider`. In ISLAMU Event-hosted or connected self-hosted mode, `tms_provider=Tolgee|Weblate` makes Infrastructure call the configured TMS APIs using server-side secrets. Lookup translations are addressed by stable lookup `MasterCode` keys such as `lookup.tag.FIQH.full_name`; UI strings use `ui.{area}.{component}.{element}`.

Static JSON bundles are still important, but they are not the primary hosted design. They are the offline provider for self-hosters that do not connect Tolgee/Weblate (`tms_provider=None`) and the graceful fallback when a connected provider fails. Static bundles must mirror the same key convention so disconnected mode behaves like connected mode.

Out of scope: adding a DB translation table, adding providers beyond Tolgee/Weblate, embedding translations into federation protocol records, preserving old draft endpoint/client shapes, and regional culture support beyond the verified `en`, `fr`, `ar` v1 allowlist unless explicitly approved.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log
| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| Current localization docs make the TMS the source of truth, live or offline. | Verified: `docs/LOCALIZATION.md` | High | States there is no translations table and TMS/offline bundles are source of truth. |
| Lookup translation keys use `MasterCode`. | Verified: `docs/LOCALIZATION.md` key convention `lookup.{entity_type}.{master_code}.{field}` | High | Examples include `lookup.tag.FIQH.full_name`. |
| Application tests already exercise MasterCode-shaped keys. | Verified by search: `Event.Application.UnitTests/Features/Localization/GetTranslationsQueryHandlerTests.cs` uses `lookup.tag.FIQH.full_name` and `lookup.madhab.HANAFI.full_name` | High | Confirms convention is not only documentation. |
| Culture allowlist exists. | Verified: `Explore.Domain/Common/Localization/CultureRegistry.cs` | High | `en`, `fr`, `ar`; two-letter codes only. |
| Localization governance keys exist. | Verified: `Explore.Domain/Constants/GovernanceSettingKeys.cs` | High | Provider/default/fallback/picker/offline keys exist. |
| Provider enum exists. | Verified: `Explore.Domain/Enums/TranslationManagementProviderEnum.cs` | High | `None`, `Tolgee`, `Weblate`. |
| Application localization contracts exist. | Verified: `ITranslationResolver.cs`, `ITranslationManagementProvider.cs`, `ITranslationConfigResolver.cs`, `IBundleFileWriter.cs` | High | Clean Architecture seam is already present. |
| Public translation API exists. | Verified: `Explore.API/Controllers/TranslationController.cs` | High | Anonymous `GET /api/Translation/{languageCode}` and languages endpoint. |
| Public translations are expected anonymous. | Verified by search: `Event.API.IntegrationTests/Features/EndpointAuthorizationMatrixTests.cs` includes `Matrix_Public_Translations_AnonymousOK` | High | Aligns with GET anonymous rule. |
| Admin localization API exists. | Verified: `Explore.API/Controllers/LocalizationAdminController.cs` | High | Authorized admin operations. |
| Governance DTO excludes raw secrets. | Verified: `Explore.Application/DTOs/Localization/UpdateLocalizationGovernanceDto.cs` | High | Secret path still needs implementation verification. |
| Runtime/offline/live providers exist. | Verified: `Explore.Infrastructure/Localization/*.cs` | High | Auth/endpoints/cache behavior need hardening. |
| Embedded static fallback bundles exist. | Verified: `Explore.Infrastructure/Localization/Bundles/en.json`, `fr.json`, `ar.json` | High | Offline provider foundation exists. |
| API and Blazor request localization are wired. | Verified: `Explore.API/Program.cs`, `Explore.Blazor/Program.cs`, `Explore.Blazor.Client/Program.cs` | High | Middleware and WASM culture bootstrap exist. |
| Blazor client translation hot path exists. | Verified: `Explore.Blazor.Client/Services/TranslationService.cs` | High | `T(key)` is in-memory/no I/O. |
| Language picker is tested. | Verified: `Explore.Blazor.Client.Tests/Components/LanguagePickerTests.cs` | High | Covers disabled/current/failure paths. |
| TMS resilience architecture test exists. | Verified: `Event.Architecture.Tests/LocalizationResilienceTests.cs` | High | Guards single retry-source design. |
| Tolgee docs require API key header and current export/import APIs. | Context7: `/tolgee/documentation` | High | `X-API-Key`; export/import endpoint shapes must be verified against current code. |
| Weblate docs require token auth and file download/upload APIs. | Context7: `/weblateorg/weblate` | High | `Authorization: Token`; `GET/POST /api/translations/{project}/{component}/{language}/file/`. |
| MudBlazor has culture/RTL support but no TMS abstraction. | Context7: `/websites/mudblazor` | Medium | Use ASP.NET/Blazor localization and project services. |
| ASP.NET Core localization baseline is request localization + localizers. | Context7: `/dotnet/aspnetcore.docs` | High | `UseRequestLocalization`, cultures/UI cultures, Blazor localization. |
| No existing active/pause workstream duplicates this. | Verified by directory listing: `dev/active`, `dev/pause` | High | New workstream is correct. |

### 2.2 Existing Implementation
- **Domain:** `CultureRegistry` owns the v1 language allowlist and RTL metadata. `GovernanceSettingKeys.Localization` and `TranslationManagementProviderEnum` define provider/config vocabulary. Lookup entities and generated DTOs broadly expose `MasterCode`, which is the stable translation identity.
- **Application:** Translation contracts, DTOs, validators, CQRS handlers, and `TranslationMetrics` already exist. `GetTranslationsQueryHandler` returns a language dictionary through `ITranslationManagementProvider`; tests already use MasterCode-shaped lookup keys.
- **Infrastructure:** `RuntimeTranslationProvider` chooses offline/Tolgee/Weblate, `TranslationResolver` caches tenant/language/mode/key results, `OfflineTranslationProvider` reads embedded/writable JSON bundles, `BundleFileWriter` writes local bundles atomically, and Tolgee/Weblate providers use Refit/HTTP clients with resilience registration.
- **API:** `TranslationController` exposes anonymous translation reads. `LocalizationAdminController` exposes authorized admin configuration, connection test, bundle health, governance update, and export-from-TMS operations.
- **Blazor/BFF/client:** Request localization and culture bootstrap exist. Client-side `TranslationService`, `LanguagePreferenceService`, `MudBlazorLocalizer`, `LocalizationAdminService`, `LocalizationAdminState`, and `LanguagePicker` exist.
- **Docs/tests:** `docs/LOCALIZATION.md` is detailed but provider endpoint examples may be stale relative to Context7. Architecture, Application, API authorization, and Blazor client tests cover important parts; provider and fallback coverage remains incomplete.

### 2.3 Existing Tests And Verification Coverage
- Verified: `Event.Application.UnitTests/Features/Localization/GetTranslationsQueryHandlerTests.cs` covers translation dictionaries with `lookup.*.{MasterCode}.*` keys.
- Verified: `Event.API.IntegrationTests/Features/EndpointAuthorizationMatrixTests.cs` covers anonymous public translation endpoints.
- Verified: `Event.Architecture.Tests/LocalizationResilienceTests.cs` protects provider resilience registration design.
- Verified: `Explore.Blazor.Client.Tests/Components/LanguagePickerTests.cs` protects picker behavior.
- Verified previously during planning creation: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 260 total, 259 succeeded, 1 skipped.
- Missing or not yet verified: end-to-end API/TMS MasterCode resolution tests, Tolgee/Weblate fake HTTP endpoint/auth tests, public API invalid-language tests, admin API no-secret tests, generated-client drift tests, embedded+writable static fallback merge tests.

### 2.4 Existing Documentation And Contracts
- `docs/LOCALIZATION.md` describes the intended localization architecture and explicitly makes TMS/offline bundles the translation source of truth, not a database table.
- `docs/API.md`, `docs/BLAZOR.md`, `docs/CONFIGURATION.md`, `docs/MULTI_TENANCY.md`, `docs/OPERATIONS.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, and `docs/CODEBASE_STRUCTURE.md` provide constraints for API, BFF, config, tenancy, ops, auth, and structure.
- Generated OpenAPI/NSwag artifacts exist in the repo but exact localization-admin client path/generation command must be verified before regeneration.

### 2.5 Current Pain Points / Improvement Areas
1. **Primary API/TMS MasterCode flow is under-specified in the plan.** The code/docs have the pieces, but the implementation tasks must explicitly protect `MasterCode`-keyed lookup translation end to end.
2. **Connected TMS authentication is likely incomplete.** Tolgee/Weblate comments/docs require headers, but the read provider code did not show header injection or a verified secret-provider path.
3. **Provider endpoint shapes need current-doc validation.** Context7 Tolgee docs emphasize current export/import file/body endpoints; Weblate docs prefer file download/upload. Current provider code may use stale or incomplete endpoint shapes.
4. **Runtime provider selection must prioritize connected TMS when configured.** Static bundles are fallback/offline, not the normal hosted path.
5. **Static fallback still needs hardening.** Current offline provider appears to prefer writable bundles wholesale, which can hide new embedded keys after upgrades.
6. **Export/cache invalidation may be incomplete.** Export invalidates resolver cache, but `OfflineTranslationProvider.InvalidateLanguage` was not verified as called after writes.
7. **All-tenant config invalidation is misleading.** `TranslationConfigResolver.InvalidateCache(null)` logs global invalidation but does not clear indexed tenant keys in verified evidence.
8. **Public API language validation is not verified.** The client validates culture codes, but public API remains the trust boundary.
9. **Generated client drift exists.** `LocalizationAdminState` uses `AdditionalProperties` and a temporary payload model pending client regeneration.
10. **Admin UI and BFF endpoint ownership need exact path verification.** Plan must not name unknown Razor/BFF files as existing until found.

### 2.6 Unknowns After Investigation
- Exact secret-provider service/interface for TMS API keys.
- Exact generated localization admin API interface path and NSwag generation command.
- Exact admin localization Razor component path.
- Actual test project(s) best suited for provider/API/static fallback tests.
- Whether connected provider reads should fetch live on demand, export whole-language dictionaries, or use provider-side bundle export per cache fill. Implementation should choose the smallest provider-specific approach that still satisfies API/TMS-primary behavior.
- Whether self-hosted HA in v1 requires a shared-volume-only contract or a concrete object-storage `IBundleFileWriter` implementation.

## 3. Proposed Future State

### 3.1 Primary hosted/connected flow
```text
Lookup rows expose stable MasterCode values
        ↓ key construction: lookup.{entity_type}.{master_code}.{field}
Blazor/client calls ISLAMU Event API: GET /api/Translation/{languageCode}
        ↓ TranslationController → GetTranslationsQuery
ITranslationResolver / RuntimeTranslationProvider
        ↓ tms_provider = Tolgee or Weblate
Tolgee/Weblate API call with server-side secret header
        ↓ normalize provider result to flat key/value dictionary
TranslationResolver tenant+language+provider cache
        ↓ API returns dictionary
Blazor TranslationService caches dictionary
        ↓ T(key) hot path: in-memory lookup only
```

This is the normal path for the ISLAMU-hosted instance and any self-hoster that connects Tolgee/Weblate. Static files are not the design center for this path; they are fallback artifacts using the same keys.

### 3.2 Offline self-host and fallback flow
```text
tms_provider = None OR connected provider fails
        ↓ RuntimeTranslationProvider selects/falls back to OfflineTranslationProvider
Embedded bundles: Explore.Infrastructure/Localization/Bundles/{lang}.json
Writable overrides: {ContentRoot}/App_Data/Localization/Bundles/{lang}.json
        ↓ validate flat key/value JSON using the same lookup/ui key convention
        ↓ merge embedded + writable per key, writable wins
TranslationResolver cache → API → Blazor cache
```

Self-hosters can run without Tolgee/Weblate by shipping or editing static bundles. Fallback mode should be visible in metrics/logs so ISLAMU-hosted operators know when live TMS is degraded.

## 4. Non-Negotiable Constraints
- Repositories return entities, never DTOs.
- Validators are manually instantiated.
- `int` for lookups, `Guid` for aggregates, `long` for cursors.
- GET endpoints are `[AllowAnonymous]`; write/admin endpoints are `[Authorize]`.
- UI action affordances are gated by HAL `_links` where resource affordances are exposed.
- Tenant isolation is API-authoritative and fail-closed.
- Domain has no external dependencies; Application owns contracts; Infrastructure implements providers; API/Blazor compose.
- Every new file starts with two `ABOUTME:` lines.
- No backward-compatibility shims for unreleased draft DTO/client/provider shapes.
- Localization hot path must not do network I/O, disk I/O, logging storms, or per-key metrics.
- `MasterCode` is the canonical lookup translation identity. Do not translate by database IDs, localized labels, or mutable display text.
- Hosted/connected mode uses Tolgee/Weblate as the normal provider path. Static bundles are only `tms_provider=None` self-host support and fallback.
- Provider API calls must match current Context7-backed Tolgee/Weblate documentation; stale draft endpoint shapes are deleted, not supported in parallel.
- TMS secrets never reach browser/WASM, generated clients, logs, metrics, ProblemDetails, support bundles, or OpenAPI examples.

## 5. Architecture And Design Decisions
### Decision 1: Complete the existing stack instead of replacing it
- **Why:** Verified code already spans all layers and matches most of the report.
- **Alternatives considered:** Greenfield rewrite; DB translation table; `.resx` only.
- **Consequences:** Smaller implementation diff, less churn, and targeted hardening.
- **Files/layers affected:** Existing localization paths across Domain, Application, Infrastructure, API, Blazor, docs, tests.

### Decision 2: API/TMS is the primary runtime path
- **Why:** The corrected product requirement is that users get translations through the ISLAMU Event API, and hosted ISLAMU Event uses Tolgee/Weblate behind that API.
- **Alternatives considered:** Static-bundle-first runtime; DB lookup translation rows; browser-side TMS calls.
- **Consequences:** Provider auth, provider endpoints, cache fill behavior, API tests, and metrics must prove live TMS mode works first.
- **Files/layers affected:** `TranslationController`, `GetTranslationsQueryHandler`, `ITranslationResolver`, `RuntimeTranslationProvider`, Tolgee/Weblate providers, Blazor `TranslationService`, tests.

### Decision 3: Lookup translations are keyed by `MasterCode`
- **Why:** Lookup IDs are persistence details; localized labels are mutable. `MasterCode` is stable and already appears in docs, DTOs, and tests.
- **Alternatives considered:** Numeric lookup IDs; slugified translated labels; per-table translation DTOs.
- **Consequences:** Key construction and tests must use `lookup.{entity_type}.{master_code}.{field}`; implementation should add/verify helpers only if existing code lacks one.
- **Files/layers affected:** Lookup DTO mapping, translation key constants/helpers if needed, provider tests, docs.

### Decision 4: Static bundles are offline/fallback mirrors, not the hosted source of truth
- **Why:** Self-hosters need zero-dependency operation, and connected providers need graceful degradation, but this must not invert the hosted architecture.
- **Alternatives considered:** Static bundles as canonical primary exchange; no static fallback.
- **Consequences:** Static bundle tasks remain, but after API/TMS contract work. Bundles use the same keys and must merge embedded+writable safely.
- **Files/layers affected:** `OfflineTranslationProvider`, `BundleFileWriter`, fallback tests/docs.

### Decision 5: Use current Tolgee/Weblate APIs from Context7
- **Why:** Connected mode must actually call Tolgee/Weblate. Context7 shows provider-specific auth and file/export endpoints that must be tested directly.
- **Alternatives considered:** Keep current provider endpoints if convenient; support old/new endpoint shapes in parallel.
- **Consequences:** Existing provider code may be replaced rather than shimmed. Fake HTTP tests must assert exact route, method, headers, and payload shape.
- **Files/layers affected:** `TolgeeTranslationProvider`, `WeblateTranslationProvider`, resilience registration/tests, docs.

### Decision 6: Keep TMS API keys server-side through the secret-provider flow
- **Why:** `UpdateLocalizationGovernanceDto` explicitly excludes API keys, and BFF/security docs prohibit browser-exposed secrets.
- **Alternatives considered:** Governance setting value; admin DTO field; environment-only key.
- **Consequences:** Need to reuse or add the minimum secret-provider integration and inject headers at HTTP-call time.
- **Files/layers affected:** Infrastructure providers, secret-management integration, admin API/UI, docs.

### Decision 7: Validate culture at the API boundary
- **Why:** Client validation is not a trust boundary. Public translation endpoints should avoid arbitrary cache/provider keys.
- **Alternatives considered:** Let provider return empty; normalize only in Infrastructure.
- **Consequences:** Predictable error behavior and bounded cache key cardinality.
- **Files/layers affected:** Translation query/controller/handler tests.

### Decision 8: Regenerate API clients and remove temporary payload drift
- **Why:** `AdditionalProperties` and temporary payload models are development-mode shims; the user explicitly rejected backward compatibility.
- **Alternatives considered:** Keep compatibility layer.
- **Consequences:** Cleaner client code; requires OpenAPI/NSwag verification.
- **Files/layers affected:** Generated API client, `LocalizationAdminState`, `LocalizationGovernancePayload`, admin service/tests.

## 6. Implementation Phases
### Phase 0: Review And Baseline
- **Goal:** Confirm this corrected hierarchy and current repo state before implementation edits.
- **Depends on:** User review.
- **Relevant files:** `dev/active/internationalization-translation/*`, docs listed above.
- **Related skills/rules:** `/dev-docs`, `senior-cto-feedback`, clean architecture, tests.
- **Acceptance criteria:** User approves/corrects scope; implementation agent refreshes current branch status and re-reads this plan/context/tasks.
- **Verification:** No code changes; docs remain coherent.
- **Rollback / failure handling:** Update these docs with user corrections.

#### Task 0.1: Add a localization intent proposal
- **Type:** docs/investigate
- **Layer:** Docs
- **Files:** `.claude/contract/intents.yaml` (existing, optional future change), `dev/active/internationalization-translation/*` (existing)
- **Description:** Decide whether a recurring localization/TMS intent should be added after implementation scope is clear.
- **Acceptance Criteria:** Decision recorded; if added, architecture context tests pass.
- **Dependencies:** User approval if changing contract files.
- **Effort:** S
- **Required Skills/Rules:** clean-architecture-rules, tests
- **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

### Phase 1: API/TMS MasterCode Resolution Contract
- **Goal:** Make the normal API translation path explicit, tested, and keyed by stable lookup `MasterCode` values.
- **Depends on:** Phase 0.
- **Relevant files:** `docs/LOCALIZATION.md`, lookup DTO/entity files exposing `MasterCode`, `ITranslationResolver.cs`, `GetTranslationsQueryHandler.cs`, `TranslationController.cs`, `TranslationResolver.cs`, `RuntimeTranslationProvider.cs`, `TranslationService.cs`, existing localization tests.
- **Related skills/rules:** clean architecture, cqrs-mediatr, api-controllers, blazor-client.
- **Acceptance criteria:** Translation keys for lookup data are generated/documented from `MasterCode`; the API returns provider-backed dictionaries for supported languages; tests prove the API/TMS path is primary when provider is configured.
- **Verification:** Application handler tests, API integration tests, build.
- **Rollback / failure handling:** Keep current public translation endpoint behavior while tightening tests/contracts.

#### Task 1.1: Verify and codify MasterCode translation key construction
- **Type:** investigate/modify/test/docs
- **Layer:** Domain / Application / Docs
- **Files:** `docs/LOCALIZATION.md` (existing), lookup DTO/entity files to verify, optional translation key helper file only if existing patterns need one
- **Description:** Locate current MasterCode ownership for translated lookup rows and document the exact key rules for lookup and UI strings.
- **Acceptance Criteria:** `lookup.{entity_type}.{master_code}.{field}` is the documented and tested lookup key shape; no plan/task translates lookup rows by database ID or localized label.
- **Dependencies:** 0.1
- **Effort:** M
- **Required Skills/Rules:** clean-architecture-rules, cqrs-mediatr-guidelines
- **Validation:** Focused Application/Domain tests if helper code changes; docs review.

#### Task 1.2: Prove API translation reads use the provider path first
- **Type:** modify/test
- **Layer:** Application / API / Infrastructure
- **Files:** `GetTranslationsQueryHandler.cs` (existing), `TranslationController.cs` (existing), `RuntimeTranslationProvider.cs` (existing), tests (existing/new)
- **Description:** Add/verify tests that `GET /api/Translation/{languageCode}` returns dictionary values from configured Tolgee/Weblate provider path before static fallback.
- **Acceptance Criteria:** Connected provider configured test returns provider values; provider failure test falls back to offline bundle; `tms_provider=None` test uses offline provider directly.
- **Dependencies:** 1.1
- **Effort:** M
- **Required Skills/Rules:** api-controllers, cqrs-mediatr-guidelines, clean-architecture-rules
- **Validation:** Handler/API integration tests; build.

#### Task 1.3: Validate language codes at the public translation API boundary
- **Type:** modify/test
- **Layer:** API / Application
- **Files:** `Explore.API/Controllers/TranslationController.cs` (existing), `GetTranslationsQuery.cs` (existing), `GetTranslationsQueryHandler.cs` (existing), tests (new/existing)
- **Description:** Normalize or reject invalid language codes with `CultureRegistry`, returning predictable validation/problem results instead of arbitrary provider/cache keys.
- **Acceptance Criteria:** `en`, `fr`, `ar` succeed; unsupported or malformed code returns controlled error; cache key cardinality is bounded.
- **Dependencies:** 1.1
- **Effort:** S
- **Required Skills/Rules:** api-controllers, cqrs-mediatr-guidelines
- **Validation:** API integration or handler tests; build.

#### Task 1.4: Keep Blazor translation consumption API-backed
- **Type:** verify/modify/test
- **Layer:** Blazor Client
- **Files:** `Explore.Blazor.Client/Services/TranslationService.cs` (existing), `MudBlazorLocalizer.cs` (existing), Blazor client tests
- **Description:** Ensure the client continues to fetch translations from the ISLAMU Event API and only uses in-memory cached dictionaries on the `T(key)` hot path.
- **Acceptance Criteria:** No browser-side Tolgee/Weblate calls; no browser-side TMS secrets; `T(key)` remains synchronous/in-memory; cache refresh uses API client.
- **Dependencies:** 1.2
- **Effort:** S
- **Required Skills/Rules:** blazor-client, blazor-bff-patterns, auth-patterns
- **Validation:** `Explore.Blazor.Client.Tests`; code inspection.

### Phase 2: Tolgee/Weblate Provider Contracts And Secrets
- **Goal:** Make connected mode authenticate and call current documented Tolgee/Weblate APIs for the API/TMS-primary flow.
- **Depends on:** Phase 1.
- **Relevant files:** `TolgeeTranslationProvider.cs`, `WeblateTranslationProvider.cs`, `RuntimeTranslationProvider.cs`, secret-provider paths to verify, resilience tests/docs.
- **Related skills/rules:** auth-patterns, clean-architecture-rules, error-tracking, aspire.
- **Acceptance criteria:** Tolgee/Weblate use server-side secrets; fake HTTP tests assert current documented methods/routes/headers/payloads; provider failures fall back to static bundles with safe metrics/logs.
- **Verification:** Provider tests with fake HTTP handlers; build; architecture tests.
- **Rollback / failure handling:** `ForceOfflineMode` and provider failure fallback remain safe.

#### Task 2.1: Verify existing secret-provider abstractions
- **Type:** investigate
- **Layer:** Infrastructure / API / Blazor
- **Files:** existing secret-management paths to locate; `docs/CONFIGURATION.md`; `docs/SECURITY-MODEL.md`
- **Description:** Find the repo-standard way to store/retrieve application-managed secrets and select the minimum integration point for `Secret_TmsApiKey` or equivalent.
- **Acceptance Criteria:** Exact service/interface/files recorded in context; no duplicate secret abstraction if an existing one fits.
- **Dependencies:** 0.1
- **Effort:** M
- **Required Skills/Rules:** auth-patterns, clean-architecture-rules
- **Validation:** Read evidence added to context/tasks.

#### Task 2.2: Inject provider auth headers from server-side secret flow
- **Type:** modify/test
- **Layer:** Infrastructure
- **Files:** `TolgeeTranslationProvider.cs` (existing), `WeblateTranslationProvider.cs` (existing), selected secret service files (existing/new)
- **Description:** Resolve the configured TMS API key/token server-side and attach provider-specific headers at HTTP-call time.
- **Acceptance Criteria:** Tolgee requests include `X-API-Key`; Weblate requests include `Authorization: Token`; missing key causes clear admin/test failure and runtime offline fallback; logs/metrics never include key.
- **Dependencies:** 2.1
- **Effort:** M
- **Required Skills/Rules:** error-tracking, auth-patterns
- **Validation:** Targeted provider tests; build.

#### Task 2.3: Align Tolgee export/import/read behavior with current Context7 docs
- **Type:** modify/test/docs
- **Layer:** Infrastructure
- **Files:** `TolgeeTranslationProvider.cs` (existing), Tolgee DTOs/contracts inside localization provider files (existing/new), tests/docs
- **Description:** Replace/verify Tolgee calls against current docs: API key auth, JSON export file/stream endpoint (`/v2/projects/export?...format=JSON...`) and/or keys export endpoint, and import via `/v2/projects/import/files` or `/v2/projects/{projectId}/single-step-import-resolvable` depending on final chosen provider flow.
- **Acceptance Criteria:** Fake HTTP tests assert route/method/query/header/body; exported Tolgee data normalizes to flat `lookup.*`/`ui.*` dictionary; import sends compatible data without unsupported draft endpoint assumptions; no parallel stale endpoint support remains.
- **Dependencies:** 1.1, 2.2
- **Effort:** L
- **Required Skills/Rules:** clean-architecture-rules, error-tracking
- **Validation:** Provider tests with sample Tolgee payload/zip/file stream; docs updated.

#### Task 2.4: Align Weblate export/import/read behavior with current Context7 docs
- **Type:** modify/test/docs
- **Layer:** Infrastructure
- **Files:** `WeblateTranslationProvider.cs` (existing), Weblate DTOs/contracts inside localization provider files (existing/new), tests/docs
- **Description:** Replace/verify Weblate calls against current docs: token auth, file download `GET /api/translations/{project}/{component}/{language}/file/` and file upload `POST /api/translations/{project}/{component}/{language}/file/`; keep unit creation only if explicitly needed for deltas.
- **Acceptance Criteria:** Fake HTTP tests assert route/method/query/header/body; project/component/language slugs are validated/configured; file upload conflict/add/fuzzy behavior is documented; exported file normalizes to flat `lookup.*`/`ui.*` dictionary.
- **Dependencies:** 1.1, 2.2
- **Effort:** L
- **Required Skills/Rules:** clean-architecture-rules, error-tracking
- **Validation:** Provider tests with sample Weblate JSON/file upload; docs updated.

#### Task 2.5: Verify fallback, retry, and metrics behavior for connected providers
- **Type:** modify/test
- **Layer:** Infrastructure / Application
- **Files:** `RuntimeTranslationProvider.cs` (existing), `TranslationMetrics.cs` (existing), `TestTmsConnectionCommandHandler.cs` (existing), resilience files/tests
- **Description:** Confirm provider failures, rate limits, auth failures, and malformed payloads activate offline fallback and record safe metrics/logs.
- **Acceptance Criteria:** `islamu.tms.fallback_activated_total` records provider/reason; connection tests record success/failure; no provider response bodies/secrets in tags/logs.
- **Dependencies:** 2.3, 2.4
- **Effort:** M
- **Required Skills/Rules:** error-tracking
- **Validation:** Runtime/provider tests; architecture resilience tests.

### Phase 3: Static Bundle Fallback For No-TMS Self-Hosters
- **Goal:** Make offline/no-TMS mode robust without letting it become the primary hosted architecture.
- **Depends on:** Phase 1; can run partly parallel with Phase 2 after MasterCode key rules are fixed.
- **Relevant files:** `Explore.Infrastructure/Localization/Bundles/*.json`, `OfflineTranslationProvider.cs`, `BundleFileWriter.cs`, `IBundleFileWriter.cs`, admin API/UI files to verify, docs.
- **Related skills/rules:** clean architecture, cqrs-mediatr, blazor-ui-conventions, accessibility, error-tracking.
- **Acceptance criteria:** `tms_provider=None` works without external services; bundles use the same MasterCode/UI key scheme as live providers; embedded defaults survive writable overrides; same-process writes are immediately visible; operator storage modes are documented.
- **Verification:** Static bundle/provider tests; admin API tests if endpoints are added; build.
- **Rollback / failure handling:** Embedded bundles remain fallback; malformed writable files do not hide embedded defaults.

#### Task 3.1: Formalize and test the fallback bundle schema
- **Type:** modify/test/docs
- **Layer:** Infrastructure / Docs
- **Files:** `Explore.Infrastructure/Localization/Bundles/*.json` (existing), `docs/LOCALIZATION.md` (existing), provider tests (existing/new after project verification)
- **Description:** Define the flat JSON dictionary schema as a mirror of TMS keys, including `lookup.{entity_type}.{master_code}.{field}` and `ui.*` keys, deterministic ordering/formatting, language filename rules, duplicate/malformed-key behavior, and validation expectations.
- **Acceptance Criteria:** Bundle schema is documented; `en/fr/ar` bundles validate; malformed JSON/key shapes fail controlled tests; output order is deterministic.
- **Dependencies:** 1.1
- **Effort:** M
- **Required Skills/Rules:** clean-architecture-rules, tests
- **Validation:** Focused static bundle tests; `dotnet build --configuration Release --verbosity quiet`.

#### Task 3.2: Merge embedded and writable bundles key-by-key
- **Type:** modify/test
- **Layer:** Infrastructure
- **Files:** `Explore.Infrastructure/Localization/OfflineTranslationProvider.cs` (existing), provider tests (existing/new)
- **Description:** Load embedded bundle and writable bundle, merge by key with writable values overriding embedded values, and return the merged dictionary.
- **Acceptance Criteria:** Missing local key falls back to embedded; local override wins; malformed local bundle falls back safely without hiding embedded keys.
- **Dependencies:** 3.1
- **Effort:** M
- **Required Skills/Rules:** clean-architecture-rules, error-tracking
- **Validation:** Focused provider tests plus build.

#### Task 3.3: Add direct static bundle import/export for no-TMS operators
- **Type:** modify/test
- **Layer:** Application / API / Blazor
- **Files:** `IBundleFileWriter.cs` (existing), `BundleFileWriter.cs` (existing), `LocalizationAdminController.cs` (existing), admin UI/service files (existing, exact paths verify)
- **Description:** Give authorized self-host operators a direct path to upload/import and download/export static JSON bundles without configuring Tolgee/Weblate.
- **Acceptance Criteria:** Admin can validate/import/export `en/fr/ar` bundle files; invalid bundle returns safe ProblemDetails/command result; raw file contents are not logged; no TMS config is required.
- **Dependencies:** 3.1
- **Effort:** L
- **Required Skills/Rules:** api-controllers, blazor-client, blazor-server, accessibility
- **Validation:** API/admin tests; Blazor client tests if UI changes; manual admin smoke during implementation.

#### Task 3.4: Invalidate bundle caches after any fallback/TMS write
- **Type:** modify/test
- **Layer:** Application / Infrastructure
- **Files:** `ExportFromTmsCommandHandler.cs` (existing), `OfflineTranslationProvider.cs` (existing), `TranslationResolver.cs` (existing), static import/export handlers/endpoints (new/existing)
- **Description:** Ensure every bundle write clears both resolver cache and offline provider cache for the language.
- **Acceptance Criteria:** Same-process import/export followed by translation read returns newly written values without app restart.
- **Dependencies:** 3.2, 3.3
- **Effort:** S
- **Required Skills/Rules:** cqrs-mediatr-guidelines, clean-architecture-rules
- **Validation:** Handler/provider test.

#### Task 3.5: Make bundle storage mode explicit for self-hosters
- **Type:** docs/config/test
- **Layer:** Infrastructure / DevOps / Docs
- **Files:** `BundleFileWriter.cs` (existing), `docs/LOCALIZATION.md`, `docs/CONFIGURATION.md`, `docs/DEPLOYMENT_MODES.md`, deployment docs/compose/AppHost files if changed
- **Description:** Document and, if needed, configure the writable bundle path for single-replica local disk and shared-volume self-hosting. Keep object-store writer deferred unless deployment requires it.
- **Acceptance Criteria:** Operator docs state where files live, backup/restore expectations, single-replica vs shared-volume behavior, and health-check meaning.
- **Dependencies:** 3.3
- **Effort:** M
- **Required Skills/Rules:** aspire, error-tracking
- **Validation:** Bundle health check and docs review.

### Phase 4: API Boundary, OpenAPI, And Client Drift
- **Goal:** Harden public/admin contracts and remove development-only client drift.
- **Depends on:** Phases 1-3 if DTOs change.
- **Relevant files:** `TranslationController.cs`, localization queries/validators, `LocalizationAdminController.cs`, generated client files, `LocalizationAdminState.cs`, `LocalizationGovernancePayload.cs`.
- **Related skills/rules:** api-controllers, cqrs-mediatr-guidelines, blazor-client.
- **Acceptance criteria:** API validates cultures; admin endpoints are documented/generated; generated client matches server DTOs; temporary payload/additional-property bridge removed.
- **Verification:** API integration/OpenAPI generation tests; Blazor client tests.
- **Rollback / failure handling:** Since pre-v1, fix generated client directly rather than compatibility shims.

#### Task 4.1: Add or align admin contracts for TMS and fallback bundle operations
- **Type:** modify/test
- **Layer:** API / Application / Blazor
- **Files:** `LocalizationAdminController.cs` (existing), localization DTOs/handlers (existing/new), `LocalizationAdminService.cs` (existing), admin UI state (existing)
- **Description:** Expose authorized operations for connection test, provider config status, TMS import/export, and static fallback bundle import/export/validate through server-owned contracts.
- **Acceptance Criteria:** OpenAPI shows intended operations; endpoints are `[Authorize]`; file payload limits/validation/errors are explicit; no raw file content or secrets in logs/responses.
- **Dependencies:** 2.2, 3.3
- **Effort:** M
- **Required Skills/Rules:** api-controllers, blazor-bff-patterns
- **Validation:** API integration tests; generated client tests.

#### Task 4.2: Verify OpenAPI/NSwag generation workflow
- **Type:** investigate
- **Layer:** API / Blazor
- **Files:** repo OpenAPI/NSwag config files to locate; generated client files to locate
- **Description:** Locate the exact generation command and checked-in generated file ownership before regeneration.
- **Acceptance Criteria:** Command and generated paths recorded in context; no stale assumptions.
- **Dependencies:** 4.1
- **Effort:** S
- **Required Skills/Rules:** api-controllers, blazor-client
- **Validation:** Generation command identified.

#### Task 4.3: Regenerate and align localization admin client
- **Type:** modify/delete/test
- **Layer:** API / Blazor
- **Files:** generated client files, `LocalizationAdminState.cs` (existing), `LocalizationGovernancePayload.cs` (existing, likely delete), admin service/tests (existing)
- **Description:** Regenerate checked-in API client, replace `AdditionalProperties` reads with typed properties, and remove temporary client payload shims.
- **Acceptance Criteria:** Admin state uses typed generated DTOs; temporary shim removed unless generator limitation is verified; tests updated.
- **Dependencies:** 2.2, 4.2
- **Effort:** M
- **Required Skills/Rules:** blazor-client, api-controllers
- **Validation:** NSwag generation; `Explore.Blazor.Client.Tests`; build.

### Phase 5: Admin UI, BFF Preference, And Accessibility Completion
- **Goal:** Ensure operator/user-facing surfaces are discoverable, safe, and accessible.
- **Depends on:** Phase 4.
- **Relevant files:** `LanguagePicker.razor`, `LocalizationAdminService.cs`, `LocalizationAdminState.cs`, admin UI component path to verify, BFF preference endpoint path to verify.
- **Related skills/rules:** blazor-ui-conventions, blazor-bff-patterns, design-system, accessibility.
- **Acceptance criteria:** Language picker obeys governance state; admin form has accessible fields/status/error messages; live TMS and offline fallback modes are clear; BFF language cookie path is validated and tested.
- **Verification:** bUnit tests; Blazor integration tests where applicable; manual browser smoke during implementation.
- **Rollback / failure handling:** Keep picker disabled when config cannot load.

#### Task 5.1: Verify and harden BFF language/direction endpoints
- **Type:** investigate/modify/test
- **Layer:** Blazor BFF
- **Files:** BFF preference endpoint extension file (existing, verify), `LanguagePreferenceService.cs` (existing), `localization.js` (existing)
- **Description:** Locate `/bff/language` and `/bff/direction` endpoint source, confirm allowlist validation, cookie settings, antiforgery posture, and tests.
- **Acceptance Criteria:** Invalid cultures rejected; cookies are secure/same-site/path-correct; endpoint ownership documented.
- **Dependencies:** 0.1
- **Effort:** S
- **Required Skills/Rules:** blazor-server, blazor-bff-patterns, auth-patterns
- **Validation:** Blazor integration or endpoint tests.

#### Task 5.2: Complete admin UI accessibility and mode behavior
- **Type:** modify/test
- **Layer:** Blazor Client
- **Files:** admin localization Razor component path (existing, verify), `LocalizationAdminState.cs` (existing), `LocalizationAdminService.cs` (existing)
- **Description:** Ensure admin localization UI clearly distinguishes live Tolgee/Weblate mode, force-offline fallback, and static no-TMS self-host mode.
- **Acceptance Criteria:** WCAG 2.2 AA basics; secret field write-only; provider test/export states visible; static fallback import/export states visible; force-offline disables live-only actions.
- **Dependencies:** 2.2, 4.3
- **Effort:** M
- **Required Skills/Rules:** blazor-client, accessibility, design-system
- **Validation:** bUnit tests and manual browser smoke.

#### Task 5.3: Keep language picker aligned with governance state
- **Type:** modify/test
- **Layer:** Blazor Client
- **Files:** `LanguagePicker.razor` (existing), caller/layout supplying `Enabled` (existing, verify), related bootstrap/state file (existing, verify)
- **Description:** Ensure picker visibility and available languages follow the approved v1 governance policy.
- **Acceptance Criteria:** Picker respects `localization.client_picker_enabled`; available-language policy is explicit; tests cover disabled/enabled behavior.
- **Dependencies:** 5.1
- **Effort:** S
- **Required Skills/Rules:** blazor-client, accessibility
- **Validation:** `Explore.Blazor.Client.Tests`.

### Phase 6: Observability, Operations, Deployment Docs, And Optional Local TMS Resources
- **Goal:** Make the stack operable in live Tolgee/Weblate, force-offline fallback, and no-TMS static deployments.
- **Depends on:** Phases 1-5.
- **Relevant files:** `TranslationMetrics.cs`, handlers/providers, `docs/LOCALIZATION.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, `docs/DEPLOYMENT_MODES.md`, `Explore.AppHost`/compose files if TMS resources are added.
- **Related skills/rules:** error-tracking, aspire, outbox-pattern.
- **Acceptance criteria:** Metrics are emitted at intended boundaries; docs describe live setup/fallback/recovery; Docker/Aspire guidance matches actual config; self-hosters can run connected or static-only mode.
- **Verification:** Tests where feasible; docs/context tests if docs context changes.
- **Rollback / failure handling:** Offline fallback remains safe when live provider is unavailable.

#### Task 6.1: Close translation metric recording gaps
- **Type:** modify/test
- **Layer:** Application / Infrastructure
- **Files:** `TranslationMetrics.cs` (existing), `TestTmsConnectionCommandHandler.cs` (existing), provider/runtime files as needed
- **Description:** Verify and add metric recording for connection tests, fallback activations, language changes, static bundle import/export validation, provider parse failures, and provider live/fallback mode without instrumenting per-key hot path.
- **Acceptance Criteria:** Metrics record success/failure with safe tags; fallback metrics include provider/reason; no secrets/provider response bodies in metric tags.
- **Dependencies:** 2.5
- **Effort:** S
- **Required Skills/Rules:** error-tracking
- **Validation:** Focused tests or metric assertions if infrastructure exists.

#### Task 6.2: Update hosting, localization, config, and operations docs
- **Type:** docs
- **Layer:** Docs / DevOps
- **Files:** `docs/LOCALIZATION.md`, `docs/CONFIGURATION.md`, `docs/API.md`, `docs/BLAZOR.md`, `docs/OPERATIONS.md`, `docs/DEPLOYMENT_MODES.md` as applicable
- **Description:** Align docs with final code: API/TMS-primary flow, MasterCode key convention, Tolgee/Weblate auth/endpoints, static fallback behavior, cache invalidation, HA storage mode, metrics, health checks, and validation commands.
- **Acceptance Criteria:** A self-hoster can configure Tolgee, Weblate, or no-TMS static fallback from docs without source reading; docs do not describe unimplemented behavior.
- **Dependencies:** 1-6 implementation tasks
- **Effort:** M
- **Required Skills/Rules:** aspire, error-tracking
- **Validation:** Docs/context architecture tests if available; build if architecture tests include docs checks.

#### Task 6.3: Decide whether to add Aspire/Docker Tolgee/Weblate local resources
- **Type:** investigate/modify/docs
- **Layer:** DevOps
- **Files:** `Explore.AppHost`/compose files/docs to verify
- **Description:** Decide whether local connected-mode development should include optional Tolgee/Weblate resources, or only docs for external provider configuration.
- **Acceptance Criteria:** Decision recorded; if resources added, local setup docs and health checks are updated.
- **Dependencies:** 6.2
- **Effort:** M
- **Required Skills/Rules:** aspire
- **Validation:** Aspire app starts or documented reason for deferral.

## 7. Testing Strategy
- **MasterCode/API contract:** Tests for `lookup.{entity_type}.{master_code}.{field}` keys, provider-backed API dictionaries, and client API consumption. Include at least one lookup translation fixture using `MasterCode`, not database ID.
- **Tolgee provider:** Fake HTTP tests for `X-API-Key`, current Context7-backed export/import/read route(s), ZIP/file stream parsing if chosen, body/file import payloads, auth/rate-limit/network fallback.
- **Weblate provider:** Fake HTTP tests for `Authorization: Token`, file download/upload routes, project/component/language slug handling, conflict/add/fuzzy settings, unit endpoint only if deliberately used.
- **Static fallback:** Validate embedded `en/fr/ar` bundles, deterministic formatting, malformed JSON behavior, embedded+writable merge, direct import/export for no-TMS operators, and cache invalidation after writes.
- **Architecture:** `Event.Architecture.Tests`, especially localization resilience, Clean Architecture, naming/context tests.
- **Domain:** `Event.Domain.UnitTests` if culture registry, lookup key helper, or enum behavior changes.
- **Application:** Handler tests for governance validation, translation query language validation, provider/fallback selection, export invalidation, and metrics boundaries in the actual Application test project.
- **Infrastructure:** Provider tests for auth headers, provider endpoint shape, fallback classification, offline bundle merge, writable/embedded precedence, and config cache invalidation.
- **API:** Integration tests for anonymous translation read, invalid culture responses, authorized admin endpoints, TMS config no-secret responses, and static fallback import/export validation.
- **Blazor Client:** Keep/extend `Explore.Blazor.Client.Tests`; cover language picker, admin state typed DTO mapping, live/offline mode UI, secret configured-state display, accessibility status/errors.
- **Blazor/BFF:** Add/verify BFF language preference endpoint tests for culture allowlist and cookie behavior.
- **Manual QA during implementation:** Run through Aspire or project host, switch languages, call translation API for `en/fr/ar`, verify a `MasterCode` lookup translation, test missing TMS key, test failed provider fallback, import/export fallback bundle, and verify admin UI secret is write-only.

## 8. Documentation, Configuration, And Operations Impact
- Update `docs/LOCALIZATION.md` with API/TMS-primary flow, MasterCode key rules, live-provider behavior, fallback bundle behavior, Tolgee/Weblate endpoint/auth specifics, cache invalidation, and storage modes.
- Update `docs/CONFIGURATION.md` with final localization governance keys, environment/secret names, defaults, allowed values, startup/runtime behavior, and rotation guidance.
- Update `docs/API.md` if public/admin endpoint contracts or response codes change.
- Update `docs/BLAZOR.md` for language preference, picker governance, admin live/offline mode UI, and generated client usage.
- Update `docs/OPERATIONS.md` with metrics, fallback alerting, TMS connection troubleshooting, bundle health, backup/restore for no-TMS deployments, and smoke checks.
- Update `docs/DEPLOYMENT_MODES.md` or compose/Aspire docs if Tolgee/Weblate local resources or shared-volume guidance changes.

## 9. Security, Authorization, Privacy, And Abuse Considerations
- TMS API keys are secrets and must never travel to browser/WASM, logs, metrics, ProblemDetails, support bundles, generated clients, or OpenAPI examples.
- Admin localization endpoints stay `[Authorize]`; fallback bundle upload/import endpoints must validate size/content/type and avoid logging raw file bodies.
- Public translation GET endpoints remain `[AllowAnonymous]` but validate bounded culture codes and should be cache/rate-limit friendly.
- Connected TMS failures must not expose provider response bodies, tokens, internal URLs, or stack details to non-admin users.
- Tenant-scoped config uses `IHierarchicalSettingsResolver` and tenant context; cross-tenant/global invalidation must be explicit.
- Abuse risk: public translation endpoint can be hit anonymously; bounded culture codes and output caching reduce cardinality and load.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations
- **Multi-Tenancy:** Applicable. Translation config/cache keys include tenant context; admin governance may differ by tenant; invalidation must not leak across tenants except explicit global operations.
- **Federation:** Not directly applicable now. Future protocol surfaces may need localized UI strings, but TMS data should not be embedded into protocol records without design.
- **Localization:** Core purpose. `CultureRegistry` remains the v1 allowlist; `MasterCode` is the lookup key identity; Tolgee/Weblate are the primary connected providers; static bundles are offline/fallback mirrors.
- **Accessibility:** Applicable. Language picker already has accessible labels/tests; admin live/offline UI must expose labels, alerts/status, keyboard behavior, RTL-safe CSS.
- **Product:** Applicable. ISLAMU-hosted mode gets live translation workflows through Tolgee/Weblate; disconnected self-hosters keep zero-dependency fallback through static bundles; Arabic RTL remains first-class.

## 11. Observability And Operations
- Metrics: `islamu.translation.fetch_total`, `islamu.translation.fetch_duration_seconds`, `islamu.translation.change_language_total`, `islamu.tms.connection_test_total`, `islamu.tms.fallback_activated_total`, and any added static bundle import/export validation metrics.
- Logs: structured provider/language/tenant/result/mode without keys, raw bundle bodies, or provider response bodies.
- Health: authorized bundle path health already exists; connected provider connection-test remains admin-driven; docs should distinguish live provider health from fallback bundle health.
- Alerts: `islamu.tms.fallback_activated_total > 0` over 5 minutes in connected mode should alert operators.
- Troubleshooting: document live TMS auth failure, provider endpoint/rate-limit failure, offline fallback, invalid culture, missing key fallback, secret-not-configured, malformed static bundle, and local filesystem HA limitation.

## 12. Migration And Compatibility Plan
- EF Core migration likely not required for known hardening tasks because governance keys and settings storage already exist.
- If a new secret metadata record/key is needed, prefer existing secret-management model and seed/config conventions; add focused migration/seed only if verified necessary.
- Regenerate client contracts rather than keeping `AdditionalProperties`/payload shims.
- Replace stale provider endpoint/request models outright if Context7-backed endpoints differ; no old draft endpoint compatibility.
- Deployment sequencing: connected TMS activates only after config + server-side secret are present; `tms_provider=None` and `force_offline_mode` remain operator safety valves; static fallback is not a compatibility layer for old live-provider contracts.

## 13. Risk Register
| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| MasterCode key generation drifts between lookup DTOs, docs, and provider payloads. | Medium | High | Phase 1 key contract and tests. | Missing lookup translations or raw keys in UI. | 1.1 |
| Connected provider path still silently uses offline bundle. | Medium | High | Provider-first tests for configured Tolgee/Weblate mode. | Fetch metrics show offline in connected mode; provider fake tests fail. | 1.2, 2.5 |
| TMS API key leaks to browser/logs/metrics/errors. | Low | Critical | Server-side secret flow, no-secret DTO tests, redaction review. | Secret scanner/test failures; review finding. | 2.1, 2.2, 4.1 |
| Tolgee endpoint shape differs from current provider code. | High | High | Context7-backed fake HTTP tests and no stale shim. | 404/400 from provider tests or live connection test. | 2.3 |
| Weblate file upload/download semantics are wrong. | High | High | Context7-backed fake HTTP tests and docs. | Provider tests fail; import creates no units. | 2.4 |
| Fallback bundle hides new embedded keys after upgrade. | Medium | Medium | Key-by-key merge tests. | Missing new keys only on upgraded self-hosters. | 3.2 |
| Cache invalidation leaves stale values after provider export/static import. | Medium | Medium | Same-process read-after-write tests. | Admin export succeeds but UI/API shows old translation. | 3.4 |
| Generated client drift remains. | Medium | Medium | Regenerate and remove temporary shims. | Build/test compile failures or `AdditionalProperties` usage remains. | 4.2, 4.3 |
| HA self-host writes are inconsistent across replicas. | Medium | Medium | Document shared-volume contract; defer object writer explicitly. | Different replicas return different translations. | 3.5, 6.2 |

## 14. Success Metrics And Definition Of Done
- **Functional:** ISLAMU Event API returns translations for supported languages from live Tolgee/Weblate when configured; lookup translations use `MasterCode` keys; no browser-side TMS calls exist.
- **Fallback:** `tms_provider=None` self-hosters get the same key/value contract from static bundles; provider failure falls back to static bundle and records fallback metric.
- **Security:** TMS secrets are server-side only and absent from generated clients, admin read DTOs, logs, metrics, ProblemDetails, and OpenAPI examples.
- **Quality:** Build passes; architecture tests pass; targeted Application/Infrastructure/API/Blazor tests pass; no temporary compatibility shim remains for localization DTO/client/provider shapes.
- **Docs:** `docs/LOCALIZATION.md`, config/API/Blazor/ops/deployment docs, and these dev docs match final behavior.
- **Manual QA:** Admin can configure/test connected provider, fetch translation API output, switch language in UI, verify a `MasterCode` lookup translation, force provider failure, and observe static fallback.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT
Future agents implementing this plan MUST follow this contract:

1. Before starting any implementation slice, read this plan, `internationalization-translation-context.md`, and `internationalization-translation-tasks.md`.
2. Start from the highest-priority incomplete task unless user instruction overrides it.
3. After completing each meaningful task or discovering new scope, update:
   - this plan if architecture/scope/phases/risks changed;
   - `internationalization-translation-context.md` with current state, decisions, files changed, blockers, validation, and next step;
   - `internationalization-translation-tasks.md` by checking completed items and adding discovered tasks.
4. Do not report “done” unless docs reflect the actual current state.
5. Every implementation summary to the user must include what changed, architecture/design patterns, important files/classes/components, data/control flow, conventions followed, verification, remaining work, and next step.
6. If validation fails, update context/tasks with the failure, root cause if known, and next recovery action.
7. Before pausing, context reset, handoff, or PR creation, refresh all three dev docs and add/refresh a handoff section.

## 16. Progress Reporting Contract
When an implementation agent finishes a slice, its final response should use this concise structure:

- **Implemented:** Developer teaching summary naming patterns, libraries/infrastructure, important files/classes, and data/control flow.
- **Verified:** Commands/tests/manual QA performed.
- **Remaining:** Known incomplete tasks or risks.
- **Next:** Recommended next task.
- **Docs updated:** plan/context/tasks updated yes/no with reason.

## 17. Potential Risks & Unknowns
The highest-risk part is the live provider boundary, not the static fallback. The API must reliably return TMS-backed translations keyed by lookup `MasterCode`, while Tolgee and Weblate each have different authentication, export/import, file/stream, and conflict semantics. Static bundles still matter, but only as the no-TMS/failure path; implementation should not let fallback convenience obscure whether hosted connected mode really calls Tolgee/Weblate and returns provider data through the ISLAMU Event API.
