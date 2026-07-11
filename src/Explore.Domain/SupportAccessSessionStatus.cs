// ABOUTME: Lookup entity describing support-access session lifecycle states.
// ABOUTME: Backed by stable int IDs from SupportAccessSessionStatusEnum.

namespace Explore.Domain;

public class SupportAccessSessionStatus
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public bool IsTerminal { get; set; }
}
