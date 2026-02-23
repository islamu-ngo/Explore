# Enterprise Cleanup - Context

## SESSION PROGRESS (2026-02-09)

### ✅ COMPLETED — PHASE 1 FULLY DONE
- Full codebase analysis across all layers
- Created docs/CODEBASE_STRUCTURE.md, docs/NAMING_CONVENTIONS.md, docs/CODEBASE_INSIGHTS.md
- Identified all inconsistencies and categorized by severity
- Created comprehensive implementation plan (enterprise-cleanup-plan.md)
- Created task checklist (enterprise-cleanup-tasks.md)
- **Phase 1.1**: Converted ALL files to file-scoped namespaces via `dotnet format --diagnostics IDE0161` with `.editorconfig`
- **Phase 1.2**: Removed all `= string.Empty` from domain entities, replaced with `required` keyword (10 occurrences across 5 files)
- **Phase 1.3**: Standardized navigation properties — removed `= null!`, made nullable for optional FKs, `required` for non-nullable FKs (hook-enforced pattern)
- **Phase 1.4**: Fixed Organization entity — added `required` to FullName, made Email/Country/City/Address/Postcode nullable, fixed nav properties
- **Phase 1.5**: Added IAuditableEntity interface + audit fields to EventRegistration, EventCategories, EventTags, StorageObject, UserAuthenticationToken, UserExternalLogin, OrganizationReview, SystemSetting, TenantSetting, TenantCapability. Removed `= true` default from ModuleDefinition.IsActive and TenantCapability.IsEnabled

### 🟡 NEXT UP
- **Phase 2: CQRS Pattern Standardization** — start with 2.1 (restructure OrganizationReviews)
- Nothing is partially done — clean handoff point

### ⚠️ CRITICAL BLOCKERS / KNOWN ISSUES

1. **`Directory.Build.props` hook**: A PostToolUse hook (`ContextTracker.cs`) runs after every Edit/Write. It:
   - Enforces `required` on non-nullable string properties (correct behavior)
   - Enforces `required` on navigation properties with non-nullable FKs (acceptable EF Core pattern)
   - Reverts any changes to `Directory.Build.props` back to the version with `TreatWarningsAsErrors=true`
   - **Do NOT fight the hook** — work with its patterns

2. **NuGet version conflicts in Directory.Packages.props** (pre-existing, NOT from our changes):
   - `FluentValidation` 11.9.1 conflicts with `FluentValidation.DependencyInjectionExtensions` 12.0.0 (needs >= 12.0.0)
   - `Microsoft.Extensions.Caching.Memory` 10.0.0-rc.1 conflicts with EF Core 10.0.2 (needs >= 10.0.2)
   - `Microsoft.Extensions.Configuration.Abstractions` 10.0.0-rc.1 conflicts with newer packages
   - **Impact**: Solution-level `dotnet build` and full `dotnet test` fail. Individual `Explore.Domain` project builds fine with 0 errors.
   - **Resolution needed**: Update rc versions to stable in `Directory.Packages.props`

3. **Lock files**: All `packages.lock.json` files were deleted (they were auto-generated artifacts from premature `RestorePackagesWithLockFile` setting). Need `dotnet restore --force-evaluate` after fixing NuGet versions.

4. **EF Migration needed**: Phase 1.5 added new audit columns (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy) to: EventRegistration, EventCategories, EventTags, StorageObject, UserAuthenticationToken, UserExternalLogin, TenantCapability. Also changed OrganizationReview's DateTimeOffset→DateTime. Also added CreatedBy/UpdatedBy to SystemSetting. A migration must be created before these entities can be used at runtime.

### 🔧 BUILD STATUS
- `dotnet build Explore.Domain.csproj` → **0 errors, ~100 warnings** (pre-existing CA/CS warnings)
- `dotnet build` (solution) → **Fails** due to NuGet version conflicts (pre-existing)
- Tests last passed in previous session (before `Directory.Build.props` was introduced)

---

## CRITICAL SESSION HANDOFF NOTES

