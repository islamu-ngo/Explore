<!-- ABOUTME: Re-baselined implementation plan for backend/API health and contract hardening. -->
<!-- ABOUTME: Converts the broad refactor backlog into source-grounded, reviewable Clean Architecture slices. -->

# Backend API Health Refactor - Implementation Plan

Last Updated: 2026-07-04 Europe/Brussels

## 0. Planning Metadata

- **Request:** Update `dev/active/backend-api-health-refactor` so the implementation plan is relevant, correct, and aligned with Senior CTO feedback, repo conventions, Context7 documentation, Tavily research, Clean Architecture, and enterprise self-hostable expectations.
- **Task directory:** `dev/active/backend-api-health-refactor/`
- **Planning status:** Implementation in progress; bounded health slice, generated inventory source fix, R-015 runtime migration endpoint retirement, R-020 event-template HAL affordance hardening, R-012 storage public-read hardening, R-014 API-key tenant execution hardening, R-036 email dispatch admin authorization parity, R-037 user-organization self-service read hardening, R-038 module governance write hardening, R-040 user-authentication-token self-service hardening, R-041 external API-key management hardening, R-004 setup/bootstrap/Keycloak maintenance safety coverage, R-007 event-aspect controller decomposition slice, R-010 organization, instance-bootstrap-state, and EventSessionSpeaker repository cancellation slices, Phase 1.4 persistence hardening for notification fanout workers, storage reconciliation guards, legacy slug lookup normalization, and registration capacity/session invariants completed with focused verification. R-029 EventSessionSpeaker Application prerequisite plus API/HAL/generated-client management contract are complete; Blazor service/dialog replacement remains next.
- **Primary outcome:** A future implementation agent can continue from this workstream without repeating broad discovery or following stale/bad tasks.
- **Matched intent:** No exact single intent exists for this broad workstream in `.claude/contract/intents.yaml`. Use this fallback contract and then classify each implementation slice under the narrower existing intents: `openapi-contract-change`, `add-get-endpoint`, `add-write-endpoint`, `add-hal-link`, `add-cqrs-handler`, `update-repository-query`, `add-ef-migration`, `blazor-component-affordance`, `bff-auth-bug`, and `cerbos-policy-change` where applicable.
- **Relevant skills loaded:** `senior-cto-feedback`, `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`, `error-tracking`, `source-command-check`.
- **Relevant rules loaded:** `.claude/rules/api-controllers.md`, `.claude/rules/api-hateoas.md`, `.claude/rules/application-layer.md`, `.claude/rules/efcore-persistence.md`, `.claude/rules/tests.md`.
- **Primary layers touched by future implementation:** API, Application, Persistence, Infrastructure, Domain, Blazor client affordance tests, docs, and DevOps evidence.
- **Estimated complexity:** XL. The work crosses security, tenant isolation, HAL authorization, OpenAPI/client generation, CQRS handler contracts, EF query behavior, background side effects, and self-hosting operations.

## 1. Executive Summary

This workstream is a backend/API platform-health refactor, not a feature build and not a generic cleanup pass. It should close known security, contract, authorization, tenant-isolation, operational-health, CQRS, and persistence debts in small, reviewable slices.

The previous plan was directionally useful but too broad and partially stale. It mixed completed implementation history, future work, mega-refactors, behavior tests mislabeled as architecture tests, and speculative tasks that were not tied to source evidence. This re-baseline keeps the valid direction but changes the execution model:

- preserve the current `/health`, `/alive`, and `/metrics` operational contract instead of inventing new endpoints;
- harden security and API/HAL contracts before controller or Application refactors;
- split behavior tests from architecture guardrails;
- avoid broad repository/domain rewrites unless a slice proves a concrete risk;
- treat generated OpenAPI/client artifacts as regenerated evidence, never hand-edited source;
- keep self-hosting, tenant isolation, auditability, idempotency, and observability as first-class acceptance criteria.

Out of scope for this plan: Blazor visual redesign, broad design-system changes, non-backend feature expansion, and compatibility shims for obsolete pre-v1 API shapes.

### CTO Re-baseline Correction - 2026-07-04

This refresh rejects the remaining broad cleanup tasks as implementation drivers. They name valid engineering concerns, but they are not safe work items by themselves in a dirty, high-churn, security-sensitive backend. Future agents must not start from a generic instruction such as "split another controller", "normalize query contracts", "quarantine hard delete", "add cursor pagination", "review indexes", or "harden outbox boundaries" unless a concrete risk card first names the source files, current behavior, expected behavior, acceptance criteria, and exact test lane.

The corrected execution model is:

1. Start from `backend-contract-risk-register.md` or `docs/API_CONTRACT_INVENTORY.md`.
2. Select exactly one open risk or one newly verified source defect.
3. Read the owning source files and matching `.claude/rules/*` before editing.
4. Write or update characterization tests at the correct layer before behavior refactor when practical.
5. Implement the smallest Clean Architecture slice that closes that risk.
6. Regenerate OpenAPI/client artifacts only when source API contracts changed.
7. Update plan/context/tasks/risk register in the same slice.

