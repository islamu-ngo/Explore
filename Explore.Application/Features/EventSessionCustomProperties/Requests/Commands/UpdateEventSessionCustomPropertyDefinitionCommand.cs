// ABOUTME: Command request for updating a session-local custom property definition after instantiation.
// ABOUTME: Allows organizers to customize instantiated definitions for their specific session needs.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;

[AuthorizeResource("tenant", AuthorizationActions.Update)]
public class UpdateEventSessionCustomPropertyDefinitionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateEventSessionCustomPropertyDefinitionDto DefinitionDto { get; set; }

    string? ISecureRequest.ResourceId => DefinitionDto.Id.ToString();
}
