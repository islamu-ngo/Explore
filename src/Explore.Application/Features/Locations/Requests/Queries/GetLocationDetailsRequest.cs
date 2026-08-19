// ABOUTME: MediatR query request for fetching a single location by ID.
// ABOUTME: Returns LocationDto.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Location;
using MediatR;

namespace Explore.Application.Features.Locations.Requests.Queries;

[AuthorizeResource(ResourceKinds.Location, AuthorizationActions.Locations.View)]
public class GetLocationDetailsRequest : IRequest<LocationDto>, ISecureRequest
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => Id == Guid.Empty ? null : Id.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        TenantId == Guid.Empty
        ? null
        : new TenantScopedAuthorizationFacts(TenantId);
}
