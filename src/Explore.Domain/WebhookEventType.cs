// ABOUTME: Canonical outgoing webhook event type catalog row with schema and retention metadata.
// ABOUTME: Gives Local and Svix providers the same event taxonomy independent of delivery backend.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class WebhookEventType : IAuditableEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string GroupName { get; set; }
    public required string Description { get; set; }
    public required string SchemaJson { get; set; }
    public int SchemaVersion { get; set; }
    public bool IsPublic { get; set; }
    public bool IsEnabled { get; set; }
    public int PayloadRetentionDays { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
