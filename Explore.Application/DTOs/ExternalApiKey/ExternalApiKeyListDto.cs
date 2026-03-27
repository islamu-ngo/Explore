// ABOUTME: Safe management projection for external API keys with masked display and credit quota info.
// ABOUTME: Excludes raw secret and hash material while exposing lifecycle, ownership, and credit metadata.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.ExternalApiKey;

public class ExternalApiKeyListDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string KeyId { get; set; }
    public required string MaskedKeyId { get; set; }
    public ExternalApiKeyOwnerType OwnerType { get; set; }
    public Guid OwnerId { get; set; }
    public List<string> Scopes { get; set; } = [];
    public ExternalApiKeyStatusEnum Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public ExternalApiKeyCreditPeriodEnum CreditPeriod { get; set; }
    public int? CreditLimit { get; set; }
    public int? MaxRolloverCredits { get; set; }
}
