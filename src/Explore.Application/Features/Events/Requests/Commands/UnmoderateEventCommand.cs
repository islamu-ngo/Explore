// ABOUTME: MediatR command for restoring reversibly moderated events to Published.
// ABOUTME: Uses explicit unmoderation authorization separate from event editing authority.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.Unmoderate)]
public sealed class UnmoderateEventCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public const string DefaultReasonCode = "unmoderation";

    public Guid Id { get; set; }
    public string ReasonCode { get; set; } = DefaultReasonCode;
    public string? CorrelationId { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
