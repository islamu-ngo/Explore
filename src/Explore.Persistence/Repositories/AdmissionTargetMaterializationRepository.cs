// ABOUTME: Loads publication-scoped schedule and admission entities under the ambient transaction.
// ABOUTME: Stages reusable target and policy additions for the catalog repository's atomic save.

using Explore.Application.Contracts.Admissions;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class AdmissionTargetMaterializationRepository(ExploreDbContext dbContext)
    : IAdmissionTargetMaterializationRepository
{
    public async Task<IReadOnlyList<EventSession>> ListScheduleSessionsForUpdateAsync(
        Guid tenantId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await LockEventSessionsAsync(tenantId, eventId, cancellationToken);
        return await dbContext.EventSessions
            .Where(session =>
                session.TenantId == tenantId &&
                session.EventId == eventId &&
                !session.IsDeleted)
            .OrderBy(session => session.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdmissionTarget>> ListTargetsForUpdateAsync(
        Guid tenantId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await LockAdmissionTargetsAsync(tenantId, eventId, cancellationToken);
        return await dbContext.AdmissionTargets
            .Where(target => target.TenantId == tenantId && target.EventId == eventId)
            .OrderBy(target => target.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdmissionCheckInPolicy>> ListPoliciesAsync(
        Guid tenantId,
        Guid eventId,
        CancellationToken cancellationToken) => await dbContext.AdmissionCheckInPolicies
            .Where(policy =>
                policy.TenantId == tenantId &&
                dbContext.AdmissionTargets.Any(target =>
                    target.TenantId == tenantId &&
                    target.EventId == eventId &&
                    target.Id == policy.AdmissionTargetId))
            .OrderBy(policy => policy.Id)
            .ToListAsync(cancellationToken);

    public async Task AddTargetsAsync(
        IReadOnlyCollection<AdmissionTarget> targets,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(targets);
        await dbContext.AdmissionTargets.AddRangeAsync(targets, cancellationToken);
    }

    public async Task AddPoliciesAsync(
        IReadOnlyCollection<AdmissionCheckInPolicy> policies,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(policies);
        await dbContext.AdmissionCheckInPolicies.AddRangeAsync(policies, cancellationToken);
    }

    private async Task LockEventSessionsAsync(
        Guid tenantId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        EnsurePostgresTransaction();
        if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                SELECT id
                FROM event_sessions
                WHERE tenant_id = {tenantId} AND event_id = {eventId} AND is_deleted = false
                FOR UPDATE
                """, cancellationToken);
        }
    }

    private async Task LockAdmissionTargetsAsync(
        Guid tenantId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        EnsurePostgresTransaction();
        if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                SELECT id
                FROM admission_targets
                WHERE tenant_id = {tenantId} AND event_id = {eventId}
                FOR UPDATE
                """, cancellationToken);
        }
    }

    private void EnsurePostgresTransaction()
    {
        if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL" &&
            dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Admission target materialization row locks require an active transaction.");
        }
    }
}
