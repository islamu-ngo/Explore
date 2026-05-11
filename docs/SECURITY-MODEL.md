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
- Documented exceptions are setup-secret bootstrap endpoints and `/bff/auth/refresh-session/internal`; these remain constrained by setup credentials, authorization, and rate limiting because they are used before or outside normal browser antiforgery semantics.

Do not add new unsafe `/bff/*` endpoints without either `.ValidateAntiforgery()` or a documented bootstrap/internal exception with equivalent compensating controls.

## Storage Upload Destination Binding

The Blazor BFF upload proxy is an SSRF-sensitive boundary because it performs server-side `PUT` requests to a URL associated with user-uploaded content. Browser callers must not control that destination directly.

- Browser upload flow starts with `/bff/storage/upload-session`. The BFF obtains the presigned upload URL from the API, validates that it is an HTTPS presigned destination with required signing markers, and stores the exact approved destination in distributed cache under an opaque upload session id.
- `/bff/storage/upload-proxy` accepts `uploadSessionId`, `contentType`, and `file`, not a trusted raw `uploadUrl`. It resolves the upload session server-side and rejects missing, expired, cross-user, content-type-mismatched, or unknown sessions.
- Arbitrary HTTPS URLs, private/internal hosts, or presigned-looking attacker URLs must not be proxied merely because they contain S3-style query parameters.
- Upload sessions are short-lived, user-bound, content-type-bound, and consumed after successful upload. This keeps the browser path bound to a server-issued upload intent without duplicating tenant storage policy in the UI layer.
- BFF storage logs must not include raw upstream response bodies, presigned URLs, signatures, tokens, or object secrets. Use safe fields such as host, status code, `hasBody`, and session failure code.

Server-side/non-browser code paths may still use direct presigned upload URLs when the server owns the trusted URL. Browser-facing upload proxy paths must use the upload-session contract.

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
- BYO Cerbos:
  - `failure_mode=closed` -> fallback `SafeMode` (deny all except instance admin path).
  - `failure_mode=open` -> standard local RBAC fallback.

## Policy Topology

Authorization policies are organized in three tiers:

### Static Policies (Disk)

Static resource policy files plus `derived_roles.yaml` live in `cerbos/policies/`. Avoid relying on an exact count in docs; architecture tests and policy parity checks are the safer source of truth.

- **`derived_roles.yaml`**: Resolves instance admin, tenant admin, and org admin roles from principal attributes and resource context.
- **Resource policies** (`{kind}.yaml`): Each defines rules per derived role and `authenticated_user`. Instance admin gets wildcard `"*"`, tenant/org admin get CRUD, authenticated user gets `"view"`.
- **Standard actions**: `view`, `create`, `update`, `delete`.
- **Extended actions**: `manage_members`, `lock`, `unlock`, `viewsharedcontacts`, `exportsharedcontacts`, `sync_diff`, `sync_apply`.

### Dynamic Policies (PostgreSQL Store)

Custom role policies generated by `PolicySyncService` from `Role` and `RolePermission` tables:

- Format: Cerbos derived roles named `dynamic_{MasterCode}` with per-resource permission definitions.
- Pushed to Cerbos PostgreSQL store via Admin API, then broadcast reload to all instances.
- Triggered by custom role CRUD commands (`CreateCustomRole`, `UpdateRolePermissions`, `DeleteCustomRole`).

### BYO Cerbos (Per-Tenant Override)

Tenants may point to their own Cerbos PDP endpoint via tenant settings:

- Receives the same `AuthorizationCheck` payloads as the instance PDP.
- `AuthorizationCheck.Scope` enables per-tenant policy resolution within the BYO PDP.
- Failure modes: `closed` → safe-mode (deny all except instance admin), `open` → fallback to local RBAC.
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
| `custom_property_template.yaml` | `custom_property_template` | view, create, update, delete, sync_diff, sync_apply | Template CRUD + sync operations. Tenant admin can manage templates within their tenant. |
| `custom_property_value.yaml` | `custom_property_value` | view, create, update, delete | Runtime value CRUD. Org admin can manage values for their organization's entities. |
| `custom_property_projection.yaml` | `custom_property_projection` | view, update | Projection admin (rebuild, drain). Tenant admin can trigger rebuilds and drain dirty scopes. |
| `custom_property_governance.yaml` | `custom_property_governance` | view | Governance reporting. Tenant admin can view governance recommendations. |
| `platform_namespace.yaml` | `platform_namespace` | view, create, update, delete | Platform-reserved namespace protection. **Explicit deny** for tenant admin and org admin on write operations. Only instance admin can write. |

#### Endpoint-to-Policy Mapping

