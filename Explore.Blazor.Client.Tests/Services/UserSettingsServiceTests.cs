// ABOUTME: Unit tests for authenticated user settings reads through Refit.
// ABOUTME: Verifies string-enum setting sources from the API no longer break Blazor deserialization.

using System.Net;
using System.Text;
using Explore.Blazor.Client.Services;
using Microsoft.JSInterop;
using Refit;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class UserSettingsServiceTests
{
    [Test]
    public async Task GetSettingsAsync_UsesRefitAndMapsStringEnumSource_WhenAuthenticated()
    {
        using var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
              "category": "AiAssistantPreferences",
              "settings": [
                {
                  "key": "ai_assistant_preferences.show_navbar_button",
                  "value": "true",
                  "settingValueTypeId": 1,
                  "settingValueTypeCode": "Boolean",
                  "settingValueTypeName": "Boolean",
                  "source": "UserPreference",
                  "isLocked": false,
                  "canEdit": true
                }
              ]
            }
            """, Encoding.UTF8, "application/json")
        });
        using var client = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://client.test")
        };
        var api = RestService.For<IUserSettingsApi>(client);
        var authState = Substitute.For<IAuthStateService>();
        authState.IsAuthenticatedAsync().Returns(Task.FromResult(true));
        var service = new UserSettingsService(
            api,
            Substitute.For<IEventApiClient>(),
            authState,
            Substitute.For<IJSRuntime>(),
            Substitute.For<ILogger<UserSettingsService>>());

        var result = await service.GetSettingsAsync("AiAssistantPreferences");

        await Assert.That(handler.Requests.Single().RequestUri?.PathAndQuery)
            .IsEqualTo("/api/settings/user/AiAssistantPreferences");
        await Assert.That(result).IsNotNull();
        var setting = result!.Settings.Single();
        await Assert.That(setting.Key).IsEqualTo("ai_assistant_preferences.show_navbar_button");
        await Assert.That(setting.Source).IsEqualTo(SettingSource.UserPreference);
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }
}
