ABOUTME: Footer customization system with admin UI, templates, governance, and CQRS endpoints.
ABOUTME: Covers link groups, social links, templates, instance governance locking, and settings resolution.

# Footer Management

Tenants customize their site footer through an admin interface. Instance admins can lock specific footer aspects to enforce branding consistency across tenants.

## Data Model

### TenantFooterLinkGroup

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `TenantId` | `Guid?` | Owning tenant (null for instance-level) |
| `Title` | `string` | Group heading displayed in footer |
| `Order` | `int` | Display order |
| `IsActive` | `bool` | Visibility toggle |
| `Links` | `ICollection` | Child links (navigation property) |
| Auditing | | `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` |

### TenantFooterLink

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Primary key |
| `FooterLinkGroupId` | `Guid` | Parent group FK |
| `Label` | `string` | Display text |
| `Url` | `string` | Link target |
| `OpenInNewTab` | `bool` | Target behavior |
| `Order` | `int` | Display order within group |
| `IsActive` | `bool` | Visibility toggle |
| Auditing | | `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` |

## Settings

Footer behavior is controlled through the settings system (section: `FooterSettingDefinitions`):

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `footer.enabled` | `bool` | `true` | Show/hide footer |
| `footer.template` | `string` | `standard-3-col` | Active template |
| `footer.show_description` | `bool` | `true` | Show brand description |
| `footer.description_text` | `string` | — | Brand description content |
| `footer.show_social_links` | `bool` | `true` | Show social media icons |
| `footer.social_links` | `JSON` | `[]` | Social platform entries |
| `footer.copyright_text` | `string` | — | Copyright line |
| `footer.show_cookie_settings_link` | `bool` | `true` | Cookie consent link |

## Templates

| Template | Layout | Use Case |
|----------|--------|----------|
| `standard-3-col` | Brand (1.5fr) + dynamic link groups (1fr each) | Default, full-featured |
| `standard-2-col` | Brand + single link column | Simpler layouts |
| `minimal` | Single row, horizontal links | Compact footers |
| `community` | Social cards + link groups | Community-focused sites |

All templates receive identical parameters from `Footer.razor` via `PublicExperienceService`. Template selection is a setting, not a code change.

## Social Platforms

Supported platforms with icon mapping via `FooterIconHelper`:

`facebook`, `twitter`, `instagram`, `linkedin`, `youtube`, `tiktok`, `bluesky`, `whatsapp`, `telegram`, `github`

Each maps to the corresponding MudBlazor icon constant.

## Governance (Instance Locking)

Instance admins can lock footer aspects to prevent tenant overrides:

| Lock Setting | What It Prevents |
|-------------|------------------|
| `footer.lock_tenant_template` | Tenants cannot change template |
| `footer.lock_tenant_link_groups` | Tenants cannot modify link groups |
| `footer.lock_tenant_social_links` | Tenants cannot change social links |
| `footer.lock_tenant_description` | Tenants cannot modify brand description |
| `footer.lock_tenant_copyright` | Tenants cannot change copyright text |

Governance is managed via `InstanceFooterGovernanceSection.razor` with toggle controls. In single-tenant mode, an info alert explains that governance has no effect.

## API Endpoints

### FooterController (11 endpoints)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/footer/config` | Anonymous | Public footer configuration |
| GET | `/api/footer/link-groups` | Authorize | List all link groups |
| GET | `/api/footer/link-groups/{id}` | Authorize | Get link group details |
| POST | `/api/footer/link-groups` | Authorize | Create link group |
| PUT | `/api/footer/link-groups/{id}` | Authorize | Update link group |
| DELETE | `/api/footer/link-groups/{id}` | Authorize | Delete link group |
| POST | `/api/footer/link-groups/reorder` | Authorize | Reorder link groups |
| POST | `/api/footer/link-groups/{groupId}/links` | Authorize | Create link in group |
| PUT | `/api/footer/links/{id}` | Authorize | Update link |
| DELETE | `/api/footer/links/{id}` | Authorize | Delete link |
| PUT | `/api/footer/settings` | Authorize | Update footer settings |

## CQRS Structure

| Type | Name | Description |
|------|------|-------------|
| Query | `GetFooterConfig` | Public config for rendering |
| Query | `GetLinkGroupList` | Admin link group listing |
| Query | `GetLinkGroupDetails` | Single group with links |
| Query | `GetGovernanceSettings` | Instance lock states |
| Command | `CreateLinkGroup` | New link group |
| Command | `UpdateLinkGroup` | Modify link group |
| Command | `DeleteLinkGroup` | Remove link group |
| Command | `ReorderLinkGroups` | Batch reorder |
| Command | `CreateLink` | New link in group |
| Command | `UpdateLink` | Modify link |
| Command | `DeleteLink` | Remove link |
| Command | `UpdateTenantSettings` | Tenant footer settings |
| Command | `UpdateGovernanceSettings` | Instance governance locks |

## Admin UI

`FooterSettings.razor` at `/admin/tenant/footer` provides:

- **General Settings** — template, description, copyright, toggles
- **Social Links** — inline editing with platform selector
- **Link Groups** — table with drag-to-reorder
- **Group Links** — nested link management per group

Dialogs: `FooterLinkDialog`, `FooterLinkGroupDialog`.

## Related

- [API.md](API.md) — endpoint conventions and middleware
- [BLAZOR.md](BLAZOR.md) — component architecture
- [ADR-005](adr/ADR-005-footer-customization.md) — architectural decision record
- [DOMAIN.md](DOMAIN.md) — entity definitions
