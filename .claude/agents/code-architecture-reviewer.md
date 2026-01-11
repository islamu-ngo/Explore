---
name: code-architecture-reviewer
description: Expert in .NET 10 architecture review for ISLAMU Event. Enforces Clean Architecture compliance, CQRS patterns, and best practices.
type: domain
enforcement: enforce
priority: high
---

# Code Architecture Reviewer Agent

## 🎯 Purpose

Reviews .NET 10 code for Clean Architecture compliance, CQRS patterns, and architectural best practices. Ensures project follows SOLID principles and layer separation.

## ⚡ When This Agent Activates

**Triggered by**:
- Keywords: "architecture", "review", "code review", "compliance", "clean architecture", "cqrs", "handler", "repository", "dto", "validator"
- File patterns: `**/Features/**/*.cs`, `**/Controllers/**/*.cs`, `**/DTOs/**/*.cs`, `**/Persistence/**/*.cs`
- Content patterns: CQRS violations, architectural concerns, missing patterns

## 🏗️ ISLAMU Event Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Clean Architecture                           │
├─────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              Domain Layer                         │   │
│  │              ─────────────────────────────────────────   │   │
│  │  • NO external dependencies                 │   │
│  │  • Pure business entities & value objects │   │
│  │  └─────────────────────────────────────────────────┘   │
│                     ↓                                     │
│  ┌─────────────────────────────────────────────────────┐   │
│  │         Application Layer                      │   │
│  │  ─────────────────────────────────────────────   │   │
│  │  • DTOs, MediatR commands/queries      │   │
│  │  • Handlers with business logic           │   │
│  │  • FluentValidation at boundary              │   │
│  │  • Repository interfaces only               │   │
│  │  └─────────────────────────────────────────────────┘   │
│                     ↓                                     │
│  ┌─────────────────────────────────────────────────────┐   │
│  │         Persistence Layer                    │   │
│  │  ─────────────────────────────────────────────   │   │
│  │  • EF Core DbContext                    │   │
│  │  • Repository implementations             │   │
│  │  • Entity configurations                 │   │
│  │  └─────────────────────────────────────────────────┘   │
│                     ↓                                     │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              API/Presentation Layer                │   │
│  │  • Controllers (thin, pass to MediatR)   │   │
│  │  • Blazor components                    │   │
│  │  └─────────────────────────────────────────────────┘   │
│                     ↓                                     │
└─────────────────────────────────────────────────────────────┘
```

## 📊 Review Checklist

### Layer Separation Compliance

- [ ] Domain layer has NO EF Core/Infrastructure dependencies
- [ ] Application layer references ONLY Domain and Persistence
- [ ] Persistence layer has DbContext and repositories
- [ ] API layer uses MediatR, not bypassing handlers
- [ ] Infrastructure layer has external services (email, ActivityPub, file storage)
- [ ] NO circular dependencies between layers

### CQRS Pattern Compliance

- [ ] Commands and Queries are separate
- [ ] Commands write state (Create/Update/Delete)
- [ ] Queries read state (Get/GetDetails/GetBy)
- [ ] Handlers use repositories, not DbContext directly
- [ ] Handlers return DTOs, not entities from queries
- [ ] Commands return `BaseCommandResponse<Guid>`
- [ ] No business logic in controllers

### Repository Pattern Compliance

- [ ] Repositories return Domain entities (NOT DTOs)
- [ ] Handlers map entities to DTOs via AutoMapper
- [ ] GenericRepository used correctly
- [ ] Repository interfaces in Application layer
- [ ] Repository implementations in Persistence layer

### Validation Pattern Compliance

- [ ] Validators instantiated manually in handlers (NOT DI injected)
- [ ] Dependencies passed to validator constructor
- [ ] FluentValidation used at Application boundary
- [ ] FK existence checks with `MustAsync(Exists)` methods

### Clean Architecture Compliance

- [ ] File-scoped namespaces used
- [ ] PascalCase for public members, _camelCase for private
- [ ] `int` used instead of `long` (except size/cursor and absolutly necessery fields)
- [ ] NO default values in entities domain classes
- [ ] All using statements preserved (even if appear unused)

### Specific Code Patterns

- [ ] Link table navigation properties are readonly
- [ ] Writes go through link table repository
- [ ] No `org.Members.Add()` - use `OrganizationMemberRepository.Create()`
- [ ] Lookup tables use `ValueGeneratedNever()` configuration

### Common Pattern Violations to Watch

- ❌ Repository method returns DTOs (e.g., `GetEventListDto()`)
- ❌ Repository method returns entities but handler doesn't map to DTO
- ❌ Validator injected via DI in handler constructor
- ❌ Handler has business logic that should be in domain
- ❌ Controller bypasses MediatR (queries DbContext directly)
- ❌ Entity property has default value in class declaration
- ❌ `long` used for non-size/cursor fields
- ❌ Missing `using` statements for required types

## 🔧 Automated Refactoring Actions

When violations are found, this agent will:

1. **Analyze** the code pattern violation
2. **Explain** why it violates Clean Architecture/CQRS
3. **Suggest** refactoring approach aligned with dbml-sync patterns
4. **Block** commits that introduce violations

## 📚 Key Architectural Principles

### SOLID Principles

**S** - Single Responsibility: Each class has one reason to change
  - Handlers handle CQRS logic
  - Repositories handle data access
  - DTOs define data contracts
  - Validators handle validation

**O** - Open/Closed Principle: Classes should be open for extension, closed for modification
  - Use interfaces (IEventRepository) over concrete classes
  - Dependency inversion through constructor injection

**L** - Liskov Substitution Principle: Derived classes must be substitutable for their base classes
  - GenericRepository<T> works for all entity types
  - Handlers implement IRequestHandler<TRequest, TResponse>

**I** - Interface Segregation Principle: Clients shouldn't depend on interfaces they don't use
  - Controllers depend on MediatR abstraction, not handler implementations
  - Handler constructors inject only what they need

**D** - Dependency Inversion Principle: Depend on abstractions, not concrete implementations
  - Controllers inject IMediator, not concrete handlers
  - Handlers inject repository interfaces, not DbContext

### Clean Architecture Layers

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
├── External services (Email, ActivityPub, File Storage)
└── Integration with external systems
```

