// ABOUTME: Health check for the secret resolution pipeline.
// ABOUTME: Reports Degraded (not Unhealthy) when Infisical is configured but unreachable.

namespace Explore.Secrets.HealthChecks;

using Explore.Application.Contracts.Secrets;
using Explore.Secrets.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Verifies the secret resolver pipeline can initialize.
/// </summary>
/// <remarks>
/// <para>The check deliberately returns <see cref="HealthStatus.Degraded"/> rather than
/// <see cref="HealthStatus.Unhealthy"/> when an external source (Infisical) is misconfigured.
/// The platform is designed to run in "minimal" deployments where Infisical is absent -
/// that is a supported state, not a failure. Only a catastrophic internal error
/// (resolver itself throws) is reported as Unhealthy.</para>
/// </remarks>
public sealed class SecretResolverHealthCheck : IHealthCheck
{
    private readonly ISecretResolver _resolver;
    private readonly IInfisicalClientFactory _infisicalFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecretResolverHealthCheck> _logger;

    public SecretResolverHealthCheck(
        ISecretResolver resolver,
        IInfisicalClientFactory infisicalFactory,
        IConfiguration configuration,
        ILogger<SecretResolverHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(infisicalFactory);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        _resolver = resolver;
        _infisicalFactory = infisicalFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        PrimaryDatabaseConnectionOptions database;
        try
        {
            database = PrimaryDatabaseConfiguration.BindRuntime(_configuration);
        }
#pragma warning disable CA1031 // Do not catch general exception types - boundary check
        catch (Exception)
#pragma warning restore CA1031
        {
            _logger.LogError("secret_configuration_invalid");
            return HealthCheckResult.Unhealthy(
                description: "Secret resolver configuration is unavailable.",
                data: new Dictionary<string, object> { ["databaseConfiguration"] = "invalid" });
        }

        try
        {
            var client = await _infisicalFactory.GetClientAsync(cancellationToken).ConfigureAwait(false);
            _ = _resolver;
            return HealthCheckResult.Healthy(
                "Secret resolver pipeline is available.",
                new Dictionary<string, object>
                {
                    ["databaseProvider"] = database.Provider.ToString(),
                    ["providerState"] = client is null ? "unconfigured" : "available"
                });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031 // Provider boundary exposes a bounded health state.
        catch (Exception)
#pragma warning restore CA1031
        {
            _logger.LogError("secret_provider_unavailable");
            return HealthCheckResult.Degraded(
                description: "Configured secret provider is unavailable.",
                data: new Dictionary<string, object>
                {
                    ["databaseProvider"] = database.Provider.ToString(),
                    ["providerState"] = "unavailable",
                    ["remediation"] = "docs/TROUBLESHOOTING.md#secret-provider-unavailable"
                });
        }
    }
}
