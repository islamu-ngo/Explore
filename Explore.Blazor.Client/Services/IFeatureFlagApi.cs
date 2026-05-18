// ABOUTME: Refit interface for feature flag BFF endpoints.
// ABOUTME: Covers authenticated user feature flag hydration.

using Refit;

namespace Explore.Blazor.Client.Services;

public interface IFeatureFlagApi
{
    [Get("/api/features/my-flags")]
    Task<IApiResponse<Dictionary<string, bool>>> GetMyFlagsAsync(CancellationToken cancellationToken);
}
