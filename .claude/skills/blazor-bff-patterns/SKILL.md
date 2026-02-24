---
name: blazor-bff-patterns
description: Backend for Frontend (BFF) patterns for Blazor applications. Covers YARP proxy, token forwarding, cookie-based auth, and service layer integration.
type: domain
enforcement: suggest
priority: high
---

ABOUTME: Blazor BFF rules for YARP, token forwarding, and auth state.
ABOUTME: Read referenced resources before applying.

# Blazor BFF (Backend for Frontend) Patterns

> **Project-Agnostic BFF Patterns for Blazor Web Apps (InteractiveAuto)**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../docs/TEMPLATE_GLOSSARY.md).

## Purpose
Keep tokens server‑side, proxy API calls via YARP, centralize auth state + service layer.

## When This Skill Activates
- Keywords: bff, yarp, proxy, token forwarding, cookie auth, auth state
- File patterns: `**/*Blazor/Program.cs`, `**/*Blazor/Services/**/*.cs`, `**/*Blazor.Client/Services/**/*.cs`

## Non‑Inferable Rules (Must Follow)
- **BFF boundary**: Browser never sees tokens; BFF uses HttpOnly cookies.
- **YARP**: API calls go through reverse proxy; attach Bearer token server‑side.
- **Token forwarding**: Use YARP transforms or delegating handlers (no raw HttpClient in WASM).
- **Service layer**: Wrap NSwag clients for error handling + safe defaults.
- **CSRF**: Enforce antiforgery on state‑changing routes.
- **InteractiveAuto**: Avoid direct HttpContext assumptions in components.

## Resources (Read Before Applying)
- [bff-configuration.md](resources/bff-configuration.md)
- [token-forwarding.md](resources/token-forwarding.md)
- [auth-state-management.md](resources/auth-state-management.md)
- [service-layer-patterns.md](resources/service-layer-patterns.md)
- [interactiveauto-yarp-security.md](resources/interactiveauto-yarp-security.md)

## Related Documentation
- [`docs/ARCHITECTURE.md`](../../../docs/ARCHITECTURE.md)
- [`auth-patterns`](../auth-patterns/SKILL.md)
