# Enterprise Tenant Onboarding - Implementation Plan

**Last Updated: 2026-02-17**

---

## Executive Summary

Transform the tenant admin onboarding experience from a minimal flat form into an enterprise-grade, wizard-based flow that supports both self-service and admin-provisioned tenant creation. The platform currently has a binary onboarding state (complete/not), no invitation system, and critical authorization gaps. This plan addresses all of these while following Clean Architecture principles and maintaining the existing cascading governance model.

### Key Deliverables
1. **Fix critical authorization gap** — Only TenantAdmin/TenantOwner/InstanceAdmin can complete onboarding (not any member)
2. **Tenant lifecycle management** — TenantStatus lookup table (Provisioning → Active → Suspended → Archived → Purged) with lifecycle audit log
3. **Tenant creation handler** — Missing `CreateTenantCommandHandler` with idempotent, transactional provisioning
4. **Invitation system** — Entity, tokens, domain whitelisting, role ceiling, email templates
5. **Self-service registration** — Implement the dead `TenantSelfServiceRegistration` flag
6. **Onboarding wizard with checklist** — Multi-step stepper with progress tracking and funnel analytics
7. **Single-tenant identity** — Enforce instance admin = tenant admin in single-tenant mode
8. **Configuration lock enforcement** — Backend-enforced lock pattern, not UI-only

### Enterprise SaaS Patterns Applied
- **Stripe**: Progressive checklist, idempotency keys for provisioning
- **Vercel**: SSO-aware invite flow, simplified team onboarding
- **Clerk**: Verified domains for auto-association, domain-based access constraints
- **WorkOS**: Domain verification (DNS TXT), tenant-scoped audit logs, self-service Admin Portal
- **Supabase**: Organization-first structure with plan selection upfront
- **AWS SaaS Lens**: Tenant-aware logs/metrics during onboarding, centralized provisioning orchestration

### Enterprise Enhancements (v2 — from Altice Informatics Feedback)
| # | Enhancement | Rationale |
|---|-------------|-----------|
| 1 | **TenantStatus Lookup Table** (not enum) | Referential integrity, metadata support (DisplayName, Description, IsActiveState), matches existing ApprovalStatus/Madhab pattern |
| 2 | **TenantLifecycleLog Table** | Full audit trail: OldStatus → NewStatus, ActorId, Reason, Timestamp. IAuditableEntity overwrite is insufficient. |
| 3 | **Transactional Provisioning** | Explicit `BeginTransactionAsync()` + idempotency checks. Prevents orphaned tenants if any step fails. |
| 4 | **Advanced Invitation Security** | Domain whitelisting, one-time token hardening (invalidate in same transaction), role ceiling enforcement |
| 5 | **Onboarding Funnel Analytics** | Track wizard step completions via existing `IAnalyticsProvider` abstraction (None/Posthog/Plausible/Rybbit/RudderStack) |
| 6 | **Backend Lock Enforcement** | `UpdateTenantPolicySettingsCommandHandler` must validate `IsLocked` on SystemSetting — not UI-only |
| 7 | **Table Summary** | TenantStatus lookup, TenantLifecycleLog, enhanced TenantInvitation |

---

## Current State Analysis

### What Exists (Verified)

