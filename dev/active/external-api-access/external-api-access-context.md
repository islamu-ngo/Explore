ABOUTME: Resumable context for the external API access planning and Phase 0 implementation work.
ABOUTME: Tracks verified files, architectural decisions, implementation progress, and current verification status.

# External API Access - Context

> **Last Updated:** 2026-03-26

## SESSION PROGRESS (2026-03-09)

### ✅ COMPLETED
- Read required planning context from `CLAUDE.md`, relevant docs, active task docs, and project skills.
- Verified the current API-authoritative tenant contract in `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs` and `docs/DEPLOYMENT_MODES.md`.
- Verified current rate-limiting implementation in `Explore.API/Extensions/RateLimitingExtensions.cs` and pipeline placement in `Explore.API/Program.cs`.
- Verified existing admin-boundary documentation in `docs/ADMIN_HIERARCHY.md`.
- Verified existing authorization and admin-authority code paths in `Explore.Application/Behaviors/AuthorizationBehavior.cs`, `Explore.Application/Authorization/AdminClaimTypes.cs`, `Explore.Application/DTOs/User/AdminAuthorityDto.cs`, and `Explore.Application/Features/Users/Handlers/Queries/GetAdminAuthorityRequestHandler.cs`.
- Verified telemetry foundation in `Explore.Application/Telemetry/BusinessMetrics.cs`.
- Verified that `UserAuthenticationToken` exists, but that it currently models provider-style stored tokens rather than the required external caller API-key contract.
- Drafted the planning package in `dev/active/external-api-access/`.
- Folded in the completed Oracle review and external research on mature SaaS API-key ownership, quotas, one-time secret display, hashed storage, and per-key observability.

