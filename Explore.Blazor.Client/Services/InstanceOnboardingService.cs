// ABOUTME: Client service for instance onboarding and governance settings via sub-resource endpoints.
// ABOUTME: Powers first-run wizard, instance admin settings, and infrastructure config from Blazor pages.

using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using Explore.Blazor.Client.Services.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Refit;

namespace Explore.Blazor.Client.Services;

public interface IInstanceOnboardingService
{
    Task<SystemOnboardingStatusModel?> GetSystemOnboardingStatusAsync();
    Task<OnboardingPreflightModel?> GetOnboardingPreflightAsync();
    Task<InstanceOnboardingStatusModel?> GetStatusAsync();
    Task<SetupSecretValidationResult> ValidateSecretAsync(string secret);
    Task<InstanceCommandResponseModel> CompleteAsync(OnboardingCompletionModel completion);

    Task<DeploymentModeModel> GetDeploymentModeAsync();
    Task<ModuleSettingsModel> GetModuleSettingsAsync();
    Task<EventPolicyModel> GetEventPolicyAsync();
    Task<OrganizationPolicyModel> GetOrganizationPolicyAsync();
    Task<BrandingSettingsModel> GetBrandingSettingsAsync();
    Task<DomainSettingsModel> GetDomainSettingsAsync();
    Task<TenantDelegationModel> GetTenantDelegationAsync();
    Task<RenderPolicyModel> GetRenderPolicyAsync();

    Task<InstanceCommandResponseModel> UpdateDeploymentModeAsync(string deploymentMode);
    Task<InstanceCommandResponseModel> UpdateModuleSettingsAsync(ModuleSettingsModel settings);
    Task<InstanceCommandResponseModel> UpdateEventPolicyAsync(EventPolicyModel settings);
    Task<InstanceCommandResponseModel> UpdateOrganizationPolicyAsync(OrganizationPolicyModel settings);
    Task<InstanceCommandResponseModel> UpdateBrandingSettingsAsync(BrandingSettingsModel settings);
    Task<InstanceCommandResponseModel> UpdateDomainSettingsAsync(DomainSettingsModel settings);
    Task<InstanceCommandResponseModel> UpdateTenantDelegationAsync(TenantDelegationModel settings);
    Task<InstanceCommandResponseModel> UpdateRenderPolicyAsync(RenderPolicyModel settings);

    Task<InstanceStorageSettingsModel> GetStorageSettingsAsync();
    Task<InstanceCommandResponseModel> UpdateStorageSettingsAsync(InstanceStorageSettingsModel settings);
    Task<StorageConnectionTestResult> TestStorageConnectionAsync();
    Task<InstanceSmtpSettingsModel> GetSmtpSettingsAsync();
    Task<InstanceCommandResponseModel> UpdateSmtpSettingsAsync(InstanceSmtpSettingsModel settings);
    Task<SmtpConnectionTestResult> TestSmtpConnectionAsync();
    Task<int> GetActiveTenantCountAsync();

    Task<AuthProviderConfigurationModel> GetAuthProviderConfigurationAsync();
    Task<InstanceCommandResponseModel> SaveAuthProviderConfigurationAsync(AuthProviderConfigurationModel config);
    Task<InstanceCommandResponseModel> UpdateAuthProviderConfigurationAsAdminAsync(AuthProviderConfigurationModel config);
    Task<bool> IsAuthProviderConfiguredAsync();
    Task RefreshAuthSchemesAsync();
    Task<bool> RefreshAuthSessionAsync();

    Task<AuthorizationProviderConfigurationModel> GetAuthorizationProviderConfigurationAsync();
    Task<AuthorizationProviderConfigurationModel> GetAuthorizationProviderConfigurationAsAdminAsync();
    Task<InstanceCommandResponseModel> SaveAuthorizationProviderConfigurationAsync(AuthorizationProviderConfigurationModel config);
    Task<InstanceCommandResponseModel> UpdateAuthorizationProviderConfigurationAsAdminAsync(AuthorizationProviderConfigurationModel config);
    Task<InstanceCommandResponseModel> SyncAuthorizationPolicyPackageAsync();
    Task<InstanceCommandResponseModel> SyncAuthorizationPolicyPackageAsAdminAsync();
    Task<PolicyPackageDownloadModel?> DownloadAuthorizationPolicyPackageAsync();
    Task<PolicyPackageDownloadModel?> DownloadAuthorizationPolicyPackageAsAdminAsync();
    Task<InstanceCommandResponseModel> VerifyCerbosEndpointAsync(string grpcEndpoint);
    Task<bool> IsAuthorizationProviderConfiguredAsync();

