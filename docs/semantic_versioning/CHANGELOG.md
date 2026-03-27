ABOUTME: Semantic version index for implemented and planned platform versions.
ABOUTME: v0.1.0 implemented scope mirrors API WORK ITEMS.md and v1.0.0 planned scope mirrors API WORK ITEMS TODO.md.

# Changelog — ISLAMU Event Platform

> All notable changes are documented in version files.
> This project follows [Semantic Versioning 2.0.0](https://semver.org/).

**Last Updated:** 2026-03-27

---

## [Unreleased]

Features merged to `develop` branch, not yet released:

- **Outbox pattern** — transactional outbox with `OutboxProcessor` background service, exponential backoff retry, dead-letter queue, optimistic concurrency. Specialized variants: `PdsSyncOutbox`, `PolicyChangeOutbox`.
- **Footer management** — tenant-configurable footer with link groups, social links, 4 templates (standard-3-col, standard-2-col, minimal, community), instance governance locking. 11 API endpoints + admin UI.
- **Design system** — CSS `@layer` architecture (6 layers), 3-tier design token system, MudBlazor wrapper components (AppButton, AppCard, AppTextField, AppIconButton, AppDialogShell), `DialogOptionsFactory` presets, MudBlazor override whitelist policy.
- **Accessibility services** — `IAccessibilityAnnouncerService` (ARIA live regions), `IAccessibilityFocusService` (focus management with save/restore), JS interop module, `MainLayout` page shell with skip-link and landmarks, architecture convention tests (8 tests).
- **Secrets library** — `Explore.Secrets` multi-provider secret management (Environment, Infisical; Vault/Azure/AWS planned). Background refresh with exponential backoff, AES-256-GCM encryption, health checks, Prometheus metrics.
- **Actor appearance** — BackgroundColor, BackgroundEffect, BannerColor, BannerPictureId, BackgroundImageId fields. `AppearanceStyleBuilder` for inline CSS generation with overlay effects.
- **Analytics relay rate limiting** — dedicated `AnalyticsRelay` rate limit policy for `POST /api/a/t`.
- **Authorization parity tests** — architecture tests ensuring resource kinds map to Cerbos policies with fallback cases.

---

## Version History

| Version | Title | Released | Status |
|---------|-------|----------|--------|
| [v0.1.0](v0.1.0.md) | First Public Release (Beta) | TBD | 🚀 **CURRENT** |
| [v1.0.0](v1.0.0.md) | Planned Stable Release | TBD | ⏳ **PLANNED** |

---

## Versioning Policy

### Current Phase: Pre-1.0 Beta (0.1.0)

- **Major version zero (0.y.z)** indicates initial development.
- **Implemented baseline for v0.1.0** is tracked in `v0.1.0.md` under **Implemented (v0.1.0)**.
- **Planned scope for v1.0.0** is tracked in `v1.0.0.md` under **Planned (v1.0.0)**.

### Progression to 1.0.0

The project will move to **v1.0.0** after planned backlog completion and release validation.

---

## Feature Checklist Mapping

### ✅ Implemented in v0.1.0

- Source of truth: [v0.1.0.md](v0.1.0.md)
- Implemented section mirrors: `API WORK ITEMS.md`

### ⏳ Planned for v1.0.0

- Source of truth: [v1.0.0.md](v1.0.0.md)
- Planned section mirrors: `API WORK ITEMS TODO.md`

---

## Notes

- All currently implemented baseline work is classified under `v0.1.0`.
- All enterprise-next backlog items are classified under planned `v1.0.0`.
