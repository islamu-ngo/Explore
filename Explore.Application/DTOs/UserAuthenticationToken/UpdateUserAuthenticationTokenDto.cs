using System;

namespace Explore.Application.DTOs.UserAuthenticationToken;

public class UpdateUserAuthenticationTokenDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public required string Provider { get; set; }
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required string PdsHost { get; set; }
    public required string DpopKey { get; set; }
    public required string IdToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
