ABOUTME: Context file for authz-parity task — key files, decisions, and interface signatures.
ABOUTME: Read this file first to resume work after a context reset.

# Authorization Parity — Context

**Last Updated:** 2026-03-24

---

## SESSION PROGRESS (2026-03-24)

### ✅ COMPLETED
- Full audit of both authorization providers (Cerbos + Fallback)
- Identified all resource kind gaps in both providers
- Discovered BUG: `GroupMemberDto` missing from `ResourceDescriptorRegistry` (runtime crash)
- Discovered BUG: `ToActionString` missing `ViewSharedContacts`/`ExportSharedContacts` mappings
- Discovered: 6 resource kinds missing from BOTH providers (not just fallback)
- Created `authz-report.md` in project root with full business-decision report
- Created implementation plan, context, and task files
- **Phase 1:** Fixed `ResourceDescriptorRegistry` (added GroupMember, Notification, Actor DTOs + action mappings), deleted 3 obsolete files
- **Phase 2:** Added 7 resource kind cases to `FallbackAuthorizationService` with 5 new evaluation methods
- **Phase 3:** Created 6 Cerbos policy YAML files (tenant_member, group, group_member, custom_property_definition, notification, actor)
- **Phase 4:** Batch optimization with pre-resolved `AuthorityProfile`, architecture parity tests, 20+ new unit tests
- All 869 non-integration tests pass (535 app + 44 arch + 100 domain + 190 secrets)

### 🟡 IN PROGRESS
- None — all phases complete

### ⚠️ BLOCKERS
- None

---

## Key Discovery: Both Providers Are Broken

The initial assumption was that Cerbos handles all resource kinds correctly and only the fallback
is missing cases. **This is wrong.** Six resource kinds have NO Cerbos YAML policy file either:
- `tenant_member`, `group`, `group_member`, `custom_property_definition`, `notification`, `actor`

Only `event_contact_share_consent` has a Cerbos policy but no fallback case.

**Implication:** Both providers need new policies for the same 6 resource kinds, plus the fallback
needs a case for `event_contact_share_consent`.

---

## Key Files

### Authorization Contract

**`Explore.Application/Contracts/Infrastructure/IAuthorizationProvider.cs`**
```csharp
public interface IAuthorizationProvider
{
    Task<bool> IsAllowedAsync(string resourceKind, string resourceId, string action,
        IDictionary<string, object>? resourceAttributes = null, CancellationToken ct = default);

    Task<IReadOnlyList<bool>> IsAllowedBatchAsync(IReadOnlyList<AuthorizationCheck> checks,
        CancellationToken ct = default);

    Task<bool> CheckSettingAccessAsync(string settingKey, string action,
        Guid? tenantId = null, Guid? organizationId = null, CancellationToken ct = default);
}

public sealed record AuthorizationCheck(
    string ResourceKind, string ResourceId, string Action,
    IReadOnlyDictionary<string, object>? ResourceAttributes = null);
```

### Fallback Provider

**`Explore.Infrastructure/Services/FallbackAuthorizationService.cs`**
- Line 50: Instance admin bypass (top of `IsAllowedAsync`)
- Line 59: SafeMode check
- Line 65: `resourceKind switch` — **this is where new cases go**
- Line 110: `IsAllowedBatchAsync` — sequential loop (optimization target)

### Cerbos Provider

**`Explore.Infrastructure/Services/CerbosAuthorizationService.cs`**
- Line 77: `IsAllowedBatchAsync` — single HTTP POST to `/api/check/resources`
- Line 182: `BuildResources` — auto-enriches `tenantId` and sets `Scope`

### Runtime Router

**`Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs`**
- Line 78: BYO check → instance provider → fallback chain
- Line 188: Instance mode cached 1 minute from SystemSetting

### Resource Kind Registry

**`Explore.Application/Authorization/ResourceDescriptorRegistry.cs`**
- Maps DTO types to resource kind strings
- **BUG:** `GroupMemberDto` and `GroupMemberListDto` are NOT registered → crash

### HATEOAS Evaluator

**`Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs`**
- Line 55: Batch call to `IsAllowedBatchAsync`
- Fail-closed on exception

### Cerbos Policies

**`cerbos/policies/`** — 20 YAML files + `derived_roles.yaml`
Missing: `tenant_member.yaml`, `group.yaml`, `group_member.yaml`,
`custom_property_definition.yaml`, `notification.yaml`, `actor.yaml`

### HATEOAS Link Policies with RequirePermission

These policies call `RequirePermission` and thus trigger authorization checks:
- `GroupLinkPolicy.cs` → `"group"` (via `GroupDto`)
- `GroupMemberLinkPolicy.cs` → ❌ crash (via `GroupMemberDto` — not in registry)
- `CustomPropertyDefinitionLinkPolicy.cs` → `"custom_property_definition"`
- `TenantMemberLinkPolicy.cs` → `"tenant_member"`

These do NOT use `RequirePermission` (static auth only):
- `NotificationLinkPolicy.cs` → `RequiresAuth: true` only
- `ActorLinkPolicy.cs` → read-only GET links only

---

## Important Decisions

1. **Notification semantics:** Notifications are personal data. All authenticated users can
   manage their own. No tenant-admin-only restriction for CRUD on own notifications.

2. **Actor semantics:** Actors are system-managed (created via registration). Read-only for
   all; write mutations only via admin.

3. **Group authorization:** Groups are org-scoped. Same authorization pattern as organizations
   (tenant admin or org admin for mutations).

4. **Batch optimization approach:** Pre-resolve admin context once, not per-check. Does NOT
   change the contract — just reduces redundant DB calls within a single batch.

---

## Quick Resume

To continue implementation:
1. Read this file
2. Check `authz-parity-tasks.md` for current progress
3. Start with Phase 1 (bug fixes) — they're small and unblock everything else
4. Phase 2 and 3 can be done in parallel (fallback cases + Cerbos YAML)
5. Phase 4 (optimization + tests) depends on Phase 2 completion
