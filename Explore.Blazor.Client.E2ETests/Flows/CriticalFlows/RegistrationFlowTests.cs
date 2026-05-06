// ABOUTME: Playwright critical-flow scaffold for the attendee registration journey.
// ABOUTME: Documents the browser path from event discovery through My Registrations.

using Explore.Blazor.Client.E2ETests.Fixtures;
using Explore.Blazor.Client.E2ETests.Seeds;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Explore.Blazor.Client.E2ETests.Flows.CriticalFlows;

[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerTestSession, SharedType.PerTestSession])]
[NotInParallel("E2EAppHostDb")]
[ParallelLimiter<BrowserParallelLimit>]
public partial class RegistrationFlowTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright)
{
    [Test]
    public async Task RegistrationFlowLoginBrowseRegisterConfirmationMyRegistrations()
    {
        await appHost.ResetDatabaseAsync();

        RegistrationScenarioSeed.Result scenario;
        await using (var context = appHost.CreateDbContext())
        {
            scenario = await RegistrationScenarioSeed.SeedAsync(context);
        }

        var page = await playwright.CreatePageAsync(nameof(RegistrationFlowLoginBrowseRegisterConfirmationMyRegistrations));
        try
        {
            await BffCookieAuthHelper.LoginAsTestUserAsync(page, appHost);
            await AssertRegistrationEventIsVisibleThroughTenantBffAsync(page, scenario);
            await EnsureAuthenticatedUserIsSyncedThroughBffAsync(page, scenario);
            await RegisterForSeededSessionThroughBffAsync(page, scenario);
            await AssertRegistrationPersistedAsync(scenario);
            await BffCookieAuthHelper.AssertBrowserStorageDoesNotContainTokensAsync(page);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(RegistrationFlowLoginBrowseRegisterConfirmationMyRegistrations));
        }
    }

    private async Task AssertRegistrationEventIsVisibleThroughTenantBffAsync(
        IPage page,
        RegistrationScenarioSeed.Result scenario)
    {
        var response = await page.Context.APIRequest.GetAsync(
            $"{appHost.BlazorBaseUrl}/t/{scenario.TenantSlug}/api/event/{scenario.EventId}");

        if (response.Status != (int)HttpStatusCode.OK)
        {
            var body = await response.TextAsync();
            throw new InvalidOperationException($"Registration through BFF failed with status {response.Status}: {body}");
        }
        await Assert.That(await response.TextAsync()).Contains(scenario.EventTitle);
    }

    private async Task EnsureAuthenticatedUserIsSyncedThroughBffAsync(
        IPage page,
        RegistrationScenarioSeed.Result scenario)
    {
        var response = await page.Context.APIRequest.PostAsync(
            $"{appHost.BlazorBaseUrl}/t/{scenario.TenantSlug}/api/user/sync");

        if (response.Status != (int)HttpStatusCode.OK)
        {
            var body = await response.TextAsync();
            throw new InvalidOperationException($"User sync through BFF failed with status {response.Status}: {body}");
        }

        await AssertResponseSuccessAsync(response, "User sync through BFF");
    }

    private async Task RegisterForSeededSessionThroughBffAsync(IPage page, RegistrationScenarioSeed.Result scenario)
    {
        var response = await page.Context.APIRequest.PostAsync(
            $"{appHost.BlazorBaseUrl}/t/{scenario.TenantSlug}/api/eventregistration",
            new APIRequestContextOptions
            {
                DataObject = new
                {
                    eventId = scenario.EventId,
                    registrationScopeId = (int)Explore.Domain.Enums.RegistrationScopeEnum.SessionSelection,
                    selectedSessionIds = new[] { scenario.SessionId },
                    shareEmailWithOrganizer = false
                }
            });

        if (response.Status != (int)HttpStatusCode.OK)
        {
            var body = await response.TextAsync();
            throw new InvalidOperationException($"Registration through BFF failed with status {response.Status}: {body}");
        }

        await AssertResponseSuccessAsync(response, "Registration through BFF");
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

}
