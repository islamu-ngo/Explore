# Runtime Render Policy Governance - Task Checklist

**Last Updated: 2026-02-23**

## Phase 0 - Planning Baseline ✅ COMPLETE

- [x] Capture user constraints and non-goals (planning only)
- [x] Verify key files/class entry points exist
- [x] Collect external reference baseline (Tavily + Context7)
- [x] Produce plan/context/tasks docs under `dev/active/runtime-render-policy-governance/`

## Phase 1 - Governance Contract (Domain/Application) ✅ COMPLETE

- [x] Add render policy setting keys in `Explore.Domain/Constants/GovernanceSettingKeys.cs`
  - Acceptance: Keys are additive, named consistently, and documented
- [x] Extend `Explore.Application/DTOs/Onboarding/InstanceGovernanceSettingsDto.cs`
  - Acceptance: DTO supports preset selection and advanced overrides
- [x] Add policy validation service/rules in application layer
  - Acceptance: Invalid combinations fail fast with clear error messages

## Phase 2 - Runtime Resolver and App Integration ✅ COMPLETE

- [x] Implement render policy resolver service (route-group -> mode/prerender)
  - Acceptance: Deterministic output for all known route groups
- [x] Integrate resolver into app render boundary in `Explore.Blazor/Components/App.razor`
  - Acceptance: Existing behavior preserved when no custom policy exists
- [x] Implement fallback handling for missing/corrupt settings
  - Acceptance: No runtime crash or blank-state regression
- [x] Extend anonymous-safe public settings payload with render policy fields
  - Acceptance: Runtime resolver can load governance policy without admin-only endpoint access

## Phase 3 - Onboarding Guardrails ✅ COMPLETE

- [x] Define onboarding route group explicitly
  - Acceptance: `/setup`, `/startup`, `/onboarding/instance`, `/onboarding/tenant` covered
- [x] Enforce invariant: onboarding cannot use Interactive Server
  - Acceptance: Validation rejects policy updates violating this rule
- [x] Add logging/telemetry signal for rejected invalid onboarding policy updates
  - Acceptance: Operational logs explain rejection reason

## Phase 4 - Instance Settings UX ✅ COMPLETE

- [x] Add policy preset cards and advanced options in `Explore.Blazor.Client/Components/Admin/Instance/InstanceGovernanceSection.razor`
  - Acceptance: Preset-first UX with advanced expansion
- [x] Add info icon and hover help for each option
  - Acceptance: Hover/focus shows clear, concise guidance
- [x] Preselect recommended option by default
  - Acceptance: Recommended preset selected on initial load
- [x] Add distinct border styling for recommended preselected option
  - Acceptance: Selection is obvious and accessible (not color-only)

## Phase 5 - Verification and Documentation ✅ COMPLETE

- [x] Application unit tests for resolver, presets, and invalid combinations
  - Acceptance: Includes onboarding Interactive Server denial test
- [x] API integration tests for save/retrieve/reject policy flows
  - Acceptance: Authz and validation paths covered
- [x] Blazor tests for recommended preselection and hover help behavior
  - Acceptance: UI state assertions stable
- [x] Update `docs/RENDER_POLICIES.md` and governance references
  - Acceptance: Documentation reflects runtime policy model and guardrails

## Cross-Cutting Checks

- [x] Authorization: write operations restricted to instance admin authority
- [x] Migration safety: additive settings only; no destructive transition
- [x] Rollout plan: define policy application timing for active sessions/tabs

## Summary

| Phase | Status | Effort |
|------|--------|--------|
| Phase 0 | ✅ Complete | S |
| Phase 1 | ✅ Complete | M |
| Phase 2 | ✅ Complete | M |
| Phase 3 | ✅ Complete | S |
| Phase 4 | ✅ Complete | M |
| Phase 5 | ✅ Complete | M |
| Total | Phases 1-5 delivered | 29-39h |

---

## Session Checkpoint (2026-02-27 Europe/Brussels)

- [x] Reviewed task continuity status for context reset handoff.
- [ ] Resume implementation work from this task latest documented in-progress section.
- [ ] Re-validate with build/tests once implementation resumes.

