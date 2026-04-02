// ABOUTME: Command request for creating an ad-hoc event-local custom property definition.
// ABOUTME: Used when organizers add properties directly to an event without a template (task 6.3).

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomProperties.Requests.Commands;

[AuthorizeResource("tenant", AuthorizationActions.Update)]
public class CreateEventCustomPropertyDefinitionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventCustomPropertyDefinitionDto DefinitionDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
