// ABOUTME: Defines canonical cross-path mutation lock identities for tenant aggregates.
// ABOUTME: Makes ordinary creation and manifest bootstrap serialize on the same normalized slug.

namespace Explore.Application.Features.Tenants;

public static class TenantMutationLockKeys
{
    private const string SlugPrefix = "tenant.slug.";

    public static string ForSlug(string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        return $"{SlugPrefix}{slug.Trim().ToLowerInvariant()}";
    }
}
