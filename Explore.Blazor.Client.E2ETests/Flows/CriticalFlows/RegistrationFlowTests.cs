// ABOUTME: Playwright critical-flow scaffold for the attendee registration journey.
// ABOUTME: Documents the browser path from event discovery through My Registrations.

using Explore.Blazor.Client.E2ETests.Fixtures;

namespace Explore.Blazor.Client.E2ETests.Flows.CriticalFlows;

[ClassDataSource<AppHostFixture, PlaywrightFixture, PostgreSqlContainerFixture>(
    Shared = [SharedType.PerTestSession, SharedType.PerTestSession, SharedType.PerTestSession])]
[ParallelLimiter<BrowserParallelLimit>]
public partial class RegistrationFlowTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright,
    PostgreSqlContainerFixture database)
{
    [Test]
    [Skip("Infrastructure-gated critical flow: requires Docker, Aspire AppHost, Keycloak login seed, and AppHost PostgreSQL override wiring.")]
    public async Task RegistrationFlowLoginBrowseRegisterConfirmationMyRegistrations()
    {
        await database.ResetAsync();

        var page = await playwright.CreatePageAsync();
        try
        {
            await BrowseEventsAsync(page);
            await OpenFirstEventAsync(page);
            await StartRegistrationAsync(page);
            await CompleteRegistrationAsync(page);
            await AssertRegistrationConfirmationAsync(page);
            await NavigateToMyRegistrationsAsync(page);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task BrowseEventsAsync(IPage page)
    {
        var response = await page.GotoAsync($"{appHost.BlazorBaseUrl}/events");
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Status).IsEqualTo((int)HttpStatusCode.OK);

        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Explore Events" })
            .WaitForAsync();
    }

    private static async Task OpenFirstEventAsync(IPage page)
    {
        await page.Locator(".event-card").First.ClickAsync();
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameRegex = EventPageButtonPattern() })
            .WaitForAsync();
    }

    private static async Task StartRegistrationAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameRegex = RegisterButtonPattern() })
            .ClickAsync();
    }

    private static async Task CompleteRegistrationAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameRegex = ConfirmRegistrationPattern() })
            .ClickAsync();
    }

    private static async Task AssertRegistrationConfirmationAsync(IPage page)
    {
        await page.GetByText("Registration Successful", new PageGetByTextOptions { Exact = false })
            .WaitForAsync();
        await Assert.That(await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions
        {
            Name = "View My Registrations"
        }).CountAsync()).IsGreaterThanOrEqualTo(1);
    }

    private static async Task NavigateToMyRegistrationsAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "View My Registrations" })
            .ClickAsync();

        await page.WaitForURLAsync(url => url.Contains("/my/registrations", StringComparison.OrdinalIgnoreCase));
        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "My Registrations" })
            .WaitForAsync();
    }

    [GeneratedRegex("^(Event Page|View Details|Details)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EventPageButtonPattern();

    [GeneratedRegex("Register", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RegisterButtonPattern();

    [GeneratedRegex("(Confirm|Complete|Register)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ConfirmRegistrationPattern();
}
