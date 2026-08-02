// ABOUTME: Defines normalized lookup rows for registration-answer subject identity.
// ABOUTME: Keeps stable persisted IDs and codes separate from the enum convenience mirror.

namespace Explore.Domain;

public sealed class RegistrationAnswerSubjectType
{
    public int Id { get; set; }
    public string MasterCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
