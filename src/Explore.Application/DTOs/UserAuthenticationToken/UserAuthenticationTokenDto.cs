// ABOUTME: Safe response DTO for user authentication-token session metadata.
// ABOUTME: Intentionally excludes access, refresh, ID, and DPoP credential material.

namespace Explore.Application.DTOs.UserAuthenticationToken;

public sealed record UserAuthenticationTokenDto
{
    public Guid Id { get; init; }
    public required string Provider { get; init; }
    public string? PdsHost { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
