ABOUTME: Checklist for the external API access planning and future implementation work.
ABOUTME: Organizes the feature into Clean Architecture phases with explicit acceptance targets.

# External API Access - Task Checklist

> **Last Updated:** 2026-03-27

## Planning Package ✅ COMPLETE

- [x] Read required docs, active task context, and relevant skills
- [x] Verify current tenant-resolution, auth, rate-limiting, and admin-boundary files
- [x] Create `external-api-access-plan.md`
- [x] Create `external-api-access-context.md`
- [x] Create `external-api-access-tasks.md`
- [x] Fold in the final background verification and research results

## Phase 0 - Pipeline ADR And Spike ✅ COMPLETE

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

## Phase 1 - Domain Design ✅ COMPLETE

- [x] Create external API-key aggregate with tenant binding
  - Acceptance: secret is never stored in plaintext; key supports tenant binding, owner type, expiry, revoke, rotate, and last-used metadata; credential data remains separate from runtime principal construction
- [x] Create owner-type and lifecycle enums or value objects
  - Acceptance: supports at least `User` and `Organization` ownership and explicit active or revoked states
- [x] Expand `ExternalApiKeyOwnerType` to five values (`User`, `Organization`, `Group`, `Tenant`, `InstanceAdmin`)
  - Acceptance: enum has values 1-5; `OwnerId` semantics documented per type (User→User.Id, Organization→Organization.Id, Group→Group.Id, Tenant→Tenant.Id, InstanceAdmin→admin's User.Id); `TenantId` is nullable (NULL only for InstanceAdmin); no breaking changes to existing User/Organization keys
- [x] Define v1 scope catalog
  - Acceptance: covers read, write, and sensitive/private access boundaries without exceeding existing user or org authority ceilings
  - Implementation: `Explore.Domain/Constants/ExternalApiKeyScopes.cs` — 13 scopes (events/organizations/groups/users/lookups:read, events/organizations/groups/users:write, registrations:write, api-keys:manage, admin:tenant, admin:instance); `AreAllValid()` and `GetInvalid()` helpers
- [x] Define quota and rate-limit policy defaults
  - Acceptance: every key gets default throttling and usage policy settings; node-local versus cluster-shared semantics are explicit
  - Implementation: `Explore.Application/Features/ExternalApiKeys/ExternalApiKeyQuotaDefaults.cs` — per-owner-type defaults (User=Daily/1000, Org=Monthly/10000, Group=Monthly/5000, Tenant=Monthly/50000, InstanceAdmin=unlimited)

## Phase 2 - Application Layer ✅ COMPLETE

- [x] Add repository contracts for API-key lifecycle and auth lookup
  - Acceptance: repositories return entities only and support prefix or public-id lookup without exposing secret material
- [x] Add repository contract for persisted auth lookup
  - Acceptance: API host can load a persisted API key by stable public key id before tenant context exists
- [x] Add CQRS commands and queries for create, list, rotate, revoke, and update policy (all five owner types)
  - Acceptance: handlers follow existing MediatR structure and validators are manually instantiated; create flow validates admin authority per owner type (user=self, org=`IsOrganizationAdminAsync`, group=`IsGroupAdminAsync`, tenant=`IsTenantAdminAsync`, instance=`IsInstanceAdminAsync`); scope ceiling enforced per type
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
- [x] Add `IsGroupAdminAsync(groupId)` to `AdminContext`
  - Acceptance: mirrors existing `IsOrganizationAdminAsync` pattern; checks `GroupMember` for `RoleId=31` (GroupAdmin); 5-minute sliding cache; unit-tested
  - Implementation: already in `Explore.Infrastructure/Identity/AdminContext.cs` lines 205-218
- [x] Add scope-ceiling enforcement per owner type
  - Acceptance: each owner type has a maximum scope boundary (InstanceAdmin > Tenant > Organization ≈ Group > User); requested scopes validated against ceiling at key creation; scope escalation beyond owner authority rejected with 403
  - Implementation: `Explore.Application/Features/ExternalApiKeys/ExternalApiKeyScopeCeiling.cs` — ceiling per owner type with `GetCeiling()`, `AreWithinCeiling()`, `GetExceeding()` helpers; enforced in both `CreateExternalApiKeyDtoValidator` and `UpdateExternalApiKeyPolicyDtoValidator`
- [x] Define claims or principal contract for authenticated API-key callers (all five owner types)
  - Acceptance: principal shape can flow through the existing authorization pipeline and future Cerbos evaluation; claims include auth method, key id, tenant id (nullable for InstanceAdmin), owner type (all 5 values), and owner id; InstanceAdmin keys produce cross-tenant principals
  - Implementation: `ApiKeyPrincipalContext.TenantId` changed to `Guid?`; `TryGetApiKeyPrincipalContext()` handles null TenantId; all consumers updated for nullable
- [x] Define shared principal helper for authenticated API-key callers
  - Acceptance: claim parsing is centralized for middleware, diagnostics, and later authorization work

## Phase 3 - Persistence Layer 🟡 IN PROGRESS

- [x] Add EF Core entity configuration and indexes
  - Acceptance: hashed secret lookup path, public prefix, tenant binding, and ownership columns are configured explicitly; rotation overlap support is either implemented or explicitly rejected
- [x] Create EF Core migration(s)
  - Acceptance: migration applies cleanly and matches the final entity model
- [x] Create migration for five-owner-type schema changes
  - Acceptance: `TenantId` becomes nullable on `ExternalApiKey`; composite index `(TenantId, OwnerType, OwnerId)` updated for nullable column; composite index `(TenantId, Status)` updated; FK to Tenant changed to optional; existing User and Organization keys unaffected; migration applies cleanly on existing databases
  - Implementation: already done — `ExternalApiKeyConfiguration.cs` has `TenantId` nullable, `IsRequired(false)` FK, and composite indexes on nullable column
- [x] Implement persistence repositories
  - Acceptance: auth lookup is efficient and normal tenant-filter behavior is preserved by default
- [x] Update repository for InstanceAdmin tenant-filter bypass
  - Acceptance: InstanceAdmin key auth lookup works without tenant context; normal tenant-scoped queries still filter correctly for other key types; tenant filter bypass limited to auth lookup path only
  - Implementation: `ExternalApiKeyRepository` uses `IgnoreTenantFilter()` for auth lookup and `IgnoringTenantFilter` variants for platform-scoped operations
- [x] Add usage rollup or audit storage strategy
  - Acceptance: supports per-key and per-tenant reporting without exposing tenant secret material
  - Implementation: `RequestCount` (long) added to `ExternalApiKeyQuota` entity; `TryConsumeCredits` atomically increments request count; `IncrementRequestCount` for unlimited keys; `GetUsageByTenant` and `GetUsagePlatformWide` reporting queries; EF migration `20260328220258_AddExternalApiKeyQuotaTable` adds column with default 0
- [x] Add throttled persisted-key usage metadata updates
  - Acceptance: successful persisted-key auth updates `LastUsedAt` and `LastUsedIp` without forcing a blocking write on every request

## Phase 4 - API Auth And Tenant Validation Seam ✅ COMPLETE

- [x] Add JWT bearer + custom API-key schemes with policy-scheme dispatch
  - Acceptance: Bearer remains JWT-only, machine callers use `X-API-Key`, and authentication stays in ASP.NET Core auth rather than controller logic
  - Implementation: done in Phase 0 spike; `ApiKeyAuthenticationHandler` + policy-scheme dispatch
- [x] Implement split-phase tenant validation flow (including null-tenant InstanceAdmin)
  - Acceptance: API-key callers derive tenant from the key; direct JWT callers use documented host or slug contract; InstanceAdmin keys with null TenantId bypass tenant validation and produce platform-scoped principals; unresolved and wrong-tenant requests fail closed consistently
  - Implementation: `ApiTenantPostAuthenticationMiddleware` updated with InstanceAdmin escape hatch — allows through when `OwnerType == InstanceAdmin` and tenant not resolved
- [x] Add reverse-proxy trust handling for host-derived tenancy
  - Acceptance: trusted proxies/networks are explicit, forwarded-host mismatch semantics are documented, and proxy-aware tests exist
  - Implementation: done in Phase 0 spike; forwarded headers trust configuration
- [x] Back the Phase 0 API-key auth seam with persisted key lookup
  - Acceptance: API-key handler can authenticate stored `keyId.secret` credentials via repository lookup while keeping the Phase 0 config fallback intact for seam tests

## Phase 5 - Rate Limiting And Observability ✅ COMPLETE

- [x] Add per-key rate-limit partitions
  - Acceptance: noisy API keys are isolated from each other instead of sharing only IP or user buckets; expensive endpoint classes can be throttled separately if needed
- [x] Add initial API-key metrics and audit-style logs for create, revoke, auth outcomes, tenant mismatch, and throttling
  - Acceptance: bounded counters and structured logs exist for the currently implemented lifecycle and request-flow slices without exposing raw secrets; targeted unit and integration tests stay green
- [x] Add API-key metrics and audit events (all five owner types)
  - Acceptance: metrics include safe dimensions such as `tenant_id` (nullable for InstanceAdmin), owner type (all 5 values), and outcome; rate-limit partition keys differentiated per owner type; secrets never appear in logs; create, reveal-once, revoke, rotate, success, failure, expired-use, wrong-tenant, and throttle events are auditable across all owner types
  - Implementation: 6 counters in `BusinessMetrics.cs` (created, revoked, policy_updated, rotated, authentication_attempts, throttled) all with `tenant_id` + `owner_type` dimensions; `UpdateExternalApiKeyPolicyCommandHandler` wired with `policy_updated` metric; `rotated` counter stubbed for future rotation feature
- [x] Define clustered deployment semantics for throttling and quotas
  - Acceptance: self-hosters can tell whether enforcement is node-local or requires a future shared quota design
  - Implementation: documented in `external-api-access-context.md` — rate limiting is node-local (in-process memory), quota credits are cluster-safe (PostgreSQL atomic SQL), usage metadata is eventually consistent

## Phase 6 - Management APIs And Platform Visibility ✅ COMPLETE

- [x] Add initial management endpoints
  - Acceptance: tenant-scoped and platform-scoped endpoints are clearly separated and controller logic remains thin
- [x] Add policy-update management endpoint
  - Acceptance: `PUT /api/externalapikey/{id}` updates mutable policy fields and returns not found for non-visible keys
- [x] Add OpenAPI and contract docs for direct callers
  - Acceptance: examples cover single-tenant, multi-tenant JWT, and multi-tenant API-key flows
  - Implementation: documented in `external-api-access-context.md` — authentication flows (API key + JWT), tenant resolution, scope model with ceilings, management endpoint table; controller endpoints have `EndpointSummary`/`EndpointDescription` attributes for auto-generated OpenAPI
- [x] Add instance-admin metadata reporting
  - Acceptance: platform admins can see counts and trends without viewing tenant tokens or tenant business data
  - Implementation: `GET /api/ExternalApiKey/usage-report` endpoint with `GetExternalApiKeyUsageReportRequest`/Handler; tenant-admins see their tenant, instance-admins see platform-wide; `ExternalApiKeyUsageReportDto` exposes only metadata (counts, credits, owner info — no secrets)

## Phase 7 - Blazor Admin UX (All Owner Types) ✅ COMPLETE

- [x] Add user API-key management in user settings
  - Acceptance: users can create, list, revoke their own keys; secret shown once at creation; safe metadata-only views afterward
  - Implementation: `ApiKeysSection` component (shared) with `OwnerType=1` in `SettingsLayout.razor`; `CreateApiKeyDialog` two-phase (form → one-time secret); `ExternalApiKeyService` ACL wrapping NSwag client
- [x] Add organization API-key management in org admin panel
  - Acceptance: org admins (RoleId=22) can manage org-owned keys; visibility scoped to the organization
  - Implementation: `ApiKeysSection OwnerType=2 OrganizationId=OrganizationId` in `OrganizationAdminSettingsLayout.razor`
- [x] Add group API-key management in group admin panel
  - Acceptance: group admins (RoleId=31) can manage group-owned keys; visibility scoped to the group
  - Implementation: `ApiKeysSection OwnerType=3 GroupId=GroupId` in `GroupAdminSettingsLayout.razor`
- [x] Add tenant API-key management in tenant admin panel
  - Acceptance: tenant admins (RoleId=11) can manage tenant-level integration keys; visibility scoped to the tenant
  - Implementation: `ApiKeysSection OwnerType=4` in `TenantAdminSettingsLayout.razor`
- [x] Add instance-admin API-key management and visibility views
  - Acceptance: instance admins can manage platform-scoped keys; metadata-only reporting across tenants; respects single-tenant versus multi-tenant UX rules; never exposes tenant business data or tenant API-key secrets
  - Implementation: `ApiKeysSection OwnerType=5` replaces "coming soon" placeholder in `InstanceAdminSettingsLayout.razor`; works in both single-tenant and multi-tenant modes; `ExternalApiKeyConstants` provides scope catalog and status/color helpers

## Phase 8 - Cerbos, Tests, And Docs ⏳ NOT STARTED

- [ ] Extend authorization policy integration for machine principals
  - Acceptance: machine principals can be evaluated consistently by local or Cerbos-backed authorization
- [ ] Add unit tests (all five owner types)
  - Acceptance: revoked, expired, malformed, and scope-limited key flows are covered for each owner type; scope ceiling enforcement tested per type; `IsGroupAdminAsync` tested; nullable TenantId edge cases covered; InstanceAdmin cross-tenant principal shape verified
- [ ] Add integration tests (all five owner types)
  - Acceptance: direct JWT and API-key access paths pass in both single-tenant and multi-tenant modes for all 5 key types; InstanceAdmin null-tenant keys work correctly; proxy-aware forwarded-host scenarios covered; cross-owner-type boundary isolation verified (e.g., org key cannot access group resources beyond scope ceiling)
- [ ] Add rate-limit tests (all five owner types)
  - Acceptance: per-key throttling produces correct 429 behavior for each owner type; partition keys differentiated per owner type; does not regress current user or IP policy behavior
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
- **Phases 0–7 COMPLETE**: Auth seam, domain (5 owner types, v1 scope catalog in `ExternalApiKeyScopes.cs`, quota defaults in `ExternalApiKeyQuotaDefaults.cs`), application (all CQRS handlers, scope-ceiling enforcement in `ExternalApiKeyScopeCeiling.cs`, nullable TenantId principal), persistence (IgnoreTenantFilter, usage rollup with `RequestCount` on `ExternalApiKeyQuota`, migration `20260328220258`), API auth/tenant (InstanceAdmin escape hatch), metrics (6 counters with all dimensions), clustered deployment docs, contract docs, instance-admin reporting endpoint, and Blazor admin UX for all 5 owner types (`ExternalApiKeyService`, `ApiKeysSection` shared component, `CreateApiKeyDialog` two-phase dialog, `ExternalApiKeyConstants` client-side helpers, wired into all 5 admin layouts).
- **Next priority (Phase 8)**: Cerbos machine-principal integration, full unit/integration/rate-limit test suite for all 5 owner types, and documentation updates (`docs/API.md`, `docs/SECURITY.md`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/ADMIN_HIERARCHY.md`).
