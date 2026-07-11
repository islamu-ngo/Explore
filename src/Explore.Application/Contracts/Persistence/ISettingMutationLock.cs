// ABOUTME: Application port for linearizing setting mutations by canonical setting key.
// ABOUTME: Runs mutation delegates under one transaction-scoped lock and retry-aware unit of work.

namespace Explore.Application.Contracts.Persistence;

public interface ISettingMutationLock
{
    Task<T> ExecuteAsync<T>(
        string canonicalSettingKey,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteManyAsync<T>(
        IEnumerable<string> canonicalSettingKeys,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
