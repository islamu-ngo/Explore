// ABOUTME: Defers Combined-profile dynamic authentication setup until the real API pipeline is dispatchable.
// ABOUTME: Shares one initialization task across concurrent cold requests and preserves fail-closed startup.

namespace Explore.Blazor.Services;

public sealed class DynamicAuthSchemeInitializer
{
    private readonly Lazy<Task> _initialization;

    public DynamicAuthSchemeInitializer(IDynamicAuthSchemeManager schemeManager)
    {
        _initialization = new Lazy<Task>(
            schemeManager.InitializeAsync,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Task InitializeAsync() => _initialization.Value;
}
