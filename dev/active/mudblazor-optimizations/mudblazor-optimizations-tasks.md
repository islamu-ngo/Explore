# MudBlazor Optimizations — Task Checklist

Last Updated: 2026-05-08 Europe/Brussels

## Status Summary
- **Overall status:** Approved
- **Completed:** 5/6 Slices
- **Current priority:** Slice D.3
- **Next recommended slice:** Slice D.3: Auth Forms Migration

## Implementation Maintenance Rules
- [ ] Before starting work, read plan/context/tasks.
- [ ] After each completed task, update this checklist immediately.
- [ ] If implementation changes scope or architecture, update the plan before continuing.
- [ ] If discoveries affect future work, update the context file.
- [ ] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

## Phase 0: Plan Review And Baseline
- [x] User reviews the plan and approves or corrects scope.
- [ ] Implementation agent confirms current repo state before first edit.

## Slice A1: Governance And Runtime Defaults ✅ COMPLETED
- [x] **A1.1 Draft UI Governance ADRs**
  - **Files:** `docs/UI_GOVERNANCE.md` (or append to `DESIGN_SYSTEM.md`)
  - **Description:** Document primitive vs semantic component policy, canonical form architecture target, and MudBlazor v9 default strategy.
  - **Acceptance:** Governance docs exist and are clear.
- [x] **A1.2 Configure MudBlazor v9 Provider/Theme Defaults**
  - **Files:** `Explore.Blazor/Program.cs`, `Explore.Blazor.Client/Program.cs`
  - **Description:** Inject valid MudBlazor provider/service defaults (e.g. `AddMudServices` options). DO NOT use removed `MudGlobal` theming defaults (ButtonVariant, InputDefaults, etc).
  - **Acceptance:** Build proves installed MudBlazor API compatibility. Visual defaults are documented as theme/token/explicit-parameter decisions.

## Slice A2: CSS/Token Cleanup ✅ COMPLETED
- [x] **A2.1 Audit and Clean mudblazor-overrides.css**
  - **Files:** `mudblazor-overrides.css`, `AppearanceThemeService.cs`
  - **Description:** Audit `mudblazor-overrides.css`. Move low-risk visual tokens into `MudTheme.LayoutProperties` or scoped CSS. Leave complex layout/drawer overrides until thoroughly verified.
  - **Acceptance:** Global CSS file is reduced safely without breaking layouts.

## Slice B: Primitive Wrapper Elimination ✅ COMPLETED
- [x] **B.1 Replace Primitive Proxy Wrappers**
  - **Files:** All `**/*.razor`
  - **Description:** Replace primitive proxies (`AppButton`, `AppTextField`, `AppIconButton`, `AppCard`) with native MudBlazor elements + explicit parameters. Retain `AdditionalAttributes`.
  - **Acceptance:** Proxy wrappers removed safely.
- [x] **B.2 Protect Semantic Components**
  - **Files:** `AppDialogShell.razor` and other semantic layout wrappers.
  - **Description:** Individually review layout wrappers. Do not delete them automatically. If they standardize layouts, preserve them.
  - **Acceptance:** Semantic components function correctly and are not deleted.

## Slice C: Validation Architecture Standardization 🟡 IN PROGRESS
- [x] **C.1 Build Canonical Form Infrastructure**
  - **Files:** New files in `Explore.Blazor.Client/Components/Forms/` (`FormSubmitState`, `ServerValidationErrorStore`, `AppValidationSummary`, `FormSubmissionGuard`).
  - **Description:** Implement infrastructure that handles one-submit-at-a-time guarding, server error mapping (401/403/Validation), live announcements, and focus management.
  - **Acceptance:** Concrete components and extensions exist. No domain form migration begins until this is robust.

## Slice D: Form Migration By Domain 🟡 IN PROGRESS
*(For each slice: Replace `MudForm` with standard `EditForm` infrastructure + FluentValidation. Validate using the explicit Accessibility Validation Checklist).*

- [x] **D.1 Migrate Event Management Forms**
  - **Files:** `CreateEvent.razor`, `EventEdit.razor`, `CreateSession.razor`, `EditSession.razor`
  - **Acceptance:** Forms migrated. A11y checklist passed.
- [x] **D.2 Migrate Settings Forms**
  - **Files:** `SettingsPersonalInfo.razor`, `OrganizationDetails.razor`
  - **Acceptance:** Forms migrated. A11y checklist passed.
- [ ] **D.3 Migrate Auth Forms**
  - **Acceptance:** Forms migrated. A11y checklist passed.
- [ ] **D.4 Migrate Admin Forms**
  - **Acceptance:** Forms migrated. A11y checklist passed.
- [ ] **D.5 Migrate Onboarding Forms**
  - **Acceptance:** Forms migrated. A11y checklist passed.

## Verification Checklist
- [ ] LSP diagnostics clean for modified files.
- [ ] `dotnet build --configuration Release --verbosity quiet` passes.
- [ ] Strict accessibility checklist verified for every migrated form domain.
- [ ] Docs updated (`UI_GOVERNANCE.md`, `DESIGN_SYSTEM.md`).
- [ ] Dev docs refreshed with final state.

## Remaining / Deferred Work
- None currently.