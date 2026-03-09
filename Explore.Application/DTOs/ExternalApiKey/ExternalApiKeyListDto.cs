// ABOUTME: Safe management projection for external API keys.
// ABOUTME: Excludes raw secret and hash material while exposing lifecycle and ownership metadata.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.ExternalApiKey;

public class ExternalApiKeyListDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string KeyId { get; set; }
    public ExternalApiKeyOwnerType OwnerType { get; set; }
    public Guid OwnerId { get; set; }
    public List<string> Scopes { get; set; } = [];
    public ExternalApiKeyStatus Status { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