    Task<Models.Analytics.AnalyticsGovernanceSettingsModel> GetAnalyticsGovernanceSettingsAsync();
    Task<InstanceCommandResponseModel> UpdateAnalyticsGovernanceSettingsAsync(Models.Analytics.AnalyticsGovernanceSettingsModel settings);

    Task<FooterGovernanceSettingsModel> GetFooterGovernanceSettingsAsync();
    Task<InstanceCommandResponseModel> UpdateFooterGovernanceSettingsAsync(FooterGovernanceSettingsModel settings);
}

public class InstanceOnboardingService : IInstanceOnboardingService
{
    private readonly IInstanceOnboardingApi _api;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<InstanceOnboardingService> _logger;
    private readonly NavigationManager _navigation;

    public InstanceOnboardingService(
        IInstanceOnboardingApi api,
        IHttpClientFactory httpClientFactory,
        IJSRuntime jsRuntime,
        ILogger<InstanceOnboardingService> logger,
        NavigationManager navigation)
    {
        _api = api;
        _httpClientFactory = httpClientFactory;
        _jsRuntime = jsRuntime;
        _logger = logger;
        _navigation = navigation;
    }

    // ── Onboarding ───────────────────────────────────────────────────────

    public async Task<SystemOnboardingStatusModel?> GetSystemOnboardingStatusAsync()
    {
        try
        {
            var response = await _api.GetSystemOnboardingStatusAsync(CancellationToken.None);
            return response.IsSuccessStatusCode ? response.Content : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch system onboarding status.");
            return null;
        }
    }

    public async Task<OnboardingPreflightModel?> GetOnboardingPreflightAsync()
    {
        try
        {
            var response = await _api.GetOnboardingPreflightAsync(CancellationToken.None);
            return response.IsSuccessStatusCode ? response.Content : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch onboarding preflight.");
            return null;
        }
    }

    public async Task<InstanceOnboardingStatusModel?> GetStatusAsync()
    {
        try
        {
            var response = await _api.GetStatusAsync(CancellationToken.None);
            return response.IsSuccessStatusCode ? response.Content : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch instance onboarding status.");
            return null;
        }
    }

    public async Task<SetupSecretValidationResult> ValidateSecretAsync(string secret)
    {
        try
        {
            var response = await _api.ValidateSecretAsync(new ValidateSecretRequest { Secret = secret }, CancellationToken.None);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Setup secret validation rate-limited (429).");
                return new SetupSecretValidationResult
                {
                    Valid = false,
                    Error = "Too many attempts. Please wait a moment and try again."
                };
            }

            if (response.StatusCode == HttpStatusCode.Gone)
            {
                return response.Content ?? new SetupSecretValidationResult { Valid = false, Error = "Setup already completed." };
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Setup secret validation failed with HTTP {StatusCode}.",
                    (int)response.StatusCode);
                return new SetupSecretValidationResult
                {
                    Valid = false,
                    Error = $"Validation unavailable (HTTP {(int)response.StatusCode}). Please try again."
                };
            }

