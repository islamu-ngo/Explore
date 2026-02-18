# Enterprise Tenant Onboarding - Context

**Last Updated: 2026-02-18**

---

## SESSION PROGRESS (2026-02-18)

### Completed
- Deep codebase exploration (4 background agents + direct reads)
- Enterprise SaaS pattern research (Stripe, Vercel, Clerk, WorkOS, Supabase)
- ASP.NET Core multi-tenancy best practices research (Tavily)
- Gap analysis: identified 8 critical/high gaps in current implementation
- Created comprehensive implementation plan (tenant-onboarding-enterprise-plan.md)
- Created task checklist (tenant-onboarding-enterprise-tasks.md)
- Created this context file
- **Altice Informatics feedback integrated**: 7 enterprise enhancements added
- **Plan rewritten** with lookup table pattern, lifecycle audit log, transactional provisioning, domain whitelisting, analytics integration, backend lock enforcement
- **Context file updated** with new decisions D6-D11, updated key files, analytics integration notes
- **Tasks file updated** with 38-task checklist matching rewritten plan
- Implemented platform-admin-controlled tenant white-label gating (multi-tenant only)
- Added governance setting key + seed for tenant white-label enablement
- Wired instance onboarding UI/settings DTO/service for new white-label toggle
- Wired tenant onboarding service/DTO/UI to respect white-label enablement and hide branding edits when disabled
- Implemented backend lock enforcement in `UpdateTenantPolicySettingsCommandHandler` with `ValidationException` when locked values are modified
- Added unit test coverage for locked branding mutation denial
- Implemented Phase 1 Task 1.1: created `TenantStatus` lookup entity and `TenantStatusEnum`
- Implemented Phase 1 Task 1.2: updated `Tenant` entity with `TenantStatusId` FK, `TenantStatus` nav, `Description`, computed `IsActive`, `IAuditableEntity`; updated TenantConfiguration, TenantContext, onboarding handlers, seed data, and all impacted tests
- Implemented Phase 1 Task 1.3: created `TenantInvitation` entity (Token, Email, RoleId, AllowedDomain, ExpiresAt, InvitedByUserId, AcceptedAt/ByUserId) + EF configuration + DbContext/query filter registration
- Implemented Phase 1 Task 1.5: created `TenantLifecycleLog` entity (OldStatusId nullable, NewStatusId, TransitionedByUserId, Reason, TransitionedAt) + EF configuration + DbContext registration
- **Task 1.4 unskipped** (onboarding step persistence) per user instruction
- **Phase 3 Tasks 3.4, 3.5, 3.9 discovered already complete**: ITenantInvitationRepository + impl, ITenantLifecycleLogRepository + impl, DI registrations in PersistenceServicesRegistration.cs (lines 105-106)
- **Updated dev docs** to reflect true state: tasks.md summary table, phase headers, context.md SESSION PROGRESS

### In Progress
- **Phase 2: Application Layer** — Starting with Task 2.3 (ITenantProvisioningService), then Task 2.1 (CreateTenantCommandHandler)
- **Scope expansion**: instance onboarding post-completion routing based on deployment mode and default home page preference

### Blockers
- None — build green (0 errors), ready to implement Phase 2

---

## Key Decisions

### D1: Tenant.IsActive -> Computed Property (Non-Breaking)
**Decision**: Keep `IsActive` as a computed property from `TenantStatus.IsActiveState` to avoid breaking existing code.
**Rationale**: `IsActive` is referenced in query filters, Cerbos attributes, and Blazor conditionals. Making it a computed navigation keeps backward compatibility.

### D2: Email Integration Deferred
**Decision**: Invitation system generates tokens + copy-link only. Email delivery is a separate feature.
**Rationale**: Email service (SMTP abstraction) is a separate work item in `dev/active/email-smtp-abstraction/`. This plan focuses on the invitation data model and API.

