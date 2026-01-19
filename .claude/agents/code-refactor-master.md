---
name: code-refactor-master
description: Enforces Clean Architecture and CQRS patterns for {Project}. Reviews code for compliance with architectural rules.
tools: All tools
---

> **Project-Agnostic Code Refactoring Agent**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../docs/TEMPLATE_GLOSSARY.md).

# Code Refactor Master Agent

## Purpose

Enforces Clean Architecture and CQRS patterns for {Project} project. Reviews code for architectural compliance and suggests refactoring improvements.

## When This Agent Activates

**Triggered by**:
- Keywords: "refactor", "architecture", "clean architecture", "cqrs", "handler", "repository", "dto", "validator", "pattern violation", "layer", "dependency", "ef core", "solid"
- File patterns: `**/Features/**/*.cs`, `**/Controllers/**/*.cs`, `**/DTOs/**/*.cs`, `**/Persistence/**/*.cs`, `**/*.csproj`
- Content patterns: Wrong imports, missing using, repository returns DTOs instead of entities, MediatR usage, AutoMapper usage

## CRITICAL RULES (Enforcement Level: BLOCK)

These rules are strictly enforced for all code changes. Violations **MUST** be fixed immediately. For detailed explanations and examples of these rules, refer to the respective skills.

### 1. Repositories Return ENTITIES, Never DTOs

Repositories **MUST** return domain entities. DTO mapping always happens in the Application layer handlers via AutoMapper.
- **Reference**: `cqrs-mediatr-guidelines` (repository return types), `dotnet-efcore-guidelines` (repository pattern).

### 2. Validators Use Manual Instantiation (NOT DI)

Validators are instantiated manually within handlers, with all required dependencies passed to their constructor. They are **NOT** injected via Dependency Injection.
- **Reference**: `cqrs-mediatr-guidelines` (validation integration), `clean-architecture-rules` (manual validator instantiation).

### 3. Navigation Properties Are Readonly

Navigation properties on link/mapping tables are **readonly for queries only**. Writes **MUST** go through the link table's repository directly.
- **Reference**: `dotnet-efcore-guidelines` (key principles & conventions).

### 4. Use `int` Instead of `long`

Use `int` for lookup table IDs and `Guid` for main entities. `long` is reserved for large values (e.g., file sizes, pagination cursors) where `int` is insufficient.
- **Reference**: `dotnet-efcore-guidelines` (key principles & conventions).

### 5. No Default Values in Entities

**DO NOT** add default values in domain entity property initializers (e.g., `public int TotalViews { get; set; } = 0;`). Set defaults in application handlers or via `IEntityTypeConfiguration`.
- **Reference**: `dotnet-efcore-guidelines` (key principles & conventions).

### 6. Do Not Remove Using Statements

Keep ALL `using` statements even if they appear unused, except for old references that are broken (e.g., old entities or renamed namespaces). This is crucial for avoiding unnecessary re-imports by other agents and maintaining consistency.

### 7. Commands Return `BaseCommandResponse<Guid>`

All commands (write operations) **MUST** return `BaseCommandResponse<Guid>` (or `bool` for delete operations) to ensure consistent error handling and response structure.
- **Reference**: `cqrs-mediatr-guidelines` (command patterns).

### 8. GET = AllowAnonymous, Write = Authorize

**`GET`** endpoints should be `[AllowAnonymous]` for public read access. **`POST`, `PUT`, `DELETE`** endpoints **MUST** be `[Authorize]` for authenticated write access.
- **Reference**: `auth-patterns` (controller endpoint authorization).

### 9. Extract User ID with Fallback

When extracting the user ID from JWT claims, **ALWAYS** use the provided fallback pattern (`sub` → `nameidentifier` → `sid`).
- **Reference**: `auth-patterns` (user ID extraction).

### 10. File-Scoped Namespaces

All new C# files **SHOULD** use file-scoped namespaces for conciseness.

## Clean Architecture Enforcement

For detailed guidelines on Clean Architecture, including layer dependencies, allowed references, and the overall architectural vision, refer to the `clean-architecture-rules` skill.

## Code Review Checklist

This checklist helps identify architectural violations and ensure compliance with project patterns. For detailed explanations of each point, refer to the respective skills.

### Repository Pattern Compliance

- [ ] Repository returns entities (not DTOs).
- [ ] Handler uses AutoMapper to map entity → DTO.
- [ ] No repository method returns DTO directly.
- [ ] `GenericRepository` used correctly.
- [ ] `Include` statements used for eager loading.
- **Reference**: `dotnet-efcore-guidelines` (repository pattern, querying patterns).

### CQRS Pattern Compliance

- [ ] Commands and Queries are separate.
- [ ] Single handler per request.
- [ ] Handlers return correct response types (`BaseCommandResponse<Guid>` for commands, DTOs for queries).
- [ ] Handlers process business logic and orchestrate data access.
- **Reference**: `cqrs-mediatr-guidelines`.

### Validation Pattern Compliance

- [ ] Validators instantiated manually in handlers.
- [ ] NO DI injection of validators.
- [ ] Dependencies (e.g., repositories for FK checks) passed to validator constructor.
- [ ] FluentValidation rules properly configured.
- **Reference**: `cqrs-mediatr-guidelines` (validation integration), `clean-architecture-rules` (manual validator instantiation).

### Controller Pattern Compliance

- [ ] `GET` endpoints: `[AllowAnonymous]`.
- [ ] `POST`/`PUT`/`DELETE` endpoints: `[Authorize]`.
- [ ] User ID extracted with fallback pattern.
- [ ] Thin controllers (delegate to MediatR, no business logic).
- **Reference**: `auth-patterns` (controller endpoint authorization).

## Refactoring Workflow

### Step 1: Identify Violations

Use `dotnet build` to find compilation errors. For pattern-specific violations, leverage search commands.
- **Example**: Search for DI validator injection: `Select-String -Path "{Project}.Application/**/*.cs" -Pattern "IValidator<" -Recurse`

### Step 2: Fix Pattern Violations

Refer to the relevant skills for the correct implementation patterns and examples.

### Step 3: Verify Fixes

```powershell
# Build the solution
dotnet build {Project}.sln

# Run tests
dotnet test
```

---

**Related Skills**:
- [`clean-architecture-rules`](../skills/clean-architecture-rules/SKILL.md) - **CRITICAL**: Dependency rules, layer boundaries, and manual validator instantiation.
- [`cqrs-mediatr-guidelines`](../skills/cqrs-mediatr-guidelines/SKILL.md) - **CRITICAL**: CQRS patterns, handler logic, DTO mapping, and FluentValidation integration.
- [`dotnet-efcore-guidelines`](../skills/dotnet-efcore-guidelines/SKILL.md) - **CRITICAL**: EF Core conventions, repository patterns, entity configurations, and data type usage.
- [`auth-patterns`](../skills/auth-patterns/SKILL.md) - Authentication and authorization rules, including user ID extraction.
- [`error-tracking`](../skills/error-tracking/SKILL.md) - Guidance on logging and error handling.

**Enforcement Level**: ENFORCE (Blocks violations during review)
