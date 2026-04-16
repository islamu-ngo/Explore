// ABOUTME: Partial class containing SaveChangesAsync override with automatic audit field population.
// ABOUTME: Handles IConcurrencyAware, IAuditableEntity, and ISoftDeletable entity interceptors.

using Explore.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence;

public partial class ExploreDbContext
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
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
                        auditable.CreatedBy = userId;
                        break;

                    case EntityState.Modified:
                        if (auditable.UpdatedAt == null || auditable.UpdatedAt == default(DateTime))
                        {
                            auditable.UpdatedAt = now;
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

        return await base.SaveChangesAsync(cancellationToken);
    }

    private Guid? GetCurrentUserId()
    {
        return CurrentUserService?.UserId;
    }
}
