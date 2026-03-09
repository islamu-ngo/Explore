ABOUTME: Strategic plan for external direct API access with tenant-aware API keys and rate limiting.
ABOUTME: Planning only; grounded in verified repo files, current multi-tenancy rules, and direct-consumer contracts.

# External API Access - Implementation Plan

> **Last Updated:** 2026-03-08

## Executive Summary

ISLAMU Event should support direct non-BFF API consumers, but only as a second ingress into the same authoritative API rather than as a separate trust model. The existing API already owns tenant resolution, authorization behavior, rate limiting, HATEOAS responses, and tenant-scoped persistence. The correct plan is to extend that same pipeline for machine callers with an explicit caller matrix:

- BFF user callers continue through the existing Blazor plus YARP flow.
- Direct JWT callers are supported for external trusted clients and tools.
- Tenant-bound API key callers are supported for server-to-server integrations.
- Instance-admin operational APIs stay platform-scoped and must not become a backdoor into tenant business data.

The design must preserve the current runtime contract:

- `SingleTenant`: tenanting remains abstracted away from callers.
- `MultiTenant`: tenant identity is resolved authoritatively by the API from trusted host or slug hints for direct JWT callers, and from the API key binding itself for API-key callers.

The plan below assumes planning only. No implementation decisions should bypass the current Clean Architecture structure or the hard boundary from `docs/ADMIN_HIERARCHY.md` that instance admins cannot access tenant business data or tenant API tokens.

---

## Current State Analysis

### Verified Current Capabilities

| Area | Verified Files | Current State |
|---|---|---|
| API-authoritative tenant resolution | `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs`, `Explore.API/Program.cs`, `docs/DEPLOYMENT_MODES.md` | `SingleTenant` resolves the default tenant automatically. `MultiTenant` resolves in the API via trusted `X-Tenant-Slug`, custom domain, then subdomain, and fails closed with `404`. |
| JWT authentication | `Explore.API/Program.cs`, `docs/SECURITY.md` | API uses JWT Bearer with Keycloak-oriented audience validation and BFF-aligned multi-client support. |
| Authorization pipeline | `Explore.Application/Behaviors/AuthorizationBehavior.cs`, `docs/API.md`, `docs/SECURITY.md` | Authorization is already centralized in MediatR request behaviors and resource-level authorization patterns. |
| Admin hierarchy | `docs/ADMIN_HIERARCHY.md`, `Explore.Application/Authorization/AdminClaimTypes.cs`, `Explore.Application/DTOs/User/AdminAuthorityDto.cs`, `Explore.Application/Features/Users/Handlers/Queries/GetAdminAuthorityRequestHandler.cs` | Instance, tenant, and organization authority are separated and already expressed in code and docs. |
| Current rate limiting | `Explore.API/Extensions/RateLimitingExtensions.cs`, `Explore.API/Program.cs`, `docs/API.md`, `docs/OPERATIONS.md` | Rate limiting exists today, but it is partitioned by IP and authenticated user, not by API key or tenant. |
| Telemetry and metrics | `Explore.Application/Telemetry/BusinessMetrics.cs`, `Explore.API/Program.cs`, `docs/OPERATIONS.md` | Business metrics already support dimensional tags including `tenant_id`. This is a useful base for API-key metrics and abuse visibility. |
| Existing token entity pattern | `Explore.Domain/UserAuthenticationToken.cs`, `Explore.Persistence/Configurations/Entities/UserAuthenticationTokenConfiguration.cs`, `Explore.Application/Contracts/Persistence/IUserAuthenticationTokenRepository.cs`, `Explore.Persistence/Repositories/UserAuthenticationTokenRepository.cs`, `Explore.API/Controllers/UserAuthenticationTokenController.cs` | A token-management slice already exists, but it stores provider/OAuth-style user tokens (`AccessToken`, `RefreshToken`, `IdToken`, `PdsHost`, `DpopKey`). It is not the external caller API-key model requested here. |
| Forwarded headers and proxy handling | `Explore.Blazor/Program.cs`, `Explore.Blazor/Extensions/MiddlewareExtensions.cs`, `Explore.Blazor/Services/CircuitAccessTokenService.cs`, `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs` | The repo already forwards and consumes `X-Forwarded-Host` and `X-Forwarded-For` style data. Blazor currently trusts all proxies for forwarded-header restoration, which is a useful reference but too permissive to become the external API trust model by default. |

