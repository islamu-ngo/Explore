// ABOUTME: Normalized lookup row for registration-answer synchronization modes.
// ABOUTME: Keeps stable synchronization IDs separate from their enum convenience mirror.

namespace Explore.Domain;

public sealed class RegistrationAnswerSyncMode
{
    public int Id { get; set; }
    public string MasterCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
