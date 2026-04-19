// ABOUTME: Client service for instance onboarding and governance settings via sub-resource endpoints.
// ABOUTME: Powers first-run wizard, instance admin settings, and infrastructure config from Blazor pages.

using System.Net.Http.Json;
using System.Text.Json;
using Explore.Blazor.Client.Services.Http;
using Microsoft.JSInterop;

namespace Explore.Blazor.Client.Services;

public interface IInstanceOnboardingService
{
    // Onboarding
    Task<InstanceOnboardingStatusModel?> GetStatusAsync();
    Task<SetupSecretValidationResult> ValidateSecretAsync(string secret);
    Task<InstanceCommandResponseModel> CompleteAsync(OnboardingCompletionModel completion);

    // Governance sub-resource reads
    Task<DeploymentModeModel> GetDeploymentModeAsync();
    Task<ModuleSettingsModel> GetModuleSettingsAsync();
    Task<EventPolicyModel> GetEventPolicyAsync();
    Task<OrganizationPolicyModel> GetOrganizationPolicyAsync();
    Task<BrandingSettingsModel> GetBrandingSettingsAsync();
    Task<DomainSettingsModel> GetDomainSettingsAsync();
    Task<TenantDelegationModel> GetTenantDelegationAsync();
    Task<RenderPolicyModel> GetRenderPolicyAsync();

    // Governance sub-resource writes
    Task<InstanceCommandResponseModel> UpdateDeploymentModeAsync(string deploymentMode);
    Task<InstanceCommandResponseModel> UpdateModuleSettingsAsync(ModuleSettingsModel settings);
    Task<InstanceCommandResponseModel> UpdateEventPolicyAsync(EventPolicyModel settings);
    Task<InstanceCommandResponseModel> UpdateOrganizationPolicyAsync(OrganizationPolicyModel settings);
    Task<InstanceCommandResponseModel> UpdateBrandingSettingsAsync(BrandingSettingsModel settings);
    Task<InstanceCommandResponseModel> UpdateDomainSettingsAsync(DomainSettingsModel settings);
    Task<InstanceCommandResponseModel> UpdateTenantDelegationAsync(TenantDelegationModel settings);
    Task<InstanceCommandResponseModel> UpdateRenderPolicyAsync(RenderPolicyModel settings);

    // Infrastructure settings
    Task<InstanceStorageSettingsModel> GetStorageSettingsAsync();
    Task<InstanceCommandResponseModel> UpdateStorageSettingsAsync(InstanceStorageSettingsModel settings);
    Task<StorageConnectionTestResult> TestStorageConnectionAsync();
    Task<InstanceSmtpSettingsModel> GetSmtpSettingsAsync();
    Task<InstanceCommandResponseModel> UpdateSmtpSettingsAsync(InstanceSmtpSettingsModel settings);
    Task<SmtpConnectionTestResult> TestSmtpConnectionAsync();
    Task<int> GetActiveTenantCountAsync();

    // Auth provider configuration
    Task<AuthProviderConfigurationModel> GetAuthProviderConfigurationAsync();
    Task<InstanceCommandResponseModel> SaveAuthProviderConfigurationAsync(AuthProviderConfigurationModel config);
    Task<InstanceCommandResponseModel> UpdateAuthProviderConfigurationAsAdminAsync(AuthProviderConfigurationModel config);
    Task<bool> IsAuthProviderConfiguredAsync();
    Task RefreshAuthSchemesAsync();

    // Authorization provider configuration
    Task<AuthorizationProviderConfigurationModel> GetAuthorizationProviderConfigurationAsync();
    Task<InstanceCommandResponseModel> SaveAuthorizationProviderConfigurationAsync(AuthorizationProviderConfigurationModel config);
    Task<InstanceCommandResponseModel> VerifyCerbosEndpointAsync(string grpcEndpoint);
    Task<bool> IsAuthorizationProviderConfiguredAsync();

    // Analytics governance
    Task<Models.Analytics.AnalyticsGovernanceSettingsModel> GetAnalyticsGovernanceSettingsAsync();
    Task<InstanceCommandResponseModel> UpdateAnalyticsGovernanceSettingsAsync(Models.Analytics.AnalyticsGovernanceSettingsModel settings);

    // Footer governance
    Task<FooterGovernanceSettingsModel> GetFooterGovernanceSettingsAsync();
    Task<InstanceCommandResponseModel> UpdateFooterGovernanceSettingsAsync(FooterGovernanceSettingsModel settings);
}