### ✅ RECENTLY COMPLETED
- Final background tasks completed. No contradictory verified file references were introduced into the planning package.
- Strengthened the plan with product guidance favoring user keys plus organization automation keys in v1, while keeping room for service-account evolution later.
- Folded in repo-specific verification around forwarded headers, current JWT-only API registration, lack of API policy-scheme dispatch, and current Blazor proxy trust posture.
- Amended the plan to add a Phase 0 pipeline ADR and spike, explicit JWT plus API-key auth-scheme dispatch, split-phase tenant handling, reverse-proxy trust rules, and clustered throttling semantics.
- Started Phase 0 implementation in `Explore.API` and `Event.API.IntegrationTests`.
- Added API auth constants in `Explore.Application/Constants/` for the explicit machine-auth contract.
- Wired `MultiAuth` policy-scheme dispatch in `Explore.API/Program.cs` so `X-API-Key` routes to the API-key handler and other direct callers stay on JWT bearer.
- Updated `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs` to capture API-key tenant hints without trusting them as authority.
- Wired `Explore.API/Middleware/ApiTenantPostAuthenticationMiddleware.cs` after authentication to set tenant from API-key principals and reject mismatches.
- Added hidden Phase 0 probe endpoint in `Explore.API/Controllers/AuthContextProbeController.cs`.
- Added `Event.API.IntegrationTests/Fixtures/ExternalApiPhase0WebApplicationFactory.cs` and `Event.API.IntegrationTests/Features/ExternalApiPhase0IntegrationTests.cs` for seam validation.
- Added `dev/active/external-api-access/phase0-auth-tenant-request-flow-adr.md` to document the implemented request flow.
- Hardened proxy trust in `Explore.API` with `ForwardedHeadersTrust` configuration and `UseForwardedHeaders()` before tenant-sensitive middleware.
- Updated tenant resolution and rate limiting to rely on normalized trusted request host/IP instead of raw `X-Forwarded-*` headers.
- Verified that `dotnet build --configuration Release --verbosity quiet` succeeds after the Phase 0 changes.
- Re-ran `Event.API.IntegrationTests`; the project still shows the pre-existing broad `404 Tenant not resolved` failures already observed before this seam work.
- Targeted Phase 0 seam tests now pass `9/9`, including mixed-credential rejection, probe gating, direct-host custom-domain resolution, and forwarded-header trust option behavior.
- Collected both background explore results for the persisted slice and confirmed the best internal references are `UserAuthenticationToken`, `UserAuthenticationTokenRepository`, and the explicit tenant-filter bypass pattern in `Explore.Persistence/Services/TenantLookupSource.cs`.
- Implemented the first persisted `ExternalApiKey` auth slice across Domain, Application, Persistence, and API layers.
- Added `ExternalApiKey` aggregate plus `ExternalApiKeyOwnerType` and `ExternalApiKeyStatus` enums.
- Added `IExternalApiKeyRepository`, `ExternalApiKeyConfiguration`, `ExternalApiKeyRepository`, `ExploreDbContext` registration, and DI wiring in `PersistenceServicesRegistration`.
- Updated `ApiKeyAuthenticationHandler` to authenticate persisted `keyId.secret` credentials through repository lookup before falling back to the Phase 0 config-backed clients.
- Expanded `ApiKeyHashing` with persisted-key formatting and parsing helpers while keeping the existing hash-matching fallback for Phase 0 tests.
- Generated EF Core migration `Explore.Persistence/Migrations/20260309122122_AddExternalApiKey.cs`.
- Extended `ExternalApiPhase0WebApplicationFactory` with persisted API-key seeding support and added persisted-key integration coverage.
- Verified `dotnet build --configuration Release --verbosity quiet` still succeeds after the persisted slice.
- Verified `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release -- --treenode-filter "/*/*/ExternalApiPhase0IntegrationTests/*" --no-ansi --disable-logo` passes `11/11` after adding the persisted-key tests.
- Completed the first management slice for persisted keys with create, list, and revoke CQRS handlers plus `ExternalApiKeyController` endpoints.
- Moved `ApiKeyHashing` out of `Explore.API` and into `Explore.Application/Services/ApiKeyHashing.cs` so both Application handlers and API auth code can consume the helper without violating Clean Architecture.
- Updated `ApiKeyAuthenticationHandler` and the Phase 0 integration harness to use the Application-owned helper location.
- Added `[ApiVersion("0.1")]` to `Explore.API/Controllers/AuthContextProbeController.cs` so the hidden diagnostics controller matches API controller conventions.
- Verified all touched ExternalApiKey management files and the moved hashing helper are LSP-clean.
- Verified `dotnet build --configuration Release -clp:ErrorsOnly` succeeds after the helper move and management-slice completion.
- Re-verified `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity minimal -- --treenode-filter "/*/*/ExternalApiPhase0IntegrationTests/*" --no-ansi --disable-logo` still passes `11/11`.
- Identified the remaining architecture-test failure as an unrelated analytics naming issue: `SanitizedAnalyticsTrackRequest` and `SanitizedAnalyticsPageViewRequest` in `Explore.Application/Analytics/AnalyticsEventDefinition.cs` ended with `Request` even though they are payload records, not CQRS query requests.
- Renamed those analytics payload records to `SanitizedAnalyticsTrackPayload` and `SanitizedAnalyticsPageViewPayload`, and updated `IAnalyticsGovernanceService` plus `AnalyticsGovernanceService` to match.
- Verified the renamed analytics files are LSP-clean.
- Verified `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity minimal` now passes `36/36`.
- Added `Explore.Application/Authentication/ApiAuthenticationPrincipalExtensions.cs` to centralize API-key tenant, owner, scope, auth-method, and JWT user-id claim parsing.
- Updated `Explore.API/Middleware/ApiTenantPostAuthenticationMiddleware.cs` and `Explore.API/Controllers/AuthContextProbeController.cs` to consume the shared principal helper instead of duplicating claim parsing.
- Verified the principal-helper files are LSP-clean.
- Re-verified `dotnet build --configuration Release -clp:ErrorsOnly` succeeds after the principal-helper refactor.
- Re-verified `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity minimal -- --treenode-filter "/*/*/ExternalApiPhase0IntegrationTests/*" --no-ansi --disable-logo` still passes `11/11`.
- Added the next unblocked management slice: `UpdateExternalApiKeyPolicy` CQRS flow and `PUT /api/externalapikey/{id}` for editing name, scopes, and expiry while keeping owner and tenant binding immutable.
- Added `UpdateExternalApiKeyPolicyDto`, its validator, command, and handler, and wired the update endpoint through `Explore.API/Controllers/ExternalApiKeyController.cs`.
- Added a dedicated single-tenant authenticated integration fixture for tenant-scoped management endpoint tests so API-key controller tests do not fail on unrelated multi-tenant resolution setup.
- Added `Event.API.IntegrationTests/Features/ExternalApiKeyIntegrationTests.cs` covering owner update success and cross-user not-found behavior.
- Verified all new policy-update files and test fixtures are LSP-clean.
- Verified `dotnet build --configuration Release -clp:ErrorsOnly` succeeds after the policy-update slice.
- Verified `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity minimal` passes `36/36`.
- Verified `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity minimal -- --treenode-filter "/*/*/ExternalApiKeyIntegrationTests/*" --no-ansi --disable-logo` passes `2/2`.
- Re-verified `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity minimal -- --treenode-filter "/*/*/ExternalApiPhase0IntegrationTests/*" --no-ansi --disable-logo` still passes `11/11`.
- Added the single-key detail slice: `GetExternalApiKeyDetailsRequest`, `GetExternalApiKeyDetailsRequestHandler`, and `GET /api/externalapikey/{id}` using the same owner-visibility rules as update and revoke.
- Reused `ExternalApiKeyListDto` for the detail endpoint because it already exposes only safe metadata and excludes secret material.
- Expanded `Event.API.IntegrationTests/Features/ExternalApiKeyIntegrationTests.cs` with owner success and cross-user not-found coverage for the detail endpoint.
- Verified all new detail-query files are LSP-clean.
- Re-verified `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity minimal -- --treenode-filter "/*/*/ExternalApiKeyIntegrationTests/*" --no-ansi --disable-logo` now passes `4/4`.
- Added a throttled usage-metadata touch path for persisted API keys by extending `IExternalApiKeyRepository` and `ExternalApiKeyRepository` with `TouchUsageMetadata(...)`.
- Wired successful persisted-key authentication in `Explore.API/Authentication/ApiKeyAuthenticationHandler.cs` to update `LastUsedAt` and `LastUsedIp` no more than once per five-minute window.
- Added an EF InMemory-safe fallback for `TouchUsageMetadata(...)` so Phase 0 seam tests keep working while the relational path still uses `ExecuteUpdateAsync`.
- Expanded `Event.API.IntegrationTests/Features/ExternalApiPhase0IntegrationTests.cs` with persisted-key usage-metadata verification.
- Verified all usage-metadata files are LSP-clean.
- Re-verified `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ExternalApiPhase0IntegrationTests/*" --no-ansi --disable-logo` now passes `12/12`.
- Re-verified `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ExternalApiKeyIntegrationTests/*" --no-ansi --disable-logo` still passes `4/4`.
- Implemented the next unblocked abuse-control slice by partitioning global API rate limiting per authenticated API-key id instead of collapsing all machine callers into shared IP buckets.
- Updated `Explore.Application/Authentication/ApiAuthenticationPrincipalExtensions.cs` with a direct `GetApiKeyId()` helper so rate limiting can key off the claim contract without requiring full principal-context parsing.
- Updated `Explore.API/Extensions/RateLimitingExtensions.cs` so API-key requests use their own token-bucket partition while JWT and anonymous traffic keep the existing IP- and identity-based behavior.
- Extended `Event.API.IntegrationTests/Fixtures/ExternalApiPhase0WebApplicationFactory.cs` with opt-in testing overrides for real rate limiting and matching `429` rejection behavior.
- Expanded `Event.API.IntegrationTests/Features/ExternalApiPhase0IntegrationTests.cs` with per-key isolation coverage proving one persisted key can be throttled without blocking another key from the same test client.
- Verified all new rate-limiting files are LSP-clean.
- Re-verified `dotnet build --configuration Release -clp:ErrorsOnly` succeeds after the per-key limiter slice.
- Re-verified `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ExternalApiPhase0IntegrationTests/*" --no-ansi --disable-logo` now passes `13/13`.
- Re-verified `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ExternalApiKeyIntegrationTests/*" --no-ansi --disable-logo` still passes `4/4`.
- Added the next observability slice for persisted external API keys without touching rotation or usage-rollup design.
- Extended `Explore.Application/Telemetry/BusinessMetrics.cs` with external API-key counters for creation, revocation, authentication outcomes, and throttling.
- Updated `CreateExternalApiKeyCommandHandler`, `RevokeExternalApiKeyCommandHandler`, and `UpdateExternalApiKeyPolicyCommandHandler` to emit structured audit-style logs and to stamp explicit mutation timestamps on persisted keys.
- Updated `Explore.API/Authentication/ApiKeyAuthenticationHandler.cs` to record success, invalid, inactive, expired, tenant-mismatch, and empty-header authentication outcomes without logging raw key material.
- Updated `Explore.API/Middleware/ApiTenantPostAuthenticationMiddleware.cs` to log and metric-tag API-key tenant-mismatch attempts as fail-closed audit events.
- Updated `Explore.API/Extensions/RateLimitingExtensions.cs` to record API-key throttle events and emit a structured warning when a key is rejected by the limiter.
- Added `Event.Application.UnitTests/Features/ExternalApiKeys/Commands/ExternalApiKeyObservabilityTests.cs` to verify the new creation and revocation metrics are emitted.
- Expanded `Event.API.IntegrationTests/Features/ExternalApiKeyIntegrationTests.cs` so the management slice now verifies timestamp mutation on update and revoke, and revoke success for owner-visible keys.
- Verified all observability-slice files are LSP-clean.
- Re-verified `dotnet build --configuration Release -clp:ErrorsOnly` succeeds after the observability slice.
- Verified `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet` passes `393/393`.
- Re-verified `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ExternalApiKeyIntegrationTests/*" --no-ansi --disable-logo` now passes `5/5`.
- Re-verified `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ExternalApiPhase0IntegrationTests/*" --no-ansi --disable-logo` still passes `13/13`.

