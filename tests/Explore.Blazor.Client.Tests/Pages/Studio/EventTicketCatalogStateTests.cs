// ABOUTME: Tests fail-closed parsing of the generated extension-data ticket catalog HAL resource.
// ABOUTME: Verifies exact root relations and item-ID-bound edit/delete presentation affordances.

using System.Text.Json;
using Explore.Blazor.Client.Pages.Studio;

namespace Explore.Blazor.Client.Tests.Pages.Studio;

public sealed class EventTicketCatalogStateTests
{
    [Test]
    public async Task TryParse_ValidCatalog_PreservesDataAndExactRelations()
    {
        var eventId = Guid.CreateVersion7();
        var ticketTypeId = Guid.CreateVersion7();
        var poolId = Guid.CreateVersion7();
        var resource = Resource($$"""
            {
              "eventId": "{{eventId}}",
              "catalogId": "{{Guid.CreateVersion7()}}",
              "versionNumber": 2,
              "currencyCode": "EUR",
              "statusId": 1,
              "statusCode": "DRAFT",
              "statusName": "Draft",
              "_links": {
                "self": { "href": "/api/events/{{eventId}}/ticketing", "method": "GET" },
                "create-type": { "href": "/api/events/{{eventId}}/ticketing/ticket-types", "method": "POST" },
                "publish": { "href": "/api/events/{{eventId}}/ticketing/publish", "method": "POST" }
              },
              "_embedded": {
                "ticket-types": [{
                  "id": "{{ticketTypeId}}",
                  "name": "General admission",
                  "ticketPricingModeId": 1,
                  "fixedPriceMinor": 1200,
                  "minimumPriceMinor": null,
                  "suggestedPriceMinor": null,
                  "participantDataCollectionModeId": 2,
                  "capacityPoolId": "{{poolId}}",
                  "minimumAge": 16,
                  "maximumAge": 90,
                  "requiresGuardian": false,
                  "requiresApproval": true,
                  "perOrderLimit": 4,
                  "perAccountLimit": 6,
                  "perVerifiedContactLimit": 2,
                  "perBookingPartyLimit": 8,
                  "entitlements": [{
                    "entitlementScopeTypeId": 1,
                    "eventDayId": null,
                    "eventSessionId": null,
                    "includedQuantity": 1,
                    "entitlementSelectionRuleId": 1
                  }],
                  "_links": {
                    "edit": { "href": "/api/events/{{eventId}}/ticketing/ticket-types/{{ticketTypeId}}", "method": "PUT" },
                    "delete": { "href": "/api/events/{{eventId}}/ticketing/ticket-types/{{Guid.CreateVersion7()}}", "method": "DELETE" }
                  }
                }],
                "capacity-pools": [{
                  "id": "{{poolId}}",
                  "name": "Main hall",
                  "maximumQuantity": 200,
                  "holdDurationSeconds": 900,
                  "capacityHoldPolicyId": 2,
                  "capacityHoldPolicyCode": "TIMED_HOLD_ON_SELECTION",
                  "capacityHoldPolicyName": "Timed hold on selection",
                  "capacityOversellPolicyId": 1,
                  "isActive": true,
                  "_links": {
                    "edit": { "href": "/api/events/{{eventId}}/ticketing/capacity-pools/{{poolId}}", "method": "PUT" }
                  }
                }]
              }
            }
            """);

        var parsed = EventTicketCatalogState.TryParse(resource, out var state);

        await Assert.That(parsed).IsTrue();
        await Assert.That(state).IsNotNull();
        await Assert.That(state!.EventId).IsEqualTo(eventId);
        await Assert.That(state.HasLink("create-type")).IsTrue();
        await Assert.That(state.HasLink("create-pool")).IsFalse();
        await Assert.That(state.TicketTypes.Single().HasLink("edit")).IsTrue();
        await Assert.That(state.TicketTypes.Single().HasLink("delete")).IsFalse();
        await Assert.That(state.TicketTypes.Single().ToRequest().Entitlements!.Single().IncludedQuantity).IsEqualTo(1);
        await Assert.That(state.CapacityPools.Single().HasLink("edit")).IsTrue();
        await Assert.That(state.CapacityPools.Single().CapacityHoldPolicyCode).IsEqualTo("TIMED_HOLD_ON_SELECTION");
    }