### D3: 4-Step Wizard (Not More)
**Decision**: Onboarding wizard has exactly 4 steps: Identity, Policies, Branding & Domain, Review & Complete.
**Rationale**: Enterprise SaaS research shows minimal required steps + checklist for optional steps. More than 4 steps causes abandonment.

### D4: TenantProvisioningService (Shared Logic)
**Decision**: Extract tenant provisioning into a shared service used by both admin-created and self-service flows.
**Rationale**: DRY principle. Both flows create the same resources (tenant, settings, onboarding state, capabilities, first member).

### D5: Phase 0 First (Critical Auth Fix)
**Decision**: Fix authorization bug before any other work.
**Rationale**: Any tenant member can currently complete onboarding. This is a security vulnerability that must be fixed immediately.

### D6: TenantStatus as Lookup Table (Not Enum)
**Decision**: TenantStatus is a database lookup table entity with Id(int), MasterCode, FullName, Description, IsActiveState(bool).
**Rationale**: Matches existing `ApprovalStatus`/`Madhab`/`AnalyticsProvider` pattern. Provides referential integrity, metadata support (DisplayName, Description), and can be extended without code changes. Cybertec PostgreSQL analysis confirms lookup tables > enums for lifecycle management.
**Pattern reference**: `Explore.Domain/ApprovalStatus.cs`, `Explore.Persistence/Configurations/Entities/ApprovalStatusConfiguration.cs`

### D7: TenantLifecycleLog for Status Transition Audit
**Decision**: New `TenantLifecycleLog` entity tracks every status change: OldStatusId -> NewStatusId, TransitionedByUserId, Reason, TransitionedAt.
**Rationale**: IAuditableEntity only tracks last update (CreatedBy/UpdatedBy). Lifecycle transitions need full history with actor, reason, and before/after state. Follows WorkOS/Infisical audit log patterns.
**Pattern reference**: `Explore.Domain/ConfigurationChangeLog.cs` (OldValue/NewValue, Scope, ActionType pattern)

### D8: Transactional Provisioning (Explicit Transaction)
**Decision**: `ITenantProvisioningService` wraps all provisioning steps in explicit `BeginTransactionAsync()`/`CommitAsync()`/`RollbackAsync()`.
**Rationale**: Current `CompleteInstanceOnboardingCommandHandler` creates 5 records without explicit transaction -- a failure mid-way leaves orphaned records. Stripe-style idempotency check (slug exists?) before creating.
**Pattern reference**: `Explore.Persistence/Repositories/AppSettingRepository.cs` (BulkUpdateAsync)

### D9: Advanced Invitation Security
**Decision**: TenantInvitation gets `AllowedDomain` field for domain whitelisting + role ceiling enforcement (inviter role >= invitee role) + one-time token hardening (accept + invalidate in same transaction).
**Rationale**: Clerk verified domains pattern. Prevents privilege escalation (TenantAdmin inviting TenantOwner). Token must be invalidated atomically with accept to prevent race conditions.

### D10: Onboarding Funnel Analytics via Existing IAnalyticsProvider
**Decision**: Track wizard step completions and lifecycle transitions using existing `IAnalyticsProvider.TrackAsync()`. No new analytics infrastructure needed.
**Rationale**: `RuntimeAnalyticsProvider` supports runtime switching across None/Posthog/Plausible/Rybbit/RudderStack with fire-and-forget behavior. Events: `onboarding.step_completed` (step_name, step_index), `tenant.status_changed` (old, new, reason).
**Pattern reference**: `Explore.Application/Contracts/Infrastructure/IAnalyticsProvider.cs`, `Explore.Infrastructure/Analytics/RuntimeAnalyticsProvider.cs`

### D11: Backend Lock Enforcement (Not UI-Only)
**Decision**: `UpdateTenantPolicySettingsCommandHandler` must check `SystemSetting.IsLocked` and throw `ValidationException` if a locked setting is being modified.
**Rationale**: UI currently hides locked fields but the handler doesn't validate. Backend enforcement is mandatory -- UI hiding is insufficient for security.

