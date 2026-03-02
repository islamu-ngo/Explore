// ABOUTME: Client service for instance onboarding status, governance, and auth provider configuration endpoints.
// ABOUTME: Powers first-run startup gating, auth provider setup, and runtime instance settings updates from Blazor pages.
// ABOUTME: Powers first-run startup gating and runtime instance settings updates from Blazor pages.

using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace Explore.Blazor.Client.Services;

public interface IInstanceOnboardingService
{
    Task<InstanceOnboardingStatusModel?> GetStatusAsync();
    Task<SetupSecretValidationResult> ValidateSecretAsync(string secret);
    Task<InstanceGovernanceSettingsModel> GetSettingsAsync();
    Task<InstanceCommandResponseModel> CompleteAsync(InstanceGovernanceSettingsModel settings);
    Task<InstanceCommandResponseModel> UpdateSettingsAsync(InstanceGovernanceSettingsModel settings);
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
    Task<bool> IsAuthProviderConfiguredAsync();
    Task RefreshAuthSchemesAsync();
}

public class InstanceOnboardingService : IInstanceOnboardingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<InstanceOnboardingService> _logger;

    public InstanceOnboardingService(
        IHttpClientFactory httpClientFactory,
        IJSRuntime jsRuntime,
        ILogger<InstanceOnboardingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    // ── Read operations ──────────────────────────────────────────────────

    public async Task<InstanceOnboardingStatusModel?> GetStatusAsync() =>
        await GetAsync<InstanceOnboardingStatusModel>("api/InstanceOnboarding/status");

    public async Task<InstanceGovernanceSettingsModel> GetSettingsAsync() =>
        await GetAsync<InstanceGovernanceSettingsModel>("api/InstanceOnboarding/settings")
        ?? new InstanceGovernanceSettingsModel();

    public async Task<InstanceStorageSettingsModel> GetStorageSettingsAsync() =>
        await GetAsync<InstanceStorageSettingsModel>("api/InstanceOnboarding/storage-settings")
        ?? new InstanceStorageSettingsModel();

    public async Task<InstanceSmtpSettingsModel> GetSmtpSettingsAsync() =>
        await GetAsync<InstanceSmtpSettingsModel>("api/InstanceOnboarding/smtp-settings")
        ?? new InstanceSmtpSettingsModel();

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

    // ── Validation ───────────────────────────────────────────────────────

    public async Task<SetupSecretValidationResult> ValidateSecretAsync(string secret)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync("api/InstanceOnboarding/validate-secret", new { secret });
            var result = await response.Content.ReadFromJsonAsync<SetupSecretValidationResult>();
            return result ?? new SetupSecretValidationResult { Valid = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate setup secret.");
            return new SetupSecretValidationResult { Valid = false };
        }
    }

    // ── Write operations ─────────────────────────────────────────────────

    public Task<InstanceCommandResponseModel> CompleteAsync(InstanceGovernanceSettingsModel settings) =>
        SendCommandAsync(HttpMethod.Post, "api/InstanceOnboarding/complete", settings);

    public Task<InstanceCommandResponseModel> UpdateSettingsAsync(InstanceGovernanceSettingsModel settings) =>
        SendCommandAsync(HttpMethod.Put, "api/InstanceOnboarding/settings", settings);

    public Task<InstanceCommandResponseModel> UpdateStorageSettingsAsync(InstanceStorageSettingsModel settings) =>
        SendCommandAsync(HttpMethod.Put, "api/InstanceOnboarding/storage-settings", settings);

    public Task<InstanceCommandResponseModel> UpdateSmtpSettingsAsync(InstanceSmtpSettingsModel settings) =>
        SendCommandAsync(HttpMethod.Put, "api/InstanceOnboarding/smtp-settings", settings);

    // ── Test operations ──────────────────────────────────────────────────

    public Task<StorageConnectionTestResult> TestStorageConnectionAsync() =>
        SendTestAsync<StorageConnectionTestResult>("api/InstanceOnboarding/test-storage");

    public Task<SmtpConnectionTestResult> TestSmtpConnectionAsync() =>
        SendTestAsync<SmtpConnectionTestResult>("api/InstanceOnboarding/test-smtp");

    // ── Auth provider configuration ──────────────────────────────────

    public async Task<AuthProviderConfigurationModel> GetAuthProviderConfigurationAsync() =>
        await GetAsync<AuthProviderConfigurationModel>("api/InstanceOnboarding/auth-provider-configuration")
        ?? new AuthProviderConfigurationModel();

    public Task<InstanceCommandResponseModel> SaveAuthProviderConfigurationAsync(AuthProviderConfigurationModel config) =>
        SendCommandAsync(HttpMethod.Put, "api/InstanceOnboarding/auth-provider-configuration", config);

    public async Task<bool> IsAuthProviderConfiguredAsync()
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync("api/InstanceOnboarding/auth-provider-configured");
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

    public async Task RefreshAuthSchemesAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BffSelfClient");
            var response = await client.PostAsync("/bff/auth/refresh-schemes", null);
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
    // ── Shared helpers ───────────────────────────────────────────────────

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

            await AddSetupSecretHeaderAsync(request);
            var response = await client.SendAsync(request);

            return await ReadCommandResponseAsync(response, path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call instance onboarding endpoint {Path}.", path);
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
            await AddSetupSecretHeaderAsync(request);

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

    private static async Task<InstanceCommandResponseModel> ReadCommandResponseAsync(
        HttpResponseMessage response, string path)
    {
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

    private static InstanceCommandResponseModel FailedCommandResponse(string error) =>
        new()
        {
            Success = false,
            Message = "Request failed.",
            Errors = [error]
        };

    private async Task AddSetupSecretHeaderAsync(HttpRequestMessage request)
    {
        if (request.Headers.Contains("X-Setup-Secret"))
        {
            return;
        }

        var requestPath = GetRequestPath(request.RequestUri);
        if (!RequiresSetupSecret(requestPath))
        {
            return;
        }

        try
        {
            var secret = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "setup-secret");
            if (!string.IsNullOrWhiteSpace(secret))
            {
                request.Headers.Add("X-Setup-Secret", secret.Trim());
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read setup-secret from sessionStorage for {Path}", requestPath);
        }
    }

    private static string GetRequestPath(Uri? requestUri)
    {
        if (requestUri is null)
        {
            return string.Empty;
        }

        var path = requestUri.IsAbsoluteUri ? requestUri.PathAndQuery : requestUri.OriginalString;
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        return path;
    }

    private static bool RequiresSetupSecret(string pathAndQuery)
    {
        return pathAndQuery.Contains("/api/InstanceOnboarding/complete", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/validate-secret", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/auth-provider-configuration", StringComparison.OrdinalIgnoreCase);
    }
}

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

public class InstanceGovernanceSettingsModel
{
    public string DeploymentMode { get; set; } = "SingleTenant";
    public bool AllowTenantSelfServiceRegistration { get; set; }
    public bool AllowTenantWhiteLabeling { get; set; }
    public string DefaultPublicHomePage { get; set; } = "EventList";
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
    public bool EnableIslamicModule { get; set; } = true;
    public bool EnableTechModule { get; set; } = true;
    public bool AllowUserSubmittedEvents { get; set; } = true;
    public bool AllowOrganizationSubmittedEvents { get; set; } = true;
    public bool AllowGroupSubmittedEvents { get; set; } = true;
    public bool AllowOrganizationSelfRegistration { get; set; } = true;
    public bool AllowGroupSelfRegistration { get; set; } = true;
    public bool EventCardClickOpensDetailPage { get; set; }
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
    public bool LockTenantEventCardClickBehavior { get; set; }
}

public class InstanceCommandResponseModel
{
    public bool Success { get; set; }
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}

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


public class AuthProviderConfigurationModel
{
    // Keycloak
    public bool KeycloakEnabled { get; set; }
    public string KeycloakAuthority { get; set; } = string.Empty;
    public string KeycloakClientId { get; set; } = string.Empty;
    public string KeycloakClientSecret { get; set; } = string.Empty;
    public bool KeycloakDetectedFromEnvironment { get; set; }

    // ATProto Login
    public bool AtprotoLoginEnabled { get; set; }
    public string AtprotoPublicUrl { get; set; } = string.Empty;

    // Google SSO
    public bool GoogleSsoEnabled { get; set; }
    public string GoogleClientId { get; set; } = string.Empty;
    public string GoogleClientSecret { get; set; } = string.Empty;

    // Lock flags (for multi-tenant override control)
    public bool LockKeycloakEnabled { get; set; }
    public bool LockAtprotoLoginEnabled { get; set; }
    public bool LockGoogleSsoEnabled { get; set; }
}

public class AuthProviderConfiguredResult
{
    public bool Configured { get; set; }
}
