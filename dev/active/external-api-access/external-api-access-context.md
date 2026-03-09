ABOUTME: Resumable context for the external API access planning and Phase 0 implementation work.
ABOUTME: Tracks verified files, architectural decisions, implementation progress, and current verification status.

# External API Access - Context

> **Last Updated:** 2026-03-09

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

### Current Direct-Access Risks

- `X-Tenant-Slug` is acceptable today for the trusted BFF-forwarded flow, but it is not sufficient as the final authority model for arbitrary API-key callers.
- Current rate limiting cannot protect one noisy API key separately from another because partitioning is not keyed by API key.
- Global direct-consumer throttling is now keyed per authenticated API-key id, but aggregated usage rollups and audit-style throttle reporting still do not exist.
- Current metrics now cover create, revoke, auth outcomes, tenant mismatch, and throttle events, and `LastUsedAt`/`LastUsedIp` still update on successful persisted-key auth, but there is still no aggregated API-key usage rollup or reporting surface.
- The API has no current policy-scheme dispatch for multi-auth direct callers.
- Host-derived tenanting depends on forwarded-host trust that needs explicit hardening for self-hosted direct API ingress.
- API-key claim constants already existed, but claim parsing was duplicated until the new shared principal helper centralized the contract.
- General authenticated API integration fixtures in this repo still assume multi-tenant resolution and can 404 before controller logic; tenant-scoped management endpoint tests need a single-tenant test host or explicit tenant-resolution setup.

### Persisted Slice Status

- The first persisted vertical slice is intentionally auth-first, not management-first.
- Current persisted behavior covers: storage model, migration, repository lookup, handler authentication, and integration-test seeding.
- It does **not** yet cover: CQRS management commands, safe one-time secret reveal APIs, revoke or rotate endpoints, usage rollups, or admin visibility.

---

## Remaining Work Before Implementation Starts

1. Decide whether rotation overlap is required in v1 before building rotation handlers and endpoints.
2. Add aggregated usage rollup and reporting storage beyond the new `LastUsedAt`/`LastUsedIp` path.
3. Extend the new API-key metrics and audit-style logging slice with reveal, rotate, and usage-rollup coverage so observability is complete without reading raw tenant secrets.
4. Reconcile the existing broad `Event.API.IntegrationTests` `404` baseline before relying on whole-project API integration runs as a signal.
5. Keep instance-admin reporting metadata-only unless a separate audited emergency-access design is approved.

---

## Quick Resume

1. Read `dev/active/external-api-access/external-api-access-plan.md`.
2. Inspect `Explore.Domain/ExternalApiKey.cs`, `Explore.Application/Contracts/Persistence/IExternalApiKeyRepository.cs`, `Explore.Persistence/Repositories/ExternalApiKeyRepository.cs`, and `Explore.Persistence/Migrations/20260309122122_AddExternalApiKey.cs` for the persisted auth slice.
3. Inspect `Explore.API/Authentication/ApiKeyAuthenticationHandler.cs` and `Explore.API/Authentication/ApiKeyHashing.cs` for the new `keyId.secret` auth path and fallback behavior.
4. Inspect `Event.API.IntegrationTests/Fixtures/ExternalApiPhase0WebApplicationFactory.cs` and `Event.API.IntegrationTests/Features/ExternalApiPhase0IntegrationTests.cs` for the updated `11/11` seam verification harness.
5. Continue with observability follow-through next; core API-key counters and audit-style logs are now in place, but usage rollups, reveal or rotate event coverage, clustered limiter semantics, and metadata-only instance-admin reporting are still open.
