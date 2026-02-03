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

### Standard User

**Identity**: Regular platform user within a tenant.

**Authority Scope**:
- View public content
- Register for events (if permitted)
- Manage own profile
- Create content (if permitted by policies)

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

---

## Authority Cascade

### Setting Resolution Order

1. **System Default** → Baseline value
2. **Instance Policy** → May override or lock
3. **Tenant Configuration** → May override if not locked
4. **User Preference** → May override if delegated

### Lock States

| State | Instance Admin | Tenant Admin | User |
|-------|----------------|--------------|------|
| **System Default** | Override ✅ | Override ✅ | Override ✅ |
| **Instance Locked** | Override ✅ | Override ❌ | Override ❌ |
| **Tenant Locked** | Override ✅ | Override ✅ | Override ❌ |
| **User Preference** | Override ✅ | Override ✅ | Override ✅ |

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

## Related Documentation

- **[MULTI_TENANCY.md](MULTI_TENANCY.md)** - Tenant isolation model
- **[SECURITY.md](SECURITY.md)** - Authentication and authorization
- **[OPERATIONS.md](OPERATIONS.md)** - Operational procedures

## Implementation Reference

For code patterns:
- **`auth-patterns`** skill - Role-based authorization
- **`clean-architecture-rules`** skill - Layer boundaries
