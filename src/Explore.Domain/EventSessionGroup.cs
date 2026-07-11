// ABOUTME: Tenant-scoped program section/track/devroom grouping for sessions inside an event.
// ABOUTME: Keeps conference program structure on EventSession instead of modeling talks as child events.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class EventSessionGroup : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; }

    [ForeignKey("Event")]
    public Guid EventId { get; set; }
    public required Event Event { get; set; }

    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }

    [ForeignKey("Location")]
    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    [ForeignKey("Room")]
    public Guid? RoomId { get; set; }
    public LocationRoom? Room { get; set; }

    public string? Color { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    public ICollection<EventSessionGroupSession> Sessions { get; set; } = new List<EventSessionGroupSession>();

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public Guid ConcurrencyStamp { get; set; }
}
