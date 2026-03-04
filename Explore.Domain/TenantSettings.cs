using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class TenantSettings : ITenantEntity, IConcurrencyAware
{
    public Guid Id { get; set; }
    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    // Event Publishing Policy
    public int EventPublishingPolicy { get; set; }

    // Organization Registration
    public bool AllowPublicOrganizationRegistration { get; set; }
    public bool RequireOrganizationVerification { get; set; }

    // Group Creation
    public bool AllowPublicGroupCreation { get; set; }
    public bool RequireGroupApproval { get; set; }

    // Default Actor References
    [ForeignKey("DefaultOrganization")]
    public Guid? DefaultOrganizationId { get; set; }
    public Organization? DefaultOrganization { get; set; }

    [ForeignKey("DefaultGroup")]
    public Guid? DefaultGroupId { get; set; }
    public Group? DefaultGroup { get; set; }

    // Concurrency control
    public Guid ConcurrencyStamp { get; set; }
}
