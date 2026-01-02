# CLAUDE.md — ISLAMU Event Project Reference

> **Single Source of Truth for AI Agents and Team Collaboration**
>
> This document provides comprehensive context for working with the ISLAMU Event codebase.
> Last Updated: December 2025

## What this is
This file is the entrypoint. Detailed docs are imported from `docs/claude/`.

## Project
@docs/PROJECT.md

## Architecture & Technical Stack
@docs/ARCHITECTURE.md

## Domain Model & Business Logic
@docs/DOMAIN.md

## Code Conventions & Standards
@docs/CONVENTIONS.md

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

### Development URLs (Default)

| Service | URL |
|---------|-----|
| Aspire Dashboard | `https://localhost:17225` |
| API | `https://localhost:7001` |
| Blazor | `https://localhost:7002` |
| Scalar API Docs | `https://localhost:7001/scalar/v1` |
| Swagger UI | `https://localhost:7001/swagger` |
