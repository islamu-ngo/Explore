// ABOUTME: Smoke tests verifying the Blazor frontend loads in a real browser.
// ABOUTME: Requires running infrastructure (PostgreSQL, Redis, Keycloak) via Aspire AppHost.

using System.Text.Json;

using Explore.Blazor.Client.E2ETests.Fixtures;

namespace Explore.Blazor.Client.E2ETests.Flows;

[Category(E2ETestCategories.E2E)]
[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerClass, SharedType.PerTestSession])]
[NotInParallel("E2EAppHostDb")]
[ParallelLimiter<BrowserParallelLimit>]
public class SmokeTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright)
{
    [Test]
    public async Task BlazorFrontend_Loads_ReturnsHtml()
    {
        var page = await playwright.CreatePageAsync(nameof(BlazorFrontend_Loads_ReturnsHtml));
        try
        {
            var response = await page.GotoAsync($"{appHost.BlazorBaseUrl}/events");
            await Assert.That(response).IsNotNull();
            await Assert.That(response!.Status).IsEqualTo(200);

            await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
            {
                Name = "Explore Events"
            })
                .WaitForAsync();
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(BlazorFrontend_Loads_ReturnsHtml));
        }
    }

    [Test]
    public async Task AuthStatus_Anonymous_ReturnsNotAuthenticated()
    {
        var page = await playwright.CreatePageAsync(nameof(AuthStatus_Anonymous_ReturnsNotAuthenticated));
        try
        {
            var response = await page.Context.APIRequest.GetAsync($"{appHost.BlazorBaseUrl}/auth/status");
            await Assert.That(response).IsNotNull();
            await Assert.That(response.Status).IsEqualTo(200);

            var content = await response.TextAsync();
            using var payload = JsonDocument.Parse(content);
            var root = payload.RootElement;

            await Assert.That(root.TryGetProperty("isAuthenticated", out var isAuthenticated)).IsTrue();
            await Assert.That(isAuthenticated.GetBoolean()).IsFalse();
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(AuthStatus_Anonymous_ReturnsNotAuthenticated));
        }
    }

    [Test]
    public async Task AuthStatus_KeycloakLogin_ReturnsAuthenticatedWithServerCookieOnly()
    {
        var page = await playwright.CreatePageAsync(nameof(AuthStatus_KeycloakLogin_ReturnsAuthenticatedWithServerCookieOnly));
        try
        {
            await BffCookieAuthHelper.LoginAsTestUserAsync(page, appHost);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(AuthStatus_KeycloakLogin_ReturnsAuthenticatedWithServerCookieOnly));
        }
    }

}
