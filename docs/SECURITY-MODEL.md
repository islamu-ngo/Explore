ABOUTME: Describes authentication, authorization, and trust boundaries for the platform.
ABOUTME: Focuses on enforced behavior in code (BFF, MediatR authorization, and fallback modes).

# Security

> **Audience:** Operators | Contributors | AI agents
> **Status:** Mixed
> **Owner:** Security
> **Last Verified:** 2026-05-06
> **Source Anchors:** `Explore.API/Extensions/AuthenticationExtensions.cs`, `Explore.API/Extensions/CorsExtensions.cs`, `Explore.Blazor/Extensions/YarpProxyExtensions.cs`, `Explore.Application/Services/ApiKeyHashing.cs`, `Explore.Application/Telemetry/BusinessMetrics.cs`, `Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs`, `Explore.Infrastructure/Services/FallbackAuthorizationService.cs`, `docs/AUTHORIZATION.md`

## Security Model

The platform uses a BFF model:

- `Explore.Blazor` (server) handles OIDC and session cookies.
- `Explore.Blazor.Client` (WASM) does not directly manage access tokens.
- `Explore.API` authorizes bearer-token requests and applies resource-level checks in Application layer.

## Security CI Gates

Security-sensitive changes are protected by both workflow checks and GitHub repository settings. See [CI_CD_GOVERNANCE.md](CI_CD_GOVERNANCE.md) for the required/advisory matrix, fork PR policy, and branch-protection guidance.

Current security gates:

- `Security Integration Tests` exercises auth, Keycloak, Cerbos, and policy-contract scenarios for matching paths and on schedule.
- `Cerbos Policy Validation` compiles static policies and policy tests with a fixed Cerbos binary version.
- `CodeQL Advanced` publishes code-scanning results for C#, JavaScript/TypeScript, and GitHub Actions.
- `Dependency Review` blocks vulnerable dependency changes on pull requests.
- Secret scanning and push protection must be enabled in GitHub repository or organization settings; they are not controlled by application runtime configuration.

## Authentication Flow (Current)

1. User authenticates through BFF OpenID Connect flow.
2. BFF stores auth session in cookie.
3. Calls to `/api/*` are proxied by YARP from BFF to API.
4. BFF adds bearer token to proxied API requests (`YarpProxyExtensions.ForwardBearerTokenAsync`).

## JWT Bearer Configuration (API)

- Authority: Keycloak OIDC metadata endpoint.
- Multi-client audience validation: `islamu-event-api`, `islamu-event-blazor`.
- Custom `AudienceValidator`: checks both `aud` claim and `azp` (Keycloak authorized party) claim. Accepts if either contains a valid audience.
- Clock skew tolerance: 5 minutes.
- Dev mode: accepts self-signed certificates, suppresses HTTPS metadata requirement.
- Detailed JWT event logging on: `OnMessageReceived`, `OnAuthenticationFailed`, and `OnChallenge`.

## Auth Diagnostic Safety

OIDC and BFF challenge failures must expose only safe diagnostic handles:

- Browser redirects use `challengeError=1`, a normalized `errorCode`, and a correlation ID.
- Browser redirects must not include `errorDetail`, raw exception messages, provider response bodies, client IDs, client-secret length, client-secret prefix, tokens, or secret-derived metadata.
- Production-path logs use structured error codes, correlation IDs, failure categories, and boolean presence flags where needed. They must not log raw provider error bodies, raw exception text from identity-provider callbacks, client-secret prefixes, client-secret lengths, tokens, or refresh-token grant payloads.
- Development-only diagnostics such as `/auth/debug` remain a local troubleshooting surface and must never include secret values.

Use `ISafeAuthDiagnosticsPolicy` for BFF auth challenge and OIDC remote-failure redirects so user-facing errors stay generic while operators can correlate failures through logs and traces.

## Header and Secret Hardening

In YARP transforms:

