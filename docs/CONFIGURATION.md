ABOUTME: Documents runtime configuration sources and precedence for API, BFF, and shared infrastructure.
ABOUTME: Focuses on non-inferable key names, mapping behavior, and settings cascade rules.

# Configuration

> **Audience:** Operators | Contributors | AI agents
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-05-23
> **Source Anchors:** `Explore.API/Extensions/ConfigurationExtensions.cs`, `Explore.Blazor/Extensions/ConfigurationExtension.cs`, `Explore.Blazor/Extensions/YarpProxyExtensions.cs`, `Explore.Infrastructure/Services/HierarchicalSettingsResolver.cs`, `Explore.Infrastructure/Storage/S3ConfigResolver.cs`, `Explore.Infrastructure/Mail/SmtpConfigResolver.cs`, `Explore.Infrastructure/Services/SetupSecretProvider.cs`, `Explore.Domain/Constants/GovernanceSettingKeys.cs`, `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs`, `Explore.Domain/Secrets/SecretDefinitionRegistry.cs`, `docs/SECRETS.md`

## Runtime Configuration Sources

The system uses three configuration layers:

1. static app settings (`appsettings*.json`, environment variables, user secrets),
2. secret management (`AddInfisicalCompatibility` / `AddInfisicalBlazorCompatibility` + `AddSecretManagement`),
3. governance settings in database (`SystemSetting` + `TenantSetting`).

Secrets have an additional ownership contract that applies across the platform:

- **Application-managed** secrets/settings are saved by ISLAMU Event and editable from setup/admin UI. Saved database/application values are the runtime authority.
- **Deployment-managed** secrets/settings are controlled by environment variables, appsettings, or a configured secret provider. UI surfaces show read-only ownership badges and changes require provider refresh or redeploy/restart.
- **Deployment bootstrap** values may prefill onboarding/admin forms when no application-managed value exists. If the operator modifies and saves them, the saved application setting is used from then on.

Do not treat environment variables as absolute authority forever. In application-managed mode the precedence is: explicit saved application/database setting, then deployment bootstrap value, then default. In deployment-managed mode the selected external source is authoritative and application-managed DB values for that field are ignored.

## Deployment CI/CD Secrets

GitHub Actions deployment secrets are **workflow environment secrets**, not runtime app settings and not `SecretProvider` keys. Configure them in GitHub repository environments so production deployments can require approval before secrets are released to the deploy job.

| Environment | Secret / variable | Purpose |
|---|---|---|
| `production` secret | `COOLIFY_DEPLOY_API_WEBHOOK` | Coolify API application deployment webhook. |
| `production` secret | `COOLIFY_DEPLOY_UI_WEBHOOK` | Coolify UI application deployment webhook. |
| `production` secret | `COOLIFY_DEPLOY_TOKEN` | Bearer token used when invoking production Coolify webhooks. |
| `production` variable | `PRODUCTION_URL` | Public environment URL shown on the GitHub deployment environment. |
| `production` variable | `PRODUCTION_API_URL` | Optional API base URL for `/alive` and `/health` smoke checks. |
| `production` variable | `PRODUCTION_UI_URL` | Optional UI base URL for `/alive` and `/health` smoke checks. |
| `staging` secret | `COOLIFY_DEPLOY_API_STAGING_WEBHOOK` | Coolify staging API application deployment webhook. |
| `staging` secret | `COOLIFY_DEPLOY_UI_STAGING_WEBHOOK` | Coolify staging UI application deployment webhook. |
| `staging` secret | `COOLIFY_DEPLOY_TOKEN` | Bearer token used when invoking staging Coolify webhooks. |
| `staging` variable | `STAGING_URL` | Public environment URL shown on the GitHub deployment environment. |
| `staging` variable | `STAGING_API_URL` | Optional API base URL for `/alive` and `/health` smoke checks. |
| `staging` variable | `STAGING_UI_URL` | Optional UI base URL for `/alive` and `/health` smoke checks. |

Keep the existing secret names until the deploy workflows are consolidated. If names are normalized later, update the workflow and this table in the same change.

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
- `EmailDispatchProcessor:*` (Basic Dispatch Mode background worker)

