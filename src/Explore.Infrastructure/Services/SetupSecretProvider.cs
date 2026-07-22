// ABOUTME: Singleton service that manages the setup secret lifecycle for instance onboarding.
// ABOUTME: Reads setup/provisioning configuration, generates or disables setup-secret validation safely, and validates with timing-safe comparison.

using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Infrastructure.Services;

public class SetupSecretProvider : ISetupSecretProvider, IDisposable
{
    private readonly string _secret;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _bootstrapCheckSemaphore = new(1, 1);
    private bool _isLocked;
    private bool? _isBootstrapComplete;

    public bool IsSetupSecretRequired { get; }
    public bool IsTrustedManagedProvisioningConfigured { get; }
    public bool IsFromEnvironmentVariable { get; }
    public DateTime InstanceStartedAt { get; }

    public bool IsTimedOut => DateTime.UtcNow - InstanceStartedAt > TimeSpan.FromMinutes(60);

    public bool IsSetupModeActive
    {
        get
        {
            if (_isLocked)
                return false;

            if (_isBootstrapComplete.HasValue)
                return !_isBootstrapComplete.Value;

            // Not yet initialized — fail closed (setup mode inactive)
            return false;
        }
    }

    public SetupSecretProvider(IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        InstanceStartedAt = DateTime.UtcNow;
        IsTrustedManagedProvisioningConfigured = HasTrustedManagedProvisioningConfiguration(configuration);

        var requestedSetupSecretRequired = ReadBoolean(configuration["SETUP_SECRET_REQUIRED"], defaultValue: true);
        IsSetupSecretRequired = requestedSetupSecretRequired || !IsTrustedManagedProvisioningConfigured;

        if (!IsSetupSecretRequired)
        {
            _secret = string.Empty;
            IsFromEnvironmentVariable = false;
            return;
        }

        var envSecret = configuration["SETUP_SECRET"];
        if (!string.IsNullOrWhiteSpace(envSecret))
        {
            _secret = envSecret;
            IsFromEnvironmentVariable = true;
        }
        else
        {
            _secret = GenerateCryptoRandomSecret();
            IsFromEnvironmentVariable = false;
        }
    }

    public bool ValidateSecret(string? secret)
    {
        if (!IsSetupSecretRequired)
            return false;

        if (_isLocked)
            return false;

        if (IsTimedOut)
            return false;

        if (string.IsNullOrEmpty(secret))
            return false;

        var expected = Encoding.UTF8.GetBytes(_secret);
        var actual = Encoding.UTF8.GetBytes(secret);

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public void Lock()
    {
        _isLocked = true;
        _isBootstrapComplete = true;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isBootstrapComplete.HasValue)
            return;

        await _bootstrapCheckSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_isBootstrapComplete.HasValue)
                return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IInstanceBootstrapStateRepository>();
                var bootstrapState = await repository.GetCurrent(cancellationToken);
                _isBootstrapComplete = bootstrapState?.IsCompleted == true;
            }
            catch
            {
                _isBootstrapComplete = false;
            }
        }
        finally
        {
            _bootstrapCheckSemaphore.Release();
        }
    }

    public void Dispose()
    {
        _bootstrapCheckSemaphore.Dispose();
    }

    private static string GenerateCryptoRandomSecret()
    {
        // 48 bytes → 64 Base64 chars; after stripping +/=/  we still have ≥ 32 alphanumeric chars.
        var bytes = RandomNumberGenerator.GetBytes(48);
        var filtered = Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");
        return filtered[..32];
    }

    private static bool HasTrustedManagedProvisioningConfiguration(IConfiguration configuration)
    {
        if (!ReadBoolean(configuration["PROVISIONING_TRUSTED"], defaultValue: false))
            return false;

        if (!IsManagedProvisioningMode(configuration["PROVISIONING_MODE"]))
            return false;

        if (string.IsNullOrWhiteSpace(configuration["MANAGED_CLIENT_EXTERNAL_PROVIDER"]))
            return false;

        return !string.IsNullOrWhiteSpace(configuration["PHYSICAL_TENANCY_MODE"]);
    }

    private static bool IsManagedProvisioningMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return false;

        var normalized = mode.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Trim();

        return normalized.Equals("managedprovider", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("managedhosting", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("managed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ReadBoolean(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
