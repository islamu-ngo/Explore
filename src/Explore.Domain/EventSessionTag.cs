// ABOUTME: Session-level tag junction distinct from event-level EventTags for program-grain precision.
// ABOUTME: Unique per (TenantId, EventSessionId, TagId); event-level umbrella taxonomy is enforced separately on Event.

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class EventSessionTag : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }

    [ForeignKey("EventSession")]
    public Guid EventSessionId { get; set; }
    public required EventSession EventSession { get; set; }

    [ForeignKey("Tag")]
    public Guid TagId { get; set; }
    public required Tag Tag { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
