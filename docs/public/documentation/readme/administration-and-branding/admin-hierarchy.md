---
description: Delegate instance, tenant, organization, and group administration without widening authority.
---

# Admin Hierarchy & Permissions

Administration in ISLAMU Event is strictly scoped. Having administrative permissions at one level does not automatically grant authority across higher scopes or global platform concerns.

---

## Administrative Scopes

| Scope | Typical Responsibilities | Dedicated Console |
|---|---|---|
| **Instance** | Deployment-wide governance, [tenant provisioning](../security-and-identity/multi-tenancy.md), provider posture, platform policies, and [monetization fee policies](../events-and-ticketing/paid-events-and-payouts.md). | `/admin/instance` |
| **Tenant** | Community policies, public storefront experience, lookups, navigation, footers, templates, [custom properties](../events-and-ticketing/custom-properties.md), and [delegated storage](../integrations-and-ai/storage.md). | `/settings/admin` |
| **Organization** | Organization profile, membership approvals, verified organizer status, and scoped API keys. | `/settings/organization/{id}` |
| **Group** | Sub-community or chapter profiles, public event listings, and group branding. | `/settings/group/{id}` |

> [!NOTE]
> Instance console routes are active primarily in [Multi-Tenant Deployments](../security-and-identity/multi-tenancy.md). Tenant administrators govern local tenant participation and settings; they possess no authority to mutate global User, Actor, or Organization records.

---

## Action Authority & HAL Affordances

Every mutable action follows current **HAL `_links`** (see [Authorization Affordance Gating](../security-and-identity/authorization.md#the-golden-rule-of-client-ui-affordances)):
* The UI never renders "Purge", "Revoke", "Edit", or "Delete" buttons based on client-side role inspection alone.
* If a tenant is suspended or an event is finalized, the server omits the action link and the button disappears automatically.
* Concurrency and version conflicts are intentional safety signals: reload the current entity rather than force-overwriting state.

---

## High-Impact Operations & Audit Checklist

Record operator ownership, preconditions, and recovery paths before executing:

* Tenant purge scheduling or restoration.
* Production API key revocation.
* Switching between [Local RBAC and Cerbos PDP](../security-and-identity/authorization.md).
* Bulk [custom property retirement](../events-and-ticketing/custom-properties.md).
* Updating platform monetization fee structures.

---

## Related Guides & Next Steps

* **[Administration Web Walkthrough](admin-guide.md)** — Step-by-step navigation of all admin screens.
* **[Authorization & Access Control](../security-and-identity/authorization.md)** — Understand policy enforcement and fail-closed gates.
* **[Multi-Tenancy Architecture](../security-and-identity/multi-tenancy.md)** — Review tenant boundary enforcement and query filters.
* **[White-Labeling & Branding](white-labeling.md)** — Configure tenant styling tokens and governance locks.
