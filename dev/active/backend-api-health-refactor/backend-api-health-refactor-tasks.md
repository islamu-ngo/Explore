<!-- ABOUTME: Tactical checklist for the backend/API health refactor implementation plan. -->
<!-- ABOUTME: Tracks reviewable slices, exact acceptance criteria, and verification commands. -->

# Backend API Health Refactor - Task Checklist

Last Updated: 2026-07-04 Europe/Brussels

## Status Summary

- **Overall status:** Implementation in progress.
- **Completed in this implementation slice:**
  - Plan/context/tasks rewritten, user implementation approval recorded, current worktree inspected, and first source slice selected.
  - Health response redaction implemented; health path product-doc drift corrected.
  - Phase 0.3 summary reconciliation recorded; inventory generator source mismatch fixed; `docs/API_CONTRACT_INVENTORY.md` regenerated from the 506-operation build-time schema with route/tenant/rate/cache posture columns.
  - Unrelated agent-context architecture blocker resolved by restoring AI-context disclosure docs.
  - R-015 runtime `/admin/migrate` endpoint removed and regression-tested.
  - R-033 webhook management/audit behavior covered across API, Application, and Persistence tests.
  - R-034 incoming webhook public-ingestion behavior hardened and covered.
  - R-035 event-report/moderation privacy reviewed, data-minimized, contract-regenerated, and covered with focused API/Application tests.
  - R-016 event-registration generic reads self-scoped and data-minimized.
  - R-017 tenant role grant reads made tenant-admin/resource protected with Cerbos/fallback parity.
  - R-018 organization member reads made tenant/org resource protected with Cerbos/fallback/HAL parity.
  - R-019 footer writes made tenant-update resource protected with focused API/Application/Infrastructure coverage.
  - R-020 event-template HAL affordance drift corrected and covered across API/Application/Blazor tests.
  - R-012 storage object metadata/content/presigned reads made authenticated/resource-protected with Cerbos/fallback/HAL parity and no-store presigned responses.
  - R-036 email dispatch admin status/control endpoints made tenant-scoped/resource-protected with Cerbos/fallback/HAL parity.
  - R-037 user-organization reads made Application-enforced self-service with typed 401/403 ProblemDetails metadata.
  - R-038 module enable/disable writes made tenant-update resource protected through MediatR commands, `ISecureRequest` metadata, write-rate metadata, and Local RBAC tenant-admin/regular-user tests.
  - R-039 analytics relay public-ingestion boundary evidence added: API metadata test locks anonymous public classification, stable route name, dedicated `AnalyticsRelay` limiter, and response metadata; Application handler test proves server-side governance sanitization before provider dispatch.
  - R-040 user-authentication-token self-service boundary hardened: reads are current-user scoped, read DTOs no longer expose credentials or user/tenant identity, create/update no longer accept client-supplied ownership, and read routes are no-store/no-output-cache.
  - Stale EventRegistration high-risk annotation reconciled: `GET /api/eventregistration/by-user/{userId}` now matches the already-mitigated R-016 source reality instead of remaining a false Critical/to-verify task.
  - Custom-property projection admin authorization context hardened and reconciled: tenant-wide status/rebuild/drain routes now carry tenant/projection resource metadata, event/session row and single-rebuild routes are server-enriched before authorization, and Cerbos/local fallback deny missing or mismatched tenant context.
  - R-013 bounded fallback parity slice completed: generic tenant-scoped fallback denies explicit cross-tenant attributes, optimized batch authorization now mirrors single-decision tenant checks for covered tenant resources, support-access projection reads, custom-property projection context, and storage metadata/download boundaries.
  - Phase 2.1 ProblemDetails migration audit completed: the direct controller raw-helper and raw 4xx command-envelope metadata sweeps are clean after migrating the final `EventSessionLanguageController.Update` missing-`If-Match` branch to the central validation helper.
  - R-004 setup-secret onboarding error/rate-limit contract mitigated with central RFC 7807 filter responses, `SetupSecret` limiter metadata, focused API tests, and regenerated OpenAPI/client artifacts.
  - R-004 bootstrap audit emission implemented through Application-owned structured audit events and `ILogger`/`EventId` output at setup-secret, Keycloak bootstrap, and setup-disablement boundaries.
  - R-004 Keycloak bootstrap provider-failure ProblemDetails implemented for 502/503 upstream failure families with focused API tests and regenerated OpenAPI/inventory/client artifacts.
  - R-004 Keycloak maintenance coverage corrected to the verified split of backup-confirmed additive sync apply plus provider-accepted-before-persist client-secret rotation.
  - Phase 2.3 route-name/HAL route guardrails reverified on the current dirty branch: the API test project builds, and `RouteNameCoverageTests` passes the constant-to-endpoint, endpoint-to-constant, and sanity checks.
  - Dirty-tree verification blockers repaired where they prevented required checks, including the shared `sid` provider-subject fallback, the Blazor support-access banner test fixture, and the Blazor integration minimal DI fixture for support-access session forwarding.
  - Phase 1.4 tenant-bypass proof now covers fourteen source-specific bypass paths: `TenantLookupSource` cache warmup, `TenantCapabilityRepository` module-capability resolution, `ExternalApiKeyRepository` credential/platform-key lookup, `ExternalApiKeyQuotaRepository` platform usage reporting, `EventRepository` authorization-target resolution, `StorageObjectRepository` delete-requested resource reconciliation, `UserExternalLoginRepository` external identity resolution, `TenantUserRoleGrantRepository` tenant-authority/membership resolution, `TenantUserRepository` tenant membership / actor / active-state resolution, `TenantSettingRepository` tenant-setting override management, `TenantSettingsDocumentRepository` typed settings document resolution, `NotificationRepository` deduplication checks, `EmailDispatchOutboxRepository` worker queue / tenant-operator / receipt idempotency resolution, and the webhook repository tenant-operation / worker-queue family. Focused persistence coverage proves each covered path is bounded by explicit predicates and does not leak ambient/wrong-tenant rows.
- **Current priority:** Continue high/critical risk ownership against the regenerated endpoint inventory. R-041 external API-key management hardening is implemented and verified for the audited credential-management defects.
- **Next recommended slice:** Select the next source-grounded high/critical row from the risk register after rechecking current source and `docs/API_CONTRACT_INVENTORY.md`. Do not reopen broad ProblemDetails, route-name cleanup, R-039 analytics relay, R-040 token self-service, or R-041 external API-key management unless a new source audit finds a concrete gap.

## Implementation Maintenance Rules

- [x] Before starting implementation, read plan/context/tasks.
- [x] Classify the exact implementation slice against `.claude/contract/intents.yaml`.
- [x] Read required docs/rules/skills for the files you will edit.
- [x] Inspect current `git status --short` and avoid unrelated dirty files.
- [x] After each completed task, update this checklist immediately.
- [x] If architecture/scope changes, update the plan before continuing.
- [x] If discoveries affect future work, update the context file.
- [ ] Every final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

## Phase 0: Plan Review And Current-State Re-baseline

- [x] **0.1 User reviews and approves or corrects this re-baselined plan.**
  - **Files:** `backend-api-health-refactor-plan.md`, `backend-api-health-refactor-context.md`, `backend-api-health-refactor-tasks.md`.
  - **Acceptance:** Planning status becomes User-reviewed or Approved in plan/context.
  - **Validation:** User directive on 2026-07-03: "fully implement the implementation plan".
  - **Effort:** S.
  - **Dependencies:** none.

- [x] **0.2 Record current worktree state before source edits.**
  - **Files:** `backend-api-health-refactor-context.md`.
  - **Acceptance:** Context records current dirty source/doc files relevant to this workstream and states which changes are unrelated.
  - **Validation:** `git status --short` captured a heavily dirty worktree with many unrelated source/doc/test changes. This slice avoided unrelated changes except for mechanical verification blockers in already-modified dirty-tree files: Support Access authorization/API-contract parity, storage content-signature byte header typing, and `DatabaseSeeder.SeedAsync` call-site argument binding.
  - **Effort:** S.
  - **Dependencies:** 0.1.

