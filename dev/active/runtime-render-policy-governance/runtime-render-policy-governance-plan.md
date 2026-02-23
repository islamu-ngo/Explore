# Runtime Render Policy Governance - Implementation Plan

**Last Updated: 2026-02-23**

## Executive Summary

This plan defines a runtime-governed Blazor render policy system managed from Instance Admin settings, with safe defaults for SEO pages and interactivity-first behavior for operational and onboarding flows. It adds a preset-first admin UX plus advanced overrides, while enforcing onboarding-specific constraints to maximize conversion and reduce refresh confusion during startup.

Scope for this track is planning only (no implementation in this deliverable).

## Current State (Verified)

| Area | File | Notes |
|------|------|-------|
| Render boundary and route behavior | `Explore.Blazor/Components/App.razor` | App-level route-aware InteractiveAuto/prerender decisions exist |
| Interactive readiness transition | `Explore.Blazor.Client/Routes.razor` | First render lifecycle hook already used |
| Startup loader visuals | `Explore.Blazor/wwwroot/css/StyleGlobal.css` | Non-SEO startup loading UX is already present |
| Instance admin settings shell | `Explore.Blazor.Client/Components/Admin/Instance/InstanceAdminSettingsLayout.razor` | Sectioned settings layout |
| Governance section | `Explore.Blazor.Client/Components/Admin/Instance/InstanceGovernanceSection.razor` | Existing governance edit pattern |
| Governance constants | `Explore.Domain/Constants/GovernanceSettingKeys.cs` | Runtime key model exists |
| Governance DTO transport | `Explore.Application/DTOs/Onboarding/InstanceGovernanceSettingsDto.cs` | Extendable DTO boundary |
| Onboarding API entrypoint | `Explore.API/Controllers/InstanceOnboardingController.cs` | Existing API surface for onboarding/governance flows |

## External Guidance Baseline (Tavily + Context7)

1. Interactive Auto is appropriate when balancing fast first paint and WASM interactivity.
2. Prerender should be selective: preserve it for SEO-critical routes.
3. `RendererInfo.IsInteractive` is the canonical way to gate pre-interactive UI/controls.
4. Render-mode consistency is best managed at top-level boundaries.

## Target Future State

### Runtime policy model

1. Presets for common goals:
   - `SEO Balanced (Recommended)`
   - `All Prerendered`
   - `All InteractiveAuto (No Prerender)`
   - `Custom Advanced`
2. Advanced controls for route groups:
   - Public SEO pages
   - Operational pages
   - Admin pages
   - Onboarding pages
3. Onboarding hard guardrail:
   - Interactive Server is disallowed for onboarding routes.
4. Policy stored via governance settings and applied at runtime without redeploy.

### Instance settings UX requirements

1. Info icon on each policy choice.
2. Hover help with plain-language tradeoffs.
3. Recommended option preselected by default.
4. Recommended option clearly highlighted with a distinct border and label.

## Clean Architecture Plan

### Phase 1 - Domain/App contracts (6-8h)

1. Add governance keys for render-policy values and versioning.
2. Extend governance DTO/model for presets + advanced overrides.
3. Add validation rules for incompatible combinations.

Acceptance criteria:
- Governance payload supports preset and advanced values.
- Invalid combinations return validation failures with clear messages.

### Phase 2 - Policy resolution service (8-10h)

1. Add application-level resolver that maps route group -> render mode + prerender flag.
2. Add stable fallback policy when settings are missing/corrupt.
3. Integrate resolver outputs into app render boundary.

Acceptance criteria:
- Deterministic route-group resolution.
- No null/invalid policy causes broken rendering.

### Phase 3 - Onboarding invariant enforcement (3-4h)

1. Define onboarding route group explicitly.
2. Reject Interactive Server mapping for onboarding in validator and resolver.
3. Add telemetry/log entry on rejected updates.

Acceptance criteria:
- Onboarding never resolves to Interactive Server.

### Phase 4 - Admin UI delivery (6-8h)

1. Add preset cards and advanced section to `InstanceGovernanceSection.razor`.
2. Add info icon + hover text for each option.
3. Apply preselected recommended option and high-contrast border styling.

Acceptance criteria:
- Recommended option is both selected and visually obvious.
- Tooltip/help content is accessible and understandable.

### Phase 5 - Verification + docs (6-9h)

1. Unit tests for resolver and validation.
2. Integration tests for API settings save/retrieve and rule enforcement.
3. Blazor tests for default selection + help overlays.
4. Update `docs/RENDER_POLICIES.md` and governance docs.

Acceptance criteria:
- Test coverage includes presets, advanced overrides, onboarding guardrail, fallback behavior.

## Dependencies

1. Existing governance setting persistence and key model.
2. Existing app render boundary implementation.
3. Existing instance settings API + UI save/load flows.

## Authorization and Security

1. Write access limited to instance-level admin authority.
2. Validation and enforcement server-side, not UI-only.
3. Governance changes auditable through existing logging approach.

## Migration and Rollout Considerations

1. Additive keys only; no destructive migration expected.
2. Default to `SEO Balanced (Recommended)` for backward-safe rollout.
3. Support phased rollout by instance and policy fallback.

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Route-group misclassification harms SEO | Medium | High | Explicit route tests + documented allowlists |
| Admin misconfiguration creates confusing behavior | Medium | Medium | Preset-first UX + help text + validation |
| Runtime policy payload drift across layers | Medium | High | Shared DTO contract + integration tests |
| Mid-session policy changes create inconsistent tabs | Medium | Medium | Define apply-on-next-navigation behavior |

## Effort Summary

Total estimate: **29-39 hours** across five phases.

## Potential Risks & Unknowns

The biggest unknown is long-term route-group governance as new modules are introduced; without strict classification ownership, SEO and onboarding behavior can drift silently. Another unknown is policy change timing semantics across active tabs and cached app state, which requires explicit contract definition (immediate apply vs next navigation) before implementation. Finally, advanced policy flexibility can become configuration debt if not constrained with clear precedence rules and practical defaults.
