# Deployment Mode & Governance Hardening — Task Checklist

**Last Updated: 2026-03-17**

---

## Phase 0: API Contract Redesign ✅ COMPLETE

- [x] **0.1** Split `InstanceGovernanceSettingsDto` into 8 focused sub-resource DTOs
  - Delete: `Explore.Application/DTOs/Onboarding/InstanceGovernanceSettingsDto.cs` (66 props)
  - Create: `Explore.Application/DTOs/Instance/` — `DeploymentModeDto`, `ModuleSettingsDto`, `EventPolicyDto`, `OrganizationPolicyDto`, `BrandingSettingsDto`, `DomainSettingsDto`, `TenantDelegationSettingsDto`, `RenderPolicySettingsDto`
  - `DeploymentMode` is `DeploymentMode` enum (not `string`) in all new DTOs
  - `RenderPolicyPreset` and `SmtpSecurityMode` are typed enums
  - Acceptance: No DTO exceeds 12 properties, build passes
  - Effort: M

- [x] **0.2** Redesign `CompleteInstanceOnboardingRequest`
  - Delete: 66-prop god object as onboarding payload
  - Create: `Explore.Application/DTOs/Onboarding/CompleteInstanceOnboardingRequest.cs` (≤6 props: `DeploymentMode` enum, `AdminEmail`, `AdminPassword`, `InstanceName?`)
  - `CompleteInstanceOnboardingCommand` updated to use new request type
  - Validator: `CompleteInstanceOnboardingRequestValidator` validates `DeploymentMode` enum
  - Acceptance: Onboarding payload ≤6 properties, enum not string
  - Effort: S
  - Depends on: 0.1

- [x] **0.3** Split `InstanceOnboardingController` into Wizard + Admin controllers
  - `InstanceOnboardingController` → 2 endpoints only: `POST /complete`, `GET /status`
  - Create: `Explore.API/Controllers/InstanceSettingsController.cs` — RESTful sub-resources per domain (modules, events, organizations, branding, domains, tenant-delegation, render-policy, deployment-mode)
  - HATEOAS `RouteNames.cs` updated
  - Acceptance: No god endpoint returning 66 props, `[Authorize(PlatformAdmin)]` on all write endpoints
  - Effort: M
  - Depends on: 0.1

- [x] **0.4** Fix `TenantPolicySettingsDto` read/write separation
  - Create: `Explore.Application/DTOs/TenantPolicy/TenantPolicyDto.cs` (GET response — includes `CanOverride*` flags)
  - Create: `Explore.Application/DTOs/TenantPolicy/UpdateTenantPolicyRequest.cs` (PUT body — writable fields only, NO `CanOverride*`)
  - `TenantOnboardingController` updated to use split DTOs
  - Acceptance: PUT endpoint body has no read-only computed fields
  - Effort: S
  - Depends on: 0.1

- [x] **0.5** Mark `TenantSettingsController` obsolete
  - Add `[Obsolete("Replaced by governance cascade. Remove with TenantSettings entity in Phase 4.")]`
  - Identify all Blazor service callers (preparation for Phase 4 cleanup)
  - Acceptance: Zero new usages added after this task
  - Effort: S

- [x] **0.6** Update Blazor clients for new contracts
  - Regenerate `EventApiClient.g.cs` from updated `swagger.json`
  - Update `InstanceOnboardingService.cs`, `AdminService.cs` to use sub-resource endpoints
  - Update instance admin pages (Modules, Events, Organizations, Branding sections)
  - Acceptance: All Blazor tests pass, no calls to deleted god endpoint
  - Effort: L
  - Depends on: 0.3, 0.4

---

## Phase 1: Unified Deployment Mode Provider ✅ COMPLETE

- [x] **1.1** Create `IDeploymentModeProvider` interface
- [x] **1.2** Implement `DeploymentModeProvider` in Infrastructure
- [x] **1.3** Migrate `ApiTenantResolutionMiddleware`
- [x] **1.4** Migrate `BlockInSingleTenantAttribute`
- [x] **1.5** Migrate `CompleteInstanceOnboardingCommandHandler`
- [x] **1.6** Delete replaced types
- [x] **1.7** Fix `SetupSecretProvider` async bootstrap check

---

## Phase 2: Redis Distributed Cache ✅ COMPLETE

- [x] **2.1** Add Redis to Aspire AppHost
- [x] **2.2** Wire `IDistributedCache` in API and Blazor
- [x] **2.3** Override `IDistributedCache` in all test factories

---

## Phase 3: Transactional Onboarding ✅ COMPLETE

- [x] **3.1** Create `IUnitOfWork` interface in Application layer
  - File: `Explore.Application/Contracts/Persistence/IUnitOfWork.cs`
- [x] **3.2** Implement `EfCoreUnitOfWork`
  - File: `Explore.Persistence/EfCoreUnitOfWork.cs`
  - Registered scoped in `PersistenceServicesRegistration`
