# MudBlazor Optimizations — Context

Last Updated: 2026-05-08 Europe/Brussels

## SESSION PROGRESS (2026-05-08 Europe/Brussels)

### ✅ COMPLETED
- Planning created and revised based on CTO feedback.
- Architecture strategy shifted to vertical slices, distinguishing primitive proxy wrappers from semantic components, and emphasizing frontend governance.
- Corrected plan regarding MudBlazor v9 `MudGlobal` removed APIs.
- **Slice A1 completed:** drafted `UI_GOVERNANCE.md` and added safe MudBlazor v9 provider defaults in `Program.cs`.
- **Slice A2 completed:** cleaned up `mudblazor-overrides.css` and mapped border tokens (`LinesInputs`) properly in `AppearanceThemeService.cs`.
- **Slice B completed:** Used a python script to aggressively replace primitive proxies (`AppButton`, `AppTextField`, `AppIconButton`, `AppCard`) with `Mud` counterparts. Cleaned up multiple compilation errors (`CS0103`, `CS1662`, `CS0266`, `CS1503`) resolving parameter naming mismatch and out-of-sync API contracts (NSwag updates).

### 🟡 IN PROGRESS
- Slice C (Validation Architecture Standardization).
- **Task C.1 completed:** Created canonical Form Infrastructure in `Explore.Blazor.Client/Components/Forms/` (`FormSubmitState`, `ServerValidationErrorStore`, `AppValidationSummary`, `FormSubmissionGuard`).

### ⏭️ NEXT
1. Verify the newly added components build correctly.
2. Begin domain migrations starting with D.1 (Migrate Event Management Forms).

### ⚠️ BLOCKERS
- None.

## Quick Resume
1. Read `mudblazor-optimizations-plan.md`.
2. Read `mudblazor-optimizations-tasks.md`.
3. Start from the first unchecked high-priority task (Slice A1) unless user instruction overrides it.
4. Keep all three dev docs updated after each meaningful implementation slice.

## Key Files And Responsibilities
| Path | Existing/New | Layer | Purpose | Notes |
| :--- | :--- | :--- | :--- | :--- |
| `docs/UI_GOVERNANCE.md` | New | Docs | Frontend Rules | Canonical form patterns, semantic component guidelines. |
| `Explore.Blazor.Client/Program.cs` | Existing | Blazor | App Bootstrapping | Configure v9 provider/service defaults here. |
| `Explore.Blazor.Client/Components/Common/App*.razor` | Existing | Blazor | UI Wrappers | Primitive proxy wrappers (e.g. `AppButton`) deleted; Semantic wrappers (`AppDialogShell`) kept. |
| `Explore.Blazor/wwwroot/css/mudblazor-overrides.css` | Existing | UI/CSS | Global Hacks | To be audited and safely reduced (Slice A2). |

## Key Decisions
- **Provider Defaults over MudGlobal:** Do not use removed MudBlazor v9 `MudGlobal` theming defaults. Use provider options, `MudTheme`, explicit parameters, and semantic components instead.
- **Distinguish Wrappers:** Delete primitive proxy wrappers, but deliberately retain/encourage semantic composition components (e.g., layout panels, dialog shells).
- **Form Architecture Standard:** Formalize a standard `EditContext` architecture (submit pipelines, server-error adapters, async busy-states) BEFORE migrating any forms.
- **Vertical Slices:** Migrate forms domain-by-domain to minimize "big bang" operational risk.
- **Accessibility Checklist:** Mandatory validation per form migration domain (tab order, focus restore, screen-reader announcements).

## Constraints And Rules To Remember
- The UI architecture flow is: `MudBlazor primitives` → `Design tokens` → `Semantic composition components` → `Feature components`.
- `AdditionalAttributes` must be retained on native and semantic components to support testing hooks and ARIA attributes.
- No backward compatibility required for proxy wrappers.

## Validation Baseline
- `dotnet build --configuration Release` must pass.
- Detailed accessibility checklist validation is required for each form migration slice.

## Current Known Risks / Unknowns
- Ensuring the new Form Architecture Standard fully replicates or improves upon the robust UX provided by implicit `MudForm` behaviors.
- CSS token cleanup causing unintentional wide-ranging visual regressions if done too aggressively.

## Handoff Notes

### Handoff — 2026-05-08 Europe/Brussels
- **Current state:** Task D.2 completed. Settings forms (`SettingsPersonalInfo.razor`, `OrganizationDetails.razor`) are migrated to the Canonical Form Infrastructure.
- **Next action:** Begin Slice D.3 (Migrate Auth Forms).
- **Blockers:** None.
- **Validation:** `dotnet build` succeeds entirely with 0 errors.
- **Risks:** Ensure that remaining domains also successfully adapt their specific state variables to `FormSubmitState` and `ServerValidationErrorStore`.