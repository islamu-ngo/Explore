ABOUTME: Operator and integrator guide for embedding ISLAMU Event as a white-label module in enterprise ERP platforms.
ABOUTME: Documents single-domain reverse proxy routing, automated tenant provisioning, OIDC SSO bridge, and visual branding alignment.

# Enterprise ERP & Modular Application Integration Guide

> **Audience:** Operators | Admins | Integrators | Enterprise ERP Partners
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-08-05
> **Source Anchors:** `docs/MULTI_TENANCY.md`, `docs/DEPLOYMENT_MODES.md`, `src/Explore.API/Middleware/ApiTenantResolutionMiddleware.cs`, `src/Explore.API/Controllers/ManagedProviderProvisioningController.cs`

---

## Executive Summary

ISLAMU Event is a multi-tenant, white-label event management and ticketing platform designed to run standalone or as an integrated sub-system. 

This guide documents how enterprise software vendors—specifically **modular ERP vendors** (such as Java Spring Boot, .NET, or Node.js monoliths)—can offer ISLAMU Event as an official, license-gated **Event Management Module** for their ERP clients.

By leveraging ISLAMU Event's **reverse proxy compatibility**, **automated managed provisioning**, **single sign-on (SSO)**, and **5-tier hierarchical settings**, ERP vendors can deliver a seamless, single-domain event management experience with zero custom UI coding required on the ERP side.

---

## Architecture Overview: 1-Domain Reverse Proxy Topology

Enterprise ERP platforms often operate on a single client domain (e.g., `https://erp.client-company.com`). ISLAMU Event can be mounted on the exact same domain under a dedicated subpath (e.g., `/events/`) via an edge gateway or reverse proxy (Nginx, Traefik, Caddy, or Spring Cloud Gateway).

```text
                        [ Client Browser ]
                                |
                    https://erp.client-company.com
                                |
             +------------------v-------------------+
             |    ERP Edge Reverse Proxy / Gateway   |
             |   (Nginx / Spring Cloud Gateway)     |
             +------------------+-------------------+
                                |
        +-----------------------+-----------------------+
        | /api/v1/crm, /accounting                      | /events/*
        v                                               v
+-----------------------+               +-------------------------------+
|  Java Spring Boot ERP |               |   ISLAMU Event Platform       |
|  (Core Monolith)      |               |   - Explore.Blazor (BFF)      |
|                       |               |   - Explore.API (.NET 10)     |
+-----------------------+               +-------------------------------+
                                                        |
                                            [ Managed Tenant Database ]
```

---

## Reverse Proxy & Request Header Forwarding

To ensure ISLAMU Event correctly resolves the client's tenant, generates relative links, and routes traffic without leaking internal infrastructure URLs, the reverse proxy must pass standardized headers.

### Required Header Forwarding Configuration

| Forwarded Header | Purpose | Example Value |
|---|---|---|
| `X-Forwarded-Host` | Preserves the client's public domain name | `erp.client-company.com` |
| `X-Forwarded-Proto` | Preserves the original request protocol | `https` |
| `X-Forwarded-Prefix` | Instructs the Blazor BFF and API of the base path | `/events` |
| `X-Tenant-Slug` | Authoritative tenant resolution header | `client-acme-corp` |

### Sample Nginx Configuration

```nginx
server {
    listen 443 ssl http2;
    server_name erp.client-company.com;

    # ERP Monolith (Java Spring Boot, etc.)
    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # ISLAMU Event Module Proxy Route
    location /events/ {
        proxy_pass http://127.0.0.1:5000/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Prefix /events;
        proxy_set_header X-Tenant-Slug acme-corp;
        
        # Buffer and timeout settings for Blazor WebAssembly / Server circuit
        proxy_read_timeout 86400;
        proxy_send_timeout 86400;
    }
}
```

### Runtime Tenant Resolution Flow

When a request enters `Explore.API`, `ApiTenantResolutionMiddleware` resolves the tenant using the strict precedence order documented in [MULTI_TENANCY.md](MULTI_TENANCY.md):

1. **Trusted `X-Tenant-Slug` Header**: Forwarded by the reverse proxy.
2. **Custom Domain**: Lookup against `domains.tenant_custom_domain`.
3. **Subdomain**: Lookup against `domains.tenant_subdomain`.
4. **Fallback**: Fails closed with `404 Not Found` if unresolved.

Once bound, Entity Framework Core automatically enforces tenant boundaries across all queries using global query filters (`ExploreDbContext`).

---

## Automated Managed Tenant Provisioning

When a client unlocks or purchases the Event Management Module within the ERP platform:

1. **License Key Activation**: The ERP backend validates the license key.
2. **Automated Provisioning Call**: The ERP executes a machine-to-machine HTTP request to ISLAMU Event's managed provider provisioning controller:

```http
POST /api/managed-provider-provisioning/clients:ensure
Host: event-api.internal
Authorization: Bearer <InstanceAdminTokenOrSetupSecret>
Content-Type: application/json

{
  "clientSlug": "acme-corp",
  "clientName": "Acme Corporation ERP Tenant",
  "adminEmail": "admin@acmecorp.com",
  "externalProviderId": "erp-license-99821"
}
```

3. **Provisioning Execution**:
   - Creates or updates the `Tenant` entity.
   - Assigns initial tenant administrator roles (`TenantUser`, `TenantUserRoleGrant`).
   - Registers external binding metadata for durable idempotency.
