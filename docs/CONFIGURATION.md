ABOUTME: Documents runtime configuration sources and precedence for API, BFF, and shared infrastructure.
ABOUTME: Focuses on non-inferable key names, mapping behavior, and settings cascade rules.

# Configuration

## Runtime Configuration Sources

The system uses three configuration layers:

1. static app settings (`appsettings*.json`, environment variables, user secrets),
2. secret management (`AddInfisicalCompatibility` / `AddInfisicalBlazorCompatibility` + `AddSecretManagement`),
3. governance settings in database (`SystemSetting` + `TenantSetting`).

## Core Static Sections

Commonly consumed sections in code:

- `Keycloak:*` (authority, metadata, client IDs/secrets)
- `ConnectionStrings:DefaultConnection`
- `Cors:AllowedOrigins`
- `RateLimiting:*`
- `RequestTimeouts:*`
- `Cerbos:*`
- `Deployment:*`
- `S3Settings:*` (fallback source for storage resolver)
- `SecretProvider:*`
- `SecretRefresh:*`

## Secret Provider Configuration

`Explore.Secrets` binds provider config from `SecretProvider`:

- `SecretProvider:Provider` (default `None`)
- `SecretProvider:FailFast`
- `SecretProvider:Infisical:*` (project/client credentials, paths, environment)

Refresh behavior binds from `SecretRefresh` and runs via hosted `SecretRefreshService`.

## API Compatibility Mapping (Infisical -> .NET keys)

`Explore.API.Extensions.ConfigurationExtensions` maps external secret names to canonical keys, including:

- `POSTGRESQL_PUBLIC_URL` -> `ConnectionStrings:DefaultConnection`
- keycloak realm/base URL values -> `Keycloak:Authority`, `Keycloak:MetadataAddress`, `Keycloak:Audience`
- S3 integration values -> `S3Settings:*`

Important behavior:

- mapping uses `TrySet`: existing canonical keys are not overwritten.

## Blazor Server Compatibility Mapping

`Explore.Blazor.Extensions.ConfigurationExtensions` maps Keycloak and API base URL keys similarly.

Important behavior:

- `Keycloak:ClientSecret` is explicitly overridden when a mapped secret is present.

## Governance Settings (Database)

Governance keys are centralized in `Explore.Domain.Constants.GovernanceSettingKeys`.

Major groups:

- `deployment.*`
- `tenants.*`
- `routing.*` and `routing.render_policy.*`
- `events.*`
- `organizations.*`
- `modules.*`
- `branding.*`
- `domains.*`
- `email.*`
- `s3.*`
- `authorization.*`
- `cerbos.*`
- `analytics.*`
- `auth.*`
- `federation.*`

Values are stored as JSON-serialized strings in `SystemSetting.Value` and `TenantSetting.Value`.

Post-onboarding management note:

- Instance admins can update auth-provider governance values through `PUT /api/InstanceOnboarding/admin/auth-provider-configuration`.
- Secret values (`keycloak`/`google` client secrets) continue to use secret-setting storage, not plain governance values.

## Settings Cascade Rules

`SettingsResolver` resolves values in this order:

1. system setting,
2. if not locked and tenant provided, tenant override,
3. fallback to system default.

Cache behavior:

- system settings cache key: `SystemSettings_All`
- tenant settings cache key prefix: `TenantSettings_`
- default cache TTL: 5 minutes

## Deployment Mode Configuration

Static deployment config is bound from `Deployment` section (`DeploymentSettings`):

- `Mode`: `SingleTenant` or `MultiTenant` (default `MultiTenant`)
- `DefaultTenantId`
- `HidePlatformAdminInSingleTenant` (default `true`)
- `DefaultTenantSubdomain`

Runtime nuance:

- `TenantContext` can override static mode using DB key `deployment.mode` when available.

## Related

- [MULTI_TENANCY.md](MULTI_TENANCY.md)
- [RENDER_POLICIES.md](RENDER_POLICIES.md)
- [OPERATIONS.md](OPERATIONS.md)
