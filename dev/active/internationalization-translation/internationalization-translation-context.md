<!-- ABOUTME: Operational context for the API/TMS-primary internationalization translation workstream. -->
<!-- ABOUTME: Summarizes MasterCode translation evidence, fallback bundle role, next actions, decisions, and handoff notes. -->
# Internationalization Translation — Context

Last Updated: 2026-07-10 Europe/Brussels

## SESSION PROGRESS (2026-07-09 Europe/Brussels)

### ✅ COMPLETED
- Read `/dev-docs` command requirements and created the required planning shape.
- Read and classified `AGENTS.md`, `dev/active/README.md`, `.claude/contract/intents.yaml`, `docs/QUICK_REFERENCE.md`, and `docs/GOVERNANCE.md`.
- Confirmed there is no direct localization/TMS intent; using fallback cross-layer contract.
- Loaded relevant skills/rules and recorded constraints.
- Used Context7 for ASP.NET Core/Blazor localization, MudBlazor culture/RTL evidence, and current Tolgee/Weblate provider docs.
- Read `dev/report/internationalization-translation-report.md` and verified that the repository already implements much of its proposed stack.
- Inspected current localization implementation across Domain, Application, Infrastructure, API, Blazor, bundles, startup wiring, docs, and tests.
- Re-baselined once for senior CTO self-hosting concerns, then corrected the architecture per user feedback: API/TMS-backed translation by lookup `MasterCode` is primary; static bundles are only no-TMS self-host fallback and provider-failure fallback.
- Completed Phase 1 Task 1.1 by adding `TranslationKeys.Lookup(...)`, tests, and localization docs for MasterCode-based lookup keys.
- Completed Phase 1 Task 1.2 by removing successful-empty live response fallback and strengthening runtime provider tests so configured Tolgee/Weblate values are proven to win before offline fallback.
- Completed Phase 1 Task 1.3 by validating public translation language codes through `CultureRegistry` before provider export and documenting the 400 ProblemDetails response.
- Completed Phase 1 Task 1.4 by verifying Blazor translation consumption stays API-backed, browser-safe, and synchronous cache-only on the hot path.
- Completed Phase 2 Task 2.1 by locating the shared secret-binding abstractions and proving localization/TMS has no registered key, backend rotation endpoint, or provider auth wiring yet.
- Completed Phase 2 Task 2.2 by registering the localization/TMS API-key secret, adding backend-only rotation through inline-encrypted `SecretBinding`, and injecting Tolgee/Weblate auth headers through `ISecretResolver`.
- Completed Phase 2 Task 2.3 by replacing the hand-written Tolgee API integration with an NSwag-generated provider client from `schemas/openapi-tolgee-provider.yaml` and testing current auth/export/import routes.
- Completed Phase 2 Task 2.4 by replacing the hand-written Weblate API integration with an NSwag-generated provider client from `schemas/openapi-weblate-provider.yaml` and testing token auth plus file download/upload routes.
- Completed Phase 2 Task 2.5 by recording fallback metrics on provider import failures and covering the runtime fallback path with focused infrastructure tests.
- Completed Phase 3 Tasks 3.1 through 3.5 by adding bundle schema validation, deterministic writes, embedded+writable key-by-key merge, static bundle import/export seams, cache invalidation, and self-host storage docs.
- Completed Phase 4 Task 4.1 by adding authorized static bundle import/export API contracts and Blazor admin service hooks.
- Completed Phase 4 Task 4.2 by verifying the API OpenAPI generation and Blazor NSwag client generation workflow after adding static bundle routes.
- Completed Phase 4 Task 4.3 by removing stale Blazor localization DTO shims and aligning admin state/service payloads with generated DTOs.
- Completed Phase 5 Task 5.1 by verifying BFF language/direction endpoint ownership, allowlist validation, cookie persistence, API forwarding, and antiforgery posture through existing source and integration tests.
- Completed Phase 5 Task 5.2 by wiring the admin UI write-only TMS API-key field to backend rotation, removing the misleading clear action, and clarifying live TMS-to-static bundle export affordances.
- Completed Phase 5 Task 5.3 by exposing localization picker governance through public experience settings and wiring shell language pickers to that flag.
- Completed Phase 6 Tasks 6.1 through 6.3 by documenting safe translation metrics, confirming static bundle storage docs, and deferring local Aspire/Docker Tolgee/Weblate resources to operator-provided TMS smoke.

### ✅ FINAL VERIFICATION STATE
- Anonymous/local Aspire smoke completed for public translation fetch, language validation, Blazor shell/language picker rendering, Weblate root reachability, and admin endpoint authorization boundaries.
- Authenticated admin static bundle/provider smoke remains unrun because no local admin token/credentials or operator-provided live TMS endpoint were available.
- Full `Event.API.IntegrationTests` is not green for unrelated reasons: notification intent inserts violate `fk_notification_intents_notification_categories_category_id`, and a later run also hit Keycloak Testcontainers exiting with code 137.

### ⏭️ NEXT
1. Fix or isolate the unrelated notification category seed/test-data failure before relying on full API integration results.
2. Run authenticated admin smoke for static bundle import/export and live provider test/export when an admin token and operator TMS endpoint are available.

### ⚠️ BLOCKERS
- None blocking the localization implementation.
- Full-repo API integration is blocked by unrelated notification/Keycloak test-environment failures, not by localization code.

## Quick Resume
1. Read `internationalization-translation-plan.md`.
2. Read `internationalization-translation-tasks.md`.
3. Start from resolving unrelated API integration failures or authenticated admin smoke if credentials/TMS endpoint are available; provider generated clients, fallback metrics, static bundle schema/merge, static bundle admin hooks, OpenAPI generation evidence, generated admin DTO alignment, BFF preference endpoints, admin UI secret/live-export affordances, language-picker governance alignment, and metric docs are complete.
4. Keep all three dev docs updated after each meaningful implementation slice.
5. Do not treat this as greenfield: most localization layers already exist.
6. Do not make static bundles the primary hosted design; they are fallback/offline support.
7. Phase 1 Task 1.1 added `TranslationKeys.Lookup(...)` as the canonical Application helper for `lookup.{entity_type}.{master_code}.{field}` keys.

