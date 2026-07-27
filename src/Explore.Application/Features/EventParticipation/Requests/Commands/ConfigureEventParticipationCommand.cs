// ABOUTME: Authorized command that reconfigures one event's explicit participation policy.
// ABOUTME: Carries the configuration concurrency stamp separately from ordinary event-shell updates.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventParticipation.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Update)]
public sealed class ConfigureEventParticipationCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventId { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
    public required ConfigureEventParticipationDto ParticipationConfiguration { get; set; }

    string? ISecureRequest.ResourceId => EventId.ToString();
}
