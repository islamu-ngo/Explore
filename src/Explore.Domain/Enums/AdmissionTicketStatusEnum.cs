// ABOUTME: Stable lifecycle identities for independently revocable admission tickets.
// ABOUTME: Keeps admission state separate from registration-order and payment state.

namespace Explore.Domain.Enums;

public enum AdmissionTicketStatusEnum
{
    Active = 1,
    Suspended = 2,
    Revoked = 3,
    Cancelled = 4,
    Transferred = 5,
    Expired = 6
}
