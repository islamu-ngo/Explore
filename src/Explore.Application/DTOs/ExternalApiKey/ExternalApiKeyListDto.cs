// ABOUTME: Safe management projection for external API keys with masked display and credit quota info.
// ABOUTME: Excludes raw secret and hash material while exposing lifecycle, ownership, and credit metadata.

namespace Explore.Application.DTOs.ExternalApiKey;

public sealed record ExternalApiKeyListDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string KeyId { get; init; }
    public required string MaskedKeyId { get; init; }
    public int ExternalApiKeyOwnerTypeId { get; init; }
    public required string ExternalApiKeyOwnerTypeCode { get; init; }
    public required string ExternalApiKeyOwnerTypeName { get; init; }
    public Guid OwnerId { get; init; }
    public List<string> Scopes { get; init; } = [];
    public int ExternalApiKeyStatusId { get; init; }
    public required string ExternalApiKeyStatusCode { get; init; }
    public required string ExternalApiKeyStatusName { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public int ExternalApiKeyCreditPeriodId { get; init; }
    public required string ExternalApiKeyCreditPeriodCode { get; init; }
    public required string ExternalApiKeyCreditPeriodName { get; init; }
    public int? CreditLimit { get; init; }
    public int? MaxRolloverCredits { get; init; }
}
