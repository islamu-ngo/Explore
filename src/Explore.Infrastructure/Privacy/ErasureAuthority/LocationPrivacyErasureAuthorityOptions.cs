// ABOUTME: Defines connection configuration for the independently operated erasure-authority database.
// ABOUTME: Keeps authority storage outside the application database and its EF migration lifecycle.

namespace Explore.Infrastructure.Privacy.ErasureAuthority;

public sealed class LocationPrivacyErasureAuthorityOptions
{
    public const string SectionName = "LocationPrivacy:ErasureAuthority";

    public string ConnectionString { get; set; } = string.Empty;
}
