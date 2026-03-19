# Settings Architecture Refactor — Task Checklist

**Last Updated: 2026-02-27**

---

## Phase 1: Foundation — Setting Definition Registry & Shared Utilities
**Layer:** Domain + Application | **Effort:** L | **Risk:** Low

- [ ] **1.1** Create `SettingDefinition` record type + `SettingScope` enum → `Explore.Domain/Settings/`
- [ ] **1.2** Create `SettingRegistry` static class → `Explore.Domain/Settings/SettingRegistry.cs`
- [ ] **1.3** Create per-category definition classes (Email, Storage, Branding, Deployment, Event, Organization, Module, Domain, Analytics, Security, Routing, Tenant) → `Explore.Domain/Settings/Definitions/`
- [ ] **1.4** Extract shared `SettingValueSerializer` → `Explore.Application/Settings/SettingValueSerializer.cs`
- [ ] **1.5** Extract shared `SettingUpsertService` → `Explore.Application/Settings/SettingUpsertService.cs`
- [ ] **1.6** Unit tests for registry + serializer → `Event.Application.UnitTests/Settings/`
- [ ] **1.7** Verify: `dotnet build` + all unit tests pass

---

## Phase 2: Hierarchical Resolver
**Layer:** Application (contract) + Infrastructure (impl) | **Effort:** L | **Risk:** Medium

- [ ] **2.1** Define `IHierarchicalSettingsResolver` contract → `Explore.Application/Contracts/Infrastructure/`
- [ ] **2.2** Define `SettingContext` value object → `Explore.Application/Models/SettingContext.cs`
- [ ] **2.3** Implement `HierarchicalSettingsResolver` → `Explore.Infrastructure/Services/`
  - [ ] Batch-loads all scope tables in ≤2 queries
  - [ ] 5-tier cascade: Instance → Tenant → Org → Group → User
  - [ ] Lock semantics checked at each level
  - [ ] Validates writes against `SettingDefinition` scope range and allowed values
  - [ ] Routes all writes through `ConfigurationChangeLogService`
- [ ] **2.4** Implement `SettingsCacheManager` → `Explore.Infrastructure/Services/`
  - [ ] Composite cache keys: `Settings:{scope}:{scopeId}`
  - [ ] Scope-aware invalidation cascades
  - [ ] `IMemoryCache` primary, `IDistributedCache`-ready interface
- [ ] **2.5** Adapter: `SettingsResolver` delegates to `IHierarchicalSettingsResolver` (backward compat)
- [ ] **2.6** Unit tests for resolver: cascade, locks, batch, cache invalidation, scope rejection
- [ ] **2.7** Register `IHierarchicalSettingsResolver` in DI container
- [ ] **2.8** Verify: `dotnet build` + all tests pass

---

## Phase 3: New Scope Entities
**Layer:** Domain + Persistence | **Effort:** M | **Risk:** Low

- [ ] **3.1** Create `OrganizationSetting` entity → `Explore.Domain/OrganizationSetting.cs`
- [ ] **3.2** Create `GroupSetting` entity → `Explore.Domain/GroupSetting.cs`
- [ ] **3.3** Create `UserPreference` entity → `Explore.Domain/UserPreference.cs`
- [ ] **3.4** Add `Group = 4` and `User = 5` to `ConfigurationScopeEnum`
- [ ] **3.5** EF configurations → `Explore.Persistence/Configurations/Entities/`
  - [ ] `OrganizationSettingConfiguration.cs` — unique index on (OrganizationId, SettingKey)
  - [ ] `GroupSettingConfiguration.cs` — unique index on (GroupId, SettingKey)
  - [ ] `UserPreferenceConfiguration.cs` — unique index on (UserId, SettingKey)
- [ ] **3.6** Repository interfaces → `Explore.Application/Contracts/Persistence/`
- [ ] **3.7** Repository implementations → `Explore.Persistence/Repositories/`
- [ ] **3.8** Register repositories in DI
- [ ] **3.9** EF migration: `dotnet ef migrations add AddHierarchicalSettings`
- [ ] **3.10** Wire new repos into `HierarchicalSettingsResolver`
- [ ] **3.11** Verify: `dotnet build` + migration applies cleanly + all tests pass