### ⚠️ BLOCKERS
- No coding blocker remains inside the Phase 0 seam itself.
- Full `Event.API.IntegrationTests` still contains the known baseline `404 Tenant not resolved` failures, which makes project-level green verification noisy.
- Rotation, usage rollups beyond `LastUsedAt`/`LastUsedIp`, and admin-visibility flows for persisted API keys are not implemented yet.

---

## Key Decisions Captured So Far

1. **Direct API consumers are supported.** This feature is not optional; the user explicitly wants non-BFF consumers.
2. **There is no single universal tenant contract.** The contract differs by caller type.
3. **Single-tenant mode stays abstracted.** Direct callers should not need tenant-selection data in `SingleTenant` mode.
4. **API keys should be tenant-bound.** In `MultiTenant` mode, API-key callers should derive tenant from the key binding rather than from a raw caller-supplied slug.
5. **Direct JWT callers are still allowed.** In `MultiTenant` mode they need explicit tenant context by trusted host or `X-Tenant-Slug`, followed by membership or admin validation.
6. **Instance-admin operations stay platform-scoped.** They must not become a backdoor into tenant business data or tenant API tokens.
7. **Do not reuse `UserAuthenticationToken` blindly.** Reuse the CQRS and repository pattern if helpful, but the aggregate itself is oriented toward stored provider tokens (`AccessToken`, `RefreshToken`, `IdToken`, `PdsHost`, `DpopKey`).
8. **Middleware order is the hardest design seam.** `ApiTenantResolutionMiddleware` currently executes before authentication in `Explore.API/Program.cs`, so API-key-based tenant discovery needs a deliberate pre-auth or split-phase design.
9. **Auth dispatch should be explicit.** The API plan now assumes JWT bearer plus a custom API-key `AuthenticationHandler<TOptions>` behind a policy-scheme dispatcher instead of open-ended “equivalent pipeline component” wording.
10. **Proxy trust is now part of the feature design.** Host-derived tenanting for direct API consumers must define trusted proxies, forwarded-host handling, and mismatch semantics rather than treating them as deployment trivia.
11. **Persisted auth uses `keyId.secret`.** The handler now parses a stable public key id from the incoming raw key, performs a single-row repository lookup, and verifies only the secret segment hash.
12. **Tenant-filter bypass is limited to auth lookup.** Persisted key authentication uses an explicit repository method that ignores only the named tenant filter for pre-tenant auth resolution.
13. **Five owner types, not two.** `ExternalApiKeyOwnerType` expanded to User (1), Organization (2), Group (3), Tenant (4), InstanceAdmin (5). Each maps OwnerId to a different entity. TenantId is nullable for InstanceAdmin platform-scoped keys.
14. **Expanded OwnerType enum over Actor FK.** Tenant is not an Actor entity and InstanceAdmin is a role on User, not a separate entity. The existing OwnerId (Guid) pattern handles all types without polymorphic FK complexity.
15. **Group admin authority gap.** `AdminContext` currently has `IsInstanceAdminAsync`, `IsTenantAdminAsync`, and `IsOrganizationAdminAsync` but no `IsGroupAdminAsync`. This must be added before group key authorization works.
16. **Scope ceiling hierarchy.** InstanceAdmin > Tenant > Organization ~ Group > User. A key's effective permissions are the intersection of its scope set and the creator's authority level.

