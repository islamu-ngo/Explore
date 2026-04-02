// ABOUTME: Command request for deleting an event-local custom property definition and its values.
// ABOUTME: Hard deletes the definition so namespace+key can be reused without stale-row conflicts.

using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventCustomProperties.Requests.Commands;

[AuthorizeResource("tenant", AuthorizationActions.Update)]
public class DeleteEventCustomPropertyDefinitionCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