## Key Files And Responsibilities
| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `dev/report/internationalization-translation-report.md` | Existing | Docs | Source report for target architecture and risks. | Input to this plan. |
| `docs/LOCALIZATION.md` | Existing | Docs | Current localization architecture guide. | Confirms TMS/offline source-of-truth and MasterCode key convention. |
| `Explore.Domain/Common/Localization/CultureRegistry.cs` | Existing | Domain | Culture allowlist and RTL metadata. | Supports `en`, `fr`, `ar`; rejects regional codes. |
| `Explore.Domain/Constants/GovernanceSettingKeys.cs` | Existing | Domain | Localization governance setting key constants. | Contains localization provider/config keys. |
| `Explore.Domain/Enums/TranslationManagementProviderEnum.cs` | Existing | Domain | TMS provider enum. | `None`, `Tolgee`, `Weblate`. |
| Lookup entities/DTOs with `MasterCode` | Existing | Domain/Application/API contracts | Stable lookup translation identity. | Must drive `lookup.{entity_type}.{master_code}.{field}` keys. |
| `Explore.Application/Localization/TranslationKeys.cs` | New | Application | Canonical lookup translation key builder. | Builds keys from entity type, stable `MasterCode`, and field; rejects IDs/labels with delimiters or whitespace. |
| `Explore.Application/Contracts/Infrastructure/ITranslationResolver.cs` | Existing | Application | Unified key resolver contract. | Cache/fallback abstraction. |
| `Explore.Application/Contracts/Infrastructure/ITranslationManagementProvider.cs` | Existing | Application | TMS/offline provider contract. | Import/export/test/languages. |
| `Explore.Application/Contracts/Infrastructure/ITranslationConfigResolver.cs` | Existing | Application | Governance config resolver contract. | Tenant-aware provider config. |
| `Explore.Application/Contracts/Infrastructure/IBundleFileWriter.cs` | Existing | Application | Bundle write seam. | Used for fallback/static bundle persistence. |
| `Explore.Application/Contracts/Infrastructure/IStaticTranslationBundleReader.cs` | New | Application | Static bundle read seam. | Lets admin export merged static bundles without live Tolgee/Weblate access. |
| `Explore.Application/DTOs/Localization/LocalizationConfigDto.cs` | Existing | Application | Admin config read DTO. | Includes `TmsApiKeyConfigured`, not raw key. |
| `Explore.Application/DTOs/Localization/RotateLocalizationTmsApiKeyDto.cs` | New | Application | Admin TMS API-key rotation input. | Write-only secret DTO; plaintext is validated and immediately protected. |
| `Explore.Application/DTOs/Localization/UpdateLocalizationGovernanceDto.cs` | Existing | Application | Admin governance update DTO. | Explicitly excludes secrets. |
| `Explore.Application/Features/Localization/Handlers/Commands/RotateLocalizationTmsApiKeyCommandHandler.cs` | New | Application | Rotates localization TMS API keys through the shared secret-binding seam. | Authorizes tenant/instance admins, respects locked instance policy, writes tenant-scoped inline-encrypted binding, and invalidates resolver cache. |
| `Explore.Application/Features/Localization/Handlers/**` | Existing | Application | CQRS handlers for localization. | Must prove provider-backed API path. |
| `Explore.Application/Telemetry/TranslationMetrics.cs` | Existing | Application | Translation/TMS metrics. | Extend for connected/fallback mode if needed. |
| `Explore.Infrastructure/Localization/RuntimeTranslationProvider.cs` | Existing | Infrastructure | Runtime provider selection and fallback. | Connected providers first when configured. |
| `Explore.Infrastructure.Tests/Infrastructure/Localization/RuntimeTranslationProviderTests.cs` | Existing, modified | Tests | Runtime provider routing tests. | Now proves Tolgee/Weblate connected-provider values are returned before offline fallback. |
| `Explore.Infrastructure/Localization/TranslationResolver.cs` | Existing | Infrastructure | Tenant/language/mode/key cache. | Preloads language into memory cache. |
| `Explore.Infrastructure/Localization/TranslationConfigResolver.cs` | Existing | Infrastructure | Hierarchical settings backed config. | `InvalidateCache(null)` is likely incomplete. |
| `schemas/openapi-tolgee-provider.yaml` | New | Schema | Provider-normalized Tolgee OpenAPI slice. | Keeps raw upstream schema traceable while fixing missing `projectId` metadata for generated client use. |
| `schemas/openapi-weblate-provider.yaml` | New | Schema | Provider-normalized Weblate OpenAPI slice. | Models multipart binary file upload so NSwag generates a usable file client. |
| `Explore.Infrastructure/nswag.tolgee.json` | New | Infrastructure | NSwag config for Tolgee provider client. | Generates `Localization/Generated/Tolgee/TolgeeApiClient.g.cs`. |
| `Explore.Infrastructure/nswag.weblate.json` | New | Infrastructure | NSwag config for Weblate provider client. | Generates `Localization/Generated/Weblate/WeblateApiClient.g.cs`. |
| `Explore.Infrastructure/Localization/TolgeeTranslationProvider.cs` | Existing, modified | Infrastructure | Tolgee provider using generated client. | Resolves server-side API key, sends `X-API-Key`, imports resolvable keys, exports flat `lookup.*`/`ui.*` values. |
| `Explore.Infrastructure/Localization/WeblateTranslationProvider.cs` | Existing, modified | Infrastructure | Weblate provider using generated client. | Resolves server-side API key, sends `Authorization: Token`, downloads/uploads JSON translation files. |
| `Explore.Infrastructure/Localization/OfflineTranslationProvider.cs` | Existing, modified | Infrastructure | Offline JSON bundle provider and static bundle reader. | Merges embedded defaults with valid writable overrides key-by-key. |
| `Explore.Infrastructure/Localization/BundleSchema.cs` | New | Infrastructure | Flat bundle schema validator/reader. | Enforces `ui.*`/`lookup.*` string dictionary shape and deterministic ordering. |
| `Explore.Infrastructure/Localization/BundleFileWriter.cs` | Existing, modified | Infrastructure | Atomic local JSON bundle writer. | Validates/sorts through `BundleSchema`; local filesystem/shared-volume writer seam. |
| `Explore.Infrastructure/Localization/Bundles/en.json` | Existing | Infrastructure | Embedded English fallback bundle. | Offline/fallback. |
| `Explore.Infrastructure/Localization/Bundles/fr.json` | Existing | Infrastructure | Embedded French fallback bundle. | Offline/fallback. |
| `Explore.Infrastructure/Localization/Bundles/ar.json` | Existing | Infrastructure | Embedded Arabic fallback bundle. | RTL fallback. |
| `Explore.API/Controllers/TranslationController.cs` | Existing | API | Anonymous translation read endpoints. | Primary API surface for clients. |
| `Explore.API/Controllers/LocalizationAdminController.cs` | Existing, modified | API | Authorized localization admin endpoints. | Provider config/test/export plus static bundle GET/POST and bundle health. |
| `Explore.Blazor.Client/Services/TranslationService.cs` | Existing | Blazor Client | Client API-backed translation cache and hot-path `T`. | No browser-side TMS calls. |
| `Explore.Blazor.Client/Services/LocalizationAdminService.cs` | Existing, modified | Blazor Client | Admin API wrapper. | Adds static bundle export/import service hooks and uses generated DTOs for governance/import bodies. |
| `Explore.Blazor.Client/Models/Admin/LocalizationAdminState.cs` | Existing, modified | Blazor Client | Localization admin view model. | Uses generated `LocalizationConfigDto` and `UpdateLocalizationGovernanceDto` typed properties. |
| `Explore.Blazor.Client/Clients/EventApiClient.g.cs` | Existing, generated | Blazor Client | Generated API client. | Contains static bundle admin operations and DTOs from `schemas/openapi.json`. |
| `Explore.Blazor.Client/Shared/LanguagePicker.razor` | Existing | Blazor Client | User language picker. | Tested and accessible. |
| `Event.Application.UnitTests/Infrastructure/Localization/GetTranslationsQueryHandlerTests.cs` | Existing | Tests | Translation handler tests. | Uses MasterCode-shaped lookup keys. |
| `Event.Application.UnitTests/Infrastructure/Localization/TranslationKeysTests.cs` | New | Tests | Translation key contract tests. | Locks MasterCode key construction and invalid segment rejection. |
| `Event.API.IntegrationTests/Features/EndpointAuthorizationMatrixTests.cs` | Existing | Tests | Endpoint auth matrix. | Public translations anonymous. |
| `Event.Architecture.Tests/LocalizationResilienceTests.cs` | Existing | Tests | TMS resilience architecture guard. | Protects single retry source. |
| `Explore.Blazor.Client.Tests/Components/LanguagePickerTests.cs` | Existing | Tests | bUnit picker behavior. | Covers core picker interactions. |