---

## Verified Files And Why They Matter

### API Layer

`Explore.API/Program.cs`
- Confirms middleware order: routing -> tenant resolution -> request timeouts -> authentication -> rate limiter -> authorization -> output cache.
- Confirms current CORS policies and JWT setup.
- Confirms the API currently registers JWT bearer directly rather than using a policy-scheme dispatcher.

`Explore.API/Middleware/ApiTenantResolutionMiddleware.cs`
- Confirms current authoritative tenant resolution rules and fail-closed `404` behavior.

`Explore.API/Extensions/RateLimitingExtensions.cs`
- Confirms current rate-limit partitions are IP- and user-based only.

`Explore.API/Controllers/UserAuthenticationTokenController.cs`
- Confirms an existing token-management controller slice exists and can serve as a pattern reference.

`Explore.API/Authentication/ApiKeyAuthenticationHandler.cs`
- Now authenticates both persisted `ExternalApiKey` records and the original config-backed Phase 0 clients.

### Application Layer

`Explore.Application/Behaviors/AuthorizationBehavior.cs`
- Existing central enforcement boundary for request authorization.

`Explore.Application/Authorization/AdminClaimTypes.cs`
- Confirms current admin claim taxonomy.

`Explore.Application/DTOs/User/AdminAuthorityDto.cs`
- Confirms current authority DTO shape: instance, tenant, and organization scopes.

