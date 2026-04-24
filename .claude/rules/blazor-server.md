---
name: blazor-server
description: Apply when editing Explore.Blazor server-side BFF, auth, proxy, or SSR code.
paths:
  - "Explore.Blazor/**/*.cs"
  - "Explore.Blazor/**/*.razor"
related_skills: [blazor-bff-patterns, auth-patterns]
related_docs: [docs/BLAZOR.md, docs/SECURITY.md, docs/ARCHITECTURE.md]
minimum_tests: [Explore.Blazor.IntegrationTests, Event.Architecture.Tests]
related_intents: [bff-auth-bug]
---
<!-- ABOUTME: Path-scoped rules for the Blazor BFF/server host. -->
<!-- ABOUTME: Auto-loaded by Claude Code when editing files matching the `paths` glob. -->

# Blazor Server / BFF Rules

> **Applies to:** `Explore.Blazor/**/*.cs`, `Explore.Blazor/**/*.razor`.
> **Authority:** Below `docs/QUICK_REFERENCE.md` and `docs/GOVERNANCE.md`; cross-reference them instead of copying them.

## Rules (Correct / Wrong)

| # | Rule | Correct | Wrong |
|---|---|---|---|
| 1 | Preserve the BFF boundary | Keep tokens server-side and proxy `/api/*` through YARP | Expose raw bearer tokens to WASM or browser storage |
| 2 | Keep forwarding concerns split | Use dedicated forwarding handlers/transforms for token, tenant, and setup-secret | Collapse all forwarding into one opaque handler |
| 3 | Respect runtime hardening | Keep `UseCookies = false` on outbound server-side API clients when documented | Reuse pooled cookie containers across proxied clients |
| 4 | Follow SSR/InteractiveAuto constraints | Avoid component logic that assumes `HttpContext` is always present | Bind component behavior directly to request-only objects |
| 5 | Treat tenant identity as API-authoritative | Forward trusted tenant hints only; let API resolve final tenant | Invent alternate tenant resolution inside the BFF |
| 6 | Keep endpoint families separated | Put auth/setup/storage/preference endpoints in their dedicated extension files | Grow `BffEndpointExtensions` into a grab bag |

## Must-Reads for This Path

- `AGENTS.md`
- `docs/BLAZOR.md`
- `docs/SECURITY.md`
- `.claude/skills/blazor-bff-patterns/SKILL.md`
- `.claude/skills/auth-patterns/SKILL.md`

## Anti-Patterns (Forbidden on These Paths)

- Client-side token storage or token serialization into rendered markup.
- Trusting inbound `X-Setup-Secret` without the documented strip-and-replace flow.
- Reordering auth/proxy behavior without checking `docs/BLAZOR.md` and `docs/SECURITY.md`.

## Verification

- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Explore.Blazor.IntegrationTests`, `Event.Architecture.Tests`

## Related

- Intents: `bff-auth-bug`
- Agents: `.claude/agents/auth-route-debugger.md`, `.claude/agents/auth-route-tester.md`
- Rules: `blazor-client.md`, `api-controllers.md`
