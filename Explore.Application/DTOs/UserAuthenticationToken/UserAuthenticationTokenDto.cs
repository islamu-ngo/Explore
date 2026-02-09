using System;

namespace Explore.Application.DTOs.UserAuthenticationToken;

public class UserAuthenticationTokenDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string UserEmail { get; set; }
    public required string UserFullName { get; set; }
    public Guid TenantId { get; set; }
    public required string TenantFullName { get; set; }
    public required string Provider { get; set; }
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required string PdsHost { get; set; }
    public required string DpopKey { get; set; }
    public required string IdToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
