ABOUTME: Resumable context for the external API access planning and Phase 0 implementation work.
ABOUTME: Tracks verified files, architectural decisions, implementation progress, and current verification status.

# External API Access - Context

> **Last Updated:** 2026-03-08

## SESSION PROGRESS (2026-03-08)

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
- Verified that `dotnet build --configuration Release --verbosity quiet` succeeds after the Phase 0 changes.
- Re-ran `Event.API.IntegrationTests`; the project still shows the pre-existing broad `404 Tenant not resolved` failures already observed before this seam work.

### ⚠️ BLOCKERS
- No coding blocker remains inside the Phase 0 seam itself.
- Full `Event.API.IntegrationTests` still contains the known baseline `404 Tenant not resolved` failures, which makes project-level green verification noisy.
- Oracle review of the implemented seam is running and must be collected before final wrap-up.

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

`Explore.Application/Contracts/Persistence/IUserAuthenticationTokenRepository.cs`
- Pattern reference for repository contract shape.

`Explore.Application/Features/UserAuthenticationTokens/Handlers/Commands/CreateUserAuthenticationTokenCommandHandler.cs`
- Pattern reference for manual validator instantiation and tenant-context-driven persistence.

`Explore.Application/Features/UserAuthenticationTokens/Handlers/Queries/GetUserAuthenticationTokenListRequestHandler.cs`
- Pattern reference for query handler structure.

### Domain And Persistence

`Explore.Domain/UserAuthenticationToken.cs`
- Important because it looks like an API-key entity at first glance but is actually provider-token storage.

`Explore.Persistence/Configurations/Entities/UserAuthenticationTokenConfiguration.cs`
- Pattern reference for EF configuration.

`Explore.Persistence/Repositories/UserAuthenticationTokenRepository.cs`
- Pattern reference for repository implementation.

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
- Current metrics are a good foundation, but there is no verified API-key usage rollup or per-key last-used visibility yet.
- The API has no current policy-scheme dispatch for multi-auth direct callers.
- Host-derived tenanting depends on forwarded-host trust that needs explicit hardening for self-hosted direct API ingress.

---

## Remaining Work Before Implementation Starts

1. Collect the Oracle review result for the Phase 0 seam.
2. Decide whether to keep expanding within Phase 0 or move into Domain/Application work for the real external API-key model.
3. Reconcile the existing broad `Event.API.IntegrationTests` `404` baseline before relying on whole-project API integration runs as a signal.
4. Keep instance-admin reporting metadata-only unless a separate audited emergency-access design is approved.

---

## Quick Resume

1. Read `dev/active/external-api-access/external-api-access-plan.md`.
2. Read `dev/active/external-api-access/phase0-auth-tenant-request-flow-adr.md`.
3. Inspect `Explore.API/Program.cs`, `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs`, and `Explore.API/Middleware/ApiTenantPostAuthenticationMiddleware.cs` for the implemented seam.
4. Inspect `Event.API.IntegrationTests/Fixtures/ExternalApiPhase0WebApplicationFactory.cs` and `Event.API.IntegrationTests/Features/ExternalApiPhase0IntegrationTests.cs` for the current Phase 0 verification harness.
5. Collect the pending Oracle review before finalizing status or moving into the next phase.
