// ABOUTME: MediatR command for deleting a location by ID.
// ABOUTME: Carries the target location ID.
using System;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.Locations.Requests.Commands;

[AuthorizeResource(ResourceKinds.Location, AuthorizationActions.Delete)]
public class DeleteLocationCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