### Verified Gaps

1. There is no verified external API-key authentication scheme in the API pipeline today.
2. There is no verified tenant-bound service account or automation principal model.
3. There is no verified per-key rate-limit partitioning or per-key usage reporting.
4. The current tenant middleware in `Explore.API/Program.cs` runs before authentication, which creates a design constraint for direct callers: tenant discovery cannot depend on an already-authenticated principal.
5. The existing `UserAuthenticationToken` slice is likely reusable as a CQRS and repository pattern reference, but not as the final domain model for external API keys.
6. The API currently registers JWT bearer directly in `Explore.API/Program.cs`; there is no verified `AddPolicyScheme`-style auth dispatch in the API today.
7. Host-derived tenanting already depends on forwarded headers in the current repo, but the external API trust boundary for proxies and forwarded hosts is not yet formalized.

### Architectural Constraints To Keep

1. Domain -> Application -> Persistence/Infrastructure -> API/Blazor dependency flow must stay intact.
2. Validators remain manually instantiated in handlers.
3. Repositories return entities, not DTOs.
4. Multi-tenant unresolved API requests must continue to fail closed.
5. Instance-admin capabilities must remain platform-scoped unless an explicitly audited emergency path is designed.

---

## Proposed Future State

## Caller Matrix

| Caller Type | SingleTenant Contract | MultiTenant Contract | Auth Material | Tenant Source |
|---|---|---|---|---|
| BFF user | No tenant selector exposed | Existing route or host flow through BFF | Keycloak JWT via BFF | API resolves from trusted forwarded slug or host |
| Direct JWT user | No tenant selector required | Must provide explicit tenant context by host or `X-Tenant-Slug` | Direct bearer JWT | API resolves from explicit request context, then validates membership or admin authority |
| Tenant API-key client | No tenant selector required | Must not send tenant selector as authority; tenant comes from the key binding | API key | Key itself resolves tenant and owner context |
| Instance-admin ops client | Platform-scoped only | Platform-scoped only | JWT only, not tenant API key | No tenant-derived business scope by default |

## Credential And Principal Separation

The design should separate the stored credential from the runtime principal explicitly:

- **Credential**: the persisted secret-bearing record, such as `ExternalApiKey`, with prefix, hash, expiry, status, tenant binding, owner reference, and audit metadata.
- **Principal**: the authenticated runtime identity built from a credential or JWT, carrying claims such as tenant id, owner type, owner id, key id, scope bundle, and authentication method.

This keeps the domain ready for future service accounts, robot users, tenant integrations, or stronger non-secret machine auth methods without rewriting authorization semantics.

## Authentication Scheme Strategy

The framework-aligned strategy should be fixed up front:

- **JWT bearer scheme** for direct user or trusted app callers.
- **Custom API-key scheme** implemented with `AuthenticationHandler<TOptions>` for tenant-bound machine callers.
- **Policy scheme** as the default API auth scheme to dispatch based on request shape.

Operational rules:

- keep `Authorization: Bearer` for JWTs only
- use a dedicated header such as `X-API-Key` for machine callers
- keep authentication in the ASP.NET Core authentication subsystem rather than controller logic

## Target API-Key Model

Introduce a first-class machine credential model for external access. The plan assumes a new aggregate rather than reusing `UserAuthenticationToken` directly.

Required properties:

- stable public key identifier or prefix for lookup and support workflows
- one-time shown secret value
- hashed secret at rest
- owner type (`User`, `Organization`, optionally `TenantIntegration` later)
- owner identifier
- tenant binding
- scope set or permission bundle
- status (`Active`, `Revoked`, `Expired`)
- optional expiry date
- rotation metadata
- last-used metadata
- audit fields
- optional per-key quota override fields
- optional IP allowlist or CIDR restrictions when tenant governance enables them

