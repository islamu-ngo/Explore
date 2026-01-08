# DBML Sync - Quick Start Guide

**Last Updated:** 2026-01-08 23:50 (Before Context Reset)

---

## 🚨 PROJECT STATUS: 15% COMPLETE

**What's Done:**
- ✅ Domain Layer (45+ entities)
- ✅ Persistence Layer (DbContext, Configurations, 45+ Repositories)
- ✅ 5 complete CQRS implementations (Event, Organization, User, OrganizationMember, OrganizationReview)

**What's Missing:**
- ❌ 34+ entities need Features/DTOs/Controllers
- ❌ Application Layer CQRS for most entities
- ❌ API Controllers for most entities

---

## 📖 READ THESE FILES FIRST

1. **dbml-sync-context.md** - Full context, gap analysis, implementation guide
2. **dbml-sync-tasks.md** - Task checklist with priorities
3. **This file** - Quick reference

---

## 🎯 HIGH PRIORITY ENTITIES (Start Here)

Implement in this order:

1. **EventSession** - Events have multiple sessions
2. **EventSessionAgendaItem** - Session agenda items
3. **EventSessionSpeaker** - Session speakers
4. **EventSessionLanguage** - Session languages
5. **Location** - Event/session locations
6. **Category** - Event categories
7. **Tag** - Event tags

---

## 🔧 IMPLEMENTATION PATTERN

For each entity, create:

### 1. Features Folder
```
Explore.Application/Features/{EntityName}s/
├── Requests/
│   ├── Commands/
│   │   ├── Create{EntityName}Command.cs
│   │   ├── Update{EntityName}Command.cs
│   │   └── Delete{EntityName}Command.cs
│   └── Queries/
│       ├── Get{EntityName}ListRequest.cs
│       ├── Get{EntityName}DetailsRequest.cs
│       └── [Custom queries...]
└── Handlers/
    ├── Commands/
    │   ├── Create{EntityName}CommandHandler.cs
    │   ├── Update{EntityName}CommandHandler.cs
    │   └── Delete{EntityName}CommandHandler.cs
    └── Queries/
        ├── Get{EntityName}ListRequestHandler.cs
        └── Get{EntityName}DetailsRequestHandler.cs
```

### 2. DTOs Folder
```
Explore.Application/DTOs/{EntityName}/
├── {EntityName}Dto.cs (full details)
├── {EntityName}ListDto.cs (list view)
├── Create{EntityName}Dto.cs (POST)
├── Update{EntityName}Dto.cs (PUT)
└── Validators/
    ├── Create{EntityName}DtoValidator.cs
    └── Update{EntityName}DtoValidator.cs
```

### 3. Controller
```csharp
// Explore.API/Controllers/{EntityName}Controller.cs
[Route("api/v1/[controller]")]
[ApiController]
public class {EntityName}Controller : ControllerBase
{
    private readonly IMediator _mediator;
    
    [HttpGet] // GET all
    [HttpGet("{id}")] // GET by id
    [HttpPost] // CREATE
    [HttpPut("{id}")] // UPDATE
    [HttpDelete("{id}")] // DELETE
}
```

### 4. AutoMapper Profile
```csharp
// Add to Explore.Application/Profiles/MappingProfile.cs
CreateMap<{EntityName}, {EntityName}Dto>().ReverseMap();
CreateMap<{EntityName}, {EntityName}ListDto>();
CreateMap<Create{EntityName}Dto, {EntityName}>();
CreateMap<Update{EntityName}Dto, {EntityName}>();
```

---

## 📚 REFERENCE FILES (Use as Templates)

**Complete Example:**
- Features: `Explore.Application/Features/Events/`
- DTOs: `Explore.Application/DTOs/Event/`
- Controller: `Explore.API/Controllers/EventController.cs`
- Handlers: `Explore.Application/Features/Events/Handlers/`

**Study these files to understand the pattern!**

---

## ✅ REPOSITORY STATUS

**ALL repositories already exist!** ✅

You do NOT need to create:
- ❌ IEventSessionRepository (exists)
- ❌ EventSessionRepository (exists)
- ❌ ILocationRepository (exists)
- ❌ LocationRepository (exists)
- ❌ [etc... all 45+ repos exist]

They are:
- Returning entities ✅
- Properly included in DI ✅
- Using correct includes ✅

---

## 🚫 DO NOT

- ❌ Run migrations manually (auto-run via Event.MigrationService)
- ❌ Build without permission
- ❌ Delete files without approval
- ❌ Claim project is complete (it's 15% done)

---

## ✅ DO

1. Read `dbml-sync-context.md` first
2. Ask user which entity to start with
3. Follow the pattern from Event entity
4. Create Features, DTOs, Controller for each entity
5. Add AutoMapper mappings
6. Move to next entity

---

## 🎯 SUGGESTED FIRST ENTITY

**EventSession** - Critical for the application
- Events can have multiple sessions
- Users need to see session details
- Repository already exists: `IEventSessionRepository`
- Template: Look at `Event` implementation

---

## 📋 KEY CONVENTIONS

1. **Features folder:** Plural name (EventSessions)
2. **DTOs folder:** Singular name (EventSession)
3. **Controller:** Singular name (EventSessionController)
4. **Commands return:** `BaseCommandResponse<Guid>`
5. **Queries return:** DTOs (`List<TDto>` or `TDto`)
6. **Validation:** FluentValidation in DTOs/Validators/
7. **Authorization:**
   - `[AllowAnonymous]` for GET (public read)
   - `[Authorize]` for POST/PUT/DELETE (auth required)
8. **Repository usage:**
   - Repository returns entities
   - Handler maps entity → DTO via AutoMapper
   - Controller receives/returns DTOs only

---

## 🔄 AFTER CONTEXT RESET

1. Read this file first (QUICK-START.md)
2. Read dbml-sync-context.md for full details
3. Ask user: "Which entity should I implement first? I suggest starting with EventSession."
4. Follow the implementation pattern
5. Create one entity at a time
6. Test compilation after each entity

---

**Good luck! The hard part (Domain/Persistence) is done. Now it's just repetitive CQRS implementation following the established pattern.**