            return response.Content ?? new SetupSecretValidationResult { Valid = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate setup secret.");
            return new SetupSecretValidationResult { Valid = false };
        }
    }

    public async Task<InstanceCommandResponseModel> CompleteAsync(OnboardingCompletionModel completion)
    {
        try
        {
            var response = await _api.CompleteOnboardingAsync(completion, CancellationToken.None);
            return await MapCommandResponseAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete onboarding.");
            return FailedCommandResponse(ex.Message);
        }
    }

    // ── Governance Sub-Resource Reads ─────────────────────────────────────

    public async Task<DeploymentModeModel> GetDeploymentModeAsync() =>
        await GetSettingsAsync(_api.GetDeploymentModeAsync, () => new DeploymentModeModel());

    public async Task<ModuleSettingsModel> GetModuleSettingsAsync() =>
        await GetSettingsAsync(_api.GetModuleSettingsAsync, () => new ModuleSettingsModel());

    public async Task<EventPolicyModel> GetEventPolicyAsync() =>
        await GetSettingsAsync(_api.GetEventPolicyAsync, () => new EventPolicyModel());

    public async Task<OrganizationPolicyModel> GetOrganizationPolicyAsync() =>
        await GetSettingsAsync(_api.GetOrganizationPolicyAsync, () => new OrganizationPolicyModel());

    public async Task<BrandingSettingsModel> GetBrandingSettingsAsync() =>
        await GetSettingsAsync(_api.GetBrandingSettingsAsync, () => new BrandingSettingsModel());

    public async Task<DomainSettingsModel> GetDomainSettingsAsync() =>
        await GetSettingsAsync(_api.GetDomainSettingsAsync, () => new DomainSettingsModel());

    public async Task<TenantDelegationModel> GetTenantDelegationAsync() =>
        await GetSettingsAsync(_api.GetTenantDelegationAsync, () => new TenantDelegationModel());

    public async Task<RenderPolicyModel> GetRenderPolicyAsync() =>
        await GetSettingsAsync(_api.GetRenderPolicyAsync, () => new RenderPolicyModel());

    // ── Governance Sub-Resource Writes ────────────────────────────────────

    public Task<InstanceCommandResponseModel> UpdateDeploymentModeAsync(string deploymentMode) =>
        SendCommandAsync(
            ct => _api.UpdateDeploymentModeAsync(new UpdateDeploymentModeRequest { DeploymentMode = deploymentMode }, ct));

    public Task<InstanceCommandResponseModel> UpdateModuleSettingsAsync(ModuleSettingsModel settings) =>
        SendCommandAsync(ct => _api.UpdateModuleSettingsAsync(settings, ct));

    public Task<InstanceCommandResponseModel> UpdateEventPolicyAsync(EventPolicyModel settings) =>
        SendCommandAsync(ct => _api.UpdateEventPolicyAsync(settings, ct));

    public Task<InstanceCommandResponseModel> UpdateOrganizationPolicyAsync(OrganizationPolicyModel settings) =>
        SendCommandAsync(ct => _api.UpdateOrganizationPolicyAsync(settings, ct));

    public Task<InstanceCommandResponseModel> UpdateBrandingSettingsAsync(BrandingSettingsModel settings) =>
        SendCommandAsync(ct => _api.UpdateBrandingSettingsAsync(settings, ct));

    public Task<InstanceCommandResponseModel> UpdateDomainSettingsAsync(DomainSettingsModel settings) =>
        SendCommandAsync(ct => _api.UpdateDomainSettingsAsync(settings, ct));

    public Task<InstanceCommandResponseModel> UpdateTenantDelegationAsync(TenantDelegationModel settings) =>
        SendCommandAsync(ct => _api.UpdateTenantDelegationAsync(settings, ct));

    public Task<InstanceCommandResponseModel> UpdateRenderPolicyAsync(RenderPolicyModel settings) =>
        SendCommandAsync(ct => _api.UpdateRenderPolicyAsync(settings, ct));

    // ── Infrastructure Settings ──────────────────────────────────────────

    public async Task<InstanceStorageSettingsModel> GetStorageSettingsAsync() =>
        await GetSettingsAsync(_api.GetStorageSettingsAsync, () => new InstanceStorageSettingsModel());

    public Task<InstanceCommandResponseModel> UpdateStorageSettingsAsync(InstanceStorageSettingsModel settings) =>
        SendCommandAsync(ct => _api.UpdateStorageSettingsAsync(settings, ct));

    public async Task<StorageConnectionTestResult> TestStorageConnectionAsync()
    {
        try
        {
            var response = await _api.TestStorageConnectionAsync(CancellationToken.None);
            return response.IsSuccessStatusCode ? response.Content ?? new() : new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test storage connection.");
            return new StorageConnectionTestResult();
        }
    }

    public async Task<InstanceSmtpSettingsModel> GetSmtpSettingsAsync() =>
        await GetSettingsAsync(_api.GetSmtpSettingsAsync, () => new InstanceSmtpSettingsModel());

    public Task<InstanceCommandResponseModel> UpdateSmtpSettingsAsync(InstanceSmtpSettingsModel settings) =>
        SendCommandAsync(ct => _api.UpdateSmtpSettingsAsync(settings, ct));

    public async Task<SmtpConnectionTestResult> TestSmtpConnectionAsync()
    {
        try
        {
            var response = await _api.TestSmtpConnectionAsync(CancellationToken.None);
            return response.IsSuccessStatusCode ? response.Content ?? new() : new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test SMTP connection.");
            return new SmtpConnectionTestResult();
        }
    }

    public async Task<int> GetActiveTenantCountAsync()
    {
        try
        {
            var response = await _api.GetActiveTenantCountAsync(CancellationToken.None);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return int.TryParse(content, out var count) ? count : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active tenant count.");
            return 0;
        }
    }

    // ── Auth Provider Configuration ──────────────────────────────────────

    public async Task<AuthProviderConfigurationModel> GetAuthProviderConfigurationAsync() =>
        await GetSettingsAsync(_api.GetAuthProviderConfigurationAsync, () => new AuthProviderConfigurationModel());

    public Task<InstanceCommandResponseModel> SaveAuthProviderConfigurationAsync(AuthProviderConfigurationModel config) =>
        SendCommandAsync(ct => _api.SaveAuthProviderConfigurationAsync(config, ct));

    public Task<InstanceCommandResponseModel> UpdateAuthProviderConfigurationAsAdminAsync(AuthProviderConfigurationModel config) =>
        SendCommandAsync(ct => _api.UpdateAuthProviderConfigurationAsAdminAsync(config, ct));

    public async Task<bool> IsAuthProviderConfiguredAsync()
    {
        try
        {
            var response = await _api.IsAuthProviderConfiguredAsync(CancellationToken.None);
            return response.IsSuccessStatusCode && response.Content?.Configured == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check auth provider configuration status.");
            return false;
        }
    }

    // ── Authorization Provider Configuration ─────────────────────────────

    public async Task<AuthorizationProviderConfigurationModel> GetAuthorizationProviderConfigurationAsync() =>
        await GetSettingsAsync(_api.GetAuthorizationProviderConfigurationAsync, () => new AuthorizationProviderConfigurationModel());

    public async Task<AuthorizationProviderConfigurationModel> GetAuthorizationProviderConfigurationAsAdminAsync() =>
        await GetSettingsAsync(_api.GetAuthorizationProviderConfigurationAsAdminAsync, () => new AuthorizationProviderConfigurationModel());

    public Task<InstanceCommandResponseModel> SaveAuthorizationProviderConfigurationAsync(AuthorizationProviderConfigurationModel config) =>
        SendCommandAsync(ct => _api.SaveAuthorizationProviderConfigurationAsync(config, ct));

    public Task<InstanceCommandResponseModel> UpdateAuthorizationProviderConfigurationAsAdminAsync(AuthorizationProviderConfigurationModel config) =>
        SendCommandAsync(ct => _api.UpdateAuthorizationProviderConfigurationAsAdminAsync(config, ct));

    public Task<InstanceCommandResponseModel> SyncAuthorizationPolicyPackageAsync() =>
        SendCommandAsync(ct => _api.SyncAuthorizationPolicyPackageAsync(ct));

    public Task<InstanceCommandResponseModel> SyncAuthorizationPolicyPackageAsAdminAsync() =>
        SendCommandAsync(ct => _api.SyncAuthorizationPolicyPackageAsAdminAsync(ct));

    public Task<PolicyPackageDownloadModel?> DownloadAuthorizationPolicyPackageAsync() =>
        DownloadFileAsync("api/InstanceOnboarding/authz-provider-configuration/package", "authorization-policy-package.zip", "application/zip");

    public Task<PolicyPackageDownloadModel?> DownloadAuthorizationPolicyPackageAsAdminAsync() =>
        DownloadFileAsync("api/instance/settings/authz-provider/package", "authorization-policy-package.zip", "application/zip");

    public Task<InstanceCommandResponseModel> VerifyCerbosEndpointAsync(string grpcEndpoint) =>
        SendCommandAsync(ct => _api.VerifyCerbosEndpointAsync(new VerifyCerbosEndpointRequest { GrpcEndpoint = grpcEndpoint }, ct));

    public async Task<bool> IsAuthorizationProviderConfiguredAsync()
    {
        try
        {
            var response = await _api.IsAuthorizationProviderConfiguredAsync(CancellationToken.None);
            return response.IsSuccessStatusCode && response.Content?.Configured == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check authorization provider configuration status.");
            return false;
        }
    }

    public async Task RefreshAuthSchemesAsync()
    {
        try
        {
            using var response = await CreateBffSelfClient().PostAsync("/bff/auth/refresh-schemes", null);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to refresh auth schemes. Status: {StatusCode}", (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh auth schemes.");
        }
    }

    public async Task<bool> RefreshAuthSessionAsync()
    {
        try
        {
            using var response = await CreateBffSelfClient().PostAsync("/bff/auth/refresh-session/internal", null);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Failed to refresh auth session. Status: {StatusCode} Body: {ResponseBody}",
                (int)response.StatusCode,
                string.IsNullOrWhiteSpace(responseBody) ? "<empty>" : responseBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh auth session.");
            return false;
        }
    }

    // ── Analytics Governance ─────────────────────────────────────────────

    public async Task<Models.Analytics.AnalyticsGovernanceSettingsModel> GetAnalyticsGovernanceSettingsAsync() =>
        await GetSettingsAsync(_api.GetAnalyticsGovernanceSettingsAsync, () => new Models.Analytics.AnalyticsGovernanceSettingsModel());

    public Task<InstanceCommandResponseModel> UpdateAnalyticsGovernanceSettingsAsync(Models.Analytics.AnalyticsGovernanceSettingsModel settings) =>
        SendCommandAsync(ct => _api.UpdateAnalyticsGovernanceSettingsAsync(settings, ct));

    // ── Footer Governance ────────────────────────────────────────────────

    public async Task<FooterGovernanceSettingsModel> GetFooterGovernanceSettingsAsync() =>
        await GetSettingsAsync(_api.GetFooterGovernanceSettingsAsync, () => new FooterGovernanceSettingsModel());

    public Task<InstanceCommandResponseModel> UpdateFooterGovernanceSettingsAsync(FooterGovernanceSettingsModel settings) =>
        SendCommandAsync(ct => _api.UpdateFooterGovernanceSettingsAsync(settings, ct));

    // ── Shared Helpers ───────────────────────────────────────────────────

    private HttpClient CreateBffSelfClient()
    {
        var client = _httpClientFactory.CreateClient("BffSelfClient");
        client.BaseAddress = new Uri(_navigation.BaseUri);
        return client;
    }

    private async Task<T> GetSettingsAsync<T>(
        Func<CancellationToken, Task<IApiResponse<T>>> apiCall,
        Func<T> defaultValueFactory) where T : class
    {
        try
        {
            var response = await apiCall(CancellationToken.None);
            return response.IsSuccessStatusCode ? response.Content ?? defaultValueFactory() : defaultValueFactory();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch {Type}.", typeof(T).Name);
            return defaultValueFactory();
        }
    }

    private async Task<InstanceCommandResponseModel> SendCommandAsync(
        Func<CancellationToken, Task<IApiResponse<InstanceCommandResponseModel>>> apiCall)
    {
        try
        {
            var response = await apiCall(CancellationToken.None);
            return await MapCommandResponseAsync(response);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to send command.");
            return MapCommandApiException(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send command.");
            return FailedCommandResponse(ex.Message);
        }
    }

    private static InstanceCommandResponseModel MapCommandApiException(ApiException exception)
    {
        var result = new InstanceCommandResponseModel
        {
            Success = false,
            StatusCode = (int)exception.StatusCode,
            Message = $"Failed with status {(int)exception.StatusCode}."
        };

        if (string.IsNullOrWhiteSpace(exception.Content))
        {
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(exception.Content);
            var root = doc.RootElement;

            if (root.TryGetProperty("success", out var successProp))
            {
                result.Success = successProp.GetBoolean();
                result.Message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? string.Empty : string.Empty;

                if (root.TryGetProperty("id", out var idProp) && idProp.TryGetGuid(out var id))
                {
                    result.Id = id;
                }

                if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Array)
                {
                    result.Errors = errorsProp.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString()!)
                        .ToList();
                }
            }
            else if (root.TryGetProperty("title", out var titleProp))
            {
                result.Message = titleProp.GetString() ?? "Validation failed.";

                if (root.TryGetProperty("errors", out var pdErrors) && pdErrors.ValueKind == JsonValueKind.Object)
                {
                    var errors = new List<string>();
                    foreach (var field in pdErrors.EnumerateObject())
                    {
                        if (field.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var msg in field.Value.EnumerateArray())
                            {
                                errors.Add(msg.GetString() ?? field.Name);
                            }
                        }
                    }

                    result.Errors = errors;
                }
            }
        }
        catch (JsonException)
        {
            // Keep the default status-derived values when the error body is not JSON.
        }

        return result;
    }

    private async Task<PolicyPackageDownloadModel?> DownloadFileAsync(
        string path,
        string fallbackFileName,
        string fallbackContentType)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BffClient");
            var response = await client.GetAsync(path);
            response.EnsureSuccessStatusCode();

            var fileBytes = await response.Content.ReadAsByteArrayAsync();
            if (fileBytes.Length == 0)
            {
                return null;
            }

            return new PolicyPackageDownloadModel
            {
                FileBytes = fileBytes,
                FileName = GetDownloadFileName(response, fallbackFileName),
                ContentType = response.Content.Headers.ContentType?.MediaType ?? fallbackContentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download {Path}.", path);
            return null;
        }
    }

    private static string GetDownloadFileName(HttpResponseMessage response, string fallbackFileName)
    {
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName;
        return string.IsNullOrWhiteSpace(fileName)
            ? fallbackFileName
            : fileName.Trim('"');
    }

    private static async Task<InstanceCommandResponseModel> MapCommandResponseAsync(IApiResponse response)
    {
        var result = new InstanceCommandResponseModel
        {
            Success = response.IsSuccessStatusCode,
            StatusCode = (int)response.StatusCode,
            Message = response.IsSuccessStatusCode ? "OK" : $"Failed with status {(int)response.StatusCode}."
        };

        if (response.Error?.Content is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(response.Error.Content);
                var root = doc.RootElement;

                if (root.TryGetProperty("success", out var successProp))
                {
                    result.Success = successProp.GetBoolean();
                    result.Message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? "" : "";
                    result.Id = root.TryGetProperty("id", out var idProp) && idProp.TryGetGuid(out var id) ? id : Guid.Empty;

                    if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Array)
                    {
                        result.Errors = errorsProp.EnumerateArray()
                            .Where(e => e.ValueKind == JsonValueKind.String)
                            .Select(e => e.GetString()!)
                            .ToList();
                    }
                }
                else if (root.TryGetProperty("title", out var titleProp))
                {
                    result.Message = titleProp.GetString() ?? "Validation failed.";

                    if (root.TryGetProperty("errors", out var pdErrors) && pdErrors.ValueKind == JsonValueKind.Object)
                    {
                        var errors = new List<string>();
                        foreach (var field in pdErrors.EnumerateObject())
                        {
                            if (field.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var msg in field.Value.EnumerateArray())
                                {
                                    errors.Add(msg.GetString() ?? field.Name);
                                }
                            }
                        }

                        result.Errors = errors;
                    }
                }
            }
            catch
            {
                // Keep the default values
            }
        }
        else if (response.IsSuccessStatusCode && response is IApiResponse<InstanceCommandResponseModel> typed)
        {
            return typed.Content ?? result;
        }

        return result;
    }

    private static InstanceCommandResponseModel FailedCommandResponse(string error) =>
        new()
        {
            Success = false,
            Message = "Request failed.",
            Errors = [error]
        };
}

