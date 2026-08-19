// ABOUTME: Query for event publish-readiness diagnostics used by management clients before publish.
// ABOUTME: Carries event resource authorization metadata so readiness checks do not bypass policy.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Update)]
public class GetEventPublishReadinessRequest : IRequest<EventPublishReadinessDto?>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new EventScopedAuthorizationFacts(Guid.Empty, Id);
}
