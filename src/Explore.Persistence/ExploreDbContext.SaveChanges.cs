// ABOUTME: Partial class containing SaveChangesAsync override with automatic audit and generated field population.
// ABOUTME: Handles public event codes plus IConcurrencyAware, IAuditableEntity, and ISoftDeletable entity interceptors.

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
        var userId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Explore.Domain.WebhookAuditEvent
                && entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Webhook audit events are append-only and cannot be modified or deleted.");
            }

            if (entry.Entity is Explore.Domain.Event eventEntity &&
                entry.State == EntityState.Added &&
                string.IsNullOrWhiteSpace(eventEntity.PublicCode))
            {
                eventEntity.PublicCode = GeneratePublicCode();
            }

            if (entry.Entity is IConcurrencyAware concurrencyAware &&
                (entry.State == EntityState.Added || entry.State == EntityState.Modified))
            {
                concurrencyAware.ConcurrencyStamp = Guid.NewGuid();
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

    private Guid? GetCurrentUserId()
    {
        return CurrentUserService?.UserId;
    }

    private static string GeneratePublicCode()
    {
        return Guid.NewGuid().ToString("N")[..12];
    }
}