## Key Decisions
1. Complete the existing localization stack instead of replacing it.
2. API/TMS is the primary runtime path for hosted and connected deployments.
3. Lookup translations are keyed by stable `MasterCode`, never database ID or localized label.
4. Static bundles are offline/fallback mirrors for `tms_provider=None` and provider failure, not the hosted source of truth.
5. Provider integrations are schema-first: regenerate Tolgee/Weblate provider clients through NSwag configs instead of restoring hand-written Refit interfaces.
6. Keep TMS API keys server-side through the existing secret-provider pattern once verified.
7. Validate language codes at public API boundaries with `CultureRegistry`.
8. Regenerate/align API clients instead of keeping temporary compatibility shims.
9. Application code should build lookup translation keys through `Explore.Application.Localization.TranslationKeys.Lookup(entityType, masterCode, field)`.

## Constraints And Rules To Remember
- Repositories return entities, never DTOs.
- Validators are manually instantiated.
- Domain must stay pure; Application owns contracts; Infrastructure implements providers; API/Blazor compose.
- GET endpoints are anonymous; write/admin endpoints are authorized.
- Browser/WASM never receives TMS API keys, bearer tokens, or secret values.
- Tenant settings and cache keys must respect tenant context.
- `MasterCode` is the canonical lookup translation identity.
- Hosted/connected mode uses Tolgee/Weblate through the ISLAMU Event API; static bundles are fallback/no-TMS support.
- Public translation reads accept only `CultureRegistry` languages (`en`, `fr`, `ar`) and normalize before provider access.
- Provider calls must match generated clients from the checked-in provider-normalized OpenAPI slices.
- All new files need two `ABOUTME:` lines.
- Localization hot path must stay synchronous/in-memory/no-I/O.
- No backward-compatibility shims for draft unreleased DTO/provider/client shapes unless user explicitly asks.

## Validation Baseline
- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- For API/controller changes: `Event.API.IntegrationTests` plus architecture tests.
- For Blazor client changes: `Explore.Blazor.Client.Tests` plus architecture tests.
- For Blazor BFF changes: `Explore.Blazor.IntegrationTests` plus architecture tests.
- For Domain changes: `Event.Domain.UnitTests` plus architecture tests.
- For provider/static fallback/Application tests: verify actual test project names before adding/running.

