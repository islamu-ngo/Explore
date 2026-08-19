// ABOUTME: MediatR command for route-ID EventSeries PATCH updates.
// ABOUTME: Carries If-Match concurrency stamp and grouped update payload.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSeries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSeries.Requests.Commands;

[AuthorizeResource(ResourceKinds.Actor, AuthorizationActions.Update)]
public class UpdateEventSeriesCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventSeriesId { get; set; }
    public Guid ActorId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
    public required UpdateEventSeriesDto EventSeriesDto { get; set; }

    string? ISecureRequest.ResourceId => ActorId.ToString();
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new ActorAuthorizationFacts(TenantId, ActorId);
}