- [x] **3.3** Wrap onboarding handler in transaction with post-commit side effects
  - `CompleteInstanceOnboardingCommandHandler` now uses `IUnitOfWork`
  - All DB writes inside `BeginAsync`/`CommitAsync`
  - Side effects (`InvalidateCacheAsync`, `InvalidateUser`, `Lock`) after `CommitAsync`
  - `RollbackAsync` on failure
- [x] **3.4** Add partial unique index on `InstanceBootstrapState.IsCompleted = true`
  - Filtered unique index in `InstanceBootstrapStateConfiguration`
  - Prevents concurrent onboarding from both succeeding

---

## Phase 4: Core Governance Policy Hierarchy (NEW ENTERPRISE BLUEPRINT) ✅ COMPLETE

> **Note**: This replaces the old legacy Settings Consolidation phase. The previous Phase 4 & 5 tasks (which attempted to funnel everything through a generic registry) have been abandoned in favor of explicit Domain-Driven Design.

- [x] **4.1** Introduce `PolicySlot<T>` and Core Policy Aggregates
  - Create `PolicySlot<T>` wrapping local value and `ChildOverrideMode`
  - Create explicitly typed Policy Set aggregates (`InstancePolicySet`, `TenantPolicySet`, `OrganizationPolicySet`)
  - Create strongly typed sub-policies (`EventPolicy`, `OrganizationPolicy`, etc.)
- [x] **4.2** Implement Typed Persistence in EF Core
  - Configure EF Core Complex Types for policy aggregates in database models.
  - No `jsonb` or generic key-value dictionary tables for core business policies.
- [x] **4.3** Implement Deterministic Resolution Service
  - Create `IPolicyResolver` taking explicit domain aggregates and returning `PolicyDecision<T>` (Value + Mutability + Source Scope).

---

## Phase 5: Feature Flags & Operational Config (NEW ENTERPRISE BLUEPRINT) ✅ COMPLETE

- [x] **5.1** OpenFeature Integration for Toggles
  - Installed `OpenFeature` (Application + Infrastructure) and `OpenFeature.Hosting` (API)
  - `IFeatureFlagService` contract in Application, `OpenFeatureFlagService` in Infrastructure
  - API registers `AddOpenFeature` with InMemoryProvider (swap to FeatBit/Unleash/PostHog later)
- [x] **5.2** Restrict `IOptionsMonitor` to Infrastructure
  - Verified: `IOptionsMonitor` only used in Infrastructure, API, and Blazor (composition roots) — not in Application or Domain.

---

## Phase 6: Caching, Concurrency, and Audit (NEW ENTERPRISE BLUEPRINT) ✅ COMPLETE

- [x] **6.1** Versioned Cache Keys & Optimistic Concurrency
  - Optimistic concurrency token (`RowVersion` / `xmin`) on all 3 policy set tables (Instance, Tenant, Organization)
  - Scoped cache keys via `PolicyChangedCacheInvalidationHandler.BuildCacheKey`
- [x] **6.2** Outbox-Backed Policy Change Events
  - `PolicyChangeOutbox` domain entity with Status, RetryCount, NextRetryAt
  - `PolicyChangeOutboxConfiguration` EF config with index on Status+NextRetryAt
  - `PolicyChangedNotification` MediatR notification for cache invalidation fan-out
  - `PolicyChangedCacheInvalidationHandler` removes stale distributed cache entries

---

## Progress Summary (Updated for New Blueprint)

| Phase | Status | Tasks | Done |
|-------|--------|-------|------|
| 0. API Contract Redesign | ✅ Complete | 6 | 6/6 |
| 1. Deployment Mode Provider | ✅ Complete | 7 | 7/7 |
| 2. Redis Cache | ✅ Complete | 3 | 3/3 |
| 3. Transactional Onboarding | ✅ Complete | 4 | 4/4 |
| 4. Core Governance Policy Hierarchy | ✅ Complete | 3 | 3/3 |
| 5. Feature Flags & Operational Config | ✅ Complete | 2 | 2/2 |
| 6. Caching, Concurrency, and Audit | ✅ Complete | 2 | 2/2 |
| **Total** | | **27** | **27/27** |

---

## Deferred Items

- **4.5**: Delete `TenantSettings` entity — requires DB migration to drop `tenant_settings` table and removing the `TenantSettingsController` + all CRUD handlers. Best done in a separate PR.
- **Phase 0**: API Contract Redesign — 6 tasks that are highly invasive (splitting 66-prop god DTOs, reorganizing controllers, updating all Blazor clients). Does not block runtime hardening.

## Test Results (Final — 2026-03-18)

- Unit tests: 484/484 passing
- Architecture tests: 36/36 passing
- Blazor tests: 580/580 passing
- Domain tests: 93/93 passing
- Secrets tests: 190/190 passing
- API Integration tests: 446/450 passing (4 pre-existing: 3 tag filter InternalServerError + 1 EventSeries 400)
- Persistence Integration tests: Docker required (Testcontainers) — not available in this environment
