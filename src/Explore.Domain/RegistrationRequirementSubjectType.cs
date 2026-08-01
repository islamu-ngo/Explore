// ABOUTME: Normalized lookup row for registration-requirement subject applicability.
// ABOUTME: Keeps stable subject IDs separate from their enum convenience mirror.

namespace Explore.Domain;

public sealed class RegistrationRequirementSubjectType
{
    public int Id { get; set; }
    public string MasterCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