---

## Phase 4: Strongly-Typed Setting Groups
**Layer:** Application | **Effort:** M | **Risk:** Low

- [ ] **4.1** Define `ISettingGroup` interface → `Explore.Application/Settings/ISettingGroup.cs`
- [ ] **4.2** Create `EmailSettingGroup` → `Explore.Application/Settings/Groups/`
- [ ] **4.3** Create `StorageSettingGroup`
- [ ] **4.4** Create `BrandingSettingGroup`
- [ ] **4.5** Create `AnalyticsSettingGroup`
- [ ] **4.6** Create `CerbosSettingGroup`
- [ ] **4.7** Create `EventPolicySettingGroup`
- [ ] **4.8** Create `DeploymentSettingGroup`
- [ ] **4.9** Create `RoutingSettingGroup`
- [ ] **4.10** Refactor `SmtpConfigResolver` → use `EmailSettingGroup`, remove own cache
- [ ] **4.11** Refactor `S3ConfigResolver` → use `StorageSettingGroup`, remove own cache
- [ ] **4.12** Refactor `AnalyticsConfigResolver` → use `AnalyticsSettingGroup`, remove own cache
- [ ] **4.13** Refactor `CerbosConfigResolver` → use `CerbosSettingGroup`, remove own cache
- [ ] **4.14** Unit tests for all setting groups
- [ ] **4.15** Verify: `dotnet build` + all tests pass

---

## Phase 5: Migrate Consumers — Kill N+1 and Copy-Paste
**Layer:** Application | **Effort:** L | **Risk:** Medium

- [ ] **5.1** Refactor `InstanceGovernanceSettingService` — use batch resolve instead of 30+ GetByKey()
- [ ] **5.2** Refactor `InstanceStorageSettingService` — use shared serializer + upsert service
- [ ] **5.3** Refactor `InstanceSmtpSettingService` — use shared serializer + upsert service
- [ ] **5.4** Refactor `TenantPolicySettingService` — use batch resolve (~27 queries → 2)
- [ ] **5.5** Refactor `GetPublicExperienceSettingsQueryHandler` — use batch resolve
- [ ] **5.6** Integration tests: hierarchical cascade end-to-end
- [ ] **5.7** Verify: full test suite passes (unit + integration)

---

## Phase 6: Deprecate Legacy TenantSettings Entity
**Layer:** Cross-cutting | **Effort:** M | **Risk:** Medium

- [ ] **6.1** Data migration script: `TenantSettings` columns → `TenantSetting` EAV rows
- [ ] **6.2** Update `TenantSettingsController` → use hierarchical resolver
- [ ] **6.3** Update `TenantSettingsDto` / Hateoas policies
- [ ] **6.4** Remove `TenantSettings.cs` entity + repository + EF config
- [ ] **6.5** Regenerate API client (`EventApiClient.g.cs`)
- [ ] **6.6** Update Blazor components referencing `TenantSettingsDto`
- [ ] **6.7** DB migration: drop legacy `TenantSettings` table
- [ ] **6.8** Verify: full test suite passes

---

## Phase 7: Cleanup & Documentation
**Layer:** Cross-cutting | **Effort:** S | **Risk:** Low

- [ ] **7.1** Mark flat aliases in `GovernanceSettingKeys` as `[Obsolete]`
- [ ] **7.2** Update `docs/CONFIGURATION.md` — document 5-tier cascade, registry, groups
- [ ] **7.3** Update `docs/MULTI_TENANCY.md` — reference org/group/user settings
- [ ] **7.4** Update `docs/ADMIN_HIERARCHY.md` — mark as "implemented" not "planned"
- [ ] **7.5** Final architecture test: verify no direct setting table access outside resolver
- [ ] **7.6** Verify: clean build + clean test + clean lint
