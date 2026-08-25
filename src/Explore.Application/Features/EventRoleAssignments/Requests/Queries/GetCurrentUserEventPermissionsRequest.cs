// ABOUTME: CQRS query for current user's effective event permissions for HAL affordance gating.
// ABOUTME: Returns role codes and permission codes from the event authority snapshot service.

using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Requests.Queries;

public sealed record GetCurrentUserEventPermissionsRequest : IRequest<CurrentUserEventPermissionsDto>
{
    public Guid TenantId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
}

public sealed record CurrentUserEventPermissionsDto
{
    public Guid EventId { get; init; }
    public bool HasAnyRole { get; init; }
    public bool IsOwner { get; init; }
    public bool IsManager { get; init; }
    public required IReadOnlySet<string> RoleCodes { get; init; }
    public required IReadOnlySet<string> PermissionCodes { get; init; }
}
