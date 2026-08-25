// ABOUTME: Query to retrieve all projection rows for a specific event session with optional exposure ceiling.
// ABOUTME: Used for admin inspection and future aggregate view composition.

using Explore.Application.Authorization;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Queries;

[AuthorizeResource(ResourceKinds.CustomPropertyProjection, AuthorizationActions.CustomPropertyProjections.View)]
public sealed record GetEventSessionCustomPropertyProjectionsForSessionQuery : IRequest<BaseCommandResponse<IReadOnlyList<EventSessionCustomPropertyProjectionDto>>>, ISecureRequest
{
    public Guid EventSessionId { get; init; }
    public ExposureLevel? ExposureCeiling { get; init; }

    string? ISecureRequest.ResourceId => EventSessionId == Guid.Empty ? null : EventSessionId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        EventSessionId == Guid.Empty
        ? null
        : new CustomPropertyProjectionAuthorizationFacts(Guid.Empty, null, EventSessionId);
}
