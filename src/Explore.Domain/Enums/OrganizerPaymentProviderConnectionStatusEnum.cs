// ABOUTME: Stable lookup IDs for organizer payment-provider connection lifecycle state.
// ABOUTME: Keeps readiness terminal states provider-neutral and persistence-friendly.

namespace Explore.Domain.Enums;

public enum OrganizerPaymentProviderConnectionStatusEnum
{
    PendingOnboarding = 1,
    Restricted = 2,
    Ready = 3,
    Disabled = 4,
    Replaced = 5
}