## Current Known Risks / Unknowns
- Admin OpenAPI/NSwag generation and generated Blazor client DTO drift are verified through Phase 4.3; remaining admin UI work is direct file-import UX and any visual polish found during browser smoke.
- TMS API-key secret-provider path is implemented for tenant-scoped inline-encrypted rotation and provider resolution; instance/env/Infisical binding behavior remains inherited from the shared secret resolver.
- Tolgee/Weblate auth headers and endpoint/payload shapes are implemented through generated provider clients and fake HTTP tests; authenticated live-provider smoke is still pending against an operator-provided TMS endpoint.
- API/TMS failures must remain visible through logs/metrics; successful empty connected-provider responses now stay empty instead of silently using static fallback.
- `TranslationConfigResolver.InvalidateCache(null)` does not clear all tenant cache entries.
- Admin UI direct file-import UX is not implemented yet; service/API hooks exist.
- Language picker visibility now consumes `localization.client_picker_enabled` through anonymous-safe public experience settings.
- HA-safe bundle writer is not implemented; current local writer is single-replica/shared-volume safe only.

## Implementation Progress

### Phase 1 Task 1.1 — MasterCode Translation Key Contract
- **Status:** Completed.
- **Changed files:** `Explore.Application/Localization/TranslationKeys.cs`, `Event.Application.UnitTests/Infrastructure/Localization/TranslationKeysTests.cs`, `docs/LOCALIZATION.md`, this context file, and `internationalization-translation-tasks.md`.
- **Behavior:** Lookup translation keys are now built through an Application helper as `lookup.{entity_type}.{master_code}.{field}`. Entity type and field are trimmed/lowercased; `MasterCode` is trimmed and case-preserved. Blank segments, dotted segments, and whitespace-bearing segments are rejected so database IDs/localized labels/display names do not silently become translation keys.
- **Validation:** `aft_inspect` on changed files reported 0 diagnostics, `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed (2074/2074), and `dotnet build --configuration Release --verbosity quiet` passed with 0 errors. Warnings are pre-existing package/analyzer/compiler warnings.

### Phase 1 Task 1.2 — Provider-First API Translation Reads
- **Status:** Completed.
- **Changed files:** `Explore.Infrastructure/Localization/RuntimeTranslationProvider.cs`, `Explore.Infrastructure.Tests/Infrastructure/Localization/RuntimeTranslationProviderTests.cs`, this context file, and `internationalization-translation-tasks.md`.
- **Behavior:** The runtime provider selection path is now covered with concrete provider-value assertions. Configured Tolgee and Weblate modes return values parsed from their connected provider HTTP responses before offline fallback can win. Successful empty connected-provider exports now return an empty live result instead of silently falling back to static bundles; offline fallback is reserved for provider exceptions, `tms_provider=None`, force-offline mode, or config-resolution failure.
- **Validation:** `aft_inspect` on the changed runtime/test files reported 0 diagnostics, `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed (702/702), and `dotnet build --configuration Release --verbosity quiet` passed with 0 errors. Warnings are pre-existing package/analyzer/compiler warnings.

### Phase 1 Task 1.3 — Public Translation Language Validation
- **Status:** Completed.
- **Changed files:** `Explore.Application/Features/Localization/Handlers/Queries/GetTranslationsQueryHandler.cs`, `Explore.API/Controllers/TranslationController.cs`, `Event.Application.UnitTests/Infrastructure/Localization/GetTranslationsQueryHandlerTests.cs`, this context file, and `internationalization-translation-tasks.md`.
- **Behavior:** `GetTranslationsQueryHandler` now validates `LanguageCode` with `CultureRegistry.TryGetEntry(...)` before calling `ITranslationManagementProvider`. Supported inputs normalize to canonical two-letter codes before provider/export access (`" EN "` -> `"en"`, `"Ar"` -> `"ar"`); malformed or unsupported codes throw a property-keyed `FluentValidation.ValidationException`, so the API exception pipeline returns a controlled 400 ProblemDetails response and provider/cache is not called with arbitrary language values.
- **Validation:** `lsp_diagnostics` reported no diagnostics for changed source/test files, `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed (2076/2076), `dotnet build --configuration Release --verbosity quiet` passed with 0 errors, and `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed (259 succeeded, 1 skipped). `Event.API.IntegrationTests` no longer fails the translation smoke route after `Event.API.IntegrationTests/Features/ApiEndpointSmokeTests.cs` was updated to sample `/api/translation/en`; the remaining 2 failures are unrelated event-registration notification-outbox failures in `NotificationIntentRepository.CreateIntentAsync` / `CreateEventRegistrationCommandHandler.Handle`. Warnings are pre-existing package/analyzer/compiler warnings.