- `X-Tenant-Slug` is forwarded when route or request context provides an explicit tenant hint.
- Any incoming or stale proxied `X-Setup-Secret` header is removed first. The BFF then resolves a setup secret through `ISetupSecretResolver` in this source order:
  1. BFF-owned setup handshake/session state,
  2. protected BFF-issued setup cookie,
  3. explicit local/development/bootstrap configuration fallback.
- Inbound request headers are never trusted as setup-secret sources. Browser-controlled `X-Setup-Secret` values must be stripped and ignored by both YARP and server-side forwarding handlers.

This prevents stale outgoing proxy headers and browser-controlled privileged headers from leaking across requests. Treat the setup secret as bootstrap-only sensitive material; the BFF protects the setup cookie with ASP.NET Core Data Protection and forwards only resolver output to downstream API calls.

## BFF Antiforgery Contract

Cookie-authenticated unsafe BFF endpoints must validate antiforgery tokens because browsers automatically include BFF cookies on same-site requests.

- Token issuance: `UseAntiforgeryTokenMiddleware` calls `IAntiforgery.GetAndStoreTokens` on `GET` requests and writes the request token to the readable `XSRF-TOKEN` cookie.
- Header contract: clients send the token back in the `X-CSRF-TOKEN` header. This matches the BFF `AddAntiforgery` configuration.
- Browser client path: `BrowserCredentialsMessageHandler` attaches browser credentials and adds `X-CSRF-TOKEN` for `POST`, `PUT`, `PATCH`, and `DELETE` requests.
- Server self-call path: `BffCookieForwardingHandler` forwards captured cookies and mirrors `XSRF-TOKEN` into `X-CSRF-TOKEN` when InteractiveServer code calls BFF endpoints.
- Endpoint validation: unsafe minimal BFF endpoints call `.ValidateAntiforgery()`, which returns `400 Antiforgery validation failed` for missing or invalid tokens.
- Protected endpoint families include auth refresh, storage upload proxy, preference mutations, and appearance profile mutations.
- Documented exceptions are setup-secret bootstrap endpoints, `/bff/auth/refresh-session/internal`, and the storage upload session/proxy endpoints (`/bff/storage/upload-session`, `/bff/storage/upload-proxy`); these remain constrained by setup credentials, authorization, per-user session ownership binding (`IStorageUploadSessionStore`), cryptographically random short-lived session IDs, server-side stream-to-API proxying (browser never reaches the storage provider), and rate limiting because they are used before, outside, or via InteractiveServer circuit self-calls where browser antiforgery semantics cannot be reliably satisfied.

Do not add new unsafe `/bff/*` endpoints without either `.ValidateAntiforgery()` or a documented bootstrap/internal exception with equivalent compensating controls.

## Storage Upload Session Binding

The Blazor BFF upload proxy is an SSRF-sensitive boundary because browser uploads could otherwise try to make the server send bytes to attacker-chosen destinations. Browser callers must not control provider, tenant, destination URL, object key, local path, or max-size policy.

- Browser upload flow starts with `/bff/storage/upload-session`. The BFF asks the API for a provider-neutral upload session and stores only the approved session metadata under an opaque BFF upload-session id.
- `/bff/storage/upload-proxy` accepts `uploadSessionId`, `contentType`, and `file`, not a trusted raw `uploadUrl`, provider object key, or filesystem path. It resolves the BFF session server-side and rejects missing, expired, cross-user, content-type-mismatched, size-mismatched, or unknown sessions.
- The BFF streams bytes to the API upload-session content endpoint. The API owns provider selection and writes to the selected `IFileStorageProvider`.
- Arbitrary HTTPS URLs, private/internal hosts, local filesystem paths, or presigned-looking attacker values must not be proxied merely because they resemble storage destinations.
- Upload sessions are short-lived, user-bound, content-type-bound, and consumed after successful upload. This keeps the browser path bound to a server-issued upload intent without duplicating tenant storage policy in the UI layer.
- BFF/API storage logs must not include raw upstream response bodies, presigned URLs, signatures, tokens, object keys, filesystem paths, filenames, or object secrets. Use safe fields such as status code, presence booleans, bounded provider labels, and session failure code.

