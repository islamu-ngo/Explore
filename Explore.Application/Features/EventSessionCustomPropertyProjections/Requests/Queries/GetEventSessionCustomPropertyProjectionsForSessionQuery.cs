// ABOUTME: Query to retrieve all projection rows for a specific event session with optional exposure ceiling.
// ABOUTME: Used for admin inspection and future aggregate view composition.

using Explore.Application.Authorization;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Queries;

[AuthorizeResource(ResourceKinds.CustomPropertyProjection, AuthorizationActions.CustomPropertyProjections.View)]
public class GetEventSessionCustomPropertyProjectionsForSessionQuery : IRequest<BaseCommandResponse<IReadOnlyList<EventSessionCustomPropertyProjectionDto>>>, ISecureRequest
{
    public Guid EventSessionId { get; set; }
    public ExposureLevel? ExposureCeiling { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