- [ ] **0.3 Reconcile endpoint inventory and risk register with current source/OpenAPI.**
  - **Files:** `endpoint-inventory.md`, `backend-contract-risk-register.md`, generated OpenAPI/inventory artifacts only through documented generation commands if source contract changed.
  - **Acceptance:** Inventory rows are marked current or stale; high/critical rows link to risk IDs; no manual edits to generated `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, or `EventApiClient.g.cs`.
  - **Partial evidence 2026-07-04:** `ApiContractInventoryGeneratorTests` reads `schemas/openapi.json`, and `docs/API_CONTRACT_INVENTORY.md` has been regenerated to `365` paths/`506` operations/`0` missing operation IDs/`0` missing endpoint classes. The generated table includes `RouteName`, `TenantMode`, `RateLimitPolicy`, and `CachePolicy` columns. The prior doc-only `/admin/migrate` and schema-only webhook message/delivery diff is resolved.
  - **Mitigated risks in this reconciliation lane:** R-032 inventory source drift; R-015 runtime `/admin/migrate`; R-016 self-service event-registration reads; R-017 tenant role grant reads; R-018 organization member reads; R-019 footer writes; R-020 event-template HAL affordances; R-033 webhook audit/retry; R-034 incoming webhook ingestion; R-035 event-report/moderation privacy; R-036 EmailDispatch admin authorization; R-037 user-organization self-service reads; R-038 module enable/disable tenant governance; R-039 analytics relay public-ingestion boundary; R-040 user-authentication-token self-service credential boundary; the R-013 bounded local fallback tenant-context/batch-parity sub-slice; the custom-property projection admin authorization-context row; and the verified R-004 setup/bootstrap/admin safety surface.
  - **R-016 annotation reconciliation:** the manually maintained high-risk annotation for `GET /api/eventregistration/by-user/{userId}` has been corrected from stale Critical/to-verify language to Mitigated. Source evidence: `EventRegistrationController.GetRegistrationsByUser` is authenticated, classed as `EndpointClass.Authenticated`, checks route user id against `CurrentUserId`, and returns `403` before MediatR dispatch for cross-user reads. Application handlers repeat current-user ownership checks before repository access, and DTO privacy tests prove registrant identity fields are not serialized. Focused verification passed: `EventRegistrationControllerTests` 5/5, `EventRegistrationSelfReadQueryHandlerTests` 4/4, and `EventRegistrationReadDtoPrivacyTests` 2/2.
  - **Custom-property projection admin evidence:** the manually maintained Critical/to-verify projection admin row has been corrected from "`[Authorize]` only" to Mitigated. Source evidence: all projection status/row/dirty-scope queries require `ResourceKinds.CustomPropertyProjection` / `AuthorizationActions.CustomPropertyProjections.View`; all rebuild/drain commands require `ResourceKinds.CustomPropertyProjection` / `AuthorizationActions.CustomPropertyProjections.Update`; tenant-wide routes carry `tenantId`; event/session-specific routes carry `eventId`/`eventSessionId` and are enriched to tenant context by `AuthorizationBehavior` before provider evaluation; local fallback denies missing or mismatched tenant context; Cerbos tests prove instance admin and same-tenant tenant admin allow while cross-tenant admin and regular users deny.
  - **R-013 bounded fallback evidence:** `FallbackAuthorizationService.Evaluators` now denies invalid or explicit cross-tenant `tenantId` attributes for generic tenant-scoped resources while preserving the ambient-tenant fallback when no tenant attribute is supplied. `FallbackAuthorizationService.Batch` now mirrors single-decision tenant matching for tenant resources, tenant role grants, taxonomy/location, custom-property governance, EmailDispatch, webhooks, custom-property projection, and support-access session read evidence; storage batch evaluation now denies broad metadata `view` to regular users while preserving active content download/presigned rules for public/authenticated/private-owner objects.
  - **R-038 evidence:** module enable/disable now dispatches `EnableTenantModuleCommand` and `DisableTenantModuleCommand` through MediatR; both commands expose `ISecureRequest` tenant/module/action metadata under `ResourceKinds.Tenant` / `AuthorizationActions.Update`; enable resolves the audit actor through `IAdminContext`; routes advertise the `Write` limiter; Local RBAC proves tenant-admin allow and regular-user deny; generated OpenAPI/inventory records `Write` for both operations.
  - **R-039 evidence:** `AnalyticsRelayController.Relay` is `[AllowAnonymous]`, `EndpointClass.Public`, `RouteNames.RelayAnalyticsEvent`, and `RateLimitingExtensions.AnalyticsRelayPolicy`; it has 202/400 response metadata and no HAL mutation rel. `RelayAnalyticsEventCommandHandler` applies tenant analytics configuration, server-side tenant context, allow-listed event/property shapes, and `AnalyticsGovernanceService` sanitization before provider calls.
  - **R-040 evidence:** `UserAuthenticationTokenController` remains authenticated, but list/detail handlers now scope repository access by `ICurrentUserService.UserId`; create/update stamp `UserId` and `TenantId` from server context; update/delete load by id plus current user; read DTOs expose only session metadata; credential fields remain write-only request inputs; and list/detail routes use `ResponseCache(NoStore=true, Location=None)` without `OutputCache`.
  - **Validation:** `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity minimal --no-restore -maxcpucount:1`; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/ApiContractInventory_Generate_WritesMarkdownToDocs" --minimum-expected-tests 1`; `dotnet msbuild Explore.Blazor.Client/Explore.Blazor.Client.csproj /t:GenerateApiClient /p:Configuration=Release /p:Restore=false /m:1 /v:minimal`; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/AdminMigrate_Post_IsNotMappedInTestingHost" --minimum-expected-tests 1`; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/WebhooksControllerTests/*" --minimum-expected-tests 1`; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IncomingWebhookFrameworkTests/*" --minimum-expected-tests 1`; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/WebhookConsumerHandlersTests/*" --minimum-expected-tests 1`; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/WebhookPersistenceTests/MessageAndDeliveryRepositories_ApplyExplicitTenantPredicates" --minimum-expected-tests 1`; readback of `docs/API_CONTRACT_INVENTORY.md`; `rg` checks for `/admin/migrate` source references and webhook message/delivery rows.
  - **Additional R-038 validation:** `dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/Event.Application.UnitTests.Features.Modules.Commands/*/*" --minimum-expected-tests 1`; `dotnet test Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ModuleControllerTests/*" --minimum-expected-tests 1`; `dotnet test Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/LocalRbacAuthorizationTests/*" --minimum-expected-tests 1 --timeout 10m`; API inventory generation.
  - **Additional R-016 annotation validation:** targeted stale-text checks for the old EventRegistration Critical/public-inventory wording returned no matches; `git diff --check -- dev/active/backend-api-health-refactor/endpoint-inventory.md dev/active/backend-api-health-refactor/backend-api-health-refactor-tasks.md dev/active/backend-api-health-refactor/backend-api-health-refactor-context.md dev/active/backend-api-health-refactor/backend-api-health-refactor-plan.md` passed; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventRegistrationControllerTests/*" --minimum-expected-tests 1 --no-progress` passed 5/5; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventRegistrationSelfReadQueryHandlerTests/*" --minimum-expected-tests 1 --no-progress` passed 4/4; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventRegistrationReadDtoPrivacyTests/*" --minimum-expected-tests 1 --no-progress` passed 2/2.
  - **Additional custom-property projection admin validation:** focused metadata/enrichment/fallback/policy checks passed: `ProjectionQueryAuthorizationMetadataTests` 10/10, `ProjectionCommandAuthorizationMetadataTests` 10/10, `AuthorizationBehaviorTests` 17/17, `FallbackAuthorizationServiceTests` 78/78, and `CerbosPolicyCompilationTests` 39/39. Full `dotnet build --configuration Release --verbosity quiet` passed across 25 projects with the existing warning backlog.
  - **Additional R-013 fallback validation:** `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/FallbackAuthorizationServiceTests/*" --minimum-expected-tests 1 --no-progress` passed 88/88 after adding single and optimized-batch tenant-context, projection, support-access, and storage boundary coverage. `dotnet build --configuration Release --verbosity quiet` passed 25 projects with 0 errors and the existing warning backlog; `git diff --check` passed for the changed source/test/plan artifacts.
  - **Additional R-040 validation:** `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/UserAuthenticationToken*/*" --minimum-expected-tests 1 --no-progress` passed 8/8; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/UserAuthenticationTokenControllerMetadataTests/*" --minimum-expected-tests 1 --no-progress` passed 3/3; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet --no-restore -maxcpucount:1` passed; API inventory generation and NSwag client generation passed; `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet --no-restore -maxcpucount:1` passed when rerun by itself after an intentional parallel-build file-lock retry; full `dotnet build --configuration Release --verbosity quiet --no-restore -maxcpucount:1` passed 25 projects / 0 errors.
  - **Effort:** M.
  - **Dependencies:** 0.2.

