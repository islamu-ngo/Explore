---
name: auth-patterns
description: Guidelines for authentication and authorization patterns covering OIDC, JWT, and BFF security in .NET Clean Architecture projects.
type: domain
enforcement: suggest
priority: critical
---

ABOUTME: Authentication/authorization rules and claim extraction.
ABOUTME: Read referenced resources before applying.

# Authentication & Authorization Patterns

> **Project-Agnostic Authentication & Authorization Guide**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../docs/TEMPLATE_GLOSSARY.md).

## Purpose
Standard rules for OIDC/JWT + BFF. Keep tokens server-side, enforce endpoint auth, normalize claim extraction.

## When This Skill Activates
- Keywords: auth, jwt, oidc, keycloak, authorize, claim
- File patterns: `*Controller.cs`, `*Program.cs`

## Non‑Inferable Rules (Must Follow)
- **BFF boundary**: Browser never sees tokens; BFF stores tokens in HttpOnly cookies and forwards Bearer to API.
- **Endpoint auth**: `GET` = `[AllowAnonymous]`, write = `[Authorize]`, admin = `[Authorize(Roles = "Admin")]`.
- **Ownership**: Resource ownership checks live in handlers (not controllers).
- **UserId fallback**: `sub` → `nameidentifier` → `sid` (must use this order).
- **JWT validation**: Validate issuer + audience, and check both `aud` and `azp` (Keycloak authorized party) claims. Multi‑client audiences: `islamu-event-api`, `islamu-event-blazor`. Clock skew tolerance: 5 minutes.
- **MediatR AuthorizationBehavior**: Resource‑level auth uses `IAuthorizedRequest` interface or `[AuthorizeResource]` attribute. `ISecureRequest` provides dynamic resource context. Denied → `AuthorizationException` → `403 Forbidden` via chained `IExceptionHandler`.
- **HATEOAS authorization**: Implements a high-performance **4-phase pipeline** (Candidate → Normalize → Batch → Materialize). `HateoasAuthorizationEvaluator` deduplicates and batch-evaluates permissions. Fail‑closed: on batch failure, permission‑bound links are denied.
- **UI Action Gating**: The API's `_links` object is the **single source of truth** for client UI affordances. Clients must render "Edit/Delete" buttons based on link presence, not by local role/claim inspection.
- **Security headers**: `SecurityHeadersMiddleware` adds `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, CSP, Permissions-Policy to every response. Non-GET responses also get `Cache-Control: no-store`.

## Resources (Read Before Applying)
- [user-id-extraction.md](resources/user-id-extraction.md) — fallback extraction pattern
- [api-jwt-validation.md](resources/api-jwt-validation.md) — JWT validation + middleware order

## Related Skills
- `clean-architecture-rules`
- `blazor-bff-patterns`

## Related Documentation
- [`docs/API.md`](../../../docs/API.md) — Full middleware pipeline, rate limiting, HATEOAS authorization
- [`docs/SECURITY.md`](../../../docs/SECURITY.md) — Security headers, CORS, JWT config
