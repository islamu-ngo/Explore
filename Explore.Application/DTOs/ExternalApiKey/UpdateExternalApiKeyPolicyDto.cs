// ABOUTME: Input model for updating editable policy fields on a persisted external API key.
// ABOUTME: Keeps owner and tenant binding immutable while allowing scope and expiry maintenance.

namespace Explore.Application.DTOs.ExternalApiKey;

public class UpdateExternalApiKeyPolicyDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public List<string> Scopes { get; set; } = [];
    public DateTime? ExpiresAt { get; set; }
}