Context7 official ASP.NET Core and EF Core documentation was rechecked during this CTO refresh. Tavily MCP was also called as requested, but the configured Tavily plan returned `status 432` quota exhaustion; primary web fallback was used only for OWASP and Microsoft source confirmation. Do not treat Tavily as unavailable permanently, but record quota exhaustion when it happens instead of pretending research succeeded.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| Runtime health endpoints already exist and use project-specific paths. | CodeGraph verified `Explore.ServiceDefaults/Extensions.cs::MapDefaultEndpoints`: `/health` filters `ready`, `/alive` filters `live`, `/metrics` maps Prometheus scraping. | High | Do not plan `/health/ready` or `/health/live` unless intentionally changing the platform contract. |
| Health status code policy is already explicit. | `MapDefaultEndpoints` maps `Healthy` and `Degraded` to 200, `Unhealthy` to 503. | High | Matches `docs/OPERATIONS.md`. |
| Health response no longer serializes raw check exception messages. | `Explore.ServiceDefaults/Extensions.cs` delegates response writing to `HealthCheckResponseWriter`, which redacts exception text and sensitive data before serializing JSON. | High | Implemented 2026-07-03 with focused API integration tests. |
| Microsoft guidance supports separate readiness and liveness probes with tags/predicates. | Context7 `/dotnet/aspnetcore.docs`; Tavily extracted Microsoft Learn ASP.NET Core health-check docs. | High | Repo uses `/health` and `/alive`, which is valid; path names are project policy. |
| Microsoft guidance supports endpoint metadata as the durable API-policy boundary. | Context7 `/dotnet/aspnetcore.docs` confirmed endpoint authorization, named rate-limiting policies, output-cache policies, and OpenAPI operation metadata/transformers are first-class endpoint concerns. | High | Keep route names/operation IDs, auth classification, tenant mode, rate-limit policy, and cache policy source/generated rather than manually maintained in planning tables. |
| Microsoft guidance supports OpenTelemetry/Prometheus `/metrics`. | Tavily extracted Microsoft Learn ASP.NET Core metrics docs; repo maps `/metrics`. | High | Keep metrics low-cardinality per repo observability rules. |
| API and operations docs define `/alive`, `/health`, `/metrics`. | `docs/API.md`, `docs/OPERATIONS.md`, `docs/SELF_HOSTING.md`, `docs/MCP_DEBUGGING.md`, `docs/CONFIGURATION.md`. | High | Product-doc `/health/ready` drift was corrected on 2026-07-03. |
| Phase 0 support artifacts exist. | `endpoint-inventory.md`, `endpoint-classification.md`, `backend-contract-risk-register.md`, `authorization-policy-matrix.md`, `tenant-execution-model.md`, `api-error-catalog.md`. | High | They remain useful but some are stale and need regeneration/reconciliation. |
| Generated API inventory now follows the build-time contract source. | 2026-07-03 generator fix: `ApiContractInventoryGeneratorTests` reads `schemas/openapi.json`; regenerated `docs/API_CONTRACT_INVENTORY.md` currently has `365` paths, `506` operations, `0` missing operation IDs, `0` missing endpoint classes, route-name values, and tenant/rate/cache posture columns. | High | R-032 is mitigated. Use the generated inventory as the current per-operation table; keep the workstream table as historical context only. |
| `/admin/migrate` was a runtime-only Development/Testing endpoint risk, not a generated public contract row. | CodeGraph/source read originally found `Explore.API/Program.cs` mapping `POST /admin/migrate` only in Development/Testing, protected only by authorization metadata, lacking endpoint classification, and returning raw migration exception text on failure. The 2026-07-04 implementation removed the route and orphan `RouteNames.ApplyDatabaseMigrations` constant; `AdminMigrationEndpointRetirementTests.AdminMigrate_Post_IsNotMappedInTestingHost` proves `404` in the Testing host. | High | R-015 is mitigated by deletion. Startup migrations and `Event.MigrationService` remain the supported migration paths. |
| Webhook message/delivery operations are present in the regenerated inventory and now have risk-focused tests. | `docs/API_CONTRACT_INVENTORY.md` includes `GET /api/webhooks/messages`, `GET /api/webhooks/messages/{messageId}`, `GET /api/webhooks/delivery-attempts`, `GET /api/webhooks/delivery-attempts/{attemptId}`, and `POST /api/webhooks/delivery-attempts/{attemptId}/retry` from `schemas/openapi.json`. The 2026-07-04 R-033 slice added API, Application, and Persistence tests for auth actions, HAL retry gating, safe DTO shape, tenant-bound handler/repository calls, and retry outcome mapping. | High | R-033 is mitigated. Keep future webhook changes behind the same CQRS/MediatR, HAL, and generated-contract workflow. |
| Report/moderation families are security-sensitive and now have a bounded privacy mitigation. | Source/search evidence for `EventReportsController` and `ModerationReportController`; generated artifacts expose these operation families. R-033 has closed the outgoing webhook management/audit gap, R-034 has closed the incoming webhook public-ingestion gap, and R-035 has removed unused identity/provider-pointer fields from moderation read projections while preserving event-resource authorization and HAL action gating. | High | Keep future report/moderation changes behind the same CQRS authorization, HAL affordance, DTO minimization, and generated-contract workflow. |
| Storage object metadata/content/presigned reads are no longer anonymous public data. | R-012 changed `StorageObjectController`, storage read queries, `AuthorizationBehavior`, Cerbos/local fallback, and storage HAL policies so metadata uses `view`, content uses `download`, presigned generation uses `presigned_download`, and only `/api/storageobject/{id}/public` remains anonymous for active public-image content. | High | Regenerate OpenAPI/inventory/client artifacts after endpoint metadata changes; keep arbitrary-key and cached presigned URL routes retired. |
| Email dispatch admin status/control is tenant-scoped and resource-authorized. | R-036 changed EmailDispatch status, tenant pause/resume, park, and replay requests to use `ResourceKinds.EmailDispatch` with explicit `view`, `manage_tenant`, `park`, and `replay` actions. HAL replay/park links, Cerbos policy/schema, local fallback, and machine scopes now use the same action vocabulary. | High | Keep EmailDispatch operator APIs tenant-admin/instance-admin only; do not expose recipient/body/subject/provider-error fields; regenerate OpenAPI/inventory/client artifacts after response metadata or route changes. |
| User organization membership reads are self-service and enforced in the Application layer. | R-037 changed `GetUserOrganizationsRequestHandler` to resolve `ICurrentUserService.UserId`, fail closed on missing identity, and reject route/current-user mismatches before repository access. `UserController.GetUserOrganizations` now keeps only the transport-level current-user resolution for clean `401` and advertises `401`/`403` ProblemDetails metadata. | High | Keep `/api/user/{userId}/organizations` self-service only. A future admin/organization-owner projection must be a separate resource-authorized query with minimized DTOs and HAL affordance tests. |
| Module enable/disable writes are tenant-scoped and resource-authorized. | R-038 changed `ModuleController` enable/disable actions to dispatch `EnableTenantModuleCommand` and `DisableTenantModuleCommand` through MediatR. Both commands implement `ISecureRequest` with `[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]` and tenant/module/action attributes; the enable handler resolves the audit actor through `IAdminContext`, and route metadata now advertises the `Write` rate-limit policy. | High | Keep module capability changes under tenant update authority unless a complete `Modules.Manage` family is added with resource/action constants, Cerbos policy/schema, local fallback, machine scopes, HAL metadata, and tests together. |
| Analytics relay is intentionally anonymous but now has explicit public-ingestion evidence. | R-039 verification found `AnalyticsRelayController.Relay` is `[AllowAnonymous]`, `EndpointClass.Public`, named `RouteNames.RelayAnalyticsEvent`, and protected by `RateLimitingExtensions.AnalyticsRelayPolicy`. The Application handler resolves tenant analytics configuration, applies server-side tenant context, bounds accepted event/property shapes, and runs `AnalyticsGovernanceService` before provider dispatch. | High | Keep `/api/a/t` as first-party public ingestion only: no auth policy, no HAL mutation rel, no public output cache, dedicated limiter, and server-side governance/sanitization tests. |
| Setup-secret bootstrap endpoints now have consistent limiter, error-contract metadata, provider-failure mapping, structured audit emission, and Keycloak maintenance safety coverage. | R-004 mitigation changed `InstanceOnboardingController` so every setup-secret-gated action declares the `SetupSecret` rate-limit policy plus typed RFC 7807 `403`/`410`/`429` metadata; `SetupSecretRequiredAttribute` now returns central `ProblemDetails` for invalid setup secrets and inactive setup mode. The 2026-07-04 audit slice added Application-level instance-bootstrap audit events and structured `ILogger` emission from the setup-secret filter, `validate-secret`, Keycloak bootstrap handler, and onboarding completion handler for accepted/rejected/inactive setup-secret checks, Keycloak bootstrap start/success/failure, and setup-mode disablement. The follow-up provider-failure slice maps Keycloak bootstrap timeout/unreachable failures to `503 Service Unavailable` and invalid/upstream Keycloak Admin API failures to `502 Bad Gateway`, while operator input, credentials, unsafe URLs, and missing realm/client material remain validation failures. Keycloak maintenance source was rechecked against Context7 official Keycloak docs and Tavily results from keycloak.org: sync apply is backup-confirmed and non-destructive; client-secret rotation is an explicit admin action with application-managed/deployment-managed ownership semantics. `Explore.Infrastructure.Tests/Infrastructure/KeycloakBootstrapServiceTests` covers backup-required sync apply, additive-only Admin API calls, no DELETE calls, and redaction. `RotateKeycloakClientSecretCommandHandlerTests` now covers success, deployment-managed instructions, validation failures, and blocked provider results that do not persist the replacement secret or reload auth schemes. | High | R-004 is mitigated for the verified bootstrap/admin safety surface. Do not keep a vague "missing provider" task unless a distinct scenario is verified from source. |
| Event registration reads are now self-scoped and identity-minimized. | R-016 changed EventRegistration controller/handler/repository flow so generic list/detail/by-session/by-user reads require the authenticated current user, apply current-user ownership predicates, return 403 on by-user mismatch before MediatR dispatch, and regenerate OpenAPI/client artifacts without serialized registrant id/name/email fields. | High | R-016 is mitigated for self-service reads. A separate resource-authorized organizer/admin attendee-management projection is required before attendee identity can be exposed. |
| Event-template admin affordances now use HAL collection/item links. | R-020 changed `EventTemplateCollectionLinkPolicy`, Blazor HAL mapping, `PaginatedResult<T>`, and `EventTemplateListPage` so create comes from collection `_links.create` and row edit/delete comes from row `_links`. | High | R-020 is mitigated for the confirmed event-template surface. Continue auditing other UI surfaces under the same HAL-only action-gating rule. |
| R-029 EventSessionSpeaker Blazor stub cannot be fixed by direct wiring. | 2026-07-04 source recheck: `Explore.Blazor.Client/Services/EventSessionSpeakerService.cs` returns empty/null/false; `IEventSessionSpeakerService` exposes `object` payloads; `ManageSpeakersDialog.razor` discards service results and shows success after remove without checking the service result. The Application prerequisite is complete: create/delete commands authorize the owning event session and carry tenant/event attributes, create validates same-tenant session/actor ownership, duplicate assignment, tenant stamping, and cache invalidation, delete rejects mismatched owning-session/tenant requests before deletion/cache invalidation, update rejects route/session mismatches, and update command authorization attributes are aligned with tenant/event metadata. The API/HAL contract is also complete: authenticated nested management routes, route names, EventSession `speakers` HAL affordance, EventSessionSpeaker edit/delete HAL policies, generated OpenAPI/inventory rows, and generated `EventApiClient` methods now exist. | High | Remaining sequence is Blazor typed service/dialog behavior using generated client methods and HAL links. Do not hand-roll Blazor HTTP calls, do not expose assignment/concurrency management DTOs through anonymous public reads, and do not revive the removed R-006 affordance outside the new source-backed management route. If public speaker display is needed, add a separate minimized public projection instead of reusing the management relationship contract. |
| Prior Blazor, Docker, Testcontainers, and no-Keycloak API host blockers were rechecked. | 2026-07-03 scoped verification: `Explore.Blazor.Client` Release build passed; `docker info` returned daemon version `29.5.3`; `NoKeycloakAuthenticationTests` passed 8/8; `DatabaseSeederTests` passed 2/2 against PostgreSQL Testcontainers. | High | Full API/persistence suites were intentionally not launched during blocker classification. |
| Architecture tests are green after unrelated agent-context drift cleanup. | `Event.Architecture.Tests` passed on 2026-07-04: 240 total, 239 succeeded, 1 intentional skip. The cleanup added `Explore.Infrastructure.Tests` to the manifest test's known projects, restored the active AI-context disclosure policy docs referenced by `update-ai-context-disclosure`, and corrected the `UserPii` summary count in `docs/AI_CONTEXT_SECURITY.md`. | High | Keep this separate from backend/API health behavior evidence; it proves the context-governance lane is no longer blocking. |
| Tenant filters now fail closed. | `backend-api-health-refactor-context.md` records 2026-06-14 verification; `docs/MULTI_TENANCY.md` states missing tenant no longer broadens to all tenant rows. | Medium-High | Future work should prove bypass call sites, not reimplement fail-closed filters from scratch. |
| `TenantLookupSource`, `TenantCapabilityRepository`, `ExternalApiKeyRepository`, `ExternalApiKeyQuotaRepository`, `EventRepository`, the bounded `StorageObjectRepository` delete-requested resource path, `UserExternalLoginRepository`, `TenantUserRoleGrantRepository`, `TenantUserRepository`, `TenantSettingRepository`, `TenantSettingsDocumentRepository`, `NotificationRepository`, `EmailDispatchOutboxRepository`, and the webhook repository family are semantically covered. | `Event.Persistence.IntegrationTests/TenantIsolation/TenantLookupSourceBypassTests.cs` proves `TenantLookupSource.GetTenantLookupsAsync` can warm active tenant domain lookups across an ambient tenant context while excluding inactive tenant settings. `Event.Persistence.IntegrationTests/TenantIsolation/TenantCapabilityRepositoryBypassTests.cs` proves `TenantCapabilityRepository` can resolve explicitly requested tenant-module capabilities from an ambient different-tenant context without returning ambient-tenant rows. `Event.Persistence.IntegrationTests/TenantIsolation/ExternalApiKeyRepositoryBypassTests.cs` proves external API-key authentication and platform-management bypasses are bounded by exact `KeyId`, exact key ID, and InstanceAdmin owner/name predicates. `Event.Persistence.IntegrationTests/TenantIsolation/ExternalApiKeyQuotaRepositoryBypassTests.cs` proves platform-wide quota usage reporting is bounded by requested period and API-key aggregation; the slice also adds a tenant query filter to `ExternalApiKeyQuota` through its required `ExternalApiKey` navigation so normal quota reads are tenant-scoped. `Event.Persistence.IntegrationTests/TenantIsolation/EventAuthorizationTargetBypassTests.cs` proves event authorization-target resolution is bounded by exact event ID. `Event.Persistence.IntegrationTests/TenantIsolation/StorageObjectRepositoryBypassTests.cs` proves delete-requested storage reconciliation is bounded by explicit tenant/resource/lifecycle/provider predicates. `Event.Persistence.IntegrationTests/TenantIsolation/UserExternalLoginRepositoryBypassTests.cs` proves external-login authentication is bounded by exact provider plus provider key. `Event.Persistence.IntegrationTests/TenantIsolation/TenantUserRoleGrantRepositoryBypassTests.cs` proves tenant role-grant authority checks and user membership enumeration are bounded by explicit tenant/user/role or requested-user predicates. `Event.Persistence.IntegrationTests/TenantIsolation/TenantUserRepositoryBypassTests.cs` proves tenant-local membership, actor lookup, and active-state checks are bounded by explicit tenant/user or tenant/actor predicates and exclude suspended or soft-deleted memberships. `Event.Persistence.IntegrationTests/TenantIsolation/TenantSettingRepositoryBypassTests.cs` proves tenant-setting override reads, list, lock, unlock, and remove operations are bounded by explicit tenant/key predicates and mutate only the requested tenant. `Event.Persistence.IntegrationTests/Repositories/TenantSettingsDocumentPersistenceTests.cs` proves typed tenant-settings document reads are tenant-filtered normally, resolve only the explicit tenant/document key from a wrong ambient tenant, and return only requested tenant keys for batch reads. `Event.Persistence.IntegrationTests/TenantIsolation/NotificationRepositoryBypassTests.cs` proves notification deduplication bypasses are bounded by the exact tenant/user/deduplication-key tuple and do not match same-key rows under the wrong tenant or user. `Event.Persistence.IntegrationTests/TenantIsolation/EmailDispatchOutboxRepositoryBypassTests.cs` proves email-dispatch worker queue, tenant-operator, and receipt-idempotency bypasses are bounded by due/pending, retry-throttle, tenant-pause, exact dispatch ID, explicit tenant/outbox/publish-event, and tenant/publish-event predicates. `Event.Persistence.IntegrationTests/TenantIsolation/WebhookRepositoryBypassTests.cs` proves webhook tenant-operation and worker-queue bypasses are bounded by explicit tenant/name/id/provider/message predicates plus due, pending, stale, expired-payload, exact-claim, and status-refresh predicates. | High | These cover the `TenantLookupCacheWarmup`, `TenantCapabilityResolution`, `ExternalApiKeyAuthentication`, `ExternalApiKeyPlatformManagement`, `ExternalApiKeyPlatformUsageReport`, `EventAuthorizationTargetResolution`, one bounded `InstanceStorageAdministration` reconciliation path, `UserExternalLoginAuthentication`, `TenantUserRoleGrantRepository` exact-tenant/user-membership paths, `TenantUserRepository` exact tenant membership/actor/active-state paths, `TenantSettingRepository` exact-tenant setting override paths, `TenantSettingsDocumentRepository` exact-tenant typed settings document paths, `NotificationRepository` exact tenant/user/key deduplication path, `EmailDispatchWorkerCrossTenantQueue`, `EmailDispatchTenantOperation`, `WebhookTenantOperation`, and `WebhookWorkerCrossTenantQueue` only; Phase 1.4 remains open for the other production bypass call sites. |
| Current Phase 1.4 persistence continuation adds notification fanout worker and registration-capacity hardening. | `NotificationFanoutRunRepository.GetPendingBatchAsync` now uses `NotificationFanoutWorkerCrossTenantQueue` and non-positive batch fail-closed behavior; `NotificationFanoutRunRepositoryBypassTests` proves ambient reads stay tenant-filtered while worker polling returns pending cross-tenant rows only. `EventRegistrationIntentRepository.CreateWithChildrenAndCapacityAsync` now validates child tenant/event/session ownership before tenant/event/session-bound capacity reservation; `EventRegistrationIntentRepositoryTests` proves cross-event session input is rejected before counters or rows are written. `StorageObjectRepositoryBypassTests` now also covers invalid reconciliation bounds and blank-provider/empty-key known-key queries. | High | Focused verification passed: notification fanout 1/1, storage bypass/guards 2/2, registration intent repository 7/7, tenant-filter architecture guard 4/4, focused API build 7 projects / 0 errors, and touched-file `git diff --check`. Full solution build remains blocked by unrelated solution-wide analyzer/package issues. |
| Route-name bidirectional guardrails are active and current. | 2026-07-04 Phase 2.3 re-read `Event.API.IntegrationTests/Features/RouteNameCoverageTests.cs` and ran the current branch guardrail: every `RouteNames` constant resolves to exactly one registered endpoint, every named endpoint has a matching `RouteNames` constant, and the route catalog sanity check passed. | High | Context7 ASP.NET Core docs confirm endpoint names are global, case-sensitive URL-generation identifiers that also feed OpenAPI operation IDs. Tavily was attempted but unavailable due quota exhaustion for this slice. |
| API ProblemDetails cleanup and representative behavior coverage are complete for audited patterns. | 2026-07-04 Phase 2.1 audit used Context7 `/dotnet/aspnetcore.docs`, Tavily OWASP error-handling research, CodeGraph, and `rg` sweeps to close the final direct controller helper path in `EventSessionLanguageController.Update`. Phase 2.2 then added authorization-middleware 401/403 ProblemDetails handling, `rate_limited` 429 metadata, and `ProblemDetailsContractTests` coverage for representative 400, 401, 403, 404, 409, 429, and 500 shapes. | High | Do not run another broad controller-helper or ProblemDetails behavior sweep unless new source evidence appears. |
| External API-key management is hardened for the verified credential-management defects. | R-041 removed shared output caching from `ExternalApiKeyController` reads/reports, added no-store response metadata and named `Authenticated`/`Write` rate-limit metadata, changed create/report owner-authority denials to `AuthorizationException`/403, changed delete to return 404 when revoke finds no visible key, fixed platform-scoped key-name uniqueness to use `ExistsByOwnerAndNameIgnoringTenantFilter`, and propagated cancellation tokens through changed repository reads/exists checks. Generated inventory rows 231-236 now show no cache policy and the expected rate-limit policies. | High | R-041 is mitigated for the audited surface. Keep API-key management as credential/admin data: no shared output cache, explicit limiter metadata, typed 401/403/404 ProblemDetails, fail-closed Application authorization, and platform-key uniqueness through an intentional tenant-filter bypass. Add a distinct `ApiKeys.*` resource/action family only as a complete Cerbos/fallback/HAL/machine-scope parity slice. |
| Instance-admin API-key tenant execution now fail-closes for tenant-scoped calls without tenant context. | R-014 changed `ApiTenantPostAuthenticationMiddleware` so `InstanceAdmin` keys are nullable platform credentials but not blanket tenantless execution. Explicit tenant hints bind the request tenant for tenant-scoped API calls, single-tenant mode binds the default tenant, unresolved tenant-scoped API/MCP calls return `404 tenant_required`, and only explicit host-administration API routes continue without tenant context. | High | R-014 is mitigated for the audited API-key execution surface. Keep the host-administration allowlist narrow and source-reviewed; any new tenantless execution path must be explicit, logged, documented, and tested. |
| Event aspect endpoints now have a dedicated controller behind stable route contracts. | R-007 first slice moved Islamic and tech aspect read/upsert/delete actions from `EventController` to `EventAspectController`. The new controller keeps `[Route("api/event")]`, `[Tags("Event")]`, the same six `RouteNames`, the same `/api/event/{id}/aspects/...` templates, read `DetailData` output-cache metadata, public/authenticated split, and ProblemDetails metadata. | High | R-007 is partially mitigated. The event-aspect split is complete and should not be repeated; `EventController` still has other event CRUD/lifecycle/read responsibilities, so future decomposition must select a new cohesive route family and add characterization tests first. |
| Organization, instance-bootstrap-state, and EventSessionSpeaker repository cancellation now propagates from handlers into EF Core. | R-010 first slice added optional cancellation tokens to `IOrganizationRepository` and `OrganizationRepository`, forwarded them into organization detail/list/membership-list/PII-erasure EF async calls, and passed handler tokens from organization/public-experience query handlers. The second R-010 slice changed `IInstanceBootstrapStateRepository.GetCurrent` and `InstanceBootstrapStateRepository.GetCurrent` to accept a `CancellationToken`, forwarded it into `FirstOrDefaultAsync`, and updated five onboarding handlers to pass their MediatR tokens. The third R-010 slice added tokens to `IEventSessionSpeakerRepository` and `EventSessionSpeakerRepository` read and duplicate-check methods, forwarded them into `ToListAsync`, `CountAsync`, and `FirstOrDefaultAsync`, and updated EventSessionSpeaker query/update handlers to pass MediatR tokens. | High | R-010 is partially mitigated. The organization, instance-bootstrap-state, and EventSessionSpeaker repository slices are complete; other repositories still have no-token EF async calls and need separate source-selected slices with focused tests. |
| Generic repository existence checks no longer materialize tracked entities. | R-024 first slice changed `GenericRepository.Exists` from tracked `GetById`/`FindAsync` materialization to an EF-metadata primary-key predicate executed with `AsNoTracking().AnyAsync(...)`; `GenericRepositoryTests` proves the check returns true without tracking the entity. | High | R-024 is partially mitigated. `GetById` remains tracked for mutation paths until a later read/mutation contract split is source-selected and tested. |
| Tenant-policy effective settings reads are batched. | R-026 changed `TenantPolicySettingService.ReadEffectiveTenantSettingsAsync` to load all system settings once and all tenant overrides once, then resolve effective settings from dictionaries. Focused Application tests prove no per-key system or tenant read calls remain, and focused Persistence tests prove `GetAllForTenant` is no-tracking and exact-tenant bounded. | High | R-026 is mitigated for the audited tenant-policy read path. Keep mutation/update paths on tracked per-key reads where they modify tenant overrides. |
| Worktree is currently dirty beyond this workstream. | `git status --short` shows many unrelated modified, deleted, and untracked files. | High | Future implementation agents must not revert unrelated changes and must re-baseline before code edits. |

