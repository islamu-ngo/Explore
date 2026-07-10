<!-- ABOUTME: Tactical checklist for API/TMS-primary internationalization and translation support. -->
<!-- ABOUTME: Tracks MasterCode API contract, Tolgee/Weblate integration, static fallback, UI, ops, and validation slices. -->
# Internationalization Translation — Task Checklist

Last Updated: 2026-07-10 Europe/Brussels

## Status Summary
- **Overall status:** Implementation complete; verification has documented environment/pre-existing exceptions.
- **Completed:** 24/24 implementation tasks
- **Current priority:** Resolve unrelated API integration notification/Keycloak failures before treating the whole repo as green.
- **Next recommended slice:** Authenticated admin smoke for static bundle import/export and live provider test/export with operator-provided admin credentials/TMS endpoint.

## Implementation Maintenance Rules
- [ ] Before starting work, read plan/context/tasks.
- [ ] After each completed task, update this checklist immediately.
- [ ] If implementation changes scope or architecture, update the plan before continuing.
- [ ] If discoveries affect future work, update the context file.
- [ ] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

## Phase 0: Plan Review And Baseline ✅ COMPLETED
- [x] **0.1 User reviews the corrected plan and approves or corrects scope.**
  - **Files:** `dev/active/internationalization-translation/internationalization-translation-plan.md`
  - **Acceptance:** Planning status changes from corrected draft to User-reviewed/Approved or plan is corrected.
  - **Validation:** User confirmation or explicit correction captured in context.
  - **Effort:** S
  - **Dependencies:** None
- [x] **0.2 Implementation agent confirms current repo state before first edit.**
  - **Files:** `dev/active/internationalization-translation/*`, changed target files from selected first slice
  - **Acceptance:** Agent re-reads relevant files and does not rely on stale planning assumptions.
  - **Validation:** Context updated with branch/current-state note.
  - **Effort:** S
  - **Dependencies:** 0.1
- [x] **0.3 Decide whether to add a localization/TMS intent.**
  - **Files:** `.claude/contract/intents.yaml` (existing, optional), `Event.Architecture.Tests` context tests if changed
  - **Acceptance:** Decision recorded; if intent is added, schema/context tests pass.
  - **Validation:** No new intent was added for this slice; localization remained under the existing cross-layer contract. `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed (263 total, 262 succeeded, 1 skipped).
  - **Effort:** S
  - **Dependencies:** 0.1

## Phase 1: API/TMS MasterCode Resolution Contract ✅ COMPLETED
- [x] **1.1 Verify and codify MasterCode translation key construction.**
  - **Files:** `Explore.Application/Localization/TranslationKeys.cs`, `Event.Application.UnitTests/Infrastructure/Localization/TranslationKeysTests.cs`, `docs/LOCALIZATION.md`
  - **Acceptance:** `lookup.{entity_type}.{master_code}.{field}` is documented/tested; no lookup translation uses database ID or localized label.
  - **Validation:** `aft_inspect` clean for changed scope; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed; `dotnet build --configuration Release --verbosity quiet` passed with 0 errors.
  - **Effort:** M
  - **Dependencies:** 0.2
- [x] **1.2 Prove API translation reads use the provider path first.**
  - **Files:** `Explore.Infrastructure/Localization/RuntimeTranslationProvider.cs`, `Explore.Infrastructure.Tests/Infrastructure/Localization/RuntimeTranslationProviderTests.cs`
  - **Acceptance:** Connected provider configured test returns provider values; successful empty connected-provider response stays empty; provider failure falls back to offline bundle; `tms_provider=None` uses offline provider directly.
  - **Validation:** `aft_inspect` clean for changed runtime/test files; `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed; `dotnet build --configuration Release --verbosity quiet` passed with 0 errors.
  - **Effort:** M
  - **Dependencies:** 1.1
- [x] **1.3 Validate language codes at the public translation API boundary.**
  - **Files:** `Explore.API/Controllers/TranslationController.cs`, `GetTranslationsQuery.cs`, `GetTranslationsQueryHandler.cs`
  - **Acceptance:** `en`, `fr`, `ar` succeed; malformed/unsupported language code returns controlled validation/problem response; provider/cache is not called with arbitrary code.
  - **Validation:** `lsp_diagnostics` clean for changed source/test files; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed (2076/2076); `dotnet build --configuration Release --verbosity quiet` passed; `Event.Architecture.Tests` passed; API integration translation smoke passed after `ApiEndpointSmokeTests` started sampling `/api/translation/en`. Full `Event.API.IntegrationTests` still has 2 unrelated event-registration notification-outbox failures.
  - **Effort:** S
  - **Dependencies:** 1.1
