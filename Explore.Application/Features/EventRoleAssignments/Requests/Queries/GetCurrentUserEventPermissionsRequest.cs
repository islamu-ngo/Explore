// ABOUTME: CQRS query for current user's effective event permissions for HAL affordance gating.
// ABOUTME: Returns role codes and permission codes from the event authority snapshot service.

using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Requests.Queries;

public sealed class GetCurrentUserEventPermissionsRequest : IRequest<CurrentUserEventPermissionsDto>
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
}

public sealed class CurrentUserEventPermissionsDto
{
    public Guid EventId { get; set; }
    public bool HasAnyRole { get; set; }
    public bool IsOwner { get; set; }
    public bool IsManager { get; set; }
    public required IReadOnlySet<string> RoleCodes { get; set; }
    public required IReadOnlySet<string> PermissionCodes { get; set; }
}
