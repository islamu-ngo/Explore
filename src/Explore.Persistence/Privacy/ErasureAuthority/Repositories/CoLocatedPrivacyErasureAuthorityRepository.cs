// ABOUTME: Stores co-located privacy-erasure authority facts through short-lived application DbContexts.
// ABOUTME: Commits each append independently before the application mutation transaction begins.

using System.Data;
using Explore.Application.Configuration;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Explore.Persistence.Privacy.ErasureAuthority.Repositories;

public sealed class CoLocatedPrivacyErasureAuthorityRepository(
    IDbContextFactory<ExploreDbContext> dbContextFactory,
    TimeProvider timeProvider,
    IOptions<PrivacyErasureOptions> options) : IPrivacyErasureAuthority
{
    public async Task<PrivacyErasureIntent> AppendAsync(
        PrivacyErasureRequest intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        await using ExploreDbContext strategyContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        IExecutionStrategy strategy = strategyContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using ExploreDbContext context =
                await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);
            var repository = new ApplicationDatabasePrivacyErasureLedgerRepository(
                context,
                timeProvider,
                options.Value.AuthorityRetention);
            PrivacyErasureIntent fact = await repository.AppendAsync(intent, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return fact;
        });
    }

    public async Task<IReadOnlyList<PrivacyErasureIntent>> ReadAfterAsync(
        long authoritySequence,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using ExploreDbContext context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var repository = new ApplicationDatabasePrivacyErasureLedgerRepository(
            context,
            timeProvider,
            options.Value.AuthorityRetention);
        return await repository.ReadAfterAsync(authoritySequence, limit, cancellationToken);
    }
}