- [x] **1.4 Keep Blazor translation consumption API-backed.**
  - **Files:** `Explore.Blazor.Client/Services/TranslationService.cs`, `Explore.Blazor.Client/Services/MudBlazorLocalizer.cs`, Blazor client tests
  - **Acceptance:** No browser-side Tolgee/Weblate calls; no browser-side TMS secrets; `T(key)` remains synchronous/in-memory; cache refresh uses API client.
  - **Validation:** `lsp_diagnostics` clean for `TranslationServiceTests.cs`; service/contract search found no Tolgee/Weblate/TMS-secret references in translation consumption services; admin-only TMS wording remains isolated to `LocalizationAdminService`; `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed (1563 succeeded, 1 skipped).
  - **Effort:** S
  - **Dependencies:** 1.2

## Phase 2: Tolgee/Weblate Provider Contracts And Secrets 🟡 IN PROGRESS
- [x] **2.1 Verify existing secret-provider abstractions.**
  - **Files:** `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `Explore.Domain/Secrets/SecretDefinitionRegistry.cs`, `Explore.Domain/Secrets/SecretBinding.cs`, `Explore.Application/Contracts/Secrets/ISecretResolver.cs`, `Explore.Application/Contracts/Persistence/ISecretBindingRepository.cs`, localization config/admin/provider files.
  - **Acceptance:** Exact server-side service/interface for storing/retrieving TMS API key is identified and documented in context.
  - **Validation:** Evidence paths added to context. The existing server-side path is `SecretDefinitionRegistry` + `SecretBinding`/`ISecretBindingRepository` + `ISecretResolver`, but localization/TMS has no registered secret key, backend rotate endpoint, or provider auth wiring yet. `UpdateLocalizationGovernanceDto` excludes secrets, `LocalizationAdminController` never sets `TmsApiKeyConfigured`, and Blazor's local `TmsApiKey` field is not sent by `ILocalizationAdminService`/`LocalizationGovernancePayload`.
  - **Effort:** M
  - **Dependencies:** 0.2
- [x] **2.2 Inject Tolgee/Weblate auth headers from server-side secret flow.**
  - **Files:** `Explore.Infrastructure/Localization/TolgeeTranslationProvider.cs`, `Explore.Infrastructure/Localization/WeblateTranslationProvider.cs`, selected secret service files
  - **Acceptance:** Tolgee sends `X-API-Key`; Weblate sends `Authorization: Token`; API-key rotation stores a tenant-scoped inline-encrypted `SecretBinding`; admin configuration exposes only `TmsApiKeyConfigured`; no plaintext secret is returned to Blazor.
  - **Validation:** `lsp_diagnostics`/`aft_inspect` clean for changed source/test files; `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed (704/704); `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed (2080/2080). Build verification pending after docs refresh.
  - **Effort:** M
  - **Dependencies:** 2.1
- [x] **2.3 Align Tolgee export/import/read behavior with current Context7 docs.**
  - **Files:** `TolgeeTranslationProvider.cs`, Tolgee DTOs/contracts inside localization provider files, provider tests/docs
  - **Acceptance:** Tests assert documented route/method/query/header/body; export normalizes to flat `lookup.*`/`ui.*` dictionary; import sends compatible data via chosen documented Tolgee endpoint; stale endpoint support removed.
  - **Validation:** NSwag-generated provider client from `schemas/openapi-tolgee-provider.yaml`; fake HTTP tests assert `X-API-Key`, `GET /v2/projects/{id}/translations/{lang}?structureDelimiter=.`, and `POST /v2/projects/{id}/keys/import-resolvable`; `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed (709/709).
  - **Effort:** L
  - **Dependencies:** 1.1, 2.2
- [x] **2.4 Align Weblate export/import/read behavior with current Context7 docs.**
  - **Files:** `WeblateTranslationProvider.cs`, Weblate DTOs/contracts inside localization provider files, provider tests/docs
  - **Acceptance:** Tests assert token auth and `GET/POST /api/translations/{project}/{component}/{language}/file/`; conflict/add/fuzzy behavior documented; export normalizes to flat `lookup.*`/`ui.*` dictionary.
  - **Validation:** NSwag-generated provider client from `schemas/openapi-weblate-provider.yaml`; fake HTTP tests assert `Authorization: Token`, `GET/POST /api/translations/{project}/{component}/{language}/file/`, multipart JSON upload, and explicit `translate`/`process`/`replace` options; `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed (709/709).
  - **Effort:** L
  - **Dependencies:** 1.1, 2.2
