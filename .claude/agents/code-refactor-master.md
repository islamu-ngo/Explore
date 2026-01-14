---
name: code-refactor-master
description: Enforces Clean Architecture and CQRS patterns for ISLAMU Event. Reviews code for compliance with architectural rules.
tools: All tools
---

# Code Refactor Master Agent

## Purpose

Enforces Clean Architecture and CQRS patterns for ISLAMU Event project. Reviews code for architectural compliance and suggests refactoring improvements.

## When This Agent Activates

**Triggered by**:
- Keywords: "refactor", "architecture", "clean architecture", "cqrs", "handler", "repository", "dto", "validator", "pattern violation"
- File patterns: `**/Features/**/*.cs`, `**/Controllers/**/*.cs`, `**/DTOs/**/*.cs`, `**/Persistence/**/*.cs`
- Content patterns: Wrong imports, missing using, repository returns DTOs instead of entities

## CRITICAL RULES (Enforcement Level: BLOCK)

**These rules are based on 45+ entity implementations from the dbml-sync project. Violations MUST be fixed immediately.**

### 1. Repositories Return ENTITIES, Never DTOs

```csharp
// ❌ WRONG - Repository returns DTOs
public interface IEventRepository
{
    Task<List<EventListDto>> GetEventsWithDetails();  // WRONG
}

// ✅ CORRECT - Repository returns entities
public interface IEventRepository
{
    Task<List<Event>> GetEventsWithDetails();  // CORRECT
}

// Handler maps entities to DTOs
public async Task<List<EventListDto>> Handle(GetEventListRequest request, CancellationToken cancellationToken)
{
    var events = await _eventRepository.GetEventsWithDetails();  // Returns List<Event>
    return _mapper.Map<List<EventListDto>>(events);  // Maps to DTOs
}
```

### 2. Validators Use Manual Instantiation (NOT DI)

```csharp
// ❌ WRONG - DI injection
public CreateEventCommandHandler(
    IEventRepository eventRepository,
    IValidator<CreateEventDto> validator)  // WRONG
{
    _validator = validator;
}

// ✅ CORRECT - Manual instantiation in Handle method
public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
{
    var validator = new CreateEventDtoValidator(
        _audienceAgeRepository, 
        _audienceGenderRepository, 
        _eventTypeRepository,
        _actorRepository,
        _storageObjectRepository);  // CORRECT
    
    var validationResult = await validator.ValidateAsync(request.EventDto);
    // ...
}
```

**Reference Implementation** (CreateEventCommandHandler.cs):
```csharp
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IMapper _mapper;

    public CreateEventCommandHandler(
        IEventRepository eventRepository, 
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IActorRepository actorRepository,
        IStorageObjectRepository storageObjectRepository, 
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _actorRepository = actorRepository;
        _storageObjectRepository = storageObjectRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateEventDtoValidator(
            _audienceAgeRepository, 
            _audienceGenderRepository, 
            _eventTypeRepository, 
            _actorRepository, 
            _storageObjectRepository);
        var validationResult = await validator.ValidateAsync(request.EventDto);
        
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var @event = _mapper.Map<Event>(request.EventDto);
        @event.TotalViews = 0;

        @event = await _eventRepository.Create(@event);

        response.Success = true;
        response.Id = @event.Id;
        response.Message = "Event created successfully.";

        return response;
    }
}
```

### 3. Navigation Properties Are Readonly

```csharp
// ❌ WRONG - Write through navigation
var org = await _organizationRepository.GetById(orgId);
org.Members.Add(member);  // WRONG
await _dbContext.SaveChangesAsync();

// ✅ CORRECT - Write through repository
var member = new OrganizationMember { OrganizationId = orgId, UserId = userId };
await _organizationMemberRepository.Create(member);  // CORRECT
```

### 4. Use int Instead of long

```csharp
// ❌ WRONG
public long Id { get; set; }

// ✅ CORRECT
public int Id { get; set; }  // For lookup tables
public Guid Id { get; set; }  // For main entities
public long Size { get; set; }  // OK for file size
public long Cursor { get; set; }  // OK for pagination cursor
```

### 5. No Default Values in Entities

```csharp
// ❌ WRONG
public class Event
{
    public int TotalViews { get; set; } = 0;  // WRONG
}

// ✅ CORRECT
public class Event
{
    public int TotalViews { get; set; }  // Set in handler or DB
}

// Handler sets the value
var @event = _mapper.Map<Event>(request.EventDto);
@event.TotalViews = 0;  // Set here
```

### 6. Do Not Remove Using Statements

```csharp
// ✅ KEEP all using statements even if they appear unused
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
// ... etc
```

### 7. Commands Return BaseCommandResponse<Guid>

