// ABOUTME: Audit record for each export of shared contact data by an organisation member.
// ABOUTME: Tracks who exported, when, in what format, and how many rows were included.

using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class EventContactShareExport : ITenantEntity
{
    public Guid Id { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    [ForeignKey("RecipientActor")]
    public Guid RecipientActorId { get; set; }
    public Actor? RecipientActor { get; set; }

    /// <summary>
    /// Optional event filter applied during export. Null means all events for this org.
    /// </summary>
    [ForeignKey("Event")]
    public Guid? EventId { get; set; }
    public Event? Event { get; set; }

    [ForeignKey("ExportedByUser")]
    public Guid ExportedByUserId { get; set; }
    public User? ExportedByUser { get; set; }

    /// <summary>
    /// Export format: "csv" or "tsv".
    /// </summary>
    public required string Format { get; set; }

    public int RowCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<EventContactShareExportItem>? Items { get; set; }
}
