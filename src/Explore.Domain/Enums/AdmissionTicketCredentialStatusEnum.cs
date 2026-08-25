// ABOUTME: Stable status identities for versioned admission credential children.
// ABOUTME: Only Active credentials can authorize admission; replaced authority is retained as Revoked.

namespace Explore.Domain.Enums;

public enum AdmissionTicketCredentialStatusEnum
{
    Active = 1,
    Revoked = 2
}
