// ABOUTME: Client service for tenant onboarding status and tenant policy settings workflows.
// ABOUTME: Supports startup gating and tenant policy questionnaire submission through BFF endpoints.

using Refit;
namespace Explore.Blazor.Client.Services;

public interface ITenantOnboardingService
{
    Task<TenantOnboardingStatusModel?> GetStatusAsync();
    Task<TenantPolicySettingsModel> GetSettingsAsync();
    Task<InstanceCommandResponseModel> CompleteAsync(TenantPolicySettingsModel settings);
    Task<InstanceCommandResponseModel> UpdateSettingsAsync(TenantPolicySettingsModel settings);
    Task<IReadOnlyList<AiAssistantModelOptionModel>> GetAiModelsAsync(string endpointUrl, string? apiKey);
}

public class TenantOnboardingService : ITenantOnboardingService
{
    private readonly ITenantOnboardingApi _api;
    private readonly ILogger<TenantOnboardingService> _logger;

    public TenantOnboardingService(
        ITenantOnboardingApi api,
        ILogger<TenantOnboardingService> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<TenantOnboardingStatusModel?> GetStatusAsync()
    {
        try
        {
            var response = await _api.GetStatusAsync(CancellationToken.None);
            return response.IsSuccessStatusCode ? response.Content : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tenant onboarding status.");
            return null;
        }
    }

    public async Task<TenantPolicySettingsModel> GetSettingsAsync()
    {
        try
        {
            var response = await _api.GetSettingsAsync(CancellationToken.None);
            return response.IsSuccessStatusCode ? response.Content! : new TenantPolicySettingsModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tenant policy settings.");
            return new TenantPolicySettingsModel();
        }
    }

    public Task<InstanceCommandResponseModel> CompleteAsync(TenantPolicySettingsModel settings) =>
        SendCommandAsync(() => _api.CompleteAsync(settings, CancellationToken.None));

    public Task<InstanceCommandResponseModel> UpdateSettingsAsync(TenantPolicySettingsModel settings) =>
        SendCommandAsync(() => _api.UpdateSettingsAsync(settings, CancellationToken.None));

    public async Task<IReadOnlyList<AiAssistantModelOptionModel>> GetAiModelsAsync(string endpointUrl, string? apiKey)
    {
        try
        {
            var response = await _api.GetAiModelsAsync(new AiAssistantModelDiscoveryRequestModel
            {
                EndpointUrl = endpointUrl,
                ApiKey = apiKey
            }, CancellationToken.None);

            return response.IsSuccessStatusCode && response.Content is not null
                ? response.Content
                : [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover AI assistant models.");
            return [];
        }
    }

    private async Task<InstanceCommandResponseModel> SendCommandAsync(
        Func<Task<IApiResponse<InstanceCommandResponseModel>>> sendFunc)
    {
        try
        {
            var response = await sendFunc();

            if (response.IsSuccessStatusCode && response.Content is not null)
            {
                return response.Content;
            }

            return new InstanceCommandResponseModel
            {
                Success = false,
                StatusCode = (int)response.StatusCode,
                Message = response.Error?.Content ?? $"Request failed with status {(int)response.StatusCode}."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call tenant onboarding endpoint.");
            return new InstanceCommandResponseModel
            {
                Success = false,
                Message = "Request failed.",
                Errors = [ex.Message]
            };
        }
    }
}

public class AiAssistantModelDiscoveryRequestModel
{
    public string EndpointUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
}

public class AiAssistantModelOptionModel
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int? MaxInputTokens { get; set; }
    public int? MaxOutputTokens { get; set; }
    public bool SupportsToolProposals { get; set; }
    public bool SupportsStreaming { get; set; }
}

public class TenantOnboardingStatusModel
{
    public bool IsCompleted { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsCurrentUserTenantAdministrator { get; set; }
    public bool IsCurrentUserPlatformAdministrator { get; set; }
    public Guid TenantId { get; set; }
}

public class TenantPolicySettingsModel
{
    public bool AllowUserSubmittedEvents { get; set; } = true;
    public bool AllowOrganizationSubmittedEvents { get; set; } = true;
    public bool AllowGroupSubmittedEvents { get; set; } = true;
    public bool AllowOrganizationSelfRegistration { get; set; } = true;
    public bool AllowGroupSelfRegistration { get; set; } = true;
    public bool EventCardClickOpensDetailPage { get; set; }
    public bool RequireEventApproval { get; set; }
    public bool RequireOrganizationVerification { get; set; } = true;
    public bool CanTenantOmitVerification { get; set; }
    public string PreferredHomePage { get; set; } = "EventList";
    public string InstanceBaseDomain { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string CustomDomain { get; set; } = string.Empty;
    public bool AnnouncementBarEnabled { get; set; }
    public string AnnouncementBarMessage { get; set; } = string.Empty;
    public string AnnouncementBarLinkText { get; set; } = string.Empty;
    public string AnnouncementBarLinkUrl { get; set; } = string.Empty;
    public int AnnouncementBarRevision { get; set; }
    public bool ForceAnnouncementBarRedisplay { get; set; }
    public bool CanOverrideHomePagePreference { get; set; } = true;
    public bool CanOverrideSubdomain { get; set; } = true;
    public bool CanOverrideCustomDomain { get; set; } = true;
    public bool CanOverrideEventCardClickBehavior { get; set; } = true;
    public string CommunityGuidelinesContent { get; set; } = string.Empty;
    public bool CanOverrideCommunityGuidelines { get; set; } = true;
    public string RenderPolicyPreset { get; set; } = string.Empty;
    public bool EnableAdvancedRenderPolicyOverrides { get; set; }
    public string GlobalRenderMode { get; set; } = string.Empty;
    public bool GlobalPrerenderEnabled { get; set; }
    public string PublicSeoRenderMode { get; set; } = string.Empty;
    public bool PublicSeoPrerenderEnabled { get; set; }
    public string OperationalRenderMode { get; set; } = string.Empty;
    public bool OperationalPrerenderEnabled { get; set; }
    public string AdminRenderMode { get; set; } = string.Empty;
    public bool AdminPrerenderEnabled { get; set; }
    public bool CanOverrideRenderPolicy { get; set; }
    public bool CanOverridePublicSeoRenderPolicy { get; set; }
    public bool CanOverrideOperationalRenderPolicy { get; set; }
    public bool CanOverrideAdminRenderPolicy { get; set; }
    public bool CanOverrideSmtp { get; set; }
    public bool CanOverrideStorage { get; set; }
    public bool CanOverrideAnalytics { get; set; }
    public bool AiAssistantEnabled { get; set; }
    public string AiAssistantProvider { get; set; } = "none";
    public string AiAssistantEndpointUrl { get; set; } = string.Empty;
    public string AiAssistantApiKey { get; set; } = string.Empty;
    public string AiAssistantModelId { get; set; } = string.Empty;
    public List<string> AiAssistantAllowedModelIds { get; set; } = [];
    public bool AiAssistantAllowAnonymousAccess { get; set; }
    public bool CanOverrideAiAssistant { get; set; }
    public bool CanOverrideMcp { get; set; }
    public bool CanOverrideMcpLegacySse { get; set; }
    public bool McpEnabled { get; set; }
    public bool McpEnableLegacySse { get; set; }
}
