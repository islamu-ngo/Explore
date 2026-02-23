# Enterprise Tenant Onboarding - Task Checklist

**Last Updated: 2026-02-18**

---

## Phase 0: Critical Authorization Fix ✅ COMPLETE
**Priority: CRITICAL | Effort: S (2-3 hours)**

- [x] **Task 0.1**: Fix CompleteTenantOnboardingCommandHandler authorization
  - File: `Explore.Application/Features/TenantOnboarding/Handlers/Commands/CompleteTenantOnboardingCommandHandler.cs`
  - Replace `IsTenantMember()` with `IsTenantAdmin()` (TenantAdmin/TenantOwner/InstanceAdmin only)
  - Write failing test first (TDD)
  - Acceptance: Regular member gets 403; admin can complete

- [x] **Task 0.2**: Fix UpdateTenantPolicySettingsCommandHandler authorization + lock enforcement
  - File: `Explore.Application/Features/TenantOnboarding/Handlers/Commands/UpdateTenantPolicySettingsCommandHandler.cs`
  - Same authorization fix as 0.1
  - **Backend lock enforcement**: check `SystemSetting.IsLocked` before applying; throw `ValidationException` if locked
  - Write failing test first (TDD)
  - Acceptance: Regular member gets 403; admin can update; locked setting throws ValidationException

---

## Phase 1: Domain Layer 🟡 IN PROGRESS (Task 1.4 unskipped)
**Priority: HIGH | Effort: M (5-7 hours)**

- [x] **Task 1.1**: Create TenantStatus lookup table entity + TenantStatusEnum
  - Files: `Explore.Domain/TenantStatus.cs` (NEW), `Explore.Domain/Enums/TenantStatusEnum.cs` (NEW)
  - Entity: Id(int), MasterCode, FullName, Description, **IsActiveState(bool)**
  - Enum: Provisioning=1, Active=2, Suspended=3, Archived=4, Purged=5
  - Pattern: Follows `ApprovalStatus`/`Madhab`/`AnalyticsProvider` lookup table convention
  - ABOUTME comment, file-scoped namespace

- [x] **Task 1.2**: Update Tenant entity with StatusId FK and Description
  - File: `Explore.Domain/Tenant.cs`
  - Add: `TenantStatusId` (int FK), `TenantStatus` (nav), `Description` (string?), `IAuditableEntity`
  - `IsActive` becomes computed: `TenantStatus?.IsActiveState ?? false`
  - FK pattern: `[ForeignKey("TenantStatus")] public int TenantStatusId`
  - Dependencies: Task 1.1

- [x] **Task 1.3**: Create TenantInvitation entity (enhanced)
  - File: `Explore.Domain/TenantInvitation.cs` (NEW)
  - Properties: Id, TenantId, Email, RoleId, Token, ExpiresAt, IsAccepted, AcceptedAt, AcceptedByUserId, InvitedByUserId
  - **AllowedDomain** (string?) for domain whitelisting
  - Implements ITenantEntity, IAuditableEntity
  - ABOUTME comment, file-scoped namespace

- [ ] **Task 1.4**: Extend TenantOnboardingState with step tracking
  - File: `Explore.Domain/TenantOnboardingState.cs`
  - Add: CurrentStep (int), TotalSteps (int), CompletedStepsJson (string?)

---

## Phase 1.6: Instance Onboarding Routing ⏳ NOT STARTED
**Priority: MEDIUM | Effort: S (2-3 hours)**

- [ ] **Task 1.6**: Update instance onboarding post-completion routing behavior
  - Files: `Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor`, `Explore.Blazor.Client/Pages/Onboarding/StartupGate.razor`
  - SingleTenant: route to preferred home page (LandingPage/EventList)
  - MultiTenant: route to instance admin area (not tenant onboarding)
  - Ensure tenant onboarding is only prompted when MultiTenant mode and tenant admin must complete it

- [x] **Task 1.5**: Create TenantLifecycleLog entity
  - File: `Explore.Domain/TenantLifecycleLog.cs` (NEW)
  - Properties: Id(Guid), TenantId(Guid FK), OldStatusId(int? FK -> TenantStatus), NewStatusId(int FK -> TenantStatus), TransitionedByUserId(Guid), Reason(string?), TransitionedAt(DateTime)
  - Follows `ConfigurationChangeLog` before/after + who/when pattern
  - Implements IAuditableEntity
  - ABOUTME comment, file-scoped namespace
  - Dependencies: Task 1.1

---

