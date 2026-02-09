using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class EventSessionAgendaItem : ITenantEntity
{
    public Guid Id { get; set; }

    [ForeignKey("EventSession")]
    public Guid EventSessionId { get; set; }
    public required EventSession EventSession { get; set; }

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }

    [ForeignKey("Location")]
    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }
}
