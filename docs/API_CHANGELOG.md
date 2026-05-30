ABOUTME: API change log aligned with the current repository state and versioning in source.
ABOUTME: Keeps release notes short and focused on externally observable API behavior.

# API Changelog

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
- Actor subscription endpoints added under `/api/actor-subscriptions` for authenticated current-user subscription state, subscribe, notification-level update, and unsubscribe flows. HAL affordances now expose actor and event organizer subscription actions for organization/group actors only; clients must continue gating UI actions from `_links` rather than local role or claim checks.
- Event-published organization/group actor subscriptions now produce durable in-app notifications through an internal outbox fanout path. This is not an SMTP, push, or SignalR delivery contract.

## Historical Baseline (`v0.1.0`)

- Clean Architecture + CQRS API with MediatR.
- Keycloak JWT authentication.
- Tenant-aware data access with global query filters.
- OpenAPI + Swagger + Scalar documentation endpoints.
- Graceful shutdown behavior for rolling deployments.