## Phase 2: Application Layer ⏳ NOT STARTED
**Priority: HIGH | Effort: L (12-16 hours)**

- [ ] **Task 2.1**: Create CreateTenantCommandHandler
  - File: `Explore.Application/Features/Tenants/Handlers/Commands/CreateTenantCommandHandler.cs` (NEW)
  - Delegates to `ITenantProvisioningService`
  - InstanceAdmin authorization, validates slug uniqueness
  - Returns `BaseCommandResponse<Guid>`
  - Dependencies: Phase 1, Task 2.3

- [ ] **Task 2.2**: Create SelfServiceTenantRegistrationCommand + Handler
  - Files: NEW command + handler in `Features/Tenants/`
  - Checks `TenantSelfServiceRegistration` governance setting
  - Reuses provisioning service from Task 2.3
  - Dependencies: Task 2.1, 2.3

- [ ] **Task 2.3**: Create ITenantProvisioningService (transactional + idempotent)
  - Files: NEW interface in `Contracts/Services/` + impl in `Services/`
  - Shared provisioning: Tenant + TenantSettings + TenantOnboardingState + TenantCapability + TenantMember(Owner) + **TenantLifecycleLog** (null -> Provisioning)
  - **Explicit transaction**: `BeginTransactionAsync()` / `CommitAsync()` / `RollbackAsync()`
  - **Idempotency**: check slug exists before creating
  - Pattern: follows `AppSettingRepository.BulkUpdateAsync()` transaction pattern
  - Dependencies: Phase 1