public class InstanceOnboardingService : IInstanceOnboardingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<InstanceOnboardingService> _logger;
    private readonly BffClient? _bffClient;

    public InstanceOnboardingService(
        IHttpClientFactory httpClientFactory,
        IJSRuntime jsRuntime,
        ILogger<InstanceOnboardingService> logger,
        BffClient? bffClient = null)
    {
        _httpClientFactory = httpClientFactory;
        _jsRuntime = jsRuntime;
        _logger = logger;
        _bffClient = bffClient;
    }

    // ── Onboarding ───────────────────────────────────────────────────────

    public async Task<InstanceOnboardingStatusModel?> GetStatusAsync() =>
        await GetAsync<InstanceOnboardingStatusModel>("api/InstanceOnboarding/status");

    public async Task<SetupSecretValidationResult> ValidateSecretAsync(string secret)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync("api/InstanceOnboarding/validate-secret", new { secret });

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Setup secret validation rate-limited (429).");
                return new SetupSecretValidationResult
                {
                    Valid = false,
                    Error = "Too many attempts. Please wait a moment and try again."
                };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                // Setup already completed — API returns { valid: false, error: "Setup already completed." }
                var goneResult = await response.Content.ReadFromJsonAsync<SetupSecretValidationResult>();
                return goneResult ?? new SetupSecretValidationResult { Valid = false, Error = "Setup already completed." };
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Setup secret validation failed with HTTP {StatusCode}: {ReasonPhrase}.",
                    (int)response.StatusCode,
                    response.ReasonPhrase);
                return new SetupSecretValidationResult
                {
                    Valid = false,
                    Error = $"Validation unavailable (HTTP {(int)response.StatusCode}). Please try again."
                };
            }

            var result = await response.Content.ReadFromJsonAsync<SetupSecretValidationResult>();
            return result ?? new SetupSecretValidationResult { Valid = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate setup secret.");
            return new SetupSecretValidationResult { Valid = false };
        }
    }

    public Task<InstanceCommandResponseModel> CompleteAsync(OnboardingCompletionModel completion) =>
        SendCommandAsync(HttpMethod.Post, "api/InstanceOnboarding/complete", completion);

    // ── Governance Sub-Resource Reads ─────────────────────────────────────

    public async Task<DeploymentModeModel> GetDeploymentModeAsync() =>
        await GetAsync<DeploymentModeModel>("api/instance/settings/deployment-mode")
        ?? new DeploymentModeModel();

    public async Task<ModuleSettingsModel> GetModuleSettingsAsync() =>
        await GetAsync<ModuleSettingsModel>("api/instance/settings/modules")
        ?? new ModuleSettingsModel();

    public async Task<EventPolicyModel> GetEventPolicyAsync() =>
        await GetAsync<EventPolicyModel>("api/instance/settings/events")
        ?? new EventPolicyModel();

    public async Task<OrganizationPolicyModel> GetOrganizationPolicyAsync() =>
        await GetAsync<OrganizationPolicyModel>("api/instance/settings/organizations")
        ?? new OrganizationPolicyModel();

    public async Task<BrandingSettingsModel> GetBrandingSettingsAsync() =>
        await GetAsync<BrandingSettingsModel>("api/instance/settings/branding")
        ?? new BrandingSettingsModel();

    public async Task<DomainSettingsModel> GetDomainSettingsAsync() =>
        await GetAsync<DomainSettingsModel>("api/instance/settings/domains")
        ?? new DomainSettingsModel();

    public async Task<TenantDelegationModel> GetTenantDelegationAsync() =>
        await GetAsync<TenantDelegationModel>("api/instance/settings/tenant-delegation")
        ?? new TenantDelegationModel();

    public async Task<RenderPolicyModel> GetRenderPolicyAsync() =>
        await GetAsync<RenderPolicyModel>("api/instance/settings/render-policy")
        ?? new RenderPolicyModel();

    // ── Governance Sub-Resource Writes ────────────────────────────────────

    public Task<InstanceCommandResponseModel> UpdateDeploymentModeAsync(string deploymentMode) =>
        SendCommandAsync(HttpMethod.Post, "api/instance/settings/deployment-mode", new { DeploymentMode = deploymentMode });

    public Task<InstanceCommandResponseModel> UpdateModuleSettingsAsync(ModuleSettingsModel settings) =>
        SendCommandAsync(HttpMethod.Put, "api/instance/settings/modules", settings);

    public Task<InstanceCommandResponseModel> UpdateEventPolicyAsync(EventPolicyModel settings) =>
        SendCommandAsync(HttpMethod.Put, "api/instance/settings/events", settings);

    public Task<InstanceCommandResponseModel> UpdateOrganizationPolicyAsync(OrganizationPolicyModel settings) =>
        SendCommandAsync(HttpMethod.Put, "api/instance/settings/organizations", settings);

    public Task<InstanceCommandResponseModel> UpdateBrandingSettingsAsync(BrandingSettingsModel settings) =>
        SendCommandAsync(HttpMethod.Put, "api/instance/settings/branding", settings);

    public Task<InstanceCommandResponseModel> UpdateDomainSettingsAsync(DomainSettingsModel settings) =>
        SendCommandAsync(HttpMethod.Put, "api/instance/settings/domains", settings);

    public Task<InstanceCommandResponseModel> UpdateTenantDelegationAsync(TenantDelegationModel settings) =>
        SendCommandAsync(HttpMethod.Put, "api/instance/settings/tenant-delegation", settings);

    public Task<InstanceCommandResponseModel> UpdateRenderPolicyAsync(RenderPolicyModel settings) =>
        SendCommandAsync(HttpMethod.Put, "api/instance/settings/render-policy", settings);

    // ── Infrastructure Settings ──────────────────────────────────────────

    public async Task<InstanceStorageSettingsModel> GetStorageSettingsAsync() =>
        await GetAsync<InstanceStorageSettingsModel>("api/instance/settings/storage")
        ?? new InstanceStorageSettingsModel();

    public Task<InstanceCommandResponseModel> UpdateStorageSettingsAsync(InstanceStorageSettingsModel settings) =>
        SendCommandAsync(HttpMethod.Put, "api/instance/settings/storage", settings);

    public Task<StorageConnectionTestResult> TestStorageConnectionAsync() =>
        SendTestAsync<StorageConnectionTestResult>("api/instance/settings/storage/test");

    public async Task<InstanceSmtpSettingsModel> GetSmtpSettingsAsync() =>
        await GetAsync<InstanceSmtpSettingsModel>("api/instance/settings/smtp")
        ?? new InstanceSmtpSettingsModel();

    public Task<InstanceCommandResponseModel> UpdateSmtpSettingsAsync(InstanceSmtpSettingsModel settings) =>
        SendCommandAsync(HttpMethod.Put, "api/instance/settings/smtp", settings);

    public Task<SmtpConnectionTestResult> TestSmtpConnectionAsync() =>
        SendTestAsync<SmtpConnectionTestResult>("api/instance/settings/smtp/test");

    public async Task<int> GetActiveTenantCountAsync()
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync("api/Tenant/count");
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
        await GetAsync<AuthProviderConfigurationModel>("api/instance/settings/auth-provider")
        ?? new AuthProviderConfigurationModel();

    public Task<InstanceCommandResponseModel> SaveAuthProviderConfigurationAsync(AuthProviderConfigurationModel config) =>
        SendCommandAsync(HttpMethod.Put, "api/InstanceOnboarding/auth-provider-configuration", config);

    public Task<InstanceCommandResponseModel> UpdateAuthProviderConfigurationAsAdminAsync(AuthProviderConfigurationModel config) =>
        SendCommandAsync(HttpMethod.Put, "api/instance/settings/auth-provider", config);

    public async Task<bool> IsAuthProviderConfiguredAsync()
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync("api/instance/settings/auth-provider/status");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AuthProviderConfiguredResult>();
            return result?.Configured ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check auth provider configuration status.");
            return false;
        }
    }

    public async Task<AuthorizationProviderConfigurationModel> GetAuthorizationProviderConfigurationAsync() =>
        await GetAsync<AuthorizationProviderConfigurationModel>("api/InstanceOnboarding/authz-provider-configuration/internal")
        ?? new AuthorizationProviderConfigurationModel();

    public Task<InstanceCommandResponseModel> SaveAuthorizationProviderConfigurationAsync(AuthorizationProviderConfigurationModel config) =>
        SendCommandAsync(HttpMethod.Put, "api/InstanceOnboarding/authz-provider-configuration", config);

    public Task<InstanceCommandResponseModel> VerifyCerbosEndpointAsync(string grpcEndpoint) =>
        SendCommandAsync(HttpMethod.Post, "api/InstanceOnboarding/authz-provider-configuration/verify", new { GrpcEndpoint = grpcEndpoint });

    public async Task<bool> IsAuthorizationProviderConfiguredAsync()
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync("api/instance/settings/authz-provider/status");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AuthorizationProviderConfiguredResult>();
            return result?.Configured ?? false;
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
            using var response = _bffClient is not null
                ? await _bffClient.PostAsync("/bff/auth/refresh-schemes")
                : await _httpClientFactory.CreateClient("BffSelfClient").PostAsync("/bff/auth/refresh-schemes", null);
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

    // ── Analytics Governance ─────────────────────────────────────────────

    public async Task<Models.Analytics.AnalyticsGovernanceSettingsModel> GetAnalyticsGovernanceSettingsAsync() =>
        await GetAsync<Models.Analytics.AnalyticsGovernanceSettingsModel>("api/instance/settings/analytics-governance")
        ?? new Models.Analytics.AnalyticsGovernanceSettingsModel();

    public Task<InstanceCommandResponseModel> UpdateAnalyticsGovernanceSettingsAsync(Models.Analytics.AnalyticsGovernanceSettingsModel settings) =>
        SendCommandAsync(HttpMethod.Put, "api/instance/settings/analytics-governance", settings);

    // ── Footer Governance ────────────────────────────────────────────────

    public async Task<FooterGovernanceSettingsModel> GetFooterGovernanceSettingsAsync() =>
        await GetAsync<FooterGovernanceSettingsModel>("api/instance/settings/footer-governance")
        ?? new FooterGovernanceSettingsModel();

    public Task<InstanceCommandResponseModel> UpdateFooterGovernanceSettingsAsync(FooterGovernanceSettingsModel settings) =>
        SendCommandAsync(HttpMethod.Put, "api/instance/settings/footer-governance", settings);

    // ── Shared Helpers ───────────────────────────────────────────────────

    private HttpClient CreateClient() => _httpClientFactory.CreateClient("BffClient");

    private async Task<T?> GetAsync<T>(string path) where T : class
    {
        try
        {
            return await CreateClient().GetFromJsonAsync<T>(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch {Path}.", path);
            return null;
        }
    }

    private async Task<InstanceCommandResponseModel> SendCommandAsync<T>(
        HttpMethod method, string path, T body)
    {
        try
        {
            var client = CreateClient();
            using var request = new HttpRequestMessage(method, path)
            {
                Content = JsonContent.Create(body)
            };

            var response = await client.SendAsync(request);
            return await ReadCommandResponseAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call endpoint {Path}.", path);
            return FailedCommandResponse(ex.Message);
        }
    }

    private async Task<TResult> SendTestAsync<TResult>(string path)
        where TResult : class, new()
    {
        try
        {
            var client = CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, path);

            var response = await client.SendAsync(request);
            var result = await response.Content.ReadFromJsonAsync<TResult>();
            return result ?? Activator.CreateInstance<TResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test connection via {Path}.", path);
            return Activator.CreateInstance<TResult>();
        }
    }

    /// <summary>
    /// Reads a command response, handling both BaseCommandResponse{Guid} and ASP.NET ProblemDetails formats.
    /// </summary>
    private static async Task<InstanceCommandResponseModel> ReadCommandResponseAsync(HttpResponseMessage response)
    {
        try
        {
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                return new InstanceCommandResponseModel
                {
                    Success = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "OK" : $"Failed with status {(int)response.StatusCode}."
                };
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // BaseCommandResponse<Guid> format: { success, id, message, errors: string[] }
            if (root.TryGetProperty("success", out var successProp))
            {
                var model = new InstanceCommandResponseModel
                {
                    Success = successProp.GetBoolean(),
                    Message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? "" : "",
                    Id = root.TryGetProperty("id", out var idProp) && idProp.TryGetGuid(out var id) ? id : Guid.Empty
                };

                if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Array)
                {
                    model.Errors = errorsProp.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString()!)
                        .ToList();
                }

                return model;
            }

            // ASP.NET ProblemDetails format: { title, status, errors: { field: [msgs] } }
            if (root.TryGetProperty("title", out var titleProp))
            {
                var errors = new List<string>();
                if (root.TryGetProperty("errors", out var pdErrors) && pdErrors.ValueKind == JsonValueKind.Object)
                {
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
                }

                return new InstanceCommandResponseModel
                {
                    Success = false,
                    Message = titleProp.GetString() ?? "Validation failed.",
                    Errors = errors
                };
            }

            return new InstanceCommandResponseModel
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "OK" : $"Failed with status {(int)response.StatusCode}."
            };
        }
        catch
        {
            return new InstanceCommandResponseModel
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "OK" : $"Failed with status {(int)response.StatusCode}."
            };
        }
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
}

public class OnboardingCompletionModel
{
    public string DeploymentMode { get; set; } = "SingleTenant";
    public string? InstanceName { get; set; }
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
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
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
    public bool CerbosDetectedFromEnvironment { get; set; }
    public bool CerbosEndpointVerified { get; set; }
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
