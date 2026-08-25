// ABOUTME: Safe list DTO for user authentication-token session metadata.
// ABOUTME: Omits user PII and credential material from account-security listings.

namespace Explore.Application.DTOs.UserAuthenticationToken;

public sealed record UserAuthenticationTokenListDto
{
    public Guid Id { get; init; }
    public required string Provider { get; init; }
    public string? PdsHost { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