### 2.2 Existing Implementation

Current backend/API posture:

- `Explore.ServiceDefaults` owns shared operational endpoints. `/health` is readiness, `/alive` is liveness, `/metrics` is Prometheus scraping.
- `Explore.ServiceDefaults.HealthChecks.HealthCheckResponseWriter` serializes shared health JSON and redacts raw exception text plus sensitive health-check data at the endpoint boundary.
- `Explore.API` adds API-specific readiness checks such as storage, reconciliation, SMTP, email dispatch, RabbitMQ dispatch, AI provider/retention, MCP adapter, and Cerbos where configured.
- API errors are expected to flow through chained `IExceptionHandler` and RFC 7807 ProblemDetails with `code`, `traceId`, `timestamp`, and optional `correlationId`.
- HATEOAS/HAL is the source of truth for mutation affordances. UI must check `_links`, not local roles/claims, for per-resource actions.
- MediatR `AuthorizationBehavior` is the server-side resource-authorization boundary for `[AuthorizeResource]`, `IAuthorizedRequest`, and `ISecureRequest`.
- EF tenant filters are the production tenant-isolation enforcement layer. Explicit bypasses require reason, bounded predicates, and tests.
- The workstream already contains a useful risk register and inventories, but the main plan/tasks did not distinguish completed work from current next work cleanly.