// ── Onboarding Models ────────────────────────────────────────────────────

public class InstanceOnboardingStatusModel
{
    public bool IsCompleted { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsCurrentUserInstanceAdmin { get; set; }
    public string? SelectedDeploymentMode { get; set; }
    public bool IsSetupModeActive { get; set; }
    public bool SetupSecretFromEnvironment { get; set; }
    public bool SetupTimedOut { get; set; }
    public DateTime? InstanceStartedAt { get; set; }
    public string SetupSecretState { get; set; } = "Unavailable";
    public string SetupSecretGuidance { get; set; } = "Setup access is not currently available.";
}

public class SystemOnboardingStatusModel
{
    public bool RequiresOnboarding { get; set; }
    public string DeploymentMode { get; set; } = "SingleTenant";
}

public class OnboardingCompletionModel
{
    public string DeploymentMode { get; set; } = "SingleTenant";
    public SelfHostOnboardingProfileModel SiteProfile { get; set; } = new();
    public string? InstanceName { get; set; }
}

public class SelfHostOnboardingProfileModel
{
    [Required(ErrorMessage = "Site name is required.")]
    [StringLength(200, ErrorMessage = "Site name must be 200 characters or fewer.")]
    public string SiteName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Support email must be a valid email address.")]
    public string? SupportEmail { get; set; }

