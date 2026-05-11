---
name: blazor-server
description: Apply when editing Explore.Blazor server-side BFF, auth, proxy, or SSR code.
paths:
  - "Explore.Blazor/**/*.cs"
  - "Explore.Blazor/**/*.razor"
related_skills: [blazor-bff-patterns, auth-patterns]
related_docs: [docs/BLAZOR.md, docs/SECURITY-MODEL.md, docs/ARCHITECTURE.md]
minimum_tests: [Explore.Blazor.IntegrationTests, Event.Architecture.Tests]
related_intents: [bff-auth-bug]
---

# Blazor Server / BFF Rules

## Applies To
- `Explore.Blazor/**/*.cs`, `Explore.Blazor/**/*.razor`

## Path-Specific Constraints
- **BFF Boundary**: Tokens must stay server-side. Use YARP to proxy `/api/*` requests. Never expose raw bearer tokens to the client.
- **Concern Separation**: Use dedicated forwarding handlers/transforms for tokens, tenants, and setup-secrets.
- **Pooled Clients**: Keep `UseCookies = false` on outbound server-side API clients.
- **SSR Safety**: Avoid component logic that assumes `HttpContext` presence (crucial for InteractiveAuto components).
- **Endpoint Modularization**: Organize auth, setup, and preference endpoints into dedicated extension files.

## Must Read
- [docs/QUICK_REFERENCE.md#multi-tenancy-reminder](../../docs/QUICK_REFERENCE.md#multi-tenancy-reminder)
- [docs/BLAZOR.md](../../docs/BLAZOR.md)
- [docs/SECURITY-MODEL.md](../../docs/SECURITY-MODEL.md)

## Verification
- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Explore.Blazor.IntegrationTests`, `Event.Architecture.Tests`

## Related
- Intents: `bff-auth-bug`
- Agents: `backend-engineer-agent.md`, `presentation-engineer-agent.md`, `quality-verifier-agent.md`
- Rules: `blazor-client.md`, `api-controllers.md`