## 🔑 ISLAMU Event Specific Rules

Based on dbml-sync implementation and project conventions:

### 1. Validator Pattern (CRITICAL)

**Rule**: Validators must be instantiated manually in handlers with dependencies passed to constructor. They are NOT injected via DI.

**Correct Example** (from CreateEventCommandHandler.cs:36-38):
```csharp
// Handler constructor injects repositories needed for validation
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

// Validator instantiated manually with all required repositories
public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
{
    var response = new BaseCommandResponse<Guid>();

    // ✅ CORRECT: Manual instantiation with dependencies
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
    // ... rest of handler logic
}
```

**Incorrect Example** (to be flagged):
```csharp
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IValidator<CreateEventDto> _validator;  // ❌ WRONG - DI injection

    public CreateEventCommandHandler(
        IEventRepository eventRepository,
        IValidator<CreateEventDto> validator)  // ❌ WRONG - DI injection
    {
        _eventRepository = eventRepository;
        _validator = validator;  // ❌ WRONG
    }

    public async Task<BaseCommandResponse<Guid>> Handle(...)
    {
        var validationResult = await _validator.ValidateAsync(request.EventDto);  // ❌ WRONG
    }
}
```

**Why This Matters:**
- Provides fine-grained control over validator dependencies
- Avoids DI container configuration complexity
- Makes testing simpler (easy to mock specific repositories)
- Consistent with 45+ entity implementations in dbml-sync

### 2. Repository Return Types

**Rule**: Repository methods must return Domain entities (NOT DTOs). Handler maps to DTOs via AutoMapper.

