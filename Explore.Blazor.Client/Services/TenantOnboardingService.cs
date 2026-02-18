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
            var client = _httpClientFactory.CreateClient("BffClient");
            return await client.GetFromJsonAsync<TenantOnboardingStatusModel>("api/v1/TenantOnboarding/status");
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
            var client = _httpClientFactory.CreateClient("BffClient");
            var result = await client.GetFromJsonAsync<TenantPolicySettingsModel>("api/v1/TenantOnboarding/settings");
            return result ?? new TenantPolicySettingsModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tenant policy settings.");
            return new TenantPolicySettingsModel();
        }
    }

    public async Task<InstanceCommandResponseModel> CompleteAsync(TenantPolicySettingsModel settings)
    {
        return await SendAsync(HttpMethod.Post, "api/v1/TenantOnboarding/complete", settings);
    }

    public async Task<InstanceCommandResponseModel> UpdateSettingsAsync(TenantPolicySettingsModel settings)
    {
        return await SendAsync(HttpMethod.Put, "api/v1/TenantOnboarding/settings", settings);
    }

    private async Task<InstanceCommandResponseModel> SendAsync(HttpMethod method, string url, TenantPolicySettingsModel settings)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BffClient");
            using var request = new HttpRequestMessage(method, url)
            {
                Content = JsonContent.Create(settings)
            };

            var response = await client.SendAsync(request);
            var payload = await response.Content.ReadFromJsonAsync<InstanceCommandResponseModel>();
            if (payload != null)
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
            _logger.LogError(ex, "Failed to call tenant onboarding endpoint {Url}.", url);
            return new InstanceCommandResponseModel
            {
                Success = false,
                Message = "Request failed.",
                Errors = new List<string> { ex.Message }
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
    public bool RequireEventApproval { get; set; }
    public bool RequireOrganizationVerification { get; set; } = true;
    public bool CanTenantOmitVerification { get; set; }
    public bool IsTenantWhiteLabelingEnabled { get; set; }
    public string PreferredHomePage { get; set; } = "EventList";
    public string InstanceBaseDomain { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string CustomDomain { get; set; } = string.Empty;
    public string BrandDisplayName { get; set; } = "ISLAMU Explore";
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
}
