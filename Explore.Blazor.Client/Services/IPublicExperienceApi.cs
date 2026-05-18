// ABOUTME: Refit interface for public experience BFF endpoints.
// ABOUTME: Covers anonymous-safe settings and shell resolution for startup routing and white-label UI.

using Refit;

namespace Explore.Blazor.Client.Services;

public interface IPublicExperienceApi
{
    [Get("/api/PublicExperience/settings")]
    Task<IApiResponse<PublicExperienceSettingsModel>> GetSettingsAsync(CancellationToken cancellationToken);

    [Get("/api/PublicExperience/shell")]
    Task<IApiResponse<PublicExperienceShellModel>> GetShellAsync(CancellationToken cancellationToken);
}
