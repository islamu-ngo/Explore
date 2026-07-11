// ABOUTME: MediatR command for creating a new event location.
// ABOUTME: Carries the CreateLocationDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Location;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Locations.Requests.Commands;

[AuthorizeResource(ResourceKinds.Location, AuthorizationActions.Create)]
public class CreateLocationCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateLocationDto LocationDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        LocationDto.TenantId != Guid.Empty
            ? new Dictionary<string, object> { ["tenantId"] = LocationDto.TenantId.ToString() }
            : null;
}
