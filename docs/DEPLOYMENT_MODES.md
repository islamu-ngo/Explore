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
2. later instance-admin changes to `deployment.mode`.

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

Instance administrators can switch from single-tenant to multi-tenant at runtime. They can switch from multi-tenant back to single-tenant only when one or fewer active tenants remain.

## Related

- [MULTI_TENANCY.md](MULTI_TENANCY.md)
- [CONFIGURATION.md](CONFIGURATION.md)
- [OPERATIONS.md](OPERATIONS.md)
