// ABOUTME: Enum mirror for stable inventory-hold lifecycle lookup identities.
// ABOUTME: Separates active, consumed, released, expired, and cancelled reservations.

namespace Explore.Domain.Enums;

public enum RegistrationInventoryHoldStatusEnum
{
    Active = 1,
    Consumed = 2,
    Released = 3,
    Expired = 4,
    Cancelled = 5
}
