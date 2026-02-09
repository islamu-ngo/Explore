using System;

namespace Explore.Application.DTOs.UserAuthenticationToken;

public class UserAuthenticationTokenListDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string UserEmail { get; set; }
    public Guid TenantId { get; set; }
    public required string Provider { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
