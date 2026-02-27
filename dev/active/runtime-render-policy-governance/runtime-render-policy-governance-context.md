# Runtime Render Policy Governance - Context

**Last Updated: 2026-02-23**

## SESSION PROGRESS (2026-02-23)

### Completed

1. Confirmed current render UX work status and constraints from prior session.
2. Verified key files/classes exist before planning references.
3. Collected external guidance baseline using Tavily + Context7 for InteractiveAuto/prerender/interactivity readiness.
4. Created plan document for runtime policy governance and onboarding constraints.
5. Created this context and tasks tracking package.
6. Implemented Phase 1 governance contract changes in code:
   - Added render-policy keys (including global fallback keys) and grouped/nested key structure in `Explore.Domain/Constants/GovernanceSettingKeys.cs`.
   - Added secret-key boundary class `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs` and switched key usage in storage/mail services/resolvers.
   - Extended `InstanceGovernanceSettingsDto` and client settings model with global render-policy fields.
   - Added render-policy enums in domain for type-safe validation and normalization.
   - Added `InstanceGovernanceSettingsDtoValidator` and integrated it into complete/update instance onboarding command handlers.
   - Enhanced `InstanceGovernanceSettingService` with preset normalization, global fallback behavior, onboarding InteractiveServer guardrail enforcement, and persisted global policy keys.
7. Added/updated tests for validator, handler validation path, and governance service normalization behavior.
8. Verified build and relevant test projects are green for this scope.
9. Implemented Phase 2 runtime integration:
   - Extended anonymous-safe `PublicExperienceSettingsDto` payload with render-policy fields.
   - Wired `GetPublicExperienceSettingsQueryHandler` to source normalized governance render-policy settings via `IInstanceGovernanceSettingService`.
   - Added `RuntimeRenderPolicyService` in Blazor client services to classify route groups and resolve mode/prerender using global fallback + advanced overrides.
   - Integrated resolver into `Explore.Blazor/Components/App.razor` for dynamic `HeadOutlet` and `Routes` render mode selection.
   - Preserved startup-loader behavior by deriving loader visibility from resolved prerender policy.
10. Added focused tests for Phase 2 behavior:
   - `GetPublicExperienceSettingsQueryHandlerTests` now verifies render-policy payload projection.
   - New `RuntimeRenderPolicyServiceTests` verifies route-group mapping, fallback behavior, SeoBalanced public prerender, and onboarding InteractiveServer guardrail.
11. Re-verified build and affected test projects are green after Phase 2 changes.
12. Implemented Phase 3 telemetry for onboarding guardrail rejection events:
   - Added structured warning logging in `UpdateInstanceGovernanceSettingsCommandHandler` when onboarding policy guardrail is violated.
   - Log payload now includes user id, attempted onboarding render mode, and guardrail toggle state.
13. Added test coverage validating guardrail rejection telemetry emission in `UpdateInstanceGovernanceSettingsCommandHandlerTests`.
14. Re-verified build and affected test projects are green after Phase 3 changes.
15. Implemented Phase 4 instance settings UX in `InstanceGovernanceSection`:
    - Added preset-first render policy cards with recommended default and recommended badge.
    - Added info tooltips for preset strategy and each preset option.
    - Added advanced overrides panel for global fallback and per-route-group mode/prerender controls.
    - Enforced onboarding InteractiveServer restriction in UI normalization helpers.
16. Added scoped styling in `InstanceGovernanceSection.razor.css` for selected/recommended card emphasis and responsive card layout.
17. Added bUnit coverage in `InstanceGovernanceSectionTests` for recommended default selection, advanced preset selection flow, and hover-help tooltip text presence.
18. Updated `docs/RENDER_POLICIES.md` to reflect the implemented preset-first governance model, route-group resolution, and onboarding guardrail invariant.
19. Added API integration coverage for instance onboarding render-policy flows in `Event.API.IntegrationTests/Features/InstanceOnboardingControllerTests.cs`:
    - Anonymous status access.
    - Save + retrieve governance settings via onboarding complete/settings endpoints.
    - Reject flows for missing setup secret and invalid onboarding InteractiveServer policy.
20. Documented rollout timing contract for active sessions/tabs in `docs/RENDER_POLICIES.md` under operational notes.
21. Re-verified API integration suite with all tests passing after new onboarding coverage.

### In Progress

1. None.

### Blockers

1. None.

## User Constraints to Preserve

1. Onboarding rule: Interactive Server disallowed for onboarding flows.
2. Add instance settings UX requirements:
    - Info icon + hover help text.
    - Recommended option preselected.
    - Recommended option visually emphasized with a different border.
3. Ground decisions with Tavily + Context7 guidance.

## Verified Key Files

| File | Why it matters |
|------|----------------|
| `Explore.Blazor/Components/App.razor` | Current top-level render decision boundary |
| `Explore.Blazor.Client/Routes.razor` | Existing interactivity-ready lifecycle hook |
| `Explore.Blazor/wwwroot/css/StyleGlobal.css` | Startup loader and visual behavior for non-SEO pages |
| `Explore.Blazor.Client/Components/Admin/Instance/InstanceAdminSettingsLayout.razor` | Parent shell for instance settings sections |
| `Explore.Blazor.Client/Components/Admin/Instance/InstanceGovernanceSection.razor` | Target section for policy controls |
| `Explore.Application/DTOs/Onboarding/InstanceGovernanceSettingsDto.cs` | DTO boundary to extend for policy settings |
| `Explore.Domain/Constants/GovernanceSettingKeys.cs` | Runtime setting keys location |
| `Explore.API/Controllers/InstanceOnboardingController.cs` | Existing governance/onboarding API surface |

## Planning Decisions (Current)

### D1 - Preset-first governance model

Use a preset-first model for admin usability, then expose advanced overrides for power users.

### D2 - SEO and operational split remains explicit

Preserve prerender for SEO-critical routes and prioritize predictable interactivity UX for operational/onboarding routes.

### D3 - Onboarding guardrail is an invariant

Onboarding route group must never map to Interactive Server.

### D4 - Validation belongs server-side

UI can guide choices, but invalid policy combinations are rejected by application/API validation.

### D5 - Runtime change safety

Policy resolver must have a fallback policy when governance values are missing or malformed.

## External References Snapshot

1. Microsoft Blazor render modes docs (via Context7): InteractiveAuto behavior and prerender defaults.
2. Microsoft guidance on `RendererInfo.IsInteractive` (via Context7): gate pre-interactive state and controls.
3. Tavily-sourced Microsoft pages on prerender strategy and render mode tradeoffs.

## Technical Constraints

1. Keep layer boundaries per Clean Architecture.
2. Keep settings-driven behavior runtime configurable.
3. Avoid coupling route classification to UI-only concerns.
4. Existing user-facing startup loader behavior should remain compatible.

## Quick Resume

1. Read `runtime-render-policy-governance-plan.md` for full execution strategy.
2. Phases 1-5 are implemented; use `runtime-render-policy-governance-tasks.md` for completion status and audit trail.
3. Next technical step: optional hardening and maintenance only (no open required items in this track).
4. Keep onboarding Interactive Server restriction as a non-negotiable invariant across validation and runtime resolution.

---

## SESSION CHECKPOINT (2026-02-27 Europe/Brussels)

### Status This Session
- No implementation changes were made in this task during this session.
- Task remains in its previously documented state.

### Continuation Notes
- Re-open this context file and matching *-tasks.md before resuming work.
- Re-run project build/tests relevant to that task branch before new edits.

