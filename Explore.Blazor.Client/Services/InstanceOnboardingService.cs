// ABOUTME: Client service for instance onboarding status and governance settings endpoints.
// ABOUTME: Powers first-run startup gating and runtime instance settings updates from Blazor pages.

using System.Net.Http.Json;

namespace Explore.Blazor.Client.Services;

public interface IInstanceOnboardingService
{
    Task<InstanceOnboardingStatusModel?> GetStatusAsync();
    Task<InstanceGovernanceSettingsModel> GetSettingsAsync();
    Task<InstanceCommandResponseModel> CompleteAsync(InstanceGovernanceSettingsModel settings);
    Task<InstanceCommandResponseModel> UpdateSettingsAsync(InstanceGovernanceSettingsModel settings);
}

public class InstanceOnboardingService : IInstanceOnboardingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InstanceOnboardingService> _logger;

    public InstanceOnboardingService(
        IHttpClientFactory httpClientFactory,
        ILogger<InstanceOnboardingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<InstanceOnboardingStatusModel?> GetStatusAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BffClient");
            return await client.GetFromJsonAsync<InstanceOnboardingStatusModel>("api/v1/InstanceOnboarding/status");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch instance onboarding status.");
            return null;
        }
    }

    public async Task<InstanceGovernanceSettingsModel> GetSettingsAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BffClient");
            var result = await client.GetFromJsonAsync<InstanceGovernanceSettingsModel>("api/v1/InstanceOnboarding/settings");
            return result ?? new InstanceGovernanceSettingsModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch instance governance settings.");
            return new InstanceGovernanceSettingsModel();
        }
    }

    public async Task<InstanceCommandResponseModel> CompleteAsync(InstanceGovernanceSettingsModel settings)
    {
        return await SendAsync(HttpMethod.Post, "api/v1/InstanceOnboarding/complete", settings);
    }

    public async Task<InstanceCommandResponseModel> UpdateSettingsAsync(InstanceGovernanceSettingsModel settings)
    {
        return await SendAsync(HttpMethod.Put, "api/v1/InstanceOnboarding/settings", settings);
    }

    private async Task<InstanceCommandResponseModel> SendAsync(HttpMethod method, string url, InstanceGovernanceSettingsModel settings)
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
            _logger.LogError(ex, "Failed to call instance onboarding endpoint {Url}.", url);
            return new InstanceCommandResponseModel
            {
                Success = false,
                Message = "Request failed.",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}

public class InstanceOnboardingStatusModel
{
    public bool IsCompleted { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsCurrentUserInstanceAdmin { get; set; }
    public string? SelectedDeploymentMode { get; set; }
}

public class InstanceGovernanceSettingsModel
{
    public string DeploymentMode { get; set; } = "SingleTenant";
    public bool AllowTenantSelfServiceRegistration { get; set; }
    public string DefaultPublicHomePage { get; set; } = "EventList";
    public bool EnableIslamicModule { get; set; } = true;
    public bool EnableTechModule { get; set; } = true;
    public bool AllowUserSubmittedEvents { get; set; } = true;
    public bool RequireOrganizationVerification { get; set; } = true;
    public bool AllowTenantToOmitVerification { get; set; }
    public string InstanceBaseDomain { get; set; } = string.Empty;
    public bool AllowTenantCustomDomains { get; set; } = true;
    public string DefaultBrandDisplayName { get; set; } = "ISLAMU Explore";
    public string DefaultBrandLogoUrl { get; set; } = string.Empty;
    public string DefaultBrandFaviconUrl { get; set; } = string.Empty;
    public string DefaultBrandCustomCssUrl { get; set; } = string.Empty;
    public bool LockTenantHomePagePreference { get; set; }
    public bool LockTenantSubdomain { get; set; }
    public bool LockTenantCustomDomain { get; set; }
    public bool LockTenantBrandDisplayName { get; set; }
    public bool LockTenantBrandLogoUrl { get; set; }
    public bool LockTenantBrandFaviconUrl { get; set; }
    public bool LockTenantBrandCustomCssUrl { get; set; }
}

public class InstanceCommandResponseModel
{
    public bool Success { get; set; }
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}
