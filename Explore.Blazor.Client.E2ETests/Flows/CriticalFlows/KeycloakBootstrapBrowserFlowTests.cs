// ABOUTME: Browser E2E coverage for setup-time Keycloak bootstrap from the onboarding UI.
// ABOUTME: Verifies the UI can submit one-time credentials through the setup-gated BFF path.

using Explore.Blazor.Client.E2ETests.Fixtures;

namespace Explore.Blazor.Client.E2ETests.Flows.CriticalFlows;

[Category("E2E")]
[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerTestSession, SharedType.PerTestSession])]
[NotInParallel("E2EAppHostDb")]
[ParallelLimiter<BrowserParallelLimit>]
public sealed class KeycloakBootstrapBrowserFlowTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright)
{
    private static readonly float BrowserTimeoutMilliseconds = (float)TimeSpan.FromSeconds(90).TotalMilliseconds;

    [Test]
    [Timeout(420_000)]
    public async Task AuthProviderOnboarding_BootstrapMode_SubmitsSetupGatedBootstrap()
    {
        await appHost.ResetDatabaseAsync();

        var page = await playwright.CreatePageAsync(nameof(AuthProviderOnboarding_BootstrapMode_SubmitsSetupGatedBootstrap));
        try
        {
            await PersistSetupSecretAsync(page);
            await SubmitKeycloakBootstrapFormAsync(page);
            await BffCookieAuthHelper.AssertBrowserStorageDoesNotContainTokensAsync(page, appHost.BlazorBaseUrl);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(AuthProviderOnboarding_BootstrapMode_SubmitsSetupGatedBootstrap));
        }
    }

    private async Task PersistSetupSecretAsync(IPage page)
    {
        var response = await page.Context.APIRequest.PostAsync(
            $"{appHost.BlazorBaseUrl}/bff/setup-secret",
            new APIRequestContextOptions
            {
                Timeout = BrowserTimeoutMilliseconds,
                DataObject = new
                {
                    secret = AppHostFixture.SetupSecret
                }
            });

        await Assert.That(response.Status).IsEqualTo((int)HttpStatusCode.OK);
    }

    private async Task SubmitKeycloakBootstrapFormAsync(IPage page)
    {
        await page.GotoAsync($"{appHost.BlazorBaseUrl}/onboarding/auth-provider", new PageGotoOptions
        {
            Timeout = BrowserTimeoutMilliseconds,
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
            {
                Name = "Authentication Providers"
            })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        var keycloakHeader = page.GetByTestId("keycloak-provider-header");
        await keycloakHeader.WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });

        await keycloakHeader.ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        var enableKeycloak = page.GetByRole(AriaRole.Switch, new PageGetByRoleOptions
        {
            Name = "Enable Keycloak"
        });
        await enableKeycloak
            .WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = BrowserTimeoutMilliseconds
            });

        await enableKeycloak
            .CheckAsync(new LocatorCheckOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByTestId("keycloak-bootstrap-mode-radio")
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await page.GetByLabel("Keycloak base URL (Required)")
            .FillAsync(appHost.KeycloakBaseUrl, new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByLabel("Realm (Required)")
            .FillAsync(BffKeycloakFixture.RealmName, new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByLabel("Blazor BFF client ID (Required)")
            .FillAsync(BffKeycloakFixture.TestClientId, new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByLabel("Blazor BFF client secret (Required)")
            .FillAsync(BffKeycloakFixture.TestClientSecret, new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByLabel("One-time Keycloak admin username (Required)")
            .FillAsync("admin", new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.GetByLabel("One-time Keycloak admin password (Required)")
            .FillAsync("admin", new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await page.Keyboard.PressAsync("Tab");

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
            {
                Name = "Save & Continue to Login"
            })
            .ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

        await page.WaitForURLAsync(
            new Regex("/onboarding/authz-provider", RegexOptions.IgnoreCase),
            new PageWaitForURLOptions { Timeout = BrowserTimeoutMilliseconds });
    }
}
