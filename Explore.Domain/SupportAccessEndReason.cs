// ABOUTME: Lookup entity describing why a support-access session ended.
// ABOUTME: Keeps lifecycle reporting deterministic while allowing optional explanatory text.

namespace Explore.Domain;

public class SupportAccessEndReason
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
