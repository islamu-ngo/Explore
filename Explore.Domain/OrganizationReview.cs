using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class OrganizationReview : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }

    [ForeignKey("Organization")]
    public Guid OrganizationId { get; set; }
    public required Organization Organization { get; set; }

    [ForeignKey("Event")]
    public Guid EventId { get; set; }
    public required Event Event { get; set; }

    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public required User User { get; set; }

    public required string ReviewerName { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }
}
