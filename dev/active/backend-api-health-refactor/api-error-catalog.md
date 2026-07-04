<!-- ABOUTME: API error catalog artifact for backend/API health refactor Phase 0. -->
<!-- ABOUTME: Defines typed ProblemDetails codes, statuses, extensions, and verification expectations. -->

# API Error Catalog

Last Updated: 2026-07-04 Europe/Brussels

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
| `tenant_required` | 400/401/403/404 | Tenant context required but absent or invalid. | Tenant middleware, query-filter guard, tenant-scoped handlers. | Missing tenant produces expected status/code and no data leak. |
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

## Implementation Checkpoint

2026-07-04 Phase 2.1 source audit:

- Context7 `/dotnet/aspnetcore.docs` confirmed ASP.NET Core support for centralized RFC 7807 `ProblemDetails`, `ValidationProblemDetails`, `IProblemDetailsService`, and explicit `ProblemDetails` response metadata.
- Tavily returned OWASP error-handling guidance to avoid returning internal diagnostic details to clients.
- Direct controller raw-helper sweeps are clean under `Explore.API/Controllers`: no direct `BadRequest`, `Unauthorized`, `Forbid`, `NotFound`, `Conflict`, `Problem`, `StatusCode`, or `ValidationProblem` calls remain for the audited patterns.
- `EventSessionLanguageController.Update` was the final direct validation helper path; missing or invalid `If-Match` now maps through `ToValidationProblem` with `code=validation_failed`, standard trace/timestamp extensions, and `errors.If-Match`.
- No `BaseCommandResponse` 4xx response metadata remains under controllers. Successful command responses may still legitimately use `BaseCommandResponse<T>` until a separate contract-normalization task changes command success contracts.

2026-07-04 Phase 2.2 behavior coverage:

- Context7 `/dotnet/aspnetcore.docs` confirmed `IProblemDetailsService` for RFC 7807 responses and `IAuthorizationMiddlewareResultHandler` as the supported ASP.NET Core hook for challenge/forbid customization.
- Tavily returned OWASP Improper Error Handling and Error Handling Cheat Sheet guidance: client-facing errors should remain generic and must not expose stack traces, SQL/server paths, raw provider details, or internal diagnostics.
- `ProblemDetailsContractTests` now locks representative 400, 401, 403, 404, 409, 429, and 500 shapes. The new middleware examples cover authorization-middleware `authentication_required` and `forbidden`; the rate-limit example covers an API-key partition returning `429`, `code=rate_limited`, `Retry-After`, `X-RateLimit-Limit`, and `X-RateLimit-Remaining`.
- The direct controller migration task remains closed for audited patterns. Reopen R-005 only for a concrete source-backed gap or a new ProblemDetails family.

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
  "type": "https://httpstatuses.com/404",
  "title": "Tenant not resolved",
  "status": 404,
  "detail": "The tenant could not be resolved for this request.",
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
