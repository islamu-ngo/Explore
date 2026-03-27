ABOUTME: Tracks known accessibility gaps, deferred work, and compliance exceptions.
ABOUTME: Review periodically — no items past target date without documented extension.

# Accessibility Debt Register

**Standard**: WCAG 2.2 Level AA
**Last Reviewed**: 2026-03-26

---

## Active Debt Items

### D-001: `aria-invalid` Not Set on Invalid Form Fields
- **WCAG**: 3.3.1 Error Identification (A)
- **Impact**: Low — MudBlazor shows red helper text visually, screen readers get `aria-required` and error text via `ValidationMessage`
- **Rationale**: MudBlazor v9 does not set `aria-invalid="true"` on invalid inputs. Would require custom wrapper component.
- **Workaround**: Error messages wrapped in `role="alert"` announce errors to screen readers.
- **Target**: MudBlazor v10 or custom `AppTextField` wrapper enhancement
- **Owner**: Blazor UI team

### D-002: Playwright + axe-core E2E Tests Not Implemented
- **WCAG**: Testing infrastructure (no specific criterion)
- **Impact**: Medium — automated browser-level WCAG scanning not available. Relying on bUnit + architecture tests + manual testing.
- **Rationale**: Requires full Aspire AppHost startup (DB, Keycloak, all services). No Playwright infrastructure exists.
- **Workaround**: bUnit tests for component markup, architecture tests for file conventions, manual NVDA testing.
- **Target**: When E2E test project is created
- **Owner**: QA / DevOps

### D-003: Loading States Missing `aria-busy`
- **WCAG**: 4.1.3 Status Messages (AA)
- **Impact**: Low — loading spinners are visual-only, but key pages announce completion via `AnnouncerService`.
- **Rationale**: MudProgressCircular/MudSkeleton don't support `aria-busy` parameter. Would need wrapper div.
- **Workaround**: `AnnouncerService.AnnouncePoliteAsync()` announces when loading completes.
- **Target**: Incremental — add `aria-busy` to wrapper containers in high-traffic pages
- **Owner**: Blazor UI team

### D-004: Not All Pages Have AnnouncerService Integration
- **WCAG**: 4.1.3 Status Messages (AA)
- **Impact**: Low — only EventList, EventDetail, GroupProfile have announcements. Other data-loading pages don't announce to screen readers.
- **Rationale**: Incremental rollout — started with highest-traffic pages.
- **Workaround**: Error messages use `role="alert"` (12 instances across 10 files).
- **Target**: Add to remaining pages during feature development
- **Owner**: Blazor UI team

### D-005: Pre-existing bUnit Test Failures (86)
- **WCAG**: Testing infrastructure
- **Impact**: None for accessibility — all failures from concurrent AppButton/AppIconButton migration (CaptureUnmatchedValues type mismatch).
- **Rationale**: Not caused by accessibility work. Tracked separately.
- **Target**: AppButton migration completion
- **Owner**: UI component migration team

### D-006: Direction Toggle UI Not Yet in Settings Page
- **WCAG**: 1.3.4 Orientation (AA)
- **Impact**: Low — backend API and BFF endpoints are complete (`POST /bff/direction`). UI toggle missing from SettingsPersonalInfo.
- **Rationale**: Phase 5 implemented full backend. UI pending.
- **Workaround**: Direction auto-detects from language. Can be set via API/cookie directly.
- **Target**: Next settings page iteration
- **Owner**: Blazor UI team

---

## Resolved Items

| ID | Description | Resolved | Resolution |
|----|-------------|----------|------------|
| — | No resolved items yet | — | — |

---

## Review Schedule

- **Monthly**: Review all active items, update target dates
- **Per Release**: Verify no critical (A-level) WCAG items past target date
- **New Features**: Check this register before marking features complete
