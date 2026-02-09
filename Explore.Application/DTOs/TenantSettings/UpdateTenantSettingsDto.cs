using System;

namespace Explore.Application.DTOs.TenantSettings;

public class UpdateTenantSettingsDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
}
