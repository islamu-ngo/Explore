---
name: auth-trust-boundaries
description: Apply when editing authentication, authorization, BFF sessions, tenant resolution, or security boundaries.
paths:
  - "src/Explore.API/Controllers/**/*.cs"
  - "src/Event.Web.BffHosting/**/*.cs"
  - "src/Explore.Application/Authorization/**/*.cs"
  - "src/Explore.Application/Authentication/**/*.cs"
  - "cerbos/**/*.yaml"
related_skills: [auth-patterns, blazor-bff-patterns, clean-architecture-rules]
related_docs: [docs/internal/AUTHORIZATION.md, docs/internal/SECURITY-MODEL.md, docs/internal/MULTI_TENANCY.md, docs/internal/QUICK_REFERENCE.md]
minimum_tests: [Event.API.IntegrationTests, Event.Architecture.Tests, Explore.Blazor.IntegrationTests]
related_intents: [bff-auth-bug, cerbos-policy-change, add-write-endpoint]
---

<!-- ABOUTME: Path-scoped rules for Tier 1 Security & Identity Trust Boundaries. -->
<!-- ABOUTME: Twin copy at .agents/rules/auth-trust-boundaries.md. When modifying this file, update both paths. -->

# Auth & Trust Boundary Rules (Tier 1 — Security)

## Applies To
- `src/Explore.API/Controllers/**/*.cs`, `src/Event.Web.BffHosting/**/*.cs`, `src/Explore.Application/Authorization/**/*.cs`, `cerbos/**/*.yaml`

## Critical Rules & Invariants

| # | Rule | Correct | Wrong |
|---|---|---|---|
| 1 | **Fail-Closed Authorization** | Default all write endpoints to explicit `[Authorize]` and `[EndpointClassification]`; GET endpoints explicitly declare `[AllowAnonymous]`. | Omitting authorization attributes on mutative API controllers. |
| 2 | **Single User ID Authority** | Derive user identity exclusively via `Explore.Application.Authentication.PlatformIdentityPrincipalExtensions` (`sub` $\rightarrow$ `nameidentifier` $\rightarrow$ `sid` $\rightarrow$ `internal_user_id`). | Parsing raw claims or re-deriving identity ad-hoc in controllers. |
| 3 | **Central Tenant Isolation** | Enforce tenant isolation centrally via EF Core global query filters in `ExploreDbContext`. | Using `IgnoreQueryFilters()` casually without proven architectural isolation. |
| 4 | **BFF Anti-Spoofing** | Trust tenant and user headers only from the verified BFF gateway; strip incoming client `X-Tenant-Slug` on public edge. | Trusting caller-supplied tenancy or identity headers from browser clients. |
| 5 | **Server-Side Action Authority** | Gate actual mutation authority server-side in handlers/policies; client HAL links are affordances for UI visibility only. | Checking roles or permissions in client Blazor code to authorise actions. |
| 6 | **Air-Gapped Fallback** | Wrap external OAuth and identity provider communication in circuit breakers with offline/local fallback support. | Creating synchronous hard dependencies on external cloud identity services. |
| 7 | **Secrets Source of Truth** | Sourced strictly from Infisical or `.env` (documented in `.env.example`). | Hard-coding credentials/keys in `AppHost.cs`, test fixtures, or code. |

## Must Read
- [docs/internal/AUTHORIZATION.md](../../docs/internal/AUTHORIZATION.md)
- [docs/internal/SECURITY-MODEL.md](../../docs/internal/SECURITY-MODEL.md)
- [docs/internal/MULTI_TENANCY.md](../../docs/internal/MULTI_TENANCY.md)

## Verification
- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.API.IntegrationTests`, `Event.Architecture.Tests`, `Explore.Blazor.IntegrationTests`
- Invariant-Breaker tests: Exploit/bypass tests verifying 401/403 on invalid tokens, cross-tenant leak attempts, and header spoofing.

## Related
- Intents: `bff-auth-bug`, `cerbos-policy-change`, `add-write-endpoint`
- Agents: `security-privacy-agent.md`, `quality-verifier-agent.md`
- Skills: `auth-patterns`, `blazor-bff-patterns`
