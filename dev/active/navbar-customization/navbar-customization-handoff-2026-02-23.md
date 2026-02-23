# Handoff: Navbar Customization / Admin Consolidation

Last Updated: 2026-02-23 18:12 Europe/Brussels

## Goal of Current Changes

Implement user-requested admin UX consolidation:
- Add tenant admin panel sections for organization approvals and lookup tables.
- Add instance admin SMTP configuration panel section with test connection.
- Update navbar admin dropdown labels/links by role.
- Remove legacy `/admin` dashboard and standalone lookup pages once migrated.

## Exact Files + Lines to Start Editing

- `Explore.Blazor.Client/Components/Admin/Tenant/TenantAdminSettingsLayout.razor:45`
  - Insert new sidebar items after Policies/Domain/Branding (`Organizations`, `Lookup Tables`).
- `Explore.Blazor.Client/Components/Admin/Tenant/TenantAdminSettingsLayout.razor:64`
  - Add render branches for new tenant section components.
- `Explore.Blazor.Client/Components/Admin/Instance/InstanceAdminSettingsLayout.razor:61`
  - Insert new sidebar item (`SMTP`).
- `Explore.Blazor.Client/Components/Admin/Instance/InstanceAdminSettingsLayout.razor:88`
  - Add render branch for SMTP section component.
- `Explore.Blazor.Client/Layout/NavMenu.razor:145`
  - Remove `/admin` link and rename admin entries to administration wording.
- `Explore.Blazor.Client/Pages/Admin/AdminList.razor:1`
  - Legacy dashboard route/page targeted for removal after migration.

## New Components to Create First

- `Explore.Blazor.Client/Components/Admin/Tenant/TenantOrganizationsSection.razor`
- `Explore.Blazor.Client/Components/Admin/Tenant/TenantLookupTablesSection.razor`
- `Explore.Blazor.Client/Components/Admin/Instance/InstanceSmtpSection.razor`

## Dependencies / Integration Points

- SMTP keys already defined in `Explore.Domain/Constants/GovernanceSettingKeys.cs`.
- API pattern to follow for test action: `Explore.API/Controllers/InstanceOnboardingController.cs:209` (`test-storage`).
- Client service extension point: `Explore.Blazor.Client/Services/InstanceOnboardingService.cs:9` (`IInstanceOnboardingService`).
- Admin dropdown role checks already exist in `Explore.Blazor.Client/Layout/NavMenu.razor.cs`.

## Current Implementation State

- Completed: exhaustive analysis, implementation plan, dev docs + journal updates.
- Not started: product code edits for admin consolidation (all implementation tasks remain open).
- Blockers: none technical; only implementation work pending.

## Uncommitted Changes Needing Attention

- Repository already has many unrelated modified files before this handoff update (domain/persistence/test files).
- Do not revert unrelated changes.
- New/updated docs in this session are under:
  - `dev/active/*-context.md`
  - `dev/active/*-tasks.md`
  - `dev/active/navbar-customization/*`
  - `dev/_journal/journal.md`
  - `dev/_journal/MAJOR_DECISIONS.md`

## Commands to Run on Restart

```bash
git status --short
```

```bash
dotnet build
```

```bash
dotnet test Explore.Blazor.Client.Tests
```

```bash
dotnet test Event.Application.UnitTests
```

## Verification Commands After Implementation

```bash
dotnet build
```

```bash
dotnet test Explore.Blazor.Client.Tests --filter "NavMenu|Admin|Onboarding"
```

```bash
dotnet test Event.Application.UnitTests
```
