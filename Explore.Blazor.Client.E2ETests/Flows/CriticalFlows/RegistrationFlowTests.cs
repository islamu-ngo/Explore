// ABOUTME: Playwright critical-flow scaffold for the attendee registration journey.
// ABOUTME: Documents the browser path from event discovery through My Registrations.

using System.Text.Json;
using Explore.Blazor.Client.E2ETests.Fixtures;
using Explore.Blazor.Client.E2ETests.Seeds;
using Explore.Domain;
using Explore.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace Explore.Blazor.Client.E2ETests.Flows.CriticalFlows;

[Category(E2ETestCategories.E2E)]
[Category(E2ETestCategories.Email)]
[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerTestSession, SharedType.PerTestSession])]
[NotInParallel("E2EAppHostDb")]
[ParallelLimiter<BrowserParallelLimit>]
public partial class RegistrationFlowTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright)
{
    private const string TestRegistrantEmail = "user@test.islamu.org";
    private const string TenantSlugHeaderName = "X-Tenant-Slug";
    private static readonly float ApiRequestTimeoutMilliseconds = (float)TimeSpan.FromSeconds(90).TotalMilliseconds;
    private static readonly TimeSpan ApiRequestRetryWindow = TimeSpan.FromMinutes(2);

    [Test]
    [Timeout(420_000)]
    public async Task RegistrationFlowLoginBrowseRegisterConfirmationMyRegistrations()
    {
        await appHost.ResetDatabaseAsync();
        await appHost.ClearMailpitMessagesAsync();

        RegistrationScenarioSeed.Result scenario;
        await using (var context = appHost.CreateDbContext())
        {
            scenario = await RegistrationScenarioSeed.SeedAsync(context);
        }

        var page = await playwright.CreatePageAsync(nameof(RegistrationFlowLoginBrowseRegisterConfirmationMyRegistrations));
        try
        {
            var accessToken = await appHost.GetTestUserAccessTokenAsync();
            await AssertRegistrationEventIsVisibleThroughApiAsync(page, scenario, accessToken);
            await EnsureAuthenticatedUserIsSyncedThroughApiAsync(page, scenario, accessToken);
            await RegisterForSeededSessionThroughApiAsync(page, scenario, accessToken);
            await AssertRegistrationPersistedAsync(scenario);
            await AssertRegistrationConfirmationEmailDispatchedAsync(scenario);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(RegistrationFlowLoginBrowseRegisterConfirmationMyRegistrations));
        }
    }

    private async Task AssertRegistrationEventIsVisibleThroughApiAsync(
        IPage page,
        RegistrationScenarioSeed.Result scenario,
        string accessToken)
    {
        var response = await SendApiRequestWithTransientRetryAsync(
            () => page.Context.APIRequest.GetAsync(
                $"{appHost.ApiBaseUrl}/api/event/{scenario.EventId}",
                CreateTenantRequestOptions(scenario.TenantSlug, accessToken: accessToken)));

        if (response.Status != (int)HttpStatusCode.OK)
        {
            var body = await response.TextAsync();
            throw new InvalidOperationException($"Registration event API visibility failed with status {response.Status}: {body}");
        }
        await Assert.That(await response.TextAsync()).Contains(scenario.EventTitle);
    }

    private async Task EnsureAuthenticatedUserIsSyncedThroughApiAsync(
        IPage page,
        RegistrationScenarioSeed.Result scenario,
        string accessToken)
    {
        var response = await SendApiRequestWithTransientRetryAsync(
            () => page.Context.APIRequest.PostAsync(
                $"{appHost.ApiBaseUrl}/api/user/sync",
                CreateTenantRequestOptions(scenario.TenantSlug, accessToken: accessToken)));

        if (response.Status != (int)HttpStatusCode.OK)
        {
            var body = await response.TextAsync();
            throw new InvalidOperationException($"User sync through API failed with status {response.Status}: {body}");
        }

        await AssertResponseSuccessAsync(response, "User sync through API");
    }

    private async Task RegisterForSeededSessionThroughApiAsync(
        IPage page,
        RegistrationScenarioSeed.Result scenario,
        string accessToken)
    {
        var response = await SendApiRequestWithTransientRetryAsync(
            () => page.Context.APIRequest.PostAsync(
                $"{appHost.ApiBaseUrl}/api/eventregistration",
                CreateTenantRequestOptions(
                    scenario.TenantSlug,
                    accessToken,
                    new
                    {
                        eventId = scenario.EventId,
                        registrationScopeId = (int)Explore.Domain.Enums.RegistrationScopeEnum.SessionSelection,
                        selectedSessionIds = new[] { scenario.SessionId },
                        shareEmailWithOrganizer = false
                    })));

        if (response.Status != (int)HttpStatusCode.OK)
        {
            var body = await response.TextAsync();
            throw new InvalidOperationException($"Registration through API failed with status {response.Status}: {body}");
        }

        await AssertResponseSuccessAsync(response, "Registration through API");
    }

    private static APIRequestContextOptions CreateTenantRequestOptions(
        string tenantSlug,
        string? accessToken = null,
        object? dataObject = null)
    {
        var headers = new Dictionary<string, string>
        {
            [TenantSlugHeaderName] = tenantSlug
        };

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            headers["Authorization"] = $"Bearer {accessToken}";
        }

        var options = new APIRequestContextOptions
        {
            Timeout = ApiRequestTimeoutMilliseconds,
            Headers = headers
        };

        if (dataObject is not null)
        {
            options.DataObject = dataObject;
        }

        return options;
    }

    private static async Task AssertResponseSuccessAsync(IAPIResponse response, string operation)
    {
        var body = await response.TextAsync();
        using var document = JsonDocument.Parse(body);

        if (document.RootElement.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True)
        {
            return;
        }

        throw new InvalidOperationException($"{operation} returned an unsuccessful command response: {body}");
    }

    private static async Task<IAPIResponse> SendApiRequestWithTransientRetryAsync(Func<Task<IAPIResponse>> send)
    {
        var deadline = DateTimeOffset.UtcNow.Add(ApiRequestRetryWindow);

        while (true)
        {
            var response = await send();
            if (!IsTransientGatewayStatus(response.Status) || DateTimeOffset.UtcNow >= deadline)
            {
                return response;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }

    private static bool IsTransientGatewayStatus(int status)
    {
        return status is (int)HttpStatusCode.RequestTimeout
            or (int)HttpStatusCode.BadGateway
            or (int)HttpStatusCode.ServiceUnavailable
            or (int)HttpStatusCode.GatewayTimeout;
    }

    private async Task AssertRegistrationPersistedAsync(RegistrationScenarioSeed.Result scenario)
    {
        await using var context = appHost.CreateDbContext();

        var intentCount = await context.EventRegistrationIntents
            .IgnoreQueryFilters()
            .CountAsync(x => x.EventId == scenario.EventId && x.TenantId == scenario.TenantId);
        var childCount = await context.EventRegistrations
            .IgnoreQueryFilters()
            .CountAsync(x => x.EventSessionId == scenario.SessionId && x.TenantId == scenario.TenantId);

        await Assert.That(intentCount).IsEqualTo(1);
        await Assert.That(childCount).IsEqualTo(1);
    }

    private async Task AssertRegistrationConfirmationEmailDispatchedAsync(RegistrationScenarioSeed.Result scenario)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        EmailDispatchOutbox? dispatched = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            await using var context = appHost.CreateDbContext();
            dispatched = await context.EmailDispatchOutbox
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == scenario.TenantId
                    && x.EventId == scenario.EventId
                    && x.Kind == EmailDispatchKind.RegistrationConfirmation);

            if (dispatched?.Status == EmailDispatchStatus.Sent)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        await Assert.That(dispatched).IsNotNull();
        await Assert.That(dispatched!.Status).IsEqualTo(EmailDispatchStatus.Sent);
        await Assert.That(dispatched.RecipientEmail).IsEqualTo(TestRegistrantEmail);
        await Assert.That(dispatched.Subject).Contains(scenario.EventTitle);

        await using (var context = appHost.CreateDbContext())
        {
            var attemptCount = await context.EmailDispatchAttempts
                .IgnoreQueryFilters()
                .CountAsync(x => x.EmailDispatchOutboxId == dispatched.Id
                    && x.Outcome == EmailDispatchAttemptOutcome.Succeeded);
            var receiptCount = await context.EmailDispatchReceipts
                .IgnoreQueryFilters()
                .CountAsync(x => x.EmailDispatchOutboxId == dispatched.Id
                    && x.Status == EmailDispatchReceiptStatus.Completed);

            await Assert.That(attemptCount).IsGreaterThanOrEqualTo(1);
            await Assert.That(receiptCount).IsEqualTo(1);
        }

        var messages = await appHost.GetMailpitMessagesAsync();
        var message = messages.FirstOrDefault(x =>
            x.Subject.Contains(scenario.EventTitle, StringComparison.OrdinalIgnoreCase)
            && x.To.Any(address => string.Equals(address.Address, TestRegistrantEmail, StringComparison.OrdinalIgnoreCase)));

        await Assert.That(message).IsNotNull();

        var text = await appHost.GetMailpitMessageTextAsync(message!.Id);
        await Assert.That(text).Contains("has been received");
        await Assert.That(text).Contains(scenario.EventTitle);

        var html = await appHost.GetMailpitMessageHtmlAsync(message.Id);
        var headers = await appHost.GetMailpitMessageHeadersAsync(message.Id);
        var unsubscribeUrl = GetSingleHeaderValue(headers, "List-Unsubscribe").Trim('<', '>');

        await Assert.That(GetSingleHeaderValue(headers, "X-Email-Dispatch-ID")).IsEqualTo(dispatched.Id.ToString());
        await Assert.That(GetSingleHeaderValue(headers, "X-Correlation-ID")).IsNotEmpty();
        AssertUnsubscribeUrlShape(unsubscribeUrl);
        await Assert.That(GetSingleHeaderValue(headers, "List-Unsubscribe-Post"))
            .IsEqualTo("List-Unsubscribe=One-Click");
        await Assert.That(text).Contains(unsubscribeUrl);
        await Assert.That(html).Contains(unsubscribeUrl);

        await AssertUnsubscribeTokenDisablesPreferenceAsync(unsubscribeUrl, dispatched, scenario.TenantSlug);
    }

    private async Task AssertUnsubscribeTokenDisablesPreferenceAsync(
        string unsubscribeUrl,
        EmailDispatchOutbox dispatched,
        string tenantSlug)
    {
        var page = await playwright.CreatePageAsync(nameof(AssertUnsubscribeTokenDisablesPreferenceAsync));
        try
        {
            var response = await page.Context.APIRequest.PostAsync(
                BuildReachableUnsubscribeUrl(unsubscribeUrl),
                new APIRequestContextOptions
                {
                    Headers = new Dictionary<string, string>
                    {
                        [TenantSlugHeaderName] = tenantSlug
                    },
                    Timeout = ApiRequestTimeoutMilliseconds
                });

            if (response.Status != (int)HttpStatusCode.OK)
            {
                var body = await response.TextAsync();
                throw new InvalidOperationException($"Email unsubscribe failed with status {response.Status}: {body}");
            }
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(AssertUnsubscribeTokenDisablesPreferenceAsync));
        }

        await using var context = appHost.CreateDbContext();
        var userId = dispatched.UserId
            ?? throw new InvalidOperationException("Registration confirmation dispatch did not capture a user id.");

        var preference = await context.UserNotificationPreferences
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.TenantId == dispatched.TenantId
                && x.UserId == userId
                && x.Category == NotificationPreferenceCategories.RegistrationConfirmations);

        await Assert.That(preference).IsNotNull();
        await Assert.That(preference!.IsEnabled).IsFalse();
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
        var header = headers.FirstOrDefault(x => string.Equals(x.Key, name, StringComparison.OrdinalIgnoreCase));
        if (header.Value is not { Length: > 0 })
        {
            throw new InvalidOperationException($"Mailpit message did not contain expected '{name}' header.");
        }

        return header.Value[0];
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
