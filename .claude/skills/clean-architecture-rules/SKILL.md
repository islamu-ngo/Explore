---
name: clean-architecture-rules
description: Enforces Clean Architecture dependency rules (Domain → Application → Infrastructure → API/Blazor). Blocks violations to maintain architectural integrity.
type: guardrail
enforcement: block
priority: critical
---

# Clean Architecture Dependency Rules

## 🎯 Purpose

This is a **CRITICAL GUARDRAIL** that enforces Clean Architecture's fundamental dependency rule: **dependencies flow inward only**. Violations are **BLOCKED** to prevent architectural degradation.

## ⚡ When This Skill Activates

**Automatically BLOCKS when**:
- Attempting to add wrong project references
- Importing namespaces that violate dependency rules
- Detecting prohibited `using` statements in Domain or Application layers

**Triggered by**:
- Keywords: "dependency", "reference", "architecture", "layer", "add project"
- File patterns: Domain/**/*.cs, Application/**/*.cs
- Content patterns: `using Explore.Infrastructure`, `using Microsoft.EntityFrameworkCore` in Domain

## 🚨 The Dependency Rule

```
┌─────────────────────────────────────────────────────────────┐
│              ISLAMU EVENT ARCHITECTURE LAYERS                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              1. DOMAIN (Core)                       │   │
│  │              Explore.Domain                         │   │
│  │              ↑ NO DEPENDENCIES                      │   │
│  │  • Entities, Enums, Value Objects, Domain Events    │   │
│  │  • Pure C# - No framework dependencies             │   │
│  └─────────────────────────────────────────────────────┘   │
│                         ▲                                   │
│                         │ References                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │         2. APPLICATION (Use Cases)                  │   │
│  │         Explore.Application                         │   │
│  │         ↑ References: Domain ONLY                   │   │
│  │  • CQRS Commands/Queries, DTOs, Interfaces          │   │
│  │  • MediatR, FluentValidation, AutoMapper            │   │
│  └─────────────────────────────────────────────────────┘   │
│                         ▲                                   │
│                         │ References                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │    3. INFRASTRUCTURE (Implementation)               │   │
│  │    Explore.Persistence + Explore.Infrastructure     │   │
│  │    ↑ References: Application, Domain                │   │
│  │  • DbContext, Repositories, External APIs           │   │
│  │  • EF Core, PostgreSQL, Email, File Storage         │   │
│  └─────────────────────────────────────────────────────┘   │
│                         ▲                                   │
│                         │ References                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │       4. PRESENTATION (Entry Points)                │   │
│  │       Explore.API + Explore.Blazor                  │   │
│  │       ↑ References: ALL (Composition Root)          │   │
│  │  • Controllers, Pages, Dependency Registration      │   │
│  │  • ASP.NET Core, MudBlazor, SignalR                 │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 📚 Resources

| Resource | Description |
|----------|-------------|
| [dependency-rules.md](resources/dependency-rules.md) | Complete dependency matrix and flow diagram |
| [layer-responsibilities.md](resources/layer-responsibilities.md) | What code belongs in each layer |
| [violation-examples.md](resources/violation-examples.md) | Common violations and error messages |
| [fix-patterns.md](resources/fix-patterns.md) | How to fix violations using interfaces and DI |

## ✅ Valid Dependency Examples

```csharp
// ✅ VALID: Application references Domain
namespace Explore.Application.Features.Events.Commands;

using Explore.Domain.Entities;  // ✅ OK - App can reference Domain
using Explore.Domain.Enums;     // ✅ OK
using MediatR;                  // ✅ OK - Framework dependency

// ✅ VALID: Infrastructure references Application and Domain
namespace Explore.Persistence.Repositories;

using Explore.Application.Interfaces;  // ✅ OK - Implements interfaces
using Explore.Domain.Entities;         // ✅ OK - Works with entities
using Microsoft.EntityFrameworkCore;   // ✅ OK - Infrastructure can use EF Core

// ✅ VALID: API references all layers
namespace Explore.API.Controllers;