### How to verify Phase 1 is clean
```bash
# Check no block-scoped namespaces remain (except generated/migration files)
# Use Grep tool: pattern "^namespace [^;]+$" in Explore.Domain — should be 0 matches
# Use Grep tool: pattern "= string\.Empty;" in Explore.Domain — should be 0 matches
# Use Grep tool: pattern "= null!;" in Explore.Domain — should be 0 matches

# Build Domain project (always works)
dotnet build "C:/ISLAMU/GitHub/Explore/Explore.Domain/Explore.Domain.csproj" --configuration Release --verbosity quiet
```

### How to fix the NuGet blocker (MUST do before Phase 2)
The NuGet conflicts are in `Directory.Packages.props`. Fix these version pins:
- `FluentValidation` → change from `11.9.1` to `12.0.0`
- `Microsoft.Extensions.Caching.Memory` → change from `10.0.0-rc.1.25451.107` to `10.0.2`
- `Microsoft.Extensions.Configuration.Abstractions` → change from `10.0.0-rc.1.25451.107` to `10.0.2`
- Then run `dotnet restore --force-evaluate`
- Note: the `Directory.Build.props` hook will re-add `TreatWarningsAsErrors=true` — may need to add `<NoWarn>` for CA/CS codes until cleanup is complete

### .editorconfig
Created at solution root with:
```
root = true
[*.cs]
csharp_style_namespace_declarations = file_scoped:warning
```

### Files created/modified this session

