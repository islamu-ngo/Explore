// ABOUTME: Application read-model vocabulary for public shell primary organization resolution states.
// ABOUTME: Keeps organization-centric posture states out of Domain and outside DTO naming conventions.

namespace Explore.Application.Models;

public enum PublicExperiencePrimaryOrganizationState
{
    Available = 0,
    NotConfigured = 1,
    Missing = 2,
    Deleted = 3,
    HiddenOrInactive = 4,
    CrossTenantInvalid = 5,
    ActorUnavailable = 6
}
