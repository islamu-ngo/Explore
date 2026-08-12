---
name: tests
description: Apply when editing unit, integration, architecture, or end-to-end test source files.
paths:
  - "**/*Tests/*.cs"
  - "**/*UnitTests/*.cs"
  - "**/*IntegrationTests/*.cs"
  - "**/*.Tests/*.cs"
related_skills: []
related_docs: [docs/TESTING.md, AGENTS.md, docs/QUICK_REFERENCE.md]
minimum_tests: [Event.Domain.UnitTests, Event.Application.UnitTests, Event.Architecture.Tests, Event.Persistence.IntegrationTests, Event.API.IntegrationTests, Explore.Blazor.IntegrationTests, Explore.Blazor.Client.Tests, Explore.Secrets.UnitTests]
related_intents: [add-get-endpoint, add-write-endpoint, add-cqrs-handler, add-ef-migration, update-repository-query, blazor-component-affordance, bff-auth-bug, openapi-contract-change]
---

# Test Rules

## Applies To
- All test projects and source files (`**/*Tests/*.cs`).

## Path-Specific Constraints
- **Suite Integrity**: Failing tests must be fixed or investigated; never deleted to bypass failures.
- **Pristine Output**: Test runs must have zero stray warnings, stack traces, or noisy logs.
- **Runtime Realism**: Keep in-process integration tests deterministic; use explicit runtime lanes when real provider infrastructure is the behavior under test.
- **Project Role Balance**: Assertions must live in the project matching the host profile (e.g., Domain logic in `Domain.UnitTests`, not API tests).

## Must Read
- [docs/QUICK_REFERENCE.md#build-and-test-baseline](../../docs/QUICK_REFERENCE.md#build-and-test-baseline)
- [docs/TESTING.md](../../docs/TESTING.md)

## Verification
- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: Run the specific test project using `--project` with `--configuration Release`.

## Related
- Intents: `add-get-endpoint`, `add-write-endpoint`, `add-cqrs-handler`, `add-ef-migration`, `update-repository-query`, `blazor-component-affordance`, `bff-auth-bug`, `openapi-contract-change`
- Agents: `quality-verifier-agent.md`
- Rules: `application-layer.md`, `api-controllers.md`, `blazor-client.md`
