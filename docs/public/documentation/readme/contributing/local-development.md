---
description: Set up your local developer workstation to contribute code to ISLAMU Event.
---

# Local Development Guide

This guide helps developers set up a local development workstation to contribute to ISLAMU Event.

---

## 1. Prerequisites

To build and run the complete solution from source, install:

- **.NET 10 SDK** (v10.0.302 or compatible)
- **Docker Engine / Docker Desktop** (v24+ with Compose v2)
- **.NET Aspire CLI**:
  ```bash
  dotnet tool install -g Aspire.Cli
  ```
- **Git**

---

## 2. Clone & Run with .NET Aspire

We use .NET Aspire for local orchestration, automatically launching PostgreSQL, Keycloak, Mailpit, `Explore.API`, and `Explore.Blazor`:

```bash
# 1. Clone repository
git clone https://github.com/islamu-ngo/Event.git
cd Event

# 2. Copy configuration
cp .env.example .env

# 3. Launch with Aspire AppHost
aspire run --apphost Explore.AppHost/Explore.AppHost.csproj
```

Open the **Aspire Dashboard** URL displayed in your terminal (typically `http://localhost:18888`) to view running resources, inspect logs, and navigate to the application UI.

---

## 3. Building and Testing

Build the solution in Release configuration:

```bash
dotnet build --configuration Release --verbosity quiet
```

Run tests using TUnit:

```bash
# Run unit tests for Application layer
dotnet test tests/Explore.Application.Tests/Explore.Application.Tests.csproj
```

---

## 4. Where to Learn More

For full engineering specifications, invariants, and coding conventions, refer to the internal documentation in GitHub:

- 📖 **[Developer Guide](https://github.com/islamu-ngo/Event/blob/develop/docs/internal/DEVELOPER_GUIDE.md)**: 5-minute mental model, invariants, and coding style.
- 📖 **[Architecture Overview](https://github.com/islamu-ngo/Event/blob/develop/docs/internal/ARCHITECTURE_OVERVIEW.md)**: C4 container diagrams and component interactions.
- 📖 **[Contributor Recipes](https://github.com/islamu-ngo/Event/blob/develop/docs/internal/CONTRIBUTOR_RECIPES.md)**: Blueprints for adding entities, CQRS slices, and endpoints.
- 📖 **[Testing Guide](https://github.com/islamu-ngo/Event/blob/develop/docs/internal/TESTING.md)**: TUnit conventions and Testcontainers test lanes.