- [x] **0.4 Recheck verification blockers.**
  - **Files:** `backend-api-health-refactor-context.md`.
  - **Acceptance:** Current status is known for architecture context failures, API integration Docker/Testcontainers/host-shutdown issue, and Blazor build issue.
  - **Evidence 2026-07-04:** The prior architecture blockers were unrelated agent-context manifest/doc issues: `external-infrastructure-bootstrap` referenced unknown `Explore.Infrastructure.Tests`, and `update-ai-context-disclosure` referenced missing `dev/active/ai-context-disclosure-policy/*.md` files. The fix added `Explore.Infrastructure.Tests` to the manifest test's known project list, restored the active AI-context disclosure plan/matrix/tasks/MCP audit docs, and corrected the `UserPii` summary count in `docs/AI_CONTEXT_SECURITY.md`. `Event.Architecture.Tests` passes: 240 total, 239 succeeded, 1 intentional skip. `Explore.Blazor.Client` Release build passes from the scoped recheck. Docker daemon was available (`29.5.3`). Focused API host/auth lane passed (`NoKeycloakAuthenticationTests`, 8/8). Focused PostgreSQL Testcontainers lane passed (`DatabaseSeederTests`, 2/2). The full API/persistence suites were not launched for this blocker-classification slice.
  - **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`; run only scoped commands needed to classify blockers; do not launch full suite unless source changes require it.
  - **Effort:** M.
  - **Dependencies:** 0.2.

- [x] **0.5 Decide first implementation PR boundary.**
  - **Files:** plan/context/tasks.
  - **Acceptance:** First source slice selected as Phase 2.5/2.6 health response redaction and health path doc reconciliation.
  - **Validation:** Context names changed files and tests.
  - **Effort:** S.
  - **Dependencies:** 0.3, 0.4.

## Phase 1: Security, Authorization, Tenant, And HAL Corrections

- [x] **1.1 Finish P0 identity-bearing endpoint hardening.**
  - **Files:** `EventRegistrationController`, `TenantUserRoleGrantController`, `OrganizationMemberController`, related handlers/DTOs/tests as verified in Phase 0.
  - **Acceptance:** Anonymous requests fail or return safe public projections; identity/member/role/grant fields are not leaked.
  - **Validation:** API integration auth/field-shape tests; endpoint classification architecture tests.
  - **Evidence 2026-07-04:** R-016/EventRegistration is mitigated. Generic registration reads are authenticated and self-scoped in Application handlers; `by-user/{userId}` rejects mismatched route/current-user IDs before MediatR dispatch; `EventRegistrationDto` and `EventRegistrationListDto` no longer serialize `userId`, `userFullName`, or `userEmail`; repositories no longer load `User.Pii` for generic registration reads; MCP registration descriptors no longer expose registrant names; OpenAPI/inventory/client artifacts were regenerated. R-017/TenantUserRoleGrant is mitigated by keeping the identity-bearing role-grant DTOs administrative: read requests now require authentication plus `islamuevent_tenant_user_role_grant` resource authorization for action `view` with tenant attributes, regular authenticated users are denied by Cerbos and local fallback, and OpenAPI/inventory/client artifacts were regenerated. R-018/OrganizationMember is mitigated by keeping the identity-bearing member DTO administrative: list/detail read requests require authentication plus `islamuevent_organization_member:view`, direct member-id reads are enriched with tenant/organization/user attributes before authorization, regular authenticated users are denied by Cerbos and local fallback, HAL collection-create checks carry tenant/organization attributes, `OrganizationMemberDto` carries `tenantId`, and OpenAPI/inventory/client artifacts were regenerated. R-040/UserAuthenticationToken is mitigated as self-service token-session management: read handlers scope by current user, write handlers stamp ownership from server context, read DTOs no longer expose credentials or identity fields, create/update DTOs no longer accept `userId`/`tenantId`, and generated OpenAPI/client artifacts were regenerated.
  - **Effort:** L.
  - **Dependencies:** Phase 0.

- [ ] **1.2 Complete resource-action authorization parity for high-risk endpoint families.**
  - **Files:** `authorization-policy-matrix.md`, Application auth metadata, Cerbos/local fallback policies, API attributes.
  - **Acceptance:** Event registrations, tenant role grants, organization members, footer writes, module enable/disable writes, AI assistant, storage objects, tenant storage, email admin, and bootstrap/admin flows have explicit resource/action policy decisions or documented deferrals.
  - **Validation:** `AuthorizationParityTests`, Application unit tests, API 401/403 ProblemDetails tests.
  - **Partial evidence 2026-07-04:** R-033 webhook audit/retry parity is covered: controller route metadata and rate/cache posture, Application `AuthorizeResource` actions, HAL retry affordance gating, MediatR tenant mapping, and repository tenant predicates now have focused tests. R-017 tenant role grant read parity is covered across controller auth metadata, `ISecureRequest` query metadata, Cerbos policy/schema, local fallback, and API denial-path tests; create/revoke commands now also carry tenant attributes through `ISecureRequest` for Cerbos tenant-admin derived-role evaluation. R-018 organization member read parity is covered across controller auth metadata, `ISecureRequest` query metadata, direct member-id authorization enrichment, Cerbos policy/schema, local fallback, HAL collection-create metadata, and API denial-path tests. R-019 footer write parity is covered for the current tenant-update convention: the controller sends resolved tenant id into all footer write commands, commands expose tenant resource metadata, local fallback allows tenant admins only for the resolved tenant, and API denial tests prove unauthorized writes return `403`. R-012 storage read parity is covered for metadata/content/presigned endpoints: API routes are authenticated, Application queries carry `islamuevent_storage_object` actions through `ISecureRequest`, `AuthorizationBehavior` enriches storage object attributes, Cerbos and fallback policies agree on metadata/admin and content/read semantics, HAL content/presigned links are permission-bound, and presigned responses are no-store/no-output-cache. R-036 EmailDispatch admin parity is covered for status/pause/resume/park/replay endpoints: controller routes remain authenticated, Application requests now carry `islamuevent_email_dispatch` tenant/outbox metadata through `ISecureRequest`, Cerbos and fallback policies agree on tenant-admin-only operator access, HAL replay/park links use split permission actions, and API denial tests prove provider-denied calls return `403`. R-037 user-organization self-service parity is covered for the current endpoint shape: the controller stays authenticated and advertises typed 401/403 metadata, the handler resolves `ICurrentUserService.UserId`, and missing/mismatched users fail before repository access. R-004 setup/bootstrap/admin safety is mitigated for the verified surface: setup-secret-gated onboarding actions consistently use the `SetupSecret` limiter and typed RFC 7807 `403`/`410`/`429` metadata, `complete` still requires authentication plus setup secret, setup-secret/Keycloak/bootstrap-disablement audit events emit through structured Application logging, Keycloak bootstrap provider timeout/unreachable/invalid/upstream failures return typed `502`/`503` ProblemDetails, sync apply blocks without backup confirmation and uses only additive Admin API calls, and client-secret rotation only persists/reloads after Keycloak reports success. This does not complete the broader parity task because other endpoint families remain.
  - **R-038 parity evidence 2026-07-04:** Module enable/disable writes now use the same tenant-update convention as tenant-governed settings writes. The controller sends the resolved tenant id into `EnableTenantModuleCommand` and `DisableTenantModuleCommand`; both commands expose tenant resource metadata; enable resolves the audit actor in the Application handler; route metadata advertises the `Write` limiter; and API denial tests prove regular authenticated users receive `403` while tenant admins can enable/disable modules. This does not complete the broader parity task because other endpoint families remain.
  - **R-039 evidence 2026-07-04:** Analytics relay is not a privileged auth policy; it is deliberate anonymous public ingestion with a dedicated limiter and Application-layer governance. `AnalyticsRelayControllerTests` locks public classification, `[AllowAnonymous]`, `RouteNames.RelayAnalyticsEvent`, `AnalyticsRelay` rate limiting, and response metadata. `RelayAnalyticsEventCommandHandlerTests` locks tenant-aware governance sanitization. This does not complete the broader parity task because other endpoint families remain.
  - **R-040 evidence 2026-07-04:** User-authentication-token generic routes are self-service, not administrative. Current handlers fail closed without `ICurrentUserService.UserId`, list/detail/update/delete use user-scoped repository methods, create/update stamp server-owned user/tenant ids, and credential values are never serialized back through read DTOs. `UserAuthenticationTokenControllerMetadataTests` locks authenticated endpoint classification and no-store/no-output-cache read metadata. This does not complete the broader parity task because other endpoint families remain.
  - **Effort:** L.
  - **Dependencies:** 1.1.

- [x] **1.3 Close HAL affordance drift.**
  - **Files:** HAL policies/assemblers and Blazor client components identified by Phase 0.
  - **Acceptance:** Edit/delete/publish/create/admin actions render from `_links`; local role/claim checks are limited to navigation/route/menu-level UX.
  - **Validation:** API HAL tests and `Explore.Blazor.Client.Tests` bUnit tests.
  - **Evidence 2026-07-04:** R-020 event-template affordance drift is mitigated. `EventTemplateCollectionLinkPolicy` emits item `edit`/`delete` rels through tenant-resource `AuthorizationActions.Update/Delete` metadata; `ResourceDescriptorRegistry` recognizes event-template DTOs as tenant-governed resources; `PaginatedResult<T>` now carries collection HAL links; event-template model mapping preserves item `_links` and `definitionCount`; `EventTemplateListPage` gates create from collection `_links.create` and row actions from item links. `MyOrganizations` was inspected and already gates create-event from row HAL links, with no role/claim fallback.
  - **Effort:** M.
  - **Dependencies:** 1.2.

- [ ] **1.4 Add semantic tenant-bypass proof for remaining bypass call sites.**
  - **Files:** Persistence repositories/services using tenant bypass, `tenant-execution-model.md`, tests.
  - **Acceptance:** Each bypass has a bounded predicate, reason, operation name, and test proving it does not leak the ambient/wrong tenant.
  - **Validation:** `Event.Persistence.IntegrationTests`; architecture guard that controllers cannot call raw bypass helpers.
  - **Partial evidence 2026-07-04:** `TenantLookupSource.GetTenantLookupsAsync` uses the approved `TenantLookupCacheWarmup` tenant-filter bypass reason only on `TenantSettingOverrides`, after first loading active tenant IDs from the unfiltered `Tenants` set and then bounding the bypassed setting query by those active tenant IDs plus the domain-setting keys. `TenantLookupSourceBypassTests.GetTenantLookupsAsync_WithAmbientTenant_ReturnsOnlyActiveTenantsAndTheirDomainSettings` proves a tenant-filtered context normally sees only the ambient tenant setting, while the lookup source intentionally returns active tenant A and B lookup values and excludes an inactive tenant's subdomain. This is one covered call-site family, not completion of the full bypass-audit task.
  - **Partial evidence 2026-07-04:** `TenantCapabilityRepository` uses the approved `TenantCapabilityResolution` tenant-filter bypass reason for tenant-module capability resolution and bounds every bypassed read by the explicit tenant ID supplied to the repository call. `TenantCapabilityRepositoryBypassTests.TenantCapabilityResolution_WithAmbientTenant_ReturnsOnlyExplicitTenantCapabilities` proves an ambient tenant B context normally sees only tenant B capabilities, while repository methods called with tenant A's ID return only tenant A capability rows, enabled module state, and module-key lookups.
  - **Partial evidence 2026-07-04:** `ExternalApiKeyRepository` uses the approved `ExternalApiKeyAuthentication` and `ExternalApiKeyPlatformManagement` tenant-filter bypass reasons for credential authentication, usage metadata touch, and platform-scoped InstanceAdmin key management. `ExternalApiKeyRepositoryBypassTests.CredentialBypasses_WithAmbientTenant_ReturnOnlyExplicitApiKeyPredicates` proves an ambient tenant B context normally sees only tenant B API keys, while bypassed repository methods can resolve tenant A by exact `KeyId`, touch tenant A usage metadata by exact key ID without updating tenant B, and resolve platform keys only by exact ID or InstanceAdmin owner/name tuple.
  - **Partial evidence 2026-07-04:** `ExternalApiKeyQuotaRepository.GetUsagePlatformWide` uses the approved `ExternalApiKeyPlatformUsageReport` tenant-filter bypass reason for InstanceAdmin quota reporting. The slice also hardened `ExternalApiKeyQuota` with a tenant query filter through its required `ExternalApiKey` navigation after the regression test exposed that raw quota rows were not tenant-filtered. `ExternalApiKeyQuotaRepositoryBypassTests.GetUsagePlatformWide_WithAmbientTenant_ReturnsOnlyRequestedPeriodUsageAcrossKeys` proves an ambient tenant B context normally sees only tenant B quota rows, tenant A usage-by-tenant remains empty from tenant B, while platform reporting returns tenant A, tenant B, and platform-key summaries only for the requested period.
  - **Partial evidence 2026-07-04:** `EventRepository.GetAuthorizationTargetByIdAsync` uses the approved `EventAuthorizationTargetResolution` tenant-filter bypass reason and bounds the authorization-target lookup by exact event ID. `EventAuthorizationTargetBypassTests.GetAuthorizationTargetByIdAsync_WithAmbientTenant_ReturnsOnlyExactEvent` proves an ambient tenant B context normally sees only tenant B events and cannot resolve tenant A through the standard repository read, while the bypassed authorization-target read returns tenant A only when tenant A's exact event ID is requested.
  - **Partial evidence 2026-07-04:** `StorageObjectRepository.ListDeleteRequestedForResourceAsync` uses the approved `InstanceStorageAdministration` tenant-filter bypass reason, but is bounded by explicit tenant ID, lifecycle state, provider set, owning resource kind, owning resource ID, soft-delete state, and limit. `StorageObjectRepositoryBypassTests.ListDeleteRequestedForResourceAsync_WithAmbientTenant_ReturnsOnlyExplicitTenantResourceRows` proves an ambient tenant B context normally sees only tenant B storage objects, while the bypassed reconciliation query for tenant A returns only tenant A delete-requested rows for the requested event resource. This covers the bounded delete-requested resource reconciliation path, not the broader instance storage report/reconciliation bypass surface.
  - **Partial evidence 2026-07-04:** `UserExternalLoginRepository.GetByProviderAndKey` uses the approved `UserExternalLoginAuthentication` tenant-filter bypass reason and bounds authentication identity resolution by exact provider plus provider key. `UserExternalLoginRepositoryBypassTests.GetByProviderAndKey_WithAmbientTenant_ReturnsOnlyExactExternalLogin` proves an ambient tenant B context normally sees only tenant B external-login rows, while the bypassed authentication lookup returns tenant A only for tenant A's exact provider key; the non-bypassed `GetByUser` path remains ambient-tenant scoped.
  - **Partial evidence 2026-07-04:** `TenantUserRoleGrantRepository` uses the approved `TenantScopedRepositoryExactTenantPredicate` bypass reason for exact tenant/user authority checks and the approved `UserTenantMembershipEnumeration` bypass reason for cross-tenant membership enumeration by user. `TenantUserRoleGrantRepositoryBypassTests.RoleGrantBypasses_WithAmbientTenant_ReturnOnlyExplicitTenantAndUserMembershipRows` proves an ambient tenant B context normally sees only tenant B grants, exact tenant A authority methods return tenant A grants only by explicit tenant/user/role predicates, cross-tenant membership enumeration returns only the requested user's active grants, and revoked or unrelated-user grants are excluded.
  - **Partial evidence 2026-07-04:** `TenantUserRepository` uses the approved `TenantScopedRepositoryExactTenantPredicate` bypass reason for tenant-local membership, actor-context, and active-state checks. `TenantUserRepositoryBypassTests.ExactTenantUserBypasses_WithAmbientTenant_ReturnOnlyExplicitTenantMembershipRows` proves an ambient tenant B context normally sees only tenant B tenant-user rows, while repository methods return tenant A only by explicit tenant/user or tenant/actor predicates and active-state checks exclude suspended or soft-deleted tenant users.
  - **Partial evidence 2026-07-04:** `TenantSettingRepository` uses the approved `TenantScopedRepositoryExactTenantPredicate` bypass reason for tenant-setting override reads, lock/unlock, and removal. `TenantSettingRepositoryBypassTests.ExactTenantSettingBypasses_WithAmbientTenant_ReturnAndMutateOnlyExplicitTenantRows` proves an ambient tenant B context normally sees only tenant B settings, while repository methods return tenant A only by explicit tenant/key predicates, list only tenant A rows for explicit tenant A, lock/unlock only tenant A keys, remove only tenant A's requested override, and leave tenant B rows unchanged.
  - **Partial evidence 2026-07-04:** `TenantSettingsDocumentRepository` uses the approved `TenantScopedRepositoryExactTenantPredicate` bypass reason for typed tenant settings document reads. Existing `TenantSettingsDocumentPersistenceTests` prove normal tenant context sees only the ambient tenant's document rows, a wrong-ambient tenant context can resolve only the explicitly requested tenant/document key, and `GetManyForTenant` returns only the requested tenant's requested document keys.
  - **Partial evidence 2026-07-04:** `NotificationRepository.ExistsByDeduplicationKeyAsync` uses the approved `TenantScopedRepositoryExactTenantPredicate` bypass reason for cross-tenant notification deduplication checks. `NotificationRepositoryBypassTests.DeduplicationBypass_WithAmbientTenant_ReturnsOnlyExactTenantUserKeyPredicate` proves an ambient tenant B context normally sees only tenant B notifications, while the bypassed lookup returns true only for the explicitly requested tenant/user/deduplication-key tuple and does not match the same key under the wrong tenant or user.
  - **Partial evidence 2026-07-04:** `EmailDispatchOutboxRepository` uses `EmailDispatchWorkerCrossTenantQueue` for background worker queue reads/updates and `EmailDispatchTenantOperation` for tenant-operator status/control/receipt idempotency operations. `EmailDispatchOutboxRepositoryBypassTests` proves an ambient tenant B context normally sees only tenant B outbox/control/receipt rows; worker queue reads return only due/pending rows and RabbitMQ-publishable rows that satisfy due, retry-throttle, and tenant-pause predicates; worker claims mutate only the exact dispatch ID; tenant-operator reads and pause/park/replay actions are bounded by explicit tenant/outbox/publish-event predicates; and receipt duplicate detection is bounded by tenant plus publish event.
  - **Partial evidence 2026-07-04:** Webhook repositories use `WebhookTenantOperation` for tenant management/audit/idempotency reads and exact-row updates, and `WebhookWorkerCrossTenantQueue` for provider sync, local delivery, payload cleanup, and stale-worker recovery. `WebhookRepositoryBypassTests` proves an ambient tenant B context normally sees only tenant B webhook consumers/endpoints/messages/attempts/incoming messages/provider links; tenant-operation methods return tenant A rows only by explicit tenant/name/id/provider/message predicates and update only tenant A rows; worker queue methods return only due scheduled attempts, pending provider links, stale sending attempts, and expired payloads across tenants; worker claims require the exact tenant/attempt ID; and status refresh is bounded by explicit tenant/message predicates.
  - **Partial validation 2026-07-04:** `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/TenantLookupSourceBypassTests/*" --minimum-expected-tests 1 --no-progress` passed 1/1; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/TenantCapabilityRepositoryBypassTests/*" --minimum-expected-tests 1 --no-progress` passed 1/1; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ExternalApiKeyRepositoryBypassTests/*" --minimum-expected-tests 1 --no-progress` passed 1/1; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ExternalApiKeyQuotaRepositoryBypassTests/*" --minimum-expected-tests 1 --no-progress` passed 1/1; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventAuthorizationTargetBypassTests/*" --minimum-expected-tests 1 --no-progress` passed 1/1; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/StorageObjectRepositoryBypassTests/*" --minimum-expected-tests 1 --no-progress` passed 1/1; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/UserExternalLoginRepositoryBypassTests/*" --minimum-expected-tests 1 --no-progress` passed 1/1; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/TenantUserRoleGrantRepositoryBypassTests/*" --minimum-expected-tests 1 --no-progress` passed 1/1; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/TenantUserRepositoryBypassTests/*" --minimum-expected-tests 1 --no-progress` passed 1/1; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/TenantSettingRepositoryBypassTests/*" --minimum-expected-tests 1 --no-progress` passed 1/1; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/TenantSettingsDocumentPersistenceTests/*" --minimum-expected-tests 1 --no-progress` passed 9/9; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/NotificationRepositoryBypassTests/*" --minimum-expected-tests 1 --no-progress` passed 1/1; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchOutboxRepositoryBypassTests/*" --minimum-expected-tests 1 --no-progress` passed 3/3; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/WebhookRepositoryBypassTests/*" --minimum-expected-tests 1 --no-progress` passed 2/2; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/PersistenceTenantFilterArchitectureTests/*" --minimum-expected-tests 1 --no-progress` passed 4/4; `git diff --check -- Explore.Persistence/ExploreDbContext.QueryFilters.cs Event.Persistence.IntegrationTests/TenantIsolation/TenantLookupSourceBypassTests.cs Event.Persistence.IntegrationTests/TenantIsolation/TenantCapabilityRepositoryBypassTests.cs Event.Persistence.IntegrationTests/TenantIsolation/ExternalApiKeyRepositoryBypassTests.cs Event.Persistence.IntegrationTests/TenantIsolation/ExternalApiKeyQuotaRepositoryBypassTests.cs Event.Persistence.IntegrationTests/TenantIsolation/EventAuthorizationTargetBypassTests.cs Event.Persistence.IntegrationTests/TenantIsolation/StorageObjectRepositoryBypassTests.cs Event.Persistence.IntegrationTests/TenantIsolation/UserExternalLoginRepositoryBypassTests.cs Event.Persistence.IntegrationTests/TenantIsolation/TenantUserRoleGrantRepositoryBypassTests.cs Event.Persistence.IntegrationTests/TenantIsolation/TenantUserRepositoryBypassTests.cs Event.Persistence.IntegrationTests/TenantIsolation/TenantSettingRepositoryBypassTests.cs Event.Persistence.IntegrationTests/Repositories/TenantSettingsDocumentPersistenceTests.cs Event.Persistence.IntegrationTests/TenantIsolation/NotificationRepositoryBypassTests.cs Event.Persistence.IntegrationTests/TenantIsolation/EmailDispatchOutboxRepositoryBypassTests.cs Event.Persistence.IntegrationTests/TenantIsolation/WebhookRepositoryBypassTests.cs` passed.
  - **Effort:** M.
  - **Dependencies:** Phase 0.

- [x] **1.5 Finish bootstrap/admin operational safety.**
  - **Files:** onboarding handlers/controllers, Keycloak/provider sync paths, audit infrastructure, docs.
  - **Acceptance:** Setup secret remains bootstrap-only; Keycloak bootstrap provider failures use typed ProblemDetails; audit events emit for bootstrap start/success/failure/disablement; Keycloak sync apply is backup-confirmed and additive-only; Keycloak client-secret rotation proves deployment-managed secrets produce operator instructions and application-managed secrets are persisted/reloaded only after Keycloak accepts the update.
  - **Validation:** Application unit tests, API integration tests, operations/security docs review.
  - **Evidence 2026-07-04:** Setup-secret transport safety is source-enforced for `InstanceOnboardingController`: every `[SetupSecretRequired]` action also declares `[EnableRateLimiting(RateLimitingExtensions.SetupSecretPolicy)]`, advertises `ProblemDetails` for `403`, `410`, and `429`, and uses setup-secret terminology in endpoint descriptions. `SetupSecretRequiredAttribute` returns central RFC 7807 ProblemDetails for invalid setup secrets and inactive setup mode instead of anonymous JSON. Bootstrap audit emission is implemented as an Application-owned structured audit boundary: setup-secret accepted/rejected/inactive checks, Keycloak bootstrap validation/start/failure/success, and setup-mode disablement emit `InstanceBootstrapAuditEvent` values through `IInstanceBootstrapAuditLogger`, with secrets, tokens, raw provider payloads, and endpoint URLs excluded. Provider-failure coverage is implemented: `keycloak_timeout`/`keycloak_unreachable` return `503`, invalid/upstream Keycloak Admin API failures return `502`, and focused API tests assert the ProblemDetails shape and secret redaction. Keycloak maintenance coverage was source-verified and corrected against official Keycloak docs: `ApplyRealmSyncAsync_WithoutBackupConfirmation_DoesNotContactKeycloak` blocks sync apply before any Admin API call, `ApplyRealmSyncAsync_WithTemporaryAdminCredentials_AppliesOnlyAdditiveAdminApiChangesAndRedactsSecrets` proves backup-confirmed sync uses GET/POST/PUT only and no DELETE, `RotateClientSecretAsync_WhenInputsAreValid_UsesPutAndRedactsSecrets` proves the replacement secret is sent to Keycloak but not echoed, `Handle_WhenApplicationManagedProviderBlocksRotation_DoesNotPersistNewSecretOrRefreshAuthSchemes` proves failed provider rotation does not persist the runtime secret or reload auth schemes, and deployment-managed rotation returns operator instructions without contacting Keycloak. Verification passed for this slice: focused `RotateKeycloakClientSecretCommandHandlerTests` 5/5; focused `KeycloakBootstrapServiceTests` 18/18; full `Event.Architecture.Tests` 240 total / 239 succeeded / 1 intentional skip; full Release build 25 projects / 0 errors; `git diff --check`. Prior R-004 validation also passed: `Explore.API` Release build; `Event.API.IntegrationTests` focused onboarding class 24/25 with 1 existing skip; API contract inventory generation 1/1; NSwag client generation; `Event.Application.UnitTests` 1905/1905; `Explore.Blazor.Client.Tests` 1447/1448 with 1 existing skip; `Explore.Blazor.IntegrationTests` 178/178 after a minimal support-access DI fixture fix; and focused setup API filter/flow tests. Full `Event.API.IntegrationTests` ran earlier in the R-004 audit slice but still has 6 unrelated `StorageObjectHateoasTests` failures where storage HATEOAS requests return unauthorized/empty bodies; the R-004 setup/provider tests passed within their focused lanes.
  - **Effort:** L.
  - **Dependencies:** 1.2.

- [x] **1.6 Mitigate R-035 event-report/moderation privacy.**
  - **Files:** `EventReportsController`, `ModerationReportController`, event-report query handlers/DTOs/HAL policies, generated contract artifacts, active docs.
  - **Acceptance:** Public report options stay anonymous and content-light; reporter reads stay authenticated/current-user scoped; moderation reads/actions stay event-resource authorized; HAL action rels remain the UI affordance source; moderation read DTOs do not expose stable reporter IDs, evidence creator IDs, decision moderator IDs, raw provider IDs/URLs/correlation IDs, reporter hashes, or raw provider payloads.
  - **Validation:** `GetModerationReportQueueRequestHandlerTests`; `GetModerationReportDetailRequestHandlerTests`; `EventReportHateoasTests`; `EventReportsControllerTests`; `ModerationReportControllerTests`; API build; generated inventory/client workflow.
  - **Effort:** M.
  - **Dependencies:** 0.3, 1.2, 1.3.

- [x] **1.7 Mitigate R-041 external API-key management hardening.**
  - **Files:** `Explore.API/Controllers/ExternalApiKeyController.cs`; `Explore.Application/Features/ExternalApiKeys/Handlers/Commands/CreateExternalApiKeyCommandHandler.cs`; `Explore.Application/Features/ExternalApiKeys/Handlers/Queries/GetExternalApiKeyUsageReportRequestHandler.cs`; `Explore.Application/DTOs/ExternalApiKey/Validators/UpdateExternalApiKeyPolicyDtoValidator.cs`; `Explore.Application/Contracts/Persistence/IExternalApiKeyRepository.cs`; `Explore.Persistence/Repositories/ExternalApiKeyRepository.cs`; `Event.API.IntegrationTests/Features/ExternalApiKeyIntegrationTests.cs`; new `Event.API.IntegrationTests/Features/ExternalApiKeyControllerMetadataTests.cs`; `Event.Application.UnitTests/Features/ExternalApiKeys/Commands/ExternalApiKeyObservabilityTests.cs`; generated contract artifacts; `docs/API_CHANGELOG.md`; active workstream docs.
  - **Acceptance:** `GetAll`, `GetById`, and `GetUsageReport` no longer use `[OutputCache]`; sensitive reads use `ResponseCache(NoStore=true, Location=None)` and advertise typed 401/403/404 metadata where applicable; read actions use `RateLimitingExtensions.AuthenticatedPolicy`; create/update/delete actions use `RateLimitingExtensions.WritePolicy`; create owner-authority denials throw `AuthorizationException` and return 403 instead of validation 400; usage-report denials throw `AuthorizationException` and return 403 instead of an empty success list; delete returns 404 when revoke returns false; platform-scoped `InstanceAdmin` key-name uniqueness uses `ExistsByOwnerAndNameIgnoringTenantFilter`; repository read/exists methods propagate `CancellationToken` where changed; generated OpenAPI/inventory/client artifacts reflect new response/cache/rate metadata; changelog records the behavior hardening.
  - **Validation:** `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ExternalApiKey*/*" --minimum-expected-tests 1 --no-progress`; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ExternalApiKey*/*" --minimum-expected-tests 1 --no-progress`; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet --no-restore -maxcpucount:1`; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/ApiContractInventory_Generate_WritesMarkdownToDocs" --minimum-expected-tests 1 --no-progress`; `dotnet msbuild Explore.Blazor.Client/Explore.Blazor.Client.csproj /t:GenerateApiClient /p:Configuration=Release /p:Restore=false /m:1 /v:minimal`; `git diff --check -- <touched files>`.
  - **Evidence 2026-07-04:** Implemented. `ExternalApiKeyController` is still authenticated, but all actions now advertise no-store response metadata and typed 401/403 ProblemDetails; list/detail/usage-report reads no longer use `[OutputCache]` and use the `Authenticated` limiter; create/update/delete use the `Write` limiter; delete maps `false` revoke results to central 404 ProblemDetails. Application authorization failures now fail closed through `AuthorizationException` for unauthorized owner create/report access, while not-owned details/revokes remain 404 to avoid key existence disclosure. Platform-scoped update-name uniqueness uses `ExistsByOwnerAndNameIgnoringTenantFilter`, and changed repository/authentication/validator/handler calls propagate cancellation tokens. Focused API/Application tests cover metadata, 403 create/report denials, 404 non-owned delete behavior, and platform-name uniqueness bypass. API inventory generation and NSwag client generation were rerun from current source.
  - **Verification 2026-07-04:** `Explore.API` Release build passed (7 projects, 0 errors, existing package warning backlog). Focused ExternalApiKey API lane passed 15/15; focused ExternalApiKey Application lane passed 21/21; API contract inventory generation passed 1/1; NSwag `GenerateApiClient` passed and patched generated void returns; `git diff --check` passed for the R-041 source, tests, generated artifacts, changelog, and active workstream docs.
  - **Effort:** M.
  - **Dependencies:** 1.2, 2.1, 2.3, 2.4.