Server-side/non-browser code paths may still use direct provider upload URLs when the server owns the trusted URL. Browser-facing upload proxy paths must use the upload-session contract.

Forwarded-host trust for direct API traffic:

- `Explore.API` only applies `X-Forwarded-Host`, `X-Forwarded-For`, and `X-Forwarded-Proto` when a trusted proxy boundary is configured through `ForwardedHeadersTrust`.
- Host-derived tenant resolution must use normalized `Request.Host` after trusted forwarded-header processing, not raw `X-Forwarded-Host`.
- If no trusted proxy boundary is configured, the API ignores forwarded host/IP headers and falls back to the direct request host and remote IP.

## Authorization Boundary

Server-side enforcement is layered:

1. API endpoint-level attributes (`[AllowAnonymous]`, `[Authorize]`).
2. Application MediatR pipeline `AuthorizationBehavior`:
   - Checks `IAuthorizedRequest` interface — commands/queries declare required permissions.
   - Checks `[AuthorizeResource]` attribute — declarative resource-level authorization.
   - Optionally enhanced by `ISecureRequest` — provides dynamic resource context for fine-grained permission evaluation.
3. Runtime provider (`RuntimeAuthorizationProvider`) deciding Cerbos vs fallback.

See [AUTHORIZATION.md](AUTHORIZATION.md) for the full provider model, request patterns, and role boundary details.

Hard deny behavior:

- `AuthorizationBehavior` throws `AuthorizationException` on deny.
- API global exception handler returns HTTP `403 Forbidden` via RFC 7807 ProblemDetails.

## Runtime Authorization Providers

Provider selection:

- Tenant BYO Cerbos (if configured) has priority.
- Else instance setting `AuthorizationProvider` chooses:
  - `"cerbos"` -> `CerbosAuthorizationService`
  - default/other -> `FallbackAuthorizationService`

Failure behavior:

- Instance Cerbos failure denies all authorized requests (fail-closed). The operator explicitly chose Cerbos; falling back to a potentially more permissive local RBAC would silently bypass intended policies.
- Instance provider-mode read failures also enter the Cerbos fail-closed path and log only safe failure-type metadata; they do not default open to local RBAC.
- BYO Cerbos:
  - `failure_mode=closed` -> provider-instance fallback `SafeMode` (deny all except instance admin path).
  - `failure_mode=open` -> standard local RBAC fallback.
  - BYO config resolver failures activate provider-instance safe mode instead of silently using local RBAC.
  - `cerbos.mode=custom_endpoint` with a blank PDP endpoint preserves BYO mode/failure mode and any explicit BYO Admin API config; runtime authorization applies the configured failure mode rather than falling back to the instance PDP.

Runtime failure logs must not include raw PDP/Admin API endpoints, Admin API credentials, JWTs/tokens, response bodies, or exception objects/messages. Log failure type, request/correlation identifiers, counts, modes, and actions only.

## Policy Topology

Authorization policies are organized in three tiers:

### Static Policies (Disk)

Static resource policy files plus `derived_roles.yaml` live in `cerbos/policies/`. Avoid relying on an exact count in docs; architecture tests and policy parity checks are the safer source of truth.

- **`derived_roles.yaml`**: Resolves instance admin, tenant admin, and org admin roles from principal attributes and resource context.
- **Resource policies** (`{kind}.yaml`): Each defines rules per derived role and `authenticated_user`. Instance admin gets wildcard `"*"`, tenant/org admin get CRUD, authenticated user gets `"view"`.
- **Standard actions**: `view`, `create`, `update`, `delete`.
- **Extended actions**: `manage_members`, `lock`, `unlock`, `viewsharedcontacts`, `exportsharedcontacts`, `sync_diff`, `sync_apply`.

### Package Publishing (Admin API Store)

Cerbos policy publishing is package-based. `IPolicyPackageService` builds the bundled policy package from the static policy manifest and source files, then pushes that package through the configured Cerbos Admin API endpoint and triggers reload/status handling.

