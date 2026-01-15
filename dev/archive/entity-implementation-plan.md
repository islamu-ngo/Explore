# Entity Implementation Plan

**Created**: 2026-01-10  
**Status**: Ready for Implementation  
**Total Entities**: 33 remaining (out of 45 total)

---

## 📊 Current Status

### ✅ Completed (12 entities - 27%)

1. Event ✅
2. Organization ✅
3. User ✅
4. OrganizationMember ✅
5. OrganizationReview ✅
6. EventSession ✅
7. Location ✅
8. Category ✅
9. Tag ✅
10. EventSessionAgendaItem ✅
11. EventSessionSpeaker ✅
12. Language ✅ (readonly lookup)

### ⚠️ Remaining (33 entities - 73%)

**HIGH PRIORITY - Core Features (7 entities)**
1. Actor (federation identity) - **START HERE**
2. Tenant (multi-tenancy)
3. TenantUser (user-tenant mapping)
4. TenantSettings (tenant config)
5. IndexedDid (ATProto federation)
6. SyncState (federation sync)
7. AtprotoRecord (ATProto linking)

**MEDIUM PRIORITY - User Features (6 entities)**
8. UserAuthenticationToken
9. UserExternalLogin
10. ActorKeyStore
11. EventRegistration
12. EventSessionLanguage
13. StorageObject

**LOW PRIORITY - Lookup Tables (20 entities)**
14. Madhab ✅ (has handler, needs controller)
15. AudienceAge
16. AudienceGender
17. EventType
18. EventStatus
19. EventFormat
20. VisibilityType
21. RegistrationMode
22. OrganizationRole
23. OrganizationPosition
24. UserRole
25. ActorType
26. DidCustodyType
27. FileType
28. OwnerType
29. TagType
30. ApprovalStatus

**LOW PRIORITY - Link Tables (3 entities)**
31. EventCategories
32. EventTags
33. TagTypeTags

---

## 🎯 Implementation Strategy

### Phase 1: Core Federation (Actor, Tenant, ATProto) - 3-4 hours
**Priority**: CRITICAL - Blocking other features

**Entities**: Actor, Tenant, TenantUser, TenantSettings, IndexedDid, SyncState, AtprotoRecord

**Why First**:
- Actor is referenced by Event, Organization, User
- Tenant isolation required for all entities
- ATProto federation is core platform feature

**Estimated Time**: ~30-40 min per entity × 7 = 3.5-4.5 hours

---

### Phase 2: User Management (Auth & Tokens) - 2-3 hours
**Priority**: HIGH - Required for user features

**Entities**: UserAuthenticationToken, UserExternalLogin, ActorKeyStore

**Why Second**:
- Enables hybrid auth (Keycloak + ATProto OAuth)
- Required for DID management
- Blocking user registration flows

**Estimated Time**: ~30-40 min per entity × 3 = 1.5-2 hours

---

### Phase 3: Event Features (Registration, Storage) - 2-3 hours
**Priority**: HIGH - User-facing features

**Entities**: EventRegistration, EventSessionLanguage, StorageObject

**Why Third**:
- EventRegistration is core event feature
- StorageObject needed for images
- User-visible functionality

**Estimated Time**: ~30-40 min per entity × 3 = 1.5-2 hours

---

### Phase 4: Lookup Tables (Readonly Endpoints) - 3-4 hours
**Priority**: MEDIUM - Simple but numerous

**Entities**: All 20 lookup tables

**Why Fourth**:
- Simpler pattern (no commands, only queries)
- Can be done in batch
- Low risk

**Pattern**: GetList + GetDetails only (no Create/Update/Delete)

**Estimated Time**: ~10-15 min per entity × 20 = 3-5 hours

---

### Phase 5: Link Tables (Explicit Entities) - 1-2 hours
**Priority**: LOW - Nice to have

**Entities**: EventCategories, EventTags, TagTypeTags

**Why Last**:
- Already have readonly navigation properties
- Can be accessed through parent entities
- Optional API endpoints

**Estimated Time**: ~20-30 min per entity × 3 = 1-1.5 hours

---

## 📝 Implementation Checklist (Per Entity)

Use this checklist for each entity:

### Step 1: DTOs (4 files - 10 min)
- [ ] `{Entity}Dto.cs` - Full details
- [ ] `{Entity}ListDto.cs` - List view
- [ ] `Create{Entity}Dto.cs` - Create payload (if not readonly)
- [ ] `Update{Entity}Dto.cs` - Update payload (if not readonly)

### Step 2: Validators (2 files - 10 min)
- [ ] `Create{Entity}DtoValidator.cs` - FK checks with repositories
- [ ] `Update{Entity}DtoValidator.cs` - FK checks + entity exists

### Step 3: Commands (3 files - 5 min)
- [ ] `Create{Entity}Command.cs`
- [ ] `Update{Entity}Command.cs`
- [ ] `Delete{Entity}Command.cs`

### Step 4: Command Handlers (3 files - 15 min)
- [ ] `Create{Entity}CommandHandler.cs` - Instantiate validator manually
- [ ] `Update{Entity}CommandHandler.cs` - Instantiate validator manually
- [ ] `Delete{Entity}CommandHandler.cs` - Simple delete

### Step 5: Queries (2-4 files - 5 min)
- [ ] `Get{Entity}ListRequest.cs`
- [ ] `Get{Entity}DetailsRequest.cs`
- [ ] Custom queries (e.g., `Get{Entities}By{RelatedEntity}Request.cs`)