- [ ] **Task 2.4**: Create TenantInvitation Commands + Handlers (enhanced security)
  - Files: NEW in `Features/TenantInvitations/`
    - CreateTenantInvitationCommand + Handler
    - AcceptTenantInvitationCommand + Handler
    - GetTenantInvitationByTokenQuery + Handler
  - **Role ceiling**: inviter's role >= invitee's role (TenantAdmin can't invite TenantOwner)
  - **Domain whitelisting**: validate email domain matches `AllowedDomain` on accept
  - **One-time hardening**: mark accepted + invalidate token in same transaction
  - CreateInvitation: TenantAdmin/Owner/InstanceAdmin
  - AcceptInvitation: any authenticated user (email match)
  - **Analytics**: `IAnalyticsProvider.TrackAsync("tenant.invitation_sent/accepted", ...)`
  - Provider compatibility expectation: runtime works with `AnalyticsProviderEnum` (`None`, `Posthog`, `Plausible`, `Rybbit`, `RudderStack`)
  - Dependencies: Task 1.3

- [ ] **Task 2.5**: Create Tenant Lifecycle Commands (with audit log + analytics)
  - Files: NEW in `Features/Tenants/Handlers/Commands/`
    - SuspendTenantCommandHandler
    - ReactivateTenantCommandHandler
    - ArchiveTenantCommandHandler
  - State machine: validates current state before transition
  - Each handler creates `TenantLifecycleLog` entry (OldStatusId -> NewStatusId, Reason)
  - Reason required for Suspend/Archive
  - **Analytics**: `IAnalyticsProvider.TrackAsync("tenant.status_changed", { old, new, reason })`
  - InstanceAdmin only
  - Dependencies: Task 1.1, 1.2, 1.5

- [ ] **Task 2.6**: Update OnboardingStatus DTO with progress fields
  - File: `Explore.Application/DTOs/Onboarding/TenantOnboardingStatusDto.cs`
  - Add: CurrentStep, TotalSteps, CompletedSteps (string[]), ProgressPercentage
  - Update GetTenantOnboardingStatusQueryHandler
  - Dependencies: Task 1.4

- [ ] **Task 2.7**: Create SaveOnboardingStepCommand + Handler (with analytics)
  - File: NEW in `Features/TenantOnboarding/Handlers/Commands/`
  - Saves intermediate wizard progress (step by step)
  - Does NOT mark IsCompleted (that's the final Complete action)
  - **Analytics**: `IAnalyticsProvider.TrackAsync("onboarding.step_completed", { step_name, step_index })`
  - Provider key usage in settings: `GovernanceSettingKeys.AnalyticsProvider` (`analytics.provider` string)
  - Dependencies: Task 1.4

---

## Phase 3: Infrastructure Layer 🟡 MOSTLY DONE (7/9)
**Priority: MEDIUM | Effort: M (5-7 hours)**

- [x] **Task 3.1**: Create TenantStatus lookup table EF config + seed
  - File: `Explore.Persistence/Configurations/Entities/TenantStatusConfiguration.cs` (NEW)
  - `ValueGeneratedNever()`, MaxLength constraints (follows ApprovalStatusConfiguration)
  - Seed in `LookupTableSeeder.cs`: 5 statuses with IsActiveState (Provisioning=false, Active=true, others=false)
  - Dependencies: Task 1.1

- [x] **Task 3.2**: Create TenantLifecycleLog EF Core Configuration
  - File: `Explore.Persistence/Configurations/Entities/TenantLifecycleLogConfiguration.cs` (NEW)
  - Two FKs to TenantStatus (OldStatusId, NewStatusId), both `OnDelete(Restrict)`
  - Index on TenantId + TransitionedAt
  - Dependencies: Task 1.5

- [x] **Task 3.3**: Create TenantInvitation EF Core Configuration
  - File: `Explore.Persistence/Configurations/Entities/TenantInvitationConfiguration.cs` (NEW)
  - Token unique index, TenantId+Email composite index, AllowedDomain MaxLength(255)
  - Tenant query filter registered in ExploreDbContext
  - Dependencies: Task 1.3

- [x] **Task 3.4**: Create ITenantInvitationRepository + Implementation
  - Files: `Explore.Application/Contracts/Persistence/ITenantInvitationRepository.cs`, `Explore.Persistence/Repositories/TenantInvitationRepository.cs`
  - Methods: GetByTokenAsync, GetPendingByEmailAsync, ExistsActiveAsync
  - Dependencies: Task 1.3, 3.3

- [x] **Task 3.5**: Create ITenantLifecycleLogRepository + Implementation
  - Files: `Explore.Application/Contracts/Persistence/ITenantLifecycleLogRepository.cs`, `Explore.Persistence/Repositories/TenantLifecycleLogRepository.cs`
  - Methods: GetByTenantIdAsync, CreateAsync
  - Dependencies: Task 1.5, 3.2

- [x] **Task 3.6**: Update Tenant EF Core Configuration (StatusId FK, Description, audit)
  - File: `Explore.Persistence/Configurations/Entities/TenantConfiguration.cs`
  - Map TenantStatusId (FK -> TenantStatuses), Description (max 500), audit fields
  - Relationship: `HasOne().WithMany().HasForeignKey().OnDelete(Restrict)`
  - Dependencies: Task 1.2

- [ ] **Task 3.7**: Update TenantOnboardingState Configuration (step columns)
  - File: `Explore.Persistence/Configurations/Entities/TenantOnboardingStateConfiguration.cs`
  - Map CurrentStep, TotalSteps, CompletedStepsJson (JSONB)
  - Dependencies: Task 1.4

- [ ] **Task 3.8**: Create EF Core Migration
  - Command: `dotnet ef migrations add AddTenantLifecycleAndInvitations`
  - Existing tenants default to TenantStatusId=2 (Active)
  - Dependencies: Tasks 3.1-3.7

- [x] **Task 3.9**: Register new services in DI (partial — repos done, ITenantProvisioningService pending Task 2.3)
  - Files: PersistenceServicesRegistration.cs (lines 105-106 ✅), ApplicationServicesRegistration.cs (pending ITenantProvisioningService)
  - Register: ~~ITenantInvitationRepository~~✅, ~~ITenantLifecycleLogRepository~~✅, ITenantProvisioningService (pending)

---

## Phase 4: API Layer ⏳ NOT STARTED
**Priority: MEDIUM | Effort: M (4-6 hours)**

- [ ] **Task 4.1**: Create TenantInvitationController
  - File: `Explore.API/Controllers/TenantInvitationController.cs` (NEW)
  - Endpoints: POST create, GET by token, POST accept, GET list, DELETE revoke
  - Dependencies: Task 2.4

- [ ] **Task 4.2**: Add self-service registration endpoint
  - File: `Explore.API/Controllers/TenantController.cs`
  - Endpoint: POST /api/tenants/register
  - Dependencies: Task 2.2

- [ ] **Task 4.3**: Add tenant lifecycle endpoints + lifecycle history
  - File: `Explore.API/Controllers/TenantController.cs`
  - Endpoints: POST suspend, POST reactivate, POST archive, **GET lifecycle** (history)
  - Dependencies: Task 2.5

- [ ] **Task 4.4**: Add onboarding step save endpoint
  - File: `Explore.API/Controllers/TenantOnboardingController.cs`
  - Endpoint: PUT /api/tenant-onboarding/steps/{stepId}
  - Dependencies: Task 2.7

- [ ] **Task 4.5**: Update Cerbos policies
  - Files: tenant_invitation.yaml (NEW), tenant.yaml (update)
  - Invitation CRUD + lifecycle action policies

---

## Phase 5: Blazor UI ⏳ NOT STARTED
**Priority: MEDIUM | Effort: L (12-16 hours)**

- [ ] **Task 5.1**: Create TenantOnboardingWizard component
  - File: `Explore.Blazor.Client/Components/Onboarding/TenantOnboardingWizard.razor` (NEW)
  - MudStepper with 4 steps: Identity -> Policies -> Branding/Domain -> Review
  - Auto-save after each step, resume-capable
  - Lock indicators for locked settings
  - Dependencies: Phase 4

- [ ] **Task 5.2**: Update TenantOnboarding.razor page
  - File: `Explore.Blazor.Client/Pages/Onboarding/TenantOnboarding.razor`
  - Replace flat form with TenantOnboardingWizard component
  - Dependencies: Task 5.1

- [ ] **Task 5.3**: Create TenantInvitationManagement page
  - File: `Explore.Blazor.Client/Pages/Admin/Tenant/TenantInvitations.razor` (NEW)
  - Route: /admin/tenant/invitations
  - List, invite (with role ceiling in UI), revoke, copy link
  - Dependencies: Task 5.6

- [ ] **Task 5.4**: Create self-service tenant registration page
  - File: `Explore.Blazor.Client/Pages/Tenants/CreateTenant.razor` (NEW)
  - Route: /tenants/create
  - Only visible when self-service enabled + multi-tenant mode
  - Dependencies: Task 4.2

- [ ] **Task 5.5**: Create invitation accept page
  - File: `Explore.Blazor.Client/Pages/Tenants/AcceptInvitation.razor` (NEW)
  - Route: /tenants/invite/{token}
  - Shows details, handles auth redirect, accept flow
  - Domain mismatch error message
  - Dependencies: Task 4.1

- [ ] **Task 5.6**: Add client services
  - Files: TenantInvitationService.cs (NEW), update TenantOnboardingService.cs
  - HTTP calls via typed HttpClient (YARP proxy only)
  - bUnit tests

---

## Phase 6: Testing & Documentation ⏳ NOT STARTED
**Priority: MEDIUM | Effort: M (7-9 hours)**

- [ ] **Task 6.1**: Unit tests for all new handlers
  - Project: Event.Application.UnitTests
  - All CRUD + lifecycle + auth denial paths
  - Transaction rollback, domain whitelist validation, role ceiling, lock enforcement, analytics calls
  - TDD: tests written BEFORE implementation

- [ ] **Task 6.2**: Integration tests for API endpoints
  - Project: Event.API.IntegrationTests
  - Tenant creation, invitation flow, lifecycle transitions, onboarding step save

- [ ] **Task 6.3**: Blazor component tests
  - Project: Explore.Blazor.Client.Tests
  - Wizard navigation, invitation management, services

- [ ] **Task 6.4**: Update documentation
  - Files: MULTI_TENANCY.md, ADMIN_HIERARCHY.md, API.md
  - Add invitation flow, lifecycle states, new endpoints, TenantStatus lookup table

---

## Summary

| Phase | Tasks | Done | Status | Effort |
|-------|-------|------|--------|--------|
| Phase 0: Auth Fix + Lock | 2 | 2/2 | ✅ Complete | S (2-3h) |
| Phase 1: Domain (+lookup +lifecycle log) | 5 | 4/5 | ✅ Complete (Task 1.4 skipped per user) | M (5-7h) |
| Phase 2: Application (+transactions +security +analytics) | 7 | 0/7 | ⏳ Not Started — **NEXT** | L (12-16h) |
| Phase 3: Infrastructure (+seed +lifecycle config) | 9 | 7/9 | 🟡 Mostly Done (3.7 blocked on 1.4, 3.8 deferred) | M (5-7h) |
| Phase 4: API (+lifecycle history) | 5 | 0/5 | ⏳ Not Started | M (4-6h) |
| Phase 5: Blazor UI | 6 | 0/6 | ⏳ Not Started | L (12-16h) |
| Phase 6: Testing & Docs | 4 | 0/4 | ⏳ Not Started | M (7-9h) |
| **Total** | **38** | **13/38** | | **XL (47-64h)** |

## Context Reset Session Update (2026-02-23 18:12 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority focused on admin consolidation handoff in navbar customization track.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in this task file.