### D12: Tenant White-Labeling Requires Platform Toggle + Multi-Tenant Mode
**Decision**: Tenant branding overrides are only enabled when both conditions are true: (1) instance deployment mode is `MultiTenant`, and (2) platform admin enabled `tenants.white_labeling_enabled`.
**Rationale**: White-labeling is a tenant capability, not a guaranteed default. Enforcing dual gating avoids accidental tenant-brand drift in single-tenant deployments and keeps platform-admin governance explicit.

### D13: Instance Onboarding Redirect Behavior (Single vs Multi-Tenant)
**Decision**: After completing instance onboarding, do not redirect to tenant onboarding. Instead:
- **SingleTenant**: route to the instance-level preferred home page (LandingPage or EventList), then allow tenant admins to override in tenant onboarding when available.
- **MultiTenant**: route to instance admin area (platform management pages), not tenant onboarding.
**Rationale**: Aligns with enterprise onboarding expectations; single-tenant acts as unified admin, while multi-tenant continues in platform admin area.

---

## Key Files (Existing)

### Domain Layer
| File | Purpose | Status |
|------|---------|--------|
| `Explore.Domain/Tenant.cs` | Tenant entity (Id, FullName, Slug, IsActive, NavigationLinks) | Needs TenantStatusId FK, Description, IAuditableEntity |
| `Explore.Domain/TenantMember.cs` | User-tenant membership with roles | Good |
| `Explore.Domain/TenantOnboardingState.cs` | Binary completion state | Needs step tracking |
| `Explore.Domain/Enums/RoleEnum.cs` | TenantOwner=10, TenantAdmin=11, TenantModerator=12, TenantMember=13 | Good |
| `Explore.Domain/Constants/GovernanceSettingKeys.cs` | All governance setting keys | Has TenantSelfServiceRegistration |
| `Explore.Domain/TenantSettings.cs` | Empty settings entity | Created during provisioning |
| `Explore.Domain/Modules/TenantCapability.cs` | Module-tenant linking | Good |
| `Explore.Domain/ApprovalStatus.cs` | **Reference**: Lookup table pattern (Id, MasterCode, FullName, Description) | Good |
| `Explore.Domain/ConfigurationChangeLog.cs` | **Reference**: Audit log pattern (OldValue/NewValue, Scope, ActionType) | Good |
| `Explore.Domain/Interfaces/IAuditableEntity.cs` | **Reference**: 21 entities implement this | Good |

### Key Files: NEW Domain Entities (CREATED)
| File | Purpose | Phase | Status |
|------|---------|-------|--------|
| `Explore.Domain/TenantStatus.cs` | Lookup table: Id, MasterCode, FullName, Description, IsActiveState | Phase 1, Task 1.1 | Done |
| `Explore.Domain/Enums/TenantStatusEnum.cs` | Enum: Provisioning=1, Active=2, Suspended=3, Archived=4, Purged=5 | Phase 1, Task 1.1 | Done |
| `Explore.Domain/TenantInvitation.cs` | Invitation: Token, Email, RoleId, AllowedDomain, ExpiresAt, InvitedByUserId | Phase 1, Task 1.3 | Done |
| `Explore.Domain/TenantLifecycleLog.cs` | Audit: OldStatusId?, NewStatusId, TransitionedByUserId, Reason, TransitionedAt | Phase 1, Task 1.5 | Done |

