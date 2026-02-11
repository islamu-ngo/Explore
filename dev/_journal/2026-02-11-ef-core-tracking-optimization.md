# EF Core Query Tracking Optimization

**Date**: 2026-02-11
**Status**: ✅ Completed

## Objective

Optimize EF Core read-only queries by:
1. Ensuring all read-only queries use `.AsNoTracking()` (already done for most queries)
2. **Upgrading complex queries with multiple `Include()`/`ThenInclude()` to use `.AsNoTrackingWithIdentityResolution()`**

## Background

### AsNoTracking vs AsNoTrackingWithIdentityResolution

**AsNoTracking()**:
- Disables change tracking for read-only queries → improves performance
- Does NOT perform identity resolution
- If the same entity appears multiple times in result (due to complex Include/ThenInclude), you get **duplicate instances**

**AsNoTrackingWithIdentityResolution()** (EF Core 5.0+):
- Disables change tracking BUT maintains identity resolution
- Uses a temporary change tracker in the background (garbage collected after enumeration)
- Ensures same entity instance when it appears multiple times in complex object graphs
- **Critical for queries that include the same navigation property multiple times**

### When to Use Which?

✅ **Use `.AsNoTracking()`** for:
- Simple queries with no Include
- Queries with single simple Include chain

✅ **Use `.AsNoTrackingWithIdentityResolution()`** for:
- Complex queries where the same navigation property is included multiple times with different `ThenInclude()` paths
- Example: `Include(e => e.Actor).ThenInclude(a => a.Type)` + `Include(e => e.Actor).ThenInclude(a => a.Photo)`

## Changes Made

### Files Updated (4 repositories)

#### 1. **EventRepository.cs** (6 methods)
All complex Event queries now use `.AsNoTrackingWithIdentityResolution()`:

**Pattern detected**: `e.Actor` included twice + `e.IslamicAspect` included twice
```csharp
.Include(e => e.Actor)
    .ThenInclude(a => a.ActorType)      // Actor included once
.Include(e => e.Actor)
    .ThenInclude(a => a!.ProfilePicture) // Actor included again → needs identity resolution
.Include(e => e.IslamicAspect)
    .ThenInclude(a => a!.Madhab)         // IslamicAspect included once
.Include(e => e.IslamicAspect)
    .ThenInclude(a => a!.PrimaryLanguage) // IslamicAspect included again → needs identity resolution
```

**Methods updated**:
- `GetEventsWithDetails()` - Line 30
- `GetEventWithDetails()` - Line 54
- `GetMyEventsWithDetails()` - Line 84
- `GetEventsWithDetailsPaged(int, int)` - Line 120
- `GetEventsWithDetailsPaged(int, int, EventQuerySpecification)` - Line 155
- `GetMyEventsWithDetailsPaged()` - Line 254

#### 2. **OrganizationRepository.cs** (1 method)
**Pattern detected**: `o.Members` included three times with different ThenInclude paths
```csharp
.Include(o => o.Members)
    .ThenInclude(m => m.User)
.Include(o => o.Members)
    .ThenInclude(m => m.OrganizationRole)
.Include(o => o.Members)
    .ThenInclude(m => m.OrganizationPosition)
```

**Method updated**:
- `GetOrganizationWithDetails()` - Line 44

#### 3. **EventTagsRepository.cs** (1 method)
**Pattern detected**: `e.Actor` included twice
```csharp
.Include(e => e.Actor)
    .ThenInclude(a => a.ActorType)
.Include(e => e.Actor)
    .ThenInclude(a => a!.ProfilePicture)
```

**Method updated**:
- `GetEventsByTag()` - Line 27

#### 4. **EventCategoriesRepository.cs** (1 method)
**Pattern detected**: Same as EventTagsRepository
```csharp
.Include(e => e.Actor)
    .ThenInclude(a => a.ActorType)
.Include(e => e.Actor)
    .ThenInclude(a => a!.ProfilePicture)
```

**Method updated**:
- `GetEventsByCategory()` - Line 27

## Repositories NOT Changed (Correctly Using AsNoTracking)

These repositories have simple Include chains where `.AsNoTracking()` is correct:

- **UserRepository.cs** - Single ThenInclude chain
- **OrganizationMemberRepository.cs** - Single ThenInclude chain
- **EventSessionLanguageRepository.cs** - Single ThenInclude chain
- **EventSessionSpeakerRepository.cs** - Single ThenInclude chain
- **EventSessionAgendaItemRepository.cs** - Single ThenInclude chain
- **EventRegistrationRepository.cs** - Single ThenInclude chain
- **All other repositories** - No ThenInclude or simple queries

## Verification

### Build Status
```bash
dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet
```
✅ **Result**: 0 errors, 290 warnings (pre-existing nullable reference type warnings)

### Test Status
```bash
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release
```
✅ **Result**: 2/2 tests passed

**Tests validated**:
- `Create_ShouldPersistEvent()` - Entity creation and retrieval
- `GetEventWithDetails_ShouldReturnIncludes()` - Complex Include/ThenInclude chains with identity resolution

## Performance Impact

### Before (AsNoTracking only)
- ❌ **Memory overhead**: Duplicate entity instances for same navigation property
- ❌ **Reference inequality**: Same entity appears as different object instances
- ✅ **No change tracking overhead**: Good performance for read-only scenarios

### After (AsNoTrackingWithIdentityResolution)
- ✅ **Memory optimization**: Single entity instance reused across complex graph
- ✅ **Reference equality**: Same entity = same object instance
- ✅ **No change tracking overhead**: Still optimized for read-only scenarios
- ⚠️ **Slight overhead**: Temporary change tracker during enumeration (negligible, GC'd immediately)

## References

- [EF Core Tracking Documentation](https://github.com/dotnet/entityframework.docs/blob/main/entity-framework/core/querying/tracking.md)
- [EF Core Identity Resolution](https://github.com/dotnet/entityframework.docs/blob/main/entity-framework/core/change-tracking/identity-resolution.md)
- Context7 MCP: `/dotnet/entityframework.docs`

## Conclusion

✅ All complex queries with multiple Include/ThenInclude statements now use `.AsNoTrackingWithIdentityResolution()`
✅ Simple queries continue using `.AsNoTracking()` for optimal performance
✅ All tests passing
✅ No breaking changes

**Total changes**: 9 methods across 4 repository files
