// ABOUTME: Tenant branding settings payload for typed document storage.
// ABOUTME: Stores URLs and display labels, never credentials or tokens.

namespace Explore.Domain.Settings.Documents.Payloads;

public sealed record BrandingSettings
{
    public string? DisplayName { get; init; }

    public string? LogoUrl { get; init; }

    public string? FaviconUrl { get; init; }

    public string? CustomCssUrl { get; init; }
}
