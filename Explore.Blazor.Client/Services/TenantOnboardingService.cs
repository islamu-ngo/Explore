// ABOUTME: Client service for tenant onboarding status and tenant policy settings workflows.
// ABOUTME: Supports startup gating and tenant policy questionnaire submission through BFF endpoints.

using System.Net.Http.Json;

namespace Explore.Blazor.Client.Services;

public interface ITenantOnboardingService
{
    Task<TenantOnboardingStatusModel?> GetStatusAsync();
    Task<TenantPolicySettingsModel> GetSettingsAsync();
    Task<InstanceCommandResponseModel> CompleteAsync(TenantPolicySettingsModel settings);
    Task<InstanceCommandResponseModel> UpdateSettingsAsync(TenantPolicySettingsModel settings);
}

public class TenantOnboardingService : ITenantOnboardingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TenantOnboardingService> _logger;

    public TenantOnboardingService(
        IHttpClientFactory httpClientFactory,
        ILogger<TenantOnboardingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TenantOnboardingStatusModel?> GetStatusAsync()
    {
        try
        {
            return await CreateClient().GetFromJsonAsync<TenantOnboardingStatusModel>("api/TenantOnboarding/status");
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
            var result = await CreateClient().GetFromJsonAsync<TenantPolicySettingsModel>("api/TenantOnboarding/settings");
            return result ?? new TenantPolicySettingsModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tenant policy settings.");
            return new TenantPolicySettingsModel();
        }
    }

    public Task<InstanceCommandResponseModel> CompleteAsync(TenantPolicySettingsModel settings) =>
        SendCommandAsync(HttpMethod.Post, "api/TenantOnboarding/complete", settings);

    public Task<InstanceCommandResponseModel> UpdateSettingsAsync(TenantPolicySettingsModel settings) =>
        SendCommandAsync(HttpMethod.Put, "api/TenantOnboarding/settings", settings);

    private HttpClient CreateClient() => _httpClientFactory.CreateClient("BffClient");

    private async Task<InstanceCommandResponseModel> SendCommandAsync(
        HttpMethod method, string path, TenantPolicySettingsModel settings)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path)
            {
                Content = JsonContent.Create(settings)
            };

            var response = await CreateClient().SendAsync(request);
            var payload = await response.Content.ReadFromJsonAsync<InstanceCommandResponseModel>();
            if (payload is not null)
            {
                return payload;
            }

            return new InstanceCommandResponseModel
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode
                    ? "Operation completed successfully."
                    : $"Operation failed with status {(int)response.StatusCode}."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call tenant onboarding endpoint {Path}.", path);
            return new InstanceCommandResponseModel
            {
                Success = false,
                Message = "Request failed.",
                Errors = [ex.Message]
            };
        }
    }
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
    public bool IsTenantWhiteLabelingEnabled { get; set; }
    public string PreferredHomePage { get; set; } = "EventList";
    public string InstanceBaseDomain { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string CustomDomain { get; set; } = string.Empty;
    public string BrandDisplayName { get; set; } = string.Empty;
    public string BrandLogoUrl { get; set; } = string.Empty;
    public string BrandFaviconUrl { get; set; } = string.Empty;
    public string BrandCustomCssUrl { get; set; } = string.Empty;
    public bool CanOverrideHomePagePreference { get; set; } = true;
    public bool CanOverrideSubdomain { get; set; } = true;
    public bool CanOverrideCustomDomain { get; set; } = true;
    public bool CanOverrideBrandDisplayName { get; set; } = true;
    public bool CanOverrideBrandLogoUrl { get; set; } = true;
    public bool CanOverrideBrandFaviconUrl { get; set; } = true;
    public bool CanOverrideBrandCustomCssUrl { get; set; } = true;
    public bool CanOverrideEventCardClickBehavior { get; set; } = true;

    // Community guidelines
    public string CommunityGuidelinesContent { get; set; } = string.Empty;
    public bool CanOverrideCommunityGuidelines { get; set; } = true;

    // Render policy tenant overrides
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
    // Category-level override flags
    public bool CanOverrideSmtp { get; set; }
    public bool CanOverrideStorage { get; set; }
    public bool CanOverrideAnalytics { get; set; }
}
