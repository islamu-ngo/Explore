# API Changelog

All notable changes to the Explore API are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [1.0.0] - 2026-02-18

### Added

#### Security
- **Security headers** on all responses: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Content-Security-Policy`
- **Tiered rate limiting** with `Retry-After` and `X-RateLimit-*` response headers:
  - Global: 200 requests/IP (token bucket, configurable)
  - Authenticated: 200 requests/min/user (sliding window)
  - Write operations: 30 requests/min/user (fixed window)
- **Hardened CORS policies** with configurable origin allowlists (replaced `AllowAnyOrigin`)

#### API Versioning
- **Media type versioning** via `Accept` header: `application/json;v=1.0`
- Default version 1.0 assumed when no version specified
- All controllers annotated with `[ApiVersion("1.0")]`

#### Performance
- **ETag / conditional requests** on all GET endpoints returning JSON
  - Responses include `ETag` header (weak validator, SHA256 body hash)
  - Send `If-None-Match` header to receive `304 Not Modified` when content unchanged
- **Request timeouts** per endpoint category:
  - Default: 30 seconds
  - Lookup endpoints: 10 seconds
  - Complex operations: 60 seconds

#### Observability
- **Correlation ID propagation** via `X-Correlation-ID` / `X-Request-ID` headers
  - Send a correlation ID in the request; it will be echoed in the response
  - If no correlation ID is sent, one is auto-generated
- **Structured request logging** with method, path, status, duration, user, and tenant
- **Business metrics** via OpenTelemetry: `events.created`, `registrations.created`, `organizations.created`, `authorization.decisions`

### Usage

#### Media Type Versioning
```http
GET /api/v1/event HTTP/1.1
Accept: application/json;v=1.0
```

When no version is specified, the API defaults to version 1.0.

#### Conditional Requests (ETag)
```http
GET /api/v1/category HTTP/1.1

HTTP/1.1 200 OK
ETag: W/"abc123..."

GET /api/v1/category HTTP/1.1
If-None-Match: W/"abc123..."

HTTP/1.1 304 Not Modified
```

#### Correlation ID
```http
GET /api/v1/event HTTP/1.1
X-Correlation-ID: my-trace-id-123

HTTP/1.1 200 OK
X-Correlation-ID: my-trace-id-123
```

---

## [0.1.0] - 2025-01-01

### Initial Release
- REST API with Clean Architecture + CQRS (MediatR)
- HATEOAS/HAL+JSON with RFC 7240 `Prefer: return=minimal` support
- Keycloak JWT authentication with BFF pattern
- Cerbos authorization with fallback provider
- OutputCache (Lookup 1h, List 30s, Detail 60s) + HybridCache (L1+L2)
- Response compression (Brotli + Gzip)
- OpenTelemetry traces, metrics, and logs
- RFC 7807 ProblemDetails error responses
- Offset-based pagination with HATEOAS navigation links
- Graceful shutdown with 25-second SIGTERM grace period
