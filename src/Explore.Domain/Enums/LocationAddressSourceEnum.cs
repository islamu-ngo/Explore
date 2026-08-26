// ABOUTME: Stable lookup identifiers for the provenance of a Location's current address.
// ABOUTME: Keeps legacy, manual, and protected provider-selected origins independent from reuse visibility.

namespace Explore.Domain.Enums;

public enum LocationAddressSourceEnum
{
    UnknownLegacy = 1,
    Manual = 2,
    ProviderSelection = 3
}
