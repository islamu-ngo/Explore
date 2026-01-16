# Quick Reference - ISLAMU Event Critical Rules

**Last Updated**: 2026-01-10  
**Based On**: 45+ entity implementations from dbml-sync project

---

## ⚠️ 10 CRITICAL RULES (Never Violate)

### 1. Repositories Return ENTITIES, Never DTOs ⚠️

```csharp
// ❌ WRONG - Repository returns DTOs
public interface IEventRepository
{
    Task<List<EventListDto>> GetEventsWithDetails();  // ❌ WRONG
}

// ✅ CORRECT - Repository returns entities
public interface IEventRepository
{
    Task<List<Event>> GetEventsWithDetails();  // ✅ CORRECT
}

// Handler maps entities to DTOs
public async Task<List<EventListDto>> Handle(...)
{
    var events = await _eventRepository.GetEventsWithDetails();  // Entities
    return _mapper.Map<List<EventListDto>>(events);  // DTOs
}
```

**Why**: Clean Architecture - repositories are infrastructure, DTOs are application layer.

---

### 2. Validators Use Manual Instantiation (NOT DI) ⚠️

```csharp
// ❌ WRONG - DI injection
public CreateEventCommandHandler(
    IEventRepository eventRepository,
    IValidator<CreateEventDto> validator)  // ❌ WRONG
{
    _validator = validator;
}

// ✅ CORRECT - Manual instantiation
public async Task<BaseCommandResponse<Guid>> Handle(...)
{
    // Instantiate with dependencies
    var validator = new CreateEventDtoValidator(
        _audienceAgeRepository, 
        _audienceGenderRepository, 
        _eventTypeRepository);
    
    var validationResult = await validator.ValidateAsync(request.EventDto);
}
```

**Why**: Fine-grained control, avoids DI complexity, consistent with all 45+ entities.

---

### 3. Navigation Properties on Link Tables Are Readonly ⚠️

```csharp
// ❌ WRONG - Write through navigation
var org = await _organizationRepository.GetById(orgId);
org.Members.Add(member);  // ❌ WRONG
await _dbContext.SaveChangesAsync();

// ✅ CORRECT - Write through repository
var member = new OrganizationMember 
{ 
    OrganizationId = orgId, 
    UserId = userId 
};
await _organizationMemberRepository.Create(member);  // ✅ CORRECT
```

**Why**: Tenant isolation, explicit writes, prevents accidental cross-tenant data mixing.

---

### 4. Use int Instead of long (except size/cursor) ⚠️

```csharp
// ❌ WRONG
public long Id { get; set; }

// ✅ CORRECT
public int Id { get; set; }  // For lookup tables
public Guid Id { get; set; }  // For main entities
public long Size { get; set; }  // OK for file size
public long Cursor { get; set; }  // OK for pagination cursor
```

**Why**: Project convention, consistent with all entities.

---

### 5. No Default Values in Entities ⚠️

```csharp
// ❌ WRONG
public class Event
{
    public int TotalViews { get; set; } = 0;  // ❌ WRONG
}

// ✅ CORRECT
public class Event
{
    public int TotalViews { get; set; }  // ✅ Set in handler or DB
}

// Set in handler
var @event = _mapper.Map<Event>(request.EventDto);
@event.TotalViews = 0;  // ✅ Set here
```

**Why**: Clear initialization, avoids confusion between default and database values.

---

### 6. Do Not Remove Using Statements ⚠️

```csharp
// ✅ KEEP all using statements even if they appear unused
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using MediatR;
// ... etc
```

**Why**: Build system may need them, prevents hard-to-debug issues.

---

### 7. Commands Return BaseCommandResponse<Guid> ⚠️

```csharp
// ❌ WRONG
public class CreateEventCommand : IRequest<Guid>

// ✅ CORRECT
public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>
```

**Response Structure**:
```csharp
public class BaseCommandResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Id { get; set; }
    public List<string> Errors { get; set; }
}
```

**Why**: Consistent error handling, validation messages, success/failure distinction.

---

### 8. GET = AllowAnonymous, Write = Authorize ⚠️

```csharp
[HttpGet]
[AllowAnonymous]  // ✅ Public read access
public async Task<ActionResult<List<EventListDto>>> GetAll() { }

[HttpPost]
[Authorize]  // ✅ Authenticated write access
public async Task<ActionResult<BaseCommandResponse<Guid>>> Create(...) { }

[HttpPut("{id}")]
[Authorize]  // ✅ Authenticated write access
public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(...) { }

[HttpDelete("{id}")]
[Authorize]  // ✅ Authenticated write access
public async Task<ActionResult> Delete(Guid id) { }
```

**Why**: Public discovery, protected writes, standard security pattern.

---

### 9. Extract UserId with Fallback Pattern ⚠️

```csharp
var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;

if (string.IsNullOrEmpty(userId))
{
    return Unauthorized(new { error = "User ID not found in token" });
}
```

**Claim Priority**:
1. `sub` - Standard OIDC subject claim (preferred)
2. `nameidentifier` - Legacy JWT claim (fallback)
3. `sid` - Session ID (last resort)

**Why**: Different auth providers use different claim names.

---

### 10. File-Scoped Namespaces ⚠️

```csharp
// ❌ WRONG - Nested namespace
namespace Explore.Application.Features.Events.Handlers.Commands
{
    public class CreateEventCommandHandler { }
}

// ✅ CORRECT - File-scoped namespace
namespace Explore.Application.Features.Events.Handlers.Commands;

public class CreateEventCommandHandler { }
```

**Why**: C# 10+ convention, reduces nesting, cleaner code.

---

## 🏗️ CQRS Pattern Quick Reference

