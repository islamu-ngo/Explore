// ABOUTME: Command request for creating an ad-hoc session-local custom property definition.
// ABOUTME: Used when organizers add properties directly to a session without a template.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class CreateEventSessionCustomPropertyDefinitionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventSessionCustomPropertyDefinitionDto DefinitionDto { get; set; }

    string? ISecureRequest.ResourceId => null;
}
