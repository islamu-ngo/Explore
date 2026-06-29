// ABOUTME: Lifecycle status for an AI data-disclosure consent grant.
// ABOUTME: Values ordered Pending → Granted → (Revoked|Expired); only Granted authorizes disclosure.

namespace Explore.Domain.Enums;

public enum AiConsentGrantStatusEnum
{
    Pending = 0,
    Granted = 1,
    Revoked = 2,
    Expired = 3
}