**New files:**
- `.editorconfig` — enables `dotnet format` to enforce file-scoped namespaces
- `Directory.Build.props` — created by previous agent (hook-protected, can't modify)

**Domain entity files modified (Phase 1.2-1.5):**
- `PdsSyncOutbox.cs` — `required` on Did, Collection, RecordKey
- `ModuleDefinition.cs` — `required` on ModuleKey, Name; removed `= true` from IsActive
- `SystemSetting.cs` — `required` on Value; added IAuditableEntity + CreatedBy, UpdatedBy
- `TenantAdministratorRole.cs` — `required` on FullName, MasterCode
- `TenantSetting.cs` — `required` on SettingKey, Value; added IAuditableEntity interface
- `Actor.cs` — nav properties nullable/required
- `ActorKeyStore.cs` — nav properties nullable
- `InstanceAdministrator.cs` — User nav nullable
- `OrganizationReview.cs` — all nav nullable, ReviewerName `required`, DateTimeOffset→DateTime, added CreatedBy/UpdatedBy, IAuditableEntity
- `TenantAdministrator.cs` — all nav nullable
- `TenantOnboardingState.cs` — Tenant nav nullable (hook may have changed to required)
- `UserAuthenticationToken.cs` — nav nullable, added IAuditableEntity + audit fields
- `UserRole.cs` — Tenant nav nullable
- `Organization.cs` — FullName `required`, Email/Country/City/Address/Postcode nullable, nav nullable, Members nullable
- `EventRegistration.cs` — nav nullable, IAuditableEntity + audit fields
- `EventCategories.cs` — nav nullable, IAuditableEntity + audit fields
- `EventTags.cs` — nav nullable, IAuditableEntity + audit fields
- `StorageObject.cs` — nav nullable, string props `required`, IAuditableEntity + audit fields
- `UserExternalLogin.cs` — nav nullable, IAuditableEntity + audit fields
- `TenantCapability.cs` — IAuditableEntity + audit fields, removed `= true` from IsEnabled

**~300+ files via `dotnet format`** (file-scoped namespace conversion, no manual edits)

---

## Key Files & Locations

### CQRS Issues (Phase 2 targets)

**OrganizationReviews non-standard structure**:
- `Features/OrganizationReviews/Commands/CreateOrganizationReview/CreateOrganizationReviewCommand.cs`
- `Features/OrganizationReviews/Commands/CreateOrganizationReview/CreateOrganizationReviewCommandHandler.cs`
- `Features/OrganizationReviews/Queries/GetMyReviews/GetMyReviewsQuery.cs`
- `Features/OrganizationReviews/Queries/GetMyReviews/GetMyReviewsQueryHandler.cs`
- `Features/OrganizationReviews/Queries/GetOrganizationReviews/GetOrganizationReviewsQuery.cs`
- `Features/OrganizationReviews/Queries/GetOrganizationReviews/GetOrganizationReviewsQueryHandler.cs`

**Validators injected via DI (Rule #2 violation)**:
- `UpdateActorKeyStoreCommandHandler.cs` — `IValidator<UpdateActorKeyStoreDto>` in constructor
- `CreateTenantCommandHandler.cs` — `IValidator<CreateTenantDto>` in constructor
- `UpdateTenantCommandHandler.cs` — `IValidator<UpdateTenantDto>` in constructor
- `UpdateStorageObjectCommandHandler.cs` — `IValidator<UpdateStorageObjectDto>` in constructor

**Command handlers with NO validation at all**:
- `CreateOrganizationCommandHandler.cs` — maps DTO directly, no validator
- `UpdateOrganizationDetailsCommandHandler.cs` — no validator
- `UpdateUserCommandHandler.cs` — no validator
- `CreateOrganizationReviewCommandHandler.cs` — no validator

**Delete commands returning bool** (26 files):
- All in pattern `Features/*/Requests/Commands/Delete*Command.cs`

### API Controller Issues (Phase 3 targets)

**Dead stub code in ApprovalStatusController**:
- `GetById(int id)` returns hardcoded `"value"` string
- `Post()` has empty body
- `Put()` has empty body

**Console.WriteLine in UserController**: Lines 109, 117, 147, 152

**Incomplete userId extraction**:
- `UserController.cs` — 5 locations, missing `sid` fallback
- `OrganizationMemberController.cs` — 5 locations, missing both fallbacks

### Persistence Issues (Phase 4 targets)

**Missing DbSets**: ModuleDefinition, OwnerType, Role, TenantCapability
**Missing entity configurations**: ModuleDefinition, OwnerType, Role
**Missing query filter**: EventSessionSpeaker
**Typo**: `CongfigurePersistenceServices` in PersistenceServicesRegistration.cs + Program.cs

### Blazor Client Issues (Phase 5 targets)

**BaseCommandResponse duplication**: Application vs Blazor.Client
**Mixed service interface locations**: Some inline, some in `Services/Contracts/`
**ServiceResult<T> exists but unused**: `Models/Responses/ServiceResult.cs`

---

## Important Decisions Made

1. **Navigation properties pattern**: Non-nullable FK → `required` on nav property (hook-enforced). Nullable FK → `?` on nav property. This is the EF Core recommended approach.

2. **`= string.Empty` replacement**: Use `required` keyword (C# 11). Works with EF Core materialization. Hook enforces this pattern automatically.

3. **Organization nullable fields**: Only `FullName` is `required`. Email, Country, City, Address, Postcode are nullable since organizations may not have all info at creation time.

4. **OrganizationReview DateTimeOffset→DateTime**: Changed to align with IAuditableEntity interface which uses `DateTime`. Requires migration.

5. **Phase 1.5 scope**: Added audit fields to ALL listed entities even though it requires a migration. This is correct per the cleanup plan. Migration deferred to after Phase 4 (persistence cleanup).

---

## Quick Resume

To continue work:
1. Read this file for current state
2. **Fix NuGet version conflicts in `Directory.Packages.props`** (see handoff notes above)
3. Create EF migration for Phase 1.5 entity changes (new audit columns)
4. Start Phase 2.1: Restructure OrganizationReviews CQRS folders
5. Check `enterprise-cleanup-tasks.md` for detailed Phase 2 checklist
6. Build and test after each sub-task
## Context Reset Session Update (2026-02-15 21:25 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority shifted to analytics implementation completion and verification.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in `enterprise-cleanup-tasks.md`.

## Context Reset Session Update (2026-02-23 18:12 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority focused on admin consolidation handoff in navbar customization track.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in this task file.

## Context Reset Session Update (2026-02-23 18:47 Europe/Brussels)

- Current implementation state: No direct implementation changes in this track during this session.
- Key decisions made this session: Prioritized completion and verification of admin consolidation in the navbar customization track.
- Files modified and why: None for this specific track in this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from the highest-priority unchecked tasks in this track's tasks file.