- [x] **2.5 Verify fallback, retry, and metrics behavior for connected providers.**
  - **Files:** `RuntimeTranslationProvider.cs`, `TranslationMetrics.cs`, `TestTmsConnectionCommandHandler.cs`, resilience files/tests
  - **Acceptance:** Provider failures/rate limits/auth failures/malformed payloads activate offline fallback and safe metrics/logs without hiding connected-mode degradation.
  - **Validation:** Runtime fallback tests cover provider failure fallback and import failure metric recording; `ImportKeysAsync` now records `islamu.tms.fallback_activated_total`; focused infrastructure test project passed after unrelated notification compile blocker was left untouched per user instruction.
  - **Effort:** M
  - **Dependencies:** 2.3, 2.4

## Phase 3: Static Bundle Fallback For No-TMS Self-Hosters ✅ COMPLETED
- [x] **3.1 Formalize and test the fallback bundle schema.**
  - **Files:** `Explore.Infrastructure/Localization/Bundles/*.json`, `docs/LOCALIZATION.md`, provider tests in actual test project to verify/create
  - **Acceptance:** Flat JSON dictionary schema mirrors TMS keys; language filename rules, malformed-key behavior, and deterministic formatting are documented/tested; `en/fr/ar` validate.
  - **Validation:** `BundleSchema` validates flat JSON shape and deterministic ordering; embedded `en/fr/ar` starter bundles validate through provider tests; `Explore.Infrastructure.Tests` passed (709/709).
  - **Effort:** M
  - **Dependencies:** 1.1
- [x] **3.2 Merge embedded and writable offline bundles key-by-key.**
  - **Files:** `Explore.Infrastructure/Localization/OfflineTranslationProvider.cs`, provider tests in actual test project to verify/create
  - **Acceptance:** Embedded keys are always available; writable keys override embedded values; malformed local bundle does not hide embedded defaults.
  - **Validation:** Offline provider tests cover embedded defaults, writable override merge, writable-only key inclusion, and malformed writable fallback; `Explore.Infrastructure.Tests` passed (709/709).
  - **Effort:** M
  - **Dependencies:** 3.1
- [x] **3.3 Add direct static bundle import/export for no-TMS operators.**
  - **Files:** `Explore.Application/Contracts/Infrastructure/IBundleFileWriter.cs`, `Explore.Infrastructure/Localization/BundleFileWriter.cs`, `Explore.API/Controllers/LocalizationAdminController.cs`, admin service/UI files to verify
  - **Acceptance:** Authorized admin can validate/import/export bundle JSON without Tolgee/Weblate; invalid bundle returns safe error; raw file content is not logged.
  - **Validation:** Application command/query, API routes, and Blazor service hooks compile through `Explore.Infrastructure.Tests`; no raw bundle content is logged by import handler/service wrappers.
  - **Effort:** L
  - **Dependencies:** 3.1
- [x] **3.4 Invalidate fallback/provider resolver caches after any static/TMS bundle write.**
  - **Files:** `ExportFromTmsCommandHandler.cs`, `OfflineTranslationProvider.cs`, `TranslationResolver.cs`, static import/export handlers/endpoints
  - **Acceptance:** Same-process import/export followed by translation read returns newly written values without app restart.
  - **Validation:** `ExportFromTmsCommandHandler` and `ImportLocalizationBundleCommandHandler` both call `ITranslationResolver.InvalidateLanguageAsync(...)` after writes; offline provider exposes explicit per-language cache invalidation.
  - **Effort:** S
  - **Dependencies:** 3.2, 3.3
