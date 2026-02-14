# ADR-001: Authorization Provider Architecture — HTTP Transport with Graceful Fallback

**Status:** Accepted  
**Date:** 2026-02-14  
**Deciders:** ISLAMU Event Core Team

---

## Context

The ISLAMU Event platform requires fine-grained, resource-level authorization across a multi-tenant hierarchy (Instance > Tenant > Organization). We evaluated several approaches for integrating Cerbos as the Policy Decision Point (PDP).

### Key Questions

1. **Transport protocol**: HTTP REST vs gRPC for Cerbos communication?
2. **Failure mode**: What happens when Cerbos is unreachable?
3. **Deployment flexibility**: How to support deployments without Cerbos (Tier 1 Humble)?

### Constraints

- The platform must start and function without Cerbos running (self-hoster friendly)
- Authorization must never block application startup
- Latency budget: <50ms per authorization check (p99)
- Clean Architecture boundaries must be preserved (Application layer defines contracts, Infrastructure implements)

---

## Decision

### Dual-Provider Architecture with Runtime Switching

```
RuntimeAuthorizationProvider (IAuthorizationProvider)
    ├── CerbosAuthorizationProvider  ← HTTP REST to Cerbos PDP
    └── LocalAuthorizationProvider   ← In-process RBAC fallback
```

**`RuntimeAuthorizationProvider`** reads the `authorization.provider` SystemSetting at runtime and delegates to the appropriate concrete provider. This is NOT a code-level switch — it's a database-driven configuration that can be changed by instance admins without redeployment.

### HTTP Transport (not gRPC)

Cerbos communication uses the HTTP REST API (`/api/check/resources`) via a named `HttpClient` with Polly resilience policies.

### Resilience Configuration

- **Timeout**: 2 seconds (hard limit)
- **Circuit breaker**: trips after 50% failure rate over 30s sampling window, breaks for 15s
- **No retry**: fail-fast to LocalAuthorizationProvider is safer than retrying authorization checks

---

## Rationale

### Why HTTP over gRPC

| Factor | HTTP | gRPC |
|--------|------|------|
| Deployment complexity | Standard reverse proxy | Requires HTTP/2 + TLS termination |
| Debugging | `curl` friendly, standard tooling | Requires `grpcurl` or specialized tooling |
| Load balancer compatibility | Any L7 LB | Requires gRPC-aware LB (not all support it) |
| Cerbos API parity | First-class REST API | First-class, but no advantage for our use case |
| Latency | ~5ms per check (HTTP/1.1) | ~2ms per check (multiplexed) |
| Docker Compose simplicity | Port 3592, standard health check | Port 3593, requires TLS |

**Decision**: The 3ms latency difference is negligible for our use case. HTTP's operational simplicity (debugging, proxying, health checks) outweighs gRPC's raw performance advantage.

### Why Dual-Provider (not Cerbos-only)

1. **Tier 1 Humble deployments** run without Cerbos. LocalAuthorizationProvider handles basic RBAC: role hierarchy checks, permission lookups from the RolePermission table.
2. **Graceful degradation**: If Cerbos becomes unreachable in Tier 2/3, the circuit breaker trips and authorization falls back to Local rather than failing all requests.
3. **LocalAuthorizationProvider covers ~95% of decisions** for simple deployments — it checks role-based permissions from the database. Cerbos adds contextual policies (resource attributes, conditions, derived roles) that Local cannot evaluate.

### Why No Retry on Authorization

Authorization checks are idempotent but time-sensitive. Retrying a failed auth check:
- Doubles latency for the user's request
- Masks Cerbos availability issues (circuit breaker needs accurate failure counts)
- Is unnecessary because LocalAuthorizationProvider provides a safe fallback

Fail-fast + fallback is safer than retry + hope.

---

## Consequences

### Positive

- Platform works at every deployment tier (Humble through Ummah-Scale)
- Instance admins can switch authorization providers without code changes
- Circuit breaker prevents cascading failures during Cerbos outages
- Simple Docker deployment — no gRPC infrastructure requirements

### Negative

- HTTP is ~3ms slower per check than gRPC (acceptable for our p99 budget)
- LocalAuthorizationProvider cannot evaluate Cerbos-specific conditions (derived roles, resource attributes) — it only does flat RBAC
- Two authorization codepaths to maintain (mitigated by shared `IAuthorizationProvider` interface)

### Neutral

- PolicySyncService pushes to Cerbos Admin API via a separate `CerbosAdminClient` HttpClient (no resilience needed — sync is background, not request-critical)

---

## Deployment Tier Mapping

| Tier | Provider Setting | Behavior |
|------|-----------------|----------|
| 1 (Humble) | `local` | LocalAuthorizationProvider only, no Cerbos |
| 2 (Community) | `cerbos` | CerbosAuthorizationProvider primary, Local fallback |
| 3 (Ummah-Scale) | `cerbos` | Cerbos HA cluster, Local fallback |

See [DEPLOYMENT_TIERS.md](../DEPLOYMENT_TIERS.md) for infrastructure details.

---

## Related

- [AUTHORIZATION_PATTERNS.md](../AUTHORIZATION_PATTERNS.md) — When to use each authorization pattern
- [SECURITY.md](../SECURITY.md) — Security architecture overview
- [DEPLOYMENT_TIERS.md](../DEPLOYMENT_TIERS.md) — Infrastructure scaling guide
