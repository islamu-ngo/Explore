// ABOUTME: Client service for tenant onboarding status and tenant policy settings workflows.
// ABOUTME: Supports startup gating and tenant policy questionnaire submission through BFF endpoints.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Services;

public interface ITenantOnboardingService
{
    Task<TenantOnboardingStatusDto?> GetStatusAsync();
    Task<TenantPolicySettingsDto> GetSettingsAsync();
    Task<BaseCommandResponseOfGuid> CompleteAsync(TenantPolicySettingsDto settings);
    Task<BaseCommandResponseOfGuid> UpdateSettingsAsync(
        TenantPolicySettingsDto settings,
        bool forceAnnouncementBarRedisplay = false);
    Task<IReadOnlyList<AiAssistantModelDto>> GetAiModelsAsync(string endpointUrl, string? apiKey);
}

public class TenantOnboardingService : ITenantOnboardingService
{
    private readonly IEventApiClient _api;
    private readonly ILogger<TenantOnboardingService> _logger;

    public TenantOnboardingService(
        IEventApiClient api,
        ILogger<TenantOnboardingService> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<TenantOnboardingStatusDto?> GetStatusAsync()
    {
        try
        {
            var resource = await _api.GetTenantOnboardingStatusAsync(cancellationToken: CancellationToken.None);
            return resource.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tenant onboarding status.");
            return null;
        }
    }

    public async Task<TenantPolicySettingsDto> GetSettingsAsync()
    {
        try
        {
            return await _api.GetTenantOnboardingPolicySettingsAsync(cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tenant policy settings.");
            return DefaultSettings();
        }
    }

    public Task<BaseCommandResponseOfGuid> CompleteAsync(TenantPolicySettingsDto settings) =>
        SendCommandAsync(() => _api.CompleteTenantOnboardingAsync(
            ToRequest(settings, false),
            cancellationToken: CancellationToken.None));

    public Task<BaseCommandResponseOfGuid> UpdateSettingsAsync(
        TenantPolicySettingsDto settings,
        bool forceAnnouncementBarRedisplay = false) =>
        SendCommandAsync(() => _api.UpdateTenantOnboardingPolicySettingsAsync(
            ToRequest(settings, forceAnnouncementBarRedisplay),
            cancellationToken: CancellationToken.None));

    public async Task<IReadOnlyList<AiAssistantModelDto>> GetAiModelsAsync(string endpointUrl, string? apiKey)
    {
        try
        {
            var response = await _api.GetAiAssistantModelsAsync(new AiAssistantModelDiscoveryRequestDto
            {
                EndpointUrl = endpointUrl,
                ApiKey = apiKey
            }, cancellationToken: CancellationToken.None);

            return response.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover AI assistant models.");
            return [];
        }
    }

    private async Task<BaseCommandResponseOfGuid> SendCommandAsync(
        Func<Task<BaseCommandResponseOfGuid>> sendFunc)
    {
        try
        {
            return await sendFunc();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Tenant onboarding endpoint returned status {StatusCode}.", ex.StatusCode);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"Request failed with status {ex.StatusCode}."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call tenant onboarding endpoint.");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = "Request failed.",
                Errors = [ex.Message]
            };
        }
    }

    private static TenantPolicySettingsDto DefaultSettings() => new()
    {
        AllowUserSubmittedEvents = true,
        AllowOrganizationSubmittedEvents = true,
        AllowGroupSubmittedEvents = true,
        AllowOrganizationSelfRegistration = true,
        AllowGroupSelfRegistration = true,
        RequireOrganizationVerification = true,
        PreferredHomePage = "EventList",
        CanOverrideHomePagePreference = true,
        CanOverrideSubdomain = true,
        CanOverrideCustomDomain = true,
        CanOverrideEventCardClickBehavior = true,
        CanOverrideCommunityGuidelines = true,
        AiAssistantProvider = "none"
    };

    private static UpdateTenantPolicyRequest ToRequest(
        TenantPolicySettingsDto settings,
        bool forceAnnouncementBarRedisplay) => new()
        {
            AllowUserSubmittedEvents = settings.AllowUserSubmittedEvents,
            AllowOrganizationSubmittedEvents = settings.AllowOrganizationSubmittedEvents,
            AllowGroupSubmittedEvents = settings.AllowGroupSubmittedEvents,
            AllowOrganizationSelfRegistration = settings.AllowOrganizationSelfRegistration,
            AllowGroupSelfRegistration = settings.AllowGroupSelfRegistration,
            EventCardClickOpensDetailPage = settings.EventCardClickOpensDetailPage,
            RequireEventApproval = settings.RequireEventApproval,
            RequireOrganizationVerification = settings.RequireOrganizationVerification,
            PreferredHomePage = settings.PreferredHomePage,
            Subdomain = settings.Subdomain,
            CustomDomain = settings.CustomDomain,
            AnnouncementBarEnabled = settings.AnnouncementBarEnabled,
            AnnouncementBarMessage = settings.AnnouncementBarMessage,
            AnnouncementBarLinkText = settings.AnnouncementBarLinkText,
            AnnouncementBarLinkUrl = settings.AnnouncementBarLinkUrl,
            ForceAnnouncementBarRedisplay = forceAnnouncementBarRedisplay,
            CommunityGuidelinesContent = settings.CommunityGuidelinesContent,
            RenderPolicyPreset = settings.RenderPolicyPreset,
            EnableAdvancedRenderPolicyOverrides = settings.EnableAdvancedRenderPolicyOverrides,
            GlobalRenderMode = settings.GlobalRenderMode,
            GlobalPrerenderEnabled = settings.GlobalPrerenderEnabled,
            PublicSeoRenderMode = settings.PublicSeoRenderMode,
            PublicSeoPrerenderEnabled = settings.PublicSeoPrerenderEnabled,
            OperationalRenderMode = settings.OperationalRenderMode,
            OperationalPrerenderEnabled = settings.OperationalPrerenderEnabled,
            AdminRenderMode = settings.AdminRenderMode,
            AdminPrerenderEnabled = settings.AdminPrerenderEnabled,
            AiAssistantEnabled = settings.AiAssistantEnabled,
            AiAssistantProvider = settings.AiAssistantProvider,
            AiAssistantEndpointUrl = settings.AiAssistantEndpointUrl,
            AiAssistantApiKey = settings.AiAssistantApiKey,
            AiAssistantModelId = settings.AiAssistantModelId,
            AiAssistantAllowedModelIds = settings.AiAssistantAllowedModelIds,
            AiAssistantAllowAnonymousAccess = settings.AiAssistantAllowAnonymousAccess,
            McpEnabled = settings.McpEnabled,
            McpEnableLegacySse = settings.McpEnableLegacySse
        };
}