### Step 6: Query Handlers (2-4 files - 10 min)
- [ ] `Get{Entity}ListRequestHandler.cs` - Returns List<{Entity}ListDto>
- [ ] `Get{Entity}DetailsRequestHandler.cs` - Returns {Entity}Dto
- [ ] Custom query handlers

### Step 7: Controller (1 file - 10 min)
- [ ] `{Entity}Controller.cs` - Standard CRUD endpoints
- [ ] GET endpoints: [AllowAnonymous]
- [ ] POST/PUT/DELETE: [Authorize]

### Step 8: AutoMapper (1 update - 2 min)
- [ ] Add 4 mappings to `MappingProfile.cs`

### Step 9: Verify (5 min)
- [ ] `dotnet build` succeeds
- [ ] No compilation errors
- [ ] Patterns match existing entities (Event, EventSession, Location)

**Total Time per Entity**: ~60-75 minutes (complex) or ~20-30 minutes (lookup)

---

## 🚀 Quick Start: Actor Entity Example

Follow this pattern for all entities:

### Actor Implementation Outline

```
Explore.Application/DTOs/Actor/
├── ActorDto.cs
├── ActorListDto.cs
├── CreateActorDto.cs
├── UpdateActorDto.cs
└── Validators/
    ├── CreateActorDtoValidator.cs (inject IActorTypeRepository, IDidCustodyTypeRepository)
    └── UpdateActorDtoValidator.cs

Explore.Application/Features/Actors/
├── Requests/
│   ├── Commands/
│   │   ├── CreateActorCommand.cs
│   │   ├── UpdateActorCommand.cs
│   │   └── DeleteActorCommand.cs
│   └── Queries/
│       ├── GetActorListRequest.cs
│       ├── GetActorDetailsRequest.cs
│       ├── GetActorByDidRequest.cs (custom)
│       └── GetActorByHandleRequest.cs (custom)
└── Handlers/
    ├── Commands/
    │   ├── CreateActorCommandHandler.cs (manual validator instantiation)
    │   ├── UpdateActorCommandHandler.cs
    │   └── DeleteActorCommandHandler.cs
    └── Queries/
        ├── GetActorListRequestHandler.cs
        ├── GetActorDetailsRequestHandler.cs
        ├── GetActorByDidRequestHandler.cs
        └── GetActorByHandleRequestHandler.cs

Explore.API/Controllers/
└── ActorController.cs (6-8 endpoints)
```

---

## 📋 Batch Implementation Tip

**Lookup Tables (Fast Track)**:

Since lookup tables follow identical pattern, you can batch them:

1. Copy Language implementation (already done)
2. Find/Replace: Language → {NewEntity}
3. Update properties to match DBML
4. Verify build

**Estimated Time**: 10-15 min per lookup table

---

## ⚠️ Critical Reminders

When implementing, ALWAYS follow these rules:

1. ✅ Repositories return ENTITIES (not DTOs)
2. ✅ Validators instantiated manually (NOT DI)
3. ✅ Navigation properties readonly (write via repository)
4. ✅ Use int (not long) except size/cursor
5. ✅ No default values in entities
6. ✅ Keep ALL using statements
7. ✅ Commands return BaseCommandResponse<Guid>
8. ✅ GET = AllowAnonymous, Write = Authorize
9. ✅ Extract userId with fallback
10. ✅ File-scoped namespaces

**Reference**: docs/QUICK_REFERENCE.md

---

## 📊 Progress Tracking

Update this as you complete entities:

**Phase 1 Progress**: 0/7 entities (0%)
- [ ] Actor
- [ ] Tenant
- [ ] TenantUser
- [ ] TenantSettings
- [ ] IndexedDid
- [ ] SyncState
- [ ] AtprotoRecord

**Phase 2 Progress**: 0/3 entities (0%)
- [ ] UserAuthenticationToken
- [ ] UserExternalLogin
- [ ] ActorKeyStore

**Phase 3 Progress**: 0/3 entities (0%)
- [ ] EventRegistration
- [ ] EventSessionLanguage
- [ ] StorageObject

**Phase 4 Progress**: 0/20 entities (0%)
- [ ] Madhab (controller only)
- [ ] AudienceAge
- [ ] AudienceGender
- [ ] EventType
- [ ] EventStatus
- [ ] EventFormat
- [ ] VisibilityType
- [ ] RegistrationMode
- [ ] OrganizationRole
- [ ] OrganizationPosition
- [ ] UserRole
- [ ] ActorType
- [ ] DidCustodyType
- [ ] FileType
- [ ] OwnerType
- [ ] TagType
- [ ] ApprovalStatus

**Phase 5 Progress**: 0/3 entities (0%)
- [ ] EventCategories
- [ ] EventTags
- [ ] TagTypeTags

---

## 🎯 Recommendation

**DO NOT** try to implement all 33 entities in one session. Instead:

1. **Start with Actor** (1 hour) - Most important, complex pattern
2. **Complete Phase 1** (4 hours) - Core federation
3. **Test thoroughly** - Ensure patterns work
4. **Continue phases incrementally** - 3-5 entities per day

**Total Estimated Time**: 12-16 hours spread over 3-4 days

This is sustainable and maintainable. Rushing will introduce bugs and pattern violations.

---

**Next Steps**:
1. Review this plan
2. Implement Actor entity (reference implementation)
3. Verify pattern compliance
4. Continue with Tenant, TenantUser
5. Track progress in this document

Good luck! 🚀