    [Url(ErrorMessage = "Canonical URL must be a valid URL.")]
    public string? CanonicalUrl { get; set; }

    [Required]
    [StringLength(20)]
    public string Locale { get; set; } = "en";

    [Required]
    [StringLength(100)]
    public string TimeZone { get; set; } = "UTC";

    [StringLength(500, ErrorMessage = "Purpose must be 500 characters or fewer.")]
    public string? Purpose { get; set; }
}

public class OnboardingPreflightModel
{
    public string DeploymentMode { get; set; } = "SingleTenant";
    public bool IsReadyToLaunch { get; set; }
    public List<OnboardingPreflightCheckModel> BlockingChecks { get; set; } = [];
    public List<OnboardingPreflightCheckModel> WarningChecks { get; set; } = [];
}

public class OnboardingPreflightCheckModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
}

// ── Governance Sub-Resource Models ───────────────────────────────────────

public class DeploymentModeModel
{
    public string Mode { get; set; } = "SingleTenant";
}

public class ModuleSettingsModel
{
    public bool EnableIslamicModule { get; set; } = true;
    public bool EnableTechModule { get; set; } = true;
}

public class EventPolicyModel
{
    public bool AllowUserSubmittedEvents { get; set; } = true;
    public bool AllowOrganizationSubmittedEvents { get; set; } = true;
    public bool AllowGroupSubmittedEvents { get; set; } = true;
    public bool EventCardClickOpensDetailPage { get; set; }
    public bool LockTenantEventCardClickBehavior { get; set; }
}