### Phase 1 Task 1.4 — Blazor API-Backed Translation Consumption
- **Status:** Completed.
- **Changed files:** `Explore.Blazor.Client/Services/TranslationService.cs`, `Explore.Blazor.Client/Services/MudBlazorLocalizer.cs`, `Explore.Blazor.Client/Contracts/Services/ITranslationService.cs`, `Explore.Blazor.Client/Program.cs`, `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs`, `Explore.Blazor.Client.Tests/Services/TranslationServiceTests.cs`, this context file, and `internationalization-translation-tasks.md`.
- **Behavior:** `TranslationService.T(...)` remains synchronous and cache-only with no I/O. Cache refresh validates language input through `CultureRegistry`, normalizes to the canonical code, and uses `IEventApiClient.GetTranslationByLanguageAsync`; available languages use `GetAvailableTranslationLanguagesAsync`. Unknown languages return an empty dictionary without calling the API or poisoning the cache. `MudBlazorLocalizer` resolves `mudblazor.{key.ToLowerInvariant()}` through the same cache. WASM DI uses the same-origin API client and has no runtime Tolgee/Weblate/TMS secret client.
- **Validation:** `lsp_diagnostics` reported no diagnostics for `Explore.Blazor.Client.Tests/Services/TranslationServiceTests.cs`. Runtime service/contract searches found no Tolgee/Weblate/TMS-secret references in translation consumption services; TMS references were limited to admin localization service/interface/tests. `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed (1564 total, 0 failed, 1563 succeeded, 1 skipped). Warnings are pre-existing `NU1903` AutoMapper advisory and deprecated `Microsoft.Extensions.ApiDescription.Client` warnings.

### Phase 2 Task 2.1 — Server-Side TMS Secret-Provider Abstraction
- **Status:** Completed.
- **Evidence files:** `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs`, `Explore.Domain/Secrets/SecretDefinitionRegistry.cs`, `Explore.Domain/Settings/Definitions/LocalizationSettingDefinitions.cs`, `Explore.Domain/Constants/GovernanceSettingKeys.cs`, `Explore.Application/Contracts/Secrets/ISecretResolver.cs`, `Explore.Application/Contracts/Persistence/ISecretBindingRepository.cs`, `Explore.Application/Contracts/Infrastructure/ITranslationConfigResolver.cs`, `Explore.Infrastructure/Localization/TranslationConfigResolver.cs`, `Explore.Application/DTOs/Localization/LocalizationConfigDto.cs`, `Explore.Application/DTOs/Localization/UpdateLocalizationGovernanceDto.cs`, `Explore.Application/Features/Localization/Handlers/Commands/UpdateLocalizationGovernanceCommandHandler.cs`, `Explore.API/Controllers/LocalizationAdminController.cs`, `Explore.Blazor.Client/Contracts/Services/ILocalizationAdminService.cs`, `Explore.Blazor.Client/Services/ILocalizationAdminApi.cs`, `Explore.Blazor.Client/Services/LocalizationAdminService.cs`, `Explore.Blazor.Client/Models/Admin/LocalizationAdminState.cs`, `Explore.Blazor.Client/Models/Admin/LocalizationGovernancePayload.cs`, `Explore.Infrastructure/Localization/TolgeeTranslationProvider.cs`, and `Explore.Infrastructure/Localization/WeblateTranslationProvider.cs`.
- **Finding:** The correct server-side abstraction is the shared secret-binding seam: `SecretDefinitionRegistry` declares allowed secret keys, `SecretBinding` stores source metadata only, `ISecretBindingRepository` retrieves bindings, and `ISecretResolver.ResolveAsync(settingKey, tenantId, ct)` resolves plaintext from the single declared source. However, localization/TMS is not registered in `InfrastructureSecretSettingKeys` or `SecretDefinitionRegistry`, and `docs/CONFIGURATION.md` explicitly says localization/TMS keys are still area-specific and not fully migrated to shared ownership metadata.
- **Current gap:** TMS non-secret governance exists (`localization.tms_provider`, `localization.tms_api_url`, `localization.tms_project_id`, `localization.tms_component`, language settings, picker/force-offline switches), but no TMS API-key setting key, `SecretBinding`, backend rotation endpoint, or generated/API DTO carries the key. `LocalizationConfigDto.TmsApiKeyConfigured` exists but `LocalizationAdminController.GetConfiguration` does not populate it. `UpdateLocalizationGovernanceDto` and `LocalizationGovernancePayload` intentionally omit secrets. The Blazor `LocalizationAdminState` has a local `TmsApiKey` field, but `ILocalizationAdminService`, `ILocalizationAdminApi`, and `LocalizationAdminService` expose only config/test/export/governance/bundle-health operations, so that field is not persisted. `TolgeeTranslationProvider` and `WeblateTranslationProvider` document required auth headers but currently create unauthenticated clients.
- **Next implementation implication:** Phase 2.2 must add a localization/TMS API-key secret definition and backend write/resolve path before provider auth headers can work. The minimal path is to reuse the existing `SecretDefinitionRegistry`/`SecretBinding`/`ISecretResolver` seam rather than inventing a new storage abstraction.

### Phase 2 Task 2.2 — Server-Side TMS Secret Rotation And Provider Auth Headers
- **Status:** Completed.
- **Changed files:** `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs`, `Explore.Domain/Secrets/SecretDefinitionRegistry.cs`, `Explore.Application/Contracts/Secrets/IInlineSecretProtector.cs`, `Explore.Application/DTOs/Localization/RotateLocalizationTmsApiKeyDto.cs`, `Explore.Application/DTOs/Localization/Validators/RotateLocalizationTmsApiKeyDtoValidator.cs`, `Explore.Application/Features/Localization/Requests/Commands/RotateLocalizationTmsApiKeyCommand.cs`, `Explore.Application/Features/Localization/Handlers/Commands/RotateLocalizationTmsApiKeyCommandHandler.cs`, `Explore.Secrets/Services/InlineSecretProtector.cs`, `Explore.Secrets/Extensions/SecretResolutionServiceCollectionExtensions.cs`, `Explore.API/Hateoas/RouteNames.cs`, `Explore.API/Controllers/LocalizationAdminController.cs`, `Explore.Infrastructure/Localization/TolgeeTranslationProvider.cs`, `Explore.Infrastructure/Localization/WeblateTranslationProvider.cs`, `Explore.Infrastructure.Tests/Infrastructure/Localization/RuntimeTranslationProviderTests.cs`, `Explore.Infrastructure.Tests/Infrastructure/Localization/RuntimeTranslationProviderFallbackTests.cs`, `Event.Application.UnitTests/Infrastructure/Localization/RotateLocalizationTmsApiKeyCommandHandlerTests.cs`, this context file, and `internationalization-translation-tasks.md`.
- **Behavior:** Localization now has a registered shared secret key, `localization.tms_api_key` / `LOCALIZATION_TMS_API_KEY`. Admin rotation is backend-only: `LocalizationAdminController` sends `RotateLocalizationTmsApiKeyCommand`, the handler manually validates the write-only DTO, authorizes tenant or instance admins, blocks tenant override when an instance binding is locked, protects the trimmed API key through `IInlineSecretProtector`, stores or updates a tenant-scoped inline-encrypted `SecretBinding`, and invalidates the tenant resolver cache. `LocalizationConfigDto.TmsApiKeyConfigured` is populated from binding metadata only. Tolgee and Weblate providers resolve the plaintext server-side through `ISecretResolver`; Tolgee sends `X-API-Key`, and Weblate sends `Authorization: Token`. Blazor still receives only metadata and never receives the plaintext key.
- **Validation:** `lsp_diagnostics` reported no diagnostics for `Event.Application.UnitTests/Infrastructure/Localization/RotateLocalizationTmsApiKeyCommandHandlerTests.cs`. `aft_inspect` reported 0 diagnostics for the changed handler/test scope, with C# LSP unavailable. `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed (704/704). `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed (2080/2080). Release build verification is pending immediately after this docs refresh. Warnings are pre-existing package/analyzer/compiler warnings.

### Phase 2 Tasks 2.3–2.4 — Schema-First Tolgee/Weblate Provider Contracts
- **Status:** Completed.
- **Changed files:** `schemas/openapi-tolgee-provider.yaml`, `schemas/openapi-weblate-provider.yaml`, `Explore.Infrastructure/nswag.tolgee.json`, `Explore.Infrastructure/nswag.weblate.json`, `Explore.Infrastructure/Localization/Generated/Tolgee/TolgeeApiClient.g.cs`, `Explore.Infrastructure/Localization/Generated/Weblate/WeblateApiClient.g.cs`, `Explore.Infrastructure/Localization/TolgeeTranslationProvider.cs`, `Explore.Infrastructure/Localization/WeblateTranslationProvider.cs`, `Explore.Infrastructure.Tests/Infrastructure/Localization/RuntimeTranslationProviderTests.cs`, docs, this context file, and `internationalization-translation-tasks.md`.
- **Behavior:** Tolgee and Weblate integrations now use NSwag-generated provider clients from checked-in provider-normalized OpenAPI slices instead of hand-written Refit interfaces. Tolgee resolves the server-side API key, sends `X-API-Key`, tests `GET /v2/projects/{projectId}`, imports resolvable key payloads, exports `GET /v2/projects/{projectId}/translations/{language}?structureDelimiter=.`, and flattens `lookup.*`/`ui.*` values. Weblate resolves the server-side API key, sends `Authorization: Token`, downloads translation files through `GET /api/translations/{project}/{component}/{language}/file/`, and imports JSON translation files through generated multipart upload with method `translate`, fuzzy behavior `process`, and conflicts `replace`.
- **Validation:** `lsp_diagnostics`/`aft_inspect` reported no diagnostics on changed provider/test files, and `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed after provider and generated-client updates. Remaining warnings are pre-existing package/analyzer/compiler warnings.

