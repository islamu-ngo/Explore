<!-- ABOUTME: API error catalog artifact for backend/API health refactor Phase 0. -->
<!-- ABOUTME: Defines typed ProblemDetails codes, statuses, extensions, and verification expectations. -->

# API Error Catalog

Last Updated: 2026-05-07 Europe/Brussels

## Purpose

This artifact defines the typed ProblemDetails vocabulary for backend/API errors. It is the implementation input for `ApiProblemCodes`, `ApiProblemFactory`, middleware ProblemDetails writing, and command response/result mapping.

## Required ProblemDetails Extensions

Every API error response must include:

- `traceId`
- `timestamp`
- `correlationId` when available
- `code` using one of the catalog values below

Security-sensitive production errors must avoid leaking implementation detail.

## Initial Code Catalog

| Code | HTTP Status | Meaning | Typical Sources | Required Tests |
|---|---:|---|---|---|
| `validation_failed` | 400 | Request failed validation. | FluentValidation/manual validators, model binding, command validation. | Invalid request returns code and validation details when safe. |
| `tenant_required` | 400/401/403 | Tenant context required but absent or invalid. | Tenant middleware, query-filter guard, tenant-scoped handlers. | Missing tenant produces expected status/code and no data leak. |
| `authentication_required` | 401 | User identity/token/API key is missing or invalid. | Auth middleware, required user extraction, API key auth. | Missing identity returns 401/code. |
| `forbidden` | 403 | Authenticated principal lacks permission. | Policy auth, resource auth, Cerbos/local provider deny. | Authenticated unauthorized user returns 403/code. |
| `resource_not_found` | 404 | Resource does not exist or is intentionally hidden. | Query handlers, command preconditions. | Missing resource returns 404/code. |
| `resource_conflict` | 409 | Business conflict not specifically concurrency/idempotency. | Duplicate slug/name, invalid lifecycle transition. | Conflict returns 409/code. |
| `concurrency_conflict` | 409 | Optimistic concurrency conflict. | Event lifecycle/status/settings updates. | Stale version returns 409/code. |
| `duplicate_request` | 409 or replayed original status | Duplicate idempotency key/request. | Idempotent create/publish/registration/bootstrap actions. | Duplicate key returns replay or 409/code by endpoint contract. |
| `rate_limited` | 429 | Request rejected by rate limiter. | Rate limiting middleware. | 429 includes Retry-After/rate-limit headers when available. |
| `unexpected_error` | 500 | Unhandled server error. | Global exception handler. | Production hides detail; dev includes detail per environment. |

## Mapping Rules

- Controllers do not return raw `BadRequest(string)`, string `Forbid(...)`, or ad hoc anonymous problem shapes.
- Middleware uses the same writer/factory as controllers where possible.
- `BaseCommandResponse<T>` failures map through `CommandResponseResultMapper`.
- Bool delete failures map to `resource_not_found` unless the command exposes a more specific failure.
- Idempotency and optimistic-concurrency outcomes use dedicated codes.
- Rate limiting uses RFC 6585 semantics and includes `Retry-After` when available.

## Canonical Payload Examples

### Validation failure

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/event",
  "code": "validation_failed",
  "traceId": "0HN...",
  "timestamp": "2026-05-07T19:59:04Z",
  "correlationId": "7f0d0ef0-0f52-4d2f-a8b7-8d50f2f89d3f",
  "errors": {
    "title": ["Title is required."]
  }
}
```

### Tenant required

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Tenant context required",
  "status": 400,
  "detail": "This endpoint requires a resolved tenant context.",
  "instance": "/api/event/my",
  "code": "tenant_required",
  "traceId": "0HN...",
  "timestamp": "2026-05-07T19:59:04Z",
  "correlationId": "7f0d0ef0-0f52-4d2f-a8b7-8d50f2f89d3f"
}
```

### Forbidden resource action

```json
{
  "type": "https://httpstatuses.com/403",
  "title": "Forbidden",
  "status": 403,
  "detail": "You are not allowed to perform this action on the requested resource.",
  "instance": "/api/event/4f3d8f2c-854d-4f68-a1d7-0f4d87257d7e/publish",
  "code": "forbidden",
  "traceId": "0HN...",
  "timestamp": "2026-05-07T19:59:04Z",
  "correlationId": "7f0d0ef0-0f52-4d2f-a8b7-8d50f2f89d3f"
}
```

### Optimistic concurrency conflict

```json
{
  "type": "https://httpstatuses.com/409",
  "title": "Concurrency conflict",
  "status": 409,
  "detail": "The resource was modified by another request. Reload and retry with the latest version.",
  "instance": "/api/event/4f3d8f2c-854d-4f68-a1d7-0f4d87257d7e/status",
  "code": "concurrency_conflict",
  "traceId": "0HN...",
  "timestamp": "2026-05-07T19:59:04Z",
  "correlationId": "7f0d0ef0-0f52-4d2f-a8b7-8d50f2f89d3f"
}
```

## OpenAPI Requirements

- Each endpoint declares expected ProblemDetails responses.
- ProblemDetails schema documents the `code`, `traceId`, `timestamp`, and `correlationId` extensions.
- Breaking error-shape changes are recorded in `backend-contract-risk-register.md` and `docs/API_CHANGELOG.md`.
