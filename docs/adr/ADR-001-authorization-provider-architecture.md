ABOUTME: ADR for authorization provider architecture in the current codebase.
ABOUTME: Captures runtime provider routing, HTTP transport choice, and fallback semantics.

# ADR-001: Authorization Provider Architecture

**Status:** Accepted  
**Date:** 2026-02-14  
**Deciders:** ISLAMU Event Core Team

## Context

The platform needs resource-level authorization that:
- works with or without Cerbos running,
- supports tenant-specific BYO Cerbos,
- fails safely when PDP calls fail.

## Decision

Use one runtime wrapper (`RuntimeAuthorizationProvider`) that delegates to:
- `CerbosAuthorizationService` (HTTP PDP checks),
- `FallbackAuthorizationService` (local DB-backed authorization).

Provider resolution order:
1. tenant BYO Cerbos config (if present),
2. otherwise instance-level `AuthorizationProvider` setting (`"cerbos"` or local default),
3. fallback to local provider on Cerbos failure.

## HTTP Transport And Resilience

Cerbos communication uses HTTP clients:
- instance PDP client: `CerbosClient`
- tenant BYO client: `CerbosByoClient`

Configured resilience:
- instance: timeout 2s, circuit-breaker 50% failure ratio, 30s sampling, min throughput 10, break 15s.
- BYO: timeout 3s, circuit-breaker 50% failure ratio, 30s sampling, min throughput 5, break 15s.
- no retry policy is configured for authorization checks.

## Failure Mode Contract

When BYO Cerbos fails:
- `failure_mode=closed` -> local provider enters `SafeMode` (deny except explicit safe paths like instance-admin checks).
- `failure_mode=open` -> local provider runs normal fallback authorization.

When instance Cerbos fails:
- runtime provider logs and falls back to local authorization.

## Consequences

1. Deployment can run in local-only mode (no Cerbos dependency at startup).
2. Instance admins can switch provider mode via settings without code changes.
3. Two authorization paths must be maintained and tested.
4. Local fallback cannot evaluate all advanced Cerbos policy semantics.

## Related

- [AUTHORIZATION_PATTERNS.md](../AUTHORIZATION_PATTERNS.md)
- [SECURITY.md](../SECURITY.md)
- [DEPLOYMENT_TIERS.md](../DEPLOYMENT_TIERS.md)