### Phase 2 Task 2.5 — Connected Provider Fallback Metrics
- **Status:** Completed.
- **Changed files:** `Explore.Infrastructure/Localization/RuntimeTranslationProvider.cs`, `Explore.Infrastructure.Tests/Infrastructure/Localization/RuntimeTranslationProviderFallbackTests.cs`, this context file, and `internationalization-translation-tasks.md`.
- **Behavior:** Provider import failures now record `islamu.tms.fallback_activated_total` with provider and classified reason before being swallowed/logged, matching export/test/language fallback visibility. Runtime fallback tests cover provider exception fallback and import failure metric recording.
- **Validation:** `lsp_diagnostics`/`aft_inspect` reported no diagnostics on changed fallback files. `Explore.Infrastructure.Tests` passed after fixing metric-listener parallelism by selecting the latest matching measurement. An unrelated untracked notification handler compile error surfaced once and was left untouched per user instruction.

### Phase 3 Tasks 3.1–3.5 — Static Bundle Fallback And Storage
- **Status:** Completed.
- **Changed files:** `Explore.Infrastructure/Localization/BundleSchema.cs`, `Explore.Infrastructure/Localization/OfflineTranslationProvider.cs`, `Explore.Infrastructure/Localization/BundleFileWriter.cs`, `Explore.Infrastructure/Localization/Bundles/en.json`, `Explore.Infrastructure/Localization/Bundles/fr.json`, `Explore.Infrastructure/Localization/Bundles/ar.json`, `Explore.Infrastructure.Tests/Infrastructure/Localization/OfflineTranslationProviderTests.cs`, docs, this context file, and `internationalization-translation-tasks.md`.
- **Behavior:** Static bundles now use a shared flat JSON schema validator that accepts only string values under valid `ui.*`/`lookup.*` keys and writes deterministic sorted JSON. The offline provider loads embedded defaults first, merges valid writable overrides key-by-key, includes writable-only keys, and ignores malformed writable bundles without hiding embedded defaults. Starter `en/fr/ar` bundles now contain minimal `ui.common.*` fallback keys. Bundle storage and shared-volume limitations are documented for self-hosters.
- **Validation:** `lsp_diagnostics`/`aft_inspect` reported no diagnostics on changed static bundle files, and `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed (709/709) after the merge constructor fix. Remaining warnings are pre-existing.

### Phase 4 Task 4.1 — Static Bundle Admin Contracts And Client Hooks
- **Status:** Completed.
- **Changed files:** `Explore.Application/Contracts/Infrastructure/IStaticTranslationBundleReader.cs`, `Explore.Application/DTOs/Localization/ImportLocalizationBundleDto.cs`, `Explore.Application/Features/Localization/Requests/Commands/ImportLocalizationBundleCommand.cs`, `Explore.Application/Features/Localization/Handlers/Commands/ImportLocalizationBundleCommandHandler.cs`, `Explore.Application/Features/Localization/Requests/Queries/ExportLocalizationBundleQuery.cs`, `Explore.Application/Features/Localization/Handlers/Queries/ExportLocalizationBundleQueryHandler.cs`, `Explore.Infrastructure/Localization/OfflineTranslationProvider.cs`, `Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `Explore.API/Hateoas/RouteNames.cs`, `Explore.API/Controllers/LocalizationAdminController.cs`, `Explore.Blazor.Client/Contracts/Services/ILocalizationAdminService.cs`, `Explore.Blazor.Client/Services/ILocalizationAdminApi.cs`, `Explore.Blazor.Client/Services/LocalizationAdminService.cs`, docs, this context file, and `internationalization-translation-tasks.md`.
- **Behavior:** Authorized admins can now export the merged static bundle with `GET /api/admin/localization/bundle?languageCode={code}` and import a flat bundle with `POST /api/admin/localization/bundle`. Imports validate language, reject empty payloads, write through `IBundleFileWriter`, invalidate `ITranslationResolver`, and return safe command errors without logging raw bundle content. `OfflineTranslationProvider` implements `IStaticTranslationBundleReader`, and Blazor admin service/API contracts expose static bundle export/import hooks without adding a file-picker UI yet.
- **Validation:** `lsp_diagnostics` reported no diagnostics on changed Application/API/Infrastructure/Blazor service files, and `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed (709/709). API integration and manual Aspire smoke remain pending.

### Phase 4 Tasks 4.2–4.3 — OpenAPI/NSwag Admin Client Alignment
- **Status:** Completed.
- **Changed files:** `schemas/openapi.json`, `Explore.API/Explore.API.csproj`, `Explore.Blazor.Client/nswag.json`, `Explore.Blazor.Client/Explore.Blazor.Client.csproj`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `Explore.Blazor.Client/Models/Admin/LocalizationAdminState.cs`, `Explore.Blazor.Client/Models/Admin/LocalizationGovernancePayload.cs`, `Explore.Blazor.Client/Contracts/Services/ILocalizationAdminService.cs`, `Explore.Blazor.Client/Services/ILocalizationAdminApi.cs`, `Explore.Blazor.Client/Services/LocalizationAdminService.cs`, `Explore.Blazor.Client.Tests/Services/LocalizationAdminServiceTests.cs`, this context file, and `internationalization-translation-tasks.md`.
- **Behavior:** The API Release build owns `schemas/openapi.json`, and `Explore.Blazor.Client/nswag.json` owns `Clients/EventApiClient.g.cs`. The generated schema/client now include `ExportLocalizationBundle`, `ImportLocalizationBundle`, `ImportLocalizationBundleDto`, `UpdateLocalizationGovernanceDto`, and typed `LocalizationConfigDto` governance fields. The Blazor admin state no longer reads generated config from `AdditionalProperties`, the temporary `LocalizationGovernancePayload` shim was removed, and the BFF Refit wrapper now accepts generated governance/static-bundle DTOs while preserving server-side token/tenant/setup-secret forwarding.
- **Validation:** `lsp_diagnostics` and `aft_inspect` reported no diagnostics on changed Blazor admin service/state/test files. `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed (1566 succeeded, 1 skipped). Remaining warnings are pre-existing package/analyzer warnings.