`Explore.Application/Features/Users/Handlers/Queries/GetAdminAuthorityRequestHandler.cs`
- Confirms admin authority resolution path from the application layer.

`Explore.Application/Telemetry/BusinessMetrics.cs`
- Confirms dimensional metric support with `tenant_id` tagging that can be extended for API-key reporting.
- Now includes external API-key lifecycle, authentication-outcome, and throttle counters with bounded dimensions.

`Explore.Application/Contracts/Persistence/IUserAuthenticationTokenRepository.cs`
- Pattern reference for repository contract shape.

`Explore.Application/Features/UserAuthenticationTokens/Handlers/Commands/CreateUserAuthenticationTokenCommandHandler.cs`
- Pattern reference for manual validator instantiation and tenant-context-driven persistence.

`Explore.Application/Features/UserAuthenticationTokens/Handlers/Queries/GetUserAuthenticationTokenListRequestHandler.cs`
- Pattern reference for query handler structure.

`Explore.Application/Contracts/Persistence/IExternalApiKeyRepository.cs`
- New persisted auth lookup contract for storage-backed API-key resolution.

`Explore.Application/Authentication/ApiAuthenticationPrincipalExtensions.cs`
- Shared reader for the API-key principal contract and standard authenticated user-id fallback.

`Event.Application.UnitTests/Features/ExternalApiKeys/Commands/ExternalApiKeyObservabilityTests.cs`
- Verifies the new external API-key creation and revocation counters are emitted through `BusinessMetrics`.

