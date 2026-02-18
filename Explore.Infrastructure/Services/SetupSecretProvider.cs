// ABOUTME: Singleton service that manages the setup secret lifecycle for instance onboarding.
// ABOUTME: Reads SETUP_SECRET from env var or auto-generates a 32-char crypto-random token; validates with timing-safe comparison.

using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Infrastructure.Services;

public class SetupSecretProvider : ISetupSecretProvider
{
    private readonly string _secret;
    private readonly IServiceProvider _serviceProvider;
    private readonly object _bootstrapCheckLock = new();
    private bool _isLocked;
    private bool? _isBootstrapComplete;

    public bool IsFromEnvironmentVariable { get; }
    public DateTime InstanceStartedAt { get; }

    public bool IsTimedOut => DateTime.UtcNow - InstanceStartedAt > TimeSpan.FromMinutes(60);

    public bool IsSetupModeActive
    {
        get
        {
            if (_isLocked)
                return false;

            return !IsBootstrapComplete();
        }
    }

    public SetupSecretProvider(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InstanceStartedAt = DateTime.UtcNow;

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
    }

    /// <summary>
    /// Returns the secret value for startup logging only.
    /// This method is internal and should NEVER be exposed via API or any public endpoint.
    /// </summary>
    internal string GetSecretForLogging() => _secret;

    private bool IsBootstrapComplete()
    {
        if (_isBootstrapComplete.HasValue)
            return _isBootstrapComplete.Value;

        lock (_bootstrapCheckLock)
        {
            if (_isBootstrapComplete.HasValue)
                return _isBootstrapComplete.Value;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IInstanceBootstrapStateRepository>();
                var bootstrapState = repository.GetCurrent().GetAwaiter().GetResult();
                _isBootstrapComplete = bootstrapState?.IsCompleted == true;
            }
            catch
            {
                // If the database is not available yet (e.g., during startup), assume setup mode is active
                _isBootstrapComplete = false;
            }

            return _isBootstrapComplete.Value;
        }
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
}
