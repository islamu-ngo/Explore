// ABOUTME: Regression tests for EventAgendaItem HAL link policy authorization metadata.
// ABOUTME: Prevents event-detail child agenda requests from failing during link generation.

namespace Event.Api.IntegrationTests.Features.Hateoas;

using Explore.API.Hateoas.Policies;
using Explore.Application.Authorization;
using Explore.Application.Hateoas;

public sealed class EventAgendaItemLinkPolicyTests
{
    [Test]
    public async Task CollectionCreateLink_ShouldResolveAgendaItemResourceKind()
    {
        var link = new EventAgendaItemCollectionLinkPolicy()
            .GetCollectionLinks(user: null)
            .Single(definition => definition.Rel == "create");

        await Assert.That(link.PermissionResourceKind).IsEqualTo(ResourceKinds.EventAgendaItem);
        await Assert.That(link.PermissionAction).IsEqualTo(AuthorizationActions.Create);
    }
}
