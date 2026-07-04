# Administrative Hierarchy

> **Authority Model and Permission Boundaries**
>
> This document defines the administrative roles, their authorities, and the governance model
> for the platform.

**Last Updated**: January 2026

---

## Overview

The platform implements a strict hierarchical authority model that separates infrastructure concerns from business operations. This ensures that SaaS providers, self-hosters, and community administrators each have appropriate control without overstepping boundaries.

---

## Role Definitions

### Instance Administrator (Super Admin)

**Identity**: The organization or individual who operates the physical/virtual infrastructure.

**Examples**:
- SaaS platform operator
- IT department running self-hosted instance
- DevOps team managing infrastructure

**Authority Scope**:

| Area | Can Do | Cannot Do |
|------|--------|-----------|
| **Infrastructure** | Server configuration, scaling, backups | Access tenant business data |
| **Modules** | Enable/disable globally, set restrictions | Use modules within tenants |
| **Tenants** | Create, suspend, archive, purge | Make business decisions for tenants |
| **Policies** | Lock settings, enforce quotas | Override tenant preferences within allowed bounds |
| **Security** | Authentication providers, encryption | Impersonate tenant users |

### Tenant Administrator (Community Admin)

**Identity**: The organization or individual who manages a specific community/tenant.

**Examples**:
- Mosque administrator
- Event organizer for a community
- Organization leadership

**Authority Scope**:

| Area | Can Do | Cannot Do |
|------|--------|-----------|
| **Configuration** | Customize allowed settings | Override locked settings |
| **Modules** | Enable from allowed set | Enable globally disabled modules |
| **Users** | Manage membership, roles | Access other tenants' users |
| **Content** | Moderate, publish, archive | Bypass instance policies |
| **Branding** | Colors, logos, themes | Change core UI patterns |

### Organization Administrator

**Identity**: A user with elevated privileges within a specific organization entity.

**Authority Scope**:
- Manage organization members and roles
- Create and manage events
- Approve registrations
- Respond to reviews
- Configure organization-specific settings

### Group Administrator

**Identity**: A user with elevated privileges within a specific group (sub-unit of an organization or tenant).

**Authority Scope**:
- Manage group members
- Create events within the group
- Configure group-specific settings

### Standard User

**Identity**: Regular platform user within a tenant.

**Authority Scope**:
- View public content
- Register for events (if permitted)
- Manage own profile
- Create content (if permitted by policies)
- Manage personal preferences

---

## Permission Boundaries

### What Instance Admin CANNOT Do

These are hard boundaries protecting tenant autonomy:

| Restriction | Rationale |
|-------------|-----------|
| Access tenant business data | Privacy, data sovereignty |
| Read individual user records | GDPR, privacy regulations |
| Modify tenant content | Business autonomy |
| Override tenant business rules | Operational independence |
| Access tenant API tokens | Security isolation |

### What Tenant Admin CANNOT Do

These are hard boundaries protecting platform integrity:

| Restriction | Rationale |
|-------------|-----------|
| Disable enforced security policies | Platform security |
| Access other tenants' data | Multi-tenant isolation |
| Exceed resource quotas | Fair resource usage |
| Enable unapproved modules | Platform consistency |
| Modify system tables | Data integrity |

### Event Moderation Boundary

Event moderation is a narrow safety exception to the normal business-ownership boundary. Instance administrators and tenant administrators can receive event `moderate-light`, `moderate-heavy`, `unmoderate`, and `view-management` authority in scope without receiving event `update` or `delete` authority.

| Action | Instance Admin | Tenant Admin | Organizer / Event Team |
|--------|----------------|--------------|-------------------------|
| Light moderate published event | Allowed in scope | Allowed in tenant | Not granted by ownership alone |
| Heavy redact unsafe event content/images | Allowed in scope | Allowed in tenant | Not granted by ownership alone |
| Unmoderate reversible light violation | Allowed in scope | Allowed in tenant | Not granted by ownership alone |
| Edit event business content | Not granted by moderation authority | Not granted by moderation authority | Granted only by normal event management policy |
| View safe moderation history | Allowed through `view-management` | Allowed through `view-management` | Allowed only when normal event management policy grants it |

Light moderation hides the event while preserving content and can be reversed back to `Published`. Heavy moderation is irreversible: event-owned text is redacted, event image references are detached, provider objects are deleted through the storage abstraction, attendee notifications are generic, and audit/observability keep only safe metadata. All moderation affordances must be driven by HAL links, not by client-side role checks.

---

## Authority Cascade

### Setting Resolution Order

Resolution follows a 5-tier cascade via `HierarchicalSettingsResolver`:

1. **User Preference** (Highest priority)
2. **Group Setting**
3. **Organization Setting**
4. **Tenant Configuration**
5. **System Default** (Lowest priority)

### Lock States

Higher tiers can lock settings to prevent lower-tier overrides.

| Tier | Can Override | Can Be Locked By |
|-------|----------------|--------------|
| **User** | Yes | Group, Org, Tenant, Instance |
| **Group** | Yes | Org, Tenant, Instance |
| **Organization** | Yes | Tenant, Instance |
| **Tenant** | Yes | Instance |
| **Instance** | N/A | — |

**Note**: In single-tenant mode, instance-level locks are bypassed for the default tenant.

---

## Enforcement Policies

### Policy Types

| Type | Enforced By | Example |
|------|-------------|---------|
| **Technical** | System code | Authentication required |
| **Configuration** | Settings engine | Render mode restrictions |
| **Resource** | Quota service | Storage limits |
| **Compliance** | Audit system | Data retention rules |

