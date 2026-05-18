// ABOUTME: Command request for deleting an event session template and its nested definitions and options.
// ABOUTME: Uses hard delete semantics so session template keys can be reused without stale-row conflicts.

using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventSessionTemplates.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class DeleteEventSessionTemplateCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
