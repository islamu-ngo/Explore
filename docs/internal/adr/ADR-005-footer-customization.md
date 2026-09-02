ABOUTME: Decision record for the tenant-customizable footer system with governance locking.
ABOUTME: Covers template-based rendering, link group management, and instance admin controls.

# ADR-005: Footer Customization System

- **Status:** Accepted
- **Date:** 2026-02
- **Deciders:** Core team

## Context

Multi-tenant deployments need per-tenant footer customization (branding, legal links, social media), but instance administrators must be able to enforce consistency across tenants for compliance or branding reasons. A static footer cannot serve both needs.

## Decision

Adopt a template-based footer system with governance locking:

### Templates

Four footer templates, selectable per tenant via `footer.template` setting:

| Template | Layout | Use Case |
|---|---|---|
| `standard-3-col` | Brand column (1.5fr) + dynamic link group columns (1fr each) | Default, most content |
| `standard-2-col` | Brand + single link group column | Simpler layouts |
| `minimal` | Single row with inline links | Landing pages |
| `community` | Social cards grid + link groups | Community-focused tenants |

All templates receive the same parameters from `Footer.razor` via `PublicExperienceService`.

### Data Model

- **TenantFooterLinkGroup** — ordered groups with title and active flag. Tenant-scoped.
- **TenantFooterLink** — individual links within a group (label, URL, open-in-new-tab, order, active).
- **Footer settings** — 8 `footer.*` keys in `app_settings` (enabled, template, show_description, description_text, show_social_links, social_links JSON, copyright_text, show_cookie_settings_link).

### Governance Locking

Instance administrators can lock specific footer aspects via `footer.lock_*` settings:

| Lock Setting | Effect |
|---|---|
| `footer.lock_tenant_template` | Tenant cannot change template |
| `footer.lock_tenant_link_groups` | Tenant cannot modify link groups |
| `footer.lock_tenant_social_links` | Tenant cannot change social links |
| `footer.lock_tenant_description` | Tenant cannot change footer description |
| `footer.lock_tenant_copyright` | Tenant cannot change copyright text |

Lock settings are silently ignored in single-tenant mode (`BlockInSingleTenant` pattern).

### Social Platforms

10 supported platforms: facebook, twitter, instagram, linkedin, youtube, tiktok, bluesky, whatsapp, telegram, github. `FooterIconHelper` maps platform names to MudBlazor icons.

## Consequences

1. Tenants get meaningful footer customization without code changes.
2. Instance admins maintain brand/legal compliance through locks.
3. Template additions require both a Razor component and a `PublicExperienceService` integration.
4. Social links are stored as JSON in settings, not as normalized database entities.
5. The 11-endpoint API surface requires careful authorization testing.

## Related

- [FOOTER_MANAGEMENT.md](../FOOTER_MANAGEMENT.md) — full footer system reference
- [ARCHITECTURE.md](../ARCHITECTURE.md) — system architecture
- [MULTI_TENANCY.md](../MULTI_TENANCY.md) — tenant isolation