### Ownership Rules

- **User API key**: acts on behalf of a user inside one tenant; can only access what that user is authorized to access.
- **Organization API key**: acts on behalf of an organization-admin-approved automation context for a specific organization within one tenant; can create and manage organization-owned resources within allowed scopes.
- **Tenant integration key**: optional later evolution for tenant-wide integrations; should not be part of v1 unless a clear use case exists.
- **Instance-admin operational key**: do not model this as a normal tenant key. If needed later, it should be a separate platform-client capability with narrow ops scopes and stronger auditing.

Recommended product direction for v1:

- ship **user keys** for developer or personal automation use cases
- ship **organization automation keys** for production integrations
- defer a full **service account** abstraction unless zero-downtime rotation or bot-style ownership is required immediately
- keep the domain ready to evolve toward service accounts later, because mature systems often move there over time

## Secret And Rotation Handling

This plan should be opinionated rather than stopping at “hashed”:

- generate strong random secrets
- show the raw secret once at creation time only
- store only hash plus prefix/public id at rest
- never log the raw key
- use constant-time verification
- optionally support an application-level pepper or HMAC strategy with secret material stored outside the database
- explicitly choose whether rotation supports an overlap window for zero-downtime client cutover

### Authorization Direction

API keys should authenticate into a principal shape that can still flow through the existing application authorization system. The plan should not fork business authorization into controller-only checks.

Preferred direction:

1. API authenticates the key.
2. API constructs claims describing principal type, tenant, owner type, owner id, and scopes.
3. Existing controller plus MediatR path remains the enforcement boundary.
4. Application handlers continue to perform resource ownership and permission checks.
5. Cerbos or local authorization can later consume the same key-derived principal attributes.

Additional policy rules:

- interactive or platform-admin operations remain JWT-only
- tenant business automation may use API keys within narrow scopes
- future higher-trust machine clients can add stronger sender constraints later without changing the credential/principal split

### Tenant Resolution Direction

The future design must be explicit about pre-auth versus post-auth needs:

- **Direct JWT callers**: tenant still comes from host or `X-Tenant-Slug`, then membership or authority is validated after authentication.
- **API-key callers**: key lookup provides tenant identity before downstream authorization. Raw caller-provided tenant slug must not be trusted as the authority for API-key requests.

The recommended direction is **split-phase tenant handling**, not a single middleware that tries to do everything:

1. **Pre-auth request context**
   - short-circuit `SingleTenant`
   - normalize trusted host and forwarded-host information
   - capture direct-JWT tenant hints from trusted host or explicit `X-Tenant-Slug`
   - do not trust caller tenant hints as authority for API-key callers
2. **Authentication**
   - JWT handler authenticates bearer tokens
   - API-key handler authenticates `X-API-Key`, loads credential metadata, and creates claims including tenant id and key id
3. **Post-auth tenant validation**
   - direct JWT path validates the authenticated user against the requested tenant
   - API-key path validates route, host, or slug hints against the tenant bound to the key
   - unresolved or mismatched multi-tenant requests fail closed

## Reverse Proxy And Host Trust Contract

Because the platform is self-hosted and supports host-derived tenant resolution, proxy trust must be treated as a first-class design concern.

Required rules:

- document the trusted proxy or load-balancer boundary explicitly
- process forwarded headers deliberately in reverse-proxy deployments
- restrict trusted proxies or networks for the external API host instead of trusting all forwarded-host sources
- define exact mismatch handling when forwarded host, direct host, and `X-Tenant-Slug` disagree
- include proxy-aware tests for host-derived tenant flows

Repo-specific note: `Explore.Blazor/Extensions/MiddlewareExtensions.cs` currently clears known proxies and known networks for Blazor forwarded-header handling. That is a useful local reference, but the external API plan should not copy that trust posture blindly into direct API ingress.

---

## Implementation Phases

### Phase 0 - Pipeline ADR And Spike

