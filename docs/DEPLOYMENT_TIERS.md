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
   - authorization mode: `authorization.provider=cerbos` with fail-closed PDP behavior.
   - data topology: PostgreSQL + Cerbos service + optional replica/cache.
3. Tier 3 - Ummah-Scale
   - target: high-scale / strict isolation.
   - authorization mode: Cerbos HA with explicit operational failover.
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
- keep local authorization configured only as an explicit operator-selected recovery mode,
- add basic redundancy for API and data reads.

Runtime behavior:

- `RuntimeAuthorizationProvider` chooses Cerbos first (when configured),
- instance Cerbos failures deny/fail closed; they do not automatically fall back to local RBAC,
- switching to local authorization requires an explicit provider-mode configuration change,
- BYO tenant Cerbos outages always fail closed; legacy `failure_mode=open` values are parsed but ignored at runtime.

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

## Database Instance Isolation

Deployment tier does not change the relational namespace rule:

- PostgreSQL and SQL Server use the configured `Database:Schema` /
  `DATABASE_SCHEMA` as the application boundary and keep clean table names.
- SQLite forces `ie_` and requires a distinct durable local file plus one
  application replica per instance.
- MariaDB and MySQL force `ie_`; create a distinct database for each instance
  on the same server rather than placing production and staging in one database.

PostgreSQL TickerQ state is outside the application schema in the fixed
`ticker` schema. Tier 2/3 deployments that run more than one ISLAMU instance
must use separate PostgreSQL databases while TickerQ is enabled, or use the
portable HostedService email-dispatch mode before sharing a database through
separate application schemas.

## Upgrade Path

1. Start Tier 1 with local authorization.
2. Introduce Cerbos and move to Tier 2 by updating runtime setting `authorization.provider`.
3. Split infrastructure domains and add HA to reach Tier 3.

No feature rewrite is required for these transitions.

## Related

- [OPERATIONS.md](OPERATIONS.md)
- [SECURITY.md](SECURITY.md)
- [CONFIGURATION.md](CONFIGURATION.md)