### Phase 5 Task 5.1 — BFF Language And Direction Preferences
- **Status:** Completed.
- **Evidence files:** `Explore.Blazor/Extensions/BffPreferenceEndpoints.cs`, `Explore.Blazor/Extensions/BffEndpointExtensions.cs`, `Explore.Blazor/Services/Preferences/BffPreferenceValidationService.cs`, `Explore.Blazor/Services/Preferences/BffPreferenceCookieService.cs`, `Explore.Blazor/Services/Preferences/BffPreferenceForwardingService.cs`, `Explore.Blazor.Client/Services/LanguagePreferenceService.cs`, `Explore.Blazor.Client/wwwroot/js/localization.js`, `Explore.Blazor.IntegrationTests/Endpoints/BffPreferenceAntiforgeryTests.cs`, `Explore.Blazor.IntegrationTests/Endpoints/BffPreferenceValidationEndpointsTests.cs`, `Explore.Blazor.IntegrationTests/Services/BffPreferenceValidationServiceTests.cs`, `Explore.Blazor.IntegrationTests/Services/BffPreferenceCookieServiceTests.cs`, and `Explore.Blazor.IntegrationTests/Services/BffPreferenceForwardingServiceTests.cs`.
- **Behavior:** `/bff/language` and `/bff/direction` are owned by `BffPreferenceEndpoints` and mapped through `MapBffEndpoints()`. Both mutation endpoints require antiforgery validation, normalize input through `BffPreferenceValidationService`, reject unsupported language/direction values before cookies or API forwarding, persist anonymous language/direction cookies through `BffPreferenceCookieService`, and forward authenticated preference changes through the named BFF client without exposing tokens to the browser. The client `LanguagePreferenceService` also validates language codes through `CultureRegistry` before calling `/bff/language`.
- **Validation:** Existing integration/unit coverage verifies invalid language/direction returns 400 without cookies, valid language persists `lang` and `.AspNetCore.Culture`, `dir=auto` deletes the direction cookie, preference validation normalizes allowlisted values only, authenticated forwarding maps language/direction to the API preference DTO, and the browser antiforgery handler adds `X-CSRF-TOKEN` for mutating BFF requests.