### Cerbos Authorization Configuration

Cerbos runtime settings are the first implemented consumer of the shared secrets ownership metadata:

- `Cerbos:GrpcEndpoint` can prefill onboarding/admin forms as deployment bootstrap. Once an operator saves an application-managed endpoint, the saved setting takes precedence unless the key is explicitly deployment-managed.
- `Cerbos:AdminApi:*` configures policy package sync/status operations, not runtime authorization checks. Admin API credentials are secret-bearing and must be treated as write-only/redacted in UI and API responses.
- `Secrets:Ownership:DeploymentManagedKeys` can mark `cerbos.grpc_endpoint`, `Cerbos:AdminApi:AdminUsername`, `Cerbos:AdminApi:AdminPassword`, or `*` as deployment-managed. Deployment-managed fields are read-only in UI and ignore application-managed DB values for that field.
- Governance settings select the active provider (`AuthorizationProvider`), whether tenant customization is enabled, and per-tenant BYO values such as `cerbos.mode`, `cerbos.custom_endpoint`, `cerbos.failure_mode`, custom Admin API endpoint, and custom Admin API credentials.

Endpoint and secret safety rules:

- Non-local PDP/Admin API endpoints must use safe TLS-capable URLs. Unsafe endpoint changes are rejected before provider settings are persisted or sync/cache invalidation runs.
- Runtime failure logs must not include raw PDP/Admin API endpoints, Admin API credentials, JWTs/tokens, response bodies, or exception objects/messages.
- A tenant with `cerbos.mode=custom_endpoint` and a blank PDP endpoint remains in BYO mode. Runtime authorization applies the tenant `failure_mode` instead of falling back to the instance PDP, while any explicit BYO Admin API configuration is still preserved for package operations.
- `failure_mode=closed` activates provider-instance safe mode for local fallback decisions; `failure_mode=open` uses standard local RBAC fallback only for that tenant BYO failure path.

### Email Dispatch Processor Configuration

Basic Dispatch Mode uses PostgreSQL as the durable source of truth and the existing SMTP abstraction as the transport. It does **not** require RabbitMQ. Registration confirmation currently creates an `EmailDispatchOutbox` row in the registration transaction; `EmailDispatchProcessor` later claims due rows, rebinds tenant context, calls `IEmailService`, records attempts/receipts, and advances final delivery state.

Static worker settings bind from `EmailDispatchProcessor` and are validated at startup with `ValidateOnStart`:

| Key | Default | Description |
|---|---:|---|
| `Enabled` | `true` | Enables the Basic Dispatch Mode worker. When disabled, the `email-dispatch` readiness check reports `Degraded` intentionally. |
| `PollingIntervalSeconds` | `5` | Delay between polling loops. Must be greater than zero. |
| `BatchSize` | `50` | Maximum due outbox rows loaded per loop. Must be greater than zero. |
| `MaxAttemptCount` | `5` | Worker-level cap used with per-row `MaxAttempts` before dead-lettering. Must be greater than zero. |
| `InitialRetryDelaySeconds` | `5` | Base retry delay for failed SMTP dispatch. Must be greater than zero. |
| `MaxRetryDelaySeconds` | `3600` | Maximum retry delay cap. Must be greater than or equal to `InitialRetryDelaySeconds`. |
| `ConsumerId` | machine name | Worker identity recorded in receipts and logs. Must not be blank. |
| `VerboseLogging` | `false` | Enables additional worker logs when troubleshooting. Logs must remain free of bodies, recipients, and secrets. |

SMTP settings still come from the `email.*` governance/secret keys resolved by `SmtpConfigResolver`; the dispatch processor does not introduce new SMTP credential keys. RabbitMQ Dispatch Mode is not part of Basic mode. If RabbitMQ mode is added later, it must be a separate optional profile sharing the same `EmailDispatchOutbox` state machine rather than replacing PostgreSQL as business truth.

## Secret Provider Configuration

`Explore.Secrets` binds provider config from `SecretProvider`:

- `SecretProvider:Provider` (default `None`)
- `SecretProvider:FailFast`
- `SecretProvider:Infisical:*` (project/client credentials, paths, environment)

