ABOUTME: Task-focused administration guide for instance, tenant, organization, and group operators.
ABOUTME: Maps implemented admin UI surfaces to roles, entry points, dangerous operations, and recovery notes.

# Admin Guide

> **Audience:** Admins | Operators | Contributors
> **Status:** Mixed
> **Owner:** Product/Admin
> **Last Verified:** 2026-05-06
> **Source Anchors:** `Explore.Blazor.Client/Pages/Admin/`, `docs/ADMIN_HIERARCHY.md`, `docs/AUTHORIZATION.md`, `docs/AUTHORIZATION_PATTERNS.md`

## Scope

This guide documents implemented administration workflows in the Blazor admin UI. It complements [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md), which defines authority boundaries, and [AUTHORIZATION_PATTERNS.md](AUTHORIZATION_PATTERNS.md), which defines enforcement patterns.

Each workflow below states the required role, the UI entry point, and the recovery note for risky changes. If a section is labeled planned or backend-pending, do not treat it as an operator contract.

## Admin Authority Model

| Scope | Typical role | UI entry point | Boundary |
|---|---|---|---|
| Instance | Instance administrator | `/admin/instance/settings` | Platform-wide settings, tenant management, platform API keys, global provider settings. |
| Tenant | Tenant administrator | `/admin/tenant/settings` and tenant admin sub-pages | Tenant policies, public experience, lookup tables, tenant API keys, navigation, footer, templates, custom properties. |
| Organization | Organization administrator | `/admin/organization/{OrganizationId}/settings` | Organization profile, members, verification state, organization API keys. |
| Group | Group administrator | `/admin/group/{GroupId}/settings` | Group profile, branding, members, group API keys. |

Admin pages require authentication. Editability is still checked at runtime by role helpers and admin-state services, so a visible page does not imply write permission.

## Instance Administration

**Required role:** Instance administrator.

**UI entry point:** `/admin/instance/settings`.

Instance settings are the platform-control surface. Use them for:

- Governance and render policy defaults.
- Authentication provider configuration.
- Module, domain, branding, and localization settings.
- Platform S3 storage and SMTP settings.
- Analytics and privacy settings.
- Footer governance and platform API keys.
- Tenant management in multi-tenant deployments.

### Dangerous Operations

| Operation | Risk | Recovery note |
|---|---|---|
| Delete tenant | Permanent tenant removal if exposed by the current UI path. | Confirm backups and export tenant-critical data before deleting. Use [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) for restore/rollback planning. |
| Revoke platform API key | Direct callers using the key fail immediately. | Create and distribute a replacement key before revocation when possible. |
| Change auth provider settings | Administrators can lock themselves out if authority/client settings are wrong. | Keep a known-good configuration snapshot from [SECRETS.md](SECRETS.md) and [CONFIGURATION.md](CONFIGURATION.md). |
| Switch Cerbos/local authorization provider | Cerbos mode is fail-closed when the external provider is unavailable. | Switch back to local authorization if Cerbos availability breaks production authorization. |

### Planned Or Backend-Pending Controls

- Webhook administration is planned, not implemented as an operator contract.
- Tenant-members management under the instance surface has UI scaffolding, but backend completion is pending in the explored admin source.

## Tenant Administration

**Required role:** Tenant administrator.

**UI entry points:** `/admin/tenant/settings`, `/admin/tenant/footer`, `/admin/tenant/navigation`, `/admin/tenant/event-templates`, `/admin/tenant/custom-properties`, and `/admin/tenant/custom-property-definitions`.

Tenant administration controls tenant-level policy and public experience:

- Tenant policies and render-policy choices.
- Announcement bar and public-experience settings.
- Organization approval settings.
- Lookup tables for categories, tags, and locations.
- Navigation links and tenant footer.
- Tenant API keys.
- Event templates, template details, and custom property definitions.

### SEO And Public Discovery

There is no standalone SEO administration page. Public discovery is configured through tenant render-policy settings and public-experience settings, including discovery-centric or organization-centric behavior, home-block JSON, call-to-action JSON, and event-section presets.

### Dangerous Operations

