// ABOUTME: Command request for updating an event-local custom property definition after instantiation.
// ABOUTME: Allows organizers to customize instantiated definitions for their specific event needs (task 5.6).

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomProperties.Requests.Commands;

[AuthorizeResource("tenant", AuthorizationActions.Update)]
public class UpdateEventCustomPropertyDefinitionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateEventCustomPropertyDefinitionDto DefinitionDto { get; set; }

    string? ISecureRequest.ResourceId => DefinitionDto.Id.ToString();
}
