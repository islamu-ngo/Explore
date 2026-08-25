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
    private IReadOnlyList<string> _scopes = Array.AsReadOnly(Array.Empty<string>());

    public IReadOnlyList<string> Scopes
    {
        get => _scopes;
        init => _scopes = value is null ? null! : Array.AsReadOnly(value.ToArray());
    }
    public DateTime? ExpiresAt { get; init; }
}