Goal: settle the risky request-flow seam before heavy domain, persistence, or UI work begins.

#### Task 0.1: Write ADR for authentication plus tenant-resolution request flow
- **Layer:** Architecture/Docs
- **Deliverables:** ADR describing pre-auth context, auth-scheme dispatch, post-auth tenant validation, proxy trust, and fail-closed semantics
- **Acceptance Criteria:**
  - fixes the auth strategy as JWT bearer plus custom API-key handler plus policy-scheme dispatch
  - defines the dedicated machine-auth header and rejects Bearer-overloaded API keys
  - defines single-tenant fast-path behavior
  - defines exact wrong-tenant and unresolved-tenant error semantics
- **Effort:** S

#### Task 0.2: Build a pipeline spike in the API host
- **Layer:** API/Test
- **Deliverables:** proof-of-concept request flow for one direct JWT path, one API-key path, and one single-tenant path
- **Acceptance Criteria:**
  - direct JWT plus explicit tenant context works through the chosen auth dispatch model
  - API-key auth derives tenant from the key and rejects conflicting caller tenant hints
  - single-tenant path requires no tenant-specific caller material
  - multi-tenant unresolved or mismatched requests fail closed exactly as documented
- **Effort:** M

### Phase 1 - Domain Design And Policy Model

Goal: define the API-key domain model, ownership rules, lifecycle, and invariants.

#### Task 1.1: Define external API credential aggregate
- **Layer:** Domain
- **Deliverables:** new aggregate, status enum, owner-type enum or value object, audit-friendly metadata
- **Acceptance Criteria:**
  - key secret is never stored in plaintext
  - aggregate explicitly binds to a tenant
  - aggregate supports user and organization ownership
  - aggregate does not give instance admins tenant business access by default
  - credential data stays separate from runtime principal construction
- **Effort:** M

#### Task 1.2: Define scope model and permission translation
- **Layer:** Domain/Application
- **Deliverables:** scope vocabulary, mapping rules to resource permissions, v1 scope matrix
- **Acceptance Criteria:**
  - scopes cover read-only, event creation, organization management, and sensitive/private-event access where allowed
  - organization keys cannot exceed organization-admin ceilings
  - user keys cannot exceed user-authorized capabilities
- **Effort:** M

#### Task 1.3: Define rate-limit and quota semantics
- **Layer:** Domain/Application
- **Deliverables:** default quota policy, optional per-key overrides, usage dimensions
- **Acceptance Criteria:**
  - defaults exist for every new key
  - per-key and per-tenant limits can coexist
  - design distinguishes hard throttling from observability-only metrics
  - the plan explicitly states whether enforcement is node-local or requires a future shared quota design
- **Effort:** S

### Phase 2 - Application Contracts And CQRS

Goal: add use cases, request models, validators, repository contracts, and authorization semantics.

#### Task 2.1: Add repository contracts and query models
- **Layer:** Application
- **Deliverables:** repository interfaces for create, lookup by public id/prefix, revoke, rotate, usage listing
- **Acceptance Criteria:**
  - repositories return entities only
  - contracts support authentication lookup without leaking secret material to callers
  - contracts support admin-safe listing metadata without exposing full secrets
- **Effort:** M

#### Task 2.2: Add CQRS handlers for API-key lifecycle
- **Layer:** Application
- **Deliverables:** commands and queries for create, list, detail, rotate, revoke, update policy
- **Acceptance Criteria:**
  - handlers manually instantiate validators
  - create and update commands return `BaseCommandResponse<Guid>` when applicable
  - queries return DTOs without exposing hashed or secret material
- **Effort:** M

#### Task 2.3: Add authorization rules for key ownership and scope enforcement
- **Layer:** Application
- **Deliverables:** request authorization strategy, owner checks, organization membership checks, tenant membership checks
- **Acceptance Criteria:**
  - user-created keys require valid current user in tenant
  - organization keys require org-level authority
  - instance-admin listing remains metadata-only and excludes tenant secret access
  - disabled users, removed organization authority, and disabled tenants cause dependent keys to fail immediately
