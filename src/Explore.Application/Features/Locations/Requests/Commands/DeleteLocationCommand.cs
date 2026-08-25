// ABOUTME: MediatR command for deleting a location by ID.
// ABOUTME: Carries the target location ID.
using System;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.Locations.Requests.Commands;

[AuthorizeResource(ResourceKinds.Location, AuthorizationActions.Delete)]
public sealed record DeleteLocationCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; init; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
