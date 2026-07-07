ABOUTME: Describes authentication, authorization, and trust boundaries for the platform.
ABOUTME: Focuses on enforced behavior in code (BFF, MediatR authorization, and fallback modes).

# Security

> **Audience:** Operators | Contributors | AI agents
> **Status:** Mixed
> **Owner:** Security
> **Last Verified:** 2026-07-04
> **Source Anchors:** `Explore.API/Extensions/AuthenticationExtensions.cs`, `Explore.API/Extensions/CorsExtensions.cs`, `Explore.Blazor/Extensions/YarpProxyExtensions.cs`, `Event.Web.BffHosting/Authentication/EventBffAuthenticationExtensions.cs`, `Event.Web.BffHosting/Proxy/EventApiProxyExtensions.cs`, `Event.ControlPlane.Blazor/Program.cs`, `Explore.Application/Services/ApiKeyHashing.cs`, `Explore.Application/Telemetry/BusinessMetrics.cs`, `Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs`, `Explore.Infrastructure/Services/FallbackAuthorizationService.cs`, `docs/AUTHORIZATION.md`

## Security Model

The platform uses a BFF model:

- `Explore.Blazor` (server) handles OIDC and session cookies.
- `Event.ControlPlane.Blazor` uses the same shared BFF hosting primitives for the separate self-hostable control-plane host, but with a dedicated Keycloak confidential client, a separate cookie name, and a coarse instance-admin-only control-plane policy.
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

1. User authenticates through a browser BFF OpenID Connect flow.
2. The BFF stores the auth session in an HttpOnly cookie.
3. Calls to `/api/*` are proxied by YARP from the BFF to the API.
4. The BFF adds the server-held bearer token to proxied API requests through the shared BFF hosting token-forwarding path.
5. `Event.ControlPlane.Blazor` uses the same flow with the `islamu-event-control-plane` Keycloak confidential client. It must not support setup-secret, API-key, or browser-stored bearer-token login for operators.

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

Browser-facing BFF hosts emit security headers before HTML, error, static asset, and proxied responses are written:

