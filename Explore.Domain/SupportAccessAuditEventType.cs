// ABOUTME: Lookup entity describing support-access audit event categories.
// ABOUTME: Backed by stable int IDs from SupportAccessAuditEventTypeEnum.

namespace Explore.Domain;

public class SupportAccessAuditEventType
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public bool IsLifecycleEvent { get; set; }
}