- **Effort:** M

#### Task 2.4: Define principal and claims model for authenticated API keys
- **Layer:** Application/API
- **Deliverables:** claim names, principal type marker, owner and tenant claim contract
- **Acceptance Criteria:**
  - principal shape can flow through existing authorization behavior
  - claim contract supports future Cerbos evaluation
  - no duplicated authorization model is created outside the existing handler pipeline
  - claims include auth method, key id, tenant id, owner type, and owner id
- **Effort:** S

### Phase 3 - Persistence And Data Model

Goal: persist API-key data and usage analytics safely.

#### Task 3.1: Add EF Core entity configurations and migrations
- **Layer:** Persistence
- **Deliverables:** entity configuration, indexes, migration, optional usage table or rollup table
- **Acceptance Criteria:**
  - hashed secret and public prefix are indexed appropriately
  - tenant binding is enforced by schema and query filters where appropriate
  - migration is explicit and reversible in normal EF Core workflow
  - the model supports rotation overlap if that product behavior is approved
- **Effort:** M

#### Task 3.2: Add repository implementations
- **Layer:** Persistence
- **Deliverables:** repository implementations for lifecycle and auth lookup flows
- **Acceptance Criteria:**
  - lookup path avoids loading unnecessary data
  - tenant filter usage is intentional and documented for platform-safe metadata views
  - selective query-filter bypass, if any, is limited to explicit administrative metadata paths
- **Effort:** M

#### Task 3.3: Add usage storage strategy
- **Layer:** Persistence/Application
- **Deliverables:** design for request-count storage, rate-limit metadata, last-used updates, and rollups
- **Acceptance Criteria:**
  - design avoids hot-row contention under load
  - instance-admin visibility can be served from metadata or rollups without exposing tenant data contents
  - retention policy is defined for detailed versus aggregated records
  - `last_used_at` and optional `last_used_ip` updates avoid a blocking database write on every request
- **Effort:** M

### Phase 4 - API Authentication And Tenant Validation Seam

Goal: authenticate API keys in the API host and reconcile them with tenant resolution.

#### Task 4.1: Add API-key authentication scheme
- **Layer:** API
- **Deliverables:** named API-key auth scheme, `AuthenticationHandler<TOptions>`, and policy-scheme dispatch in the API host
- **Acceptance Criteria:**
  - supports `Authorization: Bearer` for JWT and a dedicated `X-API-Key` flow for machine clients
  - logs failures without leaking raw key material
  - principal claims include tenant and owner context
  - authentication remains in the ASP.NET Core auth subsystem rather than controller logic
- **Effort:** M

#### Task 4.2: Resolve middleware-order contract
- **Layer:** API
- **Deliverables:** implemented split-phase request flow for direct JWT and API-key callers
- **Acceptance Criteria:**
  - split-phase request handling is implemented and documented
  - multi-tenant unresolved requests still fail closed
  - API-key requests derive tenant from the key, not from untrusted caller-provided slug alone
  - wrong-tenant requests return the exact documented status code consistently
- **Effort:** M

#### Task 4.3: Add reverse-proxy trust handling for host-derived tenancy
- **Layer:** API/Infrastructure/Docs
- **Deliverables:** trusted-proxy configuration approach, forwarded-host decision rules, and deployment guidance
- **Acceptance Criteria:**
  - host-derived tenant resolution only trusts configured proxies or networks
  - mismatch behavior between host and slug signals is documented and testable
  - proxy-aware integration tests exist for self-hosted reverse-proxy scenarios
- **Effort:** M

### Phase 5 - Rate Limiting, Observability, And Platform Visibility

Goal: provide abuse control and non-invasive operational insight.

#### Task 5.1: Add per-key partitioned rate limiting
- **Layer:** API
- **Deliverables:** new rate-limit partitions, policy selection logic, response metadata
- **Acceptance Criteria:**
  - machine traffic is partitioned by key id or prefix, not by user name
  - tenant-level or owner-level partitioning can coexist with per-key throttling
  - anonymous and JWT policies continue to work without regression
  - expensive endpoint classes can be throttled separately if needed
  - the rejection model defines which limiter wins when multiple partitions reject
