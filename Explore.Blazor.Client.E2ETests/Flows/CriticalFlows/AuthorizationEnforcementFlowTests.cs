// ABOUTME: Playwright critical-flow scaffold for browser and API authorization enforcement.
// ABOUTME: Documents protected-route redirects, hidden edit affordances, and direct mutation denial.

using Explore.Blazor.Client.E2ETests.Fixtures;

namespace Explore.Blazor.Client.E2ETests.Flows.CriticalFlows;

[Category(E2ETestCategories.E2E)]
[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerTestSession, SharedType.PerTestSession])]
[NotInParallel("E2EAppHostDb")]
[ParallelLimiter<BrowserParallelLimit>]
public class AuthorizationEnforcementFlowTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright)
{
    [Test]
    public async Task AnonymousProtectedRoutes_ChallengeToKeycloakLogin()
    {
        await appHost.ResetDatabaseAsync();

        var page1 = await playwright.CreatePageAsync($"{nameof(AnonymousProtectedRoutes_ChallengeToKeycloakLogin)}-1");
        try
        {
            await BffCookieAuthHelper.AddSetupSecretBypassCookieAsync(page1.Context, appHost.BlazorBaseUrl);
            await AssertAnonymousProtectedRouteRedirectsToLoginAsync(page1, "/events/create");
        }
        finally
        {
            await playwright.ClosePageAsync(page1, $"{nameof(AnonymousProtectedRoutes_ChallengeToKeycloakLogin)}-1");
        }

        var page2 = await playwright.CreatePageAsync($"{nameof(AnonymousProtectedRoutes_ChallengeToKeycloakLogin)}-2");
        try
        {
            await BffCookieAuthHelper.AddSetupSecretBypassCookieAsync(page2.Context, appHost.BlazorBaseUrl);
            await AssertAnonymousProtectedRouteRedirectsToLoginAsync(page2, "/settings");
        }
        finally
        {
            await playwright.ClosePageAsync(page2, $"{nameof(AnonymousProtectedRoutes_ChallengeToKeycloakLogin)}-2");
        }
    }

    [Test]
    public async Task ForbiddenErrorRoute_RendersAccessDeniedRecoveryUi()
    {
        await appHost.ResetDatabaseAsync();

        var page = await playwright.CreatePageAsync(nameof(ForbiddenErrorRoute_RendersAccessDeniedRecoveryUi));
        try
        {
            await AssertUnauthorizedShellRendersAsync(page);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(ForbiddenErrorRoute_RendersAccessDeniedRecoveryUi));
        }
    }

    [Test]
    public async Task AnonymousProtectedRoutes_DoNotCreateBrowserTokenStorage()
    {
        await appHost.ResetDatabaseAsync();

        var page = await playwright.CreatePageAsync(nameof(AnonymousProtectedRoutes_DoNotCreateBrowserTokenStorage));
        try
        {
            await BffCookieAuthHelper.AddSetupSecretBypassCookieAsync(page.Context, appHost.BlazorBaseUrl);

            await AssertAnonymousProtectedRouteRedirectsToLoginAsync(page, "/events/create");
            await BffCookieAuthHelper.AssertBrowserStorageDoesNotContainTokensAsync(page, appHost.BlazorBaseUrl);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(AnonymousProtectedRoutes_DoNotCreateBrowserTokenStorage));
        }
    }

    [Test]
    public async Task AuthenticatedLowPrivilegeUser_InstanceAdminApiReturnsForbiddenWithoutBrowserTokens()
    {
        await appHost.ResetDatabaseAsync();

        var page = await playwright.CreatePageAsync(
            nameof(AuthenticatedLowPrivilegeUser_InstanceAdminApiReturnsForbiddenWithoutBrowserTokens));
        try
        {
            await BffCookieAuthHelper.LoginAsTestUserAsync(page, appHost);
            await AssertInstanceAdminApiIsForbiddenForLowPrivilegeUserAsync(page);
            await BffCookieAuthHelper.AssertBrowserStorageDoesNotContainTokensAsync(page, appHost.BlazorBaseUrl);
        }
        finally
        {
            await playwright.ClosePageAsync(
                page,
                nameof(AuthenticatedLowPrivilegeUser_InstanceAdminApiReturnsForbiddenWithoutBrowserTokens));
        }
    }

    private async Task AssertAnonymousProtectedRouteRedirectsToLoginAsync(IPage page, string protectedPath)
    {
        var response = await page.GotoAsync($"{appHost.BlazorBaseUrl}{protectedPath}");
        await Assert.That(response).IsNotNull();

        await page.Locator("#username").WaitForAsync();

        await Assert.That(page.Url).Contains("/protocol/openid-connect/auth");
        await Assert.That(page.Url).Contains("client_id=islamu-event-blazor");
        await Assert.That(page.Url).Contains("redirect_uri=");
    }

    private async Task AssertUnauthorizedShellRendersAsync(IPage page)
    {
        var response = await page.GotoAsync($"{appHost.BlazorBaseUrl}/errors/403");
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Status).IsEqualTo((int)HttpStatusCode.OK);

        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Access Denied" })
            .WaitForAsync();

        var requestAccessLink = page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Request Access" });
        await requestAccessLink.WaitForAsync();
        await Assert.That(await requestAccessLink.GetAttributeAsync("href")).IsEqualTo("/contact");
    }

    private async Task AssertInstanceAdminApiIsForbiddenForLowPrivilegeUserAsync(IPage page)
    {
        var response = await page.Context.APIRequest.GetAsync(
            $"{appHost.BlazorBaseUrl}/api/instance/settings/modules");

        await Assert.That(response).IsNotNull();
        await Assert.That(response.Status).IsEqualTo((int)HttpStatusCode.Forbidden);
    }

}
