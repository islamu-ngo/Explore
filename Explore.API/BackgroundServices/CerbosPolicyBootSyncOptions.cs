// ABOUTME: Options controlling one-shot Cerbos policy package publishing during API startup.
// ABOUTME: Keeps boot-time policy sync opt-in and bounded without exposing Admin API credentials.

namespace Explore.API.BackgroundServices;

/// <summary>
/// Controls zero-touch Cerbos policy package synchronization at API startup.
/// </summary>
public sealed class CerbosPolicyBootSyncOptions
{
    public const string SectionName = "Cerbos:PolicyBootSync";

    /// <summary>
    /// Enables a one-shot boot publish when instance Admin API settings are complete.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Delay before the background worker attempts the boot publish.
    /// </summary>
    public int InitialDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Maximum time allowed for the boot publish attempt.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}
