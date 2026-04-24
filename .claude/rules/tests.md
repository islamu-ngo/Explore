---
name: tests
description: Apply when editing unit, integration, architecture, or end-to-end test source files.
paths:
  - "**/*Tests/*.cs"
  - "**/*UnitTests/*.cs"
  - "**/*IntegrationTests/*.cs"
  - "**/*.Tests/*.cs"
related_skills: []
related_docs: [docs/TESTING.md, CLAUDE.md, docs/QUICK_REFERENCE.md]
minimum_tests: [Event.Domain.UnitTests, Event.Application.UnitTests, Event.Architecture.Tests, Event.Persistence.IntegrationTests, Event.API.IntegrationTests, Explore.Blazor.IntegrationTests, Explore.Blazor.Client.Tests, Explore.Blazor.Client.E2ETests, Explore.Secrets.UnitTests]
related_intents: [add-get-endpoint, add-write-endpoint, add-cqrs-handler, add-ef-migration, update-repository-query, blazor-component-affordance, bff-auth-bug, openapi-contract-change]
---
<!-- ABOUTME: Path-scoped rules for test source files across the repository. -->
<!-- ABOUTME: Auto-loaded by Claude Code when editing files matching the `paths` glob. -->

# Test Rules

> **Applies to:** common test-project source patterns across the repo.
> **Authority:** `docs/TESTING.md` and `CLAUDE.md` are canonical for verification policy.

## Rules (Correct / Wrong)

| # | Rule | Correct | Wrong |
|---|---|---|---|
| 1 | Run projects individually | Use `dotnet test --project ... --configuration Release` per affected project | Run solution-level `dotnet test` |
| 2 | Keep Release parity | Verify with Release configuration to match repo policy | Treat Debug-only test runs as sufficient |
| 3 | Preserve suite integrity | Fix or investigate failing tests; never delete them to go green | Remove red tests as cleanup |
| 4 | Keep output clean | Expect no stray warnings, stack traces, or noisy logs | Normalize messy output as acceptable |
| 5 | Keep E2E realistic | Use real browser/infrastructure flows and avoid mocks in E2E | Mock away the behavior the suite exists to prove |
| 6 | Respect test-project roles | Put assertions in the project matching the layer/host profile | Dump every scenario into API integration tests |

## Must-Reads for This Path

- `AGENTS.md`
- `CLAUDE.md`
- `docs/TESTING.md`

## Anti-Patterns (Forbidden on These Paths)

- Solution-level `dotnet test` as the default verification story.
- Ad-hoc scripts replacing the documented test projects.
- Architecture-test failures ignored because they are “just conventions.”

## Verification

- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: run the affected entries from `minimum_tests` with `--project`

## Related

- Intents: `add-get-endpoint`, `add-write-endpoint`, `add-cqrs-handler`, `add-ef-migration`, `update-repository-query`, `blazor-component-affordance`, `bff-auth-bug`, `openapi-contract-change`
- Agents: `.claude/agents/codebase-verifier.md`, `.claude/agents/auto-error-resolver.md`
- Rules: `application-layer.md`, `api-controllers.md`, `blazor-client.md`
