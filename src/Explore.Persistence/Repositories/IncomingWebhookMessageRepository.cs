// ABOUTME: EF Core repository for incoming integration webhook idempotency and processing state.
// ABOUTME: Captures provider callbacks safely before outbox-backed aggregate mutations run.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Explore.Persistence.Repositories;

public class IncomingWebhookMessageRepository : IIncomingWebhookMessageRepository
{
    private const string UniqueViolationSqlState = "23505";
    private const int MaxFailureCategoryLength = 100;
    private const int MaxSafeDetailLength = 1000;

    private readonly ExploreDbContext _dbContext;

    public IncomingWebhookMessageRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryCreateAsync(IncomingWebhookMessage message, CancellationToken cancellationToken)
    {
        if (message.Id == Guid.Empty)
        {
            message.Id = Guid.CreateVersion7();
        }

        try
        {
            await _dbContext.IncomingWebhookMessages.AddAsync(message, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolationSqlState })
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<IncomingWebhookMessage?> GetByProviderMessageIdAsync(
        Guid tenantId,
        string provider,
        string providerMessageId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.IncomingWebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.TenantId == tenantId
                    && e.Provider == provider
                    && e.ProviderMessageId == providerMessageId,
                cancellationToken);
    }

    public async Task MarkProcessedAsync(
        Guid tenantId,
        Guid messageId,
        DateTime processedAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.IncomingWebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .Where(e => e.TenantId == tenantId && e.Id == messageId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, IncomingWebhookMessageStatus.Processed)
                .SetProperty(e => e.ProcessedAt, processedAt)
                .SetProperty(e => e.UpdatedAt, processedAt), cancellationToken);
    }

    public async Task MarkRejectedAsync(
        Guid tenantId,
        Guid messageId,
        string failureCategory,
        string? safeDetail,
        DateTime rejectedAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.IncomingWebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .Where(e => e.TenantId == tenantId && e.Id == messageId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, IncomingWebhookMessageStatus.Rejected)
                .SetProperty(e => e.FailureCategory, Truncate(failureCategory, MaxFailureCategoryLength))
                .SetProperty(e => e.SafeDetail, Truncate(safeDetail, MaxSafeDetailLength))
                .SetProperty(e => e.UpdatedAt, rejectedAt), cancellationToken);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return value is null || value.Length <= maxLength ? value : value[..maxLength];
    }
}
