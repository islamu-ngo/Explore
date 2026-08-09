// ABOUTME: Resolves persisted event authorization context for event-tag assignment updates.
// ABOUTME: Keeps authorization lookup read-only so command mutation stays inside the handler.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventTags.Requests.Commands;

namespace Explore.Application.Features.EventTags.Authorization;

public sealed class UpdateEventTagsAuthorizationContextEnricher(
    IEventTagsRepository repository)
    : IAuthorizationContextEnricher<UpdateEventTagsCommand>
{
    public async Task<AuthorizationContext> ResolveAsync(
        UpdateEventTagsCommand request,
        CancellationToken cancellationToken)
    {
        var assignment = await repository.GetById(request.EventTagId);
        if (assignment is null)
        {
            throw new AuthorizationException(ResourceKinds.Event, AuthorizationActions.Update);
        }

        return new AuthorizationContext(assignment.EventId.ToString(), null);
    }
}
