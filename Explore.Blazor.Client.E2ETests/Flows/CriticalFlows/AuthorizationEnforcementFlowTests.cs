// ABOUTME: Playwright critical-flow scaffold for browser and API authorization enforcement.
// ABOUTME: Documents protected-route redirects, hidden edit affordances, and direct mutation denial.

using System.Text;
using Explore.Blazor.Client.E2ETests.Fixtures;

namespace Explore.Blazor.Client.E2ETests.Flows.CriticalFlows;

[ClassDataSource<AppHostFixture, PlaywrightFixture, PostgreSqlContainerFixture>(
    Shared = [SharedType.PerTestSession, SharedType.PerTestSession, SharedType.PerTestSession])]
[ParallelLimiter<BrowserParallelLimit>]
public class AuthorizationEnforcementFlowTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright,
    PostgreSqlContainerFixture database)
{
    [Test]
    [Skip("Infrastructure-gated critical flow: requires Docker, Aspire AppHost, Keycloak login seed, and authenticated low-privilege browser state.")]
    public async Task AuthorizationEnforcementHidesEditAffordancesAndRejectsDirectMutation()
    {
        await database.ResetAsync();

        var page = await playwright.CreatePageAsync();
        try
        {
            await AssertAnonymousProtectedRouteRedirectsToLoginAsync(page, "/events/create");
            await AssertAnonymousProtectedRouteRedirectsToLoginAsync(page, "/my/events");
            await AssertUnauthorizedShellRendersAsync(page);
            await AssertDirectAnonymousMutationIsDeniedAsync();

            // Runtime continuation point once deterministic low-privilege login exists:
            // authenticate as a regular user, open a seeded event detail page, assert no Edit button,
            // then issue the same mutation with the browser cookie session and assert 403 Forbidden.
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task AssertAnonymousProtectedRouteRedirectsToLoginAsync(IPage page, string protectedPath)
    {
        var response = await page.GotoAsync($"{appHost.BlazorBaseUrl}{protectedPath}");
        await Assert.That(response).IsNotNull();

        await page.WaitForURLAsync(url =>
            url.Contains("/login", StringComparison.OrdinalIgnoreCase)
            && url.Contains("returnUrl=", StringComparison.OrdinalIgnoreCase));

        await Assert.That(page.Url).Contains("/login");
        await Assert.That(Uri.UnescapeDataString(page.Url)).Contains(protectedPath);
    }

    private async Task AssertUnauthorizedShellRendersAsync(IPage page)
    {
        var response = await page.GotoAsync($"{appHost.BlazorBaseUrl}/errors/403");
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Status).IsEqualTo((int)HttpStatusCode.OK);

        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Access Denied" })
            .WaitForAsync();
    }

    private async Task AssertDirectAnonymousMutationIsDeniedAsync()
    {
        using var client = new HttpClient();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{appHost.BlazorBaseUrl}/api/event", content);

        await Assert.That(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden).IsTrue();
    }
}
