# Dependency Rules - Complete Reference

> **Project-Agnostic Dependency Rules**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

## Dependency Matrix

| Layer | Can Reference | Cannot Reference | Framework Dependencies Allowed |
|-------|---------------|------------------|-------------------------------|
| **{Project}.Domain** | Nothing | Everything | None (pure C#) |
| **{Project}.Application** | Domain | Infrastructure, Persistence, API, Blazor | MediatR, FluentValidation, AutoMapper |
| **{Project}.Persistence** | Application, Domain | API, Blazor | EF Core, Database Provider, PostGIS |
| **{Project}.Infrastructure** | Application, Domain | API, Blazor | Any (email, file storage, external APIs) |
| **{Project}.API** | All | None (top layer) | ASP.NET Core, Swashbuckle, Serilog |
| **{Project}.Blazor** | All | None (top layer) | Blazor, MudBlazor, SignalR |
| **{Project}.Blazor.Client** | Shared DTOs | Server components | Blazor WebAssembly, MudBlazor |

## Visual Dependency Flow

```mermaid
graph TD
    subgraph Presentation Layer
        A[{Project}.API]
        B[{Project}.Blazor]
    end

    subgraph Infrastructure Layer
        C[{Project}.Persistence]
        D[{Project}.Infrastructure]
    end

    subgraph Application Layer
        E[{Project}.Application]
    end

    subgraph Domain Layer
        F[{Project}.Domain]
    end

    A --> E
    A --> C
    A --> D

    B --> E
    B --> C
    B --> D

    C --> E
    C --> F

    D --> E
    D --> F

    E --> F

    style A fill:#fbe5c5,stroke:#333
    style B fill:#fbe5c5,stroke:#333
    style C fill:#c5fbe5,stroke:#333
    style D fill:#c5fbe5,stroke:#333
    style E fill:#e5c5fb,stroke:#333
    style F fill:#c5fbc5,stroke:#333
```

## The Dependency Inversion Principle (DIP)

**Core Idea**: High-level modules should not depend on low-level modules. Both should depend on abstractions.

### Example: Application needs Database Access

**❌ WITHOUT Dependency Inversion** (Violation):
```
Application ──────> Infrastructure
(high-level)        (low-level, concrete)

Create{Entity}CommandHandler → {DbContext}
```
*Problem*: Application layer depends on concrete Infrastructure implementation.

**✅ WITH Dependency Inversion** (Correct):
```
Application ──────> I{Entity}Repository (Interface)
                           ▲
                           │ implements
Infrastructure ────────────┘
{Entity}Repository (implements I{Entity}Repository)
```
*Solution*: Both depend on abstraction (interface) defined in Application.

## Allowed `using` Statements by Layer

### {Project}.Domain

```csharp
// File: {Project}.Domain/{Entity}.cs
namespace {Project}.Domain;

// ✅ ALLOWED - Pure C# only
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

public class {Entity}
{
    public {IdType} Id { get; set; }

    [ForeignKey("{LookupEntity}")]
    public {LookupIdType} {LookupEntity}Id { get; set; }
    public {LookupEntity} {LookupEntity} { get; set; }

    [ForeignKey("{RelatedEntity}")]
    public {IdType} {RelatedEntity}Id { get; set; }
    public {RelatedEntity} {RelatedEntity} { get; set; }

    // ... more navigation properties
}

// ❌ NOT ALLOWED in Domain
using Microsoft.EntityFrameworkCore;           // Infrastructure concern
using {Project}.Application;                   // Layer above
using {Project}.Infrastructure;                // Layer above
using Microsoft.AspNetCore.Mvc;                // Presentation concern
using MediatR;                                 // Application concern
```

### {Project}.Application

```csharp
// File: {Project}.Application/Features/{Entities}/Handlers/Commands/Create{Entity}CommandHandler.cs
namespace {Project}.Application.Features.{Entities}.Handlers.Commands;

// ✅ ALLOWED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;                                       // Application framework
using {Project}.Application.Contracts.Persistence;     // Same layer
using {Project}.Application.DTOs.{Entity};             // Same layer
using {Project}.Application.DTOs.{Entity}.Validators;  // Same layer
using {Project}.Application.Features.{Entities}.Requests.Commands;  // Same layer
using {Project}.Application.Responses;                 // Same layer
using {Project}.Domain;                                // Domain entities
using MediatR;                                         // CQRS framework

// ❌ NOT ALLOWED
using {Project}.Persistence;                   // Infrastructure layer
using {Project}.Infrastructure;                // Infrastructure layer
using {Project}.API;                           // Presentation layer
using Microsoft.EntityFrameworkCore;           // Infrastructure concern
using Microsoft.AspNetCore.Mvc;                // Presentation concern
```

### {Project}.Persistence

```csharp
// File: {Project}.Persistence/Repositories/{Entity}Repository.cs
namespace {Project}.Persistence.Repositories;

// ✅ ALLOWED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using {Project}.Application.Contracts.Persistence;     // Application interfaces
using {Project}.Domain;                                // Domain entities
using Microsoft.EntityFrameworkCore;                   // ORM framework

// ❌ NOT ALLOWED
using {Project}.API;                           // Presentation layer
using {Project}.Blazor;                        // Presentation layer
using Microsoft.AspNetCore.Mvc;                // Presentation concern
```

### {Project}.Infrastructure

```csharp
// File: {Project}.Infrastructure/Email/EmailService.cs
namespace {Project}.Infrastructure.Email;

// ✅ ALLOWED
using {Project}.Domain;                                   // Domain entities
using {Project}.Application.Contracts.Infrastructure;     // Application interfaces
using SendGrid;                                           // External service
using Azure.Storage.Blobs;                                // External service

// ❌ NOT ALLOWED
using {Project}.API;                           // Presentation layer
using {Project}.Blazor;                        // Presentation layer
using Microsoft.AspNetCore.Mvc;                // Presentation concern
```

### {Project}.API

```csharp
// File: {Project}.API/Controllers/{Entity}Controller.cs
namespace {Project}.API.Controllers;

// ✅ ALLOWED (Everything)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using {Project}.Application.DTOs.{Entity};                          // DTOs
using {Project}.Application.Features.{Entities}.Requests.Commands;
using {Project}.Application.Features.{Entities}.Requests.Queries;
using {Project}.Application.Responses;                              // Response wrappers
using MediatR;                                                      // CQRS
using Microsoft.AspNetCore.Authorization;                           // Auth
using Microsoft.AspNetCore.Http;                                    // HTTP context
using Microsoft.AspNetCore.Mvc;                                     // Controllers
// ... any other dependencies
```

## Project Reference Rules (.csproj)

### {Project}.Domain.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <!-- ✅ NO PROJECT REFERENCES -->
  <ItemGroup>
    <!-- Only System.ComponentModel.Annotations for [ForeignKey] -->
    <PackageReference Include="System.ComponentModel.Annotations" Version="10.0.0" />
  </ItemGroup>
</Project>
```

### {Project}.Application.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <!-- ✅ ALLOWED: Reference to Domain -->
    <ProjectReference Include="..\{Project}.Domain\{Project}.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- ✅ ALLOWED: Framework packages -->
    <PackageReference Include="MediatR" Version="12.4.0" />
    <PackageReference Include="FluentValidation" Version="11.9.0" />
    <PackageReference Include="AutoMapper" Version="13.0.1" />
  </ItemGroup>

  <!-- ❌ NOT ALLOWED: References to Infrastructure, Persistence, API, Blazor -->
</Project>
```

### {Project}.Persistence.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <!-- ✅ ALLOWED: References to Domain and Application -->
    <ProjectReference Include="..\{Project}.Domain\{Project}.Domain.csproj" />
    <ProjectReference Include="..\{Project}.Application\{Project}.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- ✅ ALLOWED: EF Core and database packages -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
  </ItemGroup>
</Project>
```

### {Project}.API.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <!-- ✅ ALLOWED: References to ALL -->
    <ProjectReference Include="..\{Project}.Application\{Project}.Application.csproj" />
    <ProjectReference Include="..\{Project}.Infrastructure\{Project}.Infrastructure.csproj" />
    <ProjectReference Include="..\{Project}.Persistence\{Project}.Persistence.csproj" />
  </ItemGroup>
</Project>
```

## Verification Commands

### Check Project References
```bash
# Domain should have NO project references
dotnet list {Project}.Domain/{Project}.Domain.csproj reference

# Application should ONLY reference Domain
dotnet list {Project}.Application/{Project}.Application.csproj reference

# Persistence should reference Domain + Application
dotnet list {Project}.Persistence/{Project}.Persistence.csproj reference
```

### Search for Violations
```bash
# Find prohibited using statements in Domain
rg "using (Microsoft\.(EntityFrameworkCore|AspNetCore)|{Project}\.(Application|Infrastructure|API|Blazor))" {Project}.Domain/

# Find validation annotations in Domain (except [ForeignKey])
rg "\[Required\]|\[MaxLength\]|\[Range\]|\[StringLength\]" {Project}.Domain/
```

## Common Questions

### Q: Can Domain use System.ComponentModel.DataAnnotations?
**A**: LIMITED USE. Only `[ForeignKey]` is acceptable for EF Core relationships. Avoid `[Required]`, `[MaxLength]`, `[Range]` etc.

```csharp
// File: {Project}.Domain/{Entity}.cs
public class {Entity}
{
    public {IdType} Id { get; set; }

    // ✅ OK - Specifies foreign key relationship
    [ForeignKey("{LookupEntity}")]
    public {LookupIdType} {LookupEntity}Id { get; set; }
    public {LookupEntity} {LookupEntity} { get; set; }

    // ❌ NOT OK - Validation annotations
    // [Required]
    // [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
}
```

**Why**: `[ForeignKey]` is metadata for EF Core navigation, not validation. Use FluentValidation in Application layer for validation rules.

### Q: Can Application use AutoMapper?
**A**: ✅ YES. AutoMapper is an Application-layer concern for mapping Domain entities to DTOs.

```csharp
// File: {Project}.Application/Features/{Entities}/Handlers/Queries/Get{Entity}ListRequestHandler.cs
public class Get{Entity}ListRequestHandler : IRequestHandler<Get{Entity}ListRequest, List<{Entity}ListDto>>
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly IMapper _mapper;

    public async Task<List<{Entity}ListDto>> Handle(Get{Entity}ListRequest request, CancellationToken cancellationToken)
    {
        var {entities} = await _{entity}Repository.Get{Entities}WithDetails();  // Returns List<{Entity}>
        return _mapper.Map<List<{Entity}ListDto>>({entities});  // ✅ Maps to DTOs
    }
}
```

### Q: Can Application reference EF Core for IQueryable?
**A**: ❌ NO. Define repository interfaces that return `Task<List<T>>` instead. Let Infrastructure handle IQueryable.

**Correct Pattern**:
```csharp
// File: {Project}.Application/Contracts/Persistence/I{Entity}Repository.cs
namespace {Project}.Application.Contracts.Persistence;

public interface I{Entity}Repository : IGenericRepository<{Entity}, {IdType}>
{
    Task<{Entity}?> Get{Entity}WithDetails({IdType} id);
    Task<List<{Entity}>> Get{Entities}WithDetails();  // ✅ Returns List<{Entity}>, not IQueryable
}

// File: {Project}.Persistence/Repositories/{Entity}Repository.cs
public class {Entity}Repository : GenericRepository<{Entity}, {IdType}>, I{Entity}Repository
{
    public async Task<List<{Entity}>> Get{Entities}WithDetails()
    {
        return await _dbContext.{Entities}  // ✅ EF Core IQueryable handled here
            .Include(e => e.{LookupEntity})
            .Include(e => e.{RelatedEntity})
            .ToListAsync();  // ✅ Materializes to List
    }
}
```

### Q: Where do I put Email sending logic?
**A**:
- **Interface**: `{Project}.Application/Contracts/Infrastructure/IEmailService.cs`
- **Implementation**: `{Project}.Infrastructure/Services/EmailService.cs`
- **Usage**: Application layer uses `IEmailService`, Infrastructure provides concrete implementation

### Q: Can Blazor.Client reference Persistence?
**A**: ❌ NO. Blazor.Client runs in the browser (WebAssembly) and cannot access databases directly. Use shared DTOs and API calls.

**Correct Pattern**:
```csharp
// Shared DTO in {Project}.Application
public record {Entity}ListDto
{
    public {IdType} Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string {LookupEntity}Name { get; init; } = string.Empty;
}

// API endpoint in {Project}.API
[HttpGet]
[AllowAnonymous]
public async Task<ActionResult<List<{Entity}ListDto>>> GetAll()
{
    var {entities} = await _mediator.Send(new Get{Entity}ListRequest());
    return Ok({entities});
}

// Blazor.Client calls API
@inject HttpClient Http

var {entities} = await Http.GetFromJsonAsync<List<{Entity}ListDto>>("/api/v1/{entity}");
```

---

**Next**: See [violation-examples.md](violation-examples.md) for common mistakes and [fix-patterns.md](fix-patterns.md) for comprehensive fix strategies.
