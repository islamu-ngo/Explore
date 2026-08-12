// ABOUTME: Interface for the setup secret provider that gates instance onboarding.
// ABOUTME: Implementation generates or reads secret, validates with timing-safe comparison, and locks after completion.

namespace Explore.Application.Contracts.Services;

/// <summary>
/// Manages the setup secret lifecycle for instance onboarding.
/// Registered as a singleton — the secret is resolved once at startup and locked after onboarding completion.
/// </summary>
public interface ISetupSecretProvider
{
    /// <summary>
    /// Asynchronously loads bootstrap state from the database so that
    /// <see cref="IsSetupModeActive"/> can return a cached value without blocking.
    /// Must be called once at startup before any request processing.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the instance has not completed onboarding and is not locked.
    /// Returns false if <see cref="InitializeAsync"/> has not been called yet.
    /// </summary>
    bool IsSetupModeActive { get; }

    /// <summary>
    /// True when interactive setup endpoints require SETUP_SECRET validation.
    /// Defaults to true and may only become false for explicitly trusted managed-provisioning deployments.
    /// </summary>
    bool IsSetupSecretRequired { get; }

    /// <summary>
    /// True when the secret was loaded from the SETUP_SECRET environment variable (vs auto-generated).
    /// </summary>
    bool IsFromEnvironmentVariable { get; }

    string? GeneratedSecretFilePath => null;

    /// <summary>
    /// Validates the provided secret against the stored secret using timing-safe comparison.
    /// Returns false if the secret is wrong, null, or setup has been locked.
    /// </summary>
    bool ValidateSecret(string? secret);

    /// <summary>
    /// Transitions the provider to locked mode after onboarding completion.
    /// Once locked, IsSetupModeActive returns false and ValidateSecret always returns false.
    /// </summary>
    void Lock();

}