- [x] **3.5 Make bundle storage mode explicit for self-hosters.**
  - **Files:** `BundleFileWriter.cs`, `docs/LOCALIZATION.md`, `docs/CONFIGURATION.md`, `docs/DEPLOYMENT_MODES.md`, deployment docs/compose/AppHost files if changed
  - **Acceptance:** Docs explain local disk vs shared volume, backup/restore, writable path, bundle health meaning, and HA limitation.
  - **Validation:** `docs/LOCALIZATION.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, and `docs/DEPLOYMENT_MODES.md` document local/shared-volume storage, bundle health, backup/HA limitations, and the distributed writer seam.
  - **Effort:** M
  - **Dependencies:** 3.3

## Phase 4: API Boundary, OpenAPI, And Client Drift ✅ COMPLETED
- [x] **4.1 Add or align admin contracts for TMS and fallback bundle operations.**
  - **Files:** `LocalizationAdminController.cs`, localization DTOs/handlers, `LocalizationAdminService.cs`, admin UI state
  - **Acceptance:** OpenAPI shows intended operations; endpoints are `[Authorize]`; file size/content validation/errors are explicit; no raw file content or secrets in logs/responses.
  - **Validation:** Authorized admin endpoints now include static bundle GET/POST contracts plus Blazor service hooks; `lsp_diagnostics` clean and `Explore.Infrastructure.Tests` passed (709/709). API integration/manual smoke still pending.
  - **Effort:** M
  - **Dependencies:** 2.2, 3.3
- [x] **4.2 Verify OpenAPI/NSwag generation workflow.**
  - **Files:** repo OpenAPI/NSwag config files to locate; generated client files to locate
  - **Acceptance:** Exact command and generated file ownership recorded in context before client regeneration.
  - **Validation:** `Explore.API` Release build ownership for `schemas/openapi.json` and `Explore.Blazor.Client/nswag.json`/`EventApiClient.g.cs` ownership were verified; generated OpenAPI and Blazor client already include `ExportLocalizationBundle`, `ImportLocalizationBundle`, and `ImportLocalizationBundleDto`.
  - **Effort:** S
  - **Dependencies:** 4.1
- [x] **4.3 Regenerate and align localization admin client.**
  - **Files:** generated client files, `Explore.Blazor.Client/Models/Admin/LocalizationAdminState.cs`, `Explore.Blazor.Client/Models/Admin/LocalizationGovernancePayload.cs`
  - **Acceptance:** Admin state uses typed DTO properties; temporary `AdditionalProperties` reads and local payload shim are removed unless generator limitation is verified.
  - **Validation:** Stale `AdditionalProperties` reads and local `LocalizationGovernancePayload`/`ImportLocalizationBundleRequest` shims were removed; Blazor admin service now uses generated DTOs for governance/static bundle bodies; `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed (1566 succeeded, 1 skipped).
  - **Effort:** M
  - **Dependencies:** 2.2, 4.2

## Phase 5: Admin UI, BFF Preference, And Accessibility Completion ✅ COMPLETED
- [x] **5.1 Locate and verify BFF language/direction endpoints.**
  - **Files:** BFF preference endpoint extension file to locate; `Explore.Blazor.Client/Services/LanguagePreferenceService.cs`; `Explore.Blazor.Client/wwwroot/js/localization.js`
  - **Acceptance:** `/bff/language` and `/bff/direction` endpoint ownership, allowlist validation, cookie settings, and antiforgery posture are verified.
  - **Validation:** Existing BFF flow verified in `BffPreferenceEndpoints`, `BffPreferenceValidationService`, `BffPreferenceAntiforgeryTests`, `BffPreferenceValidationEndpointsTests`, `BffPreferenceValidationServiceTests`, and `BffPreferenceCookieServiceTests`; language/direction mutations are allowlisted, antiforgery-protected, and cookie-persisted safely.
  - **Effort:** S
  - **Dependencies:** 0.2
