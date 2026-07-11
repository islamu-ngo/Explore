// ABOUTME: Browser E2E coverage for the Keycloak provider configured through API onboarding.
// ABOUTME: Verifies the BFF completes real OIDC login without exposing browser token storage.

using Explore.Blazor.Client.E2ETests.Fixtures;

namespace Explore.Blazor.Client.E2ETests.Flows.CriticalFlows;

[Category(E2ETestCategories.E2E)]
[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerClass, SharedType.PerTestSession])]
[NotInParallel("E2EAppHostDb")]
[ParallelLimiter<BrowserParallelLimit>]
public sealed class KeycloakBootstrapBrowserFlowTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright)
{
    [Test]
    [Timeout(420_000)]
    public async Task ApiOnboardedKeycloakProvider_AllowsBffLoginWithoutBrowserTokens()
    {
        var page = await playwright.CreatePageAsync(
            nameof(ApiOnboardedKeycloakProvider_AllowsBffLoginWithoutBrowserTokens));
        try
        {
            await BffCookieAuthHelper.LoginAsTestUserAsync(page, appHost);
            await BffCookieAuthHelper.AssertBrowserStorageDoesNotContainTokensAsync(
                page,
                appHost.BlazorBaseUrl);
        }
        finally
        {
            await playwright.ClosePageAsync(
                page,
                nameof(ApiOnboardedKeycloakProvider_AllowsBffLoginWithoutBrowserTokens));
        }
    }
}
