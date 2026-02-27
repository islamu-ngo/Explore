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

## Historical Baseline (`v0.1.0`)

- Clean Architecture + CQRS API with MediatR.
- Keycloak JWT authentication.
- Tenant-aware data access with global query filters.
- OpenAPI + Swagger + Scalar documentation endpoints.
- Graceful shutdown behavior for rolling deployments.