- **Effort:** M

#### Task 5.2: Add API-key metrics and audit events
- **Layer:** Application/API/Infrastructure
- **Deliverables:** counter and audit hooks for create, one-time reveal, revoke, rotate, auth success, auth fail, wrong-tenant attempt, expired-use attempt, throttle, and key usage
- **Acceptance Criteria:**
  - metrics include dimensions such as `tenant_id`, key type, owner type, and outcome where safe
  - raw secrets are never logged
  - failed authentication and throttling are traceable via correlation ids
- **Effort:** M

#### Task 5.3: Define clustered deployment semantics for quotas and throttling
- **Layer:** API/Operations/Docs
- **Deliverables:** explicit statement of node-local versus cluster-shared enforcement for self-hosted multi-node deployments
- **Acceptance Criteria:**
  - plan does not imply cross-node consistency from in-process rate limiting alone
  - any future shared-quota requirement is called out as a separate design slice
  - self-hosters can understand the operational tradeoff clearly
- **Effort:** S

### Phase 6 - Management APIs And Platform Visibility

Goal: expose management operations and safe operational reporting after the auth and limiter foundations are settled.

#### Task 6.1: Add management endpoints for external API keys
- **Layer:** API
- **Deliverables:** controllers or controller extensions for key lifecycle and stats
- **Acceptance Criteria:**
  - controllers remain thin
  - endpoint authorization follows existing patterns
  - public APIs clearly separate tenant-scoped management from platform-scoped ops endpoints
- **Effort:** M

#### Task 6.2: Add direct-consumer contract documentation to HTTP surface
- **Layer:** API/Docs
- **Deliverables:** OpenAPI notes, error semantics, examples for single-tenant and multi-tenant callers
- **Acceptance Criteria:**
  - direct JWT examples show explicit tenant selection in multi-tenant mode
  - API-key examples show no tenant selector when tenant is key-bound
  - platform-scoped instance-admin endpoints are documented separately
- **Effort:** S

#### Task 6.3: Add instance-admin metadata visibility
- **Layer:** API/Application/Blazor
- **Deliverables:** platform-safe stats views or endpoints
- **Acceptance Criteria:**
  - instance admins can see counts, health, issuance volume, throttle trends, and tenant-level usage metadata
  - instance admins cannot view raw tenant tokens or tenant business data payloads
  - any emergency access path is explicitly out of band and fully audited
- **Effort:** M

### Phase 7 - Blazor Admin Surfaces

Goal: expose lifecycle management and visibility in the existing admin UX.

#### Task 7.1: Add tenant-admin and organization-admin API-key management UX
- **Layer:** Blazor Client
- **Deliverables:** pages or sections for creating, rotating, revoking, and viewing scoped metadata
- **Acceptance Criteria:**
  - secrets are shown once at creation time
  - subsequent views show only prefix, owner, scopes, status, and last-used info
  - UX distinguishes user keys from organization keys
- **Effort:** M

#### Task 7.2: Add platform-ops visibility UX
- **Layer:** Blazor Client
- **Deliverables:** instance-admin stats section
- **Acceptance Criteria:**
  - only operational metadata is shown
  - cross-tenant browsing does not reveal tenant secret material
  - single-tenant mode hides irrelevant multi-tenant platform affordances
- **Effort:** S

### Phase 8 - Cerbos, Testing, And Documentation

Goal: make the feature production-grade and repo-aligned.

#### Task 8.1: Extend Cerbos or local authorization model
- **Layer:** Application/Infrastructure/Docs
- **Deliverables:** principal shape notes, policy updates if needed
- **Acceptance Criteria:**
  - machine principals are represented explicitly
  - organization-bound keys cannot exceed organization authority
  - private-event access decisions remain policy-driven rather than hard-coded in controllers
- **Effort:** M

