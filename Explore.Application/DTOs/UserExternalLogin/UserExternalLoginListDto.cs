using System;

namespace Explore.Application.DTOs.UserExternalLogin;

public class UserExternalLoginListDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string UserEmail { get; set; }
    public Guid TenantId { get; set; }
    public required string Provider { get; set; }
    public required string ProviderDisplayName { get; set; }
}
