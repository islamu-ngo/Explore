// ABOUTME: Safe response DTO for user authentication-token session metadata.
// ABOUTME: Intentionally excludes access, refresh, ID, and DPoP credential material.

namespace Explore.Application.DTOs.UserAuthenticationToken;

public class UserAuthenticationTokenDto
{
    public Guid Id { get; set; }
    public required string Provider { get; set; }
    public string? PdsHost { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
