// ABOUTME: Safe management projection for external API keys with masked display and credit quota info.
// ABOUTME: Excludes raw secret and hash material while exposing lifecycle, ownership, and credit metadata.

namespace Explore.Application.DTOs.ExternalApiKey;

public class ExternalApiKeyListDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string KeyId { get; set; }
    public required string MaskedKeyId { get; set; }
    public int ExternalApiKeyOwnerTypeId { get; set; }
    public required string ExternalApiKeyOwnerTypeCode { get; set; }
    public required string ExternalApiKeyOwnerTypeName { get; set; }
    public Guid OwnerId { get; set; }
    public List<string> Scopes { get; set; } = [];
    public int ExternalApiKeyStatusId { get; set; }
    public required string ExternalApiKeyStatusCode { get; set; }
    public required string ExternalApiKeyStatusName { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int ExternalApiKeyCreditPeriodId { get; set; }
    public required string ExternalApiKeyCreditPeriodCode { get; set; }
    public required string ExternalApiKeyCreditPeriodName { get; set; }
    public int? CreditLimit { get; set; }
    public int? MaxRolloverCredits { get; set; }
}
