// ABOUTME: Fetches the authenticated user's feature flags from GET /api/features/my-flags.
// ABOUTME: Hydrates the FeatureStateContainer on login; no OpenFeature SDK dependency in UI.

using System.Net;

namespace Explore.Blazor.Client.Services;

public interface IFeatureFlagClientService
{
    Task LoadFlagsAsync();
}

public class FeatureFlagClientService : IFeatureFlagClientService
{
    private readonly IFeatureFlagApi _api;
    private readonly FeatureStateContainer _featureState;
    private readonly ILogger<FeatureFlagClientService> _logger;

    public FeatureFlagClientService(
        IFeatureFlagApi api,
        FeatureStateContainer featureState,
        ILogger<FeatureFlagClientService> logger)
    {
        _api = api;
        _featureState = featureState;
        _logger = logger;
    }

    public async Task LoadFlagsAsync()
    {
        try
        {
            var response = await _api.GetMyFlagsAsync(CancellationToken.None);

            if (response.IsSuccessStatusCode && response.Content is not null)
            {
                _featureState.SetFlags(response.Content);
                _logger.LogDebug("Loaded {Count} feature flags", response.Content.Count);
                return;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogDebug("Feature flags not loaded — user is not authenticated");
                return;
            }

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("Failed to load feature flags from API: {StatusCode}", (int)response.StatusCode);
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogDebug("Feature flags not loaded — user is not authenticated");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load feature flags from API");
        }
    }
}
