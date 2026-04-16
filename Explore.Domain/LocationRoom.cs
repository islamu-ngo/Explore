// ABOUTME: Persistent room entity under a Location for conference-style scheduling and room-aware agenda rendering.
// ABOUTME: Owns the stable identity that same-room overlap validation and concurrency-token guards key off.

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class LocationRoom : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; }

    [ForeignKey("Location")]
    public Guid LocationId { get; set; }
    public required Location Location { get; set; }

    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }

    public int? Capacity { get; set; }
    public int SortOrder { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public Guid ConcurrencyStamp { get; set; }
}
