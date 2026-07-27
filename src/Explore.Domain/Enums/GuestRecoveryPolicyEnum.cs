// ABOUTME: Scalar policy identifiers governing how a guest can recover participation access.
// ABOUTME: This enum is intentionally not backed by a fourth normalized lookup entity.

namespace Explore.Domain.Enums;

public enum GuestRecoveryPolicyEnum
{
    VerifiedEmailRequired = 1,
    UnverifiedEmailAccepted = 2,
    EmailOptional = 3,
    CapabilityLinkOnly = 4,
    NoRecovery = 5
}
