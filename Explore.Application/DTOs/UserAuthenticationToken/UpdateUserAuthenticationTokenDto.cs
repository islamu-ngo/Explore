// ABOUTME: Request DTO for updating the authenticated user's external auth token metadata.
// ABOUTME: Prevents client-controlled user or tenant ownership changes.

namespace Explore.Application.DTOs.UserAuthenticationToken;

public class UpdateUserAuthenticationTokenDto
{
    public Guid Id { get; set; }
    public required string Provider { get; set; }
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public string? PdsHost { get; set; }
    public required string DpopKey { get; set; }
    public required string IdToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
