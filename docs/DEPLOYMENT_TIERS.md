ABOUTME: Defines practical deployment tiers mapped to current runtime behavior and provider switching.
ABOUTME: Uses the authorization/runtime architecture as the anchor for scaling decisions.

# Deployment Tiers

## Purpose

Tiers describe infrastructure maturity levels, not different codebases.  
The same application can move across tiers mainly through configuration and infrastructure changes.

## Tier Summary

1. Tier 1 - Humble
   - target: small/self-hosted.
   - authorization mode: `authorization.provider=local`.
   - data topology: single PostgreSQL deployment.
2. Tier 2 - Community
   - target: production multi-tenant.
   - authorization mode: `authorization.provider=cerbos` with local fallback.
   - data topology: PostgreSQL + Cerbos service + optional replica/cache.
3. Tier 3 - Ummah-Scale
   - target: high-scale / strict isolation.
   - authorization mode: Cerbos HA + local fallback.
   - data topology: separated clusters for app data, identity, and policy.

## Tier 1 - Humble

Recommended when:

- one community or early-stage deployment,
- minimal operational overhead required.

Typical setup:

- API + Blazor + PostgreSQL + Keycloak,
- Cerbos not deployed (or deployed but unused),
- local authorization decisions from database roles/permissions.

## Tier 2 - Community

Recommended when:

- shared platform with multiple tenants,
- stronger policy controls required.

Typical setup:

- enable Cerbos and set `authorization.provider=cerbos`,
- keep local provider as automatic fallback,
- add basic redundancy for API and data reads.

Runtime behavior:

- `RuntimeAuthorizationProvider` chooses Cerbos first (when configured),
- failures fall back to local provider without redeploying code.

## Tier 3 - Ummah-Scale

Recommended when:

- strict isolation, compliance, and high availability are required,
- independent scaling of policy, identity, and app data is needed.

Typical setup:

- Cerbos cluster behind dedicated load balancer,
- separate database clusters for:
  - application data,
  - identity data (Keycloak),
  - authorization policy storage,
- centralized observability stack.

## Upgrade Path

1. Start Tier 1 with local authorization.
2. Introduce Cerbos and move to Tier 2 by updating runtime setting `authorization.provider`.
3. Split infrastructure domains and add HA to reach Tier 3.

No feature rewrite is required for these transitions.

## Related

- [OPERATIONS.md](OPERATIONS.md)
- [SECURITY.md](SECURITY.md)
- [CONFIGURATION.md](CONFIGURATION.md)
