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
2. Application MediatR pipeline `AuthorizationBehavior`.
3. Runtime provider (`RuntimeAuthorizationProvider`) deciding Cerbos vs fallback.

Hard deny behavior:

- `AuthorizationBehavior` throws `AuthorizationException` on deny.
- API global exception handler returns HTTP `403 Forbidden`.

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
