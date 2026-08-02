// ABOUTME: Implements per-setting transaction-scoped mutation locking for every supported relational provider.
// ABOUTME: Orders canonical keys deterministically and delegates cross-instance locking to the database provider.

using Explore.Application.Contracts.Persistence;
using Explore.Persistence.Database;

namespace Explore.Persistence;

public sealed class RelationalSettingMutationLock(
    ExploreDbContext dbContext,
    IUnitOfWork unitOfWork) : ISettingMutationLock
{
    public Task<T> ExecuteAsync<T>(
        string canonicalSettingKey,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalSettingKey);
        return ExecuteManyAsync([canonicalSettingKey], operation, cancellationToken);
    }

    public Task<T> ExecuteManyAsync<T>(
        IEnumerable<string> canonicalSettingKeys,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(canonicalSettingKeys);
        string[] orderedKeys = NormalizeCanonicalKeys(canonicalSettingKeys);
        if (orderedKeys.Length == 0)
        {
            throw new ArgumentException("At least one canonical setting key is required.", nameof(canonicalSettingKeys));
        }

        return dbContext.Database.CurrentTransaction is not null
            ? ExecuteInsideTransactionAsync(orderedKeys, operation, cancellationToken)
            : unitOfWork.ExecuteInTransactionAsync(
                token => ExecuteInsideTransactionAsync(orderedKeys, operation, token),
                cancellationToken);
    }

    private async Task<T> ExecuteInsideTransactionAsync<T>(
        IReadOnlyList<string> canonicalSettingKeys,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var leases = new List<IAsyncDisposable>(canonicalSettingKeys.Count);
        try
        {
            foreach (string canonicalSettingKey in canonicalSettingKeys)
            {
                leases.Add(await RelationalNamedLock.AcquireTransactionAsync(
                    dbContext,
                    $"explore:setting-mutation:{canonicalSettingKey}",
                    cancellationToken));
            }

            return await operation(cancellationToken);
        }
        finally
        {
            for (int index = leases.Count - 1; index >= 0; index--)
            {
                await leases[index].DisposeAsync();
            }
        }
    }

    internal static long ComputeStableLockKey(string canonicalSettingKey) =>
        RelationalNamedLock.ComputeStableKey($"explore:setting-mutation:{canonicalSettingKey.Trim().ToLowerInvariant()}");

    internal static string[] NormalizeCanonicalKeys(IEnumerable<string> canonicalSettingKeys)
    {
        return canonicalSettingKeys
            .Select(key =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(key);
                return key.Trim().ToLowerInvariant();
            })
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
    }
}
