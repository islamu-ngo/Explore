using System;

namespace Explore.Application.DTOs.TenantSettings;

public class UpdateTenantSettingsDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public int EventPublishingPolicy { get; set; }
    public bool AllowPublicOrganizationRegistration { get; set; }
    public bool RequireOrganizationVerification { get; set; }
    public bool AllowPublicGroupCreation { get; set; }
    public bool RequireGroupApproval { get; set; }
}
