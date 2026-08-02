// ABOUTME: Enum mirror for stable registration-submission lifecycle lookup identities.
// ABOUTME: Separates finalizable evidence from retained evidence-only late submissions.

namespace Explore.Domain.Enums;

public enum RegistrationSubmissionStatusEnum
{
    Received = 1,
    Finalized = 2,
    EvidenceOnly = 3
}
