using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Explore.Domain;

public class EventType
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string MasterCode { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// Null = global shared event type.
    /// Non-null = tenant-specific event type visible only in that tenant context.
    /// </summary>
    [ForeignKey(nameof(Tenant))]
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
}