- `Content-Security-Policy` keeps scripts self-hosted with the Blazor WebAssembly runtime allowances, blocks framing with `frame-ancestors 'none'`, limits forms to `self`, allows the documented Google font endpoints, and allows `data:`, `https:`, and `blob:` images so event images and browser-generated downloads keep working.
- `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, and `Referrer-Policy: strict-origin-when-cross-origin` are set at the BFF boundary.
- `Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=()` disables browser capabilities that are not part of the launch surface.

In YARP transforms:

- `X-Tenant-Slug` is forwarded when route or request context provides an explicit tenant hint.
- Browser-supplied `X-Support-Access-*` headers are stripped before proxying. The BFF may add `X-Support-Access-Session-Id` only from server-owned support-access session storage after the authenticated actor has an active session bound to the current OIDC session.
- Any incoming or stale proxied `X-Setup-Secret` header is removed first. The BFF then resolves a setup secret through `ISetupSecretResolver` in this source order:
  1. BFF-owned setup handshake/session state,
  2. protected BFF-issued setup cookie,
  3. explicit local/development/bootstrap configuration fallback.
- Inbound request headers are never trusted as setup-secret sources. Browser-controlled `X-Setup-Secret` values must be stripped and ignored by both YARP and server-side forwarding handlers.

This prevents stale outgoing proxy headers and browser-controlled privileged headers from leaking across requests. Treat the setup secret as bootstrap-only sensitive material; the BFF protects the setup cookie with ASP.NET Core Data Protection and forwards only resolver output to downstream API calls.

## Control Plane BFF Boundary

The separate control-plane app is a browser BFF, not a native client and not a management API. Its security boundary is:

- `Event.ControlPlane.Blazor` authenticates through Keycloak OIDC Authorization Code flow plus PKCE using a dedicated confidential client.
- The control-plane client secret is server-side only through environment variables, appsettings, user secrets, or Infisical-compatible startup loading.
- The separate host is Interactive Server-only: it registers only the server component render mode and applies `@rendermode="InteractiveServer"` to `HeadOutlet` and `Routes`.
- InteractiveAuto and WebAssembly render modes must not be added to `Event.ControlPlane.Blazor`; privileged control-plane features must not ship as a WASM client bundle.
- The browser receives only the HttpOnly BFF session cookie and display-safe page payloads. It must not receive access tokens, refresh tokens, client secrets, setup secrets, API keys, instance-admin authority claims, or raw OIDC diagnostics.
- The BFF enforces a coarse `ControlPlaneAccess` policy before rendering the shell, but `Explore.API` and Application/MediatR authorization remain the authoritative boundary for every control-plane action.
- Control-plane UI affordances must still come from API/HAL `_links` or server-confirmed status endpoints. Local claim checks are UX hints only and must not unlock actions.
- Browser-supplied privileged headers are stripped before proxying. Trusted tenant hints, setup-secret forwarding, and support-access forwarding remain server-owned BFF adapter decisions.
- The existing `Explore.Blazor` BFF can also render an embedded control-plane shell on hosts configured in `Bff:AdminHosts`. Optional `Bff:AdminHostAllowedIpRanges` restricts those admin hosts by IP/CIDR, and configured admin hosts are excluded from tenant custom-domain/subdomain resolution. This host classification is routing and shell selection only; it is not a replacement for instance-admin authorization, HAL affordance gating, or API/Application checks.

A separate control-plane UI host does not guarantee operational rescue if the shared API, database, or reverse proxy is saturated. Reserved-resource management APIs/workers are a future management-plane concern, not something implied by this BFF split.

## Support Access Trust Boundary

Admin support access is a persisted, time-boxed support session, not an impersonation cookie or tenant role grant.

- The real actor identity remains authoritative. `ICurrentUserService.UserId` continues to identify the authenticated instance admin; support metadata lives separately in `ISupportAccessContext`.
- The BFF stores only an opaque support-access session reference in server-side distributed cache, keyed to the authenticated user and OIDC `sid`. The browser does not receive access tokens, target-tenant role claims, or support-access authority claims.
- Runtime support context is explicit-header-only. Ordinary API requests without a BFF/server-injected `X-Support-Access-Session-Id` are treated as inactive even if the actor has a persisted active session.
- `SupportAccessSessionService` validates the forwarded session against persisted state, actor id, resolved tenant id, expiry, mode, and instance governance settings. Disabled support access, missing sessions, stopped sessions, expired sessions, actor mismatch, tenant mismatch, and write-mode-disabled sessions fail closed.
- Support access never creates `TenantUserRoleGrant` rows and never replaces tenant membership. Resource authorization must continue through MediatR, the runtime authorization provider, Cerbos/local fallback parity, and HAL link filtering.
- `SupportAccessAuditMiddleware` records bounded API request evidence for active support sessions after authorization. It captures method, route pattern/name, status/outcome, correlation id, trace id, actor, target tenant, and session id, without raw request bodies, cookies, tokens, provider responses, or unbounded reason text.
- Tenant-facing support-access evidence is read-only. The Blazor tenant settings view resolves the current tenant through the BFF/API status path and renders audit drill-in only from the API/HAL `audit-events` link.
- Audit persistence failures are warning-level operational events and do not change the original API response; security-sensitive lifecycle events still belong in the support-access command transaction where the command handler creates the session/audit records.

## Incoming Webhook Public Ingestion

Some machine callbacks are intentionally anonymous because the provider signature is the authentication boundary. The Svix operational callback at `POST /api/integrations/svix/operational` is one of these public-ingestion exceptions.

- Verify signatures over the raw body before parsing JSON. For Svix-compatible callbacks, verification uses `svix-id`, `svix-timestamp`, and `svix-signature` with a bounded timestamp tolerance and fixed-time signature comparison.
- Enforce a configured body-size limit before dispatching to provider-specific verification or Application commands.
- Treat the provider message ID as the replay/idempotency key. Duplicate verified deliveries are acknowledged but must not re-run side effects.
- Persist only the durable idempotency ledger fields needed for processing: tenant binding when present, provider name, provider message ID, idempotency key, event type, payload hash, redacted headers, and bounded status/failure metadata.
- ProblemDetails, logs, metrics, and traces must not include raw callback bodies, signature headers, authorization headers, secrets, tokens, tenant/user identifiers, provider message IDs, or raw verification exceptions.

## Outgoing Webhook Egress

Outgoing LocalProvider endpoints are user-configured URLs, so delivery is an SSRF-sensitive egress boundary.

- Block loopback, localhost, RFC1918/private, link-local, metadata, and internal DNS destinations by default.
- Allow private CIDRs only through explicit operator configuration for deliberate self-hosted/internal delivery.
- Disable redirects and use bounded connect/request timeouts.
- Sign LocalProvider requests with Svix-compatible `svix-id`, `svix-timestamp`, and `svix-signature` headers over the raw body.
- Store endpoint signing material through secret refs and rotate through the endpoint rotation route; never return old or current secret material after the allowed one-time reveal path.
- Health checks and metrics may report provider mode, queue counts, bounded failure categories, and secret-resolution booleans only. They must not expose endpoint URLs, query strings, payload JSON, secret refs, tokens, authorization headers, full responses, or raw transport exceptions.

SvixProvider keeps the Svix API token server-side behind `webhooks.svix.auth_token`. App Portal URLs are generated by the backend and are short-lived; browser clients never receive the Svix API token.

## Event Report And Moderation Privacy

Event reporting separates reporter-facing status from moderator-facing review workflows.

- Public report options are content-light and anonymous only for published-event reportability discovery.
- Report submission and `my reports` reads require the authenticated current user. Reporter-facing responses contain status/reason/event/timestamp/contact-consent metadata only; they exclude evidence text, reporter hashes, provider workflow data, moderation cases, decisions, signals, and internal notes.
- Moderator queue/detail reads and moderation actions require event-resource authorization before handlers load or mutate report state.
- Moderator projections remain data-minimized even for authorized management callers. They expose workflow state, reason/priority/status, current case, authorized evidence text on detail reads, safe signal summaries, provider type, sync state, retry counts, and HAL action affordances. They do not expose stable reporter user/actor identifiers, evidence creator identifiers, decision moderator identifiers, raw provider case/signal identifiers, provider URLs, provider correlation identifiers, reporter fingerprints, raw provider payloads, raw provider errors, or unsafe notes.
- Blazor and other clients must use HAL rels (`report-event`, `moderation-reports`, `triage-report`, `assign-report`, `decide-report`, and `execute-report-decision`) as the action affordance source instead of local role/claim checks.
- Managed reporting routing uses the same privacy boundary. Tenant routing-state, tenant dashboard, and control-plane operations surfaces expose only redacted provider target identifiers, configured flags, aggregate queue/provider-sync counts, and server-emitted HAL actions (`routing-state`, `edit`, `test-osprey-provider`, `test-coop-provider`). Raw endpoint URLs, API keys, webhook secrets, provider payloads, callback signatures, raw provider errors, report evidence, and tenant lists are never returned by read DTOs, HAL resources, generated response models, logs, telemetry, or disabled-state text. Provider test actions are readiness checks only; they do not dispatch external HTTP requests.

## Event Registration Privacy

Event registration reads are self-service by default. Attendee identity is not a generic event-registration read concern.

- Generic registration list, registration detail, and by-session reads require the authenticated current user and return only registration rows owned by that user.
- `GET /api/eventregistration/by-user/{userId}` is self-only. A route user id that does not match the authenticated current user returns `403 Forbidden` before MediatR dispatch.
- Client/API registration DTOs must not serialize registrant user ids, full names, or email addresses. A server-only `UserId` may remain on Application DTOs only when hidden from JSON and used for internal authorization/HAL context.
- Organizer or admin attendee-management workflows need a separate resource-authorized management projection before exposing attendee identity. Do not reuse self-read DTOs or anonymous/public event projections for attendee rosters.

## BFF Antiforgery Contract

Cookie-authenticated unsafe BFF endpoints must validate antiforgery tokens because browsers automatically include BFF cookies on same-site requests.

- Token issuance: `UseAntiforgeryTokenMiddleware` calls `IAntiforgery.GetAndStoreTokens` on `GET` requests and writes the request token to the readable `XSRF-TOKEN` cookie.
- Header contract: clients send the token back in the `X-CSRF-TOKEN` header. This matches the BFF `AddAntiforgery` configuration.
- Browser client path: `BrowserCredentialsMessageHandler` attaches browser credentials and adds `X-CSRF-TOKEN` for `POST`, `PUT`, `PATCH`, and `DELETE` requests.
- Server self-call path: `BffCookieForwardingHandler` forwards captured cookies and mirrors `XSRF-TOKEN` into `X-CSRF-TOKEN` when InteractiveServer code calls BFF endpoints.
- Endpoint validation: unsafe minimal BFF endpoints call `.ValidateAntiforgery()`, which returns `400 Antiforgery validation failed` for missing or invalid tokens.
- Protected endpoint families include auth refresh, support-access start/stop, storage upload session/proxy, preference mutations, and appearance profile mutations.
- InteractiveServer storage upload self-calls use a short-lived Data Protection protected `X-ISLAMU-BFF-SELF-CALL` token bound to method, path, host, and authenticated user. That token lets same-process server calls satisfy the same endpoint filter without turning browser-originated storage uploads into an antiforgery exception.
- Documented exceptions are setup-secret bootstrap endpoints and `/bff/auth/refresh-session/internal`; these remain constrained by setup credentials, server-owned setup/session state, authorization where applicable, and rate limiting because they run before or outside normal browser antiforgery semantics.

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

## Storage Object Download Boundary

Storage download access uses stable storage object IDs, never browser-supplied provider keys or local paths. Metadata/list/detail routes are authenticated and authorized with `islamuevent_storage_object:view`; content streaming uses `download`; presigned URL generation uses `presigned_download`. The dedicated anonymous route is `GET /api/storageobject/{id}/public`, which is limited by the storage content reader to active `public_image` objects.

Presigned URLs are bearer credentials. API responses containing them must not be output-cached, must send no-store cache metadata, and must not log the URL, signature, token, object key, bucket path, or raw provider error. The presigned response intentionally keeps `ObjectKey` empty; consumers must treat the returned URL as short-lived secret material.

## Email Dispatch Operator Boundary

EmailDispatch status and delivery controls are operational APIs, not general tenant data reads. `GET /api/admin/email-dispatch/status`, tenant pause/resume, park, and replay all require authentication plus MediatR resource authorization against `islamuevent_email_dispatch`. Status uses `view`, tenant pause/resume uses `manage_tenant`, parking uses `park`, and replay uses `replay`.

Only tenant administrators for the resolved tenant and instance administrators should receive these operator decisions from Cerbos or local fallback. Regular authenticated users must receive `403 Forbidden`. The status projection must stay sanitized: no recipient email, subject, plain text or HTML body, reply-to, provider message id, raw SMTP/provider error, object key, token, or secret-derived metadata. HAL `replay` and `park` links are the only client affordance source for row-level controls.

Dispatch-time unsubscribe handling is part of the same boundary. The worker checks persisted `UserNotificationPreference` after claiming a row and before SMTP handoff for mapped lifecycle categories; opted-out rows become terminal `Skipped` outcomes instead of provider failures or retries. Outgoing email may contain opaque unsubscribe tokens in `List-Unsubscribe` headers and body links, but admin status APIs, logs, metrics, and health details must not expose those tokens or rendered message bodies. `Skipped` rows are terminal and must not receive replay or park HAL affordances.

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
3. **Cerbos scope field** — The effective tenant ID populates the Cerbos resource `scope` field only when `Cerbos:UsePolicyScope=true`, enabling per-tenant policy overrides within a shared PDP.

By default, runtime HATEOAS checks keep tenant context in resource attributes and do not set Cerbos resource scope. If `Cerbos:UsePolicyScope=true` is enabled, the instance Cerbos PDP must run with `engine.lenientScopeSearch=true` and have a complete scoped-policy chain; otherwise Cerbos can return missing decisions and permission-bound HAL links fail closed.

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

- API key rows are tenant-scoped (`TenantId` FK) except for `InstanceAdmin` keys, whose credential row is nullable because it belongs to the platform operator rather than one tenant. Every non-auth query applies the `Tenant` query filter.
- API-key auth lookups are the **only API-key path** permitted to bypass the tenant filter — narrowly scoped to `GetByKeyIdForAuthentication` via `IgnoreTenantFilter`.
- `ApiTenantPostAuthenticationMiddleware` enforces that the API-key `TenantId` matches the resolved request tenant. Mismatches return `404 Not Found` (not `401` — to avoid leaking tenant existence).
- `InstanceAdmin` API keys do not implicitly make tenant-scoped API/MCP execution tenantless. If the request carries an explicit tenant hint, post-auth middleware binds that tenant for the request. If a tenant-scoped API or MCP request has no resolved tenant, it fails closed with `404` and `code=tenant_required`. Only explicit host-administration API routes may continue without tenant context.
- Tenant user authority is rooted in `TenantUserRoleGrant`, which must reference a matching `(TenantId, TenantUserId)` pair and a tenant-scoped role. Effective tenant-admin checks also require the linked `TenantUser` to be active and not soft-deleted.
- Organization membership reads are administrative, identity-bearing resources. `OrganizationMemberDto` includes tenant, organization, user, email/name, role, and position data; list/detail routes therefore require authenticated `islamuevent_organization_member:view` authorization and are denied to regular authenticated users. Do not reuse this DTO for public organization profile display; add a separate safe public projection if product requirements later need anonymous member/profile data.
- Footer management writes are tenant-administration actions. The API must resolve the current user and current tenant before dispatch, then footer link-group/link/reorder/settings commands authorize as `ResourceKinds.Tenant` with `AuthorizationActions.Update` and tenant attributes. A missing actor fails as authentication required; a signed-in user without tenant-admin or instance-admin authority receives `403 Forbidden`.

### Scope Model

- Each key holds an explicit `Scopes` set (e.g., `events:read`, `admin:tenant`, `admin:instance`).
- Scopes are bounded by the owner type (`ExternalApiKeyScopeCeiling`): a `User`-owned key cannot hold `admin:tenant`, a `Tenant`-owned key cannot hold `admin:instance`. Attempts to create or update a key with out-of-ceiling scopes are rejected at validator level.
- Authorization evaluators apply scope gates before any owner-authority check (see `MachineScopeMapping.ScopesPermit`). A key with `events:read` alone cannot perform mutations regardless of owner authority.
- MCP scopes are deliberately narrow: `mcp:read` permits generic MCP read discovery, while private event-management MCP reads also require the existing event read scope gate (`events:read`, `events:write`, or tenant/admin equivalent accepted by `MachineScopeMapping`). `mcp:propose` is required for MCP proposal tools/prompts and permits proposal creation without granting event write, event confirmation, or arbitrary user-write authority. SDK authorization filters hide event-management reads from API keys that only have `mcp:read`, hide proposal tools from API keys that lack `mcp:propose`, and MediatR authorization still fail-closes the call path.

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

Event moderation links follow the same rule. The active moderation affordances are `moderate-light`, `moderate-heavy`, and eligible `unmoderate`; clients must render them only from HAL `_links`, never from local admin-role checks. Instance and tenant administrators can receive moderation links without receiving event edit/update/delete links. Heavy redaction is irreversible, redacts event-owned text, detaches event images, deletes provider-backed objects through the storage abstraction, and sends generic attendee notifications without event identity. Unmoderation is limited to the latest reversible light-moderation record.

Moderated events remain hidden from public discovery and exact public event URLs. Authorized management detail, actor-profile management lists, and moderation-history reads use the event `view-management` action. The moderation-history API and moderation telemetry are safe metadata surfaces only: they must not include original titles, descriptions, slugs, URLs, image identifiers, object keys, storage paths, bucket names, provider endpoints, raw provider errors, or arbitrary moderator free text.

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
