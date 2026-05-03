// ABOUTME: Query to retrieve all projection rows for a specific event, optionally filtered by exposure ceiling.
// ABOUTME: Used for admin inspection and as a dependency for Milestone F aggregate view composition.

using Explore.Application.Authorization;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventCustomPropertyProjections.Requests.Queries;

[AuthorizeResource(ResourceKinds.CustomPropertyProjection, AuthorizationActions.CustomPropertyProjections.View)]
public class GetEventCustomPropertyProjectionsForEventQuery : IRequest<BaseCommandResponse<IReadOnlyList<EventCustomPropertyProjectionDto>>>, ISecureRequest
{
    public Guid EventId { get; set; }
    public ExposureLevel? ExposureCeiling { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