- [x] **5.2 Complete admin UI accessibility and live/fallback mode behavior.**
  - **Files:** admin localization Razor component path to verify; `LocalizationAdminState.cs`; `LocalizationAdminService.cs`
  - **Acceptance:** Fields have labels; dynamic errors/status use alert/status patterns; secret field is write-only; live Tolgee/Weblate and static fallback modes are clear; force-offline disables live-only actions; CSS uses logical properties.
  - **Validation:** Write-only TMS key saves now call backend rotation; the misleading clear action was removed because there is no delete endpoint; force-offline disables the live-only TMS-to-static export; the export card copy names the live provider mirror path explicitly; `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed (1568 succeeded, 1 skipped).
  - **Effort:** M
  - **Dependencies:** 2.2, 4.3
- [x] **5.3 Keep language picker aligned with governance state.**
  - **Files:** `Explore.Blazor.Client/Shared/LanguagePicker.razor`, caller/layout file supplying `Enabled`, related state/bootstrap file
  - **Acceptance:** Picker visibility reflects `localization.client_picker_enabled`; available languages reflect the chosen v1 policy; tests cover disabled/enabled behavior.
  - **Validation:** Public experience settings now expose `ClientPickerEnabled`; `MainLayout` and `NavMenu` pass it to `LanguagePicker.Enabled`; `NavMenuAdminTests` covers picker suppression when public settings disable it; `Explore.Blazor.Client.Tests` passed (1569 succeeded, 1 skipped); `Event.Application.UnitTests` passed (2092/2092) after adding public settings handler coverage.
  - **Effort:** S
  - **Dependencies:** 5.1

## Phase 6: Observability, Operations, And Deployment Docs ✅ COMPLETED
- [x] **6.1 Close translation metric recording gaps.**
  - **Files:** `Explore.Application/Telemetry/TranslationMetrics.cs`, `TestTmsConnectionCommandHandler.cs`, provider/runtime files as needed
  - **Acceptance:** Connection test, live/fallback mode, fallback activation, static import/export validation, and provider parse failures record safe metrics; hot path stays uninstrumented.
  - **Validation:** `TranslationMetrics` now covers runtime fetches/durations, language changes, connection tests, fallback activation, and static bundle import/export boundaries; `ExportLocalizationBundleQueryHandler` was made async-compatible for the static export metric and `Event.Application.UnitTests` passed (2092/2092).
  - **Effort:** S
  - **Dependencies:** 2.5
- [x] **6.2 Update hosting/localization/config/API/Blazor/operations docs.**
  - **Files:** `docs/LOCALIZATION.md`, `docs/CONFIGURATION.md`, `docs/API.md`, `docs/BLAZOR.md`, `docs/OPERATIONS.md`, `docs/DEPLOYMENT_MODES.md` as applicable
  - **Acceptance:** A self-hoster can configure Tolgee, Weblate, or no-TMS static fallback from docs without source reading; docs match final provider auth/endpoints, MasterCode key rules, cache invalidation, HA limitation, config keys, metrics, and validation commands.
  - **Validation:** `docs/LOCALIZATION.md` now documents safe metric names/tags and the no-hot-path-instrumentation rule; `docs/OPERATIONS.md` documents operator telemetry for fallback and static bundle operations.
  - **Effort:** M
  - **Dependencies:** 1-6 implementation tasks
- [x] **6.3 Decide whether to add Aspire/Docker Tolgee/Weblate local resources.**
  - **Files:** `Explore.AppHost`/compose files/docs to verify
  - **Acceptance:** Decision recorded; if resources added, local setup docs and health checks are updated.
  - **Validation:** Deferred: no new Aspire/Docker Tolgee/Weblate resources in this slice. Existing connected-provider behavior is covered by generated-client/fake-HTTP tests; live-provider validation remains a manual smoke step against an operator-provided Tolgee/Weblate endpoint.
  - **Effort:** M
  - **Dependencies:** 6.2

## Verification Checklist
- [x] LSP diagnostics clean for modified files.
- [x] `dotnet build --configuration Release --verbosity quiet` passes.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passes.
- [x] Intent/path minimum test projects pass individually with `dotnet test --project ...` for Application, Infrastructure, Blazor client, and architecture slices.
- [x] MasterCode/API tests cover lookup translation keys and provider-first connected mode.
- [x] Tolgee provider tests cover current export/import/read endpoints, payload parsing, and auth header behavior.
- [x] Weblate provider tests cover file download/upload endpoints, conflict/fuzzy/method form behavior, and auth header behavior.
- [x] Static fallback tests cover schema, deterministic writing, embedded+writable merge, direct static export seam, and cache invalidation call sites.
- [ ] API integration tests cover translation/admin endpoint changes and no-secret responses. Public translation smoke passed manually; full `Event.API.IntegrationTests` currently fails outside localization: notification intent FK violations on `notification_intents.category_id` and Keycloak Testcontainers exits with code 137.
- [x] Blazor client tests cover Phase 1.4 translation consumption boundary, Phase 4.3 generated admin DTO alignment, Phase 5.2 TMS API-key rotation/admin UI service affordances, and Phase 5.3 language-picker governance suppression.
- [ ] Manual smoke covers language switch, API translation fetch, one MasterCode lookup translation, provider configured path, provider failure fallback, static bundle import/export, and admin config/test/export failure path. Completed anonymous/local Aspire smoke for API translation fetch, language validation, Blazor shell/language picker, Weblate root, and unauthenticated admin 401s; authenticated admin/static/provider smoke needs admin credentials and an operator-provided TMS endpoint.
- [x] Dev docs refreshed through Phase 6.3 with generated provider clients, fallback metrics, static bundle merge/schema, admin static bundle contracts, generated admin DTO alignment, BFF preference verification, admin UI secret/live-export affordances, language-picker governance alignment, translation metric documentation, and Aspire/Docker TMS resource deferral.

## Remaining / Deferred Work
- HA-safe object-store `IBundleFileWriter` implementation is deferred unless deployment requires multi-replica writes beyond shared-volume support.
- Local Aspire/Docker Tolgee/Weblate resources are deferred; use operator-provided TMS endpoints for live-provider manual smoke.
- Additional TMS providers beyond Tolgee/Weblate are deferred.
- Protocol/federation-level translation records are deferred until public federation endpoints require them.
- Regional cultures such as `en-US`/`fr-BE` are deferred; `CultureRegistry` intentionally accepts only two-letter codes for v1.
