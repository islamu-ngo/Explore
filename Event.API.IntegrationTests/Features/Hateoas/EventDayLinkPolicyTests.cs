// ABOUTME: Regression tests for EventDay HAL link policy authorization metadata.
// ABOUTME: Prevents event-detail day requests from failing during collection link generation.

namespace Event.Api.IntegrationTests.Features.Hateoas;

using Explore.API.Hateoas.Policies;
using Explore.Application.Authorization;
using Explore.Application.Hateoas;

public sealed class EventDayLinkPolicyTests
{
    [Test]
    public async Task CollectionCreateLink_ShouldResolveEventDayResourceKind()
    {
        var link = new EventDayCollectionLinkPolicy()
            .GetCollectionLinks(user: null)
            .Single(definition => definition.Rel == LinkRelations.Create);

        await Assert.That(link.PermissionResourceKind).IsEqualTo(ResourceKinds.EventDay);
        await Assert.That(link.PermissionAction).IsEqualTo(AuthorizationActions.Create);
    }
}
