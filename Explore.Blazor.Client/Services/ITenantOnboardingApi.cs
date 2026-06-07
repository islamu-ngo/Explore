// ABOUTME: Refit interface for tenant onboarding BFF endpoints.
// ABOUTME: Covers onboarding status, tenant policy settings, and completion workflow.

using Refit;

namespace Explore.Blazor.Client.Services;

public interface ITenantOnboardingApi
{
    [Get("/api/TenantOnboarding/status")]
    Task<IApiResponse<TenantOnboardingStatusModel>> GetStatusAsync(CancellationToken cancellationToken);

    [Get("/api/TenantOnboarding/settings")]
    Task<IApiResponse<TenantPolicySettingsModel>> GetSettingsAsync(CancellationToken cancellationToken);

    [Post("/api/ai/assistant/models")]
    Task<IApiResponse<IReadOnlyList<Explore.Blazor.Client.Services.AiAssistantModelOptionModel>>> GetAiModelsAsync(
        [Body] Explore.Blazor.Client.Services.AiAssistantModelDiscoveryRequestModel request,
        CancellationToken cancellationToken);

    [Post("/api/TenantOnboarding/complete")]
    Task<IApiResponse<InstanceCommandResponseModel>> CompleteAsync([Body] TenantPolicySettingsModel settings, CancellationToken cancellationToken);

    [Put("/api/TenantOnboarding/settings")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateSettingsAsync([Body] TenantPolicySettingsModel settings, CancellationToken cancellationToken);
}