### Phase 5 Task 5.2 — Admin Localization UI Secret And Live/Fallback Affordances
- **Status:** Completed.
- **Changed files:** `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceLocalizationSection.razor`, `Explore.Blazor.Client/Contracts/Services/ILocalizationAdminService.cs`, `Explore.Blazor.Client/Services/ILocalizationAdminApi.cs`, `Explore.Blazor.Client/Services/LocalizationAdminService.cs`, `Explore.Blazor.Client.Tests/Services/LocalizationAdminServiceTests.cs`, this context file, and `internationalization-translation-tasks.md`.
- **Behavior:** The admin write-only TMS API-key field now persists through the backend rotation endpoint during Save, clears the typed key only after successful rotation, and keeps it available for retry on failure. The misleading local-only clear action was removed because no backend delete endpoint exists. The live provider export card now names the action as TMS-to-static-bundle export and explicitly states force-offline mode disables that live-only mirror path.
- **Validation:** `aft_inspect` reported no diagnostics for the changed admin UI/service/test scope. Razor LSP is unavailable in this environment, so project compile/test is authoritative. `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed (1568 succeeded, 1 skipped) after adding rotate-key service tests and the UI copy/method rename.

### Phase 5 Task 5.3 — Language Picker Governance Alignment
- **Status:** Completed.
- **Changed files:** `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs`, `Explore.Application/DTOs/Onboarding/PublicExperienceSettingsDto.cs`, `Explore.Blazor.Client/Services/PublicExperienceService.cs`, `Explore.Blazor.Client/Layout/MainLayout.razor`, `Explore.Blazor.Client/Layout/MainLayout.razor.cs`, `Explore.Blazor.Client/Layout/NavMenu.razor`, `Explore.Blazor.Client/Layout/NavMenu.razor.cs`, `Explore.Blazor.Client.Tests/Common/PublicExperienceSettingsBuilder.cs`, `Explore.Blazor.Client.Tests/Layout/NavMenuAdminTests.cs`, `Event.Application.UnitTests/Features/PublicExperience/Queries/GetPublicExperienceSettingsQueryHandlerTests.cs`, this context file, and `internationalization-translation-tasks.md`.
- **Behavior:** `localization.client_picker_enabled` now flows through `ITranslationConfigResolver` into the anonymous-safe public experience settings payload. `MainLayout` and `NavMenu` default the picker visible, then pass the public `ClientPickerEnabled` flag into `LanguagePicker.Enabled` after settings load. The picker still uses the compile-time `CultureRegistry` allowlist; TMS-discovered languages remain reporting/configuration data, not runtime picker authority.
- **Validation:** `lsp_diagnostics`/`aft_inspect` reported no diagnostics on the changed public settings/test scope. `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed (1569 succeeded, 1 skipped). `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed (2092/2092) after adding the public settings handler test and translation config resolver substitute.

### Phase 6 Tasks 6.1–6.3 — Observability, Operations, And TMS Resource Decision
- **Status:** Completed; final verification exceptions documented.
- **Changed files:** `Explore.Application/Features/Localization/Handlers/Queries/ExportLocalizationBundleQueryHandler.cs`, `Explore.Application/Telemetry/TranslationMetrics.cs`, `docs/LOCALIZATION.md`, `docs/OPERATIONS.md`, this context file, and `internationalization-translation-tasks.md`.
- **Behavior:** Translation observability now covers runtime fetch count/duration, language changes, TMS connection tests, fallback activation, and static bundle import/export boundaries through the `Explore.Translation` meter. Metrics use safe low-cardinality tags only and explicitly avoid translation keys, bundle contents, raw provider payloads, and TMS secrets. The Blazor `TranslationService.T(key)` hot path remains synchronous/cache-only and uninstrumented. Local Aspire/Docker Tolgee/Weblate resources are deferred; connected-provider behavior is verified by generated-client/fake-HTTP tests and live smoke should use an operator-provided endpoint.
- **Validation:** `Event.Application.UnitTests` passed (2098/2098), `Explore.Infrastructure.Tests` passed (711/711), `Explore.Blazor.Client.Tests` passed on rerun (1570 total, 1569 succeeded, 1 skipped), `Event.Architecture.Tests` passed (263 total, 262 succeeded, 1 skipped), and `dotnet build --configuration Release --verbosity quiet` passed with known warnings only. `Event.API.IntegrationTests` failed outside localization: notification intent inserts violate `fk_notification_intents_notification_categories_category_id`, and a later run also hit Keycloak Testcontainers exiting with code 137. Manual Aspire smoke passed for public translation endpoints, language validation, Blazor shell/language picker rendering, Weblate root, and unauthenticated admin 401s; authenticated admin static/provider operations were not smoked because no local admin token/credentials were available.

## Handoff Notes

- **Current state:** All 24 implementation tasks are complete. Localization now has server-side TMS secret rotation, NSwag-generated Tolgee/Weblate provider clients, fallback/static-bundle metrics, static bundle schema/merge/write support, authorized static bundle import/export API plus Blazor service hooks, generated admin DTO alignment, verified BFF language/direction preference endpoints, admin UI secret/live-export affordance fixes, language-picker governance alignment, operator-facing observability docs, and a recorded decision not to add a dedicated localization/TMS intent in this slice.
- **Next action:** Resolve unrelated API integration notification/Keycloak failures, then run authenticated admin static bundle/provider smoke with credentials and an operator TMS endpoint.
- **Blockers:** Authenticated admin/live-provider smoke needs credentials/TMS endpoint. Full repo API integration is blocked by unrelated notification category FK failures and Keycloak container exits. Unrelated notification/listmonk worktree changes must remain untouched unless user approves.
- **Modified files:** Many localization files are modified across Application, Infrastructure, API, Blazor client services, docs, schemas, generated provider clients, and tests. Use `git status --short` and scope carefully; do not revert unrelated worktree changes.
- **Validation:** Final focused verification: Release build passed; `Event.Application.UnitTests` passed (2098/2098); `Explore.Infrastructure.Tests` passed (711/711); `Explore.Blazor.Client.Tests` passed on rerun (1569 succeeded, 1 skipped; one prior AI assistant rail failure was flaky/unrelated); `Event.Architecture.Tests` passed (262 succeeded, 1 skipped). Manual Aspire smoke passed for the public/localized surface listed above. `Event.API.IntegrationTests` failed for unrelated notification/Keycloak issues, not localization.
- **Documentation impact:** `docs/LOCALIZATION.md`, `docs/CONFIGURATION.md`, `docs/API.md`, `docs/BLAZOR.md`, `docs/OPERATIONS.md`, `docs/DEPLOYMENT_MODES.md`, and the active task checklist have been refreshed through Phase 6.3.
- **Risks:** Missing UI file-picker affordance for static imports, authenticated live-provider smoke, API integration suite health, and multi-replica writable bundle storage remain the likely hard parts.
- **Notes for next contributor/agent:** Reuse existing seams: provider clients stay generated from schema slices, static writes go through `IBundleFileWriter`, static reads go through `IStaticTranslationBundleReader`, translation cache invalidation goes through `ITranslationResolver`. Do not make static bundles the hosted primary path; they are no-TMS/fallback support.
