# Handoff: Navbar Customization / Admin Consolidation

Last Updated: 2026-02-23 18:47 Europe/Brussels

## Goal of Current Changes

Implement user-requested admin UX consolidation:
- Add tenant admin panel sections for organization approvals and lookup tables.
- Add instance admin SMTP configuration panel section with test connection.
- Update navbar admin dropdown labels/links by role.
- Remove legacy `/admin` dashboard and standalone lookup pages once migrated.

## Exact Files + Lines Last Edited

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

- Completed in this session:
  - Tenant panel now includes Organizations and Lookup Tables sections.
  - Instance panel now includes SMTP section with connection test button.
  - SMTP settings get/update/test flow implemented in Blazor client + API + Application service/CQRS.
  - Navbar admin dropdown updated: removed `/admin`, renamed to Instance/Tenant Administration.
  - Legacy standalone admin pages/routes removed from `Pages/Admin` and `Routes.razor`.
- Verification completed:
  - `dotnet build` passed.
  - `dotnet test --project "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --no-build` passed (522).
  - `dotnet test --project "Event.Application.UnitTests/Event.Application.UnitTests.csproj"` passed (278).
- Blockers: no blocking issues; remaining work is manual UI smoke testing and optional warning cleanup.

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
dotnet test --project "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --no-build
```

```bash
dotnet test --project "Event.Application.UnitTests/Event.Application.UnitTests.csproj"
```

## Verification Commands (Repeatable)

```bash
dotnet build
```

```bash
dotnet test --project "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --no-build
```

```bash
dotnet test --project "Event.Application.UnitTests/Event.Application.UnitTests.csproj"

## Remaining Work (Unfinished)

- Manual browser smoke pass:
  - Tenant admin: Organizations approvals and Lookup Tables CRUD/tabs.
  - Instance admin: SMTP save and test connection UX feedback.
  - Navbar dropdown visibility by role claims.
- Optional cleanup:
  - Pre-existing analyzer/nullability warnings not introduced by this feature.
```
