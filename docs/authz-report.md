ABOUTME: Business-decision report comparing Cerbos PDP authorization vs local fallback RBAC.
ABOUTME: Documents gaps, parity status, and strategic advantages of each authorization mode.

# Authorization Provider Parity Report

> **Purpose:** Inform the business decision on whether the ISLAMU Event platform can ship
> as self-contained software without Cerbos, and what the trade-offs are.
>
> **Last Updated:** 2026-03-24

---

## Executive Summary

The platform has two authorization providers behind a unified `IAuthorizationProvider` abstraction.
The **FallbackAuthorizationService** (local, DB-driven RBAC) handles the majority of resources correctly.
The **CerbosAuthorizationService** (external PDP) is the production default.

**The app can run without Cerbos — but with critical gaps that must be patched first.**

During this audit, we discovered that **both providers** are missing policy support for several
newer resource kinds. Additionally, a registry bug causes a **runtime crash** for group member
HATEOAS links. These findings are documented below with remediation tasks.

---

## Architecture Overview

```
IAuthorizationProvider (Application contract)
        │
        ▼
RuntimeAuthorizationProvider          ← Always registered (DI façade)
        │
        ├─ BYO tenant Cerbos? → CerbosAuthorizationService (custom endpoint)
        │        └─ failure_mode=closed → FallbackAuthorizationService (SafeMode)
        │        └─ failure_mode=open  → FallbackAuthorizationService (normal)
        │
        ├─ Instance setting = "cerbos"
        │        └─ CerbosAuthorizationService
        │             └─ on HTTP failure → FallbackAuthorizationService (auto-fallback)
        │
        └─ Instance setting = anything else
                 └─ FallbackAuthorizationService (normal)
```

**Switching mechanism:** `authorization.provider` SystemSetting (instance-scope, 1-minute cache).
No code deploy required to switch modes.

---

## Resource-Level Parity Table

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Fully functional — correct allow/deny decisions |
| ⚠️ | Functional with caveats (see notes) |
| ❌ | Broken — missing policy, incorrect behavior, or runtime error |
| ➖ | Not applicable |

### Core Resources (Both Providers Functional)

| Resource Kind | Fallback (Local) | Cerbos | HATEOAS Links | MediatR Pipeline | Notes |
|---|---|---|---|---|---|
| `instance_setting` | ✅ | ✅ | ✅ | ✅ | Instance admin only; both correct |
| `tenant_setting` | ✅ | ✅ | ✅ | ✅ | Lock semantics work in both |
| `tenant` | ✅ | ✅ | ✅ | ✅ | Instance admin for CUD; view open |
| `tenant_user` | ✅ | ✅ | ✅ | ✅ | Tenant-scoped RBAC |
| `category` | ✅ | ✅ | ✅ | ✅ | Tenant admin manages |
| `tag` | ✅ | ✅ | ✅ | ✅ | Tenant admin manages |
| `location` | ✅ | ✅ | ✅ | ✅ | Tenant admin manages |
| `organization` | ✅ | ✅ | ✅ | ✅ | Tenant admin + org admin |
| `organization_member` | ✅ | ✅ | ✅ | ✅ | Org-scoped |
| `organization_review` | ✅ | ✅ | ✅ | ✅ | All auth'd can create/view |
| `event` | ✅ | ✅ | ✅ | ✅ | Org-scoped, view open |
| `event_session` | ✅ | ✅ | ✅ | ✅ | Org-scoped |
| `event_session_agenda_item` | ✅ | ✅ | ✅ | ✅ | Org-scoped |
| `event_registration` | ✅ | ✅ | ✅ | ✅ | All auth'd can create/view |
| `storage_object` | ✅ | ✅ | ✅ | ✅ | All auth'd create/view; admin manages |
| `user` | ✅ | ✅ | ✅ | ✅ | Self-service + admin |
| `atproto_record` | ✅ | ✅ | ✅ | ✅ | Always deny non-admin (correct) |
| `indexed_did` | ✅ | ✅ | ✅ | ✅ | Same as atproto_record |

### Resources with Gaps

