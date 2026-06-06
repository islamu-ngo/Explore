// ABOUTME: Browser E2E coverage for setup-time Keycloak bootstrap from the onboarding UI.
// ABOUTME: Verifies the UI can submit one-time credentials through the setup-gated BFF path.

using System.Diagnostics;
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

        var enableKeycloak = await TryGetVisibleLocatorAsync(
            page.Locator("[data-testid='keycloak-enable-switch'] [role='switch']"),
            TimeSpan.FromSeconds(5).TotalMilliseconds);

        if (enableKeycloak is null)
        {
            var keycloakHeader = page.GetByTestId("keycloak-provider-header");
            await keycloakHeader.WaitForAsync(new LocatorWaitForOptions { Timeout = BrowserTimeoutMilliseconds });
            await keycloakHeader.ClickAsync(new LocatorClickOptions { Timeout = BrowserTimeoutMilliseconds });

            enableKeycloak = await TryGetVisibleLocatorAsync(
                page.Locator("[data-testid='keycloak-enable-switch'] [role='switch']"),
                BrowserTimeoutMilliseconds);
        }

        if (enableKeycloak is null)
        {
            throw new TimeoutException("Timed out waiting for a visible Keycloak enable switch.");
        }

        await EnsureSwitchCheckedAsync(enableKeycloak);

        if (!await HasVisibleLabeledFieldAsync(page, "Keycloak base URL (Required)"))
        {
            var bootstrapModeOption = await WaitForVisibleTestIdAsync(page, "keycloak-bootstrap-mode-radio");
            var bootstrapModeRadioInput = await TryGetVisibleLocatorAsync(
                page.Locator("[data-testid='keycloak-bootstrap-mode-radio'] input[type='radio']"),
                TimeSpan.FromSeconds(5).TotalMilliseconds);

            if (bootstrapModeRadioInput is not null)
            {
                await bootstrapModeRadioInput.CheckAsync(new LocatorCheckOptions
                {
                    Timeout = BrowserTimeoutMilliseconds,
                    Force = true
                });
            }
            else if (await bootstrapModeOption.IsVisibleAsync())
            {
                await bootstrapModeOption.EvaluateAsync("element => element.click()");
            }
            else
            {
                var bootstrapModeText = page.GetByText("Let ISLAMU configure Keycloak clients now", new PageGetByTextOptions
                {
                    Exact = true
                });

                await bootstrapModeText.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = BrowserTimeoutMilliseconds
                });

                await bootstrapModeText.ClickAsync(new LocatorClickOptions
                {
                    Timeout = BrowserTimeoutMilliseconds,
                    Force = true
                });
            }
        }

        var keycloakBaseUrl = await WaitForVisibleLabeledFieldAsync(page, "Keycloak base URL (Required)");

        await keycloakBaseUrl.FillAsync(appHost.KeycloakBaseUrl, new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await (await WaitForVisibleLabeledFieldAsync(page, "Realm (Required)"))
            .FillAsync(BffKeycloakFixture.RealmName, new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await (await WaitForVisibleLabeledFieldAsync(page, "Blazor BFF client ID (Required)"))
            .FillAsync(BffKeycloakFixture.TestClientId, new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await (await WaitForVisibleLabeledFieldAsync(page, "Blazor BFF client secret (Required)"))
            .FillAsync(BffKeycloakFixture.TestClientSecret, new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await (await WaitForVisibleLabeledFieldAsync(page, "One-time Keycloak admin username (Required)"))
            .FillAsync("admin", new LocatorFillOptions { Timeout = BrowserTimeoutMilliseconds });
        await (await WaitForVisibleLabeledFieldAsync(page, "One-time Keycloak admin password (Required)"))
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

    private static async Task<ILocator> WaitForVisibleLabeledFieldAsync(IPage page, string label)
    {
        var candidates = page.GetByLabel(label);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed.TotalMilliseconds < BrowserTimeoutMilliseconds)
        {
            var count = await candidates.CountAsync();
            for (var index = count - 1; index >= 0; index--)
            {
                var candidate = candidates.Nth(index);
                if (await candidate.IsVisibleAsync())
                {
                    return candidate;
                }
            }

            await page.WaitForTimeoutAsync(100);
        }

        throw new TimeoutException($"Timed out waiting for visible field labeled '{label}'.");
    }

    private static async Task<bool> HasVisibleLabeledFieldAsync(IPage page, string label)
    {
        var candidates = page.GetByLabel(label);
        var count = await candidates.CountAsync();

        for (var index = count - 1; index >= 0; index--)
        {
            if (await candidates.Nth(index).IsVisibleAsync())
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<ILocator> WaitForVisibleTestIdAsync(IPage page, string testId)
    {
        var candidates = page.GetByTestId(testId);
        var visibleCandidate = await TryGetVisibleLocatorAsync(candidates, BrowserTimeoutMilliseconds);

        if (visibleCandidate is not null)
        {
            return visibleCandidate;
        }

        throw new TimeoutException($"Timed out waiting for visible element with test id '{testId}'.");
    }

    private static async Task<ILocator?> TryGetVisibleLocatorAsync(ILocator candidates, double timeoutMs)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed.TotalMilliseconds < timeoutMs)
        {
            var count = await candidates.CountAsync();
            for (var index = count - 1; index >= 0; index--)
            {
                var candidate = candidates.Nth(index);
                if (await candidate.IsVisibleAsync())
                {
                    return candidate;
                }
            }

            await candidates.Page.WaitForTimeoutAsync(100);
        }

        return null;
    }

    private static async Task WaitForCheckedAsync(ILocator locator)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed.TotalMilliseconds < BrowserTimeoutMilliseconds)
        {
            if (await locator.IsCheckedAsync())
            {
                return;
            }

            await locator.Page.WaitForTimeoutAsync(100);
        }

        throw new TimeoutException("Timed out waiting for the checkbox to become checked.");
    }

    private static async Task WaitForAriaCheckedAsync(ILocator locator)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed.TotalMilliseconds < BrowserTimeoutMilliseconds)
        {
            if (string.Equals(await locator.GetAttributeAsync("aria-checked"), "true", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await locator.Page.WaitForTimeoutAsync(100);
        }

        throw new TimeoutException("Timed out waiting for the switch to become checked.");
    }

    private static async Task EnsureSwitchCheckedAsync(ILocator switchLocator)
    {
        if (string.Equals(await switchLocator.GetAttributeAsync("aria-checked"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await switchLocator.ClickAsync(new LocatorClickOptions
        {
            Timeout = BrowserTimeoutMilliseconds,
            Force = true
        });

        if (await IsSwitchCheckedAsync(switchLocator))
        {
            return;
        }

        await switchLocator.EvaluateAsync("element => element.click()");

        if (await IsSwitchCheckedAsync(switchLocator))
        {
            return;
        }

        await switchLocator.PressAsync("Space", new LocatorPressOptions
        {
            Timeout = BrowserTimeoutMilliseconds
        });

        await WaitForAriaCheckedAsync(switchLocator);
    }

    private static async Task<bool> IsSwitchCheckedAsync(ILocator locator)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed.TotalMilliseconds < 5000)
        {
            if (string.Equals(await locator.GetAttributeAsync("aria-checked"), "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            await locator.Page.WaitForTimeoutAsync(100);
        }

        return false;
    }
}
