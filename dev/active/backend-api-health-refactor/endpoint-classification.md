<!-- ABOUTME: Endpoint classification artifact for backend/API health refactor Phase 0. -->
<!-- ABOUTME: Defines endpoint auth, tenant, rate-limit, cache, and HAL classification rules. -->

# Endpoint Classification

Last Updated: 2026-06-13 Europe/Brussels

## Purpose

This artifact defines the target classification model used by `endpoint-inventory.md`, tests, OpenAPI metadata, rate limiting, cache policy, authorization, and HAL affordances.

## Endpoint Classes

| Classification | Meaning | Required Metadata | Default Auth | Tenant Mode | Rate Limit | Cache Posture |
|---|---|---|---|---|---|---|
| Public | Anonymous read of intentionally public data. | RouteName, operation ID, visibility rule, cache variance, rate-limit policy. | `[AllowAnonymous]` allowed. | `RuntimeTenantOptionalPublicRead` only under strict rules. | `anonymous-public-read` | Public cache only when tenant/host/auth/query variance is safe. |
| Authenticated | User-specific or tenant-specific read/write requiring a signed-in user. | RouteName, operation ID, auth policy/attribute, ProblemDetails responses. | `[Authorize]`. | `RuntimeTenantRequired`. | `authenticated-read` or `authenticated-write` | Usually no public cache; vary by user/tenant if cached. |
| Admin | Tenant/platform administrative action. | Capability policy, handler metadata, audit event, rate-limit policy. | `[Authorize(Policy = ...)]`. | `RuntimeTenantRequired` or `HostAdministration`. | `admin-write` or `auth-sensitive` | No shared public cache. |
| HostAdministration | Host/platform-wide operation crossing tenant boundaries. | Reason enum, operation name, audit event, explicit service API. | Host-admin capability policy. | `HostAdministration`. | `admin-write` | No public cache. |
| SetupBootstrap | First-admin/setup/onboarding operation. | Setup-secret or bootstrap policy, disablement rule, audit event. | Setup-secret or bootstrap auth path. | Host/setup-specific. | `setup_secret` or `auth-sensitive` | No public cache. |
| BackgroundSystem | Background processor/system operation, not controller-invoked. | Operation name, reason enum, structured logs, tests. | Not an API endpoint. | `BackgroundSystem`. | N/A | N/A |
| PublicIngestion | Anonymous machine/browser-submitted telemetry or relay payload. | Abuse controls, payload validation, tenant/host binding, dedicated rate-limit policy. | `[AllowAnonymous]` only when explicitly intended. | `RuntimeTenantOptionalPublicRead` or endpoint-specific tenant binding. | `PublicIngestion`, `AnalyticsRelay`, or equivalent dedicated limiter. | No output cache. |

## Required Per-Endpoint Decisions

- Public endpoints must state whether data is tenant-scoped public data, global platform-level data, or authenticated export/download data. Anonymous public endpoints must not return user IDs, user emails, user full names, roles, memberships, invitations, role grants, revocation metadata, registration identity, or tenant/member authorization metadata unless an explicit product/security decision is recorded in the risk register and enforced by tests.
- Writes must identify handler-level authorization metadata or a documented exception.
- Admin endpoints must use capability/resource/action policy names, not role-sounding placeholders.
- HAL-enabled resources must list candidate rels and fail-closed authorization behavior. UI affordances consume these rels as the source of truth; local role checks are not a valid substitute.
- Rate-limit policy must be one of: `anonymous-public-read`, `authenticated-read`, `authenticated-write`, `auth-sensitive`, `download-sensitive`, `admin-write`, `setup_secret`, `export-public-or-authenticated`.
- Cache policy must state whether the endpoint is uncached, output-cached, user-varying, tenant-varying, host-varying, or public-resource-varying.

## Current-to-Target Rate Limit Mapping

| Target Posture | Current Policy Name | Applies To | Implementation Phase |
|---|---|---|---|
| `anonymous-public-read` | `Global` today unless endpoint-specific; may need dedicated public-read limiter later. | Public GETs over public/tenant-resolved data. | Phase 0B inventory, Phase 2 metadata normalization. |
| `authenticated-read` | `Authenticated` | User/tenant reads requiring identity. | Phase 0B inventory, Phase 2 metadata normalization. |
| `authenticated-write` | `Write` | Normal create/update/delete commands. | Phase 2 result/error mapping and Phase 4 idempotency/concurrency. |
| `admin-write` | `Authenticated`/`Write` today; target may reuse `Write` with stricter policy or define admin limiter. | Tenant/host admin mutations. | Phase 1D policy replacement and Phase 2 metadata normalization. |
| `auth-sensitive` | `Authenticated` or `SetupSecret` depending endpoint. | Auth/provider config tests, API-key operations, migration/admin endpoints. | Phase 1D/1E. |
| `download-sensitive` | `Global`/`Authenticated` today depending endpoint. | Presigned URL and export/download endpoints. | Phase 1D for auth, Phase 2 for metadata. |
| `setup_secret` | `SetupSecret` | Setup-secret validation, onboarding provider writes/tests, bootstrap completion. | Phase 1E. |
| `public-ingestion` | `AnalyticsRelay` | Browser telemetry/relay submission. | Phase 0B inventory, preserve dedicated limiter unless policy changes. |
| `export-public-or-authenticated` | `Global`, `Authenticated`, or `Write` depending endpoint. | Calendar/contact/export endpoints whose visibility follows resource visibility. | Phase 2 contract normalization. |

