---
name: blazor-server
description: Apply when editing Explore.Blazor server-side BFF, auth, proxy, or SSR code.
paths:
  - "src/Explore.Blazor/**/*.cs"
  - "src/Explore.Blazor/**/*.razor"
related_skills: [blazor-bff-patterns, auth-patterns]
related_docs: [docs/internal/BLAZOR.md, docs/internal/SECURITY-MODEL.md, docs/internal/ARCHITECTURE.md]
minimum_tests: [Explore.Blazor.IntegrationTests, Event.Architecture.Tests]
related_intents: [bff-auth-bug]
---

<!-- ABOUTME: Path-scoped rules for Explore.Blazor server-side BFF, auth, proxy, and SSR code. -->
<!-- ABOUTME: Twin copy at .omo/rules/blazor-server.md. When modifying this file, update both paths. -->

# Blazor Server / BFF Rules

## Applies To
- `src/Explore.Blazor/**/*.cs`, `src/Explore.Blazor/**/*.razor`

## Path-Specific Constraints
- **BFF Boundary**: Tokens must stay server-side. Use YARP to proxy `/api/*` requests. Never expose raw bearer tokens to the client.
- **Concern Separation**: Use dedicated forwarding handlers/transforms for tokens, tenants, and setup-secrets.
- **Pooled Clients**: Keep `UseCookies = false` on outbound server-side API clients.
- **SSR Safety**: Avoid component logic that assumes `HttpContext` presence (crucial for InteractiveAuto components).
- **Endpoint Modularization**: Organize auth, setup, preference, storage, support-access, and ATProto endpoints into dedicated extension files.
- **Dynamic Schemes & OAuth**: Manage OIDC/OAuth schemes via `IDynamicAuthSchemeManager`. Expose AT Protocol client metadata (`/oauth/client-metadata.json`) and JWKS (`/oauth/jwks.json`) via `AtprotoOAuthEndpointExtensions`.

## Must Read
- [docs/internal/QUICK_REFERENCE.md#multi-tenancy-reminder](../../docs/internal/QUICK_REFERENCE.md#multi-tenancy-reminder)
- [docs/internal/BLAZOR.md](../../docs/internal/BLAZOR.md)
- [docs/internal/SECURITY-MODEL.md](../../docs/internal/SECURITY-MODEL.md)

## Verification
- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Explore.Blazor.IntegrationTests`, `Event.Architecture.Tests`

## Related
- Intents: `bff-auth-bug`
- Agents: `backend-engineer-agent.md`, `presentation-engineer-agent.md`, `quality-verifier-agent.md`
- Rules: `blazor-client.md`, `api-controllers.md`
