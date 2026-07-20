// ABOUTME: Partial class containing SaveChangesAsync override with automatic audit and generated field population.
// ABOUTME: Preserves pre-generated Added stamps while rotating Modified IConcurrencyAware entities and audit metadata.

using Explore.Domain;
using Explore.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence;

public partial class ExploreDbContext
{
    public override int SaveChanges() => SaveChanges(acceptAllChangesOnSuccess: true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareTrackedEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        PrepareTrackedEntities();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareTrackedEntities()
    {
        ValidateEventLocationCarrierConsistency();
        var userId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Explore.Domain.WebhookAuditEvent
                && entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Webhook audit events are append-only and cannot be modified or deleted.");
            }

            if (entry.Entity is Explore.Domain.EventLocationDisclosureAudit
                    or Explore.Domain.EventLocationExactReadAudit
                    or Explore.Domain.PrivacyErasureReplayCheckpoint
                && entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Event location privacy evidence is append-only and cannot be modified or deleted.");
            }

            if (entry.Entity is Explore.Domain.Event eventEntity &&
                entry.State == EntityState.Added &&
                string.IsNullOrWhiteSpace(eventEntity.PublicCode))
            {
                eventEntity.PublicCode = GeneratePublicCode();
            }

            if (entry.Entity is IConcurrencyAware concurrencyAware &&
                (entry.State == EntityState.Added || entry.State == EntityState.Modified) &&
                (entry.State == EntityState.Modified || concurrencyAware.ConcurrencyStamp == Guid.Empty))
            {
                concurrencyAware.ConcurrencyStamp = entry.Entity is Explore.Domain.EventLocation
                    or Explore.Domain.Location
                    or Explore.Domain.LocationRoom
                    ? Guid.CreateVersion7()
                    : Guid.NewGuid();
            }

            if (entry.Entity is IAuditableEntity auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedAt = now;
                        auditable.CreatedBy = userId ?? auditable.CreatedBy;
                        break;

                    case EntityState.Modified:
                        if (auditable.UpdatedAt == null || auditable.UpdatedAt == default(DateTime))
                        {
                            auditable.UpdatedAt = now;
                        }

                        if (userId.HasValue)
                        {
                            auditable.UpdatedBy = userId;
                        }
                        break;
                }
            }

