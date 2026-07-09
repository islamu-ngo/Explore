<!-- ABOUTME: Architecture note for the three-tier admin product hierarchy: Tenant Console, Instance Console (AdminPortal), and Fleet Console. -->
<!-- ABOUTME: Defines product boundaries, authority levels, and the managed-hosting fleet vision without conflating instance admin with hosting provider. -->

# Admin Product Hierarchy And Fleet Console Vision

Last Updated: 2026-07-08 Europe/Brussels

## Product Architecture: Two Blazor Apps

There are exactly **two** Blazor apps, not three:

```text
1. AdminPortal Blazor App
   Single app for one ISLAMU Event instance.
   Hosts two shells depending on the user's authority:
     - Instance Console shell  (instance admin)
     - Tenant Console shell    (tenant admin, if instance admin allows)
   One BFF, one Keycloak client, one API backend.

2. ConsolePortal Blazor App (future)
   Separate app for managed-hosting providers.
   Manages many ISLAMU Event instances across a fleet.
   Different authority level entirely (hosting provider, not instance admin).
```

The existing code uses `Event.ControlPlane.Blazor` / `Event.ControlPlane.Client` project names. Rename to `Event.AdminPortal.Blazor` / `Event.AdminPortal.Client` when the Tenant Console shell is added. All new documentation should use "AdminPortal" for the product and "Control Plane" only when referencing existing code artifacts.

## Authority Hierarchy

```text
Tenant Admin
  manages one tenant/community inside one ISLAMU Event instance

Instance Admin
  manages one ISLAMU Event instance
  may be the customer/client of a managed-hosting provider

Fleet Admin / Hosting Provider
  manages many ISLAMU Event instances
  is the managed-hosting provider, NOT the customer
```

These are not the same authority level. Instance admin ≠ Fleet admin.

## Product Surface Map

```text
Event.Web
  public app / event discovery / user experience

Event.Studio
  organizer and creator workspace

Event.AdminPortal  (formerly Control Plane)
  optional separate admin app for one instance
  contains:
    Tenant Console   — tenant admins
    Instance Console — instance admins

Event.FleetConsole  (future)
  managed-hosting provider app
  manages many instances
```

## AdminPortal: Two Shells In One App

The AdminPortal is a **single Blazor app** that serves both instance admins and tenant admins (if the instance admin allows it). Both shells share the same BFF, Keycloak client, and API backend — they differ by authority level and route prefix.

### Instance Console (current scope)

Audience: self-hoster, customer IT admin, managed-hosting client admin.

Manages:
- tenants inside this instance (lifecycle, provisioning, quotas)
- domains and routing
- storage/email/auth provider configuration and status
- instance policy locks
- background jobs / outbox / dead letters
- backup readiness
- tenant plan governance (SaaS tiers, versioned plans, assignment, effective config)
- global moderation escalations
- Keycloak/Cerbos provider status
- maintenance mode

### Tenant Console (future, gated by instance settings)

Audience: mosque admin, community admin, organization operator inside a tenant.

Manages:
- tenant settings (within instance limits)
- tenant branding
- tenant moderation queue
- tenant reports
- tenant users/members
- tenant footer/navigation
- tenant modules allowed by instance
- tenant events/policies
- tenant API keys / webhooks (if allowed by instance)

Instance admin controls whether tenant admins can access the AdminPortal via instance-level settings:

```text
admin_portal.enabled = true/false
admin_portal.public_url = "https://admin.example.org"
admin_portal.allow_tenant_admin_access = true/false
admin_portal.tenant_admin_sections = [...]
admin_portal.include_link_in_tenant_nav = true/false
admin_portal.include_link_in_tenant_invitation_email = true/false
```

The URL is discovery only. Actual access is enforced by Keycloak auth, BFF session, API authorization, HAL links, and tenant membership checks.

## ConsolePortal (Future, Separate Blazor App)

The ConsolePortal is a **separate Blazor app** for managed-hosting providers. It is NOT a shell inside the AdminPortal. It manages many ISLAMU Event instances across a fleet and operates at a completely different authority level (hosting provider, not instance admin).

### Scope

Manages:
- workspaces / customers (billing/support boundary, NOT an ISLAMU Event tenant)
- managed instances (many ISLAMU Event deployments)
- automated provisioning (PostgreSQL, Redis, object storage, Keycloak realm/client, Cerbos PDP, app secrets, DNS, migrations, bootstrap)
- BYOC connections (customer cloud, Terraform/OpenTofu templates, deployment agent)
- provider-level quotas and commercial limits (max storage per instance, max tenants, max events, max emails, backup retention, available modules, SLA tier)
- version and upgrade management (version inventory, security patches, canary upgrades, rollback, migration preflight)
- backup and restore governance (last backup, restore test status, retention, RPO/RTO compliance)
- support access grants (time-limited, scoped, ticket-linked, customer-approved, audited)
- fleet-wide health/incident dashboard

### Quota Layers

```text
Provider quota  — set by Fleet Console, customer/instance cannot exceed
Instance policy — set by Instance Console, controls tenants inside the instance
```

Example:
```text
Fleet Console:  Instance A max storage = 500GB
Instance Console: Tenant X max storage = 50GB, Tenant Y = 100GB
```

### Moderation Federation

Fleet Console should NOT automatically receive all moderation/report content from every instance. Default to metadata-only escalation.

```text
Mode 1: No fleet moderation — Fleet sees only health/metadata
Mode 2: Escalation metadata only (default for managed hosting)
Mode 3: Managed moderation — full report content for selected categories
Mode 4: Emergency/legal escalation — specific cases based on legal/security rules
Mode 5: Shared SaaS platform — provider is also the platform operator
```

Report visibility levels:
```text
TenantOnly           — tenant admins/moderators
TenantAndInstance    — tenant + instance admins
InstanceOnly         — instance admins (cross-tenant abuse/security)
FleetMetadata        — hosting provider sees metadata only
FleetFullAccess      — explicit managed moderation/support grant
```

Centralize: instance health, version, backup status, quota totals, report counts/severity, incident metadata, support access grants, audit metadata.

Do NOT centralize by default: user PII, event descriptions, attendee lists, private reports, uploaded evidence, tenant business data.

### Future Projects

```text
Event.ConsolePortal.Blazor     — fleet admin UI (managed-hosting provider)
Event.FleetManagement.Api      — fleet API
Event.InstanceAgent            — per-instance health/reporting agent
```

## Decision Record

- There are **two** Blazor apps, not three: AdminPortal (one instance, two shells) and ConsolePortal (future, many instances).
- The AdminPortal is a single app serving both instance admins and tenant admins (if instance admin enables it).
- Do NOT turn the AdminPortal into the ConsolePortal. Different authority levels entirely.
- Rename from `Event.ControlPlane.Blazor` to `Event.AdminPortal.Blazor` when the Tenant Console shell is added.
- For v1.0: keep AdminPortal instance-admin-only unless instance admin explicitly enables tenant admin access.
