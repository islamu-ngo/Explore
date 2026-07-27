// ABOUTME: Normalized lookup describing whether advance registration is applicable, optional, or required.
// ABOUTME: Keeps attendance obligation independent from participation ownership and identity access.

namespace Explore.Domain;

public sealed class AdvanceRegistrationObligation
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