using Explore.Application.Features.Events.Commands;  // ✅ OK
using Explore.Infrastructure.Services;               // ✅ OK
using MediatR;                                        // ✅ OK
```

## ❌ BLOCKED Violations

```csharp
// ❌ BLOCKED: Domain referencing ANYTHING
namespace Explore.Domain.Entities;

using Microsoft.EntityFrameworkCore;  // ❌ BLOCKED! Domain must be pure
using Explore.Application.DTOs;       // ❌ BLOCKED! Dependency flows wrong way

// ❌ BLOCKED: Application referencing Infrastructure
namespace Explore.Application.Features.Events.Queries;

using Explore.Infrastructure.Persistence;  // ❌ BLOCKED! Use interfaces instead
using Explore.API.Controllers;             // ❌ BLOCKED! Wrong direction

// ❌ BLOCKED: Application referencing Presentation
namespace Explore.Application.Commands;

using Microsoft.AspNetCore.Mvc;  // ❌ BLOCKED! Application must be framework-agnostic
```

## 🔧 Quick Fix: Use Dependency Inversion

**Problem**: Application needs database access (Infrastructure)

**❌ Wrong - Direct dependency**:
```csharp
// In Explore.Application
using Explore.Infrastructure.Persistence;  // ❌ BLOCKED

public class GetEventsHandler
{
    private readonly ApplicationDbContext _context;  // ❌ Concrete class
}
```

**✅ Correct - Interface in Application, Implementation in Infrastructure**:
```csharp
// Step 1: Define interface in Application layer
// File: Explore.Application/Interfaces/IEventRepository.cs
namespace Explore.Application.Interfaces;

public interface IEventRepository
{
    Task<List<Event>> GetAllAsync(CancellationToken cancellationToken);
}

// Step 2: Use interface in Application
// File: Explore.Application/Features/Events/Queries/GetEventListHandler.cs
namespace Explore.Application.Features.Events.Queries;

using Explore.Application.Interfaces;  // ✅ OK - Same layer

public class GetEventListHandler : IRequestHandler<GetEventListQuery, List<EventDto>>
{
    private readonly IEventRepository _repository;  // ✅ Abstraction

    public GetEventListHandler(IEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<EventDto>> Handle(GetEventListQuery request, CancellationToken cancellationToken)
    {
        var events = await _repository.GetAllAsync(cancellationToken);
        return events.Select(e => e.ToDto()).ToList();
    }
}

// Step 3: Implement in Infrastructure layer
// File: Explore.Persistence/Repositories/EventRepository.cs
namespace Explore.Persistence.Repositories;

using Explore.Application.Interfaces;      // ✅ OK - Implements interface
using Explore.Domain.Entities;             // ✅ OK - Works with entities
using Microsoft.EntityFrameworkCore;       // ✅ OK - Infrastructure can use EF Core

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _context;

    public async Task<List<Event>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Events.ToListAsync(cancellationToken);
    }
}

// Step 4: Register in API/Blazor (Composition Root)
// File: Explore.API/Program.cs or Explore.AppHost/Program.cs
builder.Services.AddScoped<IEventRepository, EventRepository>();  // ✅ DI binding
```

## 🎓 Why This Matters

**Benefits of Clean Architecture**:
1. **Testability**: Domain and Application can be tested without database
2. **Flexibility**: Swap PostgreSQL for SQL Server without changing business logic
3. **Maintainability**: Business logic isolated from framework changes
4. **Team Scalability**: Clear boundaries for parallel development
5. **Deployment Options**: Domain can be reused across API, Blazor, CLI, etc.

**Cost of Violations**:
- Tight coupling makes testing difficult
- Framework upgrades break business logic
- Cannot reuse domain logic across projects
- Circular dependencies cause build failures

## 📖 Deep Dive

For comprehensive guidance:
- **Dependency Matrix**: [dependency-rules.md](resources/dependency-rules.md)
- **Layer Responsibilities**: [layer-responsibilities.md](resources/layer-responsibilities.md)
- **Common Violations**: [violation-examples.md](resources/violation-examples.md)
- **Fix Patterns**: [fix-patterns.md](resources/fix-patterns.md)

---

**Enforcement Level**: 🚨 BLOCK (Violations are prevented)
**Override**: Add `@skip-architecture-check` comment in file (use sparingly)