| Endpoint | Controller | Action | Resource Kind | Policy Rule |
|---|---|---|---|---|
| `GET /api/event/{id}/custom-property-definitions` | `EventCustomPropertyDefinitionController` | view | `custom_property_template` | AllowAnonymous |
| `POST /api/event/{id}/custom-property-definitions` | `EventCustomPropertyDefinitionController` | create | `custom_property_template` | Authorize |
| `PUT /api/event/{id}/custom-property-definitions/{defId}` | `EventCustomPropertyDefinitionController` | update | `custom_property_template` | Authorize |
| `DELETE /api/event/{id}/custom-property-definitions/{defId}` | `EventCustomPropertyDefinitionController` | delete | `custom_property_template` | Authorize |
| `GET /api/event/{id}/custom-property-values` | `EventCustomPropertyValueController` | view | `custom_property_value` | AllowAnonymous |
| `POST /api/event/{id}/custom-property-values` | `EventCustomPropertyValueController` | create | `custom_property_value` | Authorize |
| `PUT /api/event/{id}/custom-property-values/{valId}` | `EventCustomPropertyValueController` | update | `custom_property_value` | Authorize |
| `DELETE /api/event/{id}/custom-property-values/{valId}` | `EventCustomPropertyValueController` | delete | `custom_property_value` | Authorize |
| `POST /api/custom-property-projection/rebuild` | `CustomPropertyProjectionAdminController` | update | `custom_property_projection` | Authorize |
| `POST /api/custom-property-projection/drain` | `CustomPropertyProjectionAdminController` | update | `custom_property_projection` | Authorize |
| `GET /api/custom-property-projection/status` | `CustomPropertyProjectionAdminController` | view | `custom_property_projection` | Authorize |
| `GET /api/custom-property-governance/recommendations` | `CustomPropertyGovernanceController` | view | `custom_property_governance` | Authorize |

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
- Auth lookups are the **only** code path permitted to bypass the tenant filter — narrowly scoped to `GetByKeyIdForAuthentication` via `IgnoreTenantFilter`.
- `ApiTenantPostAuthenticationMiddleware` enforces that the API-key `TenantId` matches the resolved request tenant. Mismatches return `404 Not Found` (not `401` — to avoid leaking tenant existence).

### Scope Model

- Each key holds an explicit `Scopes` set (e.g., `events:read`, `admin:tenant`, `admin:instance`).
- Scopes are bounded by the owner type (`ExternalApiKeyScopeCeiling`): a `User`-owned key cannot hold `admin:tenant`, a `Tenant`-owned key cannot hold `admin:instance`. Attempts to create or update a key with out-of-ceiling scopes are rejected at validator level.
- Authorization evaluators apply scope gates before any owner-authority check (see `MachineScopeMapping.ScopesPermit`). A key with `events:read` alone cannot perform mutations regardless of owner authority.

### Machine Principal

- `IMachinePrincipalAccessor` exposes the parsed `ApiKeyPrincipalContext` to both authorization providers for a uniform decision path.
- Cerbos principals synthesize `isInstanceAdmin`/`tenantMemberships`/`orgMemberships` from owner type; the local `FallbackAuthorizationService` applies symmetric logic so both backends emit identical decisions for identical calls.
- A machine principal never receives admin-enrichment claims (`is_system_admin`, `is_tenant_admin`, etc.) — those are reserved for interactive user flows. All authority derives from owner type + scopes.

## HATEOAS Authorization

The HATEOAS link generation system is authorization-aware:

1. **`HateoasAuthorizationEvaluator`** performs batch permission checks for all links in a response.
2. Static checks (authentication, role requirements, condition lambdas) run first.
3. Remaining links with `PermissionResourceKind` are batched into a single `IsAllowedBatchAsync()` call.
4. On batch authorization failure, all permission-bound links are **denied** (fail-closed).
5. This ensures clients never see links they cannot execute.

Related authorization references:

- [AUTHORIZATION.md](AUTHORIZATION.md) — provider model, resource checks, and fallback behavior.
- [AUTHORIZATION_PATTERNS.md](AUTHORIZATION_PATTERNS.md) — handler/request authoring patterns.
- [API.md](API.md) — API authentication, API-key routing, and error contracts.
- [BLAZOR.md](BLAZOR.md) — BFF proxy/token/setup-secret boundaries.

## Row-Level Security (RLS) — Planned

**Status:** Not yet implemented. Strategy documented for post-v1.0.

**Current tenant isolation:** EF Core named query filters (`HasQueryFilter(name: "Tenant", ...)`) ensure all queries are tenant-scoped at the application layer. This is sufficient when all data access flows through the application.

**Why RLS matters for defense-in-depth:**
- Direct database access (migrations, reporting, debugging, data exports) bypasses EF query filters.
- A compromised application layer could disable filters and leak cross-tenant data.
- PostgreSQL RLS adds kernel-level row filtering that cannot be bypassed from SQL.

**Planned approach:**
1. Add a `current_tenant_id` session variable set via `SET app.current_tenant_id = '<guid>'` on each connection checkout.
2. Create RLS policies on all tenant-scoped tables: `CREATE POLICY tenant_isolation ON events USING (tenant_id = current_setting('app.current_tenant_id')::uuid)`.
3. Apply to: events, event_sessions, organization, groups, actors, event_registrations, storage_objects, audit_logs, notifications, configuration_change_logs, tenant_members, tenant_settings.
4. The EF DbContext connection interceptor sets the session variable before first query.
5. Superadmin connections use `SET ROLE` to bypass RLS for cross-tenant operations.

**Risks:**
- Connection pooling: Session variables must be set per checkout, not per pool. Npgsql connection interceptors handle this.
- Performance: RLS adds a predicate to every query. Indexes on `tenant_id` (already exist) mitigate this.
- Migrations: Must run with a superuser role that bypasses RLS.
