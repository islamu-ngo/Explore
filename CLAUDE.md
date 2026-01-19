# CLAUDE.md — ISLAMU Event Project Reference

> **Single Source of Truth for AI Agents and Team Collaboration**
>
> This document provides comprehensive context for working with the ISLAMU Event codebase.
> Last Updated: January 2026

## What this is
This file is the entrypoint. Detailed docs are imported from `docs/`.

## Documentation Template System

This project uses **project-agnostic documentation** with placeholder syntax `{Placeholder}`.
All skills, agents, and governance docs use placeholders for reusability.

**Template Glossary**: [@docs/TEMPLATE_GLOSSARY.md](docs/TEMPLATE_GLOSSARY.md) - Defines all placeholders

**This Repository's Substitutions**:
| Placeholder | Value |
|-------------|-------|
| `{Project}` | `Explore` |
| `{DbContext}` | `ExploreDbContext` |
| `{IdType}` | `Guid` |
| `{LookupIdType}` | `int` |

## ⚠️ CRITICAL RULES - Quick Reference

**MUST READ**: These 10 rules are based on 45+ entity implementations. Never violate them.

1. **Repositories Return ENTITIES, Never DTOs** - Map to DTOs in handlers
2. **Validators Use Manual Instantiation (NOT DI)** - `var validator = new CreateEventDtoValidator(_repo1, _repo2);`
3. **Navigation Properties Are Readonly** - Use repository for writes: `_memberRepository.Create(member)`
4. **Use int Instead of long** - Except size/cursor fields or absolutly necessery
5. **No Default Values in Entities** - Set in handler: `@event.TotalViews = 0;`
6. **Do Not Remove Using Statements** - Keep ALL using statements (except errors or old names that were refactored)
7. **Commands Return BaseCommandResponse<Guid>** - Not just `Guid`
8. **GET = AllowAnonymous, Write = Authorize** - Public read, protected write
9. **Extract UserId with Fallback** - `sub` → `nameidentifier` → `sid`
10. **File-Scoped Namespaces** - `namespace Explore.Application.Features.Events;`

**Full Details**: [@docs/QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md)

## API documentation standard (controllers)

All API controller actions must include:

- `[EndpointSummary]` and `[EndpointDescription]`
- `[ProducesResponseType]` for success + common failures (use `typeof(...)` when applicable)
- `[Consumes("application/json")]` for JSON body endpoints

## Project
@docs/PROJECT.md

## Architecture & Technical Stack
docs/ARCHITECTURE.md

## Domain Model & Business Logic
docs/DOMAIN.md

## Security Architecture (AuthN/AuthZ)
docs/SECURITY.md

## API
docs/API.md

## Blazor Frontend (Server + WASM)
docs/BLAZOR.md

## Federation (W3C ATProto & ActivityPub)
docs/FEDERATION.md

## Configuration
docs/CONFIGURATION.md

## Operations (Deployment, Env Vars)
docs/OPERATIONS.md

## Governance
@docs/GOVERNANCE.md

## Troubleshooting
docs/TROUBLESHOOTING.md

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run with Aspire orchestrator (recommended for development)
dotnet run --project Explore.AppHost/Explore.AppHost.csproj

# Run tests
dotnet test

# Run specific test project
dotnet test Event.Application.UnitTests
```

### Database Schema
@schema/islamu-event.md

### Development URLs (Default)

| Service | URL |
|---------|-----|
| Aspire Dashboard | `https://localhost:17225` |
| API | `https://localhost:7001` |
| Blazor | `https://localhost:7002` |
| Scalar API Docs | `https://localhost:7001/scalar/v1` |
| Swagger UI | `https://localhost:7001/swagger` |

Always use Context7 MCP when I need library/API documentation, code generation, setup or configuration steps without me having to explicitly ask.

Always use sequential-thinking MCP when I need to:
- Break down complex problems into manageable steps
- Revise and refine thoughts as understanding deepens
- Branch into alternative paths of reasoning
- Adjust the total number of thoughts dynamically
- Generate and verify solution hypotheses

## Context, plans, and task management
ALWAYS refer to this file and all the files in @dev/active/ that contain context, plan, tasks...
@dev/active/README.md
@dev/active/blazor-feature-parity/blazor-feature-parity-plan.md

## Rules
- Only write inside this repo project folder, never in users folder (not in C:\Users\*\.claude\ or anywhere outside this project folder)
- When getting build errors, stop building! Get the errors, fix them, skip building until fixed. Limited retry attempts, then fix without building until confident.
- Always follow Clean Architecture principles and SOLID principles
- Always follow C# coding conventions as per .editorconfig or standard .NET conventions
- NEVER run rm -rf commands or delete files/folders unless explicitly instructed - instead report files that should be deleted
- Navigation properties for link/mapping tables are readonly (querying only). Writes go through mapping table repository directly