| Component | File | Status |
|-----------|------|--------|
| Tenant entity | `Explore.Domain/Tenant.cs` | ⚠️ Minimal (Id, FullName, Slug, IsActive, NavigationLinks, NOT IAuditableEntity) |
| TenantMember | `Explore.Domain/TenantMember.cs` | ✅ Works (UserId, TenantId, RoleId, GrantedAt) |
| TenantOnboardingState | `Explore.Domain/TenantOnboardingState.cs` | ⚠️ Binary only (IsCompleted, CompletedAt, CompletedByUserId) |
| RoleEnum (Tenant scope) | `Explore.Domain/Enums/RoleEnum.cs` | ✅ TenantOwner=10, TenantAdmin=11, TenantModerator=12, TenantMember=13 |
| Lookup table pattern | `ApprovalStatus.cs`, `Madhab.cs`, `AnalyticsProvider.cs` | ✅ Established: Id(int), MasterCode, FullName, Description |
| LookupTableSeeder | `Explore.Persistence/Seed/LookupTableSeeder.cs` | ✅ Idempotent runtime seeding |
| ConfigurationChangeLog | `Explore.Domain/ConfigurationChangeLog.cs` | ✅ Audit log with OldValue/NewValue, Scope, ActionType |
| IAuditableEntity | `Explore.Domain/Interfaces/IAuditableEntity.cs` | ✅ 21 entities implement it |
| IAnalyticsProvider | `Explore.Application/Contracts/Infrastructure/IAnalyticsProvider.cs` | ✅ TrackAsync with runtime provider switching |
| TenantOnboardingController | `Explore.API/Controllers/TenantOnboardingController.cs` | ✅ 4 endpoints |
| CompleteTenantOnboardingCommandHandler | `Explore.Application/Features/TenantOnboarding/Handlers/Commands/` | 🔴 Weak auth (any tenant member) |
| GovernanceSettingKeys | `Explore.Domain/Constants/GovernanceSettingKeys.cs` | ✅ Has TenantSelfServiceRegistration key |
| DeploymentSettings | `Explore.Infrastructure/DeploymentSettings.cs` | ✅ IsSingleTenant/IsMultiTenant |
| Explicit transaction pattern | `Explore.Persistence/Repositories/AppSettingRepository.cs` | ✅ BulkUpdateAsync uses BeginTransactionAsync |

### What's Missing (Verified Not Found)

| Component | Impact | Priority |
|-----------|--------|----------|
| `CreateTenantCommandHandler` | 🔴 Cannot create tenants programmatically | Critical |
| `TenantInvitation` entity | 🔴 No way to invite tenant admins | Critical |
| `TenantStatus` lookup table | 🟠 No lifecycle management (only IsActive boolean) | High |
| `TenantLifecycleLog` entity | 🟠 No audit trail for tenant status transitions | High |
| Self-service registration endpoint | 🟠 `TenantSelfServiceRegistration` flag is dead code | High |
| Backend lock enforcement | 🟠 UI hides locked fields but handler doesn't validate | High |
| Onboarding progress tracking | 🟡 Binary IsCompleted, no step tracking | Medium |
| Onboarding funnel analytics | 🟡 No step completion tracking | Medium |

### Authorization Bug (Critical Fix Required)

**File**: `CompleteTenantOnboardingCommandHandler.cs`
**Issue**: Uses `IsTenantMember()` — ANY tenant member can complete onboarding
**Fix**: Must check `IsTenantAdmin()` or specific role (TenantAdmin/TenantOwner/InstanceAdmin)

---

## Proposed Future State

### Tenant Lifecycle (with Audit Trail)

```
┌─────────────┐     ┌──────────┐     ┌───────────┐     ┌──────────┐     ┌────────┐
│ Provisioning│────▶│  Active   │────▶│ Suspended  │────▶│ Archived  │────▶│ Purged  │
└─────────────┘     └──────────┘     └───────────┘     └──────────┘     └────────┘
       │                  ▲                  │                                   
       │                  └──────────────────┘                                   
       │              (Reactivate)                                               
       ▼                                                                          
  [Auto-created resources — wrapped in explicit transaction]                     
  - TenantSettings, TenantOnboardingState, TenantCapability, TenantMember       
                                                                                  
  [Every transition creates a TenantLifecycleLog entry]                          
  - OldStatusId → NewStatusId, TransitionedByUserId, Reason, Timestamp           
```

### Data Model Changes

```
TenantStatuses (Lookup Table — NEW)
├── Id (int, PK, ValueGeneratedNever)
├── MasterCode (string, required)
├── FullName (string, required)
├── Description (string?)
└── IsActiveState (bool)

Tenants (updated)
├── TenantStatusId (int, FK → TenantStatuses — NEW)
├── Description (string? — NEW)
├── IsActive (COMPUTED from TenantStatus.IsActiveState)
└── + IAuditableEntity fields

TenantLifecycleLogs (NEW)
├── Id (Guid, UUIDv7)
├── TenantId (Guid, FK → Tenants)
├── OldStatusId (int, FK → TenantStatuses)
├── NewStatusId (int, FK → TenantStatuses)
├── TransitionedByUserId (Guid)
├── Reason (string?)
├── TransitionedAt (DateTime)
└── + IAuditableEntity fields

TenantInvitations (NEW — enhanced)
├── AllowedDomain (string? — domain whitelisting)
├── RoleId (int — role ceiling enforced in handler)
└── Token (invalidated atomically on accept)
```

