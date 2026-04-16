using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

/// <summary>
/// Represents a thematic grouping of related events (e.g., a Ramadan lecture series, a conference).
/// </summary>
public class EventSeries : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }

    [ForeignKey("FeaturedImage")]
    public Guid? FeaturedImageId { get; set; }
    public StorageObject? FeaturedImage { get; set; }

    [ForeignKey("Actor")]
    public Guid ActorId { get; set; }
    public Actor Actor { get; set; } = null!;

    public bool IsPublished { get; set; }
    public int TotalViews { get; set; }

    [ForeignKey("VisibilityType")]
    public int VisibilityTypeId { get; set; }
    public required VisibilityType VisibilityType { get; set; }

    // Aggregate temporal info
    public DateTimeOffset? StartDateUtc { get; set; }
    public DateTimeOffset? EndDateUtc { get; set; }

    private readonly List<Event> _events = [];
    public IReadOnlyList<Event> Events => _events.AsReadOnly();

    // ITenantEntity
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // IAuditableEntity
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // IConcurrencyAware
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
}
