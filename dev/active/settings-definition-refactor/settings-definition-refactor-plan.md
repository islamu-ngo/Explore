# Settings Definition System Refactor — Implementation Plan

Last Updated: 2026-03-18

## Senior Architect Review Status: APPROVED WITH GUARDRAILS

**Verdict:** The architecture is fundamentally correct. Code-defined definitions + hierarchical resolver + typed groups + centralized validated writes is the right model for an enterprise-grade, self-hostable, multi-tenant system with runtime-configurable behavior.

**Non-negotiable guardrails added (see Architectural Guardrails section).**

---

## Executive Summary

The settings system has a solid foundation (code-defined `SettingDefinition` records, `FrozenDictionary`-backed `SettingRegistry`, 5-tier hierarchical resolver) but suffers from three critical implementation problems:

1. **GovernanceSettingKeys bloat** — 320 lines of dual-layer constants (nested classes + flat aliases) creating maintenance burden and confusion
2. **InstanceGovernanceSettingService is a 780-line god service** — makes 40+ individual `GetByKey` DB calls per read, manually maps every setting, duplicates defaults that already exist in `SettingDefinition.DefaultValue`
3. **ISettingGroup pattern incomplete** — only 5 of 14 categories have typed group implementations, while the god service manually handles the rest

The fix: eliminate the god service by completing the `ISettingGroup` pattern for all categories, use `IHierarchicalSettingsResolver.ResolveGroupAsync<T>()` for batch reads, kill the flat aliases, and make each sub-resource endpoint resolve/write through the typed group directly.

---

## Architectural Guardrails (Non-Negotiable)

These guardrails were established during senior architect review. They define the **boundaries** of the settings system and must be enforced throughout all phases.

### Guardrail 1: Four Concerns Must Stay Separate

Settings must NOT become a dumping ground. These are distinct domains:

| Concern | Where It Lives | Example |
|---------|---------------|---------|
| **Settings** — runtime config with scope inheritance | `SettingDefinition` + `SettingRegistry` + `IHierarchicalSettingsResolver` | `branding.display_name`, `events.user_submission_enabled` |
| **Capabilities/Entitlements** — what a tenant/plan is *allowed* to use | Module system (`ITenantCapabilityRepository`, `IModuleDefinitionRepository`) | "tenant has events module", "org has premium analytics" |
| **Authorization/Policy** — who may do something | Cerbos + `[Authorize]` attributes | "user X can edit settings", "admins can submit events" |
| **Secrets/Credentials** — sensitive operational values | `InfrastructureSecretSettingKeys` + stricter handling (masked reads, no casual round-tripping, stronger audit) | SMTP password, S3 secret key, API tokens |

**Enforcement:** During Phase 3, when extracting module capability logic from the god service, it MUST go to a dedicated service, not be absorbed into the settings resolver.

### Guardrail 2: SettingDefinition Is the Single Metadata Authority

`SettingDefinition` owns ALL metadata about a setting:
- Key, ValueType, DefaultValue, Category, Description
- MinScope, MaxScope (allowed scope range)
- IsLockable, IsSensitive
- AllowedValues

No service, handler, or controller may duplicate or override this metadata. Defaults come from the definition. Scope rules come from the definition. Validation rules come from the definition.

### Guardrail 3: Typed Groups for Reads, Explicit Commands for Writes

- **Reads:** `ISettingGroup` implementations via `ResolveGroupAsync<T>()` — batch loading, typed, clean
- **Writes:** Explicit DTOs/commands with richer semantics than just "set value":
  - Set explicit value at scope
  - Remove override (revert to inherited/default) — **first-class operation**
  - Lock setting at scope
  - Validate against `AllowedValues` and scope rules

Write operations must always flow through: API Controller → MediatR Command → Handler → `IHierarchicalSettingsResolver.SetValueAsync()` / `RemoveOverrideAsync()`. No direct controller → resolver bypass.

### Guardrail 4: Reset-to-Inherited Is a First-Class Write Operation

Settings have three states, not two:
1. No override exists → inherit from parent scope
2. Override exists with explicit value
3. Override removed/reset → revert to inherited

The API must support: `DELETE /api/instance/settings/{category}/{key}` to remove an override. `RemoveOverrideAsync()` already exists on the resolver — it must be exposed through the API.

### Guardrail 5: Cache Invalidation Is a Core Design Concern

- Cache key shape: `HierSettings:{Scope}:{ScopeId}`
- Writes evict the affected scope + all child scopes
- `SettingChangedNotification` triggers invalidation
- For future multi-node: document that notification-based invalidation needs a distributed pub/sub when scaling beyond single instance
- Eventual consistency is acceptable (5-minute TTL is the upper bound)

