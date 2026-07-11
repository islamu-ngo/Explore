// ABOUTME: Request DTO for creating the authenticated user's external auth token metadata.
// ABOUTME: User and tenant ownership are stamped from server-side context, not client input.

namespace Explore.Application.DTOs.UserAuthenticationToken;

public class CreateUserAuthenticationTokenDto
{
    public required string Provider { get; set; }
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public string? PdsHost { get; set; }
    public required string DpopKey { get; set; }
    public required string IdToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
