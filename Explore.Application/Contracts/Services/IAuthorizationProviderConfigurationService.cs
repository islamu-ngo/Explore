// ABOUTME: Service contract for managing instance-level authorization provider configuration.
// ABOUTME: Handles reading, applying, and verifying authorization provider settings (Cerbos or Local).

using Explore.Application.DTOs.Onboarding;

namespace Explore.Application.Contracts.Services;

/// <summary>
/// Service for reading and applying authorization provider configuration settings.
/// </summary>
public interface IAuthorizationProviderConfigurationService
{
    /// <summary>
    /// Reads current authorization provider configuration from SystemSetting records
    /// and environment-detected state.
    /// </summary>
    Task<AuthorizationProviderConfigurationDto> ReadConfigurationAsync();

    /// <summary>
    /// Applies authorization provider configuration to SystemSetting records.
    /// </summary>
    Task ApplyConfigurationAsync(AuthorizationProviderConfigurationDto configuration);

    /// <summary>
    /// Checks whether an authorization provider has been explicitly configured.
    /// </summary>
    Task<bool> IsConfiguredAsync();

    /// <summary>
    /// Verifies that a Cerbos gRPC endpoint is reachable by performing a health check.
    /// Returns true if the endpoint responds, false otherwise.
    /// </summary>
    Task<bool> VerifyCerbosEndpointAsync(string grpcEndpoint, CancellationToken cancellationToken = default);
}
