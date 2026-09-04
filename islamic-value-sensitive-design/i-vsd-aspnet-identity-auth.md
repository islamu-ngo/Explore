<!-- ABOUTME: I-VSD planning assessment for ASP.NET Core Identity embedded authentication provider. -->
<!-- ABOUTME: Evaluates data sovereignty, credential stewardship, abuse defense, and transparent provider selection. -->

# ASP.NET Core Identity Authentication Provider — I-VSD Planning Assessment

Last Updated: 2026-09-03 Europe/Brussels

## Review Metadata

- Mode: planning
- Subject: ASP.NET Core Identity embedded JWT authentication provider
- Workstream: `aspnet-identity-auth`
- Report kind: provider-responsibility implementation assessment
- Report status: **stale — pending revalidation after CTO-mandated architectural rewrite**
- Disposition: **stale — revalidation required**
- Evidence cutoff: 2026-09-03
- Reviewed input revision: `local-working-tree` (pre-CTO-rewrite revision)
- Reviewed plan triad revision: `local-working-tree` (pre-CTO-rewrite revision — plan has been materially rewritten)
- Implementation evidence: pending implementation
- Supersedes: none
- **Revalidation triggers fired:**
  1. Runtime provider switching changes provider authority (operator can change without restart)
  2. Two-axis multi-provider coexistence changes user defaults/consent model (ATProto independently toggleable)
  3. Session continuity during transitions changes failure behavior
- **Required revalidation actions:**
  1. Add `IVSD-F005` — Runtime Provider Transition Safety (operator switching must not orphan sessions or lock out administrators)
  2. Update `IVSD-F004` — Cover two-axis provider model and mutual exclusion enforcement
  3. Re-verify all existing finding-to-scenario/task mappings against revised plan
  4. Set disposition to `plan-aligned` after revalidation against revised plan triad

## Scope

This assessment covers the provider-controlled design of an embedded authentication provider implemented using ASP.NET Core Identity, issuing and validating JWT tokens for standalone, single-container, and lightweight deployments. It evaluates the addition of `AUTHENTICATION_PROVIDER=local` alongside existing Keycloak (OIDC) and ATProto (OAuth) options, the persistence topology (`IDENTITY_DATABASE_TOPOLOGY=colocated` in primary `ExploreDbContext` by default, or `external` for dedicated databases), the onboarding and admin configuration experience, user credential storage, rate limiting, and user aggregate synchronization.

## Claim Boundary

This is provider-responsibility design reasoning, not a fatwa, Sharia certification, legal opinion, or proof that the resulting deployment is ethically sufficient. The report evaluates choices under the platform maintainer's control: identity sovereignty, password stewardship, abuse prevention, operator clarity, and failure behavior. No halal/haram or contested religious-legal conclusion is made.

## Findings

### IVSD-F001 — Data Sovereignty, Zero Infrastructure Barriers & Self-Contained Deployment

- **Lifecycle:** accepted
- **Severity:** high
- **Claim type:** provider-controlled equity, inclusivity, and stewardship
- **Principle/domain:** amanah (custody & trust), 'adl (justice & equitable access), self-determination
- **Stakeholders:** self-hosters, small grassroots organizations, mosques, non-profits, air-gapped deployments
- **Provider-controlled decision:** whether to require external enterprise servers (Keycloak), external public HTTPS domains (ATProto OAuth), or separate databases for user authentication
- **Context:** While Keycloak provides enterprise federation and ATProto provides decentralized zero-infrastructure authentication, both introduce hard prerequisites: Keycloak demands substantial server RAM and container orchestration, while ATProto OAuth rejects non-HTTPS domains outside localhost. Moreover, forcing an auxiliary database for authentication introduces operational complexity for single-container self-hosters.
- **Risk:** Excluding resource-constrained organizations or forcing them to route user authentication through external commercial identity infrastructures compromises local autonomy and data custody.
- **Mitigation (IVSD-M001):** Introduce a built-in, lightweight ASP.NET Core Identity authentication provider (`AUTHENTICATION_PROVIDER=local`). Persist identity tables directly in the primary database (`ExploreDbContext`) by default (`colocated`), while providing an optional `external` topology for enterprise operators with existing identity databases.
- **Rejected alternatives:** Requiring SQLite-backed Keycloak containers (heavy, complex orchestration for simple sites); forcing a separate database context for local deployments when the self-hoster already selected a primary database (causes unnecessary operational friction).

### IVSD-F002 — Credential Stewardship & Cryptographic Rigor

