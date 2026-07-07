<!-- ABOUTME: Future ISLAMU Identity Project boundary decisions for account authority, PDS hosting, and identity lifecycle emails. -->
<!-- ABOUTME: Clarifies that credential-token authorities own identity emails while ISLAMU Event owns product notification emails. -->

# Future ISLAMU Identity Project — Boundary Decisions

Last Updated: 2026-07-06 Europe/Brussels

## 1. Purpose

This document captures future-facing architectural decisions for an ISLAMU Identity Project that may provision ISLAMU-managed accounts, operate ATProto/PDS hosting, and coordinate account mappings for ISLAMU Event.

It is deliberately a boundary document, not an implementation plan. Its job is to prevent future agents from collapsing three different responsibilities into one vague “identity service sends email” design:

1. Account authority: creates, verifies, and secures credential/account tokens.
2. Identity orchestration: provisions, maps, audits, and coordinates accounts across authorities.
3. Product notification: sends ISLAMU Event business-state notifications with local audit and preferences.

## 2. CTO Rule

The account authority that owns, creates, and verifies the credential or security token owns the identity lifecycle email.

That rule means:

| Scenario | Identity email owner | Notes |
| --- | --- | --- |
| Keycloak email verification, password reset, required action, email update | Keycloak | ISLAMU may theme Keycloak or inject SMTP in self-hosted deployments, but Keycloak owns the token and email. |
| External ATProto/PDS account email confirmation, password reset, account migration/security email | External PDS/account authority | ISLAMU is relying party/client and must not send provider credential emails. |
| Future ISLAMU-operated PDS account confirmation, password reset, email update, migration/security email | The ISLAMU-operated PDS cell | The PDS cell owns the account token and sends the PDS lifecycle email, even if ISLAMU operates the infrastructure. |
| ISLAMU Event product, event, registration, moderation, reporting, tenant invite email | ISLAMU Event notification subsystem | These are business-state notifications, not credential-token lifecycle emails. |
| Provider-console or reviewer workflow email | External workflow provider | Example: Coop reviewer assignment/admin notification. |

Shared SMTP credentials do not change ownership. SMTP is transport. Ownership follows token authority and business lifecycle ownership.

## 3. Account Authority vs Product Domain

An account authority owns credential facts: account creation, password reset, email confirmation, migration confirmation, security notices, recovery codes, and token verification. ISLAMU Event owns product facts: event publication, registration approval, moderation decision, report receipt, tenant invite, organizer verification, and user notification preferences.

The future Identity Project must keep this boundary explicit:

- Identity lifecycle email is routed to `NotificationOwnership.AccountAuthority` or equivalent.
- Product lifecycle email is routed to `NotificationOwnership.IslamuEvent` or equivalent.
- External workflow provider internal email is routed to `NotificationOwnership.ExternalWorkflowProvider` or equivalent.
- User-facing external-provider email for ISLAMU users remains disabled unless explicitly delegated with local audit.

## 4. PDS Hosting Platform Model

Default future topology is an ISLAMU PDS Hosting Platform with multi-account PDS cells, shards, or clusters.

Do not assume “one PDS per user” as the baseline. The official Bluesky PDS documentation gives sizing guidance for a PDS hosting multiple users, and the AT Protocol account model treats a PDS as the host for accounts that can migrate between hosting providers or instances.

Recommended default model:

1. `pds_cells` represent deployable PDS capacity units.
2. Each cell hosts many accounts, subject to capacity, region, tenant policy, and operational health.
3. The Identity Project selects a cell during provisioning.
4. The selected PDS cell creates/verifies the account and owns PDS credential lifecycle emails.
5. Dedicated PDS cells are an advanced option for premium, organization, sovereign, regulatory, or hard-isolation requirements, not the default account model.

The PDS cell may use shared SMTP infrastructure in development or self-hosted deployments, but PDS lifecycle emails remain PDS-owned because the PDS owns the account credential flow.

## 5. What The Identity Microservice Owns

The future ISLAMU Identity Microservice may own:

- Signup policy and eligibility checks before account creation.
- User-facing account provisioning orchestration.
- PDS cell selection and capacity policy.
- Handle reservation and mapping coordination.
- Mapping ISLAMU users to Keycloak subjects, PDS DIDs, handles, and account authority records.
- Delegation audit when ISLAMU initiates an account-authority action.
- Lifecycle status projection for ISLAMU systems.
- Recovery/support workflows that request action from the account authority.

It may not own:

- PDS password-reset token generation.
- PDS email-confirmation code generation.
- PDS migration-confirmation code generation.
- PDS credential email body generation.
- Keycloak required-action token generation.
- Product/event/moderation notification delivery for ISLAMU Event.

The Identity Microservice orchestrates and audits. The account authority mints and verifies credential tokens.

## 6. Component Boundaries

| Component | Owns | Must not own |
| --- | --- | --- |
| ISLAMU Event | Product/event/registration/moderation/reporting notification decisions, local notification intent, product email audit, preferences, tenant branding | Keycloak/PDS credential-token emails |
| ISLAMU Identity Microservice | Account provisioning orchestration, account mapping, PDS cell allocation, account-authority delegation audit | PDS or Keycloak credential-token generation/email content |
| Keycloak | Keycloak account lifecycle tokens and emails | ISLAMU Event product emails |
| ISLAMU-operated PDS cell | ATProto/PDS account, repo host, auth/session/account lifecycle emails | ISLAMU Event product emails or generic marketing emails |
| External PDS/account authority | External ATProto account lifecycle | ISLAMU product notifications unless explicitly delegated and audited |
| Coop/Osprey/provider | Internal workflow/provider-console notifications | Raw user-facing ISLAMU moderation emails unless explicitly delegated and locally audited |

