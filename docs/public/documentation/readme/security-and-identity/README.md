---
description: Identity, resource authorization, tenant isolation, and anti-resurrection privacy workflows.
---

# Security & Identity

Authentication, authorization, tenant isolation, and privacy erasure are separate authorities. Each fails closed; none silently substitutes for another during an outage or error condition.

---

## In this Section

* **[Authentication](authentication.md)** — Keycloak OIDC, Blazor BFF cookie sessions, API tokens, and linked AT Protocol sign-in.
* **[Authorization & Access Control](authorization.md)** — Local RBAC vs. Cerbos PDP, pipeline evaluation, and server-issued HAL action links.
* **[Multi-Tenancy](multi-tenancy.md)** — Deployment modes (`SingleTenant` vs. `multi_tenant`), host resolution, and database query filters.
* **[Privacy Erasure & Anti-Resurrection](privacy-erasure.md)** — GDPR Right-to-Erasure workflows, receipt tokens, and authority storage topologies.

---

## Operational Claim Boundary

These pages describe implemented platform behavior, not security, privacy, accessibility, compliance, or religious certification. Adopters remain responsible for threat modeling, provider agreements, legal assessment, incident response, retention, data residency, vulnerability management, and operational controls.

---

## Related Guides & Next Steps

* **[Docker Compose Runbook](../self-hosting/docker-compose.md)** — Deploy Keycloak and the PostgreSQL database.
* **[Admin Hierarchy & Roles](../administration-and-branding/admin-hierarchy.md)** — Learn how platform, tenant, and event organizer roles interact.
* **[Secrets Management](../configuration-and-operations/secrets.md)** — Protect OIDC client secrets and database passwords.
* **[Troubleshooting Authentication & Cerbos](../configuration-and-operations/troubleshooting-and-health.md)** — Step-by-step recipes for login redirect loops and 403 Forbidden errors.
