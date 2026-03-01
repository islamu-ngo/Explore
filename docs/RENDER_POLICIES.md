ABOUTME: Defines runtime Blazor render-policy behavior from governance settings to route-level decisions.
ABOUTME: Documents current presets, route groups, defaults, and onboarding guardrails exactly as implemented.

# Render Policies

## Where Policy Is Resolved

Client runtime resolver:

- `Explore.Blazor.Client.Services.RuntimeRenderPolicyService`

Data source:

- `PublicExperienceSettingsDto` from API (`GetPublicExperienceSettingsQueryHandler`)
- values originate from `InstanceGovernanceSettingService.ReadEffectiveSettingsForTenantAsync(tenantId)`

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

- `AllInteractiveServer` (**default**):
  - advanced overrides disabled
  - global mode = `InteractiveServer`
  - global prerender = `false`
  - all route groups inherit global (InteractiveServer, no prerender)
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

1. governance normalization converts onboarding mode to `InteractiveAuto` when `InteractiveServer` is submitted (prevents storing InteractiveServer as the configured onboarding mode),
2. runtime resolver forces onboarding routes to `InteractiveServer` for actual rendering regardless of stored value.

The validator does **not** reject `InteractiveServer` for onboarding — normalization handles it silently. This means onboarding always runs `InteractiveServer` at runtime.

## Runtime Fallback Defaults

If settings cannot be loaded, `RuntimeRenderPolicyService` defaults to:

- `PublicSeo`: `InteractiveServer`, prerender `true`
- all other groups: `InteractiveServer`, prerender `false`

## Allowed Render Modes

Enum-backed render modes:

- `InteractiveAuto`
- `InteractiveWebAssembly`
- `InteractiveServer`

## Per-Tenant Render Policy Delegation

Instance admins can delegate render policy control to tenants via governance settings.

### Delegation Keys

Stored in `SystemSetting` with `routing.render_policy.*` keys:

- `routing.render_policy.allow_tenant_override` — master gate (must be `true` for any tenant override)
- `routing.render_policy.lock_tenant_public_seo` — locks public/SEO route group
- `routing.render_policy.lock_tenant_operational` — locks operational route group
- `routing.render_policy.lock_tenant_admin` — locks admin route group

Onboarding is always instance-controlled (no tenant override, hardcoded guardrail).

### Cascade Resolution

`ReadEffectiveSettingsForTenantAsync(tenantId)`:

1. Reads instance settings via `ReadSettingsAsync()`.
2. If `AllowTenantRenderPolicyOverride` is `false`, returns instance settings unchanged.
3. Overlays tenant preset, advanced-enabled, global mode, and global prerender overrides.
4. Runs `NormalizeRenderPolicySettings` (applies preset defaults, aligns non-advanced to global).
5. Overlays per-route-group tenant overrides only for unlocked groups (after normalization so they aren't clobbered).

### Lock Enforcement

Two enforcement layers:

1. **Service layer**: `TenantPolicySettingService` silently removes tenant overrides for locked groups during write.
2. **Handler layer**: `UpdateTenantPolicySettingsCommandHandler.EnsureLockedSettingsAreNotModifiedAsync` returns explicit validation failure if locked fields are changed.

### Tenant Admin UI

`TenantRenderPolicySection.razor` shows:
- Preset selector (all presets including CustomAdvanced)
- Per-route-group override panels with lock awareness (disabled when locked)
- `CanOverride*` flags computed from system lock keys

### `GetPublicExperienceSettingsQueryHandler`

Uses `ReadEffectiveSettingsForTenantAsync(tenantId)` — public experience always resolves tenant-specific render policy.

## Related

- [BLAZOR.md](BLAZOR.md)
- [CONFIGURATION.md](CONFIGURATION.md)
- [OPERATIONS.md](OPERATIONS.md)
- [MULTI_TENANCY.md](MULTI_TENANCY.md)
