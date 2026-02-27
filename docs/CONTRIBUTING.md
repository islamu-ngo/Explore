ABOUTME: Contribution workflow aligned with current build, test, and client-generation behavior.
ABOUTME: Highlights the non-obvious DTO-to-client synchronization flow.

# Contributing

## Prerequisites
- .NET 10 SDK (preview pinned by `global.json`).
- Docker (for container-based local stack).

## Branch And Commit
- Branch prefixes: `feature/`, `bugfix/`, `refactor/`, `docs/`.
- Use clear commit messages (e.g., `feat(api): add tenant policy endpoint`).

## Required Validation Before PR
Run from solution root:

1. `dotnet build --configuration Release --verbosity quiet`
2. Run each test project with `dotnet test --project <path>.csproj --configuration Release --verbosity quiet`
3. Use `CLAUDE.md` as the canonical current list of required test project paths.

## DTO Change Workflow (API -> Blazor Client)
When DTO contracts change, sequence matters.

1. Update DTOs, validators, mappings, and handlers in API/Application layers.
2. Build API first: `dotnet build --project Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`
3. Run API/AppHost in Development so `Explore.API/swagger.json` is refreshed.
4. Build Blazor client: `dotnet build --project Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet`
5. Update Blazor services/components that use the generated client types.
6. Rebuild and rerun tests.

## Why This Sequence Is Required
If UI code is changed before client regeneration, you can get false compile failures because generated types still reflect the old API contract.

## Pull Request Checklist
- Scope is focused and independently testable.
- Build and required test projects pass.
- API contract changes include docs updates (`docs/API.md`, `docs/API_CHANGELOG.md` when relevant).
- Breaking changes are explicitly documented.
