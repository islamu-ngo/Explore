using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Location;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Locations.Requests.Commands;

[AuthorizeResource("location", PermissionAction.Update)]
public class UpdateLocationCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateLocationDto LocationDto { get; set; }

    string? ISecureRequest.ResourceId => LocationDto.Id.ToString();
}