### 2.3 Existing Tests And Verification Coverage

Known coverage and required verification lanes:

- `Event.Architecture.Tests`: Clean Architecture, endpoint classification, route names, auth metadata, response metadata, agent-context/doc governance.
- `Event.API.IntegrationTests`: HTTP behavior, auth gates, HAL, ProblemDetails, OpenAPI contract, rate limit/idempotency/timeouts where applicable.
- `Event.Application.UnitTests`: CQRS handler behavior, authorization metadata, idempotency/concurrency/audit use cases.
- `Event.Persistence.IntegrationTests`: EF tenant filters, query filters, repositories, migrations, index/model assertions.
- `Explore.Blazor.Client.Tests`: bUnit affordance gating by HAL links.

Known verification caveats from the current context:

- Prior API integration Docker/Testcontainers and no-Keycloak host-shutdown blockers were not reproduced by the 2026-07-03 scoped checks.
- The prior `Explore.Blazor.Client` Razor syntax blocker is stale; the Release build passes.
- Architecture tests are green after resolving unrelated agent-context link/manifest issues; they still only prove structural/context invariants, not backend/API behavior risks.
- Because the worktree remains dirty, future implementation claims still need touched-scope verification and should not infer whole-suite health from the scoped blocker pass.

### 2.4 Existing Documentation And Contracts

Relevant docs/contracts:

