ABOUTME: ADR for the Phase 0 external API authentication and tenant-resolution seam.
ABOUTME: Captures the split-phase request flow implemented in Explore.API for direct JWT and API-key callers.

# ADR: Phase 0 Authentication And Tenant Request Flow

> **Last Updated:** 2026-03-08

## Status

Accepted for the Phase 0 spike.

## Context

The API now needs to support two direct-consumer authentication shapes without abandoning API-authoritative tenant resolution:

- direct JWT callers, where tenant context comes from request material
- API-key callers, where tenant context comes from the key binding

The existing API pipeline already resolved tenant before authentication. That works for JWT and BFF traffic, but it cannot blindly remain the authority path for API-key callers because a raw caller-provided slug or host hint must not become tenant authority for machine credentials.

## Decision

The Phase 0 seam uses split-phase tenant handling with explicit auth dispatch.

### Authentication strategy

- Default auth scheme: policy scheme `MultiAuth`
- JWT scheme: `Bearer`
- API-key scheme: `ApiKey`
- Machine-auth header: `X-API-Key`

Dispatch rule:

1. if `X-API-Key` is present, forward to `ApiKey`
2. otherwise, forward to `Bearer`

`Authorization: Bearer` remains JWT-only.

### Tenant handling strategy

#### Phase A: pre-auth request context

`ApiTenantResolutionMiddleware` remains authoritative for normal API request tenant discovery.

- non-API requests are ignored
- already-resolved tenant context is preserved
- `SingleTenant` resolves immediately to the configured default tenant
- `MultiTenant` JWT-style requests resolve tenant from:
  1. `X-Tenant-Slug`
  2. custom domain
  3. subdomain
  4. unresolved -> `404 Tenant not resolved`

For API-key requests:

- the middleware still reads slug or host hints
- it stores a resolved hint in `HttpContext.Items["__requested_tenant_id"]`
- it does **not** set `ITenantContextAccessor` from that hint
- unresolved API-key requests do **not** fail before authentication

This preserves fail-closed behavior for JWT traffic while allowing API-key traffic to authenticate first.

#### Phase B: authentication

- `Bearer` authenticates JWT callers
- `ApiKeyAuthenticationHandler` authenticates `X-API-Key`
- successful API-key auth emits claims for:
  - auth method
  - api key id
  - tenant id
  - owner type
  - owner id
  - repeated scope claims

#### Phase C: post-auth tenant validation

`ApiTenantPostAuthenticationMiddleware` runs after `UseAuthentication()`.

- if an authenticated API-key tenant claim conflicts with the stored requested tenant hint, return `404 Tenant mismatch`
- if no tenant is resolved yet and API-key auth provides tenant claim, set `ITenantContextAccessor` from the authenticated claim
- if `X-API-Key` was sent but authentication does not yield a tenant, return `401 API key authentication failed`
- if a resolved tenant and authenticated tenant claim disagree, return `404 Tenant mismatch`

## Consequences

### Positive

- API keys do not trust raw caller tenant hints as authority
- direct JWT callers still follow the documented explicit tenant contract
- `SingleTenant` stays abstracted away from callers
- the seam is testable with a small internal probe endpoint

### Negative

- the pipeline is more complex than a single tenant middleware
- API-key hint mismatch now depends on temporary `HttpContext.Items` state in Phase 0
- Swagger/OpenAPI documentation still needs a future pass for the dual-auth surface

## Verification Target For Phase 0

The seam should demonstrate:

1. direct JWT + explicit tenant slug -> authenticated request with resolved tenant
2. API key without tenant slug -> authenticated request with tenant derived from key
3. API key + conflicting tenant slug -> `404`
4. single-tenant JWT without tenant material -> authenticated request using default tenant
