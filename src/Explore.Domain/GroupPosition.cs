// ABOUTME: Lookup entity representing a named position within a Group (e.g., Leader, Coordinator).
// ABOUTME: Mirrors OrganizationPosition pattern — referenced by GroupMember via nullable FK.

namespace Explore.Domain;

public class GroupPosition
{
    public int Id { get; set; }
    public string MasterCode { get; set; }
    public string FullName { get; set; }
    public string? Description { get; set; }
}
