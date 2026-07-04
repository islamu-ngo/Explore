// ABOUTME: Regression tests for event-template HAL collection mapping.
// ABOUTME: Verifies collection and row affordance links survive NSwag-to-UI model conversion.

using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Tests.Helpers;

public sealed class EventTemplateHalResourceExtensionsTests
{
    [Test]
    public async Task ToEventTemplatePaginatedResult_PreservesCollectionAndItemLinks()
    {
        var templateId = Guid.NewGuid();
        var collection = new HalCollectionResourceOfEventTemplateListDto
        {
            PageNumber = 2,
            PageSize = 10,
            TotalCount = 11,
            _links = new Dictionary<string, HalLink>
            {
                ["create"] = new() { Href = "/api/EventTemplate", Method = "POST", Title = "Create event template" }
            },
            _embedded = new HalCollectionEmbeddedOfEventTemplateListDto
            {
                Items =
                [
                    new HalResourceOfEventTemplateListDto
                    {
                        Id = templateId,
                        TenantId = Guid.NewGuid(),
                        TemplateKey = "conference",
                        DisplayName = "Conference",
                        EventTypeId = 1,
                        Version = 3,
                        IsPublished = true,
                        IsActive = true,
                        DefinitionCount = 4,
                        _links = new Dictionary<string, Anonymous40>
                        {
                            ["edit"] = new() { Href = $"/api/EventTemplate/{templateId}", Method = "PUT", Title = "Edit" },
                            ["delete"] = new() { Href = $"/api/EventTemplate/{templateId}", Method = "DELETE", Title = "Delete" }
                        }
                    }
                ]
            }
        };

        var result = collection.ToEventTemplatePaginatedResult();

        await Assert.That(result.PageNumber).IsEqualTo(2);
        await Assert.That(result.TotalCount).IsEqualTo(11);
        await Assert.That(result.HasHalLink("create")).IsTrue();
        await Assert.That(result.Links!["create"].Method).IsEqualTo("POST");
        await Assert.That(result.Items.Count).IsEqualTo(1);
        await Assert.That(result.Items[0].Id).IsEqualTo(templateId);
        await Assert.That(result.Items[0].DefinitionsCount).IsEqualTo(4);
        await Assert.That(result.Items[0].HasHalLink("edit")).IsTrue();
        await Assert.That(result.Items[0].HasHalLink("delete")).IsTrue();
        await Assert.That(result.Items[0].Links!["edit"].Method).IsEqualTo("PUT");
    }

    [Test]
    public async Task ToEventTemplatePaginatedResult_PreservesCollectionLinks_WhenCollectionIsEmpty()
    {
        var collection = new HalCollectionResourceOfEventTemplateListDto
        {
            PageNumber = 1,
            PageSize = 20,
            TotalCount = 0,
            _links = new Dictionary<string, HalLink>
            {
                ["create"] = new() { Href = "/api/EventTemplate", Method = "POST" }
            },
            _embedded = new HalCollectionEmbeddedOfEventTemplateListDto { Items = [] }
        };

        var result = collection.ToEventTemplatePaginatedResult();

        await Assert.That(result.Items).IsEmpty();
        await Assert.That(result.HasHalLink("create")).IsTrue();
    }
}
