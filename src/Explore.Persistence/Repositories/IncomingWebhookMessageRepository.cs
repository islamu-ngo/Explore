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
    private readonly ExploreDbContext _dbContext;

    public IncomingWebhookMessageRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryCreateAsync(IncomingWebhookMessage message, CancellationToken cancellationToken)
    {
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

    public async Task<IncomingWebhookMessage?> GetByProviderMessageIdForUpdateAsync(
        Guid tenantId,
        string provider,
        string providerMessageId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.IncomingWebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .FirstOrDefaultAsync(
                e => e.TenantId == tenantId
                    && e.Provider == provider
                    && e.ProviderMessageId == providerMessageId,
                cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