public class OrganizationPolicyModel
{
    public bool RequireOrganizationVerification { get; set; } = true;
    public bool AllowTenantToOmitVerification { get; set; }
    public bool AllowOrganizationSelfRegistration { get; set; } = true;
    public bool AllowGroupSelfRegistration { get; set; } = true;
}

public class BrandingSettingsModel
{
    public string DefaultBrandDisplayName { get; set; } = string.Empty;
    public string DefaultBrandLogoUrl { get; set; } = string.Empty;
    public string DefaultBrandFaviconUrl { get; set; } = string.Empty;
    public string DefaultBrandCustomCssUrl { get; set; } = string.Empty;
    public bool LockTenantBrandDisplayName { get; set; }
    public bool LockTenantBrandLogoUrl { get; set; }
    public bool LockTenantBrandFaviconUrl { get; set; }
    public bool LockTenantBrandCustomCssUrl { get; set; }
}

public class DomainSettingsModel
{
    public string InstanceBaseDomain { get; set; } = string.Empty;
    public bool AllowTenantCustomDomains { get; set; } = true;
    public bool LockTenantSubdomain { get; set; }
    public bool LockTenantCustomDomain { get; set; }
}

public class TenantDelegationModel
{
    public bool AllowTenantSelfServiceRegistration { get; set; }
    public bool AllowTenantWhiteLabeling { get; set; }
    public string DefaultPublicHomePage { get; set; } = "EventList";
    public bool LockTenantHomePagePreference { get; set; }
    public bool LockTenantSmtp { get; set; } = true;
    public bool LockTenantStorage { get; set; } = true;
    public bool LockTenantAnalytics { get; set; } = true;
    public bool DecentralizationEnabled { get; set; }
    public bool LockDecentralizationEnabled { get; set; }
    public string AuthorizationProvider { get; set; } = "local";
    public bool LockTenantAiAssistant { get; set; }
}

