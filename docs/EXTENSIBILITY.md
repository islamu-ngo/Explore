ABOUTME: Extensibility model implemented in this codebase (modules + event aspects).
ABOUTME: Focuses on what exists today and explicitly calls out partial or non-wired parts.

# Extensibility

## Implemented Building Blocks

1. Aspect entities for events:
   - `EventIslamicAspect` (1:1 with `Event` via shared PK `Id`)
   - `EventTechAspect` (1:1 with `Event` via shared PK `Id`)
   - `EventSessionIslamicAspect` (1:1 with `EventSession` via shared key `EventSessionId`)
2. Module governance entities:
   - `ModuleDefinition` (global catalog)
   - `TenantCapability` (tenant-level enable/disable state)
3. Optional flexible metadata:
   - `Event.MetadataJson` (`jsonb`) for ad hoc key/value data.

## Module Keys And Defaults

Seeded module keys:
- `Mod_Core`
- `Mod_Islamic`
- `Mod_Tech`

Module definitions are seeded by `LookupTableSeeder`; tenant capabilities are seeded in `DatabaseSeeder` (development data path).

## Runtime Module Operations

`ModuleController` exposes:
- `GET /api/module/available`
- `GET /api/module/enabled`
- `GET /api/module/{moduleKey}/enabled`
- `GET /api/module/{moduleKey}/schema`
- `POST /api/module/{moduleKey}/enable`
- `POST /api/module/{moduleKey}/disable`

Important behavior:
- disabling a module marks capability `IsEnabled=false`; data rows are preserved.
- `ModuleService` caches module lookups for 5 minutes (`Modules_All`, `Modules_Tenant_{tenantId}`).

## How Modules Affect Event Queries

`GetEventListRequestHandler` applies aspect filters only when the related module is enabled for the current tenant:
- Islamic filters require `Mod_Islamic`.
- Tech filters require `Mod_Tech`.

If module-specific filters are sent while the module is disabled, they are ignored (endpoint still succeeds).

## UI-Facing Module Flags

`PublicExperienceSettingsDto` includes:
- `IsIslamicModuleEnabled`
- `IsTechModuleEnabled`
- `EnabledModules`

Blazor event list uses these flags to control filter/UI exposure.

## Current Limits (Non-Obvious)

1. Strategy infrastructure exists (`IEventStrategy`, `StrategyResolver`), but event create/update handlers do not currently call it.
2. `TechEventStrategy.IsApplicable` currently returns `false`, so strategy-based tech logic is not active in create flow.
3. `WizardSchemaUrl` and `/api/module/{moduleKey}/schema` exist, but there is no full schema-driven dynamic form pipeline wired in the current Blazor app.
4. Modules are compile-time application modules, not runtime plugin loading.

## Related

- [MODULAR_EVENTS.md](MODULAR_EVENTS.md)
- [DOMAIN.md](DOMAIN.md)
- [MULTI_TENANCY.md](MULTI_TENANCY.md)

## Translation Management System (TMS) Provider Abstraction

**Status:** Implemented.

The localization system uses the same pluggable provider pattern as analytics:

```
ITranslationManagementProvider
  ├── TolgeeTranslationProvider
  ├── WeblateTranslationProvider
  ├── OfflineTranslationProvider (default — reads bundled .json files)
  └── NullTranslationProvider (safe no-op)
```

`RuntimeTranslationProvider` wraps all concrete providers and delegates based on the `localization.tms_provider` governance setting. Falls back to `OfflineTranslationProvider` on errors.

Adding a new TMS provider:
1. Create `{Provider}TranslationProvider : ITranslationManagementProvider` in `Explore.Infrastructure/Localization/`
2. Add enum value to `TranslationManagementProviderEnum`
3. Register in `InfrastructureServicesRegistration.cs` with named HttpClient
4. Add routing case in `RuntimeTranslationProvider.ResolveProviderAsync()`

See [LOCALIZATION.md](LOCALIZATION.md) for full details.

## API Keys / Service Accounts — Planned

**Status:** Not yet implemented. Strategy documented for post-v1.0.

**Current auth model:** User-centric via OIDC (Keycloak) through BFF pattern. All API access requires a user session.

**Why machine-to-machine (M2M) is needed:**
- Enterprise consumers integrating event data into CRM, LMS, or campus systems.
- Automated event publishing from CI/CD or content pipelines.
- Partner organizations syncing events bidirectionally.

**Planned entities:**
- `ApiKey`: id (uuid v7), tenant_id, name, hashed_key (sha256), prefix (first 8 chars for identification), scopes (jsonb — list of permitted operations), expires_at, created_by, is_revoked, last_used_at.
- `ApiKeyAuditLog`: id, api_key_id, action, ip_address, timestamp.

**Design decisions:**
- Keys are tenant-scoped (no cross-tenant API keys).
- Scopes map to existing permission codes (read:events, write:events, etc.).
- Keys are hashed at rest (only shown once on creation).
- Rate limiting per key via middleware.
- Admin UI for key management (create, revoke, rotate, view usage).

## Tenant Quotas / Usage Tracking — Planned

**Status:** Not yet implemented. Strategy documented for post-v1.0.

**Why quotas matter for multi-tenant SaaS:**
- Prevent noisy-neighbor resource exhaustion.
- Enable tiered pricing (free/pro/enterprise).
- Provide usage visibility for tenant admins.

**Planned entities:**
- `TenantQuota`: id, tenant_id, resource_type (enum: events, storage_bytes, members, organizations, api_calls_per_day), max_allowed, current_usage, reset_period (monthly/none).
- `TenantUsageLog`: id, tenant_id, resource_type, delta, recorded_at (for historical tracking and billing).

**Enforcement approach:**
- MediatR pipeline behavior checks quotas before write commands.
- Soft limits: warn at 80%, block at 100%.
- Storage quota enforced at upload handler level.
- Usage counters updated via domain events (eventually consistent).
- Admin override capability for emergency situations.
