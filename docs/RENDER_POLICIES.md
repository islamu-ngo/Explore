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
| `SeoBalanced` | Recommended default | `InteractiveAuto` + prerender off globally, but `public-seo` prerender enabled |
| `AllPrerendered` | Maximum crawler-first HTML output | `InteractiveAuto` + prerender on for all route groups |
| `AllInteractiveAutoNoPrerender` | Fast interactive startup | `InteractiveAuto` + prerender off for all route groups |
| `CustomAdvanced` | Fine-grained control | Enables explicit per-route-group mode/prerender controls |

### Route Groups

| Route Group | Intent |
|------------|--------|
| `public-seo` | `/`, `/events`, `/welcome`, `/home`, `/event/detail/*` |
| `operational` | Everything else (authenticated or standard workflows) |
| `admin` | `/admin/*` |
| `onboarding` | `/setup`, `/startup`, `/onboarding/*` |

---

## Onboarding Guardrail (Invariant)

Onboarding routes are **forced to `InteractiveServer` at runtime** for instant interactivity, even though governance settings prevent administrators from selecting it.

Enforcement is layered:

- Application validation rejects onboarding render-mode settings that attempt to set `InteractiveServer`
- Settings normalization forces `DisallowInteractiveServerOnOnboarding = true`
- Runtime resolver overrides onboarding render mode to `InteractiveServer` regardless of configuration
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

## Governance Keys

Render policies are persisted in `SystemSetting` using the canonical keys from `GovernanceSettingKeys.Routing.RenderPolicy`:

| Setting Key | Purpose |
|-------------|---------|
| `routing.render_policy.version` | Render policy schema version (default `1`) |
| `routing.render_policy.preset` | Selected preset (`SeoBalanced`, `AllPrerendered`, `AllInteractiveAutoNoPrerender`, `CustomAdvanced`) |
| `routing.render_policy.advanced_enabled` | Enable route-group overrides |
| `routing.render_policy.onboarding.disallow_interactive_server` | Guardrail flag (always true) |
| `routing.render_policy.global.render_mode` | Global fallback render mode |
| `routing.render_policy.global.prerender_enabled` | Global fallback prerender flag |
| `routing.render_policy.public_seo.render_mode` | Public SEO render mode override |
| `routing.render_policy.public_seo.prerender_enabled` | Public SEO prerender override |
| `routing.render_policy.operational.render_mode` | Operational render mode override |
| `routing.render_policy.operational.prerender_enabled` | Operational prerender override |
| `routing.render_policy.admin.render_mode` | Admin render mode override |
| `routing.render_policy.admin.prerender_enabled` | Admin prerender override |
| `routing.render_policy.onboarding.render_mode` | Onboarding render mode override (validated to exclude `InteractiveServer`) |
| `routing.render_policy.onboarding.prerender_enabled` | Onboarding prerender override |

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