- Setup, admin UI, and zero-touch boot sync use the same package service rather than ad-hoc runtime role-policy generation.
- Manual ZIP fallback exports the same bundled package for operators to install with Cerbos tooling when Admin API sync is unavailable.
- Custom role CRUD remains represented through the application authorization catalog and policy package model; dynamic role-derived policy generation is deferred until it is reintroduced as a package-manifest contributor.

### BYO Cerbos (Per-Tenant Override)

Tenants may point to their own Cerbos PDP endpoint via tenant settings:

- Receives the same `AuthorizationCheck` payloads as the instance PDP.
- `AuthorizationCheck.Scope` enables per-tenant policy resolution within the BYO PDP.
- Optional BYO Admin API endpoint and credentials can target package sync/status independently of the PDP endpoint.
- Failure modes: `closed` → provider-instance safe mode (deny all except instance admin), `open` → fallback to local RBAC.
- A blank custom PDP endpoint is treated as a BYO runtime configuration error, not as an instruction to use the instance PDP. Any explicit BYO Admin API config is still preserved for package operations.
- Only applies to resource checks. Setting access always uses the instance provider.

### Authorization Catalogs

| Catalog | File | Purpose |
|---|---|---|
| `AuthorizationActions` | `Application/Authorization/AuthorizationActions.cs` | Action string constants matching Cerbos policy action names |
| `ResourceKinds` | `Application/Authorization/ResourceKinds.cs` | Resource kind string constants matching Cerbos policy file names |
| `ResourceDescriptors` | `Application/Authorization/ResourceDescriptors.cs` | DTO → authorization metadata extractors (kind, id, attributes, scope) |

### Custom Property Authorization Policies

Five resource policies govern custom property operations:

| Policy File | Resource Kind | Actions | Notes |
|---|---|---|---|
| `custom_property_template.yaml` | `islamuevent_custom_property_template` | view, create, update, delete, sync_diff, sync_apply | Template/runtime definition CRUD + sync operations. Tenant admin can manage templates within their tenant; hard purge routes additionally require the API Admin role. |
| `custom_property_value.yaml` | `islamuevent_custom_property_value` | view, create, update, delete | Runtime value CRUD. Org admin can manage values for their organization's entities. |
| `custom_property_projection.yaml` | `islamuevent_custom_property_projection` | view, update | Projection admin (rebuild, drain). Tenant admin can trigger rebuilds and drain dirty scopes. |
| `custom_property_governance.yaml` | `islamuevent_custom_property_governance` | view | Governance reporting. Tenant admin can view governance recommendations. |
| `platform_namespace.yaml` | `islamuevent_platform_namespace` | view, create, update, delete | Platform-reserved namespace protection. **Explicit deny** for tenant admin and org admin on write operations. Only instance admin can write. |

#### Endpoint-to-Policy Mapping

