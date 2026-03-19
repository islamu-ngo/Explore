// ABOUTME: Fetches the authenticated user's feature flags from GET /api/features/my-flags.
// ABOUTME: Hydrates the FeatureStateContainer on login; no OpenFeature SDK dependency in UI.

using System.Net.Http.Json;

namespace Explore.Blazor.Client.Services;

public interface IFeatureFlagClientService
{
    Task LoadFlagsAsync();
}

public class FeatureFlagClientService : IFeatureFlagClientService
{
    private readonly HttpClient _httpClient;
    private readonly FeatureStateContainer _featureState;
    private readonly ILogger<FeatureFlagClientService> _logger;

    public FeatureFlagClientService(
        HttpClient httpClient,
        FeatureStateContainer featureState,
        ILogger<FeatureFlagClientService> logger)
    {
        _httpClient = httpClient;
        _featureState = featureState;
        _logger = logger;
    }

    public async Task LoadFlagsAsync()
    {
        try
        {
            var flags = await _httpClient.GetFromJsonAsync<Dictionary<string, bool>>("api/features/my-flags");
            if (flags is not null)
            {
                _featureState.SetFlags(flags);
                _logger.LogDebug("Loaded {Count} feature flags", flags.Count);
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogDebug("Feature flags not loaded — user is not authenticated");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load feature flags from API");
        }
    }
}
