using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class Organization : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; }

    /// <summary>
    /// 1:1 extension table containing organization contact/location PII.
    /// </summary>
    public required OrganizationPii Pii { get; set; }

    [NotMapped]
    public string FullName
    {
        get => Pii.FullName;
        set => Pii.FullName = value;
    }

    [NotMapped]
    public string? Email
    {
        get => Pii.Email;
        set => Pii.Email = value;
    }

    [NotMapped]
    public string? Country
    {
        get => Pii.Country;
        set => Pii.Country = value;
    }

    [NotMapped]
    public string? City
    {
        get => Pii.City;
        set => Pii.City = value;
    }

    [NotMapped]
    public string? Address
    {
        get => Pii.Address;
        set => Pii.Address = value;
    }

    [NotMapped]
    public string? Postcode
    {
        get => Pii.Postcode;
        set => Pii.Postcode = value;
    }

    public string? WebsiteUrl { get; set; }
    public string? MetadataJson { get; set; }

    [ForeignKey("ApprovalStatus")]
    public int ApprovalStatusId { get; set; }
    public required ApprovalStatus ApprovalStatus { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    [ForeignKey("Actor")]
    public Guid? ActorId { get; set; }
    public Actor? Actor { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Approval audit fields
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApprovalNotes { get; set; }

    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Concurrency control
    public Guid ConcurrencyStamp { get; set; }

    // Navigation property for members
    public ICollection<OrganizationMember>? Members { get; set; }

}