### Guardrail 6: Observability at the Resolver Layer

Add metrics/logging for:
- Resolve latency (batch vs single)
- Cache hit/miss rates
- DB query count per resolution
- Validation failures on writes
- Lock-denied writes
- Write counts by scope/category

This is a Phase 5+ concern but should be considered during Phase 3 rewrite (structured logging at minimum).

### Guardrail 7: Scope Policy Validation in Write Path

The write path must validate:
- Is this key registered in `SettingRegistry`?
- Is the requested scope within the definition's `MinScope`–`MaxScope` range?
- Is this setting locked by a higher scope?
- Is the value in `AllowedValues` (when non-null)?
- Is the caller authorized to write at this scope?

Most of this already exists in `HierarchicalSettingsResolver.SetValueAsync()` — ensure it's complete and not bypassed.

---

## Current State Analysis

### What Works Well (Keep)
- `SettingDefinition` sealed record — immutable, well-designed, correct properties
- `SettingRegistry` with `FrozenDictionary` — O(1) lookup, thread-safe, compile-time populated
- `IHierarchicalSettingsResolver` — 5-tier cascade with lock semantics, batch loading, caching
- `SettingUpsertService` — centralized write with audit notifications
- `SettingChangedNotification` — MediatR-based audit trail
- Per-category definition files (14 files, ~80 definitions)
- Architecture tests validating registry alignment

### What's Broken

#### Problem 1: GovernanceSettingKeys (320 lines)
**File:** `Explore.Domain/Constants/GovernanceSettingKeys.cs`

- Contains nested static classes (`Routing.RenderPolicy.Fallback.RenderMode`) — good for discovery
- ALSO contains ~100 flat aliases (`RoutingRenderPolicyGlobalRenderMode`) — pure duplication
- Flat aliases exist "for backward compatibility" but this is a dev-only project with no external consumers
- Every new setting requires adding both forms
- Some flat aliases reference `InfrastructureSecretSettingKeys` which crosses concerns

**Fix:** Delete all flat aliases. Only keep nested classes. Consumers use `GovernanceSettingKeys.Routing.DefaultPublicHomePage` directly.

#### Problem 2: InstanceGovernanceSettingService (780 lines)
**File:** `Explore.Application/Services/InstanceGovernanceSettingService.cs`

- `ReadSettingsAsync()` makes **40+ individual** `_systemSettingRepository.GetByKey()` calls — each a separate DB query
- Manually deserializes every value with inline defaults that **duplicate** `SettingDefinition.DefaultValue`
- Manually maps to `InstanceGovernanceSettings` DTO with hardcoded property assignments
- Each `Apply*Async()` method manually creates `SystemSetting` objects and calls `SettingUpsertService`
- The entire service is one massive hand-rolled mapping layer between settings keys and DTOs

**Fix:** Replace with `ISettingGroup`-based resolution. Each sub-resource DTO becomes (or wraps) a setting group. `ResolveGroupAsync<BrandingSettingGroup>()` replaces 4 individual `GetByKey` calls with one batch query. Write path uses `IHierarchicalSettingsResolver.SetValueAsync()` instead of manual `SystemSetting` construction.

#### Problem 3: ISettingGroup Incomplete Coverage
**File:** `Explore.Application/Settings/Groups/` (only 5 implementations)

Existing groups: Branding, Email, Storage, Cerbos, Analytics
Missing groups: **Events, Organizations, Groups, Routing/RenderPolicy, Domains, Security, Modules, Tenants, Deployment, TenantDelegation, Authentication, Federation, Localization**

The resolver already supports `ResolveGroupAsync<TGroup>()` but it's only used for 5 categories. The god service manually handles the other 9.

**Fix:** Create `ISettingGroup` implementations for all categories that are **consumed as a coherent unit**. Once complete, `InstanceGovernanceSettingService` collapses to a thin orchestrator calling `ResolveGroupAsync<T>()` per category.

### What Should Be Removed
- `GovernanceSettingKeys` flat aliases (lines 218-319)
- `InstanceGovernanceSettingService.ReadSettingsAsync()` manual query-per-setting pattern
- `InstanceGovernanceSettings` monolithic DTO (replaced by sub-resource DTOs + setting groups)
- `ISettingsResolver` legacy 2-tier interface (fully replaced by `IHierarchicalSettingsResolver`)
- `SettingsResolver` legacy implementation
- `TenantSettingsController` (marked deprecated, Phase 4.5 removal)

---

## Proposed Future State