Refresh behavior binds from `SecretRefresh` and runs via hosted `SecretRefreshService`.

## Setup Secret And Managed Provisioning Bootstrap

`SetupSecretProvider` reads setup and managed-provisioning bootstrap keys directly from configuration at API startup.

| Key | Default | Purpose |
|---|---|---|
| `SETUP_SECRET` | generated 32-character startup secret | Optional fixed setup secret for interactive first-run onboarding. When omitted and setup mode is active, API generates a temporary secret and logs it for the operator. |
| `SETUP_SECRET_REQUIRED` | `true` | Controls whether interactive setup endpoints can validate a setup secret. `false` is effective only when trusted managed provisioning is explicitly configured; otherwise the provider fails closed and still requires a setup secret. |
| `PROVISIONING_TRUSTED` | `false` | Must be `true` before managed-provider provisioning can disable interactive setup-secret validation. |
| `PROVISIONING_MODE` | unset | Trusted values are managed-provider modes such as `managed-provider`, `managed_provider`, `managed-hosting`, or `managed`. Other values do not disable setup-secret validation. |
| `MANAGED_CLIENT_EXTERNAL_PROVIDER` | unset | Stable external provider key for the managed provisioning operator, for example an ERP or hosting-provider key. Required when `SETUP_SECRET_REQUIRED=false`. |
| `PHYSICAL_TENANCY_MODE` | unset | Deployment posture such as shared database or dedicated deployment. Required when `SETUP_SECRET_REQUIRED=false` so the operator has declared the physical tenancy model. |

Important safety behavior:

- Omitted `SETUP_SECRET_REQUIRED` defaults to `true`.
- `SETUP_SECRET_REQUIRED=false` without all trusted managed-provisioning keys is ignored and the API still requires a setup secret.
- `SETUP_SECRET_REQUIRED=false` with trusted managed provisioning does **not** make setup-secret-protected endpoints public. `ValidateSecret` returns false and those endpoints reject anonymous/no-secret calls; managed provider automation must use the authorized provisioning endpoint instead.
- Raw setup secrets are not logged when interactive setup-secret validation is disabled.

## API Compatibility Mapping (Infisical -> .NET keys)

`Explore.API.Extensions.ConfigurationExtensions` maps compatibility names into canonical .NET keys. Most mappings use `TrySet`, so existing canonical keys are not overwritten; `CERBOS_GRPC_ENDPOINT` explicitly assigns `Cerbos:GrpcEndpoint` when present.

- `DEPLOYMENT_MODE` (Infisical `/api`) -> `Deployment:Mode` (`single_tenant`/`multi_tenant` normalized to `SingleTenant`/`MultiTenant`)
- `KEYCLOAK_ENDPOINT` + `KEYCLOAK_REALM` (Infisical `/keycloak`) -> `Keycloak:Authority`, `Keycloak:MetadataAddress`, `Keycloak:AuthorizationUrl`
- Keycloak mapper defaults -> `Keycloak:Audience=islamu-event-api`, `Keycloak:RequireHttpsMetadata=true`
- `CERBOS_GRPC_ENDPOINT` (Infisical `/cerbos`) -> `Cerbos:GrpcEndpoint`
- S3 runtime values:
  - `ISLAMU_EVENT_REGION` -> `S3Settings:Region`
  - `ISLAMU_EVENT_PRIVATE_BUCKET_NAME` -> `S3Settings:BucketName`
  - `ISLAMU_EVENT_PRIVATE_ACCESS_KEY_ID` -> `S3Settings:AccessKeyId`
  - `ISLAMU_EVENT_PRIVATE_SECRET_ACCESS_KEY_ID` -> `S3Settings:SecretAccessKey`
  - `ISLAMU_EVENT_S3_ENDPOINT` -> `S3Settings:Endpoint`
  - `ISLAMU_EVENT_S3_PUBLIC_ENDPOINT` -> `S3Settings:PublicEndpoint`

Keycloak base URL: `KEYCLOAK_ENDPOINT` (Infisical `/keycloak`). No hardcoded fallback — if not set, Keycloak mapping is skipped.

Storage naming rules:

