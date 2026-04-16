// ABOUTME: Singleton service that manages the setup secret lifecycle for instance onboarding.
// ABOUTME: Reads SETUP_SECRET from env var or auto-generates a 32-char crypto-random token; validates with timing-safe comparison.

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
        _isBootstrapComplete = true;
    }

    /// <inheritdoc />
    public string? GetSecretForLogging() => IsFromEnvironmentVariable ? null : _secret;

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
                var bootstrapState = await repository.GetCurrent();
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
}