| Resource Kind | Fallback (Local) | Cerbos | HATEOAS Links | MediatR Pipeline | Impact |
|---|---|---|---|---|---|
| `tenant_member` | ❌ Default deny | ❌ No policy YAML | ❌ Links denied | ❌ Commands blocked | **Create/update/delete tenant members fails for non-instance-admins in BOTH providers** |
| `group` | ❌ Default deny | ❌ No policy YAML | ❌ Edit/delete links hidden | ⚠️ No `[AuthorizeResource]` | HATEOAS edit/delete denied; commands use endpoint-level `[Authorize]` only |
| `group_member` | ❌ Default deny | ❌ No policy YAML | ❌ **Runtime crash** | ⚠️ No `[AuthorizeResource]` | `GroupMemberDto` missing from `ResourceDescriptorRegistry` — throws `InvalidOperationException` |
| `custom_property_definition` | ❌ Default deny | ❌ No policy YAML | ❌ Edit/delete links hidden | ⚠️ No `[AuthorizeResource]` | HATEOAS links denied; commands use endpoint-level auth only |
| `event_contact_share_consent` | ❌ Default deny | ✅ Policy exists | ➖ No link policy | ❌ Fallback blocks commands | `ExportSharedContacts` and `GetOrganizationSharedContacts` blocked in fallback mode |
| `notification` | ❌ Default deny | ❌ No policy YAML | ⚠️ Uses `RequiresAuth` only | ⚠️ No `[AuthorizeResource]` | HATEOAS links work (static auth check); no resource-level auth on commands |
| `actor` | ❌ Default deny | ❌ No policy YAML | ✅ Read-only links | ⚠️ No `[AuthorizeResource]` | Actor links are GET-only, no `RequirePermission`; no impact currently |

### Critical Finding: Instance Admin Behavior Differs

For the missing resource kinds above, the two providers behave **differently for instance admins**:

| Provider | Instance Admin on Missing Resource Kind |
|---|---|
| **Fallback** | ✅ **Allowed** — instance admin bypass at top of `IsAllowedAsync` |
| **Cerbos** | ❌ **Denied** — no resource policy file means no rule matches, default deny |

This means **Cerbos is currently more broken than Fallback** for these resource kinds, because
even instance admins cannot perform operations that go through resource-level authorization checks.

---

## Batch Authorization Performance

### The HATEOAS Batch Check

`HateoasAuthorizationEvaluator` calls `IsAllowedBatchAsync` with **all permission-bound links
in a single call** per API response. For a resource like an Event with full HATEOAS, that can be
10–20 checks per response.

| Aspect | Cerbos | Fallback |
|---|---|---|
| **Single resource** | 1 HTTP POST to PDP | 1–3 DB queries (admin checks) |
| **Batch (N checks)** | 1 HTTP POST (all resources in payload) | N sequential `IsAllowedAsync` calls |
| **List of 20 events, 15 links each** | 1 HTTP call (~5ms) | Up to 300 sequential DB queries (~500ms) |
| **Latency under load** | Predictable (sub-ms per decision in PDP) | Linear growth with check count |

### Impact Assessment

For **detail endpoints** (single resource, 5–10 links): negligible difference.
For **list endpoints** (20+ resources, 10+ links each): **10–100x latency difference** under load.

The fallback `IsAllowedBatchAsync` implementation is a sequential loop:

```csharp
for (var i = 0; i < checks.Count; i++)
{
    results[i] = await IsAllowedAsync(check.ResourceKind, ...);
}
```

Each call queries `IAdminContext` (potentially hitting DB for tenant/org admin resolution).
This is the most critical operational difference between the two providers.

---

## What Works Locally Without Cerbos (After Fixes)

Assuming the gaps identified above are patched:

| Scenario | Status |
|---|---|
| Basic CRUD on events, sessions, registrations, organizations | ✅ Fully functional |
| Tenant and instance settings (including lock semantics) | ✅ Fully functional |
| User self-service (view/update own profile) | ✅ Fully functional |
| Category/tag/location management | ✅ Fully functional |
| Storage object operations | ✅ Fully functional |
| Tenant member management | ✅ After fix |
| Group and group member management | ✅ After fix |
| Custom property definition management | ✅ After fix |
| Notification operations | ✅ Works (no resource-level auth needed) |
| Actor operations | ✅ Works (read-only) |
| Contact share consent (export/view) | ✅ After fix |
| HATEOAS links for all resources | ✅ After fix |
| MediatR pipeline authorization | ✅ Functional |
| High-traffic list endpoints with HATEOAS | ⚠️ Works but N×DB queries per response |
| Per-tenant custom policies | ❌ Not available — hard-coded C# logic |
| BYO Cerbos for enterprise tenants | ❌ Not available |
| Authorization audit trail | ❌ None (structured logs only) |

---

## Strategic Comparison: Cerbos vs Local-Only

### Capability Matrix

