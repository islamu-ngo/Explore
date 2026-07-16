// ABOUTME: Appends and reads tenant-filtered PII-free evidence for exceptional exact location reads.
// ABOUTME: Validates aggregate ownership and enforces a bounded no-tracking read surface.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class EventLocationExactReadAuditRepository(ExploreDbContext dbContext)
    : IEventLocationExactReadAuditRepository
{
    public async Task<EventLocationExactReadAudit> AppendAsync(
        EventLocationExactReadAudit audit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audit);
        RequireTenant(audit.TenantId);

        bool eventLocationExists = await dbContext.EventLocations
            .AsNoTracking()
            .AnyAsync(item => item.Id == audit.EventLocationId && item.TenantId == audit.TenantId, cancellationToken);
        if (!eventLocationExists)
        {
            throw new InvalidOperationException("Exact-read evidence requires an active EventLocation in the current tenant.");
        }

        dbContext.EventLocationExactReadAudits.Add(audit);
        await dbContext.SaveChangesAsync(cancellationToken);
        return audit;
    }

    public async Task<IReadOnlyList<EventLocationExactReadAudit>> GetByEventLocationsAsync(
        IReadOnlyCollection<Guid> eventLocationIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eventLocationIds);
        RequireTenant();
        Guid[] normalizedIds = eventLocationIds.Distinct().ToArray();
        if (normalizedIds.Length > IEventLocationExactReadAuditRepository.MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(eventLocationIds),
                $"Exact-read audit batches cannot exceed {IEventLocationExactReadAuditRepository.MaximumBatchSize} unique ids.");
        }

        if (normalizedIds.Length == 0)
        {
            return [];
        }

        return await dbContext.EventLocationExactReadAudits
            .AsNoTracking()
            .Where(item => normalizedIds.Contains(item.EventLocationId))
            .OrderBy(item => item.OccurredAtUtc)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    private void RequireTenant(Guid? entityTenantId = null)
    {
        if (dbContext.IsTenantFilterBypassed)
        {
            return;
        }

        Guid tenantId = dbContext.TenantFilterTenantId
            ?? throw new InvalidOperationException("A tenant context is required for EventLocation privacy persistence.");
        if (entityTenantId.HasValue && entityTenantId.Value != tenantId)
        {
            throw new InvalidOperationException("EventLocation privacy evidence must belong to the current tenant.");
        }
    }
}
