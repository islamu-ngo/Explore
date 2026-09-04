<!-- ABOUTME: Architectural specification for self-hosting boundaries, headless onboarding, and runtime identity gates. -->
<!-- ABOUTME: Directs operational deployment runbooks to canonical public documentation. -->

# Self-Hosting Architecture & Invariants

Last Updated: 2026-09-03 Europe/Brussels

---

> 📖 **Authoritative Operator Runbooks (Single Source of Truth):**  
> Operational deployment guides, Docker Compose topologies, volume persistence, reverse proxies (Traefik/Caddy/Nginx), and step-by-step upgrade procedures have been migrated to the **canonical public documentation**:
> 
> * 🚀 **[Deployment Tiers & Sizing Guide](https://islamu.gitbook.io/islamu-event/documentation/readme/self-hosting/deployment-tiers)**
> * 🐳 **[Docker Standalone Runbook (SQLite Monolith)](https://islamu.gitbook.io/islamu-event/documentation/readme/self-hosting/docker-standalone)**
> * 📦 **[Docker Compose Production Runbook (Split Topology)](https://islamu.gitbook.io/islamu-event/documentation/readme/self-hosting/docker-compose)**
> * ⚡ **[Coolify with Cerbos & Traefik Runbook](https://islamu.gitbook.io/islamu-event/documentation/readme/self-hosting/coolify-cerbos-traefik)**
> * 💾 **[Backup, Restore & Upgrade Runbook](https://islamu.gitbook.io/islamu-event/documentation/readme/configuration-and-operations/backup-restore-upgrade)**
> 
> For C# composition roots (`Explore.API`, `Explore.Blazor`, `Event.Standalone`, `Event.MigrationService`) and startup lifecycle ordering, see **[HOSTING_ARCHITECTURE.md](HOSTING_ARCHITECTURE.md)**.

---

## 1. Headless Instance Onboarding Invariant

You can bring an instance up without touching the interactive setup screens.
Set `INSTANCE_BOOTSTRAP_MODE=ConfiguredAdministrator` plus the provider key
(`keycloak` or `atproto`), the exact subject, a positive
`INSTANCE_BOOTSTRAP_BINDING_GENERATION`, the administrator email, and
optionally both profile names together. Under `Interactive`, none of those six
keys may be set. See [CONFIGURATION.md](CONFIGURATION.md) for the options binding rules.

For `keycloak`, the subject is paired with your existing `Keycloak:Authority`
issuer. For `atproto`, use the canonical DID. Deployment mode stays on the
existing `Deployment:Mode` setting.

Startup order is fixed in both Split and Standalone:
1. Migrations and lookup table seeding.
2. Configuration manifest bootstrap.
3. Serializable state preparation.
4. HTTP readiness probe goes active (`/health`).

A failure at any stage halts initialization immediately—read the earliest reason code, not the trailing symptom.

Nothing is granted by configuration alone. The instance remains in a pending-activation state until the configured administrator signs in and presents the exact provider claim matching the configured subject. That first sign-in completes onboarding permanently. Once onboarding completes, subsequent selector changes are ignored, preventing configuration drift or malicious authority transfer.

---

## 2. Required Operator Identity Gate

Runtime API and Standalone hosts require a complete `INSTANCE__OPERATORIDENTITY__*` section before startup:

* `INSTANCE__OPERATORIDENTITY__OPERATORID`: Canonical UUIDv7 identifier.
* `INSTANCE__OPERATORIDENTITY__PUBLICNAME`: User-facing organization name.
* `INSTANCE__OPERATORIDENTITY__LEGALNAME`: Legally registered entity name.
* `INSTANCE__OPERATORIDENTITY__CONTACTEMAIL`: Operational contact email.
* `INSTANCE__OPERATORIDENTITY__JURISDICTION`: Registered legal jurisdiction.

If any of these values are missing or blank, `Explore.API` and `Event.Standalone` fail closed during Phase 0 startup validation with an informative validation exception.

---

## 3. Standalone Core and Optional Service Boundary

The minimum operational deployment is the single `Event.Standalone` container:
* The ISLAMU Event API and Blazor BFF run in one .NET process with SQLite persistence.
* Application, Data Protection, and embedded privacy-erasure migrations execute within that process before traffic is accepted.
* PostgreSQL, Redis, Keycloak, Cerbos, MinIO/S3, Mailpit/SMTP, Svix, and Weblate are optional external integrations, not requirements of the standalone core.

For external container dependencies and third-party license boundaries, consult [CI_CD_GOVERNANCE.md](CI_CD_GOVERNANCE.md#standalone-and-optional-service-license-boundary).

---

## Related Specifications & Architecture Docs

* **[HOSTING_ARCHITECTURE.md](HOSTING_ARCHITECTURE.md)** — C# composition roots, assemblies, and startup lifecycle phases.
* **[CONFIGURATION.md](CONFIGURATION.md)** — C# `IOptions<T>` binding, validation, and hierarchical settings resolver.
* **[SECRETS.md](SECRETS.md)** — Environment-first and Infisical secret providers.
* **[SECURITY-MODEL.md](SECURITY-MODEL.md)** — Identity boundaries, fail-closed auth, and tenant isolation.
* **[Public Self-Hosting Documentation](https://islamu.gitbook.io/islamu-event/documentation/readme/self-hosting)** — Complete operator runbooks and environment recipes.