- runtime .NET settings use `S3Settings:*`;
- Compose/environment overrides use `S3Settings__*`;
- Infisical/domain secret definitions use the `STORAGE_S3_*` key family under storage paths;
- do not document new `Storage__*` keys unless source code is changed to bind them.

## Blazor Server Compatibility Mapping

`Explore.Blazor.Extensions.ConfigurationExtensions` maps Keycloak, Google, and API base URL keys for the BFF host.

API base URL: `API_ENDPOINT` (Infisical `/blazor`) maps into `ExploreApi:BaseUrl`. Runtime YARP resolution checks `ExploreApi:BaseUrl`, then Aspire service discovery keys `services__explore-api__https__0` and `services__explore-api__http__0`, then falls back to `https://localhost:7039/` when no value is configured at all.

Important behavior:

- `KEYCLOAK_CLIENT_ID` maps to `Keycloak:ClientId`; when a Keycloak authority is resolved and no client id is provided, the BFF defaults to `islamu-event-blazor`.
- `Keycloak:ClientSecret` is explicitly overridden when `KEYCLOAK_BLAZOR_CLIENT_SECRET` (Infisical) is present.
- `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` map to `Google:ClientId` and `Google:ClientSecret`.
- `Keycloak:RequireHttpsMetadata` is set to `true` when Keycloak input is mapped.

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

Sensitive runtime credentials use a separate secret-setting key space. Do not expose actual values in documentation, logs, screenshots, or issue templates.

| Concern | Governance key family | Secret-bearing key family |
|---|---|---|
| SMTP | `email.*` | `email.smtp_username`, `email.smtp_password` |
| S3-compatible storage | `s3.*` | `s3.access_key_id`, `s3.secret_access_key` |
| Authentication | `auth.*` | `auth.keycloak_client_secret`, `auth.google_client_secret` |
| Cerbos admin credentials | `cerbos.*` | `cerbos.custom_admin_username`, `cerbos.custom_admin_password` |

`SecretDefinitionRegistry` recognizes provider folders for `/api`, `/storage`, `/keycloak`, `/cerbos`, `/postgresql`, `/smtp`, `/analytics`, and `/ai`. Blazor maps Google client values from `/blazor`; do not claim Google is part of the current secret-catalog folder list unless the registry changes.

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
| `analytics.cookie_consent_enabled` | bool | `false` | Whether the cookie consent banner is shown to end users |
| `analytics.decline_behavior` | enum | `"cookieless"` | What happens when a user declines consent: `disable` (no analytics) or `cookieless` (privacy-preserving analytics) |
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

`HierarchicalSettingsResolver` resolves settings through the current scope hierarchy:

1. instance/system setting,
2. tenant setting when a tenant scope is present and the instance setting is not locked,
3. organization setting when an organization scope is present and upstream settings allow delegation,
4. group setting when a group scope is present and upstream settings allow delegation,
5. user setting when a user scope is present and upstream settings allow delegation,
6. default setting definition value.

Cache behavior uses hierarchical cache keys such as `HierSettings:System` and scope-specific keys for tenant, organization, group, and user settings. The resolver honors lock flags so a higher-scope locked value prevents lower-scope overrides.

Runtime resolvers may add more specific precedence. For S3, `S3ConfigResolver` reads database settings first (`s3.*` and `s3.access_key_id`/`s3.secret_access_key`) and falls back to `IConfiguration` (`S3Settings:*`). For SMTP, `SmtpConfigResolver` reads through the hierarchical settings resolver for governance and secret-bearing email keys.

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

Current localization/TMS settings are governance settings. Do not document TMS API keys or tokens as `SecretProvider`-backed until secret definitions or resolver support exist in source.

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

- [SECRETS.md](SECRETS.md)
- [SELF_HOSTING.md](SELF_HOSTING.md)
- [MULTI_TENANCY.md](MULTI_TENANCY.md)
- [RENDER_POLICIES.md](RENDER_POLICIES.md)
- [OPERATIONS.md](OPERATIONS.md)
- [CUSTOM_PROPERTIES.md](CUSTOM_PROPERTIES.md)
- [LOCALIZATION.md](LOCALIZATION.md)
