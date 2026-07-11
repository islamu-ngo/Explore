// ABOUTME: Per-user notification category preference used by unsubscribe and dispatch-time consent checks.
// ABOUTME: Absence means the category remains enabled; explicit rows capture opt-outs and future re-subscriptions.

namespace Explore.Domain;

using Explore.Domain.Interfaces;

public class UserNotificationPreference : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    public Guid UserId { get; set; }

    public required string Category { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
