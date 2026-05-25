ABOUTME: Explains tenant resolution, isolation, and override behavior implemented in code.
ABOUTME: Prioritizes runtime rules from TenantContext, query filters, and governance services.

# Multi-Tenancy

## Deployment Modes

First-run onboarding mode:

- API configuration key `Deployment:Mode`, normally mapped from Infisical `/api` secret `DEPLOYMENT_MODE`.
- `DEPLOYMENT_MODE=multi_tenant` shows the multi-tenant onboarding flow.
- absent `DEPLOYMENT_MODE` shows the single-tenant onboarding flow only.

After onboarding, runtime mode is operator-controlled. Normal instance-admin UI cannot switch deployment mode; launch multi-tenant mode by setting `DEPLOYMENT_MODE=multi_tenant` before first-run onboarding or by following an explicit operator migration runbook.

Modes:

- `SingleTenant`
- `MultiTenant`

In single-tenant mode, tenant context always resolves to default tenant ID.

## Tenant Resolution Order

Standard runtime authority lives in `Explore.API.Middleware.ApiTenantResolutionMiddleware`.

In multi-tenant mode, the API resolves tenant in this strict order:

1. trusted `X-Tenant-Slug` header forwarded by the BFF,
2. custom-domain lookup (`domains.tenant_custom_domain`) if custom domains are allowed,
3. subdomain lookup (`domains.tenant_subdomain`),
4. unresolved request fails closed with `404`.

In single-tenant mode, tenant context always resolves to the configured default tenant ID.

Request host source:

- uses normalized `Request.Host.Host` after trusted forwarded-header processing.

For standard authenticated-browser requests, the middleware binds the resolved tenant into the request-scoped tenant context accessor immediately.

For API-key requests, `ApiTenantResolutionMiddleware` can store a requested tenant hint in `HttpContext.Items["__requested_tenant_id"]` and defer final tenant binding to `ApiTenantPostAuthenticationMiddleware` after authentication succeeds.

## Default Tenant Fallback

Fallback default tenant ID in single-tenant runtime is:

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
- `TenantUser` and `TenantUserProfile` hold tenant-local participation, status, profile, consent, and moderation state for a global `User`; tenant-admin actions such as suspend, ban, remove, or profile moderation must target these tenant-local rows rather than mutating global account identity.
- `Actor` is tenant-scoped. User actors are unique by `(UserId, TenantId)` so the same global account can have separate tenant personas.
- some entities combine tenant and soft-delete filters.

## Managed Provider Tenant Provisioning

Trusted managed-provider automation creates tenant boundaries explicitly rather than treating `Organization` as tenancy:

1. Provider/operator authority calls `POST /api/managed-provider-provisioning/clients:ensure` with instance-admin authorization.
2. The command creates or resolves a `Tenant` for the provider customer and records provider-neutral `ExternalBinding` rows for durable idempotency.
3. The external administrator is linked to the minimal global `User` account through stable IdP identifiers, then gets tenant-local `TenantUser`, `TenantUserProfile`, user `Actor`, and `TenantMember` tenant-admin state inside the new tenant.
4. Optional organizer semantics create an approved `Organization` or `Group` plus actor inside the tenant. These are tenant-scoped organizer entities, not tenancy boundaries.
5. ERP customer/admin identities receive tenant-admin authority for their tenant, not instance-admin authority. Customer-as-instance-admin is reserved for separate managed-hosting/dedicated-instance product models.

## Hierarchical Settings Model

Customization follows a 5-tier resolution cascade implemented in `HierarchicalSettingsResolver`:

1. **Instance** (`SystemSetting`) — Platform-wide defaults.
2. **Tenant** (`TenantSetting`) — Per-tenant overrides.
3. **Organization** (`OrganizationSetting`) — Specific organization overrides.
4. **Group** (`GroupSetting`) — Specific group overrides.
5. **User** (`UserPreference`) — Individual user preferences.

Resolution flow:
- Reads all tiers for the requested key(s) in a single batch operation.
- Merges values from top (User) to bottom (Instance).
- **Locking**: If a higher tier locks a setting (e.g., `SystemSetting.IsLocked=true`), lower-tier overrides are ignored.
- **Single-Tenant Bypass**: In single-tenant mode, system locks are bypassed to allow the default tenant full control.

Cache behavior:
- Each tier has its own cache prefix (e.g., `HierSettings:Tenant:{id}`).
- Default TTL: 5 minutes.
- Updates to settings invalidate the corresponding scope cache.

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

## Render Policy Delegation

Render policies can be delegated to tenants via the governance cascade.

Master gate: `routing.render_policy.allow_tenant_override` must be `true` in `SystemSetting`.

Per-route-group locks (in `SystemSetting`):

- `routing.render_policy.lock_tenant_public_seo`
- `routing.render_policy.lock_tenant_operational`
- `routing.render_policy.lock_tenant_admin`

Onboarding render policy is always instance-controlled.

Tenants store overrides in `TenantSetting` with the same `routing.render_policy.*` keys. Override cascade:

1. Instance settings are read first.
2. Tenant preset/global overrides are applied.
3. Normalization runs (preset defaults, non-advanced collapse).
4. Per-route-group tenant overrides are applied only for unlocked groups.

See [RENDER_POLICIES.md](RENDER_POLICIES.md) for full delegation details.

## Related

- [CONFIGURATION.md](CONFIGURATION.md)
- [OPERATIONS.md](OPERATIONS.md)
- [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md)
- [RENDER_POLICIES.md](RENDER_POLICIES.md)