| Endpoint | Controller | Action | Resource Kind | Policy Rule |
|---|---|---|---|---|
| `GET /api/event/{id}/custom-property-definitions` | `EventCustomPropertyDefinitionController` | view | `islamuevent_custom_property_template` | AllowAnonymous |
| `POST /api/event/{id}/custom-property-definitions` | `EventCustomPropertyDefinitionController` | create | `islamuevent_custom_property_template` | Authorize |
| `PUT /api/event/{id}/custom-property-definitions/{defId}` | `EventCustomPropertyDefinitionController` | update | `islamuevent_custom_property_template` | Authorize |
| `DELETE /api/event/{id}/custom-property-definitions/{defId}` | `EventCustomPropertyDefinitionController` | delete | `islamuevent_custom_property_template` | Authorize |
| `DELETE /api/eventcustomproperty/{defId}/purge` | `EventCustomPropertyController` | update/delete | `islamuevent_custom_property_template` | Admin role |
| `DELETE /api/eventsessioncustomproperty/{defId}/purge` | `EventSessionCustomPropertyController` | update/delete | `islamuevent_custom_property_template` | Admin role |
| `DELETE /api/custompropertydefinition/{defId}/purge` | `CustomPropertyDefinitionController` | update/delete | `islamuevent_custom_property_template` | Admin role |
| `GET /api/event/{id}/custom-property-values` | `EventCustomPropertyValueController` | view | `islamuevent_custom_property_value` | AllowAnonymous |
| `POST /api/event/{id}/custom-property-values` | `EventCustomPropertyValueController` | create | `islamuevent_custom_property_value` | Authorize |
| `PUT /api/event/{id}/custom-property-values/{valId}` | `EventCustomPropertyValueController` | update | `islamuevent_custom_property_value` | Authorize |
| `DELETE /api/event/{id}/custom-property-values/{valId}` | `EventCustomPropertyValueController` | delete | `islamuevent_custom_property_value` | Authorize |
| `POST /api/admin/custom-property-projections/rebuild` | `CustomPropertyProjectionAdminController` | update | `islamuevent_custom_property_projection` | Authorize |
| `POST /api/admin/custom-property-projections/drain-dirty-scopes` | `CustomPropertyProjectionAdminController` | update | `islamuevent_custom_property_projection` | Authorize |
| `GET /api/admin/custom-property-projections/status` | `CustomPropertyProjectionAdminController` | view | `islamuevent_custom_property_projection` | Authorize |
| `GET /api/admin/custom-property-projections/events/{eventId}` | `CustomPropertyProjectionAdminController` | view | `islamuevent_custom_property_projection` | Authorize; `exposureCeiling` limits row visibility |
| `GET /api/admin/custom-property-projections/sessions/{eventSessionId}` | `CustomPropertyProjectionAdminController` | view | `islamuevent_custom_property_projection` | Authorize; `exposureCeiling` limits row visibility |
| `GET /api/custom-property-governance/recommendations` | `CustomPropertyGovernanceController` | view | `islamuevent_custom_property_governance` | Authorize |

#### Platform Namespace Protection

The `platform_namespace` policy enforces a hard boundary around the `platform` namespace:

- **Instance admin**: Full CRUD (wildcard `"*"`).
- **Tenant admin / Org admin**: **Explicit deny** on `create`, `update`, `delete`. Can only `view`.
- **Authenticated user**: `view` only.

This ensures platform-defined property definitions (e.g., standardized fields shared across all tenants) cannot be modified by tenant-level administrators. The deny rule takes precedence over any derived role grants.

## Scoped Policy Resolution

Authorization checks carry explicit scope context via `AuthorizationCheck.Scope` (containing `TenantId` and/or `OrganizationId`). This enables fine-grained policy routing:

### Resolution Order

1. **Check scope** — `AuthorizationCheck.Scope?.TenantId` is preferred when set by a resource descriptor.
2. **Ambient context** — `ITenantContext.TenantId` is used as fallback when check scope is null.
3. **Cerbos scope field** — The effective tenant ID populates the Cerbos resource `scope` field, enabling per-tenant policy overrides within a shared PDP.

### Override Strategy

| Tier | Policies | Override Behavior |
|---|---|---|
| Instance PDP | Static (disk) + Dynamic (DB store) | Baseline for all tenants |
| BYO PDP | Tenant-controlled | Full override — all checks route to tenant endpoint |
| Scoped policies (planned) | Per-tenant Cerbos scoped resources | Selective override — only matching scopes diverge |

### Contract Enforcement

JSON schemas in `cerbos/policies/_schemas/` enforce structural contracts across all tiers:

- **Principal schema** (`principal.json`): Validates `isInstanceAdmin`, `tenantMemberships`, `orgMemberships` on every check.
- **Resource schemas** (`{kind}.json`): Validate required attributes (e.g., `tenantId`, `actorId`) per resource kind.
- **Enforcement mode**: `warn` (logs validation errors without denying). Set to `reject` in production to force-deny malformed checks.
- **BYO alignment**: BYO PDPs should adopt the same schemas to maintain contract parity. Schema files are distributed alongside static policies.

## Claim Fallback Rules in Code

Preferred user ID extraction order across API and BFF paths:

- `sub` -> `ClaimTypes.NameIdentifier` -> `sid`

Notes:

