# Split UserAppearancePreferences → UserPreferences

## Problem Statement

`UpdateUserAppearancePreferencesDto.Language` is semantically wrong — language is not "appearance". The field was added to the existing Appearance DTO/endpoint for v1 delivery speed, but the naming mismatch will cause confusion as more non-appearance preferences are added.

## Current State

- `PUT /api/user/appearance` accepts `{ ThemeMode, Direction, Language }`
- `UpdateUserAppearancePreferencesDto` in `Explore.Application/DTOs/Appearance/`
- `AppearanceSettingGroup` includes `Language` property
- BFF endpoint `POST /bff/language` calls the appearance API

## Proposed Solution

1. Create `UpdateUserPreferencesDto` (or `UpdateUserLanguagePreferencesDto`) with just `Language`
2. Create `PUT /api/user/preferences/language` endpoint (or extend existing)
3. Dual-write for one release: both old and new endpoints accept `Language`
4. Deprecate `Language` from `UpdateUserAppearancePreferencesDto`
5. Update BFF to call the new endpoint

## Migration Plan

1. Add new endpoint (additive, no breaking change)
2. Update Blazor client to use new endpoint
3. Keep old endpoint accepting `Language` for one release cycle (backward compat)
4. Remove `Language` from Appearance DTO in the next major version

## Acceptance Criteria

- [ ] New endpoint exists and is tested
- [ ] Blazor client uses new endpoint
- [ ] Old endpoint still works during transition
- [ ] Appearance DTO no longer contains `Language` after transition
