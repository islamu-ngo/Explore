// ABOUTME: Route-owned grouped PATCH contract for editable external API key policy.
// ABOUTME: Excludes key material, owner binding, tenant binding, status, and usage state.

namespace Explore.Application.DTOs.ExternalApiKey;

public sealed record UpdateExternalApiKeyPolicyDto
{
    public ExternalApiKeyMetadataUpdateDto? Metadata { get; init; }
    public ExternalApiKeyAccessPolicyUpdateDto? AccessPolicy { get; init; }
}

public sealed record ExternalApiKeyMetadataUpdateDto
{
    public required string Name { get; init; }
}

public sealed record ExternalApiKeyAccessPolicyUpdateDto
{
    public List<string> Scopes { get; init; } = [];
    public DateTime? ExpiresAt { get; init; }
}