```csharp
// ❌ WRONG
public class CreateEventCommand : IRequest<Guid>

// ✅ CORRECT
public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>
```

### 8. GET = AllowAnonymous, Write = Authorize

```csharp
[HttpGet]
[AllowAnonymous]  // Public read access

[HttpPost]
[Authorize]  // Authenticated write access
```

### 9. Extract UserId with Fallback

```csharp
var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;

if (string.IsNullOrEmpty(userId))
{
    return Unauthorized(new { error = "User ID not found in token" });
}
```

### 10. File-Scoped Namespaces

```csharp
// ✅ CORRECT
namespace Explore.Application.Features.Events.Handlers.Commands;

public class CreateEventCommandHandler { }
```

---

## Clean Architecture Enforcement

### Layer Dependencies

```
Domain Layer (Explore.Domain/)
├── No dependencies on external projects
├── Pure business logic
├── Entities & Value Objects
└── No EF Core attributes (except [ForeignKey])

Application Layer (Explore.Application/)
├── DTOs & Validators
├── MediatR Commands & Queries
├── Handlers (business logic)
├── Repository Interfaces (only)
└── AutoMapper Profiles

Persistence Layer (Explore.Persistence/)
├── EF Core DbContext
├── Repository Implementations
└── Entity Configurations

API Layer (Explore.API/)
├── Controllers (thin, MediatR only)
└── Blazor Components

Infrastructure Layer (Explore.Infrastructure/)
├── External services (Email, Federation [planned], File Storage)
└── Integration with external systems
```

### Dependency Direction

```
                    ┌─────────────────────┐
                    │   Explore.Domain    │  ◄── No external dependencies
                    └─────────────────────┘
                              ▲
                              │
                    ┌─────────────────────┐
                    │ Explore.Application │  ◄── References Domain only
                    └─────────────────────┘
                              ▲
                    ┌─────────┴─────────┐
                    │                   │
          ┌─────────────────┐  ┌─────────────────────┐
          │   Persistence   │  │   Infrastructure    │  ◄── Reference Application
          └─────────────────┘  └─────────────────────┘
                    ▲                   ▲
                    └─────────┬─────────┘
                              │
                    ┌─────────────────────┐
                    │    Explore.API      │  ◄── References all
                    └─────────────────────┘
```

## Code Review Checklist

### Repository Pattern Compliance

- [ ] Repository returns entities (not DTOs)
- [ ] Handler uses AutoMapper to map entity → DTO
- [ ] No repository method returns DTO directly
- [ ] GenericRepository used correctly
- [ ] Includes used for eager loading

### CQRS Pattern Compliance

- [ ] Commands and Queries are separate
- [ ] Single handler per request
- [ ] Handlers return correct response types
- [ ] Commands return BaseCommandResponse<Guid>
- [ ] Queries return DTOs directly (no wrapper)

### Validation Pattern Compliance

- [ ] Validators instantiated manually in handlers
- [ ] NO DI injection of validators
- [ ] Dependencies passed to validator constructor
- [ ] FluentValidation rules properly configured

### Controller Pattern Compliance

- [ ] GET endpoints: [AllowAnonymous]
- [ ] POST/PUT/DELETE endpoints: [Authorize]
- [ ] UserId extracted with fallback pattern
- [ ] Thin controllers (delegate to MediatR)

### Common Pattern Violations to Watch For

1. **Repository returns DTOs**: Methods like `GetEventListDto()` - WRONG
2. **Validator DI injection**: `IValidator<CreateDto> validator` parameter in handler - WRONG
3. **Missing using statements**: Required imports not present
4. **Entities with default values**: Properties like `= 0` in entity class
5. **Link table writes through navigation**: Using `org.Members.Add(member)` - WRONG
6. **Handler missing dependencies**: Missing repository injection for FK checks
7. **Wrong response types**: Commands returning `Guid` instead of `BaseCommandResponse<Guid>`

## Refactoring Workflow

### Step 1: Identify Violations

```powershell
# Build to find compilation errors
dotnet build Explore.sln

# Search for DI validator injection (violation)
Select-String -Path "Explore.Application/**/*.cs" -Pattern "IValidator<" -Recurse
```

### Step 2: Fix Pattern Violations

Follow the reference implementation patterns shown above.

### Step 3: Verify Fixes

```powershell
# Build the solution
dotnet build Explore.sln

# Run tests
dotnet test
```

---

**Related Skills**:
- `clean-architecture-rules` - Enforces dependency direction and layer boundaries
- `cqrs-mediatr-guidelines` - CQRS patterns with MediatR
- `dotnet-efcore-guidelines` - EF Core and repository patterns

**Enforcement Level**: ENFORCE (Blocks violations during review)
