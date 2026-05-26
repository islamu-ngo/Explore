// ABOUTME: Playwright critical-flow coverage for the Blazor BFF token-forwarding chain.
// ABOUTME: Verifies browser cookie auth reaches the API through YARP without exposing tokens to browser storage.

using Explore.Blazor.Client.E2ETests.Fixtures;

namespace Explore.Blazor.Client.E2ETests.Flows.CriticalFlows;

[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerTestSession, SharedType.PerTestSession])]
[NotInParallel("E2EAppHostDb")]
[ParallelLimiter<BrowserParallelLimit>]
public class BffTokenForwardingChainFlowTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright)
{
    [Test]
    public async Task LoginBffYarpApiUsesServerSideTokenWithoutBrowserStorage()
    {
        await appHost.ResetDatabaseAsync();

        var page = await playwright.CreatePageAsync(nameof(LoginBffYarpApiUsesServerSideTokenWithoutBrowserStorage));
        try
        {
            await AssertPublicProxyIsReachableAsync(page);
            await BffCookieAuthHelper.LoginAsTestUserAsync(page, appHost);
            await AssertAuthenticatedProxyCallUsesBffCookieAsync(page);
            await BffCookieAuthHelper.AssertBrowserStorageDoesNotContainTokensAsync(page, appHost.BlazorBaseUrl);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(LoginBffYarpApiUsesServerSideTokenWithoutBrowserStorage));
        }
    }

    private async Task AssertPublicProxyIsReachableAsync(IPage page)
    {
        var response = await page.Context.APIRequest.GetAsync($"{appHost.BlazorBaseUrl}/api/event");

        await Assert.That(response.Status).IsEqualTo((int)HttpStatusCode.OK);
    }

    private async Task AssertAuthenticatedProxyCallUsesBffCookieAsync(IPage page)
    {
        var response = await page.Context.APIRequest.GetAsync($"{appHost.BlazorBaseUrl}/api/event/my");

        await Assert.That(response.Status).IsEqualTo((int)HttpStatusCode.OK);
    }

}