## Current-to-Target Cache Mapping

| Current Policy Name | Target Posture | Applies To | Required Verification |
|---|---|---|---|
| `PublicData` | public-resource-varying | Stable public resources. | Vary by tenant/host/query where tenant-scoped. |
| `LookupData` | public or tenant-safe lookup cache | Lookup endpoints. | Confirm lookup is global, tenant-specific, or host-specific. |
| `ListData` | public/tenant/query-varying list cache | Event/list/discovery endpoints. | Confirm tenant, auth, filters, query, route, and visibility variance. |
| `DetailData` | public/tenant/resource-varying detail cache | Detail endpoints. | Confirm private/user-specific resources are not shared cached. |
| `TenantNav` | tenant/host-varying | Navigation links/config. | Confirm tenant/host variance and HAL/action gating. |
| `PublicExperienceShell` | tenant/host/query-varying | Public experience shell/settings. | Confirm no user-specific data and safe tenant resolution. |
| `SystemConfig` | host/setup-safe cache | Onboarding/system config reads. | Confirm no secret/provider details leak. |
| `SitemapData` | public-resource-varying | Sitemap endpoints. | Confirm only public, indexable resources. |
| none | uncached | Mutations, presigned URLs, admin writes, sensitive exports. | Verify no output cache metadata is present. |

## Phase 0B Guardrail Targets

- Endpoint classification metadata exists for every operation.
- Endpoint classification aligns with `[AllowAnonymous]`, `[Authorize]`, policy names, and handler metadata.
- Public endpoints with tenant-scoped data declare `RuntimeTenantOptionalPublicRead` rationale.
- Admin/setup/download-sensitive endpoints declare explicit rate-limit policy.
- Public ingestion endpoints declare abuse controls and a dedicated limiter rather than inheriting generic anonymous read semantics.

## Documented Anonymous Mutating Exceptions

These endpoints intentionally accept unauthenticated POST traffic because another control is the authentication boundary. They remain build-enforced exceptions and must keep narrow payloads, abuse controls, safe response bodies, and explicit tests.

| Endpoint | Reason | Required Control |
|---|---|---|
| `InstanceOnboardingController.ValidateSecret` | Bootstrap/setup secret validation before a tenant admin identity exists. | Setup-secret validation path and no secret echo. |
| `AnalyticsRelayController.Relay` | Anonymous browser analytics relay. | Dedicated analytics relay rate limit and relay payload validation. |
| `EmailUnsubscribeController.Post` | One-click unsubscribe callback. | Signed/tokenized unsubscribe identity and no privileged mutation surface. |
| `IncomingWebhooksController.RecordSvixOperationalCallback` | Svix operational callback where Svix-compatible signature headers are the authentication mechanism. | Raw body verification against `svix-id`, `svix-timestamp`, and `svix-signature`; bounded body size; rate limit; optional ledger capture only after verification. |

## 2026-06-13 Audit Reclassification Queue

These endpoint families are not allowed to remain generic `Public` without DTO splitting or explicit approval. The 2026-06-13 Phase 1P first slice chose the fail-closed option for all three families: protect the identity-bearing read endpoints now, then decide later whether any safe public projection is needed.

- `EventRegistration` reads: now authenticated and self-scoped in code because registration identity/state fields make public or arbitrary-user responses unsafe. Generic read DTO schemas no longer expose user identity fields. Follow-up: add a separate event/session organizer projection only if product needs attendee management, guarded by resource authorization and HAL affordances.
- `TenantUserRoleGrant` reads: now authenticated and tenant-admin/resource protected. The identity-bearing grant DTOs remain administrative contracts; list/detail requests carry tenant-scoped `ISecureRequest` metadata, Cerbos denies regular authenticated `view`, local fallback allows tenant admins only for the resolved tenant plus instance admins, and OpenAPI/client metadata was regenerated.
- `OrganizationMember` reads: now authenticated and organization-resource protected. The identity-bearing member DTO remains an administrative contract; list/detail requests carry `ISecureRequest` metadata for `ResourceKinds.OrganizationMember` and `AuthorizationActions.OrganizationMembers.View`, direct member-id reads are enriched with tenant/organization/user attributes before provider evaluation, Cerbos and local fallback deny regular authenticated `view`, and OpenAPI/client metadata was regenerated.
- `Footer` writes: now authenticated plus tenant-resource protected. Footer link-group/link/reorder/settings commands carry the resolved tenant id through `ISecureRequest`, authorize as `ResourceKinds.Tenant` with `AuthorizationActions.Update`, local fallback allows tenant admins only for the ambient tenant plus instance admins, and regular authenticated users are denied.

If product requirements later need anonymous access for any of these families, the endpoint must use a separate safe public DTO and API tests must prove only approved fields are returned.
