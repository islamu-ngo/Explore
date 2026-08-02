// ABOUTME: Records the immutable operator audit for a registration-file quarantine release.
// ABOUTME: Preserves the first actor, time, reason, and exact status transition independently of mutable file state.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public sealed class RegistrationAnswerFileRelease : ITenantEntity
{
    private RegistrationAnswerFileRelease()
    {
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; set; }
    public Guid RegistrationAnswerFileId { get; private set; }
    public Guid ReleasedBy { get; private set; }
    public DateTime ReleasedAt { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string PreviousQuarantineState { get; private set; } = string.Empty;
    public string NewQuarantineState { get; private set; } = string.Empty;

    internal static RegistrationAnswerFileRelease Record(
        RegistrationAnswerFile file,
        Guid releasedBy,
        string reason,
        DateTime releasedAt)
        => new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = file.TenantId,
            RegistrationAnswerFileId = file.Id,
            ReleasedBy = releasedBy,
            ReleasedAt = releasedAt,
            Reason = reason,
            PreviousQuarantineState = RegistrationAnswerFileQuarantineStates.Quarantined,
            NewQuarantineState = RegistrationAnswerFileQuarantineStates.Released
        };
}
