using System.Collections.Frozen;

namespace Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Provides in-memory cached access to lookup/reference data tables.
/// Data is loaded at startup and stored in FrozenDictionary for optimal read performance.
/// </summary>
public interface ILookupDataCache
{
    FrozenDictionary<int, T> Get<T>() where T : class;
    T? GetById<T>(int id) where T : class;
    IReadOnlyList<T> GetAll<T>() where T : class;
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
