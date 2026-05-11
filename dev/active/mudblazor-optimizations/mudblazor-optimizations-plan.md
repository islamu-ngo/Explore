# MudBlazor Optimizations — Implementation Plan

Last Updated: 2026-05-08 Europe/Brussels

## 0. Planning Metadata
- **Request:** Write implementation plan to work on MudBlazor improvement areas: fixing form inconsistencies, refactoring component wrappers for maintainability, and improving CSS isolation, ignoring backward compatibility.
- **Task directory:** `dev/active/mudblazor-optimizations/`
- **Planning status:** Approved
- **Matched intents:** UI Refactoring / Blazor UI Conventions (Fallback Contract)
- **Relevant skills:** `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`
- **Relevant rules:** Standard Blazor/MudBlazor development rules, UI consistency, Enterprise Form Architecture, Accessibility standards.
- **Primary layers touched:** Blazor (Client & Server host)
- **Estimated complexity:** XL (Cross-cutting UI architectural shift, executed in vertical domain slices)

## 1. Executive Summary
This plan outlines a comprehensive, enterprise-grade refactoring of the Blazor frontend to establish a mature UI architecture. We are transitioning from a fragile "custom wrapper ecosystem" to a layered design-system architecture using native MudBlazor v9 primitives, robust theme configuration, and semantic composition components. Key outcomes include the elimination of primitive proxy wrappers (e.g., `AppButton`), the standardization of forms using a newly defined canonical `EditForm` + `FluentValidation` architecture, and the reduction of global CSS hacks. To mitigate operational and accessibility regression risks, the migration will be executed in vertical domain slices (Event, Settings, Auth, etc.) rather than a "big bang" global replace, preceded by the establishment of formal Frontend Governance rules. 

Note: MudBlazor v9 removed `MudGlobal` theming defaults; we will leverage provider options, `MudTheme`, design tokens, semantic components, and explicit parameters to achieve consistency.

## 2. Source-Grounded Current State Report
### 2.1 Evidence Log
| Claim | Evidence | Confidence | Notes |
| :--- | :--- | :--- | :--- |
| Proxy wrappers used for defaults | `Verified: Explore.Blazor.Client/Components/Common/AppButton.razor` | High | Wraps MudButton to set `Variant.Filled` and `Elevation=0`. |
| Split form logic | `Verified by search: pattern "MudForm" matched 59 instances, "EditForm" matched 10` | High | Inconsistent validation UX and developer experience. |
| Global CSS overrides | `Verified: Explore.Blazor/wwwroot/css/mudblazor-overrides.css` | High | Custom drawer logic and hardcoded borders. |
| Dynamic Theme Engine | `Verified: Explore.Blazor.Client/Services/AppearanceThemeService.cs` | High | Creates MudTheme from DTOs. |

### 2.2 Existing Implementation
- **Forms:** The project uses both `EditForm` (e.g., `CreateEvent.razor`) and `MudForm` (e.g., `CreateCategoryDialog.razor`, Settings pages) without a standardized submit lifecycle or error-mapping strategy.
- **Component Wrappers:** Primitive proxy wrappers like `AppButton` and `AppTextField` forward parameters, breaking intellisense and causing maintenance overhead.
- **Styling:** Global styling and structural overrides are maintained in a monolithic `mudblazor-overrides.css` file, violating CSS layer ADRs.
- **Theming:** `AppearanceThemeService` dynamically constructs `MudTheme` and uses fluid typography (`clamp()`).

### 2.3 Existing Tests And Verification Coverage
- Blazor UI is primarily covered by end-to-end (Playwright) tests or bUnit tests.
- Form migrations risk breaking keyboard workflows and ARIA validation announcements, which currently lack explicit automated test coverage.

### 2.4 Existing Documentation And Contracts
- Customizations are currently documented via `ABOUTME:` tags.
- Frontend governance (canonical form patterns, semantic component guidelines) is currently missing and must be created.

### 2.5 Current Pain Points / Improvement Areas
- **Form Inconsistency:** Divergent validation timing, accessibility behavior, and error handling between `MudForm` and `EditForm`.
- **Proxy Wrapper Overhead:** High maintenance burden and API drift risk for non-semantic wrappers (`AppButton`).
- **Global CSS Fragility:** Hardcoded structural overrides in global CSS conflict with component scopes.
- **Missing UI Governance:** Lack of documented patterns leads to UI drift and varied implementation styles.

### 2.6 Unknowns After Investigation
- Extent of imperative validation logic (`form.Validate()`) currently hidden inside nested dialogs.

