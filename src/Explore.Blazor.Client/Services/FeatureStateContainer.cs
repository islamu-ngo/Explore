// ABOUTME: Lightweight scoped service storing the authenticated user's feature flags as booleans.
// ABOUTME: Hydrated from GET /api/features/my-flags on login; components check IsEnabled("key").

namespace Explore.Blazor.Client.Services;

public class FeatureStateContainer
{
    private Dictionary<string, bool> _flags = new();

    public bool IsEnabled(string flagKey) =>
        _flags.TryGetValue(flagKey, out var enabled) && enabled;

    public void SetFlags(Dictionary<string, bool> flags) =>
        _flags = flags ?? new();

    public IReadOnlyDictionary<string, bool> All => _flags;
}
