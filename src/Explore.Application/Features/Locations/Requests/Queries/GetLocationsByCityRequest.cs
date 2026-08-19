// ABOUTME: MediatR query for fetching locations in a given city.
// ABOUTME: Returns IEnumerable<LocationDto>.
using System.Collections.Generic;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Location;
using MediatR;

namespace Explore.Application.Features.Locations.Requests.Queries;

[AuthorizeResource(ResourceKinds.Location, AuthorizationActions.Locations.View)]
public class GetLocationsByCityRequest : IRequest<List<LocationListDto>>, ISecureRequest
{
    public required string City { get; set; }
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        TenantId == Guid.Empty
        ? null
        : new TenantScopedAuthorizationFacts(TenantId);
}
