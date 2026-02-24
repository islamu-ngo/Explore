// ABOUTME: Client service for instance onboarding status and governance settings endpoints.
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
            || pathAndQuery.Contains("/api/InstanceOnboarding/validate-secret", StringComparison.OrdinalIgnoreCase);
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
    public string RenderPolicyPreset { get; set; } = "SeoBalanced";
    public bool EnableAdvancedRenderPolicyOverrides { get; set; }
    public string GlobalRenderMode { get; set; } = "InteractiveAuto";
    public bool GlobalPrerenderEnabled { get; set; }
    public string PublicSeoRenderMode { get; set; } = "InteractiveAuto";
    public bool PublicSeoPrerenderEnabled { get; set; } = true;
    public string OperationalRenderMode { get; set; } = "InteractiveAuto";
    public bool OperationalPrerenderEnabled { get; set; }
    public string AdminRenderMode { get; set; } = "InteractiveAuto";
    public bool AdminPrerenderEnabled { get; set; }
    public string OnboardingRenderMode { get; set; } = "InteractiveAuto";
    public bool OnboardingPrerenderEnabled { get; set; }
    public bool DisallowInteractiveServerOnOnboarding { get; set; } = true;
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