public class RenderPolicyModel
{
    public int RenderPolicyVersion { get; set; } = 1;
    public string RenderPolicyPreset { get; set; } = "AllInteractiveServer";
    public bool EnableAdvancedRenderPolicyOverrides { get; set; }
    public string GlobalRenderMode { get; set; } = "InteractiveServer";
    public bool GlobalPrerenderEnabled { get; set; }
    public string PublicSeoRenderMode { get; set; } = "InteractiveServer";
    public bool PublicSeoPrerenderEnabled { get; set; }
    public string OperationalRenderMode { get; set; } = "InteractiveServer";
    public bool OperationalPrerenderEnabled { get; set; }
    public string AdminRenderMode { get; set; } = "InteractiveServer";
    public bool AdminPrerenderEnabled { get; set; }
    public string OnboardingRenderMode { get; set; } = "InteractiveServer";
    public bool OnboardingPrerenderEnabled { get; set; }
    public bool DisallowInteractiveServerOnOnboarding { get; set; } = true;
    public bool AllowTenantRenderPolicyOverride { get; set; }
    public bool LockTenantPublicSeoRenderPolicy { get; set; }
    public bool LockTenantOperationalRenderPolicy { get; set; }
    public bool LockTenantAdminRenderPolicy { get; set; }
}

