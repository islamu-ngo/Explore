ABOUTME: ADR for authorization provider architecture in the current codebase.
ABOUTME: Captures runtime provider routing, gRPC SDK transport choice, and fallback semantics.

# ADR-001: Authorization Provider Architecture

**Status:** Accepted  
**Date:** 2026-02-14  
**Updated:** 2026-02-24 (Migrated from HTTP REST to official Cerbos gRPC SDK)  
**Deciders:** ISLAMU Event Core Team

## Context

The platform needs resource-level authorization that:
- works with or without Cerbos running,
- supports tenant-specific BYO Cerbos,
- fails safely when PDP calls fail.

## Decision

Use one runtime wrapper (`RuntimeAuthorizationProvider`) that delegates to:
- `CerbosAuthorizationService` (gRPC PDP checks via official `Cerbos.Sdk`),
- `FallbackAuthorizationService` (local DB-backed authorization).

Provider resolution order:
1. tenant BYO Cerbos config (if present),
2. handler-owned local check bypasses (`user:update` self-service, `event:create` pre-create, `organization:create` pre-create, `event_session:create` pre-create, `ai_conversation` route directly to `FallbackAuthorizationService` to ensure PDP package latency cannot block self-service),
3. otherwise instance-level `AuthorizationProvider` setting (`"cerbos"` or local default).

When `authorization.provider=cerbos`: all-in on Cerbos PDP for non-bypassed checks. If Cerbos is down, deny all (fail-closed).
When `authorization.provider=local`: use `FallbackAuthorizationService` exclusively. Batch checks use single-pass `AuthorityProfile` pre-resolution and `EventAuthoritySnapshotService` batch pre-fetching.

## gRPC Transport

Cerbos communication uses the official `Cerbos.Sdk` NuGet package (gRPC):
- Instance PDP: singleton `ICerbosClient` built via `CerbosClientBuilder.ForTarget(grpcEndpoint)`
- BYO PDP: `ICerbosClientFactory` caches gRPC clients per endpoint (thread-safe, long-lived channels)
- No admin credentials needed for runtime `CheckResources` — credentials are Admin API only
- TLS: production endpoints use `https://` prefix; dev uses `http://` with `PlaintextMode=true`

No retry policy is configured — fail-fast to deny is safer than retrying authorization checks.

## Failure Mode Contract

When BYO Cerbos fails:
- local provider enters `SafeMode` (deny except explicit safe paths like instance-admin checks).
- `failure_mode=open` is parsed as a deprecated configuration value but ignored at runtime.

When instance Cerbos fails:
- All checks are denied. The operator chose Cerbos; falling back to a potentially more permissive
  local RBAC would silently bypass intended policies.

## Consequences

1. Deployment can run in local-only mode (no Cerbos dependency at startup).
2. Instance admins can switch provider mode via settings without code changes.
3. Two authorization paths must be maintained and tested.
4. Local fallback cannot evaluate all advanced Cerbos policy semantics.

## Related

- [AUTHORIZATION_PATTERNS.md](../AUTHORIZATION_PATTERNS.md)
- [SECURITY.md](../SECURITY.md)
- [DEPLOYMENT_TIERS.md](../DEPLOYMENT_TIERS.md)
