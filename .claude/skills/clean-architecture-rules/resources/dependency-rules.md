# Dependency Rules - Complete Reference

## Dependency Matrix

| Layer | Can Reference | Cannot Reference | Framework Dependencies Allowed |
|-------|---------------|------------------|-------------------------------|
| **Explore.Domain** | Nothing | Everything | None (pure C#) |
| **Explore.Application** | Domain | Infrastructure, Persistence, API, Blazor | MediatR, FluentValidation, AutoMapper |
| **Explore.Persistence** | Application, Domain | API, Blazor | EF Core, Npgsql, NetTopologySuite |
| **Explore.Infrastructure** | Application, Domain | API, Blazor | Any (email, file storage, external APIs) |
| **Explore.API** | All | None (top layer) | ASP.NET Core, Swashbuckle, Serilog |
| **Explore.Blazor** | All | None (top layer) | Blazor, MudBlazor, SignalR |
| **Explore.Blazor.Client** | Shared DTOs | Server components | Blazor WebAssembly, MudBlazor |

## Visual Dependency Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                    ISLAMU Event Dependency Flow                     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│                         DOMAIN                                      │
│                    Explore.Domain                                   │
│              ┌──────────────────────┐                               │
│              │  • Event.cs          │                               │
│              │  • Organization.cs   │                               │
│              │  • EventStatus.cs    │                               │
│              │  NO DEPENDENCIES     │                               │
│              └──────────────────────┘                               │
│                         ▲                                           │
│                         │                                           │
│                         │ References Domain                         │
│                         │                                           │
│                    APPLICATION                                      │
│               Explore.Application                                   │
│         ┌─────────────────────────────────┐                         │
│         │  • CreateEventCommand.cs        │                         │
│         │  • GetEventByIdQuery.cs         │                         │
│         │  • IEventRepository.cs          │───┐                     │
│         │  • EventDto.cs                  │   │ Defines             │
│         │                                 │   │ Interfaces          │
│         │  Dependencies:                  │   │                     │
│         │  → Explore.Domain ✓             │   │                     │
│         │  → MediatR ✓                    │   │                     │
│         │  → FluentValidation ✓           │   │                     │
│         └─────────────────────────────────┘   │                     │
│                         ▲                      │                     │
│                         │                      │                     │
│                         │ References           │                     │
│                         │ App + Domain         │                     │
│                         │                      │ Implements          │
│            ┌────────────┴──────────┐           │                     │
│            │                       │           │                     │
│       PERSISTENCE            INFRASTRUCTURE    │                     │
│  Explore.Persistence      Explore.Infrastructure│                    │
│  ┌─────────────────────┐  ┌──────────────────┐ │                     │
│  │ • ApplicationDbContext  │ • EmailService.cs│◄┘                     │
│  │ • EventRepository.cs│──┼─│ • FileService.cs │                     │
│  │ • EventConfiguration│  │ │                  │                     │
│  │                     │  │ │ Dependencies:    │                     │
│  │ Dependencies:       │  │ │ → App/Domain ✓   │                     │
│  │ → App/Domain ✓      │  │ │ → SendGrid ✓     │                     │
│  │ → EF Core ✓         │  │ │ → Azure Blob ✓   │                     │
│  │ → Npgsql ✓          │  │ └──────────────────┘                     │
│  │ → PostGIS ✓         │  │                                          │
│  └─────────────────────┘  │                                          │
│            ▲               ▲                                         │
│            │               │                                         │
│            │ References ALL layers (Composition Root)                │
│            │               │                                         │
│      ┌─────┴───────────────┴─────┐                                  │
│      │                           │                                  │
│     API                      BLAZOR                                 │
│  Explore.API            Explore.Blazor                              │
│  ┌─────────────────┐   ┌──────────────────┐                         │
│  │ • EventsController  │ • EventsList.razor  │                      │
│  │ • Program.cs    │   │ • Program.cs     │                         │
│  │ • DI Registration   │ • DI Registration│                         │
│  │                 │   │                  │                         │
│  │ References:     │   │ References:      │                         │
│  │ → All layers ✓  │   │ → All layers ✓   │                         │
│  └─────────────────┘   └──────────────────┘                         │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## The Dependency Inversion Principle (DIP)

**Core Idea**: High-level modules should not depend on low-level modules. Both should depend on abstractions.

### Example: Application needs Database Access

**❌ WITHOUT Dependency Inversion** (Violation):
```
Application ──────> Infrastructure
(high-level)        (low-level, concrete)

CreateEventHandler → ApplicationDbContext
```
*Problem*: Application layer depends on concrete Infrastructure implementation.

**✅ WITH Dependency Inversion** (Correct):
```
Application ──────> IEventRepository (Interface)
                           ▲
                           │ implements
Infrastructure ────────────┘
EventRepository (implements IEventRepository)
```
*Solution*: Both depend on abstraction (interface) defined in Application.

## Allowed `using` Statements by Layer

### Explore.Domain
```csharp
// ✅ ALLOWED
using System;
using System.Collections.Generic;
using System.Linq;

// ❌ NOT ALLOWED
using Microsoft.EntityFrameworkCore;           // Infrastructure concern
using Explore.Application;                     // Layer above
using Explore.Infrastructure;                  // Layer above
using Microsoft.AspNetCore.Mvc;                // Presentation concern
using MediatR;                                 // Application concern
```

### Explore.Application
```csharp
// ✅ ALLOWED
using Explore.Domain.Entities;                 // Domain entities
using Explore.Domain.Enums;                    // Domain enums
using MediatR;                                 // CQRS framework
using FluentValidation;                        // Validation framework
using AutoMapper;                              // Mapping framework

// ❌ NOT ALLOWED
using Explore.Persistence;                     // Infrastructure layer
using Explore.Infrastructure;                  // Infrastructure layer
using Explore.API;                             // Presentation layer
using Microsoft.EntityFrameworkCore;           // Infrastructure concern
using Microsoft.AspNetCore.Mvc;                // Presentation concern
```

### Explore.Persistence
```csharp
// ✅ ALLOWED
using Explore.Domain.Entities;                 // Domain entities
using Explore.Application.Interfaces;          // Application interfaces
using Microsoft.EntityFrameworkCore;           // ORM framework
using Npgsql.EntityFrameworkCore.PostgreSQL;   // Database provider
using NetTopologySuite;                        // PostGIS support

// ❌ NOT ALLOWED
using Explore.API;                             // Presentation layer
using Explore.Blazor;                          // Presentation layer
using Microsoft.AspNetCore.Mvc;                // Presentation concern
```

### Explore.Infrastructure
```csharp
// ✅ ALLOWED
using Explore.Domain.Entities;                 // Domain entities
using Explore.Application.Interfaces;          // Application interfaces
using SendGrid;                                // External service
using Azure.Storage.Blobs;                     // External service

// ❌ NOT ALLOWED
using Explore.API;                             // Presentation layer
using Explore.Blazor;                          // Presentation layer
using Microsoft.AspNetCore.Mvc;                // Presentation concern
```

### Explore.API / Explore.Blazor
```csharp
// ✅ ALLOWED (Everything)
using Explore.Domain.Entities;
using Explore.Application.Commands;
using Explore.Infrastructure.Services;
using Explore.Persistence;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using MudBlazor;
// ... any other dependencies
```

## Project Reference Rules (.csproj)

### Explore.Domain.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <!-- ✅ NO PROJECT REFERENCES -->
  <!-- ✅ NO NUGET PACKAGES (or minimal - just primitives) -->
</Project>
```

### Explore.Application.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <!-- ✅ ALLOWED: Reference to Domain -->
    <ProjectReference Include="..\Explore.Domain\Explore.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- ✅ ALLOWED: Framework packages -->
    <PackageReference Include="MediatR" />
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="AutoMapper" />
  </ItemGroup>

  <!-- ❌ NOT ALLOWED: References to Infrastructure, Persistence, API, Blazor -->
</Project>
```

### Explore.Persistence.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <!-- ✅ ALLOWED: References to Domain and Application -->
    <ProjectReference Include="..\Explore.Domain\Explore.Domain.csproj" />
    <ProjectReference Include="..\Explore.Application\Explore.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- ✅ ALLOWED: EF Core and database packages -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="NetTopologySuite.IO.GeoJSON" />
  </ItemGroup>
</Project>
```

### Explore.API.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <!-- ✅ ALLOWED: References to ALL -->
    <ProjectReference Include="..\Explore.Domain\Explore.Domain.csproj" />
    <ProjectReference Include="..\Explore.Application\Explore.Application.csproj" />
    <ProjectReference Include="..\Explore.Persistence\Explore.Persistence.csproj" />
    <ProjectReference Include="..\Explore.Infrastructure\Explore.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

## Verification Commands

### Check Project References
```bash
# Domain should have NO project references
dotnet list src/Explore.Domain/Explore.Domain.csproj reference

# Application should ONLY reference Domain
dotnet list src/Explore.Application/Explore.Application.csproj reference

# Persistence should reference Domain + Application
dotnet list src/Explore.Persistence/Explore.Persistence.csproj reference
```

### Search for Violations
```bash
# Find prohibited using statements in Domain
rg "using (Microsoft\.EntityFrameworkCore|Explore\.(Application|Infrastructure|API|Blazor))" src/Explore.Domain/

# Find prohibited using statements in Application
rg "using (Microsoft\.EntityFrameworkCore|Explore\.(Infrastructure|Persistence|API|Blazor))" src/Explore.Application/
```

## Common Questions

### Q: Can Domain use System.ComponentModel.DataAnnotations?
**A**: ❌ NO. Data annotations are for presentation/validation concerns. Use FluentValidation in Application layer instead.

**Why**: Domain should be pure business logic. Validation rules can change based on use case (create vs update), so they belong in Application.

### Q: Can Application use AutoMapper?
**A**: ✅ YES. AutoMapper is an Application-layer concern for mapping Domain entities to DTOs.

### Q: Can Application reference EF Core for IQueryable?
**A**: ❌ NO. Define repository interfaces that return `Task<List<T>>` instead. Let Infrastructure handle IQueryable.

**Correct Pattern**:
```csharp
// Application layer - Interface
public interface IEventRepository
{
    Task<List<Event>> GetByStatusAsync(EventStatus status, CancellationToken cancellationToken);
}

// Infrastructure layer - Implementation uses EF Core
public class EventRepository : IEventRepository
{
    public async Task<List<Event>> GetByStatusAsync(EventStatus status, CancellationToken cancellationToken)
    {
        return await _context.Events
            .Where(e => e.Status == status)
            .ToListAsync(cancellationToken);
    }
}
```

### Q: Where do I put Email sending logic?
**A**:
- **Interface**: `Explore.Application/Interfaces/IEmailService.cs`
- **Implementation**: `Explore.Infrastructure/Services/EmailService.cs`
- **Usage**: Application layer uses `IEmailService`, Infrastructure provides SendGrid implementation

### Q: Can Blazor.Client reference Persistence?
**A**: ❌ NO. Blazor.Client runs in the browser (WebAssembly) and cannot access databases directly. Use shared DTOs and API calls.

**Correct Pattern**:
```csharp
// Shared DTO in Explore.Application
public record EventDto(Guid Id, string Title, DateTime StartsAt);

// API endpoint in Explore.API
[HttpGet]
public async Task<ActionResult<List<EventDto>>> GetEvents()
{
    var query = new GetEventListQuery();
    var result = await _mediator.Send(query);
    return Ok(result);
}

// Blazor.Client calls API
@inject HttpClient Http

var events = await Http.GetFromJsonAsync<List<EventDto>>("/api/v1/events");
```

---

**Next**: See [layer-responsibilities.md](layer-responsibilities.md) for what code belongs in each layer.