## Phase 2: API Contract, Error Catalog, OpenAPI, And Operational Health

- [x] **2.1 Reconfirm current ProblemDetails migration state.**
  - **Files:** `Explore.API/ExceptionHandling/**`, controllers, `api-error-catalog.md`, risk register.
  - **Acceptance:** Remaining ad hoc `BadRequest`, `Forbid`, `Unauthorized`, `Problem`, and raw command-envelope paths are listed with owner tasks or confirmed absent.
  - **Evidence 2026-07-04:** Context7 official ASP.NET Core docs confirmed central RFC 7807 ProblemDetails and explicit `ProblemDetails` response metadata as the right direction; Tavily returned OWASP error-handling guidance to avoid leaking internal diagnostic detail. CodeGraph showed the only remaining direct helper in `EventSessionLanguageController.Update`: `ValidationProblem(ModelState)` for a missing/invalid `If-Match` header. That branch now uses `ToValidationProblem` with an `If-Match` descriptor so the response includes the project standard `code`, `traceId`, and `timestamp` extensions while preserving the field-level error key. Broad `rg` sweeps now find no direct controller calls to `BadRequest`, `Unauthorized`, `Forbid`, `NotFound`, `Conflict`, `Problem`, `StatusCode`, or `ValidationProblem`, no `ModelState.AddModelError` returns, and no `BaseCommandResponse` 4xx response metadata under `Explore.API/Controllers`.
  - **Validation:** `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet --no-restore -maxcpucount:1`; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventSessionLanguageControllerTests/*" --minimum-expected-tests 1 --no-progress`; final `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventSessionLanguageControllerTests/*" --minimum-expected-tests 1 --no-progress` passed 2/2; final `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet -- --no-progress` passed 240 total / 239 succeeded / 1 intentional skip; final `dotnet build --configuration Release --verbosity quiet --no-restore -maxcpucount:1` passed 25 projects / 0 errors / existing warning backlog; `git diff --check`; `rg` sweeps listed above.
  - **Effort:** M.
  - **Dependencies:** Phase 0.

- [x] **2.2 Add behavior-level ProblemDetails tests for representative status families.**
  - **Files:** `Explore.API/ExceptionHandling/ProblemDetailsAuthorizationMiddlewareResultHandler.cs`, `Explore.API/Extensions/AuthenticationExtensions.cs`, `Explore.API/Extensions/RateLimitingExtensions.cs`, `Event.API.IntegrationTests/Features/ProblemDetailsContractTests.cs`, `Event.API.IntegrationTests/Fixtures/ExternalApiPhase0WebApplicationFactory.cs`.
  - **Acceptance:** 400 validation, 401 authentication required, 403 forbidden, 404 not found, 409 conflict/concurrency/duplicate, 429 rate-limited, and 500 production-safe examples have deterministic shapes.
  - **Partial evidence 2026-07-04:** `EventSessionLanguageControllerTests.Update_WhenIfMatchIsMissing_ReturnsValidationProblemDetails` now asserts the central validation contract for an endpoint-owned precondition failure: HTTP 400, title `Program validation failed`, `code=validation_failed`, standard trace/timestamp extensions through `ProblemDetailsAssertions`, and `errors.If-Match`.
  - **Evidence 2026-07-04:** Context7 official ASP.NET Core docs confirmed `IProblemDetailsService` for RFC 7807 responses and `IAuthorizationMiddlewareResultHandler` as the supported challenge/forbid customization hook; Tavily returned OWASP Improper Error Handling / Error Handling Cheat Sheet guidance to keep client errors generic and avoid stack traces, SQL/server paths, and raw internal diagnostics. `ProblemDetailsAuthorizationMiddlewareResultHandler` now preserves the default authentication scheme challenge/forbid behavior, then writes central `authentication_required` or `forbidden` ProblemDetails bodies when authorization middleware rejects a request. Rate-limit rejection now emits `code=rate_limited` in production and in the test-only phase factory. `ProblemDetailsContractTests` covers deterministic 401, 403, and 429 examples in addition to the existing validation/malformed-body 400, handler 404, concurrency 409, quota 422, and production-safe 500 examples.
  - **Validation:** `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet --no-restore -maxcpucount:1`; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ProblemDetailsContractTests/RateLimiter_WhenGlobalLimitIsExceeded_ReturnsProblemDetails429" --minimum-expected-tests 1 --no-progress`; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ProblemDetailsContractTests/*" --minimum-expected-tests 1 --no-progress` passed 20/20.
  - **Effort:** M.
  - **Dependencies:** 2.1.

- [x] **2.3 Preserve route-name and HAL route guardrails.**
  - **Files:** `RouteNames`, HAL policies, route-name tests.
  - **Acceptance:** Every named endpoint maps to a constant and every constant resolves to exactly one endpoint after current branch changes.
  - **Evidence 2026-07-04:** Context7 official ASP.NET Core docs confirmed endpoint names are globally unique, case-sensitive URL-generation identifiers and feed OpenAPI operation IDs. Tavily search/extract against Microsoft Learn was attempted but unavailable because the configured Tavily quota was exhausted, so this slice relied on official Context7 docs plus repository tests. `Event.API.IntegrationTests/Features/RouteNameCoverageTests.cs` was re-read and already enforces both guardrail directions: every `RouteNames` constant resolves to exactly one registered `RouteEndpoint`, and every endpoint `RouteNameMetadata` value appears in `RouteNames`.
  - **Validation:** `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-restore -maxcpucount:1` passed with 0 errors and existing warning backlog; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/RouteNameCoverageTests/*" --minimum-expected-tests 1 --no-progress` passed 3/3.
  - **Effort:** S-M.
  - **Dependencies:** Phase 0.

- [ ] **2.4 Regenerate OpenAPI/client artifacts only after source contract is stable.**
  - **Files:** `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API_CHANGELOG.md`.
  - **Acceptance:** Regenerated artifacts match source; breaking changes are documented; no generated artifact is hand-edited.
  - **Evidence 2026-07-04:** R-040 changed public DTO/request schemas and endpoint descriptions, so the documented OpenAPI inventory and NSwag client generation workflow was run after the source change. `docs/API_CHANGELOG.md` records the breaking token-session schema hardening.
  - **Validation:** documented API build/client generation workflow plus contract tests.
  - **Effort:** M.
  - **Dependencies:** 2.1, 2.3 and any source contract slice.

- [x] **2.5 Audit health endpoint payload safety.**
  - **Files:** `Explore.ServiceDefaults/Extensions.cs`, `Explore.ServiceDefaults/HealthChecks/HealthCheckResponseWriter.cs`, `Event.API.IntegrationTests/Features/HealthCheckResponseWriterTests.cs`, `Event.API.IntegrationTests/Event.API.IntegrationTests.csproj`.
  - **Acceptance:** `/health` and `/alive` responses no longer serialize raw `Exception.Message`; shared writer redacts suspicious descriptions and sensitive data keys/values while preserving bounded booleans/status/failure-code fields.
  - **Validation:** `dotnet build Explore.ServiceDefaults/Explore.ServiceDefaults.csproj --configuration Release --verbosity quiet`; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/HealthCheckResponseWriterTests/*" --minimum-expected-tests 1`.
  - **Effort:** M.
  - **Dependencies:** Phase 0.

- [x] **2.6 Reconcile health path documentation.**
  - **Files:** `docs/MCP_DEBUGGING.md`, `docs/CONFIGURATION.md`.
  - **Acceptance:** Operator docs consistently state project paths: `/health` readiness, `/alive` liveness, `/metrics` Prometheus.
  - **Validation:** `rg -n "/health/ready|/health/live" docs` returns no product-doc matches.
  - **Effort:** S.
  - **Dependencies:** 2.5 can run before or after.

## Phase 3: Controller Decomposition Behind Stable Contracts

- [ ] **3.1 Select one controller/resource family for decomposition.**
  - **Files:** refreshed inventory, chosen controller, risk register.
  - **Acceptance:** Decision states why the split reduces complexity and which routes/contracts remain stable or intentionally break.
  - **Validation:** plan/context update.
  - **Effort:** S.
  - **Dependencies:** Phase 2 contract guardrails for that family.

- [ ] **3.2 Add characterization tests before splitting.**
  - **Files:** `Event.API.IntegrationTests`.
  - **Acceptance:** Current behavior for route names, status codes, HAL links, ProblemDetails, auth, and OpenAPI metadata is captured.
  - **Validation:** focused API tests pass/fail for the right reason.
  - **Effort:** M.
  - **Dependencies:** 3.1.

- [ ] **3.3 Split by resource/use-case cohesion.**
  - **Files:** chosen controller plus API-local mapper/services if needed.
  - **Acceptance:** Controllers dispatch MediatR, assemble HAL, map results; no repository injection or business orchestration.
  - **Validation:** API tests, architecture tests, OpenAPI diff review.
  - **Effort:** M-L.
  - **Dependencies:** 3.2.

- [ ] **3.4 Update API docs/changelog for any route/contract changes.**
  - **Files:** `docs/API.md`, `docs/API_CHANGELOG.md`, generated artifacts if required.
  - **Acceptance:** Docs match implemented route/contract behavior.
  - **Validation:** docs readback and contract tests.
  - **Effort:** S-M.
  - **Dependencies:** 3.3.

## Phase 4: Application/CQRS Use-Case Refactor

- [ ] **4.1 Select one oversized handler or query-envelope family.**
  - **Files:** refreshed risk register and chosen Application feature.
  - **Acceptance:** One slice is selected with tests, dependencies, and rollback/failure handling.
  - **Validation:** context update.
  - **Effort:** S.
  - **Dependencies:** Phase 1/2 where the endpoint is security/contract-sensitive.

- [ ] **4.2 Add characterization tests before refactor.**
  - **Files:** `Event.Application.UnitTests`, API tests if external response changes.
  - **Acceptance:** Existing behavior is covered before extraction or response-shape change.
  - **Validation:** targeted tests fail/pass as expected.
  - **Effort:** M.
  - **Dependencies:** 4.1.

- [ ] **4.3 Decompose handlers into narrow collaborators.**
  - **Files:** chosen handler, new Application services/collaborators, DI registration.
  - **Acceptance:** Handler coordinates validation, authorization context, transaction, collaborators, cache/audit/idempotency/concurrency, and response composition. Collaborators do not bypass MediatR behaviors or independently save inside the unit of work unless explicitly designed.
  - **Validation:** Application unit tests and architecture tests.
  - **Effort:** L.
  - **Dependencies:** 4.2.

- [ ] **4.4 Normalize query response contracts.**
  - **Files:** selected query requests/handlers/controllers/client contracts.
  - **Acceptance:** Queries return DTOs, nullable DTOs, `IReadOnlyList<TDto>`, or `PaginatedResult<TDto>`; command envelopes are removed from read data paths.
  - **Validation:** Application unit tests, API contract tests, OpenAPI/client regeneration when public contract changes.
  - **Effort:** M-L.
  - **Dependencies:** 4.1.

- [ ] **4.5 Add endpoint-specific idempotency, concurrency, and audit behavior.**
  - **Files:** chosen command handlers, middleware/config where applicable, audit/outbox services, tests.
  - **Acceptance:** Each high-risk write has an explicit posture: idempotent replay, duplicate conflict, optimistic concurrency, audit event, or documented deferral.
  - **Validation:** Application/API behavior tests; not architecture-only.
  - **Effort:** L.
  - **Dependencies:** 4.2.

## Phase 5: Persistence, Query Shape, Pagination, Indexes, And Reliability

- [ ] **5.1 Select persistence hotspots from evidence.**
  - **Files:** refreshed risk register, repositories/specs/configs/tests.
  - **Acceptance:** One repository/query/index/transaction risk is selected with expected behavior and test lane.
  - **Validation:** context update.
  - **Effort:** S.
  - **Dependencies:** Phase 0.

- [ ] **5.2 Separate read-only and mutation loading semantics.**
  - **Files:** selected repository contracts/implementations.
  - **Acceptance:** Read-only paths use `AsNoTracking`; mutation paths keep tracked loads where needed. No global generic-repository change without tests.
  - **Validation:** persistence tests and Application tests for update flows.
  - **Effort:** M.
  - **Dependencies:** 5.1.

- [ ] **5.3 Remove DTO-shaped repository coupling where selected.**
  - **Files:** e.g. `IEventAggregateViewRepository` or another verified target.
  - **Acceptance:** Persistence receives entities/specifications/query objects/read-model ports, not Application DTO filters.
  - **Validation:** architecture tests and repository/Application tests.
  - **Effort:** M.
  - **Dependencies:** 5.1.

- [ ] **5.4 Quarantine hard delete behavior.**
  - **Files:** `IGenericRepository`, concrete callers, admin lifecycle services/tests.
  - **Acceptance:** Production hard delete requires explicit admin/lifecycle path with authorization, audit, and tests, or remains unavailable.
  - **Validation:** architecture tests plus API/Application behavior tests for callers.
  - **Effort:** M-L.
  - **Dependencies:** 5.1.

- [ ] **5.5 Add cursor/keyset pagination only to selected high-volume endpoints.**
  - **Files:** selected API/Application/Persistence contracts.
  - **Acceptance:** Cursor format is opaque/versioned, bound to tenant/filter/sort where needed, stable under insertion/deletion, documented, and indexed.
  - **Validation:** API + persistence pagination tests; OpenAPI/client regeneration.
  - **Effort:** L.
  - **Dependencies:** 5.1 and contract approval.

- [ ] **5.6 Review indexes/migrations for selected query paths.**
  - **Files:** EF configurations, migrations, model assertions.
  - **Acceptance:** Indexes match query predicates/order; migration has rollback/reset notes for self-hosters; no partitioning unless ADR activation gates are met.
  - **Validation:** persistence model/index assertions and migration tests.
  - **Effort:** M-L.
  - **Dependencies:** 5.1.

- [ ] **5.7 Harden transaction/retry/outbox boundaries for selected background state transitions.**
  - **Files:** outbox/TickerQ/RabbitMQ/email dispatch selected services.
  - **Acceptance:** Manual transactions use `IUnitOfWork` or EF execution strategy; durable side effects are idempotent; ambiguous provider outcomes are recorded safely.
  - **Validation:** Application/Infrastructure/Persistence tests as appropriate.
  - **Effort:** L.
  - **Dependencies:** 5.1.

## Phase 6: Final Guardrails, Documentation, And Release Evidence

- [ ] **6.1 Resolve or explicitly defer all open high/critical risk rows.**
  - **Files:** `backend-contract-risk-register.md`.
  - **Acceptance:** Each row has status, owner, mitigation/detection, and next action or defer rationale.
  - **Validation:** readback.
  - **Effort:** M.
  - **Dependencies:** Phases 1-5 as applicable.

- [ ] **6.2 Convert temporary skips/failures into hard gates or explicit deferrals.**
  - **Files:** test projects and risk register.
  - **Acceptance:** No hidden permanent skips; every skip uses `Category:` and `Removal:`.
  - **Validation:** `Event.Architecture.Tests` skip governance.
  - **Effort:** M.
  - **Dependencies:** implementation slices.

- [ ] **6.3 Update product/operator docs.**
  - **Files:** `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/OPERATIONS.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/MULTI_TENANCY.md`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md` as touched.
  - **Acceptance:** Docs describe actual behavior, config, failure modes, upgrade/recovery notes, and breaking changes.
  - **Validation:** docs readback and architecture doc-quality tests where applicable.
  - **Effort:** M.
  - **Dependencies:** source slices complete.

- [ ] **6.4 Run final targeted verification.**
  - **Files:** context/tasks update with evidence.
  - **Acceptance:** Build and intent-derived per-project test commands are recorded, or blockers are documented with root cause and next recovery action.
  - **Validation:** `dotnet build --configuration Release --verbosity quiet` plus project-level tests required by touched slices.
  - **Effort:** M-L.
  - **Dependencies:** 6.1-6.3.

## Verification Checklist

- [x] LSP diagnostics clean for modified source files when source is touched.
- [x] `dotnet build --configuration Release --verbosity quiet` passes before implementation handoff, unless a pre-existing blocker is documented.
- [ ] Intent minimum test projects pass individually with `dotnet test --project ... --configuration Release --verbosity quiet`; current slice passed `Event.Application.UnitTests`, focused `Event.API.IntegrationTests` onboarding coverage, `Explore.Blazor.IntegrationTests`, `Explore.Blazor.Client.Tests`, and `Event.Architecture.Tests`, but the full API integration suite still has the known unrelated `StorageObjectHateoasTests` failure cluster from the R-004 audit pass.
- [x] API integration tests cover health behavior touched.
- [x] Application unit tests cover handler orchestration, idempotency, concurrency, audit, and query contracts touched.
- [ ] Persistence integration tests cover tenant filters, bypasses, query/index/migration behavior touched.
- [x] Blazor client tests cover HAL affordance gating touched.
- [x] OpenAPI/client artifacts regenerated through documented workflow when public API contract changes.
- [x] Docs updated for health path behavior touched.
- [x] Dev docs refreshed before handoff.

## Remaining / Deferred Work

- Domain `DataAnnotations`/persistence annotation cleanup is not a Phase 6 catch-all. Create a dedicated slice/workstream unless Phase 0 selects a narrow aggregate with tests.
- PostgreSQL partitioning remains deferred per ADR-009 until activation gates are met.
- New `/health/ready` or `/health/live` aliases are deferred unless the user approves an operational endpoint migration plan.
- Repo-wide repository cancellation/no-tracking changes are deferred unless tied to selected hotspots and tests.
- Broad controller decomposition is deferred until security/HAL/API contract guardrails are green for the target family.