| Operation | Risk | Recovery note |
|---|---|---|
| Delete lookup values | Category, tag, or location deletes can remove operator-facing classification data. | Export or document the value before deleting; prefer disabling/hiding if the UI offers a safer path. |
| Change public render policy | Public routes may change render mode or visibility. | Verify public pages and crawler-facing routes after the change. |
| Revoke tenant API key | Integrations for that tenant stop authenticating. | Rotate by creating the replacement key first, then revoke the old key. |
| Edit templates or custom properties | Event creation and downstream projections can change. | Review template sync conflicts and custom-property promotion reports before applying broad changes. |

### Planned Tenant Overrides

Tenant-specific SMTP, storage, and analytics override controls are future work in the explored UI. Platform-level settings remain the current operational source for those providers.

## Organization Administration

**Required role:** Organization administrator, or a higher administrator with organization-management permission.

**UI entry point:** `/admin/organization/{OrganizationId}/settings`.

Organization administration covers:

- Organization profile settings.
- Organization members.
- Verification status review.
- Organization-scoped API keys.

Member changes are gated by runtime role helpers. Verification is exposed as status-oriented administration in the explored UI; do not assume arbitrary verification mutation unless the source path explicitly exposes it.

## Group Administration

**Required role:** Group administrator, or a higher administrator with group-management permission.

**UI entry point:** `/admin/group/{GroupId}/settings`.

Group administration covers:

- Group profile and branding.
- Group members.
- Group-scoped API keys.

Member changes are gated by `RoleHelper.CanManageGroup(...)` in the admin UI source.

## Template Administration

**Required role:** Tenant administrator.

**UI entry points:** `/admin/tenant/event-templates`, event-template detail pages, `/admin/events/{eventId}/template-sync`, and `/admin/event-sessions/{sessionId}/template-sync`.

Use template administration to manage event templates and apply template changes to existing events or sessions. Template sync supports diff, apply, and history flows. If the sync base is stale or another update wins the race, reload the diff before applying a new plan.

Dangerous operation: applying a sync plan can update event or session content. Treat the visible diff as the approval record and keep a rollback path through backup/restore procedures for bulk changes.

## Custom Property Administration

**Required role:** Tenant administrator.

**UI entry points:** `/admin/tenant/custom-properties` and `/admin/tenant/custom-property-definitions`.

Custom-property administration includes exposure, search/filter/export governance, moderation/analytics flags, bulk edit, promotion reports, projection rebuild, and definition create/edit/delete flows.

Dangerous operation: deleting or broadly changing a property definition can affect projections and operator-facing forms. Normal delete retires and soft-deletes definitions, options, and values. Permanent purge is a separate admin-only operation for dependency-free definitions; it writes an audit summary and is blocked when historical values, projection rows, audit references, or template-sync provenance exist. Review projection status and drain/rebuild controls before high-volume changes.

## Storage, SMTP, Localization, And Analytics Administration

| Area | Current admin scope | Recovery note |
|---|---|---|
| Storage | Platform S3 storage settings are managed from instance administration. | Verify keys against [CONFIGURATION.md](CONFIGURATION.md) and [SECRETS.md](SECRETS.md); confirm backups in [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md). |
| SMTP | Platform SMTP settings are managed from instance administration. | Keep provider credentials in the configured secret provider; test mail after changes. |
| Localization | Instance localization supports offline/emergency controls and bundle export. | Use forced offline mode or reset-to-offline behavior if remote localization becomes unsafe. |
| Analytics/privacy | Instance analytics and privacy settings are platform-level controls. | Verify data sharing and retention expectations before enabling integrations. |

## Related Documentation

- [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md) — admin scope and authority boundaries.
- [AUTHORIZATION.md](AUTHORIZATION.md) — authorization model.
- [AUTHORIZATION_PATTERNS.md](AUTHORIZATION_PATTERNS.md) — implementation enforcement patterns.
- [API_COOKBOOK.md](API_COOKBOOK.md) — direct caller and API-key integration guidance.
- [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) — recovery planning for risky operational changes.
- [CUSTOM_PROPERTIES.md](CUSTOM_PROPERTIES.md) — custom-property lifecycle, projection, exposure, and purge boundaries.
