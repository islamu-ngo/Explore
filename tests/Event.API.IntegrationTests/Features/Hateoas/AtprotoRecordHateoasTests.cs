// ABOUTME: Focused HAL policy tests for read-only AT Protocol record discovery.
// ABOUTME: Ensures navigation remains available without create, update, or delete affordances.

using Explore.API.Hateoas;
using Explore.API.Hateoas.Policies;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.Hateoas;

namespace Event.Api.IntegrationTests.Features.Hateoas;

public sealed class AtprotoRecordHateoasTests
{
    [Test]
    public async Task DetailPolicy_EmitsOnlyReadNavigation()
    {
        var links = new AtprotoRecordDetailLinkPolicy().GetLinks(new AtprotoRecordDto
        {
            Id = Guid.NewGuid(),
            Did = "did:plc:read-only",
            Collection = "community.lexicon.calendar.event",
            RecordKey = "record-key"
        }, user: null).ToList();

        await Assert.That(links.Select(link => link.Rel))
            .IsEquivalentTo([LinkRelations.Self, LinkRelations.Collection, "did"]);
        await Assert.That(links.All(link => link.Method == "GET")).IsTrue();
    }

    [Test]
    public async Task CollectionPolicy_EmitsNoMutationAffordance()
    {
        var policy = new AtprotoRecordCollectionLinkPolicy();
        var collectionLinks = policy.GetCollectionLinks(user: null).ToList();
        var itemLinks = policy.GetItemLinks(new AtprotoRecordListDto
        {
            Id = Guid.NewGuid(),
            Did = "did:plc:read-only",
            Collection = "community.lexicon.calendar.event",
            RecordKey = "record-key"
        }, user: null).ToList();

        await Assert.That(collectionLinks).IsEmpty();
        await Assert.That(itemLinks.Select(link => link.Rel))
            .IsEquivalentTo([LinkRelations.Self, "did"]);
        await Assert.That(itemLinks.All(link => link.Method == "GET")).IsTrue();
    }
}