## 7. Future Signup Flow

An ISLAMU-operated PDS signup should follow this shape:

1. User starts signup in ISLAMU-controlled UI.
2. Identity Microservice validates policy, terms, locale, tenant/org constraints, abuse checks, and desired handle.
3. Identity Microservice selects a PDS cell using capacity, region, tenant policy, health, and migration constraints.
4. Identity Microservice asks the selected PDS cell to create the account.
5. PDS cell creates account authority state, repository/auth state, DID/handle binding as appropriate, and sends PDS-owned email confirmation through PDS SMTP configuration.
6. Identity Microservice stores account mapping and delegation audit.
7. ISLAMU Event consumes identity claims/mapping through approved auth/BFF/API paths and continues to send product-domain notifications through ISLAMU notification/outbox flow.

If signup fails after PDS account creation but before local mapping, remediation must be an audited account-authority reconciliation workflow, not a silent local email retry.

## 8. Identity Email vs Notification Email

Identity email proves or protects account authority state. Notification email communicates product/business state.

Examples of identity email:

- Verify this email address for your PDS account.
- Reset your PDS password.
- Confirm account migration to another PDS.
- Complete a Keycloak required action.

Examples of notification email:

- Your event registration was approved.
- A tenant invited you to join.
- Your report was received.
- A moderation decision was made.

If an ATProto/PDS account does not expose a verified email to ISLAMU Event, ISLAMU Event must request an app-level notification email or use in-app notifications. A product notification email address is not the same thing as a PDS identity email address.

## 9. Candidate Data Model

These names are planning candidates. Re-check existing code before implementation.

### 9.1 AccountAuthorityKind

Candidate enum values:

- `Keycloak`
- `AtprotoPds`
- `IslamuOperatedPds`
- `ExternalOidc`
- `LocalIdentity`

### 9.2 identity_accounts

Tracks local mapping from ISLAMU users to account authority identifiers.

Candidate fields:

- `id`
- `user_id`
- `account_authority_kind`
- `authority_instance_id`
- `subject`
- `did`
- `handle`
- `pds_cell_id`
- `status`
- `created_at`
- `updated_at`

### 9.3 identity_lifecycle_delegations

Records ISLAMU-initiated account-authority actions without claiming ownership of the account email.

Candidate fields:

- `id`
- `user_id`
- `account_authority_kind`
- `action_kind`
- `authority_request_id`
- `safe_payload_hash`
- `status`
- `requested_by_user_id`
- `requested_at`
- `completed_at`
- `failure_category`

### 9.4 user_notification_addresses

Stores app-level product notification addresses when account-authority email is missing, unavailable, or not appropriate for product email.

Candidate fields:

- `id`
- `user_id`
- `email_address_ciphertext`
- `verification_status`
- `purpose`
- `tenant_id`
- `created_at`
- `updated_at`

### 9.5 pds_cells

Tracks ISLAMU-operated PDS hosting capacity units.

Candidate fields:

- `id`
- `name`
- `base_url`
- `region`
- `capacity_limit`
- `allocated_account_count`
- `tenant_policy`
- `status`
- `smtp_profile_id`
- `health_status`
- `created_at`
- `updated_at`

## 10. Operations And Security Non-Goals

Do not implement these without a separate architecture review:

- One PDS container/database/process per user as the default.
- Identity Microservice-generated PDS reset links or email-confirmation codes.
- ISLAMU Event using PDS SMTP as a general product email sender.
- A global `emails.provider = Keycloak` or `emails.provider = PDS` switch.
- Cross-tenant PDS cell assignment without explicit tenant policy and audit.
- Logging raw credential-token email payloads, confirmation codes, reset links, or PDS SMTP secrets.

Required operational safeguards for future implementation:

- PDS cell health and capacity metrics.
- Delegation audit for every ISLAMU-initiated account-authority action.
- Secret-scoped SMTP configuration per account authority/delivery subsystem.
- Clear runbooks for PDS migration, failed account creation, and local mapping reconciliation.
- Data-minimized support views that do not expose credential tokens, reset links, or raw email bodies.

## 11. Source Evidence

| Source | Evidence |
| --- | --- |
| Official Bluesky PDS README | PDS hosts Personal Data Server, can federate, documents multi-user sizing guidance, and requires SMTP configuration for email verification and other PDS emails. |
| AT Protocol Account specification | Active accounts live on a PDS; PDS provides repository hosting, authorization/authentication, blob storage, and accounts can migrate between PDS hosting providers/instances. |
| `com.atproto.server.requestEmailConfirmation` lexicon | PDS/account API requests an email code to confirm ownership of email. |
| `com.atproto.server.requestPasswordReset` lexicon | PDS/account API initiates password reset via email. |
| `dev/active/email-responsibility-architecture/email-responsibility-architecture-plan.md` | Email responsibility plan defines AccountAuthority ownership and keeps ISLAMU product notification ownership separate. |

## 12. Open Questions

1. What is the first supported ISLAMU-operated PDS deployment target: single shared cell, tenant-scoped cells, or region-scoped cells?
2. Which operational SLOs define when a PDS cell is full, unhealthy, or eligible for account migration?
3. Will the Identity Project own its own bounded context/database, or start as a module in the current platform before extraction?
4. Which account authority identifiers become canonical for ISLAMU Event authorization: Keycloak subject, DID, handle, local user id, or an explicit identity account id?
5. Which support workflows may trigger account-authority emails, and what local delegation audit fields are mandatory?
6. When is a dedicated PDS cell justified, and who approves that cost/security tradeoff?
