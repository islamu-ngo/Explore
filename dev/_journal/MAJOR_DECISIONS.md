# Major Decisions

Last Updated: 2026-02-27

## 2026-02-23 18:12 Europe/Brussels - Admin Consolidation Handoff Scope

- Decision: Consolidate admin UX into two panel pages only:
  - `Explore.Blazor.Client/Pages/Admin/Tenant/TenantAdminSettings.razor`
  - `Explore.Blazor.Client/Pages/Admin/Instance/InstanceAdminSettings.razor`
- Why: User explicitly requested eliminating split between `/admin` and separate admin pages, and matching existing settings-style panel navigation.
- Implication: Legacy `/admin` dashboard and standalone lookup/admin pages are target candidates for removal after migration.

## 2026-02-23 18:12 Europe/Brussels - SMTP Configuration Placement

- Decision: Add SMTP configuration under instance admin panel as a dedicated sidebar section, with a test connection action.
- Why: SMTP credentials are platform-level concern requested for platform/instance administrators.
- Implementation anchor points:
  - UI pattern: `Explore.Blazor.Client/Components/Admin/Instance/InstanceStorageSection.razor`
  - API pattern: `Explore.API/Controllers/InstanceOnboardingController.cs` storage settings/test endpoints
  - Setting keys: `Explore.Domain/Constants/GovernanceSettingKeys.cs` (`EmailSmtp*`, `EmailFrom*`)

## 2026-02-23 18:12 Europe/Brussels - Dev Docs Continuity Protocol

- Decision: Before context reset, update every active context/tasks file with a timestamped checkpoint entry, and add deep handoff detail to the currently active track only.
- Why: Ensures broad continuity for all active tracks while preserving high-signal detail where active implementation is ongoing.

## 2026-02-23 18:47 Europe/Brussels - Admin Consolidation Implementation Completed

- Decision: Complete the consolidation by deleting legacy standalone admin pages/routes after embedding equivalent capabilities into panel sections.
- Why: Prevent duplicate administrative entry points and keep one canonical settings-style admin UX per role.
- Outcome:
  - Tenant administration now hosts organizations + lookup management.
  - Instance administration now hosts SMTP settings + test connection.
  - Navbar admin dropdown routes now point directly to tenant/instance administration pages.

## 2026-02-23 18:47 Europe/Brussels - Verification Baseline for This Delivery

- Decision: Treat successful `dotnet build` + targeted Blazor and Application unit tests as release gate for this session due lack of Razor LSP in environment.
- Why: Ensures functional validation while acknowledging toolchain limitation for `.razor` diagnostics.
- Evidence:
  - Build passed.
  - Blazor client tests passed (522).
  - Application unit tests passed (278).

## 2026-02-27 Europe/Brussels - Blazor Folder Restructure Continuation Baseline

- Decision: Treat `dev/active/blazor-folder-restructure` as implementation-complete with remaining work focused on checklist/doc synchronization and optional full-suite gate validation.
- Why: Core migration, imports, dialog helper refactor, and targeted Blazor test loop are already green; unresolved items are primarily documentation fidelity and broader release assurance.
- Verification anchor:
  - `dotnet build --configuration Release --verbosity quiet` passes (warnings only).
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passes (warnings only).

## 2026-02-27 Europe/Brussels - Context Reset Handoff Policy (Active Docs)

- Decision: Append explicit session checkpoint blocks to every `dev/active/*-context.md` and `dev/active/*-tasks.md` file during context-limit handoff.
- Why: Ensures no active track is left without fresh continuity metadata, reducing reset-time archaeology and ambiguity.

## 2026-02-27 Europe/Brussels - Blazor Client Contracts Boundary

- Decision: Standardize on root `Explore.Blazor.Client/Contracts` for interface contracts and keep `Explore.Blazor.Client/Services` as implementation-only.
- Structure adopted:
  - `Contracts/Services/{Lookup,Events,Organizations}`
  - `Contracts/Providers`
  - `Contracts/Interop`
- Why: Supports future non-service abstractions (providers/interop), improves testability, and avoids conflating API proxy interfaces with concrete service implementations.
- Verification:
  - `dotnet build --configuration Release --verbosity quiet` passed after namespace and Razor import updates.
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed (518 tests).