`Explore.Application/DTOs/ExternalApiKey/UpdateExternalApiKeyPolicyDto.cs`
- New DTO for editing mutable external API key policy fields without changing ownership.

`Explore.Application/Features/ExternalApiKeys/Handlers/Commands/UpdateExternalApiKeyPolicyCommandHandler.cs`
- New handler for owner-scoped policy maintenance on persisted keys.

`Explore.Application/Features/ExternalApiKeys/Requests/Queries/GetExternalApiKeyDetailsRequest.cs`
- New query contract for single-key metadata retrieval.

`Explore.Application/Features/ExternalApiKeys/Handlers/Queries/GetExternalApiKeyDetailsRequestHandler.cs`
- New detail handler that returns null for missing or unauthorized keys.

`Explore.API/Authentication/ApiKeyAuthenticationHandler.cs`
- Now records throttled persisted-key usage metadata on successful authentication.

### Domain And Persistence

`Explore.Domain/ExternalApiKey.cs`
- New aggregate for persisted machine credentials with tenant binding, owner model, status, expiry, and last-used metadata.

`Explore.Domain/Enums/ExternalApiKeyOwnerType.cs`
- New explicit owner taxonomy for user- versus organization-bound keys.

`Explore.Domain/Enums/ExternalApiKeyStatus.cs`
- New explicit persisted lifecycle state for active versus revoked keys.

`Explore.Domain/UserAuthenticationToken.cs`
- Important because it looks like an API-key entity at first glance but is actually provider-token storage.

`Explore.Persistence/Configurations/Entities/UserAuthenticationTokenConfiguration.cs`
- Pattern reference for EF configuration.

`Explore.Persistence/Repositories/UserAuthenticationTokenRepository.cs`
- Pattern reference for repository implementation.

`Explore.Persistence/Configurations/Entities/ExternalApiKeyConfiguration.cs`
- New EF configuration for persisted API-key storage, indexes, and tenant FK behavior.

`Explore.Persistence/Repositories/ExternalApiKeyRepository.cs`
- New repository implementing the pre-tenant auth lookup path via `GetByKeyIdForAuthentication`.

`Explore.Persistence/QueryFilters/QueryFilterExtensions.cs`
- Confirms the approved selective tenant-filter bypass pattern used by the new auth lookup method.

`Explore.Persistence/Migrations/20260309122122_AddExternalApiKey.cs`
- New migration adding the `external_api_keys` table and supporting indexes.

### Blazor And Proxy Handling

`Explore.Blazor/Extensions/MiddlewareExtensions.cs`
- Confirms Blazor currently restores forwarded headers and clears known proxies/networks, effectively trusting all proxies in that host.

`Explore.Blazor/Services/CircuitAccessTokenService.cs`
- Confirms the BFF forwards `X-Forwarded-Host` downstream, which matters for current host-derived tenant resolution.

### Documentation

`docs/DEPLOYMENT_MODES.md`
- Source of truth for current single-tenant and multi-tenant runtime behavior.

`docs/ADMIN_HIERARCHY.md`
- Source of truth for the hard boundary that instance admins cannot access tenant business data or tenant API tokens.

`docs/API.md`
- Source of truth for current API stack, HATEOAS, middleware, and rate-limit expectations.

`docs/SECURITY.md`
- Source of truth for JWT/BFF security conventions.

---

## Important Discoveries

### Existing `UserAuthenticationToken` Slice

This slice exists and should not be described as missing:

