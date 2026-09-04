<!-- ABOUTME: Records the architecture decision standardizing Keycloak as the mandatory identity and authentication plane across all deployment tiers. -->
<!-- ABOUTME: Defines the clear boundary between Keycloak authentication, Cerbos fine-grained authorization, and ISLAMU Event domain data. -->

# ADR-021: Standardizing Platform Authentication on Keycloak and Authorization on Cerbos

- **Status:** Superseded by [ADR-027](ADR-027-first-class-authentication-provider-matrix.md)
- **Date:** 2026-08-10
- **Deciders:** CTO, Architecture Board, Security & Platform Engineering

## Context

**ISLAMU Event** is an open-source, white-label Event Management and Ticketing platform. It supports multi-tenant SaaS, dedicated cloud, and self-hosted on-premise installations across diverse deployment scenarios (such as community portals, non-profit platforms, ticket operators, enterprise systems, and partner platforms).

As the platform grew, two primary architecture options were evaluated for identity, user credentials, and session management:
1. Build a custom proprietary Identity Provider (IdP) using low-level security frameworks.
2. Standardize on a mature, open-source identity platform (**Keycloak**) to handle authentication, OIDC/SAML enterprise federation, MFA, and account session lifecycle, while delegating fine-grained business authorization decisions to **Cerbos**.

Building a custom identity provider creates permanent, non-differentiating security maintenance overhead (credential hashing upgrades, passkey enrollment, enterprise SAML/OIDC brokering, brute-force protection, CVE patching, and account console UX). Standardizing on Keycloak provides a secure, standards-based identity foundation for all deployment targets and integration use-cases.

## Decision

1. **Keycloak is the mandatory identity authority**: Keycloak handles user authentication, credential storage, OpenID Connect (OIDC) / OAuth 2.0 / SAML 2.0 token issuance, enterprise IdP brokering (Active Directory, Okta, Entra ID), MFA/WebAuthn enrollment, session lifecycle, and password recovery.
2. **Cerbos is the policy decision point (PDP)**: Cerbos evaluates fine-grained business authorization decisions (whether an authenticated principal may perform a specific action on an event, ticket, order, registration form, or admin resource given tenant, entity, role, and workflow context).
3. **ISLAMU Event Domain owns event & participation data**: Events, sessions, tickets, registrations, and tenant participation remain mastered in the ISLAMU Event domain database and control plane. User entitlements and roles are evaluated via Cerbos, never hardcoded as raw Keycloak client roles.
4. **Single Identity Plane**: Keycloak acts as the central identity authority for the platform deployment. Web applications and client integrations authenticate against Keycloak over standard OpenID Connect protocols.
5. **Configuration as Code and Reconciler Engine**: Keycloak is managed declaratively via versioned desired-state contracts and runtime Admin API reconciliation (`CreateRealmIfMissing`, `PatchExistingRealm`, `ValidateOnly`). `realm-export.json` serves as a bootstrap and test baseline, while production backup/restore relies on database-native PostgreSQL snapshots.
6. **Browser Security Boundary**: Web applications consume Keycloak using OIDC Authorization Code Flow with PKCE (`S256`) via a Backend-for-Frontend (BFF) pattern (`Event.Web.BffHosting`). Raw JWTs are stored strictly in encrypted `HttpOnly`, `SameSite=Lax`, `Secure` session cookies and forwarded as `Bearer` headers to `Explore.API`.

## System Responsibility Matrix

| Concern | System of Record | Governance Rule |
| :--- | :--- | :--- |
| Credentials, Password Hashing & Authenticator Enrollment | **Keycloak** | Cerbos and API handlers never receive or inspect raw credentials. |
| Enterprise SSO / IdP Federation (OIDC / SAML / LDAP) | **Keycloak** | Centralized broker mapping external/upstream IdP claims to normalized OIDC tokens. |
| Login, Logout, Sessions & MFA Journeys | **Keycloak** | Central session revocation and step-up policy enforcement. |
| JWT Signing, OIDC Metadata & JWKS | **Keycloak** | API Resource Servers validate signatures locally via dynamic JWKS prefetching. |
| Tenant, Event, Ticket & Registration Data | **ISLAMU Event Domain** | Mastered in ISLAMU Event control plane; passed in request/authz evaluation context. |
| Fine-Grained Business Action Authorization | **Cerbos** | Evaluates resource, action, tenant, and workflow policies; fails closed. |
| Platform Audit Trail & Action Log | **ISLAMU Event Database** | Transactional immutable audit log separate from Keycloak auth event logs. |

## Factual Guardrails & Operating Constraints

- **Platform Identity**: ISLAMU Event is a standalone Event Management & Ticketing platform. It is NOT an ERP system and is not tied to any proprietary ERP product.
- **CNCF Status**: Keycloak is a CNCF Incubating project (accepted 10 April 2023) with strong adoption and active maintenance.
- **Client Modeling**: A Keycloak client represents an OAuth application/service security boundary (callback URI, audience, credentials), not an individual tenant or customer entity.
- **Backup & Recovery**: `realm-export.json` is a deterministic dev/test baseline. Production recovery requires database-native backup/restore (`pg_dump` / WAL archiving).
- **Extension Admission**: No third-party Keycloak extension may be deployed to production without source code review, version pinning, SBOM scanning, compatibility testing, and an assigned internal maintainer.

## Consequences

### Positive
- **Standards-Based Security**: Standard OpenID Connect / OAuth 2.0 protocols across all client applications and integrations.
- **Enterprise Readyness**: Instant integration with Active Directory, Okta, Microsoft Entra ID, or SAML IdPs without modifying ISLAMU Event domain code.
- **Deployment Flexibility**: Identical identity architecture used across SaaS, dedicated cloud, and self-hosted environments.
- **Clear Security Boundary**: Keycloak handles identity protocol plumbing; Cerbos handles business rules; application developers focus on core event management features.

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
