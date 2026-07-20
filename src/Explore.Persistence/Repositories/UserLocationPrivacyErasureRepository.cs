// ABOUTME: EF Core adapter for owner-bounded global Private Home erasure across every tenant.
// ABOUTME: Preserves scheduling references while tracking Homes, rooms, associations, and user actors.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class UserLocationPrivacyErasureRepository(ExploreDbContext dbContext)
    : IUserLocationPrivacyErasureRepository
{
    public async Task<IReadOnlyList<Location>> GetOwnedPrivateHomesAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        RequireId(ownerUserId, nameof(ownerUserId));
        List<Location> homes = await dbContext.Locations
            .IgnoreTenantFilter(TenantFilterBypassReasons.UserPrivacyErasure)
            .Where(location => location.OwnerUserId == ownerUserId
                && location.LocationKindId == (int)LocationKindEnum.PrivateHome)
            .OrderBy(location => location.TenantId)
            .ThenBy(location => location.Id)
            .ToListAsync(cancellationToken);
        if (homes.Count == 0)
        {
            return homes;
        }

        Guid[] locationIds = homes.Select(home => home.Id).ToArray();
        List<LocationRoom> rooms = await dbContext.LocationRooms
            .IgnoreTenantFilter(TenantFilterBypassReasons.UserPrivacyErasure)
            .IncludeDeleted()
            .Where(room => locationIds.Contains(room.LocationId))
            .OrderBy(room => room.LocationId)
            .ThenBy(room => room.Id)
            .ToListAsync(cancellationToken);
        ILookup<Guid, LocationRoom> roomsByLocation = rooms.ToLookup(room => room.LocationId);
        foreach (Location home in homes)
        {
            home.Rooms = roomsByLocation[home.Id].ToList();
        }

        return homes;
    }

    public async Task<IReadOnlyList<EventLocation>> GetEventLocationsAsync(
        IReadOnlyCollection<Guid> locationIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(locationIds);
        Guid[] normalizedIds = locationIds.Distinct().ToArray();
        if (normalizedIds.Any(id => id == Guid.Empty))
        {
            throw new ArgumentException("Location ids must be non-empty.", nameof(locationIds));
        }

        if (normalizedIds.Length == 0)
        {
            return [];
        }

        return await dbContext.EventLocations
            .IgnoreTenantFilter(TenantFilterBypassReasons.UserPrivacyErasure)
            .Where(eventLocation => eventLocation.LocationId.HasValue
                && normalizedIds.Contains(eventLocation.LocationId.Value))
            .OrderBy(eventLocation => eventLocation.TenantId)
            .ThenBy(eventLocation => eventLocation.EventId)
            .ThenBy(eventLocation => eventLocation.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Actor>> GetUserActorsAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        RequireId(ownerUserId, nameof(ownerUserId));
        return await dbContext.Actors
            .IgnoreTenantFilter(TenantFilterBypassReasons.UserPrivacyErasure)
            .IncludeDeleted()
            .Include(actor => actor.Pii)
            .Where(actor => actor.UserId == ownerUserId)
            .OrderBy(actor => actor.TenantId)
            .ThenBy(actor => actor.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        IReadOnlyCollection<EventLocationDisclosureAudit> audits,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audits);
        dbContext.EventLocationDisclosureAudits.AddRange(audits);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void RequireId(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A non-empty id is required.", parameterName);
        }
    }
}
