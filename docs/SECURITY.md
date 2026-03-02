ABOUTME: Describes authentication, authorization, and trust boundaries for the platform.
ABOUTME: Focuses on enforced behavior in code (BFF, MediatR authorization, and fallback modes).

# Security

## Security Model

The platform uses a BFF model:

- `Explore.Blazor` (server) handles OIDC and session cookies.
- `Explore.Blazor.Client` (WASM) does not directly manage access tokens.
- `Explore.API` authorizes bearer-token requests and applies resource-level checks in Application layer.

## Authentication Flow (Current)

1. User authenticates through BFF OpenID Connect flow.
2. BFF stores auth session in cookie.
3. Calls to `/api/*` are proxied by YARP from BFF to API.
4. BFF adds bearer token to proxied API requests (`YarpProxyExtensions.ForwardBearerTokenAsync`).

## JWT Bearer Configuration (API)

- Authority: Keycloak OIDC metadata endpoint.
- Multi-client audience validation: `explore-api`, `explore-blazor-server`, `account`.
- Custom `AudienceValidator`: checks both `aud` claim and `azp` (Keycloak authorized party) claim. Accepts if either contains a valid audience.
- Clock skew tolerance: 5 minutes.
- Dev mode: accepts self-signed certificates, suppresses HTTPS metadata requirement.
- Detailed JWT event logging on: `OnAuthenticationFailed`, `OnTokenValidated`, `OnChallenge`, `OnMessageReceived`.

## Header and Secret Hardening

In YARP transforms:

- `X-Tenant-Id` is forwarded from incoming request when present.
- Incoming `X-Setup-Secret` is stripped first, then replaced only with trusted value resolved from:
  1. request header,
  2. cookie,
  3. server-side session (per user).

This prevents direct client injection of setup-secret into proxied API traffic.

## Authorization Boundary

Server-side enforcement is layered:

1. API endpoint-level attributes (`[AllowAnonymous]`, `[Authorize]`).
2. Application MediatR pipeline `AuthorizationBehavior`:
   - Checks `IAuthorizedRequest` interface — commands/queries declare required permissions.
   - Checks `[AuthorizeResource]` attribute — declarative resource-level authorization.
   - Optionally enhanced by `ISecureRequest` — provides dynamic resource context for fine-grained permission evaluation.
3. Runtime provider (`RuntimeAuthorizationProvider`) deciding Cerbos vs fallback.

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

- Instance Cerbos failure falls back to local RBAC provider.
- BYO Cerbos:
  - `failure_mode=closed` -> fallback `SafeMode` (deny all except instance admin path).
  - `failure_mode=open` -> standard local RBAC fallback.

## Claim Fallback Rules in Code

Common user ID extraction order used in API/BFF paths:

- `sub` -> `ClaimTypes.NameIdentifier` -> `sid` (used in several API controllers and BFF admin-claims transformation).

Some BFF helpers currently use:

- `sub` -> `ClaimTypes.NameIdentifier` (without `sid` fallback).

## Client-Side Authorization Scope

Blazor client checks are UX-only:

- route/menu/button visibility,
- reduced unauthorized UI paths.

They are not security enforcement. Security enforcement remains server-side through API and MediatR authorization.

## Admin Claims Enrichment

`BffAdminClaimsTransformation`:

- calls API endpoint `api/User/admin-authority`,
- adds admin claims to principal for UI use,
- caches positive results for 5 minutes and negative results for 30 seconds.

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
| `InternalWebsitePolicy` | `iloveibadah.app` only | All | Yes | Internal website |
| `ExternalWebsitePolicy` | Configurable | `GET`, `OPTIONS` only | No | External read-only |
| `DevPolicy` | All origins | All | Yes | Development only |

## HATEOAS Authorization

The HATEOAS link generation system is authorization-aware:

1. **`HateoasAuthorizationEvaluator`** performs batch permission checks for all links in a response.
2. Static checks (authentication, role requirements, condition lambdas) run first.
3. Remaining links with `PermissionResourceKind` are batched into a single `IsAllowedBatchAsync()` call.
4. On batch authorization failure, all permission-bound links are **denied** (fail-closed).
5. This ensures clients never see links they cannot execute.
