# Deployment Modes

> **Single-Tenant vs Multi-Tenant Deployment Strategies**
>
> This document describes how the platform supports both dedicated single-tenant
> and shared multi-tenant deployments from a single codebase.

**Last Updated**: February 2026

---

## Overview

The platform supports two deployment modes from the **same codebase**:

| Mode | Description | Target Use Case |
|------|-------------|-----------------|
| **Single-Tenant** | One community per installation | Self-hosted, dedicated deployments |
| **Multi-Tenant** | Multiple communities per installation | SaaS, shared infrastructure |

**Key Insight**: The codebase is always multi-tenant internally. Single-tenant mode is a "mask" that simplifies the experience while preserving the underlying architecture.

---

## Virtual Tenant Strategy

### How It Works

Instead of maintaining two codebases:
1. Multi-tenant code is the foundation
2. Single-tenant mode forces the default tenant at runtime
3. Tenant management UI is hidden/shown based on mode
4. Mode can be switched at runtime

### Benefits

| Benefit | Description |
|---------|-------------|
| **One Codebase** | No divergence between modes |
| **Upgrade Path** | Single-tenant can become multi-tenant |
| **Testing Simplicity** | One set of tests covers both modes |
| **Consistent Security** | Same isolation mechanisms apply |

---

## Mode Characteristics

### Single-Tenant Mode

**Behavior**:
- `TenantId` hardcoded to `Default` tenant
- Tenant management UI hidden
- Instance Admin and Tenant Admin permissions merged
- No subdomain/header-based routing
- Simplified first-run experience

**Experience**:
- Feels like a traditional single-user application
- Admin sees combined settings panel
- No awareness of multi-tenant engine underneath

### Multi-Tenant Mode

**Behavior**:
- `TenantId` resolved from header → custom domain → subdomain → default tenant
- Full tenant management UI visible
- Clear separation between Instance and Tenant Admin
- Tenant-specific routing (e.g., `mosque.app.com`)

**Experience**:
- Full SaaS platform capabilities
- Tenant isolation clearly visible
- Self-service or managed onboarding

---

## First-Run Experience

### Single-Tenant Default

1. Application installs with seeded data:
   - `deployment.mode = SingleTenant`
   - Default tenant created
2. First user visits application
3. Middleware detects single mode, forces default tenant
4. User registers as Admin of default tenant
5. Application feels like standard single-user app

### No Configuration Required

The single-tenant first run requires:
- ❌ No tenant creation
- ❌ No subdomain setup
- ❌ No complex configuration
- ✅ Just install and use

---

## Runtime Mode Switching

### Enabling Multi-Tenant

When the deployment grows beyond a single community:

1. Instance Admin navigates to Settings → Instance
2. Toggles "Deployment Mode" to Multi-Tenant
3. Application clears relevant caches
4. Immediate effects:
   - Tenant sidebar menu appears
   - "Create Tenant" button visible
   - Subdomain routing activates
   - Original tenant becomes first of many

### Switching Back

Multi-tenant → Single-tenant is possible with constraints:
- Only one active tenant can exist
- Other tenants must be archived/purged
- Warning displayed about data implications

---

## Tenant Resolution

### Single-Tenant Mode

| Mechanism | Behavior |
|-----------|----------|
| Middleware | Short-circuits, returns `Default` |
| Routing | Standard routes, no subdomain |
| Context | Always populated with default tenant |

### Multi-Tenant Mode

| Mechanism | Options |
|-----------|---------|
| **Header** | `X-Tenant-Id: tenant1` → Tenant1 |
| **Custom Domain** | `events.tenant1.org` → Tenant1 |
| **Subdomain** | `tenant1.app.com` → Tenant1 |
| **Default** | Fallback to default tenant |

### Resolution Priority

1. Explicit header (`X-Tenant-Id`)
2. Custom domain
3. Subdomain
4. Default tenant (fallback)

---

## Configuration Model

### System Settings

| Setting | Values | Description |
|---------|--------|-------------|
| `deployment.mode` | `SingleTenant` / `MultiTenant` | Current mode (SystemSetting) |

### Environment Overrides

For containerized deployments:

| Environment Variable | Purpose |
|---------------------|---------|
| *(None)* | Deployment mode is resolved from SystemSettings at runtime |

---

## Infrastructure Considerations

### Single-Tenant Infrastructure

| Component | Configuration |
|-----------|---------------|
| Database | Single schema, single connection string |
| DNS | Single domain |
| SSL | Single certificate |
| Storage | Shared bucket/container |

### Multi-Tenant Infrastructure

| Component | Configuration |
|-----------|---------------|
| Database | Shared schema with tenant isolation |
| DNS | Wildcard domain (`*.app.com`) or tenant subdomains |
| SSL | Wildcard certificate or per-tenant |
| Storage | Tenant-prefixed paths or separate containers |

---

## Migration Paths

### Single → Multi (Growth)

**Trigger**: Organization wants to host other communities

**Steps**:
1. Enable multi-tenant mode
2. Configure DNS for subdomains
3. Update SSL certificate (wildcard)
4. Create additional tenants
5. Original tenant continues as-is

### Multi → Single (Consolidation)

**Trigger**: SaaS shutting down, moving to dedicated

**Steps**:
1. Export target tenant data
2. Archive/purge other tenants
3. Switch to single-tenant mode
4. Remove multi-tenant infrastructure

---

## Tenant Registration Policies

### Registration Flow Options

| Policy | Description | Visible UI |
|--------|-------------|------------|
| **Open** | Anyone can create tenant | "Start Your Community" button |
| **Invite-Only** | Instance Admin sends link | Hidden signup, invite link works |
| **Closed** | No new tenants | No signup UI |

### Policy Use Cases

| Scenario | Recommended Policy |
|----------|-------------------|
| Public SaaS | Open |
| Enterprise SaaS | Invite-Only |
| Pilot/Beta | Invite-Only |
| Maintenance | Closed |
| Single-Tenant | Closed (default) |

---

## Related Documentation

- **[MULTI_TENANCY.md](MULTI_TENANCY.md)** - Tenant isolation model
- **[ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md)** - Authority boundaries
- **[OPERATIONS.md](OPERATIONS.md)** - Deployment procedures
- **[CONFIGURATION.md](CONFIGURATION.md)** - Environment setup

## Implementation Reference

For code patterns:
- **`dotnet-efcore-guidelines`** skill - Tenant query filters
- **`auth-patterns`** skill - Tenant context middleware
