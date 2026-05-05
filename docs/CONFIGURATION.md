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
- `ForwardedHeadersTrust:*`
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

- `DEPLOYMENT_MODE` (Infisical `/api`) -> `Deployment:Mode` (`single_tenant`/`multi_tenant` normalized to `SingleTenant`/`MultiTenant`)
- `KEYCLOAK_ENDPOINT` (Infisical `/keycloak`) -> `Keycloak:Authority` (via base URL + realm)
- keycloak realm/base URL values -> `Keycloak:Authority`, `Keycloak:MetadataAddress`, `Keycloak:Audience`
- S3 integration values -> `S3Settings:*`

Keycloak base URL: `KEYCLOAK_ENDPOINT` (Infisical `/keycloak`). No hardcoded fallback — if not set, Keycloak mapping is skipped.

Important behavior:

- mapping uses `TrySet`: existing canonical keys are not overwritten.

## Blazor Server Compatibility Mapping

`Explore.Blazor.Extensions.ConfigurationExtensions` maps Keycloak and API base URL keys similarly.

API base URL: `API_ENDPOINT` (Infisical `/blazor`) or `ExploreApi:BaseUrl`. Inline code fallback `https://localhost:7039/` is used only when no value is configured at all.

Important behavior:

- `Keycloak:ClientSecret` is explicitly overridden when `KEYCLOAK_BLAZOR_CLIENT_SECRET` (Infisical) is present.

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
- `localization.*`

Values are stored as JSON-serialized strings in `SystemSetting.Value` and `TenantSetting.Value`.

## Analytics Settings (Governance)

Analytics configuration is governed entirely through `analytics.*` keys.
The runtime abstraction is optional by design: instance admins can lock a shared provider,
leave settings unlocked so tenants can bring their own provider, or disable analytics entirely.

Canonical keys:

| Key | Type | Default | Description |
|---|---|---|---|
| `analytics.provider` | string | `"none"` | Active provider: `none`, `posthog`, `plausible`, `rybbit`, `rudderstack` |
| `analytics.enabled` | bool | `false` | Master enable switch for analytics emission |
| `analytics.consent_mode` | string | `"pseudonymous"` | Privacy mode: `anonymous`, `pseudonymous`, `identified` |
| `analytics.transport_mode` | string | `"direct"` | Browser transport: `direct`, `proxy`, `relay` |
| `analytics.api_key` | string | `""` | Public or write key used by the active provider |
| `analytics.endpoint_url` | string | `""` | Provider base URL, especially important for self-hosted deployments |
| `analytics.personal_api_key` | string | `""` | Sensitive key used for advanced provider features such as PostHog feature flags |

Important notes:

- `analytics.endpoint_url` is the canonical endpoint key. Do not introduce `analytics.endpoint`.
- There is no canonical `analytics.site_id` governance key in the current abstraction.
- The analytics settings follow the standard settings cascade: system setting -> tenant override -> system default.
- Sensitive keys should still be treated carefully in UI and operational workflows even when stored as governance values.
- `analytics.transport_mode=relay` is the only mode that does not require a browser-exposed `analytics.api_key`; the browser posts first-party events to `/api/a/t` and the server uses the resolved provider settings.
- `analytics.transport_mode=proxy` still uses the provider script/client in the browser, but the script host and ingest host should usually point at a first-party reverse-proxy path through `analytics.endpoint_url`.
- `analytics.consent_mode=identified` only enables identify semantics for providers that explicitly support them today (`posthog`, `rudderstack`).

Cookie consent and privacy governance keys:

| Key | Type | Default | Description |
|---|---|---|---|
| `analytics.global_disable_client_tracking` | bool | `false` | Emergency kill switch — disables all **browser-side** analytics immediately. Server-side relay endpoints and server analytics continue normally. Scope: browser SDK initialization only. |
| `analytics.cookie_banner_enabled` | bool | `false` | Whether the cookie consent banner is shown to end users |
| `analytics.decline_behavior` | enum | `"disable"` | What happens when a user declines consent: `disable` (no analytics) or `cookieless` (privacy-preserving analytics) |
| `analytics.consent_cookie_lifetime_days` | int | `180` | How long the consent preference cookie persists (ICO recommends 6 months) |
| `analytics.posthog_cookieless_mode` | enum | `"on_reject"` | PostHog cookieless mode: `off`, `always` (never stores on device), `on_reject` (cookieless after decline) |
| `analytics.posthog_person_profiles` | enum | `"identified_only"` | PostHog person profile creation: `always`, `identified_only`, `never` |
| `analytics.posthog_session_replay` | bool | `false` | PostHog session recording (non-essential, requires consent) |
| `analytics.posthog_autocapture` | bool | `false` | PostHog autocapture of clicks/inputs (non-essential) |
| `analytics.posthog_heatmaps` | bool | `false` | PostHog heatmap data collection (non-essential) |
| `analytics.posthog_toolbar` | bool | `false` | PostHog toolbar for on-page debugging |

