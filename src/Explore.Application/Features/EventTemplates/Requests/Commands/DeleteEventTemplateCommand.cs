// ABOUTME: Command request for deleting an event template and its nested definitions and options.
// ABOUTME: Uses hard delete semantics so template keys can be reused without stale-row conflicts.

using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventTemplates.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class DeleteEventTemplateCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