            if (entry.Entity is ISoftDeletable deletable && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;

                deletable.IsDeleted = true;
                deletable.DeletedAt = now;
                deletable.DeletedBy = userId;

                if (entry.Entity is IAuditableEntity auditableDeleted)
                {
                    auditableDeleted.UpdatedAt = now;
                    auditableDeleted.UpdatedBy = userId;
                }
            }
        }

    }

    private void ValidateEventLocationCarrierConsistency()
    {
        foreach (var entry in ChangeTracker.Entries()
                     .Where(item => item.State is EntityState.Added or EntityState.Modified))
        {
            switch (entry.Entity)
            {
                case EventSession session when session.EventLocationId.HasValue:
                    ValidateEventCarrier(
                        nameof(EventSession),
                        session.TenantId,
                        session.EventId,
                        session.EventLocationId.Value,
                        session.LocationId,
                        session.RoomId);
                    break;
                case EventSessionGroup group when group.EventLocationId.HasValue:
                    ValidateEventCarrier(
                        nameof(EventSessionGroup),
                        group.TenantId,
                        group.EventId,
                        group.EventLocationId.Value,
                        group.LocationId,
                        group.RoomId);
                    break;
                case EventAgendaItem agendaItem when agendaItem.EventLocationId.HasValue:
                    ValidateEventCarrier(
                        nameof(EventAgendaItem),
                        agendaItem.TenantId,
                        agendaItem.EventId,
                        agendaItem.EventLocationId.Value,
                        agendaItem.LocationId,
                        agendaItem.RoomId);
                    break;
                case EventSessionAgendaItem sessionAgendaItem when sessionAgendaItem.EventLocationId.HasValue:
                    ValidateSessionAgendaCarrier(sessionAgendaItem);
                    break;
            }
        }
    }

    private void ValidateEventCarrier(
        string carrierName,
        Guid tenantId,
        Guid eventId,
        Guid eventLocationId,
        Guid? locationId,
        Guid? roomId)
    {
        RequireCarrierTenant(tenantId);
        EventLocation eventLocation = FindActiveEventLocation(tenantId, eventLocationId);
        if (eventLocation.EventId != eventId)
        {
            throw new InvalidOperationException($"{carrierName} and EventLocation must belong to the same event.");
        }

        ValidatePhysicalKeys(carrierName, tenantId, eventLocation, locationId, roomId);
    }

    private void ValidateSessionAgendaCarrier(EventSessionAgendaItem carrier)
    {
        RequireCarrierTenant(carrier.TenantId);
        EventLocation eventLocation = FindActiveEventLocation(carrier.TenantId, carrier.EventLocationId!.Value);
        EventSession session = ChangeTracker.Entries<EventSession>()
            .Where(item => item.State != EntityState.Deleted)
            .Select(item => item.Entity)
            .SingleOrDefault(item => item.TenantId == carrier.TenantId && item.Id == carrier.EventSessionId)
            ?? EventSessions.AsNoTracking().SingleOrDefault(
                item => item.TenantId == carrier.TenantId && item.Id == carrier.EventSessionId)
            ?? throw new InvalidOperationException("EventSessionAgendaItem requires an active session in the current tenant.");
        if (session.EventId != eventLocation.EventId)
        {
            throw new InvalidOperationException("EventSessionAgendaItem session and EventLocation must belong to the same event.");
        }

        ValidatePhysicalKeys(
            nameof(EventSessionAgendaItem),
            carrier.TenantId,
            eventLocation,
            carrier.LocationId,
            null);
    }

    private EventLocation FindActiveEventLocation(Guid tenantId, Guid eventLocationId)
    {
        EventLocation? eventLocation = ChangeTracker.Entries<EventLocation>()
            .Where(item => item.State != EntityState.Deleted)
            .Select(item => item.Entity)
            .SingleOrDefault(item => item.TenantId == tenantId && item.Id == eventLocationId)
            ?? EventLocations.AsNoTracking().SingleOrDefault(
                item => item.TenantId == tenantId && item.Id == eventLocationId);
        if (eventLocation is null || eventLocation.IsDeleted)
        {
            throw new InvalidOperationException("A carrier requires an active EventLocation in the current tenant.");
        }

        return eventLocation;
    }

    private void ValidatePhysicalKeys(
        string carrierName,
        Guid tenantId,
        EventLocation eventLocation,
        Guid? locationId,
        Guid? roomId)
    {
        if (locationId != eventLocation.LocationId)
        {
            throw new InvalidOperationException($"{carrierName} LocationId must match its EventLocation.");
        }

        if (!roomId.HasValue)
        {
            return;
        }

        bool matchingRoomExists = ChangeTracker.Entries<LocationRoom>()
            .Where(item => item.State != EntityState.Deleted)
            .Select(item => item.Entity)
            .Any(item => !item.IsDeleted
                && item.TenantId == tenantId
                && item.Id == roomId.Value
                && item.LocationId == locationId)
            || LocationRooms.AsNoTracking().Any(item =>
                item.TenantId == tenantId
                && item.Id == roomId.Value
                && item.LocationId == locationId);
        if (!matchingRoomExists)
        {
            throw new InvalidOperationException($"{carrierName} room must belong to its EventLocation's physical Location.");
        }
    }

    private void RequireCarrierTenant(Guid tenantId)
    {
        if (IsTenantFilterBypassed)
        {
            return;
        }

        Guid ambientTenantId = TenantFilterTenantId
            ?? throw new InvalidOperationException("A tenant context is required to persist EventLocation carriers.");
        if (tenantId != ambientTenantId)
        {
            throw new InvalidOperationException("EventLocation carriers must belong to the current tenant.");
        }
    }

    private Guid? GetCurrentUserId()
    {
        return CurrentUserService?.UserId;
    }

    private static string GeneratePublicCode()
    {
        return Guid.NewGuid().ToString("N")[..12];
    }
}
