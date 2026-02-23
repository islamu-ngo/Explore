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

    public async Task<InstanceOnboardingStatusModel?> GetStatusAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BffClient");
            return await client.GetFromJsonAsync<InstanceOnboardingStatusModel>("api/InstanceOnboarding/status");
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
            var result = await client.GetFromJsonAsync<InstanceGovernanceSettingsModel>("api/InstanceOnboarding/settings");
            return result ?? new InstanceGovernanceSettingsModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch instance governance settings.");
            return new InstanceGovernanceSettingsModel();
        }
    }

    public async Task<SetupSecretValidationResult> ValidateSecretAsync(string secret)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BffClient");
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

    public async Task<InstanceCommandResponseModel> CompleteAsync(InstanceGovernanceSettingsModel settings)
    {
        return await SendAsync(HttpMethod.Post, "api/InstanceOnboarding/complete", settings);
    }

    public async Task<InstanceCommandResponseModel> UpdateSettingsAsync(InstanceGovernanceSettingsModel settings)
    {
        return await SendAsync(HttpMethod.Put, "api/InstanceOnboarding/settings", settings);
    }

    public async Task<InstanceStorageSettingsModel> GetStorageSettingsAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BffClient");
            var result = await client.GetFromJsonAsync<InstanceStorageSettingsModel>("api/InstanceOnboarding/storage-settings");
            return result ?? new InstanceStorageSettingsModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch instance storage settings.");
            return new InstanceStorageSettingsModel();
        }
    }

    public async Task<InstanceCommandResponseModel> UpdateStorageSettingsAsync(InstanceStorageSettingsModel settings)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BffClient");
            using var request = new HttpRequestMessage(HttpMethod.Put, "api/InstanceOnboarding/storage-settings")
            {
                Content = JsonContent.Create(settings)
            };

            await AddSetupSecretHeaderAsync(request);

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
                    ? "Storage settings updated successfully."
                    : $"Operation failed with status {(int)response.StatusCode}."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update instance storage settings.");
            return new InstanceCommandResponseModel
            {
                Success = false,
                Message = "Request failed.",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<StorageConnectionTestResult> TestStorageConnectionAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BffClient");
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/InstanceOnboarding/test-storage");
            await AddSetupSecretHeaderAsync(request);

            var response = await client.SendAsync(request);
            var result = await response.Content.ReadFromJsonAsync<StorageConnectionTestResult>();
            return result ?? new StorageConnectionTestResult { Success = false, Message = "Empty response." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test storage connection.");
            return new StorageConnectionTestResult { Success = false, Message = ex.Message };
        }
    }

    public async Task<InstanceSmtpSettingsModel> GetSmtpSettingsAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BffClient");
            var result = await client.GetFromJsonAsync<InstanceSmtpSettingsModel>("api/InstanceOnboarding/smtp-settings");
            return result ?? new InstanceSmtpSettingsModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch instance SMTP settings.");
            return new InstanceSmtpSettingsModel();
        }
    }

    public async Task<InstanceCommandResponseModel> UpdateSmtpSettingsAsync(InstanceSmtpSettingsModel settings)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BffClient");
            using var request = new HttpRequestMessage(HttpMethod.Put, "api/InstanceOnboarding/smtp-settings")
            {
                Content = JsonContent.Create(settings)
            };

            await AddSetupSecretHeaderAsync(request);

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
                    ? "SMTP settings updated successfully."
                    : $"Operation failed with status {(int)response.StatusCode}."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update instance SMTP settings.");
            return new InstanceCommandResponseModel
            {
                Success = false,
                Message = "Request failed.",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<SmtpConnectionTestResult> TestSmtpConnectionAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BffClient");
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/InstanceOnboarding/test-smtp");
            await AddSetupSecretHeaderAsync(request);

            var response = await client.SendAsync(request);
            var result = await response.Content.ReadFromJsonAsync<SmtpConnectionTestResult>();
            return result ?? new SmtpConnectionTestResult { Success = false, Message = "Empty response." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test SMTP connection.");
            return new SmtpConnectionTestResult { Success = false, Message = ex.Message };
        }
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

            await AddSetupSecretHeaderAsync(request);

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
            || pathAndQuery.Contains("/api/InstanceOnboarding/settings", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/storage-settings", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/test-storage", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/smtp-settings", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/test-smtp", StringComparison.OrdinalIgnoreCase);
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