Storage-mode-driven consent rules:

- The cookie banner requirement is **not** determined by provider name alone. It is determined by whether the provider's configured runtime mode stores or accesses non-essential data on the user's device.
- `plausible` and `rybbit`: cookieless by design, no banner required by default.
- `posthog` with `cookieless_mode=always`: no banner required (no device storage).
- `posthog` with `cookieless_mode=on_reject`: banner required; decline switches to cookieless analytics instead of total silence.
- `posthog` with `cookieless_mode=off` and any non-essential feature enabled: banner required.
- `rudderstack`: treated as "full consent required" for v1.
- The computed storage profile (`Cookieless`, `ConsentManaged`, `FullConsent`) drives all runtime behavior through `IAnalyticsRuntimeProfileResolver`.

Consent cookie design:

- Cookie name is tenant-scoped: `explore_cc_{stableShortKey}` where the stable key is derived from the first 8 hex characters of the tenant's immutable GUID (not the mutable subdomain slug). This prevents cookie orphaning when a tenant renames their subdomain.
- Cookie value is minimal: `accepted` or `declined` only. No timestamps, user IDs, or tracking data.
- Cookie scope: per effective public host/tenant experience. `SameSite=Lax`, `Secure`, `path=/`. Consent is not shared across subdomains or tenants.
- The consent cookie itself is classified as strictly necessary (remembering the user's choice).

Post-onboarding management note:

- Instance admins can update analytics governance values through `PUT /api/InstanceOnboarding/analytics-governance`.
- Instance admins can update authentication provider governance values through `PUT /api/instance/settings/auth-provider`.
- Instance admins can update the active authorization provider through `PUT /api/instance/settings/authz-provider`.
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

- `Mode`: `SingleTenant` or `MultiTenant` (default `SingleTenant`)
- `DefaultTenantId`
- `HidePlatformAdminInSingleTenant` (default `true`)
- `DefaultTenantSubdomain`

First-run onboarding mode is controlled only by API configuration. Set `DEPLOYMENT_MODE=multi_tenant` in the Infisical `/api` folder to show the multi-tenant onboarding flow. If `DEPLOYMENT_MODE` is absent, onboarding is single-tenant only. Invalid deployment-mode values fail safely to single-tenant setup.

Normal admin UI does not switch deployment mode after onboarding. Choose multi-tenant mode before first launch by setting the API environment value; otherwise the convention-first path launches a single-tenant site with the default tenant hidden internally.

## Reverse Proxy Trust Configuration

`Explore.API` binds trusted forwarded-header settings from `ForwardedHeadersTrust`:

- `ForwardLimit` (default `1`)
- `TrustLoopbackProxy` (useful for local/test proxy chains)
- `KnownProxies` (IP list)
- `KnownNetworks` (CIDR list)

Important behavior:

- if no trusted proxy boundary is configured, forwarded host/IP processing is disabled in the API host;
- host-derived tenant resolution and proxy-aware rate limiting rely on normalized request values after trusted forwarded-header processing, not on raw `X-Forwarded-*` headers.

Runtime nuance:

- Before onboarding completes, tenant resolution uses a single-tenant fallback so setup endpoints remain reachable.
- Onboarding persists the configured API deployment mode into the database.
- After onboarding, deployment mode is operator-controlled. Runtime admin switching is disabled in the normal governance UI; change mode only through an explicit operator migration path.

## Localization / TMS Settings (Governance)

Keys in `GovernanceSettingKeys.Localization`:

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `localization.default_language` | string | `"en"` | Default language code (ISO 639-1) |
| `localization.tms_provider` | int | `0` | TMS provider: 0=None (offline), 1=Tolgee, 2=Weblate |
| `localization.tms_api_url` | string | `null` | Base URL for the TMS REST API |
| `localization.tms_project_id` | string | `null` | TMS project identifier |
| `localization.tms_component` | string | `null` | Weblate component slug (Weblate only) |

TMS API keys/tokens are stored via `SecretProvider`, not governance settings.

See [LOCALIZATION.md](LOCALIZATION.md) for full architecture.

## Custom Property Quotas (Governance)

Hard-limit quota definitions for Layer 3 custom properties (Rule 16). Each has a tenant-overridable default and a platform maximum.

| Key | Type | Default | Description |
|---|---|---|---|
| `custom_properties.max_definitions_per_tenant_per_entity_scope` | int | `500` | Max definitions per (Org/Group/Event). Max: 5000. |
| `custom_properties.max_definitions_per_event` | int | `100` | Max runtime definitions per Event. Max: 1000. |
| `custom_properties.max_definitions_per_event_session` | int | `50` | Max runtime definitions per Session. Max: 500. |
| `custom_properties.max_options_per_definition` | int | `200` | Max option rows per definition. Max: 2000. |
| `custom_properties.max_multi_value_rows_per_value` | int | `20` | Max rows for multi-valued property. Max: 200. |
| `custom_properties.projection_rebuild_batch_size` | int | `500` | Batch size for projection worker. Max: 5000. |
| `custom_properties.projection_discovery_enabled` | bool | `false` | Tenant feature flag for projection-backed search/filter. |

## AI Assistant Settings (Governance)

| Key | Type | Default | Description |
|---|---|---|---|
| `ai_assistant.enabled` | bool | `false` | Enable AI assistant features |
| `ai_assistant.endpoint_url` | string | `""` | AI provider API base URL |
| `ai_assistant.api_key` | string | `""` | AI provider API key |

## Tenant Delegation & Locking (Governance)

| Key | Type | Default | Description |
|---|---|---|---|
| `governance.lock_tenant_smtp` | bool | `false` | Prevent tenant from overriding instance SMTP |
| `governance.lock_tenant_storage` | bool | `false` | Prevent tenant from overriding instance S3 |
| `governance.lock_tenant_analytics` | bool | `false` | Prevent tenant from overriding instance analytics |
| `governance.lock_tenant_ai_assistant` | bool | `false` | Prevent tenant from overriding instance AI assistant |

## Event List Visibility (Governance)

| Key | Type | Default | Description |
|---|---|---|---|
| `event_list.browse_mode` | string | `"Standard"` | Default browse experience |
| `event_list.page_size` | int | `20` | Default items per page |
| `event_list.card.show_organizer` | bool | `true` | Show organization in cards |
| `event_list.card.show_price` | bool | `true` | Show price in cards |
| `event_list.card.show_tags` | bool | `true` | Show tags in cards |

## External API Key Defaults

Non-interactive callers use long-lived `{keyId}.{secret}` credentials. Per-key policy defaults are applied at create time by `ExternalApiKeyQuotaDefaults` and `ExternalApiKeyScopeCeiling`.

### Quota Defaults by Owner Type

| Owner Type | Default Period | Default Request Limit | Rationale |
|---|---|---|---|
| `User` (`1`) | `Daily` | `1,000` | Per-user automation, usually tied to a single developer |
| `Organization` (`2`) | `Monthly` | `10,000` | Team-scale automations and integrations |
| `Group` (`3`) | `Monthly` | `5,000` | Smaller scope than an org but shared by multiple members |
| `Tenant` (`4`) | `Monthly` | `50,000` | Tenant-wide admin automation |
| `InstanceAdmin` (`5`) | `None` | unlimited | Platform operator usage, rate-limited only by node-local policies |

All defaults are overridable per key via `PUT /api/ExternalApiKey/{id}`.

### Scope Ceilings by Owner Type

- `User`: `events:read`, `events:write`, `users:read`, `users:write`, `lookups:read`, `registrations:write`, `api-keys:manage`
- `Organization`: User scopes plus `organizations:read`, `organizations:write`
- `Group`: User scopes plus `groups:read`, `groups:write`
- `Tenant`: All of the above plus `admin:tenant`
- `InstanceAdmin`: All scopes including `admin:instance`

Validators (`CreateExternalApiKeyDtoValidator`, `UpdateExternalApiKeyPolicyDtoValidator`) reject requests containing scopes above the owner ceiling.

### Forwarded Headers Interaction

External API keys are often presented by callers behind reverse proxies. The `ForwardedHeadersTrust` settings (see above) determine which proxies are trusted to forward `X-Forwarded-For`/`X-Forwarded-Host`. When an API-key caller comes through a trusted proxy, the rate-limit partition key remains `api-key:{keyId}` — the forwarded IP is used only for `LastUsedIp` telemetry and logging.

When `TrustLoopbackProxy=true` (Aspire-style local development), loopback proxies are trusted for forwarded headers but untrusted proxies still have their `X-Forwarded-*` headers dropped before middleware sees them.

## Related

- [MULTI_TENANCY.md](MULTI_TENANCY.md)
- [RENDER_POLICIES.md](RENDER_POLICIES.md)
- [OPERATIONS.md](OPERATIONS.md)
- [CUSTOM_PROPERTIES.md](CUSTOM_PROPERTIES.md)
- [LOCALIZATION.md](LOCALIZATION.md)
