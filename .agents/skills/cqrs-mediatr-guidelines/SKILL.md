---
name: cqrs-mediatr-guidelines
description: "Load for MediatR/CQRS commands, queries, immutable request/result contracts, handlers, validators, pipeline behaviors, cache invalidation, idempotency, or specification-driven reads in `Explore.Application`; not for controller-only or repository-only edits."
type: pattern
enforcement: suggest
priority: high
---
<!-- ABOUTME: CQRS and MediatR guidance for commands, queries, handlers, validators, caching, and specification usage. -->
<!-- ABOUTME: Keeps Explore.Application handlers thin, entity-first, cancellation-aware, and aligned with repository and response contracts. -->

## Resources
- [Record contracts](../../../docs/RECORD_CONTRACTS.md) — load for request/result record selection, factories, collection ownership, and trusted authority.
- [Architecture](../../../docs/ARCHITECTURE.md) — load for Application boundaries and CQRS flow.
- [API](../../../docs/API.md) — load when result mapping or the public HTTP contract changes.
- [Quick reference](../../../docs/QUICK_REFERENCE.md) — load for repository-wide CQRS invariants.

## Rules

- Concrete MediatR requests default to sealed records. Prefer positional records only for short stable contracts; use nominal `init`/`required` members for long, optional, attributed, or presence-sensitive contracts.
- A request carries client intent, not current authority. HTTP or trusted adapters supply current tenant/user facts from principal, route, or tenant context; body identifiers name only independently authorized targets.
- `BaseCommandResponse<TKey>` and concrete payload results are immutable valid-state records. Use named success/failure factories and the shared RFC 7807 mapper; never restore public setters or contradictory constructor states.
- Repositories return entities. Handlers perform DTO mapping and never consume `ExploreDbContext` or a repository-returned `IQueryable`.
- Validators are manually instantiated in the handler. Cancellation flows end to end.
- Queries may use HybridCache read-through; commands invalidate affected keys after the owning write succeeds.
- `IQuerySpecification<T>` composition remains immutable and Application-owned; Persistence applies it to EF queries.

## Workflow

1. Lock request construction, result-state mapping, trusted authority, and consumed equality with focused tests.
2. Change the request/result contract and migrate all handlers, controllers, tests, and mappers in the same slice.
3. Replace mutable result assembly with named factories and mutable request rewrites with initialization or `with`.
4. Verify CQRS conventions plus the affected handler and API mapping tests.

## Verification
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/CqrsPatternTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
- `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- `dotnet build --configuration Release --verbosity quiet`

## Related
- [../clean-architecture-rules/SKILL.md](../clean-architecture-rules/SKILL.md)
- [../dotnet-efcore-guidelines/SKILL.md](../dotnet-efcore-guidelines/SKILL.md)
- [../auth-patterns/SKILL.md](../auth-patterns/SKILL.md)