#### Task 8.2: Add unit, integration, and architecture tests
- **Layer:** Test projects
- **Acceptance Criteria:**
  - auth handler tests cover valid, revoked, expired, and malformed keys
  - integration tests cover single-tenant and multi-tenant direct access paths
  - rate limiting tests cover per-key partitioning
  - authorization tests cover user, organization, and instance-admin boundaries
  - proxy-aware integration tests cover trusted forwarded-host scenarios and mismatch rejection
  - single-tenant fast path is verified without caller-supplied tenant material
- **Effort:** L

#### Task 8.3: Update repo docs
- **Layer:** Docs
- **Deliverables:** updates to `docs/API.md`, `docs/SECURITY.md`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/ADMIN_HIERARCHY.md`, and active task docs
- **Acceptance Criteria:**
  - docs explain caller matrix and tenant contract clearly
  - docs explain how single-tenant abstracts tenanting away
  - docs explain why instance-admin visibility is metadata-only
- **Effort:** M

---

## Detailed Task Backlog

### Phase 0 - Pipeline Spike

1. Write the ADR for authentication plus tenant-resolution request flow.
2. Prove one direct JWT request path with explicit tenant context.
3. Prove one API-key request path with tenant derived from the key.
4. Prove the single-tenant fast path and fail-closed mismatch behavior.

### Phase 1 - Domain

1. Design `ExternalApiKey` aggregate and related enums.
2. Decide whether organization automation is represented as direct organization ownership or a service-account adjunct model.
3. Define a stable scope catalog for v1.
4. Define revocation, rotation, and expiration invariants.

### Phase 2 - Application

1. Create repository contracts and DTOs.
2. Create commands and queries for lifecycle management.
3. Add validator set using manual instantiation.
4. Define claims and principal contract for authenticated keys.
5. Add owner- and tenant-aware authorization checks.

### Phase 3 - Persistence

1. Add tables, configurations, indexes, and migrations.
2. Implement auth lookup and management repositories.
3. Add usage rollup strategy.

### Phase 4 - API

1. Add API-key authentication handler.
2. Implement split-phase tenant validation around the auth pipeline.
3. Harden reverse-proxy trust for host-derived tenanting.

### Phase 5 - Operations

1. Add per-key rate-limit partitions.
2. Add metrics, audit events, and clustered deployment semantics.
3. Add response headers and operational dashboards where appropriate.

### Phase 6 - Management APIs

1. Add key lifecycle and stats endpoints.
2. Document direct-consumer contracts in the HTTP surface.
3. Add instance-admin metadata-only reporting.

### Phase 7 - UI

1. Add tenant and organization key management pages.
2. Add instance-admin metadata views.
3. Ensure single-tenant mode hides multi-tenant-only platform affordances.

### Phase 8 - Quality

1. Add unit tests.
2. Add integration tests.
3. Add authorization and architecture tests.
4. Update docs and active task context.

---

## Risks And Mitigations

| Risk | Why It Matters | Mitigation |
|---|---|---|
| Trusting raw `X-Tenant-Slug` for API-key callers | Lets caller-supplied hints become authority | Derive tenant from the key binding for API-key auth flows |
| Reusing `UserAuthenticationToken` as-is | It models stored provider tokens, not external consumer credentials | Reuse the pattern, not the aggregate |
| Middleware-order ambiguity | Current tenant middleware runs before auth | Make pre-auth versus post-auth resolution explicit in implementation design |
| Missing auth-scheme dispatch decision | Controller or middleware hacks could leak into the design | Fix the design now around JWT bearer plus custom API-key handler plus policy scheme |
| Host-derived tenanting behind proxies | Misconfigured forwarded headers can produce spoofed or wrong-tenant routing | Treat forwarded headers as a trust-boundary problem and require explicit proxy configuration |
| Instance-admin visibility drift | Violates documented admin boundary | Limit platform views to metadata, rollups, and audit-safe summaries |
| Overloading user API keys for organization automation | Blurs ownership and auditing | Keep owner type explicit and bind organization keys to organization authority |
| Per-key rate limiting added without usage analytics | Hard to tune and troubleshoot abuse | Implement metrics and audit hooks together with throttling |
| Excessive scope granularity in v1 | Delays delivery and complicates policy mapping | Start with a small scope catalog aligned to existing permission groupings |
| Assuming in-process rate limiting is cluster-wide | Self-hosted multi-node deployments may observe inconsistent enforcement | Document node-local semantics unless a shared quota store is intentionally added |
| Weak secret handling and rotation semantics | Real integrations need safe cutover and logging discipline | Require one-time display, hashed storage, no raw-key logging, and explicit overlap-window policy |

---

## Success Metrics

- external clients can authenticate directly without the BFF in both single-tenant and multi-tenant deployments
- single-tenant callers do not need tenant-specific configuration
- multi-tenant direct JWT callers use an explicit, documented tenant contract
- API-key callers authenticate without relying on caller-supplied tenant authority
- auth-scheme dispatch is settled before domain and persistence work expands
- per-key throttling produces accurate 429 behavior and usable diagnostics
- self-hosted reverse-proxy deployments have a documented trusted-host model
- instance-admin views provide operational visibility without exposing tenant secrets or tenant business data
- test coverage exists for auth, revocation, ownership, and rate-limit behavior

---

## Dependencies And Resources

### Internal Dependencies

- `Explore.API/Program.cs`
- `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs`
- `Explore.API/Extensions/RateLimitingExtensions.cs`
- `Explore.Application/Behaviors/AuthorizationBehavior.cs`
- `Explore.Application/Telemetry/BusinessMetrics.cs`
- `Explore.Application/Authorization/AdminClaimTypes.cs`
- `Explore.Domain/UserAuthenticationToken.cs` (pattern reference only)

### Documentation Dependencies

- `docs/API.md`
- `docs/SECURITY.md`
- `docs/ARCHITECTURE.md`
- `docs/DEPLOYMENT_MODES.md`
- `docs/ADMIN_HIERARCHY.md`
- `docs/OPERATIONS.md`
- `docs/CONFIGURATION.md`

### External Research Inputs Used

- Oracle architecture review for middleware order, caller matrix, and admin-boundary risks
- external research on mature SaaS API-key design: hashed secrets, one-time display, prefix-based lookup, rotation, revocation, auditability, and combined per-key plus per-tenant throttling
- ASP.NET Core framework guidance for authentication schemes, policy schemes, rate limiting, middleware ordering, and proxy/load-balancer handling

---

## Effort Estimates

| Phase | Estimate |
|---|---|
| Phase 0 - Pipeline ADR and spike | 1-2 days |
| Phase 1 - Domain design | 2-3 days |
| Phase 2 - Application contracts and CQRS | 3-4 days |
| Phase 3 - Persistence and migrations | 2-3 days |
| Phase 4 - API auth and tenant validation seam | 3-5 days |
| Phase 5 - Rate limiting and observability | 2-4 days |
| Phase 6 - Management APIs and platform visibility | 2-3 days |
| Phase 7 - Blazor admin surfaces | 2-4 days |
| Phase 8 - Tests and docs | 3-4 days |

Indicative total: **18-29 working days**, depending on how much of the Cerbos, proxy-hardening, and reporting surface is included in v1.

---

## Potential Risks & Unknowns

The highest-risk area remains the API pipeline seam between tenant resolution and authentication. The current API resolves tenant before authentication, but API-key callers ideally derive tenant from the key itself. If the Phase 0 spike does not settle that cleanly, the system could drift toward trusting raw tenant hints or duplicating enforcement logic between middleware and handlers. The second major risk is host-derived tenancy in self-hosted reverse-proxy deployments: forwarded-host and forwarded-for behavior must be treated as a trust-boundary problem, not just as deployment trivia. The third likely complexity is clustered throttling semantics: the built-in in-process limiter is a good fit for node-local abuse control, but strict cross-node quotas need an explicit follow-on design rather than assumption. Platform visibility remains constrained by `docs/ADMIN_HIERARCHY.md`, so instance-admin reporting must stay metadata-only unless a separately-audited emergency path is introduced.
