// ABOUTME: Command request for deleting a session-local custom property definition and its values.
// ABOUTME: Hard deletes the definition so namespace+key can be reused without stale-row conflicts.

using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;

[AuthorizeResource("tenant", PermissionAction.Update)]
public class DeleteEventSessionCustomPropertyDefinitionCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
