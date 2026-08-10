<!-- ABOUTME: Records the architecture decision standardizing Keycloak as the mandatory identity and authentication plane across all deployment tiers. -->
<!-- ABOUTME: Defines the clear boundary between Keycloak authentication, Cerbos fine-grained authorization, and domain tenant entitlements. -->

# ADR-021: Standardizing Authentication on Keycloak and Authorization on Cerbos

- **Status:** Accepted
- **Date:** 2026-08-10
- **Deciders:** CTO, Architecture Board, Security & Platform Engineering

## Context

Oppworx / ISLAMU Event is a modular ERP platform supporting multi-tenant SaaS, dedicated cloud (BYOC), and self-hosted on-premise installations. Modules (such as Accounting, Event Management, CRM, Procurement) can be purchased independently and activated over time.

As the product expanded, two options emerged for handling identity and credentials across modular and multi-deployment tiers:
1. Build a custom proprietary Identity Provider (IdP) using frameworks such as Spring Authorization Server.
2. Standardize on a mature, open-source identity platform (**Keycloak**) to handle authentication, OIDC/SAML enterprise federation, MFA, and account lifecycle, while delegating fine-grained business authorization decisions to **Cerbos**.

Building a proprietary IdP commits the engineering team to permanent operational and product maintenance of protocol compliance, credential hashing upgrades, passkey enrollment, enterprise SAML/OIDC brokering, brute-force protection, security CVE patching, and account console UX. For a business-focused ERP, this creates unbounded non-differentiating overhead.

## Decision

1. **Keycloak is the mandatory identity authority**: Keycloak handles authentication, user credential storage, OpenID Connect / OAuth 2.0 / SAML 2.0 token issuance, enterprise IdP brokering, MFA/WebAuthn enrollment, session lifecycle, and password recovery.
2. **Cerbos is the policy decision point (PDP)**: Cerbos evaluates fine-grained business authorization decisions (whether an authenticated principal may perform a specific action on a resource given tenant, entity, role, and workflow context).
3. **ERP Domain owns tenant & module entitlement data**: Tenant memberships, legal entity relationships, business roles, and module licenses remain mastered in the ERP domain database and control plane. Commercial entitlements are never encoded as raw Keycloak client roles or separate identity servers.
4. **One Keycloak deployment per environment/installation**: All installed modules in a deployment share a single Keycloak deployment and single user session. Adding a module introduces zero new identity servers and only adds necessary OAuth client / audience configurations if a distinct security boundary is required.
5. **Configuration as Code and Reconciler Engine**: Keycloak is managed declaratively via versioned desired-state contracts and runtime Admin API reconciliation (`CreateRealmIfMissing`, `PatchExistingRealm`, `ValidateOnly`). `realm-export.json` serves as a bootstrap and test baseline, while production backup/restore relies on database-native PostgreSQL snapshots.
6. **Browser Security Boundary**: Web applications consume Keycloak using OIDC Authorization Code Flow with PKCE (`S256`) via a Backend-for-Frontend (BFF) pattern (`Event.Web.BffHosting`). Raw JWTs are stored strictly in encrypted `HttpOnly`, `SameSite=Lax`, `Secure` session cookies and forwarded as `Bearer` headers to resource APIs.

## System Responsibility Matrix

| Concern | System of Record | Governance Rule |
| :--- | :--- | :--- |
| Credentials, Password Hashing & Authenticator Enrollment | **Keycloak** | Cerbos and API handlers never receive or inspect raw credentials. |
| Enterprise SSO / IdP Federation (OIDC / SAML / LDAP) | **Keycloak** | Centralized broker mapping upstream IdP claims to normalized OIDC tokens. |
| Login, Logout, Sessions & MFA Journeys | **Keycloak** | Central session revocation and step-up policy enforcement. |
| JWT Signing, OIDC Metadata & JWKS | **Keycloak** | API Resource Servers validate signatures locally via dynamic JWKS prefetching. |
| Tenant, Entity & Module Entitlements | **ERP Domain** | Mastered in ERP control plane; passed in request/authz evaluation context. |
| Fine-Grained Business Action Authorization | **Cerbos** | Evaluates resource, action, tenant, and workflow policies; fails closed. |
| ERP Audit Trail & Action Log | **ERP Domain / Database** | Transactional immutable audit log separate from Keycloak auth event logs. |

## Factual Guardrails & Operating Constraints

- **CNCF Status**: Keycloak is a CNCF Incubating project (accepted 10 April 2023) with strong adoption and active maintenance.
- **Client Modeling**: A Keycloak client represents an OAuth application/service security boundary, not an ERP pricing SKU or individual customer tenant.
- **Backup & Recovery**: `realm-export.json` is a deterministic dev/test baseline. Production recovery requires database-native backup/restore (`pg_dump` / WAL archiving).
- **Extension Admission**: No third-party Keycloak extension may be deployed to production without source code review, version pinning, SBOM scanning, compatibility testing, and a assigned internal maintainer.

## Consequences

### Positive
- **Single Identity Plane**: One login, one MFA policy, and one SSO session across all installed ERP modules.
- **Enterprise Readyness**: Instant integration with customer Active Directory, Okta, Microsoft Entra ID, or ADFS without modifying module code.
- **On-Premise & BYOC Parity**: Identical identity architecture used across SaaS, dedicated cloud, and self-hosted environments.
- **Clear Security Boundary**: Keycloak handles identity protocol plumbing; Cerbos handles business rules; application developers focus on core ERP domain features.

### Negative / Trade-offs
- **Platform Operational Responsibility**: Platform Engineering must operate, patch, monitor, and back up Keycloak deployments.
- **Upgrade Discipline**: Upstream Keycloak minor/major upgrades must be scheduled and tested against the dynamic reconciler.
- **Extension Governance**: Custom Keycloak Java SPI extensions require version matrix management and build integration.

## References & Evidence

- **Baseline Implementation**: Reference architecture documented in `src/Explore.API`, `src/Event.Web.BffHosting`, `src/Explore.Infrastructure/Services/Keycloak`, and `docker/keycloak/`.
- **Integration Tests**: Testcontainers-backed E2E test suite in `tests/Event.API.IntegrationTests/` using `ISLAMU-realm.test.json`.
- **Related ADRs**:
  - [ADR-001: Authorization Provider Architecture](ADR-001-authorization-provider-architecture.md) (Cerbos PDP integration)
  - [ADR-007: Durable Security & Admin Audit Trail](ADR-007-durable-security-admin-audit-trail.md)
  - [ADR-020: Global Actor And Concrete Tenant Participation](ADR-020-global-actor-and-concrete-tenant-participation.md)
