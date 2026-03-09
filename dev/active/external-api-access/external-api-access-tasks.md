ABOUTME: Checklist for the external API access planning and future implementation work.
ABOUTME: Organizes the feature into Clean Architecture phases with explicit acceptance targets.

# External API Access - Task Checklist

> **Last Updated:** 2026-03-09

## Planning Package ✅ COMPLETE

- [x] Read required docs, active task context, and relevant skills
- [x] Verify current tenant-resolution, auth, rate-limiting, and admin-boundary files
- [x] Create `external-api-access-plan.md`
- [x] Create `external-api-access-context.md`
- [x] Create `external-api-access-tasks.md`
- [x] Fold in the final background verification and research results

## Phase 0 - Pipeline ADR And Spike 🟡 IN PROGRESS

- [x] Write ADR for authentication plus tenant-resolution request flow
  - Acceptance: fixes JWT bearer + custom API-key handler + policy-scheme dispatch, dedicated `X-API-Key` usage, proxy trust rules, and fail-closed semantics
- [x] Prove one direct JWT request path with explicit tenant context
  - Acceptance: JWT caller can authenticate and resolve tenant through the agreed split-phase flow
- [x] Prove one API-key request path with tenant derived from the key
  - Acceptance: API-key caller does not rely on caller-supplied tenant authority and wrong-tenant hints are rejected deterministically
- [x] Prove the single-tenant fast path
  - Acceptance: no tenant-specific caller material is required in single-tenant mode
- [x] Harden host-derived tenant resolution behind explicit forwarded-header trust
  - Acceptance: API host processes forwarded host/IP only when `ForwardedHeadersTrust` defines a trusted proxy boundary; Phase 0 verification covers direct-host and trust-option behavior

## Phase 1 - Domain Design ⏳ NOT STARTED

- [x] Create external API-key aggregate with tenant binding
  - Acceptance: secret is never stored in plaintext; key supports tenant binding, owner type, expiry, revoke, rotate, and last-used metadata; credential data remains separate from runtime principal construction
- [x] Create owner-type and lifecycle enums or value objects
  - Acceptance: supports at least `User` and `Organization` ownership and explicit active or revoked states
- [ ] Define v1 scope catalog
  - Acceptance: covers read, write, and sensitive/private access boundaries without exceeding existing user or org authority ceilings
- [ ] Define quota and rate-limit policy defaults
  - Acceptance: every key gets default throttling and usage policy settings; node-local versus cluster-shared semantics are explicit

## Phase 2 - Application Layer 🟡 IN PROGRESS

- [x] Add repository contracts for API-key lifecycle and auth lookup
  - Acceptance: repositories return entities only and support prefix or public-id lookup without exposing secret material
- [x] Add repository contract for persisted auth lookup
  - Acceptance: API host can load a persisted API key by stable public key id before tenant context exists
- [ ] Add CQRS commands and queries for create, list, rotate, revoke, and update policy
  - Acceptance: handlers follow existing MediatR structure and validators are manually instantiated
- [x] Add initial CQRS commands and queries for create, list, and revoke
  - Acceptance: the first management slice exposes create, list, and revoke flows through thin controllers and MediatR handlers
- [x] Add policy-update CQRS flow for persisted keys
  - Acceptance: callers can update editable policy fields on visible keys without changing owner binding or exposing secret material
- [x] Add detail query for a single persisted key
  - Acceptance: callers can fetch safe metadata for one visible key, and non-visible keys fail closed as not found
- [x] Add DTOs for safe key management responses
  - Acceptance: DTOs never expose hashed secrets or full secret values after creation
- [x] Add initial owner- and tenant-aware authorization rules
  - Acceptance: user keys and organization keys cannot exceed existing authority boundaries; disabled owners or disabled tenants invalidate dependent keys immediately
- [ ] Define claims or principal contract for authenticated API-key callers
  - Acceptance: principal shape can flow through the existing authorization pipeline and future Cerbos evaluation; claims include auth method, key id, tenant id, owner type, and owner id
- [x] Define shared principal helper for authenticated API-key callers
  - Acceptance: claim parsing is centralized for middleware, diagnostics, and later authorization work

## Phase 3 - Persistence Layer ⏳ NOT STARTED

- [x] Add EF Core entity configuration and indexes
  - Acceptance: hashed secret lookup path, public prefix, tenant binding, and ownership columns are configured explicitly; rotation overlap support is either implemented or explicitly rejected
- [x] Create EF Core migration(s)
  - Acceptance: migration applies cleanly and matches the final entity model
- [x] Implement persistence repositories
  - Acceptance: auth lookup is efficient and normal tenant-filter behavior is preserved by default
- [ ] Add usage rollup or audit storage strategy
  - Acceptance: supports per-key and per-tenant reporting without exposing tenant secret material
- [x] Add throttled persisted-key usage metadata updates
  - Acceptance: successful persisted-key auth updates `LastUsedAt` and `LastUsedIp` without forcing a blocking write on every request

## Phase 4 - API Auth And Tenant Validation Seam 🟡 IN PROGRESS