### Key Files: NEW Infrastructure (CREATED)
| File | Purpose | Phase | Status |
|------|---------|-------|--------|
| `Explore.Persistence/Configurations/Entities/TenantStatusConfiguration.cs` | Lookup table EF config (ValueGeneratedNever) | Phase 3, Task 3.1 | Done |
| `Explore.Persistence/Configurations/Entities/TenantInvitationConfiguration.cs` | Token unique index, TenantId+Email composite, AllowedDomain max 255 | Phase 3, Task 3.3 | Done |
| `Explore.Persistence/Configurations/Entities/TenantLifecycleLogConfiguration.cs` | Two FKs to TenantStatus (Restrict), indexed TenantId+TransitionedAt | Phase 3, Task 3.2 | Done |
| `Explore.Persistence/Seed/LookupTableSeeder.cs` (updated) | Added SeedTenantStatusesAsync with 5 statuses | Phase 3, Task 3.1 | Done |
| `Explore.Persistence/ExploreDbContext.cs` (updated) | Added DbSet + query filter for TenantInvitation, DbSet for TenantLifecycleLog | Phase 3, Task 3.3 | Done |

### Application Layer
| File | Purpose | Status |
|------|---------|--------|
| `CompleteTenantOnboardingCommandHandler.cs` | Completes onboarding | AUTH BUG: IsTenantMember instead of IsTenantAdmin |
| `UpdateTenantPolicySettingsCommandHandler.cs` | Updates policies post-onboarding | AUTH BUG + needs lock enforcement |
| `GetTenantOnboardingStatusQueryHandler.cs` | Returns status DTO | Needs progress fields |
| `GetTenantPolicySettingsQueryHandler.cs` | Returns settings DTO | Good |
| `TenantOnboardingStatusDto.cs` | Status DTO (5 fields) | Needs progress fields |
| `TenantPolicySettingsDto.cs` | Settings DTO (17 fields) | Good |
| `CreateTenantDto.cs` | Create DTO (FullName, Slug, IsActive) | Good |
| `CreateTenantCommand.cs` | Command (exists, no handler!) | MISSING handler |
| `TenantPolicySettingService.cs` | Cascading settings resolution | Good |
| `InstanceGovernanceSettingService.cs` | Instance governance settings | Good |
| `ITenantOnboardingStateRepository.cs` | Repository contract | Good |
| `IAnalyticsProvider.cs` | Analytics abstraction (TrackAsync, IdentifyAsync, PageViewAsync) | Good -- use for funnel analytics |

### Infrastructure Layer
| File | Purpose | Status |
|------|---------|--------|
| `DeploymentSettings.cs` | SingleTenant/MultiTenant mode | Good |
| `AdminContext.cs` | Resolves admin authority (5-min cache) | Good |
| `AdminClaimsTransformation.cs` | Adds explore:admin:tenant claims | Good |
| `TenantOnboardingStateConfiguration.cs` | EF Core config | Needs new columns |
| `TenantOnboardingStateRepository.cs` | Repository impl | Good |
| `LookupTableSeeder.cs` | **Reference**: Idempotent runtime seeding | Add TenantStatus seed here |
| `ApprovalStatusConfiguration.cs` | **Reference**: Lookup table EF config (ValueGeneratedNever) | Good |
| `AppSettingRepository.cs` | **Reference**: Transaction pattern (BulkUpdateAsync) | Good |

### API Layer
| File | Purpose | Status |
|------|---------|--------|
| `TenantOnboardingController.cs` | 4 endpoints (status, settings, complete, update) | Needs step save endpoint |
| `TenantController.cs` | Tenant CRUD + nav links | Needs lifecycle + self-service endpoints |
| `TenantContext.cs` | Tenant resolution middleware | Good |
| `BlockInSingleTenantAttribute.cs` | Blocks endpoints in single-tenant | Good |

### Blazor UI
| File | Purpose | Status |
|------|---------|--------|
| `TenantOnboarding.razor` | Flat onboarding form | Replace with wizard |
| `TenantOnboardingService.cs` | Client service | Needs step save method |
| `TenantAdminSettingsLayout.razor` | Sidebar layout | Good |
| `TenantPoliciesSection.razor` | Policy toggles | Good |
| `TenantDomainSection.razor` | Domain config | Good |
| `TenantBrandingSection.razor` | Branding config | Good |

