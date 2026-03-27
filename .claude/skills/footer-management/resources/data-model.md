ABOUTME: Footer data model covering entities, settings keys, templates, and social platforms.
ABOUTME: Reference for TenantFooterLinkGroup, TenantFooterLink, and footer.* settings.

# Footer Data Model

## Entities

### TenantFooterLinkGroup

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| TenantId | Guid? | Nullable for instance-level |
| Title | string | Group heading text |
| Order | int | Display order |
| IsActive | bool | Soft visibility toggle |
| Links | nav | Collection of TenantFooterLink |
| CreatedAt, CreatedBy, UpdatedAt, UpdatedBy | audit | Standard auditing |

### TenantFooterLink

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| FooterLinkGroupId | Guid | FK to TenantFooterLinkGroup |
| Label | string | Display text |
| Url | string | Target URL |
| OpenInNewTab | bool | Default false |
| Order | int | Display order within group |
| IsActive | bool | Soft visibility toggle |
| CreatedAt, CreatedBy, UpdatedAt, UpdatedBy | audit | Standard auditing |

## Footer Settings (AppSetting Keys)

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| footer.enabled | bool | true | Master switch |
| footer.template | string | "standard-3-col" | Active template name |
| footer.show_description | bool | true | Show org/brand description |
| footer.description_text | string | "" | Description content |
| footer.show_social_links | bool | true | Show social media links |
| footer.social_links | JSON | [] | Array of {platform, url} objects |
| footer.copyright_text | string | "" | Copyright line |
| footer.show_cookie_settings_link | bool | true | Cookie preferences link |

## Templates

| Template | Layout | Description |
|----------|--------|-------------|
| standard-3-col | Brand (1.5fr) + dynamic link groups (1fr each) | Default, full-featured |
| standard-2-col | Brand + single link column | Simpler layout |
| minimal | Single row | Compact footer |
| community | Social cards + link groups | Community-focused |

All templates receive the same parameters from `Footer.razor` via `PublicExperienceService.GetFooterDataAsync()`.

## Social Platforms (Hardcoded)

facebook, twitter, instagram, linkedin, youtube, tiktok, bluesky, whatsapp, telegram, github

`FooterIconHelper` maps each platform name to the corresponding MudBlazor icon constant.

## Related

- `resources/api-endpoints.md` — CQRS operations on these entities
- `resources/governance.md` — locking rules
