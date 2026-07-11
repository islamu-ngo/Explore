// ABOUTME: Playwright critical flow for attendee registration and confirmation email delivery.
// ABOUTME: Creates and verifies backend state only through the generated API client.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.E2ETests.Fixtures;
using Explore.Blazor.Client.E2ETests.Seeds;

namespace Explore.Blazor.Client.E2ETests.Flows.CriticalFlows;

[Category(E2ETestCategories.E2E)]
[Category(E2ETestCategories.Email)]
[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerClass, SharedType.PerTestSession])]
[NotInParallel("E2EAppHostDb")]
[ParallelLimiter<BrowserParallelLimit>]
public partial class RegistrationFlowTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright)
{
    private const string TestRegistrantEmail = "user@test.islamu.org";

    [Test]
    [Timeout(420_000)]
    public async Task RegistrationFlowLoginBrowseRegisterConfirmationMyRegistrations()
    {
        await appHost.ClearMailpitMessagesAsync();
        var adminTokens = await appHost.GetTestAdminTokensAsync();
        var adminApi = appHost.CreateApiClient(adminTokens.AccessToken);
        var scenario = await RegistrationScenarioSeed.SeedAsync(adminApi);
        var userApi = appHost.CreateApiClient(await appHost.GetTestUserAccessTokenAsync());

        EventApiScenario.EnsureSuccess(await userApi.SyncUserAsync(), "syncing the E2E registrant");
        var visibleEvent = await userApi.GetEventByIdAsync(scenario.EventId);
        await Assert.That(visibleEvent.Title).IsEqualTo(scenario.EventTitle);

        var registrationScopes = await userApi.GetRegistrationScopesAsync();
        var registration = await userApi.CreateEventRegistrationAsync(body: new CreateEventRegistrationDto
        {
            EventId = scenario.EventId,
            RegistrationScopeId = EventApiScenario.FindLookup(
                registrationScopes,
                value => value.MasterCode,
                value => value.Id,
                "session_selection"),
            SelectedSessionIds = [scenario.SessionId],
            ShareEmailWithOrganizer = false
        });
        EventApiScenario.EnsureSuccess(registration, "registering for the E2E session");

        var persisted = await userApi.GetRegistrationsBySessionAsync(scenario.SessionId);
        await Assert.That(persisted.Count(candidate =>
            candidate.EventId == scenario.EventId && candidate.EventSessionId == scenario.SessionId)).IsEqualTo(1);

        await AssertRegistrationConfirmationEmailDispatchedAsync(adminApi, userApi, scenario);
    }

    private async Task AssertRegistrationConfirmationEmailDispatchedAsync(
        IEventApiClient adminApi,
        IEventApiClient userApi,
        RegistrationScenarioSeed.Result scenario)
    {
        var message = await WaitForConfirmationEmailAsync(scenario.EventTitle);
        var text = await appHost.GetMailpitMessageTextAsync(message.Id);
        var html = await appHost.GetMailpitMessageHtmlAsync(message.Id);
        var headers = await appHost.GetMailpitMessageHeadersAsync(message.Id);
        var dispatchId = Guid.Parse(GetSingleHeaderValue(headers, "X-Email-Dispatch-ID"));
        var unsubscribeUrl = GetSingleHeaderValue(headers, "List-Unsubscribe").Trim('<', '>');

        await Assert.That(text).Contains("has been received");
        await Assert.That(text).Contains(scenario.EventTitle);
        await Assert.That(GetSingleHeaderValue(headers, "X-Correlation-ID")).IsNotEmpty();
        await Assert.That(GetSingleHeaderValue(headers, "List-Unsubscribe-Post"))
            .IsEqualTo("List-Unsubscribe=One-Click");
        AssertUnsubscribeUrlShape(unsubscribeUrl);
        await Assert.That(text).Contains(unsubscribeUrl);
        await Assert.That(html).Contains(unsubscribeUrl);

        var statuses = await adminApi.GetEmailDispatchStatusAsync(scenario.TenantId, limit: 100);
        var status = statuses._embedded?.Items?.SingleOrDefault(candidate => candidate.OutboxId == dispatchId);
        await Assert.That(status).IsNotNull();
        await Assert.That(status!.DeliveryStatus).IsEqualTo("Sent");
        await Assert.That(status.AttemptCount ?? 0).IsGreaterThanOrEqualTo(1);
        await Assert.That(status.DeliveredAt).IsNotNull();

        await AssertUnsubscribeTokenDisablesPreferenceAsync(unsubscribeUrl, userApi);
    }

    private async Task<MailpitContainerFixture.MailpitMessageSummary> WaitForConfirmationEmailAsync(string eventTitle)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var message = (await appHost.GetMailpitMessagesAsync()).FirstOrDefault(candidate =>
                candidate.Subject.Contains(eventTitle, StringComparison.OrdinalIgnoreCase) &&
                candidate.To.Any(address =>
                    string.Equals(address.Address, TestRegistrantEmail, StringComparison.OrdinalIgnoreCase)));
            if (message is not null)
            {
                return message;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException("Mailpit did not receive the registration confirmation email.");
    }

    private async Task AssertUnsubscribeTokenDisablesPreferenceAsync(
        string unsubscribeUrl,
        IEventApiClient userApi)
    {
        var page = await playwright.CreatePageAsync(nameof(AssertUnsubscribeTokenDisablesPreferenceAsync));
        try
        {
            var response = await page.Context.APIRequest.PostAsync(BuildReachableUnsubscribeUrl(unsubscribeUrl));
            await Assert.That(response.Status).IsEqualTo((int)HttpStatusCode.OK);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(AssertUnsubscribeTokenDisablesPreferenceAsync));
        }

        var preferences = await userApi.GetCurrentUserNotificationPreferencesAsync();
        var registrationCells = preferences.Cells?.Where(candidate =>
            candidate.CategoryCode.Contains("registration", StringComparison.OrdinalIgnoreCase)).ToArray() ?? [];
        await Assert.That(registrationCells).IsNotEmpty();
        await Assert.That(registrationCells.All(candidate => candidate.IsEnabled == false)).IsTrue();
    }

    private string BuildReachableUnsubscribeUrl(string unsubscribeUrl)
    {
        var uri = new Uri(unsubscribeUrl, UriKind.Absolute);
        return $"{appHost.ApiBaseUrl}{uri.AbsolutePath}{uri.Query}";
    }

    private static string GetSingleHeaderValue(
        IReadOnlyDictionary<string, string[]> headers,
        string name)
    {
        var header = headers.FirstOrDefault(candidate =>
            string.Equals(candidate.Key, name, StringComparison.OrdinalIgnoreCase));
        return header.Value is { Length: > 0 }
            ? header.Value[0]
            : throw new InvalidOperationException($"Mailpit message did not contain expected '{name}' header.");
    }

    private static void AssertUnsubscribeUrlShape(string unsubscribeUrl)
    {
        if (!Uri.TryCreate(unsubscribeUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || (!string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
            || !string.Equals(uri.AbsolutePath, "/api/email/unsubscribe", StringComparison.Ordinal)
            || !uri.Query.StartsWith("?token=", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unexpected unsubscribe URL shape: {unsubscribeUrl}");
        }
    }
}
