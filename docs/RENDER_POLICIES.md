# Render Policies

> Runtime-governed Blazor render strategy for instance operations.

**Last Updated**: February 2026

---

## Overview

The platform uses a runtime render-policy model driven by instance governance settings. Instead of hardcoding render modes in page components, the app resolves mode and prerender behavior at runtime per route group.

This keeps rendering strategy configurable without redeploying code and allows instance admins to tune SEO and interactivity tradeoffs safely.

---

## Governance Model

Render policy is configured in instance governance settings with two layers:

1. Preset selection (recommended default path)
2. Optional advanced route-group overrides

### Presets

| Preset | Purpose | Default Behavior |
|------|---------|------------------|
| `SeoBalanced` | Recommended default | `InteractiveAuto` + prerender off globally, with public SEO prerender on |
| `AllPrerendered` | Maximum crawler-first HTML output | `InteractiveAuto` + prerender on for all route groups |
| `AllInteractiveAutoNoPrerender` | Fast interactive startup | `InteractiveAuto` + prerender off for all route groups |
| `CustomAdvanced` | Fine-grained control | Enables explicit per-route-group mode/prerender controls |

### Route Groups

| Route Group | Intent |
|------------|--------|
| `public-seo` | Public listing/detail routes where SEO and prerendering matter most |
| `operational` | Authenticated workflows and day-to-day interaction surfaces |
| `admin` | Administrative routes and control panels |
| `onboarding` | Setup/startup/onboarding flows |

---

## Onboarding Guardrail (Invariant)

Onboarding routes must never run in `InteractiveServer` mode.

Enforcement is layered:

- Application validation rejects invalid onboarding mode combinations
- Command handler emits warning telemetry for rejected onboarding violations
- Runtime resolver normalizes/guards onboarding behavior
- Admin UI advanced selector excludes `InteractiveServer` for onboarding

Covered onboarding routes include:

- `/setup`
- `/startup`
- `/onboarding/instance`
- `/onboarding/tenant`

---

## Runtime Resolution Flow

1. App receives current path.
2. Route-group classifier maps path to a route group.
3. Policy resolver loads public-safe governance render settings.
4. Resolver outputs:
   - render mode (`InteractiveAuto`, `InteractiveWebAssembly`, `InteractiveServer`)
   - prerender enabled (`true`/`false`)
5. App applies resolved policy to `HeadOutlet` and `Routes` render boundaries.

Fallback behavior is mandatory: malformed or missing settings must resolve to safe defaults and never crash startup.

---

## Instance Admin UX

Instance settings use a preset-first UX in `InstanceGovernanceSection`:

- preset cards first
- recommended preset preselected by default
- recommended option visually emphasized
- info icon tooltips for strategy guidance
- advanced panel for global fallback + route-group overrides

The default recommendation is `SeoBalanced`.

---

## Configuration Fields

The runtime policy payload includes:

- `RenderPolicyPreset`
- `EnableAdvancedRenderPolicyOverrides`
- global fallback:
  - `GlobalRenderMode`
  - `GlobalPrerenderEnabled`
- per route group mode/prerender fields:
  - `PublicSeoRenderMode`, `PublicSeoPrerenderEnabled`
  - `OperationalRenderMode`, `OperationalPrerenderEnabled`
  - `AdminRenderMode`, `AdminPrerenderEnabled`
  - `OnboardingRenderMode`, `OnboardingPrerenderEnabled`
- guardrail flag:
  - `DisallowInteractiveServerOnOnboarding`

---

## Operational Notes

- Governance writes are instance-admin controlled.
- Validation is server-side; UI guidance is assistive, not authoritative.
- Policy changes are runtime-applied through settings resolution.

### Rollout Timing Contract

- Policy updates apply on the next server round-trip for each tab/session.
- Open tabs keep their current render boundary until they navigate or perform a full reload.
- There is no cross-tab push/invalidation for render policy changes.
- Prerender and mode decisions are recomputed at request/navigation time from current governance settings.
- Operationally, admins should communicate that active users may need refresh/navigation to observe new policy behavior.

---

## Related Documentation

- `docs/BLAZOR.md`
- `docs/ARCHITECTURE.md`
- `docs/ADMIN_HIERARCHY.md`
- `dev/active/runtime-render-policy-governance/runtime-render-policy-governance-plan.md`
- `dev/active/runtime-render-policy-governance/runtime-render-policy-governance-context.md`
- `dev/active/runtime-render-policy-governance/runtime-render-policy-governance-tasks.md`
