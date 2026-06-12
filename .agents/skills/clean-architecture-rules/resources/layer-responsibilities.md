ABOUTME: Decide which layer owns a piece of code.
ABOUTME: Use this as a quick placement guide.

# Layer Responsibilities

## Decision Guide
- **Domain**: business rules, entities, value objects.
- **Application**: use cases, commands/queries, handlers, DTOs, validators.
- **Persistence**: EF Core, DbContext, repositories, migrations.
- **Infrastructure**: external services (email, storage, integrations).
- **API/Blazor**: HTTP endpoints and UI only.

## Quick Examples
- Domain: `{Entity}`, `{EntityStatus}`
- Application: `Create{Entity}Command`, `{Entity}Dto`
- Persistence: `{DbContext}`, `{Entity}Repository`
- Infrastructure: `EmailService`
- API/Blazor: `{Entity}Controller`, `{Entities}List.razor`

**Related**: `dependency-rules.md`.