- `internal_user_id` is a separate local-user claim added by BFF enrichment for UI/admin helpers. It is not the general fallback chain.
- A few BFF-only helpers currently stop at `sub` -> `ClaimTypes.NameIdentifier` where the server-authenticated session is already authoritative.

## Client-Side Authorization Scope

Blazor client checks are UX-only:

- route/menu/button visibility,
- reduced unauthorized UI paths.

They are not security enforcement. Security enforcement remains server-side through API and MediatR authorization.

## Blazor Auth-State Serialization Boundary

`Explore.Blazor` serializes only display-safe identity hints into the browser authentication state:

- allowed: display/name hints such as `name`, `preferred_username`, `given_name`, and `family_name`,
- excluded: `sub`, `sid`, `ClaimTypes.NameIdentifier`, `internal_user_id`, tenant identifiers, roles, permissions, admin claims, email, tokens, and any action-authority claims.

Browser-visible authorization, tenancy, feature access, and action affordances must come from BFF/API/HAL/status endpoints, not from serialized claims. Server-side claims may still enrich the BFF principal for API calls, token forwarding, setup flows, and server-only authorization decisions.

## Admin Claims Enrichment

`BffAdminClaimsTransformation`:

- calls API endpoint `api/User/admin-authority`,
- adds admin claims to the server-side BFF principal for server decisions and downstream API context,
- resolves and adds `internal_user_id` by matching external identity (`provider + provider subject`) to local user records,
- caches positive results for 5 minutes and negative results for 30 seconds.

These admin and internal-user claims are intentionally not serialized as browser authority. Blazor UI affordances must use BFF status endpoints, API/HAL `_links`, or other server-confirmed contracts.

Post-onboarding provider management safety:

- `GET/PUT /api/instance/settings/auth-provider` requires authenticated instance admin context.
- Authentication update flow denies requests that would disable all providers linked to the current admin account (self-lockout prevention).
- `GET/PUT /api/instance/settings/authz-provider` requires authenticated instance admin context and stores the selected runtime authorization provider.
- Authorization update flow permits exactly one active provider: local RBAC or Cerbos. Cerbos endpoint changes are verified before the setting is applied.
- If Cerbos is selected and unavailable, authorized requests fail closed. Recovery is an explicit operator action: switch the authorization provider setting back to local RBAC; the runtime does not silently fail over.

If enrichment fails, authentication still continues and server-side authorization remains authoritative.

## Security Headers (API)

`SecurityHeadersMiddleware` adds defensive headers to every response:

| Header | Value |
|---|---|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=()` |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` |

Non-GET responses additionally receive `Cache-Control: no-store` and `Pragma: no-cache` to prevent caching of mutation responses.

## CORS Policies

Five CORS policies are configured in `Program.cs`:

| Policy | Origins | Methods | Credentials | Use Case |
|---|---|---|---|---|
| `InternalAppPolicy` | Configurable | All | Yes | Internal app communication (BFF ↔ API) |
| `ExternalAppPolicy` | Configurable | Specific set | No | External API consumers |
| `InternalWebsitePolicy` | Configurable `Cors:AllowedOrigins` with default fallback | All | Yes | Internal website |
| `ExternalWebsitePolicy` | Configurable | `GET`, `OPTIONS` only | No | External read-only |
| `DevPolicy` | All origins | All | No | Development only |

## External API Keys

Non-interactive callers (direct API consumers, integrations, automation) authenticate with long-lived `X-API-Key` credentials instead of JWT bearer tokens. The security model is designed so credential material and principal authority are strictly separated.

### Credential / Principal Separation

- **Credential material** is a `{keyId}.{secret}` pair. The `keyId` is a stable, indexable identifier; the `secret` is a high-entropy random value that is *never* stored in plaintext.
- **Principal authority** is derived from the key row (`OwnerType`, `OwnerId`, `TenantId`, `Scopes`), **not** from the credential itself. Rotating a secret does not change authority. Revoking or reissuing a key does not change the owner identity.
- The credential is a lookup-and-verify token. The principal is reconstructed from the stored row on every request.