## 3. Proposed Future State
The UI layer will follow a strict architectural flow:
`MudBlazor primitives` → `Design tokens` → `Semantic composition components` → `Feature components`.
- **Forms:** Governed by a canonical Form Architecture Standard utilizing `EditContext`, `FluentValidation`, standardized submit pipelines, server-error adapters, and async busy-states.
- **Components:** Primitive proxy wrappers are removed. Native `MudBlazor` components are used directly, configured by provider defaults/themes, and styled by tokens. Semantic composition components (e.g., `AuditLogPanel`, `PermissionGate`, `AppDialogShell`) are encouraged.
- **Styling:** `MudTheme.LayoutProperties` and scoped CSS replace global `.mud-*` override hacks.
- **Governance:** A formalized set of UI architecture ADRs and guidelines prevent future drift.

## 4. Non-Negotiable Constraints
- No backward compatibility shims for primitive proxy wrappers.
- All forms must use the defined Canonical Form Architecture (FluentValidation).
- Avoid generic proxy wrappers with unconstrained `AdditionalAttributes` pass-through, but **preserve** `AdditionalAttributes` / `UserAttributes` support on native and semantic components for accessibility (ARIA), test hooks, and analytics.
- Maintain responsive fluid typography (`clamp()`) within the `AppearanceThemeService`.
- No "big bang" migrations. Forms must be migrated by feature domain.

## 5. Architecture And Design Decisions
- **Decision:** Define a Canonical Form Architecture Standard before migration.
- **Why:** Replaces ad-hoc implementations with predictable submit lifecycles, async validation rules, and standardized API error reconciliation.
- **Decision:** Remove primitive proxy wrappers (e.g. `AppButton`) but encourage semantic composition components.
- **Why:** Proxy wrappers add overhead and hide APIs; semantic wrappers encapsulate business intent and composition logic.
- **Decision:** Migrate forms by vertical feature domain slices.
- **Why:** Mitigates operational risk, allows isolated QA and accessibility validation, and prevents overwhelming the test surface.
- **Decision:** Do not rely on MudBlazor v9 `MudGlobal` theming defaults.
- **Why:** MudBlazor v9 removed these APIs (like ButtonVariant defaults) because they blur behavioral and visual concerns. We will use provider options, `MudTheme`, and explicit parameters instead.

## 6. Implementation Phases

### Slice A1: Governance And Runtime Defaults
- **Goal:** Formalize UI governance and establish valid provider defaults.
- **Acceptance criteria:**
  - Create `docs/UI_GOVERNANCE.md`.
  - Document primitive vs semantic component policy.
  - Document canonical form architecture target.
  - Configure MudBlazor v9-compatible provider/service defaults where supported.
  - Visual defaults moved to `MudTheme`, design tokens, explicit component parameters, or semantic composition components. Removed `MudGlobal` theming defaults are strictly avoided.

### Slice A2: CSS/Token Cleanup
- **Goal:** Stabilize the rendering foundation safely without broad visual regressions.
- **Acceptance criteria:**
  - Audit `mudblazor-overrides.css` and classify each override.
  - Move only low-risk visual tokens into `MudTheme.LayoutProperties` or scoped CSS first.
  - Leave drawer/layout overrides until verified visually to avoid breaking core layouts.

### Slice B: Primitive Wrapper Elimination
- **Goal:** Remove primitive proxy wrappers without changing behavior or visuals.
- **Acceptance criteria:**
  - `AppButton`, `AppTextField`, `AppCard`, `AppIconButton` safely replaced with native MudBlazor equivalents.
  - Semantic layout components like `AppDialogShell` are reviewed individually and preserved if they standardize layouts, focus behaviors, or responsive boundaries.
  - Native components correctly utilize explicit parameters and `AdditionalAttributes` for testing/ARIA.

### Slice C: Validation Architecture Standardization
- **Goal:** Define and build the concrete core form infrastructure. No domain migration may begin until this handles server errors, submit guarding, async busy states, authorization failures, focus management, and live announcements.
- **Acceptance criteria:**
  - Create structural components (e.g., `AppValidationSummary`, `FormSubmitState`, `ServerValidationErrorStore`, `EditContextServerErrorExtensions`, `FormSubmissionGuard`).
  - Standardized submit pipeline handles 401/403 mapping, one-submit-at-a-time guarding, and cancellation token propagation.
  - Focus restore (to opener after dialog close) and focus-to-first-invalid-field logic are formalized.

### Slice D: Form Migration By Domain
- **Goal:** Migrate `MudForm` to the new architecture, one domain at a time, followed by rigorous accessibility testing.
- **Sub-slices:**
  - **D1:** Event Management Forms
  - **D2:** Settings Forms
  - **D3:** Auth Forms
  - **D4:** Admin Forms
  - **D5:** Onboarding Forms
