// ABOUTME: MediatR query for retrieving a single room by Id.
// ABOUTME: Returns null when not found; caller translates to 404.

using Explore.Application.Authorization;
using Explore.Application.DTOs.LocationRoom;
using MediatR;

namespace Explore.Application.Features.LocationRooms.Requests.Queries;

[AuthorizeResource(ResourceKinds.LocationRoom, AuthorizationActions.LocationRooms.View)]
public class GetLocationRoomDetailRequest : IRequest<LocationRoomDto?>, ISecureRequest
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => Id == Guid.Empty ? null : Id.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object> { ["tenantId"] = TenantId.ToString("D") };
}