- **Lifecycle:** accepted
- **Severity:** critical
- **Claim type:** provider-controlled privacy, data integrity, and security
- **Principle/domain:** amanah (entrusted credentials), hifdh al-mal wa al-'ird (protection of property and dignity)
- **Stakeholders:** all registered users, system operators
- **Provider-controlled decision:** password hashing algorithm, salt generation, secret isolation, and credential exposure across layers
- **Context:** Storing user credentials locally shifts the security boundary from external IDPs onto the application database.
- **Risk:** Weak hashing, accidental credential exposure in logs or DTOs, or hardcoded signing keys could lead to credential theft and mass account compromise.
- **Mitigation (IVSD-M002):** Enforce ASP.NET Core Identity's standard `PasswordHasher<ApplicationUser>` (PBKDF2 HMAC-SHA512 with per-user cryptographic salt and high work factor). Isolate credentials into distinct `identity_users` tables rather than merging into the PII-separated domain `User` table. Mark password properties write-only in DTOs. Never return password hashes in API responses, logs, or traces. Resolve JWT signing keys strictly from environment secrets (`AUTHENTICATION_LOCAL_JWT_KEY` / Infisical) and reject default placeholder keys in non-development environments.
- **Rejected alternatives:** Merging password hashes into domain `User` entity (breaks the 1:1 `UserPii` privacy architecture); hand-rolled hashing (insecure).

### IVSD-F003 — Account Integrity & Abuse Defense

- **Lifecycle:** accepted
- **Severity:** high
- **Claim type:** provider-controlled abuse resistance and failure safety
- **Principle/domain:** raf' al-haraj (prevention of harm), 'adl (fairness)
- **Stakeholders:** system users, platform operators
- **Provider-controlled decision:** brute-force defense, lockout policies, rate limiting, and credential verification latency
- **Context:** Public authentication endpoints (`/auth/local/login`, `/api/auth/local/login`) are prime targets for automated credential stuffing and dictionary attacks.
- **Risk:** Denial of service, account lockouts of legitimate users, or successful unauthorized access via brute force.
- **Mitigation (IVSD-M003):** Apply ASP.NET Core Identity's built-in lockout mechanism (`LockoutOnFailure` with configurable attempts and duration). Apply ASP.NET Core fixed/sliding window rate limiting policies (`write` rate limiter) to all local auth endpoints. Return uniform error messages (RFC 7807 ProblemDetails) that do not leak whether an email exists or whether only the password was incorrect.
- **Rejected alternatives:** Unlimited retry attempts (vulnerable to dictionary attack); verbose account enumeration error messages (exposes user existence).

### IVSD-F004 — Transparent Provider Selection & Domain Synchronization Integrity

- **Lifecycle:** accepted
- **Severity:** medium
- **Claim type:** provider-controlled transparency and architectural correctness
- **Principle/domain:** sidq (truthfulness), amanah (consistency)
- **Stakeholders:** instance administrators, onboarding operators, authenticated users
- **Provider-controlled decision:** deployment configuration precedence, onboarding UI presentation, and synchronization into the domain `User` aggregate
- **Context:** When multiple authentication providers exist, user identity accounts must synchronize deterministically into domain `User`, `UserPii`, and `Actor` aggregates without collision.
- **Risk:** Ambiguous configuration, accidental provider takeover, or orphan identity records disconnected from domain permissions.
- **Mitigation (IVSD-M004):** Make `AUTHENTICATION_PROVIDER` explicit in `Event.Setup.Core` with clear precedence (deployment-managed environment variable vs. application onboarding choice). Synchronize local authenticated users into the core domain using `SyncUserCommandHandler` with `AuthProvider = "local"` and `AuthProviderId = user.Id.ToString()`, ensuring consistent tenant and actor mapping.
- **Rejected alternatives:** Implicitly falling back between providers on error (violates fail-closed security invariants); maintaining divergent user models for local vs external users (violates Clean Architecture and tenant isolation).

## Planning Handoff

| Finding ID | Mitigation ID | Target Scenario | Target Tasks | Disposition |
|---|---|---|---|---|
| `IVSD-F001` | `IVSD-M001` | Scenario 3.1A, Scenario 3.1B | Tasks 1.1, 1.2, 2.1, 2.2 | Plan-aligned |
| `IVSD-F002` | `IVSD-M002` | Scenario 3.2A, Scenario 3.2B | Tasks 2.1, 2.3, 4.1 | Plan-aligned |
| `IVSD-F003` | `IVSD-M003` | Scenario 3.3A, Scenario 3.3B | Tasks 4.2, 5.1 | Plan-aligned |
| `IVSD-F004` | `IVSD-M004` | Scenario 3.4A, Scenario 3.4B | Tasks 1.3, 3.2, 6.1, 7.1 | Plan-aligned |