- **Acceptance criteria (per slice):**
  - Domain forms use standard `EditForm` + `FluentValidation` infrastructure.
  - **Accessibility Validation Checklist (Mandatory):**
    - Tab order matches visual order.
    - Submit button is reachable by keyboard.
    - Invalid submit keeps focus inside form/dialog.
    - First invalid field receives focus or validation summary receives focus.
    - Validation errors are visible and screen-reader discoverable.
    - Async submit announces busy/success/failure state.
    - Escape/close behavior restores focus to opener.
    - Destructive action confirmation has correct focus target.
    - No validation-only color dependency.
    - No physical-direction CSS added.
    - Mobile layout remains usable at narrow width.

## 7. Testing Strategy
- **Accessibility Validation (Crucial):** Dedicated manual/automated checks per the checklist above for screen reader parity, keyboard workflows, and live-region announcements for every migrated form slice.
- **Unit Tests:** Update bUnit tests for wrapper removal.
- **Integration Tests:** Ensure FluentValidation rules cover all scenarios previously handled by `MudForm` inline rules.

## 8. Documentation, Configuration, And Operations Impact
- Create `docs/UI_GOVERNANCE.md` detailing the new Canonical Form Architecture and allowed CSS override policies.

## 9. Security, Authorization, Privacy, And Abuse Considerations
- Ensure the new form submit pipelines correctly handle authorization failures (401/403) via the standardized server-error adapters.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations
- **Accessibility (CRITICAL):** The migration away from `MudForm` alters validation timing and ARIA associations. Rigorous NVDA and keyboard workflow validation based on the Slice D checklist is required to ensure no accidental degradation of the user experience.
- **Localization:** FluentValidation and the new server-error adapters must integrate with the existing localization infrastructure.

## 11. Observability And Operations
- Ensure standard form infrastructure logs unexpected validation state failures uniformly.

## 12. Migration And Compatibility Plan
- Executed incrementally via Domain Slices (Slice D1-D5) to allow safe merging and continuous deployment without breaking the entire platform concurrently.

## 13. Risk Register
| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
| :--- | :--- | :--- | :--- | :--- | :--- |
| Big Bang Operational Failure | Low | High | Restructured plan to migrate forms by vertical domain slices (Slice D). | Test failures isolated to specific domains. | Impl Agent |
| Silent Accessibility Regressions | High | High | Dedicated accessibility validation phase per domain slice with an exact checklist. | Manual a11y testing feedback. | Impl Agent |
| Inconsistent Form Lifecycles | Medium | Medium | Build strict Canonical Form Architecture (Slice C) before migrating any forms. | Code review against UI Governance ADRs. | Impl Agent |
| Accidental Wrapper Reintroduction | Medium | Low | Draft clear Governance docs (Slice A1) differentiating proxy vs semantic components. | Code reviews. | Impl Agent |

## 14. Success Metrics And Definition Of Done
- **Implementation Metrics:** 0 usages of `MudForm` and primitive proxy wrappers. Substantial reduction in generic overrides in `mudblazor-overrides.css`.
- **Operational Quality Metrics:**
  - Dialog validation consistency across all domains.
  - Keyboard workflow and accessibility parity (verified by checklist).
  - Reduced CSS specificity conflicts.
  - Improved contributor onboarding (measured by presence of clear UI Governance docs).
  - Reduced UI regressions during future MudBlazor upgrades.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT
Future agents implementing this plan MUST follow this contract:
1. Before starting any implementation slice, read this plan, `[task-name]-context.md`, and `[task-name]-tasks.md`.
2. Start from the highest-priority incomplete task unless user instruction overrides it.
3. After completing each meaningful task or discovering new scope, update:
   - this plan if architecture/scope/phases/risks changed;
   - `[task-name]-context.md` with current state, decisions, files changed, blockers, validation, and next step;
   - `[task-name]-tasks.md` by checking completed items and adding discovered tasks.
4. Do not report “done” unless docs reflect the actual current state.
5. Every implementation summary to the user must include:
   - what was implemented;
   - what was verified;
   - what remains;
   - what should be worked on next.
6. If validation fails, update context/tasks with the failure, root cause if known, and next recovery action.
7. Before pausing, context reset, handoff, or PR creation, refresh all three dev docs and add/refresh a handoff section.

## 16. Progress Reporting Contract
When an implementation agent finishes a slice, its final response should use this concise structure:
- **Implemented:** ...
- **Verified:** ...
- **Remaining:** ...
- **Next:** ...
- **Docs updated:** plan/context/tasks updated? yes/no with reason

## 17. Potential Risks & Unknowns
The highest operational risk is the silent degradation of accessibility and workflow states (focus loss, unannounced validation errors) during the `MudForm` to `EditForm` migration. MudBlazor handles some of this implicitly; standard Blazor `EditForm` requires explicit architectural scaffolding (Slice C) to achieve enterprise-grade consistency. Ensuring the standardized submit pipeline and server-error adapters handle all edge cases (async saves, concurrent submits, dirty-state, proper ARIA focus targeting) robustly is critical before Slice D begins.