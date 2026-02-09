using System;

namespace Explore.Application.DTOs.TenantSettings;

public class TenantSettingsDto
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public required string TenantFullName { get; set; }
}
