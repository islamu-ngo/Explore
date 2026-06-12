ABOUTME: Footer API endpoints (11 total) and CQRS command/query structure.
ABOUTME: Reference for FooterController routes, auth requirements, and handler patterns.

# Footer API Endpoints

## Endpoint Table (FooterController)

| # | Method | Route | Auth | CQRS |
|---|--------|-------|------|------|
| 1 | GET | /api/footer/config | AllowAnonymous | GetFooterConfigQuery |
| 2 | GET | /api/footer/link-groups | Authorize | GetLinkGroupListQuery |
| 3 | GET | /api/footer/link-groups/{id} | Authorize | GetLinkGroupDetailsQuery |
| 4 | POST | /api/footer/link-groups | Authorize | CreateLinkGroupCommand |
| 5 | PUT | /api/footer/link-groups/{id} | Authorize | UpdateLinkGroupCommand |
| 6 | DELETE | /api/footer/link-groups/{id} | Authorize | DeleteLinkGroupCommand |
| 7 | POST | /api/footer/link-groups/reorder | Authorize | ReorderLinkGroupsCommand |
| 8 | POST | /api/footer/link-groups/{groupId}/links | Authorize | CreateLinkCommand |
| 9 | PUT | /api/footer/links/{id} | Authorize | UpdateLinkCommand |
| 10 | DELETE | /api/footer/links/{id} | Authorize | DeleteLinkCommand |
| 11 | PUT | /api/footer/settings | Authorize | UpdateTenantSettingsCommand |

## CQRS Structure

### Queries (4)

| Query | Returns | Notes |
|-------|---------|-------|
| GetFooterConfigQuery | FooterConfigDto | Public — all footer data for rendering |
| GetLinkGroupListQuery | List\<LinkGroupDto\> | Admin — list with link counts |
| GetLinkGroupDetailsQuery | LinkGroupDetailDto | Admin — group with nested links |
| GetGovernanceSettingsQuery | GovernanceSettingsDto | Instance admin only |

### Commands (9)

| Command | Returns | Notes |
|---------|---------|-------|
| CreateLinkGroupCommand | BaseCommandResponse\<Guid\> | |
| UpdateLinkGroupCommand | BaseCommandResponse\<Guid\> | |
| DeleteLinkGroupCommand | bool | |
| ReorderLinkGroupsCommand | BaseCommandResponse\<Guid\> | Accepts ordered ID list |
| CreateLinkCommand | BaseCommandResponse\<Guid\> | Nested under group |
| UpdateLinkCommand | BaseCommandResponse\<Guid\> | |
| DeleteLinkCommand | bool | |
| UpdateTenantSettingsCommand | BaseCommandResponse\<Guid\> | Updates footer.* AppSettings |
| UpdateGovernanceSettingsCommand | BaseCommandResponse\<Guid\> | Instance admin locks |

## Admin UI Components

| Component | Location | Purpose |
|-----------|----------|---------|
| FooterSettings.razor | /admin/tenant/footer | Main settings page |
| FooterLinkDialog.razor | Dialog | Create/edit individual links |
| FooterLinkGroupDialog.razor | Dialog | Create/edit link groups |
| InstanceFooterGovernanceSection.razor | Shared | Governance toggle controls |

## Related

- `resources/data-model.md` — entities and settings
- `resources/governance.md` — locking behavior
