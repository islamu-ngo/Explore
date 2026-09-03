---
description: Walkthrough of administrative consoles and management workflows for instance and tenant admins.
---

# Administration Guide

This guide walks administrators through the web consoles in the Blazor management interface, covering instance-level controls, tenant provisioning, monetization, branding, and organization governance.

---

## 1. Administrative Consoles & Routes

| Administration Scope | Typical Role | UI Entry Points | Capabilities |
|---|---|---|---|
| **Instance Control Plane** | Instance Administrator | `/admin/instance`<br>`/admin/instance/tenants`<br>`/admin/instance/domains` | Multi-tenant governance, provisioning tenants, domain approvals, global quotas, platform settings. |
| **Instance Settings** | Instance Administrator | `/settings/instance` | Default system policies, storage configurations, SMTP defaults, platform monetization policies. |
| **Tenant Administration** | Tenant Administrator | `/settings/admin` | Tenant branding, lookups, navigation, custom footers, event templates, and custom registration properties. |
| **Organization Management**| Organization Admin | `/settings/organization/{id}` | Organization profile, membership approvals, verified organizer status, and API keys. |
| **Group Management** | Group Admin | `/settings/group/{id}` | Group profile, public event listings, group branding, and members. |

---

## 2. Instance Administration (Multi-Tenant Deployments)

The **Instance Console** (`/admin/instance`) is the operational command center for platform owners.

### Tenant Lifecycle Management
- **Create Tenant**: Provision a new community tenant with a unique slug and primary administrator.
- **Tenant States**:
  - *Active*: Fully operational; events can be published and registered.
  - *Suspended*: Public routes return inactive notices; administrative reads remain accessible.
  - *Archived*: Read-only state prior to scheduled purge.
- **Destructive Purge**: Scheduling a tenant purge requires explicit reason confirmation and typing the tenant slug.

### Platform Monetization
Navigate to `/settings/instance` $\to$ **Monetization**:
- **Platform Fee Policy**: Set platform fees across paid ticket sales (configured in basis points and optional fixed charges per currency).
- **Platform Contribution**: Enable optional voluntary contributions during checkout with customizable heading and body text.
- *Note*: Changes use optimistic concurrency revisions; concurrent edits fail safely to prevent accidental overwrites.

---

## 3. Tenant Administration & White-Labeling

Tenant administrators manage their community experience via `/settings/admin`:

### Branding & Appearance
- **Themes & Colors**: Configure brand primary and secondary colors.
- **Logos & Favicons**: Upload high-resolution community assets.
- **Navigation & Links**: Customize top navigation links and header menus.

### Custom Footers
- Choose footer layouts, social links, and copyright notices.
- Built-in governance locks prevent tenants from removing legally required disclosures.

### Custom Registration Properties
- Define custom questions and fields for events and attendee registrations.
- Set exposure levels: `Public` (visible on listing), `Private` (visible to organizers only), or `System`.

---

## 4. Organization & Group Governance

Organizers create Organizations and Groups to co-host events:
- **Verification Badges**: Instance and tenant admins can mark verified organizations to give attendees trust.
- **Membership Management**: Assign Owner, Admin, and Member roles within an organization.
- **Payment Connections**: Organizers onboard their own Stripe Connect accounts directly through organization settings to receive ticket payouts.
