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
- **JWT validation**: Validate issuer + audience, and check `aud` and `azp` for Keycloak multi‑client tokens.

## Resources (Read Before Applying)
- [user-id-extraction.md](resources/user-id-extraction.md) — fallback extraction pattern
- [api-jwt-validation.md](resources/api-jwt-validation.md) — JWT validation + middleware order

## Related Skills
- `clean-architecture-rules`
- `blazor-bff-patterns`
