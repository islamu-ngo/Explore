// ABOUTME: Input model for issuing a tenant-bound external API key.
// ABOUTME: Supports user-owned and organization-owned keys without exposing tenant identity on the wire.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.ExternalApiKey;

public class CreateExternalApiKeyDto
{
    public required string Name { get; set; }
    public ExternalApiKeyOwnerType OwnerType { get; set; } = ExternalApiKeyOwnerType.User;
    public Guid? OrganizationId { get; set; }
    public List<string> Scopes { get; set; } = [];
    public DateTime? ExpiresAt { get; set; }
}
