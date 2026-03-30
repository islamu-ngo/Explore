// ABOUTME: EF Core repository for per-period API key credit quota tracking with race-safe atomic operations.
// ABOUTME: Uses raw SQL for atomic credit decrement and INSERT ON CONFLICT for lazy period provisioning.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class ExternalApiKeyQuotaRepository : GenericRepository<ExternalApiKeyQuota, Guid>, IExternalApiKeyQuotaRepository
{
    private readonly ExploreDbContext _dbContext;

    public ExternalApiKeyQuotaRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExternalApiKeyQuota?> GetCurrentPeriodQuota(Guid externalApiKeyId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.ExternalApiKeyQuotas
            .AsNoTracking()
            .Where(q => q.ExternalApiKeyId == externalApiKeyId
                        && q.PeriodStart <= today
                        && q.PeriodEnd >= today)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ExternalApiKeyQuota> LazyProvisionPeriod(
        Guid externalApiKeyId,
        DateOnly periodStart,
        DateOnly periodEnd,
        int creditLimit,
        int rolloverCredits,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var newId = Guid.CreateVersion7();

        // INSERT ON CONFLICT DO NOTHING — race-safe idempotent provisioning
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO ""ExternalApiKeyQuotas"" (""Id"", ""ExternalApiKeyId"", ""PeriodStart"", ""PeriodEnd"", ""CreditLimit"", ""CreditsUsed"", ""RolloverCredits"", ""RequestCount"", ""CreatedAt"")
               VALUES ({newId}, {externalApiKeyId}, {periodStart}, {periodEnd}, {creditLimit}, {0}, {rolloverCredits}, {0}, {now})
               ON CONFLICT (""ExternalApiKeyId"", ""PeriodStart"") DO NOTHING",
            cancellationToken);

        // Return the existing or newly created row
        var quota = await _dbContext.ExternalApiKeyQuotas
            .Where(q => q.ExternalApiKeyId == externalApiKeyId && q.PeriodStart == periodStart)
            .FirstAsync(cancellationToken);

        return quota;
    }

    public async Task<bool> TryConsumeCredits(Guid quotaId, int amount, CancellationToken cancellationToken = default)
    {
        // Atomic conditional UPDATE — race-safe credit enforcement + request counting
        // Only succeeds if credits_used + amount <= credit_limit + rollover_credits
        var rowsAffected = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE ""ExternalApiKeyQuotas""
               SET ""CreditsUsed"" = ""CreditsUsed"" + {amount},
                   ""RequestCount"" = ""RequestCount"" + 1,
                   ""UpdatedAt"" = {DateTime.UtcNow}
               WHERE ""Id"" = {quotaId}
                 AND ""CreditsUsed"" + {amount} <= ""CreditLimit"" + ""RolloverCredits""",
            cancellationToken);

        return rowsAffected > 0;
    }

    public async Task IncrementRequestCount(Guid quotaId, CancellationToken cancellationToken = default)
    {
        // Atomic request count increment without credit consumption — for unlimited keys
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE ""ExternalApiKeyQuotas""
               SET ""RequestCount"" = ""RequestCount"" + 1,
                   ""UpdatedAt"" = {DateTime.UtcNow}
               WHERE ""Id"" = {quotaId}",
            cancellationToken);
    }

    public async Task<IReadOnlyList<ExternalApiKeyQuota>> GetQuotaHistory(Guid externalApiKeyId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExternalApiKeyQuotas
            .AsNoTracking()
            .Where(q => q.ExternalApiKeyId == externalApiKeyId)
            .OrderByDescending(q => q.PeriodStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantApiKeyUsageSummary>> GetUsageByTenant(
        Guid tenantId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExternalApiKeyQuotas
            .AsNoTracking()
            .Include(q => q.ExternalApiKey)
            .Where(q => q.ExternalApiKey.TenantId == tenantId
                        && q.PeriodStart >= from
                        && q.PeriodEnd <= to)
            .GroupBy(q => new
            {
                q.ExternalApiKeyId,
                q.ExternalApiKey.Name,
                q.ExternalApiKey.TenantId,
                q.ExternalApiKey.OwnerType,
                q.ExternalApiKey.OwnerId,
                q.ExternalApiKey.CreditLimit
            })
            .Select(g => new TenantApiKeyUsageSummary(
                g.Key.ExternalApiKeyId,
                g.Key.Name,
                g.Key.TenantId,
                (int)g.Key.OwnerType,
                g.Key.OwnerId,
                g.Sum(q => q.RequestCount),
                g.Sum(q => q.CreditsUsed),
                g.Key.CreditLimit ?? 0))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantApiKeyUsageSummary>> GetUsagePlatformWide(
        DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExternalApiKeyQuotas
            .IgnoreTenantFilter()
            .AsNoTracking()
            .Include(q => q.ExternalApiKey)
            .Where(q => q.PeriodStart >= from && q.PeriodEnd <= to)
            .GroupBy(q => new
            {
                q.ExternalApiKeyId,
                q.ExternalApiKey.Name,
                q.ExternalApiKey.TenantId,
                q.ExternalApiKey.OwnerType,
                q.ExternalApiKey.OwnerId,
                q.ExternalApiKey.CreditLimit
            })
            .Select(g => new TenantApiKeyUsageSummary(
                g.Key.ExternalApiKeyId,
                g.Key.Name,
                g.Key.TenantId,
                (int)g.Key.OwnerType,
                g.Key.OwnerId,
                g.Sum(q => q.RequestCount),
                g.Sum(q => q.CreditsUsed),
                g.Key.CreditLimit ?? 0))
            .ToListAsync(cancellationToken);
    }
}
