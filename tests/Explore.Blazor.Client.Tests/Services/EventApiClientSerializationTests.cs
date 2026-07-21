// ABOUTME: Regression coverage for JSON contracts consumed by the generated Event API client.
// ABOUTME: Proves string enums nested inside dictionary response values deserialize correctly.

using System.Net;
using System.Text;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class EventApiClientSerializationTests
{
    [Test]
    public async Task GetHomeDiscoveryAsyncDeserializesStringEnumDictionaryValues()
    {
        const string responseBody = """
            {
              "schemaVersion": 1,
              "context": {
                "mode": "All",
                "selectedAreaDisplayName": "All events",
                "availableAreas": []
              },
              "hero": [],
              "upcomingInArea": [],
              "mostViewedInArea": [],
              "mostViewedOnline": [],
              "curatedSections": [],
              "recentlyAdded": [],
              "sectionStatuses": {
                "hero": "Available"
              },
              "generatedAtUtc": "2026-07-16T10:00:00Z"
            }
            """;
        using var httpClient = new HttpClient(new StaticResponseHandler(responseBody))
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var client = new EventApiClient(httpClient);

        var result = await client.GetHomeDiscoveryAsync();

        await Assert.That(result.SectionStatuses).IsNotNull();
        await Assert.That(result.SectionStatuses!["hero"]).IsEqualTo(HomeDiscoverySectionStatus.Available);
        await Assert.That(result.Context?.Mode).IsEqualTo(HomeDiscoveryMode.All);
    }

    [Test]
    public async Task GetInstanceAtprotoFederationSettingsAsyncDeserializesStringSettingSource()
    {
        const string responseBody = """
            {
              "category": "AtprotoFederation",
              "settings": [
                {
                  "key": "atproto.eventPublishing.capability",
                  "value": "Enabled",
                  "settingValueTypeId": 1,
                  "settingValueTypeCode": "String",
                  "settingValueTypeName": "String",
                  "source": "SystemLocked",
                  "isLocked": true,
                  "isLockable": true,
                  "canEdit": true
                }
              ],
              "_links": {}
            }
            """;
        using var httpClient = new HttpClient(new StaticResponseHandler(responseBody))
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var client = new EventApiClient(httpClient);

        var result = await client.GetInstanceAtprotoFederationSettingsAsync();

        var setting = await Assert.That(result.Settings).HasSingleItem();
        await Assert.That(setting.Source).IsEqualTo(SettingSource.SystemLocked);
    }

    private sealed class StaticResponseHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
    }
}
