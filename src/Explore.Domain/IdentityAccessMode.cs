// ABOUTME: Normalized lookup describing the identity required to enter a platform-managed participation workflow.
// ABOUTME: Distinguishes account, guest, and opaque capability-token access without boolean combinations.

namespace Explore.Domain;

public sealed class IdentityAccessMode
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