### Authorization (Cerbos)
| File | Purpose | Status |
|------|---------|--------|
| `cerbos/policies/tenant.yaml` | Tenant resource policy | Needs lifecycle actions |
| `cerbos/policies/tenant_user.yaml` | Tenant user policy | Good |
| `cerbos/policies/tenant_setting.yaml` | Tenant settings policy | Good |
| `cerbos/policies/derived_roles.yaml` | 3-level hierarchy | Good |

### Tests
| File | Purpose | Status |
|------|---------|--------|
| `TenantOnboardingServiceTests.cs` | Service tests | Needs update |
| `TenantControllerTests.cs` | API tests | Needs update |
| `AdminClaimsTransformationTests.cs` | Auth tests | Good |

---

## Analytics Integration Notes

The existing analytics abstraction is fully implemented and ready to use for onboarding funnel tracking:

- **Interface**: `IAnalyticsProvider` with `TrackAsync(eventName, properties)`, `IdentifyAsync`, `PageViewAsync`
- **Runtime switching**: `RuntimeAnalyticsProvider` resolves active provider at runtime (None/Posthog/Plausible/Rybbit/RudderStack)
- **Pattern**: Fire-and-forget with try/catch -- never blocks business logic
- **Events to track**:
  - `onboarding.step_completed` -- properties: `{ step_name, step_index, tenant_id }`
  - `onboarding.completed` -- properties: `{ tenant_id, total_time_seconds }`
  - `tenant.status_changed` -- properties: `{ old_status, new_status, reason, tenant_id }`
  - `tenant.invitation_sent` -- properties: `{ tenant_id, role }`
  - `tenant.invitation_accepted` -- properties: `{ tenant_id, role }`

No new analytics infrastructure needed -- just inject `IAnalyticsProvider` in handlers and call `TrackAsync`.

### Session Note (2026-02-17)
- Analytics naming/conventions were updated globally: enum is now `AnalyticsProviderEnum`, provider selection key is `analytics.provider` (string), and lookup values include `none`, `posthog`, `plausible`, `rybbit`, `rudderstack`.

---

## Technical Constraints

1. **Blazor BFF Separation**: Blazor.Client can ONLY communicate with API via YARP proxy. No direct access to Application/Infrastructure layers.
2. **Named Query Filters (EF Core 10)**: Use `.HasQueryFilter(name: "SoftDelete", ...)` pattern for new entities.
3. **ABOUTME Comments**: Every new file must start with 2-line ABOUTME comment.
4. **File-Scoped Namespaces**: All new files use `namespace Explore.Domain;` (not block style).
5. **Repositories Return Entities**: Never DTOs. Map in handlers.
6. **Commands Return BaseCommandResponse<Guid>**: Not just Guid.
7. **TDD Required**: Write failing test first, then implementation.
8. **No Default Values in Domain Entities**: Set in handlers.
9. **Validators Use Manual Instantiation**: `var validator = new Validator(repo1, repo2);` (not DI).
10. **Lookup Table Pattern**: ValueGeneratedNever(), MaxLength(500), idempotent seeding via LookupTableSeeder.
11. **FK Pattern**: `[ForeignKey("TenantStatus")] public int TenantStatusId` + `public required TenantStatus TenantStatus { get; set; }`
12. **Explicit Transactions**: BeginTransactionAsync/CommitAsync/RollbackAsync for multi-step operations.

---

## Quick Resume

To continue this work:
1. Read this file for current state
2. Read `tenant-onboarding-enterprise-plan.md` for full plan
3. Check `tenant-onboarding-enterprise-tasks.md` for progress checklist
4. **Phase 2 Application Layer is NEXT**: Start with Task 2.3 (ITenantProvisioningService)
5. Follow Clean Architecture layer order: Domain -> Application -> Infrastructure -> API -> Blazor
6. Build is green. Tasks 3.4, 3.5, 3.9 (repos + DI) already done.
7. Task 1.4 was deliberately skipped per user instruction — do NOT implement unless asked
