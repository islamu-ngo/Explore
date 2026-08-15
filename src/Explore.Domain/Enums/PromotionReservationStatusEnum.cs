// ABOUTME: Enum mirror for stable PromotionReservationStatus lookup identities.
// ABOUTME: Supports exact-once reserve, consume, release, and expiry transitions.

namespace Explore.Domain.Enums;

public enum PromotionReservationStatusEnum
{
    Active = 1,
    Consumed = 2,
    Released = 3,
    Expired = 4
}
