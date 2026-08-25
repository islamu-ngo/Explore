// ABOUTME: Configures admission recovery capability key rotation and bounded lifetime.
// ABOUTME: Keeps the active key version explicit while persisted versions remain resolvable.

namespace Explore.Application.Configuration;

public sealed class AdmissionRecoveryOptions
{
    public const string SectionName = "Admissions:Recovery";

    public int ActiveKeyVersion { get; set; } = 1;
    public int[] RetainedKeyVersions { get; set; } = [];
    public int CapabilityLifetimeMinutes { get; set; } = 15;
}