### Architecture
```
Read path:
API Controller (sub-resource endpoints)
    ↓
MediatR Query
    ↓
Handler calls IHierarchicalSettingsResolver.ResolveGroupAsync<TSettingGroup>(context)
    ↓
Resolver batch-loads from DB (≤2 queries), applies cascade, returns typed group
    ↓
Handler maps SettingGroup → sub-resource DTO → API response

Write path (set value):
API Controller → MediatR Command → Handler
    → IHierarchicalSettingsResolver.SetValueAsync(key, value, scope, scopeId, actorId)
    → validates registry + scope + AllowedValues + lock
    → SettingUpsertService → DB + SettingChangedNotification → cache invalidation

Write path (reset to inherited):
API Controller → MediatR Command → Handler
    → IHierarchicalSettingsResolver.RemoveOverrideAsync(key, scope, scopeId, actorId)
    → DB delete + SettingChangedNotification → cache invalidation
```

### Key Design Decisions
1. **ISettingGroup per consumed category** — each group declares its keys and knows how to populate from `ResolvedSetting` dictionary. Categories consumed as a coherent unit get a group; a 1-key group is fine if it provides consistency.
2. **No more manual GetByKey chains** — all reads go through `ResolveGroupAsync<T>()` or `ResolveBatchAsync()`
3. **SettingDefinition.DefaultValue is the single source of truth** — no more duplicated defaults in service code (Guardrail 2)
4. **GovernanceSettingKeys nested classes remain** — they're useful for compile-time key references
5. **Flat aliases deleted** — no backward compatibility needed
6. **Sub-resource DTOs stay in Application layer** — they map to/from setting groups in handlers
7. **Typed groups for reads, explicit commands for writes** (Guardrail 3) — service becomes thin orchestration, not manual mapping
8. **All writes flow through MediatR** — no controller → resolver bypass
9. **Reset-to-inherited is a first-class operation** (Guardrail 4) — exposed through API
10. **Module capabilities stay outside the settings system** (Guardrail 1) — extracted to dedicated service

---

## Implementation Phases

### Phase 0: Fix Critical Bugs in Existing SettingGroups (Effort: S)
Fix pre-existing key mismatches that cause silent resolution failures.

**Tasks:**
1. **Task 0.1:** Fix `StorageSettingGroup` — change `storage.*` keys to `s3.*` to match `StorageSettingDefinitions`
2. **Task 0.2:** Fix `CerbosSettingGroup` — align keys with `CerbosSettingDefinitions`
3. **Task 0.3:** Fix all 4 existing groups (Email, Storage, Branding, Cerbos) to use `GovernanceSettingKeys.*` constants instead of hardcoded strings
4. **Task 0.4:** Add missing routing resolver definitions to `RoutingSettingDefinitions.cs`
5. **Task 0.5:** Build + run all tests

**Acceptance Criteria:**
- All existing groups reference `GovernanceSettingKeys.*` constants (no hardcoded key strings)
- `StorageSettingGroup.SettingKeys` matches `StorageSettingDefinitions.All` keys
- `CerbosSettingGroup.SettingKeys` matches `CerbosSettingDefinitions.All` keys
- All routing resolver keys registered in `SettingRegistry`
- All tests pass

---

### Phase 1: Clean Up GovernanceSettingKeys (Effort: S)
Remove flat aliases, update all consumers to use nested class references.

**Tasks:**
1. **Task 1.1:** Delete lines 218-319 of `GovernanceSettingKeys.cs` (all flat aliases)
2. **Task 1.2:** Find-and-replace all flat alias usages across codebase to use nested form
3. **Task 1.3:** Update `GovernanceSettingKeysTests` to remove flat alias tests
4. **Task 1.4:** Build + run all tests

**Acceptance Criteria:**
- Zero flat aliases in `GovernanceSettingKeys.cs`
- All tests pass
- No `GovernanceSettingKeys.Branding*` (flat) references — only `GovernanceSettingKeys.Branding.DisplayName` (nested)

**Skill:** `clean-architecture-rules`

---

### Phase 2: Create Missing ISettingGroup Implementations (Effort: M)
Create typed setting groups for all missing categories consumed as coherent units.