- `Explore.Domain/UserAuthenticationToken.cs`
- `Explore.Persistence/Configurations/Entities/UserAuthenticationTokenConfiguration.cs`
- `Explore.Application/Contracts/Persistence/IUserAuthenticationTokenRepository.cs`
- `Explore.Persistence/Repositories/UserAuthenticationTokenRepository.cs`
- `Explore.Application/Features/UserAuthenticationTokens/Handlers/Commands/CreateUserAuthenticationTokenCommandHandler.cs`
- `Explore.Application/Features/UserAuthenticationTokens/Handlers/Queries/GetUserAuthenticationTokenListRequestHandler.cs`
- `Explore.API/Controllers/UserAuthenticationTokenController.cs`

But this slice stores external-provider auth artifacts, not the machine-consumer API keys requested by the user.

### Resolved Direct-Access Risks

The following risks identified during planning have been addressed:

- ✅ **Multi-auth dispatch**: `MultiAuth` policy-scheme dispatches `X-API-Key` to API-key handler, other callers to JWT bearer.
- ✅ **Per-key rate limiting**: Global limiter partitions API-key callers by `api-key:{apiKeyId}` instead of sharing IP buckets.
- ✅ **Usage rollups**: `ExternalApiKeyQuota` entity tracks per-period `CreditsUsed` and `RequestCount`; `GetUsageByTenant` and `GetUsagePlatformWide` repository methods provide reporting.
- ✅ **Metrics coverage**: 6 counters (created, revoked, policy_updated, rotated, authentication_attempts, throttled) all with `tenant_id` (nullable for InstanceAdmin) and `owner_type` dimensions.
- ✅ **Proxy trust**: `ForwardedHeadersTrust` configuration and `UseForwardedHeaders()` applied before tenant-sensitive middleware.
- ✅ **Centralized principal parsing**: `ApiAuthenticationPrincipalExtensions.TryGetApiKeyPrincipalContext()` provides single source of truth.
- ✅ **InstanceAdmin tenant bypass**: `ApiTenantPostAuthenticationMiddleware` allows InstanceAdmin keys without tenant context.

### Remaining Open Risks

- General authenticated API integration fixtures still assume multi-tenant resolution; tenant-scoped management tests need a single-tenant test host or explicit setup.
- Rotation overlap semantics not yet decided for v1 (rotation counter is stubbed but the feature is not implemented).

---

## Clustered Deployment Semantics

### Rate Limiting: Node-Local

ASP.NET Core `AddRateLimiter` stores rate-limit state **in-process memory**. In a multi-node deployment:

- Each node enforces limits independently.
- Effective cluster-wide limit = `configured_limit × node_count`.
- A single API key hitting different nodes can consume up to `N × limit` requests before being throttled on any one node.

**Self-hoster guidance**: For single-node deployments (most self-hosted scenarios), rate limiting works as documented. For multi-node clusters requiring strict enforcement, add a shared backing store (e.g., Redis via `AspNetCoreRateLimit` or a custom `IRateLimiterPolicy` backed by distributed cache). This is a standard ASP.NET Core extension point and can be configured without code changes to the rate-limit policy definitions.

**Partition keys** (per-key, not per-owner-type):
- API-key callers: `api-key:{apiKeyId}` (token bucket)
- JWT/anonymous callers: IP-based (token bucket)
- Authenticated endpoints: user identity or `api-key:{apiKeyId}` (sliding window)
- Write endpoints: same key as authenticated (fixed window)

### Quota Credits: Cluster-Safe

`ExternalApiKeyQuota` credit consumption uses **PostgreSQL atomic SQL operations**:

- `INSERT ... ON CONFLICT` for lazy period provisioning (race-safe)
- `UPDATE ... WHERE credits_used + amount <= credit_limit + rollover_credits` for atomic credit consumption (row-level locking)
- `RequestCount` increment is part of the same atomic UPDATE

All nodes share the same database, so credit enforcement is globally consistent regardless of cluster size.

### Usage Metadata: Eventually Consistent