    [Test]
    [Arguments("{\"eventId\":\"00000000-0000-0000-0000-000000000000\",\"_links\":{},\"_embedded\":{\"ticket-types\":[],\"capacity-pools\":[]}}")]
    [Arguments("{\"eventId\":\"01938b55-f390-7e0f-a3d7-62972ff97bd0\",\"_embedded\":{\"ticket-types\":[],\"capacity-pools\":[]}}")]
    [Arguments("{\"eventId\":\"01938b55-f390-7e0f-a3d7-62972ff97bd0\",\"_links\":{\"self\":{\"href\":\"/api\"}}}")]
    public async Task TryParse_MissingRequiredDataOrLinks_FailsClosed(string json)
    {
        var parsed = EventTicketCatalogState.TryParse(Resource(json), out var state);

        await Assert.That(parsed).IsFalse();
        await Assert.That(state).IsNull();
    }

    [Test]
    public async Task TryParse_ReadOnlyEmbeddedItemWithoutLinks_RemainsVisibleWithoutActions()
    {
        var eventId = Guid.CreateVersion7();
        var ticketTypeId = Guid.CreateVersion7();
        var resource = Resource($$"""
            {
              "eventId": "{{eventId}}",
              "currencyCode": "EUR",
              "_links": { "self": { "href": "/api/events/{{eventId}}/ticketing", "method": "GET" } },
              "_embedded": {
                "ticket-types": [{
                  "id": "{{ticketTypeId}}",
                  "name": "Published ticket",
                  "ticketPricingModeId": 2,
                  "participantDataCollectionModeId": 1,
                  "requiresGuardian": false,
                  "requiresApproval": false,
                  "entitlements": [{ "entitlementScopeTypeId": 1, "includedQuantity": 1, "entitlementSelectionRuleId": 1 }]
                }],
                "capacity-pools": []
              }
            }
            """);

        var parsed = EventTicketCatalogState.TryParse(resource, out var state);

        await Assert.That(parsed).IsTrue();
        await Assert.That(state!.TicketTypes.Single().HasLink("edit")).IsFalse();
        await Assert.That(state.TicketTypes.Single().HasLink("delete")).IsFalse();
    }

    [Test]
    public async Task TryParse_ItemMutationHrefDoesNotEndWithItemId_OmitsAction()
    {
        var eventId = Guid.CreateVersion7();
        var ticketTypeId = Guid.CreateVersion7();
        var resource = Resource($$"""
            {
              "eventId": "{{eventId}}",
              "currencyCode": "EUR",
              "_links": { "self": { "href": "/api/events/{{eventId}}/ticketing", "method": "GET" } },
              "_embedded": {
                "ticket-types": [{
                  "id": "{{ticketTypeId}}",
                  "name": "General admission",
                  "ticketPricingModeId": 1,
                  "participantDataCollectionModeId": 1,
                  "requiresGuardian": false,
                  "requiresApproval": false,
                  "entitlements": [{ "entitlementScopeTypeId": 1, "includedQuantity": 1, "entitlementSelectionRuleId": 1 }],
                  "_links": { "edit": { "href": "/api/items/{{ticketTypeId}}/edit", "method": "PUT" } }
                }],
                "capacity-pools": []
              }
            }
            """);

        var parsed = EventTicketCatalogState.TryParse(resource, out var state);

        await Assert.That(parsed).IsTrue();
        await Assert.That(state!.TicketTypes.Single().HasLink("edit")).IsFalse();
    }

    private static HalResourceOfEventTicketCatalogManagementDto Resource(string json)
    {
        using var document = JsonDocument.Parse(json);
        var resource = new HalResourceOfEventTicketCatalogManagementDto();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            resource.AdditionalProperties[property.Name] = property.Value.Clone();
        }

        return resource;
    }
}
