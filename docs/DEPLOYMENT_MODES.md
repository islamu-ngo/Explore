ABOUTME: Describes single-tenant and multi-tenant runtime modes as implemented in current code.
ABOUTME: Focuses on mode resolution, endpoint behavior, and tenant context effects.

# Deployment Modes

## Supported Modes

Configured mode values:

- `SingleTenant`
- `MultiTenant`

First-run onboarding source:

- API configuration key `Deployment:Mode`, normally populated from Infisical `/api` secret `DEPLOYMENT_MODE`.
- Use `DEPLOYMENT_MODE=multi_tenant` to show multi-tenant onboarding.
- If `DEPLOYMENT_MODE` is absent, onboarding shows single-tenant setup only.

Runtime source after onboarding:

1. persisted bootstrap/system setting selected during onboarding,
2. operator-controlled migration work only.

Normal instance-admin UI cannot switch deployment mode after onboarding. To launch in multi-tenant mode, set `DEPLOYMENT_MODE=multi_tenant` before first-run onboarding. Leaving it unset launches convention-first single-tenant setup.

## Single-Tenant Mode Behavior

When active:

- `TenantContext` always resolves default tenant ID.
- Tenant discovery via custom domain/subdomain is bypassed.
- Endpoints marked with `BlockInSingleTenant` can return `404` (if hiding enabled).

Relevant static setting:

- `Deployment:HidePlatformAdminInSingleTenant` (default `true`).

## Multi-Tenant Mode Behavior

When active, the API-authoritative resolver middleware resolves tenant by request context:

1. trusted `X-Tenant-Slug` header,
2. custom domain,
3. subdomain,
4. unresolved request fails closed with `404`.

Tenant-scoped queries are filtered by global EF query filters.

Dedicated admin hosts are static BFF configuration, not tenant routing data. Configure `Bff:AdminHosts` with exact host/origin values such as `admin.example.org`; the Blazor BFF renders the control-plane shell for those hosts and skips tenant subdomain/custom-domain lookup. Optional `Bff:AdminHostAllowedIpRanges` can restrict those admin hosts to exact IP/CIDR ranges.

Typical multi-tenant DNS:

- public platform host: `events.example.org` -> Blazor BFF edge;
- wildcard tenant host: `*.events.example.org` -> same Blazor BFF edge;
- dedicated admin host: `admin.example.org` -> same Blazor BFF edge, with `Bff:AdminHosts` configured;
- tenant custom domains: tenant-owned CNAMEs to the documented public edge target.

## Default Tenant Contract

If no configured default tenant exists in single-tenant mode, runtime uses fallback:

- `018e4e5c-7f00-7000-8000-000000000001`

This should stay aligned with seeded default tenant IDs.

## Mode-Dependent Endpoint Filters

- `BlockInSingleTenantAttribute`: hides endpoint as `404` in single-tenant mode.
- `RequireMultiTenantAttribute`: returns `403` with explicit message in single-tenant mode.

## Operational Note

Mode behavior affects:

- tenant resolution,
- endpoint visibility,
- admin UX paths,
- effective policy/delegation paths in tenant settings.

Deployment mode is intentionally operator-governed after onboarding. Do not treat `deployment.mode` as a casual runtime toggle; use an explicit migration/runbook if an installed instance must change modes.

## Localization Bundle Storage In Deployment Modes

Localization static bundles are deployment-local files unless an operator mounts
the API content-root bundle directory on shared storage:

```text
{ContentRoot}/App_Data/Localization/Bundles/{code}.json
```

Single-tenant and multi-tenant modes both use the same provider/resolver stack.
In `tms_provider=None` or live-provider fallback, embedded bundles are loaded
first and valid writable files override individual keys. For multi-replica
deployments, mount the bundle directory on a shared persistent volume so all API
replicas read the same operator-imported bundles. Otherwise static bundle writes
are only visible to the replica that received the admin request.

## Related

- [MULTI_TENANCY.md](MULTI_TENANCY.md)
- [CONFIGURATION.md](CONFIGURATION.md)
- [OPERATIONS.md](OPERATIONS.md)