`TouchUsageMetadata` (updates `LastUsedAt`/`LastUsedIp`) uses a 5-minute in-memory throttle per key. In a cluster, different nodes may race to update the same key's metadata. This is acceptable: the goal is approximate recency, not exact ordering.

---

## Direct Caller Contract Documentation

### Authentication Flows

External callers authenticate via one of two schemes, dispatched by `MultiAuth` policy:

**1. API Key Authentication** (`X-API-Key` header)
```
X-API-Key: {keyId}.{secret}
```
- Key format: `{keyId}` (short identifier) `.` `{base64Secret}`
- Persisted keys are looked up by `keyId`, then `secret` is verified against `SecretHash` (HMAC-SHA256)
- Produces claims: `explore:api-key:id`, `explore:tenant:id` (absent for InstanceAdmin), `explore:api-key:owner:type`, `explore:api-key:owner:id`, `explore:api-key:scope` (repeated per scope)
- Usable statuses: `Active`, `PendingRotation`

**2. JWT Bearer Authentication** (standard `Authorization: Bearer {token}`)
- Configured via Keycloak OIDC
- Tenant resolved from host or `X-Tenant-Slug` header (BFF-trusted)
- Standard user claims: `sub`, `nameidentifier`, `sid` (fallback order for user ID)

### Tenant Resolution for API-Key Callers

1. API-key auth handler sets `explore:tenant:id` claim from persisted key's `TenantId`
2. `ApiTenantPostAuthenticationMiddleware` uses authenticated tenant from claims
3. If key has `TenantId`: tenant must match request context or → `401`
4. If key has `null TenantId` (InstanceAdmin only): bypasses tenant requirement, produces platform-scoped principal
5. Non-InstanceAdmin keys without resolvable tenant → `401`

### Scope Model

Scopes follow `{resource}:{action}` convention (colon-separated):

| Category | Scopes |
|----------|--------|
| Read | `events:read`, `organizations:read`, `groups:read`, `users:read`, `lookups:read` |
| Write | `events:write`, `organizations:write`, `groups:write`, `users:write`, `registrations:write` |
| Management | `api-keys:manage` |
| Admin | `admin:tenant`, `admin:instance` |

Scope ceilings per owner type:
- **User**: events r/w, users r/w, lookups:read, registrations:write, api-keys:manage
- **Organization**: User scopes + organizations r/w
- **Group**: User scopes + groups r/w
- **Tenant**: All except `admin:instance`
- **InstanceAdmin**: All scopes

### Management Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/ExternalApiKey` | List visible API keys (owner-scoped) |
| `GET` | `/api/ExternalApiKey/{id}` | Get key details (metadata only, no secret) |
| `POST` | `/api/ExternalApiKey` | Create key (secret revealed once in response) |
| `PUT` | `/api/ExternalApiKey/{id}` | Update key policy (name, scopes, expiry) |
| `DELETE` | `/api/ExternalApiKey/{id}` | Revoke key |
| `GET` | `/api/ExternalApiKey/usage-report?from=&to=&tenantId=` | Usage report (admin only) |

All endpoints require `[Authorize]`. The usage-report endpoint requires tenant-admin or instance-admin authority.

---

## Quick Resume

1. Read `dev/active/external-api-access/external-api-access-plan.md` — updated 2026-03-26 with five-owner-type model.
2. **Phases 0–6 COMPLETE**: Auth seam, domain (5 owner types, scope catalog, quota defaults), application (all CQRS, scope-ceiling enforcement, nullable TenantId principal), persistence (IgnoreTenantFilter, usage rollup with RequestCount), API auth/tenant (InstanceAdmin escape), metrics (6 counters, all dimensions), instance-admin reporting endpoint.
3. **Next priorities (Phase 7)**: Blazor admin panels for all 5 owner types — user settings, org admin, group admin, tenant admin, instance admin.
4. **Then (Phase 8)**: Cerbos integration for machine principals, full unit/integration/rate-limit test suite for all 5 owner types, and documentation updates to `docs/API.md`, `docs/SECURITY.md`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/ADMIN_HIERARCHY.md`.
