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
    /// True when the instance has not completed onboarding and is not locked.
    /// </summary>
    bool IsSetupModeActive { get; }

    /// <summary>
    /// True when the secret was loaded from the SETUP_SECRET environment variable (vs auto-generated).
    /// </summary>
    bool IsFromEnvironmentVariable { get; }

    /// <summary>
    /// True when the 60-minute setup window has expired since instance boot.
    /// </summary>
    bool IsTimedOut { get; }

    /// <summary>
    /// The UTC timestamp captured at singleton construction time (instance boot).
    /// Used for 60-minute proximity timer — NOT from database (InstanceBootstrapState.CreatedAt is set at completion, not boot).
    /// </summary>
    DateTime InstanceStartedAt { get; }

    /// <summary>
    /// Validates the provided secret against the stored secret using timing-safe comparison.
    /// Returns false if the secret is wrong, null, or the 60-minute setup window has expired.
    /// </summary>
    bool ValidateSecret(string? secret);

    /// <summary>
    /// Transitions the provider to locked mode after onboarding completion.
    /// Once locked, IsSetupModeActive returns false and ValidateSecret always returns false.
    /// </summary>
    void Lock();
}
