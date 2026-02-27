ABOUTME: Defines runtime Blazor render-policy behavior from governance settings to route-level decisions.
ABOUTME: Documents current presets, route groups, defaults, and onboarding guardrails exactly as implemented.

# Render Policies

## Where Policy Is Resolved

Client runtime resolver:

- `Explore.Blazor.Client.Services.RuntimeRenderPolicyService`

Data source:

- `PublicExperienceSettingsDto` from API (`GetPublicExperienceSettingsQueryHandler`)
- values originate from `InstanceGovernanceSettingService.ReadSettingsAsync()`

## Route Groups

`RuntimeRenderPolicyService.ClassifyRouteGroup` uses:

- `Onboarding`: `/setup`, `/startup`, `/onboarding/*`
- `Admin`: `/admin/*`
- `PublicSeo`: `/`, `/events`, `/welcome`, `/home`, `/event/detail/*`
- `Operational`: all other routes

## Policy Keys

Stored in `SystemSetting` with `routing.render_policy.*` keys:

- `routing.render_policy.version`
- `routing.render_policy.preset`
- `routing.render_policy.advanced_enabled`
- `routing.render_policy.onboarding.disallow_interactive_server`
- `routing.render_policy.global.render_mode`
- `routing.render_policy.global.prerender_enabled`
- `routing.render_policy.public_seo.render_mode`
- `routing.render_policy.public_seo.prerender_enabled`
- `routing.render_policy.operational.render_mode`
- `routing.render_policy.operational.prerender_enabled`
- `routing.render_policy.admin.render_mode`
- `routing.render_policy.admin.prerender_enabled`
- `routing.render_policy.onboarding.render_mode`
- `routing.render_policy.onboarding.prerender_enabled`

## Presets (Normalization Rules)

`InstanceGovernanceSettingService` applies preset normalization:

- `SeoBalanced`:
  - advanced overrides disabled
  - global mode = `InteractiveAuto`
  - global prerender = `false`
  - public-seo prerender = `true`
- `AllPrerendered`:
  - advanced overrides disabled
  - global prerender = `true`
- `AllInteractiveAutoNoPrerender`:
  - advanced overrides disabled
  - global mode = `InteractiveAuto`
  - global prerender = `false`
- `CustomAdvanced`:
  - advanced overrides enabled

When advanced overrides are disabled, route-group mode/prerender values are aligned to global values (with SeoBalanced public-seo prerender exception).

## Onboarding Guardrail (Important)

Two enforcement behaviors exist:

1. governance validation/normalization prevents storing `InteractiveServer` as onboarding configured mode,
2. runtime resolver still forces onboarding routes to `InteractiveServer` for actual rendering.

This means onboarding always runs `InteractiveServer` at runtime, regardless of stored onboarding render-mode value.

## Runtime Fallback Defaults

If settings cannot be loaded, `RuntimeRenderPolicyService` defaults to:

- `PublicSeo`: `InteractiveServer`, prerender `true`
- all other groups: `InteractiveServer`, prerender `false`

## Allowed Render Modes

Enum-backed render modes:

- `InteractiveAuto`
- `InteractiveWebAssembly`
- `InteractiveServer`

## Related

- [BLAZOR.md](BLAZOR.md)
- [CONFIGURATION.md](CONFIGURATION.md)
- [OPERATIONS.md](OPERATIONS.md)