### Command Pattern (Write Operations)

```csharp
// 1. Command Request
public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>
{
    public CreateEventDto EventDto { get; set; }
}

// 2. Command Handler
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    // ... other repositories for validation

    public async Task<BaseCommandResponse<Guid>> Handle(...)
    {
        var response = new BaseCommandResponse<Guid>();

        // Instantiate validator manually
        var validator = new CreateEventDtoValidator(...repos...);
        var validationResult = await validator.ValidateAsync(request.EventDto);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var entity = _mapper.Map<Event>(request.EventDto);
        entity = await _eventRepository.Create(entity);

        response.Success = true;
        response.Id = entity.Id;
        return response;
    }
}
```

### Query Pattern (Read Operations)

```csharp
// 1. Query Request
public class GetEventListRequest : IRequest<List<EventListDto>> { }

// 2. Query Handler
public class GetEventListRequestHandler : IRequestHandler<GetEventListRequest, List<EventListDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public async Task<List<EventListDto>> Handle(...)
    {
        var entities = await _eventRepository.GetEventsWithDetails();  // Entities
        return _mapper.Map<List<EventListDto>>(entities);  // DTOs
    }
}
```

---

## 📁 Folder Structure Reference

```
Explore.Application/Features/{Entity}s/
├── Requests/
│   ├── Commands/
│   │   ├── Create{Entity}Command.cs
│   │   ├── Update{Entity}Command.cs
│   │   └── Delete{Entity}Command.cs
│   └── Queries/
│       ├── Get{Entity}ListRequest.cs
│       ├── Get{Entity}DetailsRequest.cs
│       └── Get{Entities}By{RelatedEntity}Request.cs (custom)
└── Handlers/
    ├── Commands/
    │   ├── Create{Entity}CommandHandler.cs
    │   ├── Update{Entity}CommandHandler.cs
    │   └── Delete{Entity}CommandHandler.cs
    └── Queries/
        ├── Get{Entity}ListRequestHandler.cs
        ├── Get{Entity}DetailsRequestHandler.cs
        └── Get{Entities}By{RelatedEntity}RequestHandler.cs (custom)

Explore.Application/DTOs/{Entity}/
├── {Entity}Dto.cs              # Full details
├── {Entity}ListDto.cs          # List view
├── Create{Entity}Dto.cs        # Create payload
├── Update{Entity}Dto.cs        # Update payload
└── Validators/
    ├── Create{Entity}DtoValidator.cs
    └── Update{Entity}DtoValidator.cs
```

---

## 🎯 Controller Pattern Reference

```csharp
[Route("api/v1/[controller]")]
[ApiController]
public class EventController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<EventListDto>>> GetAll()
        => Ok(await _mediator.Send(new GetEventListRequest()));

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<EventDto>> GetById(Guid id)
        => Ok(await _mediator.Send(new GetEventDetailsRequest { Id = id }));

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto dto)
    {
        var command = new CreateEventCommand { EventDto = dto };
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventDto dto)
    {
        if (id != dto.Id) return BadRequest(new { error = "ID mismatch" });
        var command = new UpdateEventCommand { EventDto = dto };
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> Delete(Guid id)
    {
        var command = new DeleteEventCommand { Id = id };
        var result = await _mediator.Send(command);
        return result ? NoContent() : NotFound();
    }
}
```

---

## 🧾 API Endpoint Metadata (Required)

All controller actions must be OpenAPI-friendly.

**Required per action**:

- `[EndpointSummary("...")]`
- `[EndpointDescription("...")]`
- `[ProducesResponseType(...)]` for success + common failures
- `[Consumes("application/json")]` for body endpoints (`POST/PUT`)

**Example**:

```csharp
using static Microsoft.AspNetCore.Http.StatusCodes;

[HttpGet("{id}")]
[EndpointSummary("Get Event Details")]
[EndpointDescription("Returns full event details.")]
[AllowAnonymous]
[ProducesResponseType(typeof(EventDto), Status200OK)]
[ProducesResponseType(Status404NotFound)]
public async Task<ActionResult<EventDto>> GetById(Guid id)
    => Ok(await _mediator.Send(new GetEventDetailsRequest { Id = id }));
```

---

## 🔍 Common Mistakes & Fixes

| Mistake | Fix |
|---------|-----|
| Repository returns DTOs | Change to return entities |
| Validator injected via DI | Instantiate manually in handler |
| `org.Members.Add(member)` | Use `_memberRepository.Create(member)` |
| `public long Id` | Use `public int Id` or `public Guid Id` |
| `= 0` in entity | Remove, set in handler |
| Removed using statements | Keep all using statements |
| `IRequest<Guid>` | Use `IRequest<BaseCommandResponse<Guid>>` |
| Nested namespaces | Use file-scoped namespaces |
| Missing fallback for userId | Use sub → nameidentifier → sid |
| Missing [AllowAnonymous] on GET | Add for public read access |

---

## 📚 Related Documentation

- **GOVERNANCE.md** - Complete coding conventions and patterns
- **ARCHITECTURE.md** - Clean Architecture layers and dependencies
- **API.md** - REST API endpoints and contracts
- **DOMAIN.md** - Domain entities and business rules

---

## 🎓 Learning Resources

**Internal Skills**:
- `clean-architecture-rules` - Dependency rules and layer boundaries
- `cqrs-mediatr-guidelines` - CQRS patterns with MediatR
- `dotnet-efcore-guidelines` - EF Core and repository patterns

**Example Implementations**:
- Event entity - Complete CQRS implementation
- Madhab entity - Readonly lookup table pattern
- OrganizationMember - Link table pattern

---

**Remember**: These rules come from real implementations. Follow them to maintain consistency across the codebase.
