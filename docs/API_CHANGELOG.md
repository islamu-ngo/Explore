ABOUTME: API change log aligned with the current repository state and versioning in source.
ABOUTME: Keeps release notes short and focused on externally observable API behavior.

# API Changelog

## Breaking Change Evidence

Intentional breaking API changes must add an entry to this file in the same PR that changes `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, or `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.

Each breaking-change entry must include:

- affected route, operation, schema, or generated client method;
- old behavior and new behavior;
- affected clients or operator workflows;
- migration guidance or compatibility window;
- release version or target milestone;
- link to retained OpenAPI contract evidence from `openapi-contract-guard` when available.

Non-breaking additive changes may be summarized in the current-mainline section, but removals, renamed operations, authentication/authorization contract changes, response shape changes, problem type changes, pagination/cursor changes, and generated-client breaking changes need explicit evidence here.

## Current Mainline (API version `0.1`)

Key behavior in current code:

- Media-type API versioning (`Accept: application/json;v=0.1` or `application/hal+json;v=0.1`).
- HAL responses with `Prefer: return=minimal` support.
- Output caching policies (`LookupData`, `ListData`, `DetailData`) and HybridCache usage.
- ETag middleware for successful JSON/HAL `GET` responses with `304 Not Modified` support.
- Security headers middleware (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Content-Security-Policy`).
- Tiered rate limiting and request-timeout middleware.
- Correlation ID propagation (`X-Correlation-ID`/`X-Request-ID`) and structured request logging.
- Runtime authorization provider routing (Cerbos or local fallback).
- Footer management endpoints (11 endpoints: link groups CRUD, links CRUD, settings, reorder, governance).
- Actor appearance fields (BackgroundColor, BackgroundEffect, BannerColor, BannerPictureId, BackgroundImageId) on actor update endpoints.
- OutboxProcessor background service for reliable event dispatching with retry and dead-letter.
- Analytics relay rate limit policy (`AnalyticsRelay`) for `POST /api/a/t`.
- Event and event-session template sync endpoints for diff/apply/history workflows, including 409 ProblemDetails types `/problems/stale_sync_base` and `/problems/concurrent_update`.
- Event create/update/detail APIs split card summaries from long-form copy: `description` is capped at 150 characters for list/card UI, while `content` carries the long event body up to 5000 characters.
- Managed-provider provisioning endpoint `POST /api/managed-provider-provisioning/clients:ensure` for instance-admin/provider automation. It creates or rehydrates provider-customer tenants using `ExternalBinding`, creates tenant-local `TenantUser`/`TenantUserProfile` state, creates a tenant admin role grant, and keeps ERP customer/admin identities out of instance-admin authority.
- Breaking tenant authority contract replacement: the former tenant-member public API surface is replaced by `/api/tenant-user-role-grants`, `TenantUserRoleGrantDto`, `TenantUserRoleGrantListDto`, `CreateTenantUserRoleGrantDto`, create/revoke route names, and Cerbos resource kind `islamuevent_tenant_user_role_grant`. Role-grant updates are create/revoke flows; clients should gate create/revoke affordances from HAL `_links`.
- Idempotency keys are now bound to the original request identity. A repeated key replays only the same method/target/content/body/principal fingerprint; same-key reuse for a different write request returns `409 Conflict`.
- Breaking storage download contract change: public arbitrary-key storage routes `GET /api/storageobject/file/{fileKey}` and `GET /api/storageobject/presigned-url-by-key/{objectKey}` are removed. Clients must resolve content through metadata-backed IDs such as `GET /api/storageobject/{id}/content`, public images through `GET /api/storageobject/{id}/public`, or S3-compatible presigned reads through the ID-based compatibility endpoint.
- Blazor BFF browser uploads now use provider-neutral API upload sessions. `/bff/storage/upload-session` no longer returns or stores provider upload URLs/object keys; `/bff/storage/upload-proxy` rejects raw destinations and streams bytes to the API session content endpoint.
- Instance storage administration is now provider-neutral. `GET/PUT /api/instance/settings/storage` exposes provider policy, max upload ceilings, default tenant quota, storage delegation lock, usage, provider health, and redacted optional S3 configuration. `POST /api/instance/settings/storage/test` tests the selected provider, and `POST /api/instance/settings/storage/usage/recalculate` reconciles usage counters from storage metadata.
- Tenant storage administration is now exposed through `GET/PUT /api/tenant/settings/storage`. The endpoint returns effective tenant policy, usage, delegation lock/read-only state, and redacted optional S3 override fields; updates require tenant-admin or instance-admin authority and are rejected while instance policy locks tenant delegation or when provider/max-upload values exceed allowed ceilings.
- `/health` now includes a `storage` readiness check for the selected storage provider. Local-first deployments stay healthy without S3; S3-compatible storage is probed only when selected, and failure payloads use bounded provider/status/failure-code fields without exposing paths, endpoints, bucket names, object keys, access keys, or secrets.
- `/health` also includes `storage-reconciliation` for the API hosted drift worker. This is an operational health signal only, not a new public OpenAPI operation. The worker defaults to dry-run reporting and requires explicit mutation flags for quarantine or delete actions.
- Actor subscription endpoints added under `/api/actor-subscriptions` for authenticated current-user subscription state, subscribe, notification-level update, and unsubscribe flows. HAL affordances now expose actor and event organizer subscription actions for organization/group actors only; clients must continue gating UI actions from `_links` rather than local role or claim checks.
- Event-published organization/group actor subscriptions now produce durable in-app notifications through an internal outbox fanout path. This is not an SMTP or push delivery contract.
- Authenticated notification refresh hints are available through `GET /api/notification/stream` as `text/event-stream`. The SSE payload is minimal unread-count state only; notification rows and existing notification APIs remain the delivery source of truth.
- Authenticated AI assistant API foundation is available under `/api/ai/assistant`. It exposes bootstrap, private conversation list/detail/create, send-message with `Idempotency-Key`, run-status polling, queued/in-progress run cancellation, proposed-action confirm/reject routes, and `GET /api/ai/assistant/references` for bounded event reference search. Conversation, run, proposed-action, and reference affordances are HAL-driven (`self`, `collection`, active-state `send-message`, collection `create`, cancellable-state `cancel-run`, proposed-state `confirm-action`/`reject-action`, and reference `event` links); send-message returns the run-status polling route, confirm propagates `Idempotency-Key`, provider/proposed-action/run failures map to safe ProblemDetails, reference results omit full event content, and tool calls mutate state only after explicit confirmation. Streaming is intentionally deferred; the non-streaming polling path remains the supported fallback contract.
- Breaking error-shape cleanup: `EventAgendaItemController`, `EventSessionAgendaItemController`, and `LocationRoomController` create/update 400 responses now return RFC 7807 `ValidationProblemDetails` (`application/problem+json`) with `code`, `traceId`, `timestamp`, and optional `correlationId` instead of raw `BaseCommandResponse<Guid>` envelopes or anonymous `{ error }` payloads. Clients should read validation details from `errors` and machine-readable status from `code`.
- Breaking error-shape cleanup: `ActorKeyStoreController` and `UserAuthenticationTokenController` create/update 400 responses now return RFC 7807 `ValidationProblemDetails` (`application/problem+json`) with `code`, `traceId`, `timestamp`, and optional `correlationId` instead of raw command envelopes, anonymous `{ error }`, or ad hoc `Problem(...)` payloads. `ActorKeyStoreController` delete not-found now returns RFC 7807 `ProblemDetails` with a `resource_not_found` fallback code. Clients should read validation details from `errors` and machine-readable status from `code`.

## Historical Baseline (`v0.1.0`)

- Clean Architecture + CQRS API with MediatR.
- Keycloak JWT authentication.
- Tenant-aware data access with global query filters.
- OpenAPI + Swagger + Scalar documentation endpoints.
- Graceful shutdown behavior for rolling deployments.
