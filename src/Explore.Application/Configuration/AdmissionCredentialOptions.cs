// ABOUTME: Configures the active version of the backend-only admission credential lookup key.
// ABOUTME: Keeps rotation metadata explicit so persisted credentials survive restore and key changes.

namespace Explore.Application.Configuration;

public sealed class AdmissionCredentialOptions
{
    public const string SectionName = "Admissions:CredentialLookup";

    public int ActiveKeyVersion { get; set; } = 1;
}