---

## Implementation Phases

### Phase 0: Critical Authorization Fix (Effort: S — 2-3 hours)

#### Task 0.1: Fix CompleteTenantOnboardingCommandHandler Authorization
- **File**: `CompleteTenantOnboardingCommandHandler.cs`
- Replace `IsTenantMember()` with role-based check (TenantAdmin/TenantOwner/InstanceAdmin)
- **Skills**: `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `auth-patterns`

#### Task 0.2: Fix UpdateTenantPolicySettingsCommandHandler Authorization + Lock Enforcement
- **File**: `UpdateTenantPolicySettingsCommandHandler.cs`
- Same authorization fix + **backend lock enforcement**: check `SystemSetting.IsLocked` before applying. Throw `ValidationException` if locked.
- **Skills**: `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `auth-patterns`

---

### Phase 1: Domain Layer (Effort: M — 5-7 hours)

#### Task 1.1: Create TenantStatus Lookup Table Entity + Enum
- **Files**: `Explore.Domain/TenantStatus.cs` (NEW), `Explore.Domain/Enums/TenantStatusEnum.cs` (NEW)
- Entity: Id(int), MasterCode, FullName, Description, **IsActiveState(bool)**
- Enum: Provisioning=1, Active=2, Suspended=3, Archived=4, Purged=5
- Pattern: Follows `ApprovalStatus` / `Madhab` / `AnalyticsProvider` lookup table convention

#### Task 1.2: Update Tenant Entity with StatusId FK and Description
- **File**: `Explore.Domain/Tenant.cs`
- Add `TenantStatusId` (int FK), `TenantStatus` (nav), `Description`, `IAuditableEntity`
- `IsActive` becomes computed: `TenantStatus?.IsActiveState ?? false`
- **Dependencies**: Task 1.1

#### Task 1.3: Create TenantInvitation Entity (Enhanced)
- **File**: `Explore.Domain/TenantInvitation.cs` (NEW)
- Standard fields + **AllowedDomain** (string?) for domain whitelisting
- Implements ITenantEntity, IAuditableEntity

#### Task 1.4: Extend TenantOnboardingState with Steps
- **File**: `Explore.Domain/TenantOnboardingState.cs`
- Add CurrentStep, TotalSteps, CompletedStepsJson

#### Task 1.5: Create TenantLifecycleLog Entity (NEW)
- **File**: `Explore.Domain/TenantLifecycleLog.cs` (NEW)
- OldStatusId, NewStatusId (both FK → TenantStatus), TransitionedByUserId, Reason, TransitionedAt
- Follows `ConfigurationChangeLog` before/after + who/when pattern

---

### Phase 2: Application Layer (Effort: L — 12-16 hours)

#### Task 2.1: Create CreateTenantCommandHandler
- Delegates to `ITenantProvisioningService`
- InstanceAdmin only, validates slug uniqueness

#### Task 2.2: Create SelfServiceTenantRegistrationCommand + Handler
- Checks `TenantSelfServiceRegistration` governance setting
- Reuses provisioning service

#### Task 2.3: Create ITenantProvisioningService (Transactional + Idempotent)
- **Explicit transaction** (`BeginTransactionAsync` / `CommitAsync` / `RollbackAsync`)
- **Idempotency**: check slug exists before creating
- Creates: Tenant + TenantSettings + TenantOnboardingState + TenantCapability + TenantMember + **TenantLifecycleLog** (null → Provisioning)
- Pattern: follows `AppSettingRepository.BulkUpdateAsync()` transaction pattern

