// ABOUTME: Enum mirror for stable booking-party lookup identities on registration orders.
// ABOUTME: Distinguishes individual, household, organization, company, and community group bookings.

namespace Explore.Domain.Enums;

public enum BookingPartyTypeEnum
{
    Individual = 1,
    Household = 2,
    Organization = 3,
    Company = 4,
    CommunityGroup = 5
}
