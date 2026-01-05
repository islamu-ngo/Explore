# DBML Sync Task - Development Documentation

**Task Status:** 🛑 Awaiting User Approval for DBML Corrections
**Last Updated:** 2026-01-04

---

## Quick Start (After Context Reset)

### 1. Read This First
📖 **SESSION-HANDOFF-2026-01-04.md** - Complete session summary with all context

### 2. Review Critical Document
⚠️ **dbml-corrections-required.md** - 6 critical DBML errors that MUST be fixed

### 3. Read Comprehensive Plan
📋 **C:\Users\AM5\.claude\plans\purrfect-weaving-sun.md** - 60+ page implementation guide

### 4. Check Current State
✅ **dbml-sync-context.md** - Decisions, patterns, progress
📝 **dbml-sync-tasks.md** - Task checklist with status

---

## File Guide

| File | Purpose | Priority |
|------|---------|----------|
| `SESSION-HANDOFF-2026-01-04.md` | Session summary, key insights, exact state | 🔥 READ FIRST |
| `dbml-corrections-required.md` | DBML schema errors + approval checklist | ⚠️ CRITICAL |
| `dbml-sync-context.md` | Current state, decisions, patterns, blockers | ⭐ HIGH |
| `dbml-sync-tasks.md` | Task checklist with completion status | ⭐ HIGH |
| `dbml-sync-plan.md` | Original plan + updates | 📖 REFERENCE |
| `README.md` | This file - orientation guide | 📚 META |

---

## Current State

### ✅ Completed (2026-01-04)
- Comprehensive codebase analysis (Organization entity across all layers)
- DBML schema error identification (6 critical issues)
- Design decision resolution (8/8 complete)
- Pattern documentation (Domain, Application, Persistence, API)
- Comprehensive refactored plan creation

### 🛑 Blocked
**Cannot proceed until:**
1. User reviews `dbml-corrections-required.md`
2. User approves/modifies DBML corrections
3. User decides on OrganizationReview entity (add to DBML or remove)
4. DBML file updated with corrections

### ⏳ Ready to Start (After Unblocking)
- Phase 0: Entity mapping + CQRS use case definition
- Phase 1: Domain layer implementation (43 entities)

---

## Critical Decisions (All Resolved)

| Decision | Resolution |
|----------|------------|
| atproto_record types | ✅ varchar(255/500) for did/record_key/cid |
| Location geo | ✅ PostGIS geometry + lat/long |
| Tenant enforcement | ✅ Multi-layered (filters + repo + middleware) |
| Join tables | ✅ Explicit entities with tenant_id |
| API routing | ✅ /api/v1/[controller] |
| Delete behaviors | ✅ Cascade children, Restrict cross-aggregate |
| User ID extraction | ✅ ClaimsPrincipalExtensions helper |
| Repository returns | ✅ DTOs for queries, entities for commands |

---

## Implementation Pattern Reference

### Domain Layer
```
Explore.Domain/{Entity}.cs
Explore.Domain/Enums/{Entity}Enum.cs
```
- Primary Keys: `Guid Id` (entities), `int Id` (lookups)
- Enums: `{Entity}Enum` with explicit values
- Note: `[ForeignKey]` annotations exist in Domain (actual pattern)

### Application Layer
```
Explore.Application/DTOs/{Entity}/
  ├── {Entity}Dto.cs (detail view)
  ├── {Entity}ListDto.cs (list view)
  ├── Create{Entity}Dto.cs
  ├── Update{Entity}Dto.cs
  └── Validators/Create{Entity}DtoValidator.cs

Explore.Application/Features/{EntityPlural}/
  ├── Requests/
  │   ├── Commands/{Action}{Entity}Command.cs
  │   └── Queries/Get{Entity}{Purpose}Request.cs
  └── Handlers/
      ├── Commands/{Action}{Entity}CommandHandler.cs
      └── Queries/Get{Entity}{Purpose}RequestHandler.cs
```
- Commands return: `BaseCommandResponse<Guid>`
- Queries return: DTOs directly
- Validators: FluentValidation with DI

### Persistence Layer
```
Explore.Persistence/Configurations/Entities/{Entity}Configuration.cs
Explore.Persistence/Repositories/{Entity}Repository.cs
```
- UUID generation: `HasDefaultValueSql("uuidv7()")`
- Query repos return: DTOs (project in `.Select()`)
- Command repos use: Entities

### API Layer
```
Explore.API/Controllers/{Entity}Controller.cs

[Route("api/v1/[controller]")]  // ⚠️ Standardize this
[ApiController]
```
- User ID: Use `User.GetUserId()` helper (to be created)
- Endpoints: Call MediatR, return ActionResult<T>

---

## DBML Corrections Summary

**6 Critical Issues:**
1. atproto_record.did/record_key/cid → uuid to varchar
2. event_session_agenda_items timestamps → timestamp to timestamptz
3. actor_key_store → missing tenant_id
4. user_authentication_token → missing tenant_id
5. user_external_login → missing tenant_id
6. organization_review → missing table (exists in code)

See `dbml-corrections-required.md` for full details.

---

## Timeline Estimate

| Phase | Effort |
|-------|--------|
| Phase 0: Discovery | 0.5-1 day |
| Phase 1: Domain | 2-3 days |
| Phase 2: Application | 3-4 days |
| Phase 3: Persistence | 3-4 days |
| Phase 4: API | 2-3 days |
| Phase 5: Verification | 1-2 days |
| **Total** | **12-17 days** |

---

## Key Files to Analyze (Reference Implementations)

**Organization Entity Examples:**
- Domain: `Explore.Domain/Organization.cs`
- DTOs: `Explore.Application/DTOs/Organization/*.cs`
- CQRS: `Explore.Application/Features/Organizations/**/*.cs`
- Repository: `Explore.Persistence/Repositories/OrganizationRepository.cs`
- Config: `Explore.Persistence/Configurations/Entities/OrganizationConfiguration.cs`
- Controller: `Explore.API/Controllers/OrganizationController.cs`

**DBML Source:**
- Schema: `schema/islamu-event.md` (needs corrections before use)

---

## Commands for Next Session

### Verify DBML Corrections (After Applied)
```bash
# Check atproto_record types
grep -A 10 "atproto_record" schema/islamu-event.md

# Check agenda items timestamps
grep -A 10 "event_session_agenda_items" schema/islamu-event.md

# Check tenant_id additions
grep "actor_key_store" schema/islamu-event.md -A 15
```

### When Ready to Implement
```bash
# Phase 1: Build Domain
dotnet build Explore.Domain

# Phase 2: Build Application
dotnet build Explore.Application

# Phase 3: Generate Migration
dotnet ef migrations add DbmlSync --project Explore.Persistence --startup-project Explore.API

# Verify migration
dotnet ef migrations script --project Explore.Persistence --startup-project Explore.API
```

---

## Contact / Questions

**Primary Documentation:**
- Comprehensive Plan: `C:\Users\AM5\.claude\plans\purrfect-weaving-sun.md`
- Session Handoff: `SESSION-HANDOFF-2026-01-04.md`

**For Implementation:**
- Follow patterns documented in context.md
- Reference Organization entity as implementation example
- Use comprehensive plan for step-by-step guidance

---

**Last Updated:** 2026-01-04 (End of Session)
**Next Action:** User approval of DBML corrections
