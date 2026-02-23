# Multi-Tenancy Architecture

> **Conceptual Guide for Multi-Tenant System Design**
>
> This document describes the multi-tenancy model, authority hierarchy, and data isolation patterns.
> For implementation details and code patterns, see the relevant skills in `.claude/skills/`.

**Last Updated**: February 2026

---

## Overview

The platform operates as a **multi-tenant system** where multiple independent organizations (tenants) share the same application infrastructure while maintaining complete data isolation. The architecture supports both dedicated single-tenant deployments and shared multi-tenant SaaS models.

---

## Hierarchy of Authority

### Two-Tier Administration Model

The system distinguishes between two fundamentally different administrative roles:

| Tier | Role | Scope | Typical Use Case |
|------|------|-------|------------------|
| **Tier 1** | Instance Administrator | Infrastructure, global policies, module enablement | SaaS provider, IT department |
| **Tier 2** | Tenant Administrator | Community data, local configuration, user management | Organization leader, community admin |

### Instance Administrator (Super Admin)

**Responsibilities:**
- Physical server and infrastructure management
- Global resource limits and quotas
- Module enablement/disablement at platform level
- Enforcement policies that cannot be overridden
- Tenant provisioning and lifecycle management
- System-wide security policies

**Cannot Do:**
- Access tenant-specific business data
- Make decisions on behalf of tenants
- Override tenant preferences within allowed bounds

### Tenant Administrator (Customer)

**Responsibilities:**
- Community-specific configuration
- User management within their tenant
- Branding and customization
- Local business rules and workflows
- Content moderation policies

**Constrained By:**
- Instance-level enforcement policies
- Module availability set by Instance Admin
- Resource quotas and limits

---

## Cascading Configuration Engine

### Resolution Strategy

Settings are resolved at runtime using a **fall-through strategy**:

1. **Check Tenant Override** → Does the tenant have a specific value?
2. **Check Instance Enforcement** → Is this setting locked by the Instance Admin?
3. **Fall to Default** → Use the system default value

### Setting Types

| Type | Behavior | Example |
|------|----------|---------|
| **Locked** | Tenant cannot override | Security policies, payment provider |
| **Delegated** | Tenant may customize | Theme colors, notification preferences |
| **Default-Only** | System default, no UI exposed | Internal technical settings |

### Enforcement Scenarios

**Scenario: Render Mode Control**
- Instance Admin sets `RenderMode = Auto` with `IsLocked = false`
- Tenant Admin sees a dropdown and can change to `Server` or `WebAssembly`
- If Instance Admin sets `IsLocked = true`, dropdown is disabled/hidden

**Scenario: Module Restriction**
- Instance Admin configures `AllowedModules = ["Core", "Islamic"]`
- Tenant Admin cannot enable "Tech" module even if code exists
- Tenant only sees modules within their allowed set

---

## Data Isolation

### Tenant Boundary Enforcement

Every tenant-scoped entity implements `ITenantEntity`:
- Contains `TenantId` property
- Automatically filtered in all queries via global query filters
- Cannot be accidentally accessed across tenant boundaries

### Isolation Guarantees

| Layer | Mechanism |
|-------|-----------|
| **Database** | Global query filters on `TenantId` |
| **Application** | Tenant context injected per-request |
| **API** | TenantContext resolves tenant per request |
| **UI** | Routes scoped to tenant context |

### Tenant Resolution Priority

Tenant resolution follows a strict priority order in `TenantContext`:

1. `X-Tenant-Id` HTTP header (explicit selection)
2. Custom domain lookup (checks `TenantSetting` for matching domain)
3. Subdomain extraction (extracts from host, looks up tenant by subdomain or slug)
4. Default tenant (from configuration or hardcoded fallback)

### Cross-Tenant Operations

Cross-tenant operations are **explicitly forbidden** except:
- Instance Admin reporting and analytics
- System-wide notifications (opt-in)
- Federation sync (ATProto/ActivityPub)

---

## Tenant Lifecycle

### Provisioning Flow

1. **Request** → Tenant Admin requests new tenant (or Instance Admin creates)
2. **Validation** → Check quotas, naming conflicts, policy compliance
3. **Creation** → Database schema initialized with tenant seed data
4. **Configuration** → Module selection, initial settings
5. **Activation** → Tenant becomes operational

### Tenant States

| State | Description | Operations Allowed |
|-------|-------------|-------------------|
| **Pending** | Created but not activated | Configuration only |
| **Active** | Fully operational | All operations |
| **Suspended** | Temporarily disabled | Read-only access |
| **Archived** | Soft-deleted | None (data retained) |
| **Purged** | Permanently deleted | None (data removed) |

---

## Single-Tenant vs Multi-Tenant Deployment

### Virtual Tenant Strategy

The codebase is **always multi-tenant internally** to avoid maintaining separate codebases. Single-tenant mode is achieved through a "Virtual Tenant" that masks the complexity.

| Mode | Behavior |
|------|----------|
| **Single-Tenant** | `TenantId` hardcoded to `Default`; tenant management UI hidden |
| **Multi-Tenant** | `TenantId` resolved from subdomain/header; full tenant management |

### Runtime Mode Switching

The deployment mode can be changed at runtime without application restart:
1. Instance Admin toggles setting in dashboard
2. Database updated, cache invalidated
3. Next request uses new mode
4. UI adapts automatically (tenant menu appears/disappears)

---

## Onboarding Patterns

### Instance Admin First-Run

When the platform is first installed:
1. First user becomes Instance Admin
2. Wizard asks deployment purpose (Dedicated vs. SaaS)
3. Module selection for the instance
4. Default tenant configuration

### Tenant Registration Policies

| Policy | Description |
|--------|-------------|
| **Open** | Self-service registration via public form |
| **Invite-Only** | Instance Admin sends invitation link |
| **Sales-Gated** | Request must be approved by sales team |
| **Closed** | No new tenants (maintenance mode) |

---

## Tenant Branding & Customization

### Navigation Customization

Tenants can define custom external navigation links to seamlessly integrate the event platform with their main website.

- **Entity**: `TenantNavigationLink`
- **Management**: Tenant Admins via Admin Portal (`/admin/tenant/navigation`)
- **Features**:
  - Custom Label and URL
  - Ordering support
  - "Open in New Tab" option
  - Icon selection (MudBlazor icons)

These links are rendered dynamically in the main application sidebar, appearing below standard navigation items. This reduces friction for end-users by providing easy access back to the organization's main site.

---

## Related Documentation

- **[ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md)** - Detailed authority model
- **[DEPLOYMENT_MODES.md](DEPLOYMENT_MODES.md)** - Single vs multi-tenant deployment
- **[EXTENSIBILITY.md](EXTENSIBILITY.md)** - Module and aspect architecture
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Overall system architecture

## Implementation Reference

For code patterns and implementation details:
- **`dotnet-efcore-guidelines`** skill - Tenant query filters
- **`auth-patterns`** skill - Tenant context extraction
- **`clean-architecture-rules`** skill - Layer boundaries