// ── Command Response Model ───────────────────────────────────────────────

public class InstanceCommandResponseModel
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}

public class PolicyPackageDownloadModel
{
    public byte[] FileBytes { get; set; } = [];
    public string FileName { get; set; } = "authorization-policy-package.zip";
    public string ContentType { get; set; } = "application/zip";
}

// ── Infrastructure Models ────────────────────────────────────────────────

public class InstanceStorageSettingsModel
{
    public string S3Endpoint { get; set; } = string.Empty;
    public string S3PublicEndpoint { get; set; } = string.Empty;
    public string S3BucketName { get; set; } = string.Empty;
    public string S3AccessKeyId { get; set; } = string.Empty;
    public string S3SecretAccessKey { get; set; } = string.Empty;
    public string S3Region { get; set; } = string.Empty;
    public bool S3ForcePathStyle { get; set; } = true;
    public int S3UploadUrlExpirationMinutes { get; set; } = 60;
}

public class StorageConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class InstanceSmtpSettingsModel
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Security { get; set; } = "StartTls";
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public bool SkipCertificateValidation { get; set; }
}

public class SmtpConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SetupSecretValidationResult
{
    public bool Valid { get; set; }
    public string? Error { get; set; }
}


public class SecretOwnershipModel
{
    public string Mode { get; set; } = "application-managed";
    public string Source { get; set; } = "application";
    public string Badge { get; set; } = "Managed by Application";
    public string Description { get; set; } = "Stored securely by ISLAMU Event and editable from Admin UI.";
    public bool Editable { get; set; } = true;
    public bool Configured { get; set; }
    public bool BootstrapAvailable { get; set; }
}

// ── Auth Provider Models ─────────────────────────────────────────────────

public class AuthProviderConfigurationModel
{
    public bool KeycloakEnabled { get; set; }
    public string KeycloakAuthority { get; set; } = string.Empty;
    public string KeycloakClientId { get; set; } = string.Empty;
    public string KeycloakClientSecret { get; set; } = string.Empty;
    public bool KeycloakDetectedFromEnvironment { get; set; }
    public bool AtprotoLoginEnabled { get; set; }
    public string AtprotoPublicUrl { get; set; } = string.Empty;
    public bool GoogleSsoEnabled { get; set; }
    public string GoogleClientId { get; set; } = string.Empty;
    public string GoogleClientSecret { get; set; } = string.Empty;
    public bool LockKeycloakEnabled { get; set; }
    public bool LockAtprotoLoginEnabled { get; set; }
    public bool LockGoogleSsoEnabled { get; set; }
}

public class AuthProviderConfiguredResult
{
    public bool Configured { get; set; }
}

public class AuthorizationProviderConfigurationModel
{
    public string Provider { get; set; } = "local";
    public string CerbosGrpcEndpoint { get; set; } = string.Empty;
    public string CerbosAdminEndpoint { get; set; } = string.Empty;
    public string? CerbosAdminUsername { get; set; }
    public string? CerbosAdminPassword { get; set; }
    public bool CerbosAdminUsernameConfigured { get; set; }
    public bool CerbosAdminPasswordConfigured { get; set; }
    public bool CerbosDetectedFromEnvironment { get; set; }
    public bool CerbosEndpointVerified { get; set; }
    public bool AuthorizationProviderConfigured { get; set; }
    public SecretOwnershipModel CerbosEndpointOwnership { get; set; } = new();
    public SecretOwnershipModel CerbosAdminCredentialsOwnership { get; set; } = new();
}

public class AuthorizationProviderConfiguredResult
{
    public bool Configured { get; set; }
}

// ── Footer Governance Model ──────────────────────────────────────────────

public class FooterGovernanceSettingsModel
{
    public bool LockTenantTemplate { get; set; }
    public bool LockTenantLinkGroups { get; set; }
    public bool LockTenantSocialLinks { get; set; }
    public bool LockTenantDescription { get; set; }
    public bool LockTenantCopyright { get; set; }
}