**Tasks:**
1. **Task 2.1:** Create `EventSettingGroup` (6 keys: user/org/group submission, require_approval, card_click, max_sessions)
2. **Task 2.2:** Create `OrganizationSettingGroup` (3 keys: verification_required, tenant_can_omit, self_registration)
3. **Task 2.3:** Create `GroupSettingGroup` (2 keys: self_registration, require_approval)
4. **Task 2.4:** Create `ModuleSettingGroup` (2 keys: islamic_enabled, tech_enabled)
5. **Task 2.5:** Create `DomainSettingGroup` (4 keys: instance_base_domain, allow_custom, subdomain, custom_domain)
6. **Task 2.6:** Create `RoutingSettingGroup` (6 keys: default_home_page, resolver flags, path_prefix)
7. **Task 2.7:** Create `RenderPolicySettingGroup` (14 keys: version, preset, advanced, per-context render modes + prerender + lock flags)
8. **Task 2.8:** Create `TenantDelegationSettingGroup` (5 keys: self_service, white_labeling, lock_smtp/storage/analytics)
9. **Task 2.9:** Create `DeploymentSettingGroup` (1 key: deployment.mode)
10. **Task 2.10:** Unit tests for each new setting group (Populate method, default values, edge cases)

**Note:** Every category consumed as a coherent unit gets a group. 1-key groups (Deployment) are fine for consistency. If a category only has one setting that's never read in isolation, use judgment — but default to creating the group.

**Acceptance Criteria:**
- All categories have `ISettingGroup` implementations
- Each group's `SettingKeys` returns exactly the keys from its definition file (using `GovernanceSettingKeys.*` constants)
- Each group's `Populate()` correctly deserializes all supported value types
- All tests pass

**Location:** `Explore.Application/Settings/Groups/`
**Skill:** `clean-architecture-rules`

---

### Phase 3: Rewrite InstanceGovernanceSettingService (Effort: L)
Replace 780-line god service with thin orchestrator using setting groups.

**Tasks:**
1. **Task 3.1:** Rewrite `ReadSettingsAsync()` to use `ResolveGroupAsync<T>()` per category instead of 40+ individual queries
2. **Task 3.2:** Rewrite each `Apply*Async()` method to use `IHierarchicalSettingsResolver.SetValueAsync()` instead of manual `SystemSetting` construction
3. **Task 3.3:** Remove all duplicated default value constants (they live in `SettingDefinition.DefaultValue` — Guardrail 2)
4. **Task 3.4:** Extract module capability logic to a dedicated `ModuleCapabilityService` (Guardrail 1 — capabilities ≠ settings)
5. **Task 3.5:** Add structured logging at resolution points (Guardrail 6 — observability foundation)
6. **Task 3.6:** Update `InstanceGovernanceSettingServiceTests` to verify batch resolution instead of individual queries
7. **Task 3.7:** Build + run all tests

**Acceptance Criteria:**
- Service is clean orchestration only (no per-key data access, no duplicated defaults, no hidden policy logic, no manual mapping sprawl)
- Zero `_systemSettingRepository.GetByKey()` calls in ReadSettingsAsync (uses resolver instead)
- Zero duplicated default values
- Module capability logic lives in `ModuleCapabilityService`, not in the settings service
- All existing tests pass or are updated
- DB query count for ReadSettingsAsync drops from ~40 to ≤2

**Skill:** `cqrs-mediatr-guidelines`, `clean-architecture-rules`

---

### Phase 4: Delete Legacy Code (Effort: S)
Remove deprecated interfaces and implementations.

**Tasks:**
1. **Task 4.1:** Delete `ISettingsResolver` (legacy 2-tier interface)
2. **Task 4.2:** Delete `SettingsResolver` (legacy implementation)
3. **Task 4.3:** Delete `TenantSettingsController` (deprecated CRUD controller)
4. **Task 4.4:** Remove `InstanceGovernanceSettings` monolithic DTO if no longer referenced
5. **Task 4.5:** Update DI registration to remove legacy resolver
6. **Task 4.6:** Build + run all tests

**Acceptance Criteria:**
- Zero references to `ISettingsResolver` or `SettingsResolver`
- Zero references to `TenantSettingsController`
- All tests pass
- DI registrations cleaned up

---

### Phase 5: Validation, Architecture Tests & Observability (Effort: M)
Strengthen the system with the guardrails from architect review.

**Tasks:**
1. **Task 5.1:** Complete `AllowedValues` validation in `HierarchicalSettingsResolver.SetValueAsync()` — reject values not in `AllowedValues` when non-null
2. **Task 5.2:** Complete scope policy validation in write path — verify key exists in registry, scope is within `MinScope`–`MaxScope`, lock is respected (Guardrail 7)
3. **Task 5.3:** Expose reset-to-inherited as API endpoint: `DELETE /api/instance/settings/{category}/{key}` → `RemoveOverrideAsync()` (Guardrail 4)
4. **Task 5.4:** Architecture test: every `ISettingGroup.SettingKeys` entry must exist in `SettingRegistry`
5. **Task 5.5:** Architecture test: every category in `SettingRegistry` must have a corresponding `ISettingGroup` implementation
6. **Task 5.6:** Architecture test: every `SettingDefinition` key matches its definition class naming pattern
7. **Task 5.7:** Add resolver-level structured logging: cache hit/miss, batch size, DB query count, validation failures, lock-denied writes (Guardrail 6)
8. **Task 5.8:** Document cache invalidation strategy and multi-node considerations in `docs/CONFIGURATION.md` (Guardrail 5)
9. **Task 5.9:** Build + run all tests