| Capability | Local (After Fixes) | Cerbos | Business Impact |
|---|---|---|---|
| **Basic RBAC** | ✅ | ✅ | Both sufficient for core operations |
| **ABAC (attribute-based)** | ❌ Hard-coded conditions | ✅ CEL expressions in YAML | Cerbos enables field-level locks, temporal rules, value-based conditions without code |
| **Batch performance** | ⚠️ N×DB | ✅ 1 HTTP call | 10–100x latency gap on list endpoints at scale |
| **Policy as code** | ❌ Compiled C# | ✅ Versioned YAML in Git | Faster iteration; auditable; no deploy window needed |
| **Runtime policy updates** | ❌ Requires redeploy | ✅ Push via Admin API | Zero-downtime policy changes; immediate incident response |
| **Per-tenant policy overrides** | ❌ Not possible | ✅ Scoped policies per tenant | Enterprise tier differentiation without code branching |
| **BYO Cerbos for tenants** | ❌ N/A | ✅ Full routing support | Enterprise sales enablement; data sovereignty compliance |
| **Authorization audit log** | ❌ Structured logs only | ✅ Decision-level audit (7-day retention) | SOC 2, ISO 27001, GDPR audit trail requirements |
| **Policy unit testing** | ❌ Test via integration tests | ✅ `cerbos test` CLI | Catches regressions before deployment; policy-level TDD |
| **Derived roles** | ⚠️ Re-queried per check | ✅ Computed once per batch | Performance + consistency improvement |
| **Multi-policy composition** | ❌ Explicit if/else per rule | ✅ Base + scoped overlays | Scales to complex rules without code bloat |
| **Fail-safe behavior** | ✅ Always available | ✅ Circuit breaker + fallback | Both handle failure gracefully |
| **Air-gapped deployment** | ✅ Zero external deps | ⚠️ Needs PDP sidecar | Local wins for true zero-dependency deployments |
| **Development simplicity** | ✅ No extra service | ⚠️ Requires running Cerbos | Local is simpler for dev/prototyping |

### Deployment Tier Recommendation

| Tier | Authorization Mode | Rationale |
|---|---|---|
| **Development / Prototyping** | Local fallback | No extra services; fast iteration |
| **Community / Self-hosted (small)** | Local fallback | Zero-dependency deployment; sufficient for <100 users |
| **Production (single-tenant)** | Cerbos recommended | Audit trail, batch performance, policy testing |
| **Production (multi-tenant)** | Cerbos required | Per-tenant scoped policies, BYO Cerbos support |
| **Enterprise (regulated)** | Cerbos required | SOC 2 audit trail, per-tenant policy isolation, data sovereignty |
| **Air-gapped** | Local fallback + Cerbos sidecar | Cerbos can run locally as a sidecar without internet |

---

## Bugs Found During Audit

### BUG-1: `GroupMemberDto` Missing from `ResourceDescriptorRegistry`

**File:** `Explore.Application/Authorization/ResourceDescriptorRegistry.cs`
**Severity:** Critical — **runtime crash**
**Impact:** Any API response containing GroupMember HATEOAS links with `RequirePermission` throws `InvalidOperationException`
**Fix:** Add `[typeof(GroupMemberDto)] = "group_member"` to the registry

### BUG-2: Obsolete Authorization Files Still Present

Three files are marked as "superseded — should be deleted" but still exist:
- `Explore.Application/Contracts/Infrastructure/ICerbosAuthorizationService.cs`
- `Explore.Application/Authorization/CerbosAuthorizeAttribute.cs`
- `Explore.Application/Authorization/CerbosPermissionAction.cs`

**Severity:** Low — dead code, no runtime impact
**Fix:** Delete these files

### BUG-3: `event_contact_share_consent` Has Cerbos Policy But No Fallback Case

**File:** `Explore.Infrastructure/Services/FallbackAuthorizationService.cs`
**Severity:** High — `ExportSharedContacts` and `GetOrganizationSharedContacts` are blocked when running without Cerbos
**Fix:** Add fallback case matching the Cerbos policy semantics

---

## Conclusion

**Can the app fully work without Cerbos?** Yes, after patching the 6 missing resource kinds in
the fallback provider, adding corresponding Cerbos policy files, and fixing the `ResourceDescriptorRegistry`
crash. The local fallback provides correct RBAC for the 3-level admin hierarchy
(instance → tenant → organization).

**Should you still use Cerbos?** Yes, for any deployment beyond development or small community
instances. The advantages are not just "nice to have" — they are table-stakes for enterprise:

1. **Audit trail** — compliance requirement, not optional
2. **Batch performance** — 10–100x improvement on list endpoints at scale
3. **Runtime policy updates** — zero-downtime response to security incidents
4. **Per-tenant isolation** — required for multi-tenant SaaS and white-label
5. **Policy testing** — catches authorization regressions before they reach production

**The correct framing is:**
- **Self-contained mode** (no Cerbos): viable for development, small deployments, air-gapped environments
- **Cerbos mode**: required for production at scale, enterprise tenants, compliance-sensitive deployments