### Hashing

- Secrets are stored as SHA256 hashes in `ExternalApiKey.SecretHash`. The full plaintext is never persisted.
- Verification recomputes the SHA256 hash and uses `CryptographicOperations.FixedTimeEquals` to reduce timing-oracle risk.
- The raw `{keyId}.{secret}` value is returned once at creation time; losing it requires revoking and issuing a replacement key.

### Revocation And Replacement

- Current API surfaces support creating keys, updating key policy, and revoking keys. The lookup table contains a `PendingRotation` status for future overlap workflows, but the inspected API surface does not expose a dedicated rotate endpoint.
- Secret values are returned **once only**, at creation time, in the HTTP response body. The secret is then discarded server-side and cannot be re-derived.
- Clients that lose a secret must revoke the old key and issue a replacement — the platform cannot recover it.

### Raw Key Logging Prohibited

- The `X-API-Key` header value must never appear in logs, traces, or metrics.
- `ILogger` calls inside `ApiKeyAuthenticationHandler` log only `keyId` and outcome — the secret segment is discarded after parsing.
- Business metrics (`explore.external_api_keys.authentication_attempts`) tag `tenant_id`, `owner_type`, and `outcome`, but never the credential.
- Correlation IDs and request logs redact the `Authorization`/`X-API-Key` headers before emission.

### Tenant Isolation

- API key rows are tenant-scoped (`TenantId` FK) except for `InstanceAdmin` keys (nullable). Every non-auth query applies the `Tenant` query filter.
- API-key auth lookups are the **only API-key path** permitted to bypass the tenant filter — narrowly scoped to `GetByKeyIdForAuthentication` via `IgnoreTenantFilter`.
- `ApiTenantPostAuthenticationMiddleware` enforces that the API-key `TenantId` matches the resolved request tenant. Mismatches return `404 Not Found` (not `401` — to avoid leaking tenant existence).
- Tenant user authority is rooted in `TenantUserRoleGrant`, which must reference a matching `(TenantId, TenantUserId)` pair and a tenant-scoped role. Effective tenant-admin checks also require the linked `TenantUser` to be active and not soft-deleted.

### Scope Model

- Each key holds an explicit `Scopes` set (e.g., `events:read`, `admin:tenant`, `admin:instance`).
- Scopes are bounded by the owner type (`ExternalApiKeyScopeCeiling`): a `User`-owned key cannot hold `admin:tenant`, a `Tenant`-owned key cannot hold `admin:instance`. Attempts to create or update a key with out-of-ceiling scopes are rejected at validator level.
- Authorization evaluators apply scope gates before any owner-authority check (see `MachineScopeMapping.ScopesPermit`). A key with `events:read` alone cannot perform mutations regardless of owner authority.
- MCP scopes are deliberately narrow: `mcp:read` permits MCP AI-conversation/read discovery only, while `mcp:propose` is required for MCP proposal tools/prompts and permits proposal creation without granting event write, event confirmation, or arbitrary user-write authority. SDK authorization filters hide proposal tools from API keys that lack `mcp:propose`, and MediatR authorization still fail-closes the call path.

### Machine Principal

- `IMachinePrincipalAccessor` exposes the parsed `ApiKeyPrincipalContext` to both authorization providers for a uniform decision path.
- Cerbos principals synthesize `isInstanceAdmin`/`tenantMemberships`/`orgMemberships` from owner type; the local `FallbackAuthorizationService` applies symmetric logic so both backends emit identical decisions for identical calls.
- A machine principal never receives admin-enrichment claims (`is_system_admin`, `is_tenant_admin`, etc.) — those are reserved for interactive user flows. All authority derives from owner type + scopes.

## HATEOAS Authorization

The HATEOAS link generation system is authorization-aware:

1. **`HateoasAuthorizationEvaluator`** performs batch permission checks for all links in a response.
2. Static checks (authentication, role requirements, condition lambdas) run first.
3. Remaining links with `PermissionResourceKind` are batched into a single `IsAllowedBatchAsync()` call. Link identity includes resource kind, resource id, action, optional scope, and canonicalized attributes.
4. On batch authorization failure, all permission-bound links are **denied** (fail-closed).
5. Admin/sync controllers that manually build HAL responses run definitions through the same evaluator before materializing links.
6. Clients never see links they cannot execute and must not recreate action gates from local roles or claims.

Related authorization references:

- [AUTHORIZATION.md](AUTHORIZATION.md) — provider model, resource checks, and fallback behavior.
- [AUTHORIZATION_PATTERNS.md](AUTHORIZATION_PATTERNS.md) — handler/request authoring patterns.
- [API.md](API.md) — API authentication, API-key routing, and error contracts.
- [BLAZOR.md](BLAZOR.md) — BFF proxy/token/setup-secret boundaries.

## Row-Level Security (RLS) — Prototype Support

**Status:** Prototype tenant-session infrastructure exists; production table policies are not enabled yet.

**Current tenant isolation:** EF Core named query filters (`HasQueryFilter(name: "Tenant", ...)`) and tenant-safe database foreign keys are the current production enforcement layers. EF tenant filters now fail closed when `TenantContext` is missing; approved system/admin paths must opt in through an explicit bypass reason. RLS is still defense-in-depth work, not the authority for application authorization.

**Implemented prototype pieces:**
- `Explore.Persistence/Security/PostgresTenantSessionInterceptor.cs` sets PostgreSQL session setting `app.current_tenant_id` with `set_config(..., false)` whenever EF Core opens a connection.
- Runtime registration is disabled by default and guarded by `Persistence:EnableRlsTenantSession`.
- `Event.Persistence.IntegrationTests/TenantIsolation/PostgresTenantSessionRlsPrototypeTests.cs` proves a forced RLS policy filters tenant A, tenant B, and missing-tenant access through a non-superuser app-style role.
- No production migration currently enables RLS on tenant tables.

**Why RLS matters for defense-in-depth:**
- Direct database access (migrations, reporting, debugging, data exports) bypasses EF query filters.
- A compromised application layer could disable filters and leak cross-tenant data.
- PostgreSQL RLS adds kernel-level row filtering that cannot be bypassed from SQL.

**Policy pattern proven by the prototype:**

```sql
CREATE POLICY tenant_isolation ON events
    USING (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
    WITH CHECK (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
```

The `missing_ok=true` form plus `NULLIF(..., '')` makes absent tenant context fail closed instead of raising a cast error. The interceptor sets an empty string when `ExploreDbContext.TenantContext` is missing or returns `Guid.Empty`.

**Production rollout prerequisites:**
1. Use a non-superuser, non-`BYPASSRLS` application database role. PostgreSQL superusers always bypass RLS, even when a table uses `FORCE ROW LEVEL SECURITY`.
2. Keep migration/maintenance credentials separate from the runtime app role so migrations and operator maintenance can intentionally bypass RLS.
3. Audit all direct `IDbContextFactory<ExploreDbContext>` callers and system/admin paths before enabling policies on real tables; factory-created contexts do not automatically receive scoped property injection.
4. Enable RLS table families in bounded migrations with integration tests for tenant access, absent-tenant denial, cross-tenant denial, and required host-admin/system paths.
5. Apply first to high-value tenant tables such as events, event_sessions, organizations, groups, actors, event_registrations, storage_objects, audit_logs, notifications, configuration_change_logs, tenant_user_role_grants, tenant_setting_overrides, and tenant_settings_documents.

**Risks:**
- Connection pooling: Session variables must be set every time EF opens a connection. Npgsql resets pooled connection state on close by default, and the interceptor rebinds the tenant on open.
- Role design: A superuser or `BYPASSRLS` runtime connection makes policies ineffective.
- System/admin reads: cross-tenant maintenance paths need explicit role/session design before real table policies are enabled.
- Performance: RLS adds a predicate to every query. Indexes on `tenant_id` (already exist) mitigate this.
- Migrations: Must run with a maintenance role that bypasses RLS intentionally.
