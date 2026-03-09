ABOUTME: Checklist for the external API access planning and future implementation work.
ABOUTME: Organizes the feature into Clean Architecture phases with explicit acceptance targets.

# External API Access - Task Checklist

> **Last Updated:** 2026-03-08

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

## Phase 1 - Domain Design ⏳ NOT STARTED

- [ ] Create external API-key aggregate with tenant binding
  - Acceptance: secret is never stored in plaintext; key supports tenant binding, owner type, expiry, revoke, rotate, and last-used metadata; credential data remains separate from runtime principal construction
- [ ] Create owner-type and lifecycle enums or value objects
  - Acceptance: supports at least `User` and `Organization` ownership and explicit active or revoked states
- [ ] Define v1 scope catalog
  - Acceptance: covers read, write, and sensitive/private access boundaries without exceeding existing user or org authority ceilings
- [ ] Define quota and rate-limit policy defaults
  - Acceptance: every key gets default throttling and usage policy settings; node-local versus cluster-shared semantics are explicit

## Phase 2 - Application Layer ⏳ NOT STARTED

- [ ] Add repository contracts for API-key lifecycle and auth lookup
  - Acceptance: repositories return entities only and support prefix or public-id lookup without exposing secret material
- [ ] Add CQRS commands and queries for create, list, rotate, revoke, and update policy
  - Acceptance: handlers follow existing MediatR structure and validators are manually instantiated
- [ ] Add DTOs for safe key management responses
  - Acceptance: DTOs never expose hashed secrets or full secret values after creation
- [ ] Add owner- and tenant-aware authorization rules
  - Acceptance: user keys and organization keys cannot exceed existing authority boundaries; disabled owners or disabled tenants invalidate dependent keys immediately
- [ ] Define claims or principal contract for authenticated API-key callers
  - Acceptance: principal shape can flow through the existing authorization pipeline and future Cerbos evaluation; claims include auth method, key id, tenant id, owner type, and owner id

## Phase 3 - Persistence Layer ⏳ NOT STARTED

- [ ] Add EF Core entity configuration and indexes
  - Acceptance: hashed secret lookup path, public prefix, tenant binding, and ownership columns are configured explicitly; rotation overlap support is either implemented or explicitly rejected
- [ ] Create EF Core migration(s)
  - Acceptance: migration applies cleanly and matches the final entity model
- [ ] Implement persistence repositories
  - Acceptance: auth lookup is efficient and normal tenant-filter behavior is preserved by default
- [ ] Add usage rollup or audit storage strategy
  - Acceptance: supports per-key and per-tenant reporting without exposing tenant secret material

## Phase 4 - API Auth And Tenant Validation Seam ⏳ NOT STARTED

- [ ] Add JWT bearer + custom API-key schemes with policy-scheme dispatch
  - Acceptance: Bearer remains JWT-only, machine callers use `X-API-Key`, and authentication stays in ASP.NET Core auth rather than controller logic
- [ ] Implement split-phase tenant validation flow
  - Acceptance: API-key callers derive tenant from the key; direct JWT callers use documented host or slug contract; unresolved and wrong-tenant requests fail closed consistently
- [ ] Add reverse-proxy trust handling for host-derived tenancy
  - Acceptance: trusted proxies/networks are explicit, forwarded-host mismatch semantics are documented, and proxy-aware tests exist

## Phase 5 - Rate Limiting And Observability ⏳ NOT STARTED

- [ ] Add per-key rate-limit partitions
  - Acceptance: noisy API keys are isolated from each other instead of sharing only IP or user buckets; expensive endpoint classes can be throttled separately if needed
- [ ] Add API-key metrics and audit events
  - Acceptance: metrics include safe dimensions such as `tenant_id`, owner type, and outcome; secrets never appear in logs; create, reveal-once, revoke, rotate, success, failure, expired-use, wrong-tenant, and throttle events are auditable
- [ ] Define clustered deployment semantics for throttling and quotas
  - Acceptance: self-hosters can tell whether enforcement is node-local or requires a future shared quota design

## Phase 6 - Management APIs And Platform Visibility ⏳ NOT STARTED

- [ ] Add management endpoints
  - Acceptance: tenant-scoped and platform-scoped endpoints are clearly separated and controller logic remains thin
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

## Quick Resume

- Read `dev/active/external-api-access/external-api-access-context.md` first.
- If implementation starts, begin with Phase 0 before touching persistence, controllers, or UI.