- [ ] Add JWT bearer + custom API-key schemes with policy-scheme dispatch
  - Acceptance: Bearer remains JWT-only, machine callers use `X-API-Key`, and authentication stays in ASP.NET Core auth rather than controller logic
- [ ] Implement split-phase tenant validation flow
  - Acceptance: API-key callers derive tenant from the key; direct JWT callers use documented host or slug contract; unresolved and wrong-tenant requests fail closed consistently
- [ ] Add reverse-proxy trust handling for host-derived tenancy
  - Acceptance: trusted proxies/networks are explicit, forwarded-host mismatch semantics are documented, and proxy-aware tests exist
- [x] Back the Phase 0 API-key auth seam with persisted key lookup
  - Acceptance: API-key handler can authenticate stored `keyId.secret` credentials via repository lookup while keeping the Phase 0 config fallback intact for seam tests

## Phase 5 - Rate Limiting And Observability 🟡 IN PROGRESS

- [x] Add per-key rate-limit partitions
  - Acceptance: noisy API keys are isolated from each other instead of sharing only IP or user buckets; expensive endpoint classes can be throttled separately if needed
- [x] Add initial API-key metrics and audit-style logs for create, revoke, auth outcomes, tenant mismatch, and throttling
  - Acceptance: bounded counters and structured logs exist for the currently implemented lifecycle and request-flow slices without exposing raw secrets; targeted unit and integration tests stay green
- [ ] Add API-key metrics and audit events
  - Acceptance: metrics include safe dimensions such as `tenant_id`, owner type, and outcome; secrets never appear in logs; create, reveal-once, revoke, rotate, success, failure, expired-use, wrong-tenant, and throttle events are auditable
- [ ] Define clustered deployment semantics for throttling and quotas
  - Acceptance: self-hosters can tell whether enforcement is node-local or requires a future shared quota design

## Phase 6 - Management APIs And Platform Visibility 🟡 IN PROGRESS

- [x] Add initial management endpoints
  - Acceptance: tenant-scoped and platform-scoped endpoints are clearly separated and controller logic remains thin
- [x] Add policy-update management endpoint
  - Acceptance: `PUT /api/externalapikey/{id}` updates mutable policy fields and returns not found for non-visible keys
- [ ] Add OpenAPI and contract docs for direct callers
  - Acceptance: examples cover single-tenant, multi-tenant JWT, and multi-tenant API-key flows
- [ ] Add instance-admin metadata reporting
  - Acceptance: platform admins can see counts and trends without viewing tenant tokens or tenant business data

## Phase 7 - Blazor Admin UX ⏳ NOT STARTED

- [ ] Add tenant and organization API-key management views
  - Acceptance: secrets are shown once, then replaced with safe metadata-only views
- [ ] Add instance-admin visibility views
  - Acceptance: platform ops pages show metadata only and respect single-tenant versus multi-tenant UX rules

## Phase 8 - Cerbos, Tests, And Docs ⏳ NOT STARTED

- [ ] Extend authorization policy integration for machine principals
  - Acceptance: machine principals can be evaluated consistently by local or Cerbos-backed authorization
- [ ] Add unit tests
  - Acceptance: revoked, expired, malformed, and scope-limited key flows are covered
- [ ] Add integration tests
  - Acceptance: direct JWT and API-key access paths pass in both single-tenant and multi-tenant modes, including proxy-aware forwarded-host scenarios
- [ ] Add rate-limit tests
  - Acceptance: per-key throttling produces correct 429 behavior and does not regress current user or IP policy behavior
- [ ] Update docs
  - Acceptance: `docs/API.md`, `docs/SECURITY.md`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, and `docs/ADMIN_HIERARCHY.md` reflect the final design

## Verification Notes

- [x] Clear the unrelated architecture-test blocker
  - Acceptance: non-CQRS analytics payload records no longer end with `Request`, and `Event.Architecture.Tests` passes again
- [x] Centralize API-key principal claim parsing
  - Acceptance: middleware and diagnostics probe consume one shared API-key principal helper, and targeted Phase 0 seam tests still pass
- [x] Verify persisted-key policy updates end to end
  - Acceptance: targeted integration coverage proves owner update success, unauthorized not found behavior, and no regression in Phase 0 seam tests
- [x] Verify persisted-key detail queries end to end
  - Acceptance: targeted integration coverage proves owner detail success, unauthorized not found behavior, and no regression in Phase 0 seam tests
- [x] Verify persisted-key usage metadata updates end to end
  - Acceptance: targeted seam coverage proves persisted-key auth updates `LastUsedAt`, and existing key management tests keep passing
- [x] Verify per-key throttling isolation end to end
  - Acceptance: targeted seam coverage proves one persisted API key can be throttled without blocking a different key from the same test client
- [x] Verify initial API-key observability hooks
  - Acceptance: targeted unit coverage proves creation and revocation metrics fire, and targeted integration coverage proves the management and Phase 0 seams remain green after adding auth-outcome and throttle instrumentation

## Quick Resume

- Read `dev/active/external-api-access/external-api-access-context.md` first.
- The auth-first persisted storage seam, per-key throttling, and initial observability hooks are done; continue next with reveal or rotate event coverage, usage rollups, clustered limiter semantics, docs, and metadata-only reporting.
