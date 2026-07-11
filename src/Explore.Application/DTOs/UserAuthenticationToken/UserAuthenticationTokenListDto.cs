// ABOUTME: Safe list DTO for user authentication-token session metadata.
// ABOUTME: Omits user PII and credential material from account-security listings.

namespace Explore.Application.DTOs.UserAuthenticationToken;

public class UserAuthenticationTokenListDto
{
    public Guid Id { get; set; }
    public required string Provider { get; set; }
    public string? PdsHost { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
