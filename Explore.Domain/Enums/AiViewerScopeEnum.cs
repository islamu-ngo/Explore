// ABOUTME: Defines the acting principal's scope for AI context disclosure resolution.
// ABOUTME: Drives the policy hierarchy intersection (instance > tenant > user) at the gateway layer.

namespace Explore.Domain.Enums;

public enum AiViewerScopeEnum
{
    Public = 0,
    OrganizerTeam = 1,
    InstanceAdmin = 2,
}
