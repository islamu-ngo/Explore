ABOUTME: Footer governance locking rules for instance vs tenant admin control.
ABOUTME: Covers 5 lock settings, override behavior, and single-tenant mode.

# Footer Governance

## Lock Settings

Instance admins can lock specific footer aspects to prevent tenant customization:

| Lock Setting | Controls | Effect When Locked |
|-------------|----------|-------------------|
| footer.lock_tenant_template | Template selection | Tenants cannot change footer template |
| footer.lock_tenant_link_groups | Link group management | Tenants cannot add/edit/delete/reorder link groups |
| footer.lock_tenant_social_links | Social media links | Tenants cannot modify social platform links |
| footer.lock_tenant_description | Brand description | Tenants cannot change description text |
| footer.lock_tenant_copyright | Copyright line | Tenants cannot change copyright text |

## Override Behavior

1. Instance admin sets lock → tenant admin UI disables the corresponding section.
2. Locked sections show a lock icon with explanation text.
3. Tenant API endpoints check locks before processing commands — locked operations return 403.
4. `GET /api/footer/config` (public) always returns resolved values regardless of lock state.

## Single-Tenant Mode

In single-tenant mode (only one tenant configured):
- Governance locking settings are **silently ignored** — the single tenant has full control.
- `InstanceFooterGovernanceSection.razor` shows an info alert explaining this behavior.
- The `BlockInSingleTenant` convention does not apply to footer governance — it gracefully degrades instead of blocking.

## Resolution Order

Footer data resolution for the public config endpoint:

1. Load instance-level footer settings (defaults).
2. Load tenant-level overrides (if not locked).
3. Merge: tenant values win for unlocked settings, instance values win for locked settings.
4. Load tenant link groups and links (if not locked, otherwise instance-level groups).

## Related

- `resources/data-model.md` — settings keys
- `resources/api-endpoints.md` — endpoints that enforce locks
- `docs/MULTI_TENANCY.md` — broader multi-tenancy rules
