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
