---
name: clean-architecture-rules
description: "Load for cross-layer refactors, record/class or value-object migrations, new services/interfaces/repositories, project references, dependency-direction failures, or deciding whether code belongs in Domain, Application, Persistence/Infrastructure, API, or Blazor; not for a local edit wholly inside an established layer pattern."
type: guardrail
enforcement: block
priority: critical
---
<!-- ABOUTME: Clean Architecture dependency guardrail for Explore.Domain, Explore.Application, and layer-boundary work. -->
<!-- ABOUTME: Blocks upward dependencies, misplaced validation, and layer leakage that would violate the repository contract. -->

## Resources
- [Architecture](../../../docs/ARCHITECTURE.md) — load for project/layer ownership and runtime composition.
- [Record contracts](../../../docs/RECORD_CONTRACTS.md) — load for record/class selection, immutable collections, semantic values, persistence leaves, or generated-client ownership.
- [Codebase structure](../../../docs/CODEBASE_STRUCTURE.md) — load when the destination project or folder is unresolved.
- [Quick reference](../../../docs/QUICK_REFERENCE.md) — load for global repository invariants.

## Rules

- Domain has no external framework dependency. Entities and lifecycle objects remain classes; small self-contained values may use `readonly record struct`, and reference-bearing immutable value data may use sealed records.
- Application references Domain only. Concrete MediatR requests default to sealed records, while valid-state command results use their existing named factories.
- Persistence maps Domain entities and semantic values without making Domain depend on EF Core. Repositories return entities, never DTOs or `IQueryable`.
- API and Blazor own transport, HTTP, rendering, and composition. HTTP adapters introduce trusted tenant/user authority; request bodies do not become current authority.
- Generated browser contract shape remains generator-owned. Do not move generated records into handwritten mirrors or let generated DTO concerns flow into Domain.
- Validators remain manually instantiated, specifications remain Application-owned, and HATEOAS policies/assemblers remain API-owned.

## Workflow

1. Trace the contract from Domain ownership through Application, Persistence, API/OpenAPI, and generated-client consumers.
2. Choose class, record class, or `readonly record struct` from identity and behavior, not from a layer-wide conversion rule.
3. Change the innermost owning layer first, then migrate outward callers without compatibility shims.
4. Verify dependency direction and the behavior-specific architecture ratchet.

## Verification
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/CleanArchitectureTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/NamingConventionTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
- `dotnet build --configuration Release --verbosity quiet`

## Related
- [../cqrs-mediatr-guidelines/SKILL.md](../cqrs-mediatr-guidelines/SKILL.md)
- [../dotnet-efcore-guidelines/SKILL.md](../dotnet-efcore-guidelines/SKILL.md)
