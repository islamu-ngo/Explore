---
name: domain
description: Apply when editing Explore.Domain entities, value objects, enums, or domain services.
paths:
  - "Explore.Domain/**/*.cs"
related_skills: [clean-architecture-rules]
related_docs: [docs/DOMAIN.md, docs/ARCHITECTURE.md, docs/QUICK_REFERENCE.md]
minimum_tests: [Event.Domain.UnitTests, Event.Architecture.Tests]
related_intents: [add-ef-migration]
---

# Domain Rules

## Applies To
- `Explore.Domain/**/*.cs`

## Path-Specific Constraints
- **Domain Purity**: Zero dependencies on Infrastructure, Persistence, or Presentation layers. No `using` statements pointing to other layers.
- **Explicit Logic**: All business rules must be explicit. Do not bury logic in auto-properties; use methods for state transitions.
- **Audit Consistency**: Ensure aggregate roots implement the expected auditing and soft-delete interfaces (e.g., `IAuditableEntity`, `ISoftDeletable`).

## Must Read
- [docs/QUICK_REFERENCE.md#critical-rules](../../docs/QUICK_REFERENCE.md#critical-rules) (Rules #3, #4, #5, #6, #10)
- [docs/DOMAIN.md](../../docs/DOMAIN.md)

## Verification
- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.Domain.UnitTests`, `Event.Architecture.Tests`

## Related
- Intents: `add-ef-migration`
- Agents: `architect-agent.md`, `backend-engineer-agent.md`, `quality-verifier-agent.md`
- Rules: `application-layer.md`, `efcore-persistence.md`, `efcore-migrations.md`
