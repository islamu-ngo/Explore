# CLAUDE.md — ISLAMU Event Project Reference

> **Single Source of Truth for AI Agents and Team Collaboration**
>
> This document provides comprehensive context for working with the ISLAMU Event codebase.
> Last Updated: December 2025

## What this is
This file is the entrypoint. Detailed docs are imported from `docs/`.

## Project
@docs/PROJECT.md

## Architecture & Technical Stack
@docs/ARCHITECTURE.md

## Domain Model & Business Logic
@docs/DOMAIN.md

## Security Architecture (AuthN/AuthZ)
@docs/SECURITY.md

## API
@docs/API.md

## Federation (W3C ATProto & ActivityPub)
@docs/FEDERATION.md

## Configuration
@docs/CONFIGURATION.md

## Operations (Deployment, Env Vars)
@docs/OPERATIONS.md

## Governance (Contributing)
@docs/GOVERNANCE.md

## Troubleshooting
@docs/TROUBLESHOOTING.md

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
dotnet test tests/Explore.Application.Tests/
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

## Rules
- Only write inside this repo project folder, never in users folder, only edits and changes you can make are project specific (not in C:\Users\*\.claude\ for example or anywhere outside this project folder!)
- When Gettings build errors after making changes, stop trying to build again! Get the errors and work on them and skip building until you have fixed the errors. Only Get certain amounts of trys to build again after fixing errors. If those trys fail, continue working on fixing the errors without building until you are sure the errors are fixed.
- Always use int instead of long unless absolutely necessary.
- never Add default values for properties Inside Domain Entities.
- Do not remove using imports in files even if they appear unused.
- Always follow Clean Architecture principles.
- Always Follow SOLID principles.
- Always follow C# coding conventions as per .editorconfig or standard .NET conventions.
- Never run rm -rf commands or delete files/folders unless explicitly instructed!
- Navigation properties for link/mapping tables should be readonly (for querying only). Writes should go through the mapping table repository directly!