**Acceptance Criteria:**
- `SetValueAsync` rejects values not in `AllowedValues` when `AllowedValues` is non-null
- `SetValueAsync` validates scope range against definition
- Reset-to-inherited is a working API endpoint
- Architecture tests catch missing groups, mismatched keys, and structural drift
- Resolver has structured logging for key operations
- Cache strategy is documented
- All tests pass

**Skill:** `clean-architecture-rules`

---

## Risk Assessment

### High Risk
- **InstanceGovernanceSettingService rewrite (Phase 3)** — This is the biggest change. The service has many consumers (controllers, onboarding handlers, tenant policy service). Need comprehensive test coverage before rewriting.
  - **Mitigation:** Write new tests first (TDD), keep the interface unchanged, only change the implementation.
- **Module capability extraction (Phase 3, Task 3.4)** — Module capability flow may be tightly coupled to the setting read flow. If `ReadSettingsAsync()` interleaves module lookups with setting lookups, extracting cleanly requires understanding the full dependency graph.
  - **Mitigation:** Map all module capability usages before extracting. Keep the extraction minimal — move only what's needed.

### Medium Risk
- **Flat alias removal (Phase 1)** — Many files reference flat aliases. Find-and-replace must be thorough.
  - **Mitigation:** Use grep to find all usages, compile to verify zero misses.

### Low Risk
- **New setting groups (Phase 2)** — Additive, follows established pattern, well-tested existing examples.
- **Legacy deletion (Phase 4)** — Straightforward if phases 1-3 succeed.
- **Architecture tests (Phase 5)** — Additive, catches future drift.

## Potential Risks & Unknowns

The most likely point of failure is **Phase 3 (rewriting InstanceGovernanceSettingService)** because it's the integration point. The service doesn't just read/write settings — it also orchestrates module capabilities (`ITenantCapabilityRepository`, `IModuleDefinitionRepository`), which means the rewrite needs to preserve that module-specific logic in a separate `ModuleCapabilityService`. If the module capability flow is tightly coupled to the setting read flow, extracting it may be more complex than anticipated. Additionally, the `ReadEffectiveSettingsForTenantAsync()` method mixes instance-level and tenant-level resolution, which could require careful adjustment of the `SettingContext` passed to `ResolveGroupAsync<T>()`.

The second risk is **cache invalidation correctness** (Guardrail 5). The current 5-minute TTL + scope-aware invalidation works for single-node. When scaling to multi-node, `SettingChangedNotification` (MediatR in-process) won't propagate across nodes. This doesn't block the refactor but must be documented and planned for.

---

## Success Metrics

| Metric | Before | After |
|--------|--------|-------|
| GovernanceSettingKeys lines | 320 | ~180 |
| InstanceGovernanceSettingService lines | 780 | Clean orchestration (line count is secondary) |
| DB queries per ReadSettingsAsync | ~40 | ≤2 |
| ISettingGroup implementations | 5/14 | All consumed categories |
| Architecture test coverage for settings | Partial | Complete (structural drift prevention) |
| Legacy resolvers | 2 (ISettingsResolver + IHierarchicalSettingsResolver) | 1 |
| Write path validation | Partial (scope only) | Complete (scope + AllowedValues + lock + registry) |
| Reset-to-inherited API support | Not exposed | First-class endpoint |
| Resolver observability | None | Structured logging for key operations |

## Effort Estimates

| Phase | Effort | Dependencies |
|-------|--------|-------------|
| Phase 0: Fix Critical Bugs | S (~1h) | None |
| Phase 1: Clean GovernanceSettingKeys | S (~1h) | None (can parallel with Phase 0) |
| Phase 2: Create Missing Setting Groups | M (~3h) | Phase 0 (for consistent patterns) |
| Phase 3: Rewrite God Service | L (~4h) | Phase 1 + Phase 2 |
| Phase 4: Delete Legacy Code | S (~1h) | Phase 3 |
| Phase 5: Validation, Tests & Observability | M (~3h) | Phase 2 + Phase 3 |
| **Total** | **~13h** | |
