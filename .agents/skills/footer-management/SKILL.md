---
name: footer-management
description: Footer customization system with templates, governance locking, and link management.
type: domain
enforcement: suggest
priority: medium
---

ABOUTME: Footer management skill covering templates, governance, and CQRS endpoints.
ABOUTME: Enforces governance locking, template selection, and footer data model patterns.

# Footer Management

## Keywords

footer, footer template, footer link, footer settings, governance, lock, social links, link group, footer admin

## File Patterns

- `**/FooterController.cs`
- `**/FooterAdminService.cs`
- `**/PublicExperienceService.cs`
- `**/TenantFooterLink*.cs`
- `**/Footer*.razor`
- `**/FooterSetting*.cs`

## Non-Inferable Rules

1. Footer templates are server-rendered Blazor components (standard-3-col, standard-2-col, minimal, community). All receive the same parameter set from `Footer.razor` via `PublicExperienceService`.
2. Instance admins can lock specific footer aspects (template, link groups, social links, description, copyright) to prevent tenant overrides. Locks use `footer.lock_tenant_*` settings.
3. Footer settings use the `AppSetting` system with `footer.*` keys — not separate config tables.
4. Social platform list is hardcoded (10 platforms). `FooterIconHelper` maps platform names to MudBlazor icons.
5. Link groups support reordering via dedicated `POST /api/footer/link-groups/reorder` endpoint.
6. Governance locking is silently ignored in single-tenant mode (shown as info alert in admin UI).
7. `GET /api/footer/config` is `[AllowAnonymous]` (public-facing). All other footer endpoints require `[Authorize]`.

## Activation

Load this skill when:
- Modifying footer templates or settings
- Working on footer admin UI pages
- Implementing footer governance features
- Adding or changing footer API endpoints

## Resources

- `resources/data-model.md` — Entities, settings keys, templates, social platforms
- `resources/api-endpoints.md` — 11 endpoints, CQRS commands/queries
- `resources/governance.md` — Instance locking, tenant override rules

## Related

- `docs/FOOTER_MANAGEMENT.md`
- `.Codex/skills/blazor-ui-conventions/SKILL.md`
- `.Codex/skills/cqrs-mediatr-guidelines/SKILL.md`
