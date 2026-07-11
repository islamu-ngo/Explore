// ABOUTME: MediatR command for PATCH-based Location updates.
// ABOUTME: Carries route authority, If-Match concurrency, and the grouped update payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Location;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Locations.Requests.Commands;

[AuthorizeResource(ResourceKinds.Location, AuthorizationActions.Update)]
public class UpdateLocationCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid LocationId { get; set; }

    public Guid ExpectedConcurrencyStamp { get; set; }

    public required UpdateLocationDto UpdateLocationDto { get; set; }

    string? ISecureRequest.ResourceId => LocationId.ToString();
}