### Policy Priority

When policies conflict:
1. Legal/Compliance (highest priority)
2. Security policies
3. Instance policies
4. Tenant policies
5. User preferences (lowest priority)

---

## Delegation Model

### Instance → Tenant Delegation

Instance Admin can delegate specific authorities:

| Delegatable | Default | Rationale |
|-------------|---------|-----------|
| Module selection | ✅ Delegated | Business flexibility |
| Branding | ✅ Delegated | Community identity |
| User management | ✅ Delegated | Operational need |
| Security settings | ❌ Locked | Platform protection |
| Payment processing | ⚙️ Configurable | Business model dependent |

### Tenant → Organization Delegation

Tenant Admin can delegate to Organization Admins:

| Delegatable | Default | Rationale |
|-------------|---------|-----------|
| Event management | ✅ Delegated | Core function |
| Member management | ✅ Delegated | Operational need |
| Content moderation | ⚙️ Configurable | Trust level dependent |
| Financial access | ❌ Restricted | Liability |

---

## Audit and Accountability

### Audit Requirements

| Role | Actions Audited | Retention |
|------|-----------------|-----------|
| Instance Admin | All system changes | Indefinite |
| Tenant Admin | Configuration, user management | Per policy |
| Organization Admin | Content, membership changes | Per policy |
| User | Security-relevant actions | Per policy |

### Accountability Chain

Every administrative action must be:
1. **Authenticated** - Verified identity
2. **Authorized** - Proper role/permission
3. **Logged** - Recorded with context
4. **Traceable** - Linkable to human actor

---

## Special Scenarios

### Emergency Access

Instance Admin may bypass restrictions only when:
- Security incident response
- Legal compliance requirement
- Disaster recovery
- Explicit tenant consent

**All emergency access is fully audited and reported.**

### Tenant Offboarding

When a tenant leaves:
1. Data export provided to tenant
2. Grace period for data retrieval
3. Anonymization of shared data
4. Complete deletion after retention period

### Dispute Resolution

When tenant and instance policies conflict:
1. Instance policies prevail for security/legal
2. Tenant preferences prevail for business
3. Escalation path defined in service agreement

---

## Machine Principal Authorities (External API Keys)

Non-interactive callers authenticate with long-lived API keys. Each key is bound to an **owner type** that determines the authority ceiling. The ceiling cannot be escalated by granting broader scopes — scopes merely refine the ceiling.

### Owner Type → Authority Mapping

| Owner Type | Tenant Binding | Equivalent Interactive Role | Cross-Tenant? |
|---|---|---|---|
| `User` (1) | Required | The owner user's actual memberships (tenant/org/group admin claims) | No — follows owner's memberships |
| `Organization` (2) | Required | Organization admin for the owning org | No |
| `Group` (3) | Required | Group admin for the owning group | No |
| `Tenant` (4) | Required | Tenant admin for the bound tenant | No |
| `InstanceAdmin` (5) | **Nullable credential; execution tenant required for tenant-scoped API/MCP calls** | Instance admin | Platform authority; tenantless execution only for explicit host-administration API routes |

### Scope Ceilings

Scopes declare intent; owner type declares reachable authority. Effective permission = scope ∩ ceiling.

| Owner Type | Ceiling Scopes |
|---|---|
| `User` | `events:*`, `users:*`, `lookups:read`, `registrations:write`, `api-keys:manage` |
| `Organization` | User scopes + `organizations:*` |
| `Group` | User scopes + `groups:*` |
| `Tenant` | All of the above + `admin:tenant` |
| `InstanceAdmin` | All scopes including `admin:instance` |

A `User`-owned key cannot be granted `admin:tenant` even by a system administrator — the validator refuses the request. To extend authority, the correct path is to create a new key at a higher owner type.

### InstanceAdmin Boundary

`InstanceAdmin` keys are the only keys with `TenantId = NULL`. They are meant for platform operator use:

- Cross-tenant reporting and usage analytics
- Instance-level configuration (`InstanceSetting`, `AtprotoRecord`, `IndexedDid`, `PlatformNamespace`)
- Bulk tenant operations

InstanceAdmin keys **cannot** be created through the tenant-admin UI. They are provisioned through the instance-admin settings surface, subject to the same audit logging as interactive instance admin role assignments.

### Authority Resolution in Code

`IMachinePrincipalAccessor.Current` exposes the parsed `ApiKeyPrincipalContext` to both authorization providers:

- `CerbosPrincipalBuilder.BuildMachinePrincipalAsync` synthesizes a Cerbos principal with `is_machine=true`, `api_key_id`, `owner_type`, `scopes`, plus authority attributes (`isInstanceAdmin`, `tenantMemberships`, `orgMemberships`) derived from owner type.
- `FallbackAuthorizationService.EvaluateMachineCallerAccessAsync` applies the same rules directly (scope gate first, then owner-type-specific authority check).

Both backends produce identical decisions for identical inputs, so the platform can run in Cerbos-enabled or Cerbos-disabled deployments without behavior divergence.

---

## Related Documentation

- **[MULTI_TENANCY.md](MULTI_TENANCY.md)** - Tenant isolation model
- **[SECURITY.md](SECURITY.md)** - Authentication and authorization
- **[OPERATIONS.md](OPERATIONS.md)** - Operational procedures

## Implementation Reference

For code patterns:
- **`auth-patterns`** skill - Role-based authorization
- **`clean-architecture-rules`** skill - Layer boundaries