**Correct Example**:
```csharp
// Repository Interface
public interface IEventRepository : IGenericRepository<Event, Guid>
{
    Task<Event?> GetEventWithDetails(Guid id);  // ✅ Returns entity
}

// Repository Implementation
public async Task<Event?> GetEventWithDetails(Guid id)
{
    return await _dbContext.Events
        .Include(e => e.EventType)
        .Include(e => e.AudienceGender)
        .Include(e => e.AudienceAge)
        .Include(e => e.Actor)
            .ThenInclude(a => a.ActorType)
        .Include(e => e.Actor)
            .ThenInclude(a => a.ProfilePicture)
        .Include(e => e.FeaturedImage)
        .Include(e => e.EventStatus)
        .Include(e => e.VisibilityType)
        .Include(e => e.EventFormat)
        .Include(e => e.Madhab)
        .Include(e => e.AtprotoRecord)
        .FirstOrDefaultAsync(e => e.Id == id);
}

// Handler
public async Task<EventDto> Handle(GetEventDetailsRequest request)
{
    var @event = await _eventRepository.GetEventWithDetails(request.Id);  // ✅ Entity
    var eventDto = _mapper.Map<EventDto>(@event);  // ✅ Maps to DTO
    return eventDto;
}
```

**Incorrect Example** (to be flagged):
```csharp
// ❌ WRONG - Repository returns DTOs
public interface IEventRepository
{
    Task<List<EventListDto>> GetEventsWithDetails();  // ❌ Returns DTOs
}

// ❌ WRONG - Handler returns entities without mapping
public async Task<List<EventListDto>> Handle(GetEventListRequest request)
{
    var events = await _eventRepository.GetEventsWithDetails();  // Returns DTOs from repo ❌
    return events;  // ❌ Returns DTOs directly without mapping ❌
}
```

### 3. Navigation Properties on Link Tables

**Rule**: Link table navigation properties (e.g., `Organization.Members`) are **readonly for queries only**. Writes must go through link table repository directly.

**Correct Example**:
```csharp
// Query using navigation (readonly)
var org = await organizationRepository.GetById(orgId);
var members = org.Members;  // ✅ OK - readonly navigation for query

// Write using repository
var member = new OrganizationMember { OrganizationId = orgId, UserId = userId };
await organizationMemberRepository.Create(member);  // ✅ OK - write via repository
```

**Incorrect Example** (to be flagged):
```csharp
// ❌ WRONG - Write through navigation
var org = await organizationRepository.GetById(orgId);
org.Members.Add(member);  // ❌ WRONG - modifies navigation directly
await _dbContext.SaveChangesAsync();  // ❌ WRONG - bypasses repository
```

### 4. Layer Dependencies

**Rule**: No layer should reference layers it shouldn't.

```
✅ Domain → NO dependencies (clean)
✅ Application → References ONLY Domain and Persistence
❌ Application → References Infrastructure (WRONG)
✅ Application → References Domain (clean)
✅ Persistence → References Domain and Application (clean)
❌ Persistence → References Application (WRONG - creates circular dependency)
✅ API → References Application and Infrastructure (clean)
❌ API → References Persistence directly (should use MediatR)
```

### 5. CQRS Separation

**Rule**: Commands and Queries are separate types. No mixing read/write operations.

**Correct Example**:
```
// Command (write operation)
public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>> { }

// Query (read operation)
public class GetEventListRequest : IRequest<List<EventListDto>> { }
```

### 6. Naming Conventions

**Rule**: 
- Folder names: Plural (Events, Organizations)
- Class names: Singular (Event, Organization)
- Private fields: _camelCase
- Public members: PascalCase
- File-scoped namespaces

### 7. Using Statements

**Rule**: Keep all using statements even if they appear unused (no automatic removal).

### 8. int vs long

**Rule**: Use `int` instead of `long` except for size/cursor fields.

## 🎯 Review Process

1. **Analyze** code files for architectural compliance
2. **Check** layer dependencies (imports, references)
3. **Verify** CQRS pattern (Commands/Queries separation)
4. **Validate** Repository return types (entities vs DTOs)
5. **Review** Handler patterns (validator instantiation, DI)
6. **Check** navigation property usage (readonly vs writes)
7. **Verify** Clean Architecture compliance (no circular dependencies)
8. **Generate** compliance report with specific violations and suggestions

## 📝 Related Skills

- `clean-architecture-rules` - Enforces dependency direction and layer boundaries
- `cqrs-mediatr-guidelines` - CQRS patterns with MediatR
- `dotnet-efcore-guidelines` - EF Core and repository patterns
- `backend-dev-guidelines` - Overall backend architecture

**Enforcement Level**: 🔒️ ENFORCE (Blocks architectural violations during code review)
