// ABOUTME: Loads the operator-owned PostgreSQL provisioning script for the erasure-authority database.
// ABOUTME: Keeps authority schema creation explicit and separate from application EF migrations.

using System.Reflection;

namespace Explore.Infrastructure.Privacy.ErasureAuthority;

public static class LocationPrivacyErasureAuthoritySchema
{
    private const string ResourceName =
        "Explore.Infrastructure.Privacy.ErasureAuthority.LocationPrivacyErasureAuthoritySchema.sql";

    public static string ReadProvisioningSql()
    {
        using var stream = typeof(LocationPrivacyErasureAuthoritySchema).Assembly
            .GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The erasure-authority provisioning resource is unavailable.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
