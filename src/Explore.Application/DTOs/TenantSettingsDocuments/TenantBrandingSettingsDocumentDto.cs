// ABOUTME: API-safe DTO for the tenant branding typed settings document.
// ABOUTME: Exposes typed payload and resolver metadata without leaking persistence JSON.

namespace Explore.Application.DTOs.TenantSettingsDocuments;

public sealed record TenantBrandingSettingsDocumentDto
{
    public required string DocumentKey { get; init; }

    public required int SchemaVersion { get; init; }

    public required string DefaultsVersion { get; init; }

    public required TenantBrandingSettingsPayloadDto Payload { get; init; }

    public required string Source { get; init; }

    public required Guid SourceScopeId { get; init; }

    public required Guid ConcurrencyStamp { get; init; }

    public bool IsLockedByInstance { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

public sealed record ReplaceTenantBrandingSettingsDocumentDto
{
    public required Guid ExpectedConcurrencyStamp { get; init; }

    public required TenantBrandingSettingsPayloadDto Payload { get; init; }
}

public sealed record TenantBrandingSettingsPayloadDto
{
    public string? DisplayName { get; init; }

    public string? LogoUrl { get; init; }

    public string? FaviconUrl { get; init; }

    public string? CustomCssUrl { get; init; }
}