- `docs/API.md`: middleware order, auth, HAL, ProblemDetails, OpenAPI/client generation, `/health`/`/alive`/`/metrics`.
- `docs/OPERATIONS.md`: health/readiness semantics, readiness checks, metrics, runbooks, retention/partitioning posture.
- `docs/SECURITY-MODEL.md` and `docs/AUTHORIZATION.md`: BFF trust boundary, Keycloak/JWT, resource authorization, Cerbos/local provider behavior, fail-closed rules.
- `docs/MULTI_TENANCY.md`: tenant resolution order and fail-closed query filters.
- `docs/TESTING.md`: TUnit projects, per-project commands, integration host profiles, skip governance.
- `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, and `Explore.Blazor.Client/Clients/EventApiClient.g.cs`: generated artifacts; do not hand-edit.
- Active workstream support artifacts listed in section 2.1.

### 2.5 Current Pain Points / Improvement Areas

1. **Plan artifact drift.** The old plan dated 2026-06-13/14 mixes implementation notes, historical progress, and future tasks. This makes it hard for a future agent to know the real next slice.
2. **Risk ownership still incomplete.** The generated inventory source mismatch is fixed and `docs/API_CONTRACT_INVENTORY.md` now matches the 506-operation build-time schema. Phase 0.3 still must connect high/critical operation rows to risk IDs and acceptance tests before the broad security/HAL slices are treated as ready.
3. **Health endpoint docs drift.** Product docs were corrected on 2026-07-03. Workstream docs still mention `/health/ready` only as a rejected/deferred endpoint alias; `Explore.AppHost` still contains intentional external infrastructure health checks such as MinIO/Cerbos.
4. **Health payload redaction risk.** The shared response writer now redacts raw exceptions and suspicious data. Individual checks must still prefer bounded provider/status/failure-code data so operator output stays useful.
5. **Overbroad architecture-test tasks.** Behavior such as idempotency, concurrency, audit emission, and field-shape privacy should be proven with targeted unit/integration tests first. Architecture tests should guard structural invariants only.
6. **Speculative tenant execution enum.** Current tenant filters are already fail-closed. A new execution-mode abstraction should be introduced only where it simplifies audited bypass/system-scope handling, not as a prerequisite rewrite.
7. **Repository cleanup overreach.** Read-only `AsNoTracking` is correct, but tracked aggregate loads are valid for mutation paths. Do not globally convert repository reads without separating read and mutation contracts.
8. **Controller decomposition risk.** Splitting controllers before confirming route/HAL/OpenAPI stability can break links and generated clients. Splits must be resource/use-case driven and backed by behavior tests.
9. **Persistence/domain cleanup too broad for a final phase.** Removing all Domain `DataAnnotations`/mapping attributes is probably a dedicated workstream unless scoped to one aggregate with migration/model tests.
10. **Next security slice must stay bounded.** The first health/inventory cleanup slices are complete; R-004/R-005/R-006/R-012/R-015/R-016/R-017/R-018/R-019/R-020/R-032/R-033/R-034/R-035/R-036/R-037/R-038/R-039/R-040/R-041 are mitigated for the verified surfaces; R-013 has a bounded fallback tenant-context/batch-parity mitigation but remains open for other resource families; and the stale manual EventRegistration by-user plus custom-property projection admin Critical rows have been reconciled. The next implementation should not reopen broad controller cleanup: choose the next source-grounded high/critical row from current source and generated inventory.

### 2.6 Unknowns After Investigation

- Which current dirty worktree changes are intentional user work versus incomplete implementation slices. Future agents must inspect before editing source.
- Whether the remaining P1/P2 auth/HAL endpoint families have enough behavior coverage for auth gates, DTO field-shape privacy, resource authorization, HAL affordance gating, and generated-contract evidence.
- Whether the full API and persistence suites contain additional failures outside the scoped no-Keycloak and PostgreSQL Testcontainers smoke lanes.
- Whether every readiness check's data/exception output is bounded and production-safe.
- Whether each remaining P0/P1 auth/HAL risk still exists after current untracked/modified work.
- Whether any distinct post-activation missing auth-provider scenario exists outside the now-covered Keycloak bootstrap provider-failure path; if it does, it needs source evidence before becoming a task. Do not recreate R-004 work without new source evidence.

## 3. Proposed Future State

The target is not a giant "make backend enterprise-grade" PR. The target is a sequence of slices that leave the platform easier to operate, safer to self-host, and easier to maintain:

1. Re-baseline the workstream against the current branch and current generated API contract.
2. Close security/HAL/auth gaps before structural refactors.
3. Start the next security pass from the next source-grounded high/critical row in the current risk register and generated inventory; R-014 API-key tenant execution and R-041 external API-key management are already mitigated for the verified defects.
4. Lock API contract/error/route/OpenAPI behavior with tests and generated artifacts.
5. Decompose controllers only behind stable routes and HAL policies.
6. Refactor Application/CQRS hotspots with behavior-preserving tests and explicit transaction/idempotency/concurrency boundaries.
7. Improve Persistence/query/reliability hotspots only where evidence shows risk.
8. Update operator docs, health/metrics redaction, changelogs, and validation evidence.

## 4. Non-Negotiable Constraints

- Repositories return entities, never DTOs. Mapping stays in Application handlers/read-model mappers.
- Validators are manually instantiated; do not inject `IValidator<T>`.
- Domain stays pure. No EF Core, ASP.NET Core, MediatR, AutoMapper, or infrastructure dependencies in Domain.
- GET endpoints may be anonymous only when data is intentionally public. Anonymous reads must not expose user IDs, emails, full names, roles, memberships, grants, invitations, revocation metadata, private tenant data, or sensitive storage/admin metadata.
- Writes require `[Authorize]` plus handler/resource authorization when ownership or admin authority matters.
- HAL `_links` are the source of truth for Blazor action affordances.
- Tenant filters fail closed; bypasses require bounded predicates, reason, operation name, logging/audit where sensitive, and tests.
- Controllers remain transport/composition boundaries: MediatR dispatch, HAL assembly, response mapping.
- Query handlers return DTO/list/page/null results, not command envelopes.
- Commands return `BaseCommandResponse<TId>` or established delete/result patterns.
- No external HTTP, SMTP, broker publish, provider deletion, or scheduler side effect inside DB transactions. Use transactional outbox or approved background worker patterns.
- Generated OpenAPI, inventory, and NSwag client files are regenerated through documented commands, not hand-edited.
- Health, logs, metrics, traces, and ProblemDetails must not expose secrets or high-cardinality/user-controlled data.
- Credential/session/API-key management responses must not use shared output cache. Use `ResponseCache(NoStore=true, Location=None)` for sensitive reads and explicit named rate-limit metadata for read/write credential-management operations.
- No backward-compatibility shims for obsolete pre-v1 behavior unless explicitly approved.

## 5. Architecture And Design Decisions

### Decision 1 - Preserve the current operational endpoint contract

- **Decision:** Keep `/health` for readiness, `/alive` for liveness, and `/metrics` for Prometheus unless a separate migration plan intentionally changes paths.
- **Why:** Current code, API docs, operations docs, and self-hosting docs already use these paths. Microsoft docs require separation of readiness/liveness semantics, not specific path names.
- **Alternatives considered:** Add `/health/ready` and `/health/live` aliases. Rejected for this workstream because it adds duplicated operational contract and docs/client confusion.
- **Consequences:** Fix docs that incorrectly mention `/health/ready`; do not create compatibility aliases by default.

### Decision 2 - Treat health payload safety as an implementation concern

- **Decision:** Audit and, if needed, redact health check `error` and `data` fields rather than assuming all health checks are safe.
- **Why:** The shared writer emits exception messages. Some dependency checks may contain endpoint/path/provider details unless carefully bounded.
- **Consequences:** Add API/ServiceDefaults tests for production-safe health output and update docs/runbooks.

### Decision 3 - Re-baseline before code edits

- **Decision:** The first implementation slice after this plan is a source/inventory/verification re-baseline.
- **Why:** The current worktree has extensive unrelated changes. Implementing from stale 2026-06-13 task rows would be unsafe.
- **Consequences:** Future code agents must confirm current source before touching API/Application/Persistence files.

### Decision 4 - Security and HAL correctness come before decomposition

- **Decision:** Finish P0 data exposure, resource authorization, and HAL affordance correctness before controller splits or handler cleanup.
- **Why:** Structural cleanup can move bugs around; it does not fix authorization or data exposure.
- **Consequences:** Controller decomposition remains blocked until high-risk auth/HAL issues have tests or explicit deferrals.

### Decision 5 - Behavioral risks get behavioral tests

- **Decision:** Use architecture tests for structural rules and unit/integration tests for behavior.
- **Why:** Idempotency replay, concurrency conflicts, audit emission, tenant isolation, and field-shape privacy are runtime behavior. Reflection tests alone cannot prove them.
- **Consequences:** Replace broad "add architecture test for X behavior" tasks with exact test-project expectations.

### Decision 6 - No speculative tenant abstraction

- **Decision:** Do not implement a new `TenantExecutionMode` enum just because the old plan proposed it. Introduce it only if it is the simplest way to formalize audited host-admin/background/system scopes.
- **Why:** Current fail-closed filters already exist. The real remaining risk is bypass semantics and auditability.
- **Consequences:** Phase 1C focuses on semantic bypass proof, reason-coded APIs, and health/operations visibility.

### Decision 7 - Persistence changes stay evidence-led

- **Decision:** Do not perform repo-wide no-tracking, cursor, index, hard-delete, or cancellation-token churn without selecting concrete endpoints/repositories.
- **Why:** Some tracked reads are valid; broad changes create risk without guaranteed improvement.
- **Consequences:** Persistence phases require a selected hotspot, test, and rollback/failure path.

### Decision 8 - Delete ungoverned runtime utility endpoints unless a real operator use case exists

- **Decision:** Retire the Development/Testing-only `POST /admin/migrate` route instead of upgrading it into a classified HTTP operation.
- **Why:** Runtime database migration over HTTP creates an operational and security surface that duplicated already-supported startup migrations and `Event.MigrationService` without a named self-hoster need, audit trail, host-admin capability model, redacted failure contract, or OpenAPI/client governance.
- **Alternatives considered:** Keep it as an authenticated local-dev helper and add route metadata/redaction. Rejected because no durable use case justified the extra privileged HTTP surface.
- **Consequences:** Database migration execution stays in startup/migration-service paths. Do not reintroduce HTTP migration execution without explicit host-admin authorization, audit, environment constraints, bounded/redacted ProblemDetails, operator docs, and regression tests.

### Decision 9 - Separate self-registration reads from attendee management

- **Decision:** Treat `EventRegistration` list/detail/by-session/by-user endpoints as current-user self-service reads. Do not expose attendee identity through those contracts.
- **Why:** OWASP API security guidance treats broken object-level/property-level authorization and excessive data exposure as first-order API risks. A user's registration status and an organizer's attendee roster are different authorization cases and need different projections.
- **Alternatives considered:** Keep the old general-purpose registration DTO and rely on UI hiding or local role checks. Rejected because API responses are the security boundary; clients must receive only fields they are authorized to see.
- **Consequences:** Self-service registration DTOs stay identity-minimized. If organizer/admin attendee management needs names or emails, add a dedicated resource-authorized management query, HAL policy, DTO, OpenAPI contract, and focused tests.

### Decision 10 - Keep footer writes under tenant update authority for now

- **Decision:** Footer link-group, link, reorder, and footer-settings writes use `ResourceKinds.Tenant` with `AuthorizationActions.Update` and tenant-scoped `ISecureRequest` metadata instead of introducing a partial `Footer.Manage` permission family.
- **Why:** The current authorization catalog has no footer resource kind or generic manage action. Footer management is tenant configuration, so the existing tenant update policy gives immediate Cerbos/local fallback parity without creating an under-specified policy family.
- **Alternatives considered:** Add `Footer.Manage` immediately. Rejected for this slice because a new capability family would require resource constants, action constants, Cerbos policy/schema, local fallback, machine-scope mapping, HAL metadata, and tests together.
- **Consequences:** Current footer writes are no longer authentication-only; missing actor identity fails closed, regular authenticated users are denied, and tenant admins are authorized only for the resolved tenant. A future footer-specific role can still be added as a complete parity slice.

### Decision 11 - Treat external API-key management as a credential-management surface

- **Decision:** R-041 hardens the existing `ExternalApiKeyController` and CQRS handlers without inventing a partial `ApiKeys.*` Cerbos family. The immediate fix is endpoint contract and handler semantics: remove shared output caching from user/tenant-specific key reads and reports, add explicit `Authenticated`/`Write` rate-limit metadata, return 403 for owner-authority/report authorization denials, return 404 when revoke finds no visible key, and keep platform-scoped uniqueness checks on the existing named tenant-filter bypass path.
- **Why:** These endpoints manage long-lived machine credentials. Context7 ASP.NET Core docs support `[Authorize]`, `ResponseCache(NoStore=true, Location=None)`, `[EnableRateLimiting]`, `[OutputCache]` only for cache-safe responses, and typed `ProblemDetails` metadata at the controller boundary. OWASP API guidance treats object-level authorization, function-level authorization, sensitive-property exposure, unrestricted resource consumption, and cache isolation as first-order API risks.
- **Alternatives considered:** Add a full `ResourceKinds.ExternalApiKey` / `AuthorizationActions.ApiKeys.*` family immediately. Rejected for this slice because current owner-authority logic already distinguishes user, organization, group, tenant, and instance authority; a new resource family would require resource constants, action constants, Cerbos policy/schema, local fallback, machine scopes, HAL metadata, and tests atomically. Keep that as a future complete parity slice if API-key administration grows beyond the current owner-authority model.
- **Consequences:** The implementation changed API/Application/Persistence/test/docs together and regenerated contract artifacts after response metadata/cache/rate-limit changes. `docs/API_CHANGELOG.md` records the behavioral hardening: unauthorized create/report becomes 403, non-owned/missing delete becomes 404, and key-management reads become no-store/no-output-cache.

### Decision 12 - Require an execution tenant for tenant-scoped InstanceAdmin API-key calls

- **Decision:** R-014 keeps `InstanceAdmin` API keys as nullable platform credentials but does not allow them to execute tenant-scoped API/MCP requests without an execution tenant. Tenant hints bind the request tenant, single-tenant mode binds the configured default tenant, unresolved tenant-scoped API/MCP requests return `404 tenant_required`, and only explicit host-administration API routes continue without tenant context.
- **Why:** Tenant isolation should be a request-execution property, not just a credential-storage property. This preserves legitimate host administration while preventing ordinary tenant-scoped APIs from running with no ambient tenant and accidentally broadening downstream query/filter assumptions.
- **Alternatives considered:** Treat all `InstanceAdmin` API-key calls as host administration. Rejected because it makes every tenant-scoped API path depend on downstream code remembering to re-establish tenant scope. Add a new endpoint metadata enum immediately. Rejected for this slice because existing host-administration paths are narrow and source-identifiable; a metadata taxonomy can be added later as a complete API convention with tests.
- **Consequences:** Multi-tenant `InstanceAdmin` callers must send a trusted tenant hint for tenant-scoped API/MCP operations. Existing tenant-bound keys and invalid-key behavior remain unchanged. No OpenAPI/client regeneration was required because the route and DTO contracts did not change.

### Decision 13 - Make R-029 a management contract before a Blazor stub fix

- **Decision:** Treat event-session-speaker assignment as a management relationship contract. The Application authorization/resource context, tenant/session/actor ownership prerequisite, API/HAL contract, and generated client methods are complete. Blazor calls still wait for typed service/dialog implementation. The route catalog file is `Explore.API/Hateoas/RouteNames.cs`, not a separate `Routes` folder.
- **Why:** The current Blazor service has no generated client methods to call, and direct HTTP wiring would duplicate API contract knowledge in the browser-facing client. The completed Application slice fixes the previously verified write-path gaps: delete authorization now points at the owning session id, create proves actor/session same-tenant ownership and duplicate-link rejection, and mutation paths invalidate event caches. A HAL policy still does not exist to make UI affordances API-authoritative.
- **Alternatives considered:** Wire `EventSessionSpeakerService` manually to guessed URLs, or make the speaker relationship read anonymous because GET endpoints are usually public. Rejected because management DTOs contain assignment ids/concurrency/tenant context and mutation affordances. A public speaker roster, if needed, should be a separate minimized projection or reuse existing event program-summary surfaces.
- **Consequences:** The remaining R-029 implementation must use the generated `EventApiClient` methods and HAL links from the completed management contract. The Blazor dialog must gate add/remove from HAL links and check failure results instead of assuming success.

## 6. Implementation Phases

### Phase 0 - Re-baseline and Approval Gate

- **Goal:** Make the current state trustworthy before more code changes.
- **Depends on:** This re-baselined plan.
- **Relevant files:** this workstream's plan/context/tasks; `endpoint-inventory.md`; `backend-contract-risk-register.md`; generated OpenAPI/inventory files only through generation workflow.
- **Acceptance criteria:**
  - The user approves or corrects this re-baselined plan.
  - Current `git status` and unrelated dirty files are recorded in context before implementation.
  - Current blockers are rechecked: architecture context failures, API integration/Docker issues, Blazor build issue. As of 2026-07-03, Blazor/Docker/no-Keycloak/Testcontainers smoke checks pass; architecture tests also pass after unrelated agent-context metadata/docs drift was fixed.
  - Endpoint inventory and risk register are reconciled with the current branch or explicitly marked stale. As of 2026-07-04, the generated row-level table is current in `docs/API_CONTRACT_INVENTORY.md`; R-032 is mitigated by the schema-source generator fix; R-015 is mitigated by deleting the retired HTTP migration endpoint; R-033 is mitigated by webhook audit/retry API, Application, and Persistence tests; R-034 is mitigated by incoming webhook public-ingestion hardening/tests; R-035 is mitigated by event-report/moderation privacy hardening plus regenerated contract artifacts; R-036 is mitigated by tenant-scoped EmailDispatch admin resource authorization and Cerbos/fallback/HAL parity; R-037 is mitigated by Application-enforced self-service user-organization reads with typed 401/403 contract metadata; R-038 is mitigated by tenant-update resource authorization for module enable/disable writes with write-rate metadata and Local RBAC denial coverage; R-040 is mitigated by self-scoping user-authentication-token session reads/writes, removing credential/identity fields from read DTOs, removing client-supplied ownership from create/update DTOs, and regenerating contract artifacts; R-041 is mitigated by no-store/no-output-cache key-management reads, named `Authenticated`/`Write` rate metadata, 403 owner/report denials, 404 revoke misses, platform key uniqueness through the tenant-filter bypass path, and regenerated contract artifacts; R-014 is mitigated by explicit tenant binding and fail-closed `tenant_required` behavior for `InstanceAdmin` API-key execution; R-016 is mitigated by self-scoping event-registration reads plus removing serialized registrant identity from the client contract; R-017 is mitigated by tenant-admin/resource-protecting identity-bearing tenant role grant reads with Cerbos/fallback parity; R-018 is mitigated by tenant/org resource-protecting identity-bearing organization member reads with Cerbos/fallback/HAL parity; R-019 is mitigated by tenant-update resource authorization for footer writes with API/Application/Infrastructure coverage; R-012 is mitigated by authenticated/resource-protected storage object metadata, content, and presigned reads with Cerbos/fallback/HAL parity; R-013 has a bounded local fallback parity mitigation for explicit tenant mismatch, projection/support-access context, and storage optimized-batch decisions; and the custom-property projection admin row is mitigated by `CustomPropertyProjection:view/update` request metadata, server-side tenant enrichment, Cerbos policy tests, and fail-closed local fallback. Remaining Phase 0.3 work is risk ownership for other high/critical rows.
- **Verification:** docs consistency grep; generated inventory test when `docs/API_CONTRACT_INVENTORY.md` changes.

### Phase 1 - Security, Authorization, Tenant, and HAL Corrections

- **Goal:** Close the highest-risk data exposure and authorization/HAL drift before structural refactoring.
- **Relevant files:** P0 controllers/handlers/HAL policies/components identified by refreshed inventory.
- **Acceptance criteria:**
  - Event registration self-service reads are self-scoped and identity-minimized; tenant role grant reads are tenant-admin/resource protected; organization member reads are tenant/org resource protected; footer writes are tenant-update resource protected; module enable/disable writes are tenant-update resource protected; storage object metadata/content/presigned reads are authenticated/resource protected; email dispatch admin status/control operations are tenant-scoped/resource protected; user-authentication-token routes are self-service/no-store; external API-key management is hardened under R-041; API-key tenant execution is hardened under R-014; remaining AI assistant and setup/bootstrap access rules are classified by resource/action.
  - Anonymous identity-bearing responses are blocked or replaced with safe public projections.
  - Blazor actions are gated by HAL links; broad route/menu checks remain the only allowed role/claim UI exception.
  - Tenant bypasses have bounded predicates, reasons, and semantic tests.
  - `TenantLookupSource` cache warmup is semantically covered for active tenant domain settings, `TenantCapabilityRepository` is semantically covered for explicit tenant-module capability resolution, `ExternalApiKeyRepository` is semantically covered for credential authentication plus platform-scoped InstanceAdmin key management, `ExternalApiKeyQuotaRepository` is semantically covered for platform usage reporting and quota-row tenant filtering, `EventRepository` is semantically covered for exact event authorization-target resolution, `StorageObjectRepository.ListDeleteRequestedForResourceAsync` is semantically covered for bounded delete-requested storage reconciliation, `UserExternalLoginRepository` is semantically covered for exact external identity resolution, `TenantUserRoleGrantRepository` is semantically covered for tenant-authority and user-membership resolution, `TenantUserRepository` is semantically covered for exact tenant membership, actor, and active-state checks, `TenantSettingRepository` is semantically covered for exact tenant setting override management, `TenantSettingsDocumentRepository` is semantically covered for exact typed settings document reads, `NotificationRepository` is semantically covered for exact tenant/user/key deduplication checks, `EmailDispatchOutboxRepository` is semantically covered for worker queue, tenant-operator, and receipt-idempotency bypasses, and the webhook repository family is semantically covered for tenant-operation and worker-queue bypasses; remaining tenant-filter bypass call-site families still require the same source-specific proof before Phase 1.4 can close.
  - Current continuation adds verified `NotificationFanoutRunRepository` source-idempotency/worker-polling proof and registration capacity/session ownership hardening.
  - Self-host bootstrap/admin behavior has structured audit coverage, provider-failure ProblemDetails coverage, backup-confirmed additive Keycloak sync-apply tests, and Keycloak client-secret rotation tests proving provider acceptance is required before persistence/reload.
- **Verification:** `Event.API.IntegrationTests`, `Event.Application.UnitTests`, `Explore.Blazor.Client.Tests`, `Event.Persistence.IntegrationTests`, and relevant `Event.Architecture.Tests` slices by touched files.

### Phase 2 - API Contract, ProblemDetails, OpenAPI, and Operational Health

- **Goal:** Make API errors, route names, OpenAPI metadata, generated clients, and health/metrics contracts stable.
- **Relevant files:** `Explore.API/ExceptionHandling/**`, controllers, OpenAPI transformers, `Explore.ServiceDefaults/Extensions.cs`, docs.
- **Acceptance criteria:**
  - Remaining ad hoc error paths are migrated or explicitly deferred. As of 2026-07-04, the direct controller-helper sweep is clean for the audited raw response helpers and raw 4xx command-envelope metadata.
  - `ProblemDetails` behavior tests cover representative validation/auth/not-found/conflict/rate-limit/server-error paths. As of 2026-07-04, `ProblemDetailsContractTests` locks 400, 401, 403, 404, 409, 429, and 500 shapes.
  - Route names and HAL references remain bidirectionally covered. As of 2026-07-04, `RouteNameCoverageTests` passes 3/3 against the current dirty branch after the focused API integration test project build.
  - OpenAPI operation IDs, endpoint class, rate-limit, cache, and tenant posture metadata are current.
  - Health output is bounded and does not leak raw secrets, paths, endpoints, object keys, credentials, provider response bodies, or raw exception text.
  - Docs consistently reference `/health`, `/alive`, and `/metrics`.
- **Verification:** API contract tests, architecture tests, focused health endpoint tests, OpenAPI/client generation workflow when source contract changes.

### Phase 3 - Controller Decomposition Behind Stable Contracts

- **Goal:** Reduce API transport complexity without changing external semantics accidentally.
- **Relevant files:** large controllers selected from refreshed inventory, API request mapper classes, HAL assemblers/policies.
- **Acceptance criteria:**
  - Each split is tied to a named risk row or verified controller responsibility problem, not method-count driven.
  - Existing route names remain stable unless a breaking change is recorded.
  - Controllers do not inject repositories or perform business orchestration.
  - API request-to-query mapping moves into small API-local mappers only when it removes repeated transport mapping in the selected controller family.
  - Behavior tests prove old high-risk flows through new controller boundaries.
- **Current evidence:** The first 2026-07-04 slice moved event Islamic/tech aspect subresources into `EventAspectController` with route, route-name, auth/classification, response metadata, output-cache, and OpenAPI tag grouping preserved. This reduces `EventController` scope but does not close the broader fat-controller risk.
- **Verification:** API integration tests for changed controller families; route/HAL/OpenAPI tests.

### Phase 4 - Application/CQRS Use-Case Refactor

- **Goal:** Correct one verified Application/CQRS risk at a time without bypassing MediatR behaviors or transaction boundaries.
- **Relevant files:** selected commands/queries/handlers, Application services, unit tests.
- **Acceptance criteria:**
  - A risk card names the selected handler/query, the current defect, and the acceptance criteria before implementation starts.
  - Oversized handlers such as event creation or AI run processing are split into narrow collaborators only when the selected risk requires it and characterization tests exist.
  - Query contract normalization is scoped to a named query family and public API contract change, not repo-wide cleanup.
  - Commands keep `BaseCommandResponse<TId>` or established delete/result patterns.
  - Idempotency, optimistic concurrency, audit, cache invalidation, and transaction boundaries are explicit for each changed use case.
  - Collaborators do not independently call `SaveChangesAsync` inside a broader unit-of-work unless the design explicitly permits it.
- **Verification:** `Event.Application.UnitTests`; CQRS architecture tests; API tests when response contracts change.

### Phase 5 - Persistence, Query Shape, Pagination, Indexes, and Reliability

- **Goal:** Improve data access only where source evidence shows tenant, performance, cancellation, lifecycle, or reliability risk.
- **Relevant files:** selected repositories, EF configurations, query specs, migrations, persistence tests.
- **Acceptance criteria:**
  - Repository contracts remain entity-first except explicitly named read-model/query-store ports.
  - Read-only paths use `AsNoTracking`; mutation paths keep tracked aggregate loads where needed.
  - DTO-shaped repository inputs, hard delete behavior, cursor/keyset pagination, indexes, migrations, and outbox/TickerQ/RabbitMQ reliability work are implemented only after a risk card proves the current path needs that specific change.
  - Cursor/keyset pagination is added only for selected high-volume endpoints with stable ordering, cursor binding, schema/docs updates, and index evidence.
  - Index and migration work includes model assertions plus rollback/reset notes for self-hosters.
  - Background state transitions remain idempotent, retry-safe, and observable before broker/provider side effects run.
- **Verification:** `Event.Persistence.IntegrationTests`, model/index assertions, Application/API tests where contract changes.

### Phase 6 - Final Guardrails, Docs, and Release Evidence

- **Goal:** Turn temporary planning allowances into durable safeguards or explicit deferrals.
- **Relevant files:** docs, architecture tests, API changelog, generated artifacts, risk register.
- **Acceptance criteria:**
  - Every open risk row is mitigated or explicitly deferred with owner/date/rationale.
  - No temporary skipped/failing test remains without `Category:` and `Removal:` metadata.
  - Docs and generated artifacts match implemented behavior.
  - Full targeted verification evidence is recorded in context.
- **Verification:** build plus per-project test commands required by touched intents; no solution-level `dotnet test`.

## 7. Testing Strategy

Use the smallest test lane that proves the risk:

- **Architecture tests:** structural invariants only: layer dependencies, route-name coverage, endpoint classification metadata, no controller repository injection, no API domain entity responses, HAL source-of-truth guardrails, skip governance, ABOUTME headers.
- **API integration tests:** HTTP status, auth/authorization, ProblemDetails shape, HAL link sets, OpenAPI metadata, health endpoint response safety, rate-limit/idempotency/timeouts.
- **Application unit tests:** handler orchestration, manual validators, authorization metadata, audit/idempotency/concurrency/cache behavior.
- **Persistence integration tests:** tenant filters, bypass predicates, repository contracts, migrations/indexes/query shape.
- **Blazor client tests:** mutation affordances render only when `_links` contains the required rel.

Canonical commands are project-level. Do not use solution-level `dotnet test`.

## 8. Documentation, Configuration, And Operations Impact

Expected docs updates by slice:

- `docs/API.md`: route/error/HAL/OpenAPI/health contract changes.
- `docs/API_CHANGELOG.md`: every breaking pre-v1 API/client behavior change.
- `docs/OPERATIONS.md`: health/readiness/metrics, background workers, runbooks, retention, failure modes.
- `docs/SECURITY-MODEL.md` and `docs/AUTHORIZATION.md`: auth/resource policy/provider behavior.
- `docs/MULTI_TENANCY.md`: tenant resolution, fail-closed filters, bypass semantics.
- `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`, `docs/BACKUP_RESTORE_UPGRADE.md`: operator-visible config, upgrade, health, recovery paths where touched.
- `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`: regenerated only through the documented workflow.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Treat identity-bearing data exposure as a security issue, not a DTO polish issue.
- Instance admin, tenant admin, organization admin, group admin, standard user, and machine/API-key principals must remain distinct.
- Cerbos selected at instance level fails closed; local fallback is only when local mode or explicit BYO-open behavior is configured.
- HAL links encode per-resource affordances; UI must not duplicate resource authorization.
- Setup secret is bootstrap-only, not an identity.
- API-key scopes are a ceiling and do not bypass resource authorization.
- Rate limiting and idempotency must be preserved for high-risk writes and anonymous ingestion.
- Health/logs/metrics/traces must expose bounded operational labels only.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

- **Multi-tenancy:** Applicable. Every tenant-scoped read/write and background/system path needs explicit tenant behavior.
- **Federation:** Applicable only when touching ATProto/PDS/MCP/outbox paths. Do not expand protocol scope inside this refactor.
- **Localization:** Applicable only when touching localization admin/provider endpoints or error copy.
- **Accessibility:** Applicable for Blazor affordance changes; action visibility must not create inaccessible hidden state.
- **Product:** Applicable. The platform is pre-v1, self-hostable, white-label, and breaking changes are acceptable only when they simplify the durable contract.

## 11. Observability And Operations

- Use OpenTelemetry, Prometheus, Loki/structured logs. Do not introduce Sentry.
- Keep metric dimensions bounded. Do not tag raw URLs, IDs, object keys, prompts, provider errors, emails, subjects, or exception text.
- Preserve `/metrics`.
- Preserve readiness/liveness separation.
- Review health check payloads for safe data and safe exception handling.
- For background workers, expose durable state and low-cardinality metrics rather than only warning logs.

## 12. Migration And Compatibility Plan

- Breaking changes are allowed before v1.0 but must be documented in `docs/API_CHANGELOG.md` and reflected in generated OpenAPI/client artifacts.
- EF migrations must be small, focused, and non-destructive unless the user explicitly approves development reset behavior.
- Operator-visible behavior changes need self-hosting/upgrade/recovery notes.
- Do not preserve obsolete endpoints or tests only for compatibility.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| Stale inventory drives wrong implementation. | High | High | Phase 0 re-baseline from current source/OpenAPI. | Endpoint tests disagree with inventory. | Phase 0 |
| Health responses leak provider details. | Medium | High | Add safe-output tests and redact generic exception messages if needed. | `/health` body contains endpoint/path/key/raw exception strings. | Phase 2 |
| ProblemDetails behavior coverage regresses after mapper cleanup. | Low | Medium | Keep direct controller helpers migrated and preserve `ProblemDetailsContractTests` coverage for validation, auth, forbidden, not-found, conflict, rate-limit, and production-safe 500 families. | Raw-helper grep reintroduces direct helpers, or `ProblemDetailsContractTests` fails for status-family envelopes. | R-005 / Phase 2.2 |
| Authorization drift remains after controller cleanup. | Medium | High | Close resource policies/HAL affordance tests before decomposition. | Missing/extra HAL links; 401/403 matrix failures. | Phase 1 |
| Module capability writes bypass tenant resource authorization. | Low | High | R-038 routes enable/disable through tenant-scoped `ISecureRequest` commands, resolves the enable audit actor in Application, adds `Write` limiter metadata, and proves tenant-admin allow plus regular-user deny. | A module enable/disable route calls `IModuleService` directly or loses tenant update metadata/rate-limit metadata. | R-038 / Phase 1.2 |
| External API-key management regresses to stale/wrong sensitive management data or misleading authz outcomes. | Low | High | R-041 removed output cache from management reads/reports, added no-store response metadata and named rate limiting, mapped unauthorized owner/report access to 403, mapped non-owned/missing revoke to 404, fixed platform-key uniqueness checks, and added focused API/Application metadata/behavior tests. | Generated inventory shows `ListData`/`DetailData` cache policy, missing rate-limit policy, or focused ExternalApiKey tests fail for forbidden/not-found/no-store behavior. | R-041 / Phase 1.7 |
| Tenant bypass semantics are assumed safe. | Medium | High | Add semantic bypass tests per call site. `TenantLookupSource` cache warmup, `TenantCapabilityRepository` module-capability resolution, `ExternalApiKeyRepository` credential/platform-key lookup, `ExternalApiKeyQuotaRepository` platform usage reporting plus quota-row tenant filtering, `EventRepository` authorization-target resolution, bounded storage delete-requested resource reconciliation, `UserExternalLoginRepository` external identity resolution, `TenantUserRoleGrantRepository` tenant-authority/membership resolution, `TenantUserRepository` tenant membership/actor/active-state checks, `TenantSettingRepository` exact tenant setting override management, `TenantSettingsDocumentRepository` typed settings document reads, `NotificationRepository` tenant/user/key deduplication checks, `EmailDispatchOutboxRepository` worker/tenant-operation/receipt-idempotency paths, and webhook tenant-operation/worker-queue paths are now covered, but other bypass families remain open. | Cross-tenant fixture reads wrong tenant rows. | Phase 1/5 |
| Notification fanout workers starve or leak due ambient tenant filters. | Medium | High | Use a dedicated `NotificationFanoutWorkerCrossTenantQueue` bypass reason for worker polling, keep source-idempotency lookups exact tenant/source bounded, and cover pending/completed/non-positive-batch behavior in persistence integration tests. | `NotificationFanoutRunRepositoryBypassTests` fails or worker pending batches are empty under a wrong/no ambient tenant. | Phase 1.4 |
| Registration capacity reservation increments the wrong session counter. | Low | High | Validate registration children against the parent tenant/event and ensure referenced sessions belong to that tenant/event before tenant/event/session-bound raw SQL reservation. | `EventRegistrationIntentRepositoryTests` cross-event session invariant fails or capacity counters move before child insert. | Phase 1.4 |
| Privileged runtime utility endpoints bypass API governance. | Low | High | Keep `POST /admin/migrate` retired; require explicit host-admin capability, audit, environment constraints, redacted errors, operator docs, and tests before any similar route is introduced. | `/admin/migrate` or another migration/setup utility route appears in source without inventory/risk-register ownership. | R-015 / Phase 1D |
| Broad repository cleanup breaks tracked mutation flows. | Medium | Medium | Split read-only and tracked contracts per use case. | Concurrency/update tests fail or entities not tracked. | Phase 5 |
| Generic hard-delete escape hatch returns. | Low | High | R-025 removed `HardDelete` from the generic repository contract/implementation and added an architecture guard. Future irreversible deletion must be an explicit lifecycle use case with authorization, audit, and focused tests. | `HardDelete(` appears in Application/Persistence/API/tests, or `CleanArchitectureTests.GenericRepository_ShouldNotExpose_IrreversibleDeleteMethod` fails. | R-025 / Phase 5.7 |
| OpenAPI/client drift after breaking changes. | High | Medium | Regenerate through documented workflow after stable source changes. | Contract parity/client naming tests fail. | Phase 2/6 |
| Integration tests remain environment-blocked. | Medium | Medium | Record blocker, isolate no-infra tests, run Docker lanes when available. | Docker/Testcontainers or host shutdown failures. | Phase 0/6 |
| Dirty worktree hides unrelated user changes. | High | Medium | Read and work only scoped files; do not revert unrelated changes. | `git status` changes outside planned files. | All phases |

## 14. Success Metrics And Definition Of Done

- Security P0 rows are closed or explicitly deferred with owner/rationale.
- No anonymous endpoint returns identity/membership/role/grant/private tenant data unless explicitly approved and tested.
- HAL action affordances are API-authoritative and UI consumes `_links`.
- API ProblemDetails contract is centralized, documented, and tested.
- Route names and HAL policy route references are covered in both directions.
- `/health`, `/alive`, `/metrics` behavior is documented and safe for operators.
- Query handlers do not use command envelopes for read data.
- Persistence changes remain entity-first, tenant-safe, and tested.
- All generated artifacts are regenerated, not hand-edited.
- Plan/context/tasks are current at handoff.

## 15. Implementation Agent Contract - Keep Dev Docs Current

Future implementation agents must:

1. Read this plan, `backend-api-health-refactor-context.md`, and `backend-api-health-refactor-tasks.md` before editing.
2. Re-run intent classification for the specific slice.
3. Re-read matching docs/rules/skills for files touched.
4. Update this plan when architecture/scope/phasing changes.
5. Update context after each meaningful slice with files changed, decisions, validation, blockers, and next step.
6. Check off tasks immediately when completed and add newly discovered tasks.
7. Do not report done unless dev docs match reality.
8. Include a developer teaching summary in final responses.

## 16. Progress Reporting Contract

Implementation slice summaries should use:

- **Implemented:** what changed, patterns used, files/classes involved, and data/control flow.
- **Verified:** exact commands or manual checks.
- **Remaining:** known gaps and blockers.
- **Next:** the next concrete slice.
- **Docs updated:** whether plan/context/tasks and product docs are current.

## 17. Potential Risks And Unknowns

The hardest part is not controller splitting or handler extraction. The hardest part is keeping authorization, tenant isolation, HAL affordances, OpenAPI generation, and self-hosting operations aligned while the branch is already very active. Future agents should resist broad cleanup. Pick one risk boundary, prove it with the right test layer, update the docs, then move to the next slice.

## 18. Research Notes

- 2026-07-04 CTO refresh: Context7 official ASP.NET Core docs (`/dotnet/aspnetcore.docs`) were rechecked for health-check endpoint patterns, authorization metadata, rate limiting, output caching/no-store posture, ProblemDetails metadata, and OpenAPI operation metadata. Context7 official EF Core docs (`/dotnet/entityframework.docs`) were rechecked for global query filters, EF Core 10 named filters, no-tracking reads, efficient querying, keyset pagination, and cancellation-token propagation.
- 2026-07-04 CTO refresh: Tavily MCP was called for OWASP API security and .NET health/EF planning research, but the configured Tavily account returned `status 432` quota exhaustion. Primary web fallback confirmed the same direction from OWASP API Security 2023 and Microsoft Learn: object-level/property-level authorization, rate limiting/resource consumption, separated readiness/liveness probes, and EF global/named filters are the right planning anchors.
- Context7 official ASP.NET Core docs (`/dotnet/aspnetcore.docs`) confirmed readiness/liveness health checks should be separate and can be filtered by tags/predicates through `HealthCheckOptions`.
- Context7 official ASP.NET Core docs also confirmed endpoint-level authorization, named rate-limiting policies, named output-cache policies, and OpenAPI operation metadata/transformers are supported platform patterns. Later plan slices should keep these as source/generated endpoint metadata, not manually curated spreadsheet state.
- Tavily extracted Microsoft Learn ASP.NET Core health-check docs: readiness means ready to receive traffic; liveness means process should be restarted only when unhealthy. The examples use `/health/ready` and `/health/live`, but path names are not mandatory.
- Tavily extracted Microsoft Learn ASP.NET Core metrics docs: OpenTelemetry metrics plus Prometheus scraping endpoint are the recommended OSS Prometheus/Grafana path.
- Tavily enterprise API-governance research reinforced the plan direction: contract drift should fail through generated OpenAPI/inventory tests, operation IDs should be stable, sensitive admin endpoints need explicit posture, and webhook management/audit surfaces need idempotency, retry, and audit behavior tests.
- Context7 official ASP.NET Core docs confirmed central ProblemDetails and structured `ILogger`/`EventId` logging are appropriate for bounded error contracts and security-relevant audit trails. Tavily search fallback, after the research endpoint quota failure, reinforced OWASP logging guidance: log authentication/bootstrap successes and failures with generic outcomes and identifiers, never submitted secrets, credentials, tokens, raw provider payloads, or unnecessary PII.
- Context7 official ASP.NET Core authorization and rate-limiting docs support resource-based server-side authorization plus named write-limiter metadata at the endpoint boundary. Tavily OWASP API5 research reinforces the R-038 decision: tenant module enable/disable is a broken-function-level-authorization risk unless the server enforces an explicit privileged tenant-resource decision and tests regular authenticated denial.
- Context7 official ASP.NET Core docs were used for R-041 planning around `[Authorize]`, `ResponseCache(NoStore=true, Location=None)`, `[OutputCache]`, `[EnableRateLimiting]`, and typed `ProblemDetails` endpoint metadata. Tavily MCP was attempted for API-key management research but returned quota exhaustion (`status 432`); OWASP primary web sources were used as fallback for API1 object authorization, API3 sensitive-property exposure, API4 resource consumption/rate limiting, API5 function-level authorization, REST access-control guidance, session/cache `no-store`, and multi-tenant cache/rate-limit isolation.
- Context7 official EF Core docs (`/dotnet/entityframework.docs`) were used for Phase 1.4 tenant-bypass proof, confirming global query filters as the tenant-isolation primitive and EF Core 10 named filters as the selective bypass mechanism. Tavily MCP was attempted for tenant-isolation/BOLA best-practice research but returned quota exhaustion (`status 432`), so the implementation evidence for these slices is repository source plus focused tenant-isolation tests.
- Context7 official EF Core docs (`/dotnet/entityframework.docs`) were rechecked for R-010 cancellation propagation. EF Core async execution methods such as `ToListAsync`, `FirstOrDefaultAsync`, `CountAsync`, and `ExecuteDeleteAsync` accept cancellation tokens and pass them to the underlying provider, though provider support determines whether cancellation is honored. Tavily MCP research/extract was retried for this R-010 continuation on 2026-07-04 and returned quota exhaustion (`status 432`), so no new Tavily result was used for the organization, instance-bootstrap-state, or EventSessionSpeaker cancellation slices.
- Context7 official ASP.NET Core docs (`/dotnet/aspnetcore.docs`) were rechecked during the R-029 plan correction. They confirm `[ApiController]` controllers require attribute routing, and controller OpenAPI response metadata is extracted from action signatures and attributes such as `[ProducesResponseType]`; this supports the plan requirement for explicit route templates, route names, endpoint classification, typed ProblemDetails metadata, and generated OpenAPI/client evidence. Tavily MCP extract was retried for the same Microsoft Learn API/authorization/OpenAPI sources and returned `status 432` quota exhaustion, so this R-029 correction relies on Context7 plus repository source evidence rather than Tavily content.
- Repo source (`Explore.ServiceDefaults/Extensions.cs`) currently implements this guidance with project-specific `/health`, `/alive`, and `/metrics` paths.