4. **UI Affordance Enablement**: The ERP dashboard displays the "Events" module tab pointing to `/events`.

---

## Single Sign-On (SSO) & Authorization Bridge

### Identity Federation with Keycloak OIDC

ISLAMU Event uses Keycloak for standard OIDC authentication (see [SECURITY-MODEL.md](SECURITY-MODEL.md)):

* **Shared Identity Provider**: The ERP and ISLAMU Event can connect to the same Keycloak realm.
* **Seamless Session Passing**: When the user clicks the "Events" tab in the ERP, the browser passes the existing Keycloak session cookie, completing OIDC Authorization Code Flow with PKCE without requesting user credentials again.

### HATEOAS & HAL Link Affordance Gating

Per [QUICK_REFERENCE.md](QUICK_REFERENCE.md), ISLAMU Event uses HAL (`_links`) as the single source of truth for UI actions. Clients (including the embedded Blazor UI) must gate action buttons by checking for the presence of HAL links in API responses:

```json
{
  "id": "018e4e5c-7f00-7000-8000-000000000002",
  "title": "Annual ERP User Conference",
  "_links": {
    "self": { "href": "/api/v1/events/018e4e5c-7f00-7000-8000-000000000002" },
    "edit": { "href": "/api/v1/events/018e4e5c-7f00-7000-8000-000000000002", "method": "PUT" },
    "delete": { "href": "/api/v1/events/018e4e5c-7f00-7000-8000-000000000002", "method": "DELETE" }
  }
}
```

The UI displays the **Edit** or **Delete** buttons only if `_links.edit` or `_links.delete` exists, eliminating the need to sync complex role matrices between the ERP and ISLAMU Event.

---

## White-Labeling & Visual Customization

ISLAMU Event provides a **5-tier hierarchical settings resolver** (`HierarchicalSettingsResolver`):

$$\text{Instance} \longrightarrow \text{Tenant} \longrightarrow \text{Organization} \longrightarrow \text{Group} \longrightarrow \text{User}$$

ERP vendors can apply white-label customization at the **Tenant** tier to harmonize the event module with the host ERP UI:

| Setting Category | Configurable Parameters | Customization Purpose |
|---|---|---|
| **Branding & Logos** | `branding.instance_name`, `branding.logo_url`, `branding.favicon_url` | Replaces ISLAMU branding with the client's or ERP vendor's brand. |
| **Theme & Colors** | Primary/Secondary Hex Codes, MudBlazor CSS Variables | Harmonizes the color palette with the host ERP dashboard. |
| **Navigation & Links** | Custom header links, footer template overrides | Ensures top navigation links route back to the host ERP modules. |

---

## Integration Topology Comparison

ERP integrators can select from 4 integration topologies based on UX and development requirements:

| Topology | URL Pattern | Implementation Complexity | UX Seamlessness | Recommended Use Case |
|---|---|---|---|---|
| **Subpath Proxying** *(Recommended)* | `erp.client.com/events` | Medium (Nginx / Gateway setup) | ⭐⭐⭐⭐⭐ High | Full-page native module feel on 1 domain. |
| **Subdomain Routing** | `events.client.com` | Low (DNS / CNAME) | ⭐⭐⭐⭐ High | Standalone event portal connected via SSO. |
| **Dashboard Iframe Embedding** | `erp.client.com/app/events` | Low (Iframe wrapper) | ⭐⭐⭐ Medium | Legacy ERP dashboards requiring layout wrapping. |
| **Headless REST/HAL API** | Customized in ERP | High (ERP builds custom UI) | ⭐⭐⭐⭐⭐ High | ERPs wanting 100% custom native Spring/React UI. |

---

## Operator Checklist for ERP Integrations

Before launching ISLAMU Event as an ERP module in production:

- [ ] **Configure Reverse Proxy**: Verify `X-Forwarded-Host`, `X-Forwarded-Prefix`, and `X-Tenant-Slug` headers are set.
- [ ] **Set Deployment Mode**: Ensure `DEPLOYMENT_MODE=multi_tenant` is set prior to onboarding (see [DEPLOYMENT_MODES.md](DEPLOYMENT_MODES.md)).
- [ ] **Test Managed Provisioning**: Verify `POST /api/managed-provider-provisioning/clients:ensure` successfully provisions new tenants.
- [ ] **Verify Keycloak Realm**: Ensure ERP identity token claims (`sub`, `email`) map correctly to ISLAMU Event user identity.
- [ ] **Audit Tenant Isolation**: Run architecture and integration tests (`Event.API.IntegrationTests`) to confirm query filter boundaries.
- [ ] **Customize Branding Settings**: Apply tenant-tier branding overrides to align MudBlazor CSS variables with the ERP theme.

---

## Related Documentation

- [MULTI_TENANCY.md](MULTI_TENANCY.md) — Runtime tenant resolution, EF Core query filters, and managed provisioning.
- [DEPLOYMENT_MODES.md](DEPLOYMENT_MODES.md) — Single-tenant vs. multi-tenant deployment modes.
- [CONFIGURATION.md](CONFIGURATION.md) — Environment variables, system settings, and hierarchical overrides.
- [SECURITY-MODEL.md](SECURITY-MODEL.md) — Keycloak OIDC, Cerbos authorization PDP, and trust boundaries.
- [BLAZOR.md](BLAZOR.md) — Blazor client architecture, rendering policies, and UI conventions.
