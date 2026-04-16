# Split InstanceOnboardingController → InstanceSettingsController

## Problem Statement

`InstanceOnboardingController` now hosts ongoing-settings endpoints (analytics-governance, cookie-consent-governance) that aren't really "onboarding". The localization governance endpoint was placed on `LocalizationAdminController` instead (decision D17) to avoid making this worse. The controller name is misleading for ongoing admin operations.

## Current State

- `InstanceOnboardingController` at `Explore.API/Controllers/`
- Hosts both one-time setup endpoints AND ongoing settings endpoints
- `LocalizationAdminController` hosts localization governance separately (D17)
- Analytics governance sits on `InstanceOnboardingController`

## Proposed Split

1. **`InstanceSetupController`** — one-time onboarding operations (initial tenant setup, first-run configuration)
2. **`InstanceSettingsController`** — ongoing admin settings (analytics governance, appearance governance, any future governance endpoints)
3. **`LocalizationAdminController`** — remains separate (already cohesive)

## Migration Plan

1. Create `InstanceSettingsController` with the governance endpoints
2. Add route aliases on old controller for one release cycle
3. Update Blazor client service endpoints
4. Remove old routes from `InstanceOnboardingController`

## Acceptance Criteria

- [ ] New controller exists with moved endpoints
- [ ] Swagger docs reflect the new routes
- [ ] Client services updated
- [ ] Old routes deprecated/removed after transition
