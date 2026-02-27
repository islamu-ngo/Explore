ABOUTME: Explains tenant resolution, isolation, and override behavior implemented in code.
ABOUTME: Prioritizes runtime rules from TenantContext, query filters, and governance services.

# Multi-Tenancy

## Deployment Modes

Runtime mode can come from two places:

1. `SystemSetting` key `deployment.mode` (preferred when DB is available),
2. static `DeploymentSettings.Mode` fallback.

Modes:

- `SingleTenant`
- `MultiTenant`

In single-tenant mode, tenant context always resolves to default tenant ID.

## Tenant Resolution Order

`Explore.API.Services.TenantContext` resolves tenant in this strict order:

1. `X-Tenant-Id` header (if valid GUID),
2. custom-domain lookup (`domains.tenant_custom_domain`) if custom domains are allowed,
3. subdomain lookup (`domains.tenant_subdomain`, then fallback to active tenant slug),
4. default tenant ID.

Request host source:

- uses `X-Forwarded-Host` first, then request host.

Resolved tenant is cached for the request in `HttpContext.Items["__resolved_tenant_id"]`.

## Default Tenant Fallback

Fallback default tenant ID in `TenantContext` is:

- `018e4e5c-7f00-7000-8000-000000000001`

Used when no configured default exists.

## Domain and Subdomain Rules

Subdomain extraction behavior:

- normalizes to lowercase alphanumeric/hyphen,
- ignores common prefixes: `www`, `api`, `app`, `admin`, `localhost`,
- requires active tenant status for resolved tenant IDs.

Custom/subdomain values are stored as JSON-serialized strings in `TenantSetting.Value`.

## Data Isolation Enforcement

Isolation is enforced in `ExploreDbContext` with named global filters:

- `Tenant` filter for tenant-scoped entities,
- `SoftDelete` filter for soft-deletable entities.

Notable cases:

- `User` is soft-delete filtered but not tenant-scoped.
- some entities combine tenant and soft-delete filters.

## Tenant Override Model

Tenant-specific customization uses `TenantSetting` records keyed by `SettingKey`.

Resolution flow (`SettingsResolver`):

1. read system setting,
2. if unlocked and tenant override exists, use tenant value,
3. otherwise use system value.

If setting is locked (`SystemSetting.IsLocked=true`), tenant overrides are ignored/removed by policy services.

## Tenant Lifecycle States

`TenantStatusEnum` values:

- `Provisioning`
- `Active`
- `Suspended`
- `Archived`
- `Purged`

## Endpoint Visibility by Mode

Filters in `Explore.API.Filters.BlockInSingleTenantAttribute`:

- `BlockInSingleTenant` -> returns `404` in single-tenant mode (hide endpoint),
- `RequireMultiTenant` -> returns `403` with explicit multi-tenant required message.

## Related

- [CONFIGURATION.md](CONFIGURATION.md)
- [OPERATIONS.md](OPERATIONS.md)
- [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md)