#### Task 2.4: Create TenantInvitation Commands + Handlers (Enhanced Security)
- **Role ceiling**: inviter's role ≥ invitee's role (TenantAdmin can't invite TenantOwner)
- **Domain whitelisting**: validate email domain matches `AllowedDomain` on accept
- **One-time hardening**: mark accepted + invalidate token in same transaction

#### Task 2.5: Create Tenant Lifecycle Commands (with Audit Log)
- Suspend/Reactivate/Archive handlers — each creates `TenantLifecycleLog` entry
- Reason required for Suspend/Archive
- **Analytics**: `IAnalyticsProvider.TrackAsync("tenant.status_changed", { old, new, reason })`

#### Task 2.6: Update OnboardingStatus DTO with Progress
- Add CurrentStep, TotalSteps, CompletedSteps[], ProgressPercentage

#### Task 2.7: Create SaveOnboardingStepCommand + Handler (with Analytics)
- Saves intermediate wizard progress + tracks `IAnalyticsProvider.TrackAsync("onboarding.step_completed", { step_name, step_index })`
- Analytics events flow via existing runtime provider (`AnalyticsProviderEnum`: None/Posthog/Plausible/Rybbit/RudderStack)

---

### Phase 3: Infrastructure Layer (Effort: M — 5-7 hours)

#### Task 3.1: Create TenantStatus Lookup Table EF Config + Seed
- `ValueGeneratedNever()`, MaxLength constraints (follows ApprovalStatusConfiguration)
- Idempotent seed in `LookupTableSeeder.cs`: 5 statuses with IsActiveState

#### Task 3.2: Create TenantLifecycleLog EF Core Configuration
- Two FKs to TenantStatus (OldStatusId, NewStatusId), both OnDelete(Restrict)
- Index on TenantId + TransitionedAt

#### Task 3.3: Create TenantInvitation EF Core Configuration
- Token unique index, TenantId+Email composite, AllowedDomain MaxLength(255)

#### Task 3.4: Create ITenantInvitationRepository + Implementation
#### Task 3.5: Create ITenantLifecycleLogRepository + Implementation
#### Task 3.6: Update Tenant EF Core Configuration (StatusId FK, Description, audit fields)
#### Task 3.7: Update TenantOnboardingState Configuration (step tracking columns)
#### Task 3.8: Create EF Core Migration (existing tenants default to Active)
#### Task 3.9: Register New Services in DI

---

### Phase 4: API Layer (Effort: M — 4-6 hours)

#### Task 4.1: Create TenantInvitationController (5 endpoints)
#### Task 4.2: Add Self-Service Registration Endpoint
#### Task 4.3: Add Tenant Lifecycle Endpoints (+ `GET lifecycle` for history)
#### Task 4.4: Add Onboarding Step Save Endpoint
#### Task 4.5: Update Cerbos Policies

---

### Phase 5: Blazor UI (Effort: L — 12-16 hours)

#### Task 5.1: Create TenantOnboardingWizard Component (MudStepper, 4 steps, lock indicators)
#### Task 5.2: Update TenantOnboarding.razor Page
#### Task 5.3: Create TenantInvitationManagement (role ceiling in UI)
#### Task 5.4: Create Self-Service Tenant Registration Page
#### Task 5.5: Create TenantInvitationAcceptPage (domain mismatch error)
#### Task 5.6: Add Client Services

---

### Phase 6: Testing & Documentation (Effort: M — 7-9 hours)

#### Task 6.1: Unit Tests (transaction rollback, domain whitelist, role ceiling, lock enforcement, analytics calls)
#### Task 6.2: Integration Tests
#### Task 6.3: Blazor Component Tests
#### Task 6.4: Update Documentation

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Breaking Tenant.IsActive | High | High | Computed property from TenantStatus.IsActiveState. Grep all IsActive assignments. |
| Orphaned tenants | Medium | High | Explicit transaction in TenantProvisioningService |
| Domain whitelist bypass | Low | High | Backend validation in handler, not UI-only |
| Locked setting override | Medium | High | Backend ValidationException in handler |
| Migration on existing data | Low | Medium | Existing tenants default to TenantStatusId=2 (Active) |
| Analytics overhead | Low | Low | IAnalyticsProvider is fire-and-forget with try/catch |

## Effort Summary

| Phase | Tasks | Effort |
|-------|-------|--------|
| Phase 0: Auth Fix + Lock | 2 | S (2-3h) |
| Phase 1: Domain (+lookup +lifecycle log) | 5 | M (5-7h) |
| Phase 2: Application (+transactions +security +analytics) | 7 | L (12-16h) |
| Phase 3: Infrastructure (+seed +lifecycle config) | 9 | M (5-7h) |
| Phase 4: API (+lifecycle history) | 5 | M (4-6h) |
| Phase 5: Blazor UI | 6 | L (12-16h) |
| Phase 6: Testing & Docs | 4 | M (7-9h) |
| **Total** | **38** | **XL (47-64h)** |
