// ABOUTME: Route-owned grouped PATCH contract for editable external API key policy.
// ABOUTME: Excludes key material, owner binding, tenant binding, status, and usage state.

namespace Explore.Application.DTOs.ExternalApiKey;

public sealed class UpdateExternalApiKeyPolicyDto
{
    public ExternalApiKeyMetadataUpdateDto? Metadata { get; set; }
    public ExternalApiKeyAccessPolicyUpdateDto? AccessPolicy { get; set; }
}

public sealed class ExternalApiKeyMetadataUpdateDto
{
    public required string Name { get; set; }
}

public sealed class ExternalApiKeyAccessPolicyUpdateDto
{
    public List<string> Scopes { get; set; } = [];
    public DateTime? ExpiresAt { get; set; }
}
