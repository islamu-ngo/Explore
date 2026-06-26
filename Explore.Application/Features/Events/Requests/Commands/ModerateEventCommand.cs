// ABOUTME: MediatR command for administratively hiding an event after moderation.
// ABOUTME: Uses a dedicated authorization action so moderation does not imply edit authority.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ModerateLight)]
public sealed class ModerateEventCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public const string DefaultReasonCode = "light_moderation";

    public Guid Id { get; set; }
    public string ReasonCode { get; set; } = DefaultReasonCode;
    public string? CorrelationId { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
