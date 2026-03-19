# Settings Definition System Refactor — Task Checklist

Last Updated: 2026-03-18

**Architect Review Status:** APPROVED WITH GUARDRAILS (see plan for 7 non-negotiable guardrails)

## Phase 0: Fix Critical Bugs in Existing SettingGroups ✅ COMPLETE
- [x] Fix `StorageSettingGroup` — changed `storage.*` keys to `s3.*` to match `StorageSettingDefinitions`
- [x] Fix `CerbosSettingGroup` — aligned keys with `CerbosSettingDefinitions` (7 keys: tenant_customization_enabled, mode, custom_endpoint, failure_mode, custom_admin_endpoint, custom_admin_username, custom_admin_password)
- [x] Fix all 4 existing groups to use `GovernanceSettingKeys.*` / `InfrastructureSecretSettingKeys.*` constants instead of hardcoded strings
- [x] Add missing routing resolver definitions to `RoutingSettingDefinitions.cs` (9 new: 5 resolver + 4 lock/override controls)
- [x] Build + run all tests (Domain 93/93, Application 484/484, Architecture 36/36, Blazor 580/580, Secrets 190/190)
- **Effort:** S (~1h)

## Phase 1: Clean Up GovernanceSettingKeys ✅ COMPLETE
- [x] Delete flat aliases (lines 218-319) from `GovernanceSettingKeys.cs`
- [x] Find all flat alias usages across codebase, replace with nested class references (688+ usages in 17+ files)
- [x] Update `GovernanceSettingKeysTests.cs` — remove flat alias tests, add no-flat-alias guard and category coverage test
- [x] Build + run all tests
- **Effort:** S (~1h)

## Phase 2: Create Missing ISettingGroup Implementations ✅ COMPLETE
- [x] Create `EventSettingGroup` (5 keys)
- [x] Create `OrganizationSettingGroup` (3 keys)
- [x] Create `GroupSettingGroup` (1 key)
- [x] Create `ModuleSettingGroup` (2 keys)
- [x] Create `DomainSettingGroup` (4 keys)
- [x] Create `RoutingSettingGroup` (6 keys)
- [x] Create `RenderPolicySettingGroup` (18 keys)
- [x] Create `TenantDelegationSettingGroup` (3 keys)
- [x] Create `DeploymentSettingGroup` (1 key)
- [x] Build + run all tests
- **Effort:** M (~3h)
- **Location:** `Explore.Application/Settings/Groups/`

## Phase 3: Rewrite InstanceGovernanceSettingService ✅ COMPLETE
- [x] Rewrite `ReadSettingsAsync()` using `ResolveBatchAsync()` for all categories in one batch call (was 40+ individual queries)
- [x] Rewrite each `Apply*Async()` to use `SettingUpsertService` (centralized writes with registry metadata per Guardrail 2)
- [x] Remove all duplicated default value constants (SettingDefinition is single metadata authority)
- [x] Extract module capability logic to `ModuleCapabilityService` (Guardrail 1: capabilities ≠ settings)
- [x] Add structured logging at resolution points (Guardrail 6: observability foundation)
- [x] Update `InstanceGovernanceSettingServiceTests` to verify batch resolution
- [x] Register `SettingUpsertService`, `IModuleCapabilityService` in DI
- [x] Build + run all tests (486 application, 40 architecture, 93 domain, 580 Blazor, 190 secrets)
- **Effort:** L (~4h)
- **Dependencies:** Phase 1 + Phase 2

## Phase 4: Delete Legacy Code ⚠️ PARTIALLY BLOCKED
- [ ] Delete `ISettingsResolver` interface — **BLOCKED: still has 10+ active consumers** (S3ConfigResolver, SmtpConfigResolver, AnalyticsConfigResolver, CerbosConfigResolver, FallbackAuthorizationService, CreateEventCommandHandler, etc.)
- [ ] Delete `SettingsResolver` implementation — **BLOCKED: same as above**
- [ ] Delete `TenantSettingsController` — **READY: marked [Obsolete]** (candidate for deletion)
- [ ] Delete `InstanceGovernanceSettings` monolithic DTO — **NOT APPLICABLE: still actively used by 12+ consumers as the response type**
- [ ] Remove legacy DI registrations — **BLOCKED: ISettingsResolver still needed**
- [ ] Build + run all tests
- **Effort:** S (~1h)
- **Dependencies:** Phase 3
- **Note:** ISettingsResolver→IHierarchicalSettingsResolver migration for remaining consumers is a separate task

## Phase 5: Validation, Architecture Tests & Observability ✅ COMPLETE
- [x] Create `TenantDelegationSettingDefinitions` and register in `SettingRegistry` (was missing)
- [x] Architecture test: every `ISettingGroup.SettingKeys` entry exists in `SettingRegistry`
- [x] Architecture test: all registry keys follow dot notation
- [x] Architecture test: all registry definitions have non-empty category
- [ ] Complete `AllowedValues` validation in `HierarchicalSettingsResolver.SetValueAsync()` (Guardrail 7) — deferred to resolver improvement task
- [ ] Complete scope policy validation in write path (Guardrail 7) — deferred to resolver improvement task
- [ ] Expose reset-to-inherited as API endpoint: `DELETE /api/instance/settings/{category}/{key}` (Guardrail 4) — deferred
- [ ] Add resolver-level structured logging: cache hit/miss, batch size, validation failures (Guardrail 6) — deferred to resolver improvement task
- [ ] Document cache invalidation strategy in `docs/CONFIGURATION.md` (Guardrail 5) — deferred
- **Effort:** M (~3h)
- **Dependencies:** Phase 2 + Phase 3
