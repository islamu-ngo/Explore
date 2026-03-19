// ABOUTME: EF Core implementation of IUnitOfWork using CreateExecutionStrategy for Npgsql retry compatibility.
// ABOUTME: Wraps the transaction and all operations inside the retrying strategy's ExecuteAsync scope.

using Explore.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence;

public sealed class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly ExploreDbContext _dbContext;

    public EfCoreUnitOfWork(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
    {
        // InMemory provider does not support transactions or execution strategies — run operation directly
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            await operation(ct);
            return;
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                await operation(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }
}
