// ABOUTME: Normalized lookup row for registration-requirement criticality.
// ABOUTME: Keeps stable criticality IDs separate from their enum convenience mirror.

namespace Explore.Domain;

public sealed class RegistrationRequirementCriticality
{
    public int Id { get; set; }
    public string MasterCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
