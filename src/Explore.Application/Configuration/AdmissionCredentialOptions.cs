// ABOUTME: Configures the active version of the backend-only admission credential lookup key.
// ABOUTME: Keeps rotation metadata explicit so persisted credentials survive restore and key changes.

namespace Explore.Application.Configuration;

public sealed class AdmissionCredentialOptions
{
    public const string SectionName = "Admissions:CredentialLookup";
    public const int MaximumKeyVersions = 8;

    public int ActiveKeyVersion { get; set; } = 1;
    public int[] RetainedKeyVersions { get; set; } = [];

    public int[] GetDigestKeyVersions()
    {
        int[] versions = RetainedKeyVersions
            .Prepend(ActiveKeyVersion)
            .Distinct()
            .ToArray();
        if (ActiveKeyVersion < 1 || versions.Any(version => version < 1) ||
            versions.Length > MaximumKeyVersions)
        {
            throw new InvalidOperationException(
                $"Admission credential key versions must be positive and at most {MaximumKeyVersions}.");
        }

        return versions;
    }
}
