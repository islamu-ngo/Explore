// ABOUTME: Enum mirror for stable registration-answer synchronization lookup identities.
// ABOUTME: Expresses no sync, verified completion, selected fields, canonical data, and mirror-only output.

namespace Explore.Domain.Enums;

public enum RegistrationAnswerSyncModeEnum
{
    NONE = 1,
    COMPLETION_ONLY = 2,
    SELECTED_FIELDS = 3,
    FULL_CANONICAL = 4,
    MIRROR_ONLY = 5
}
