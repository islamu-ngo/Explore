// ABOUTME: Browser-driven BFF cookie authentication helper for E2E tests.
// ABOUTME: Verifies Keycloak login without exposing bearer tokens to browser storage.

using System.Text.Json;

namespace Explore.Blazor.Client.E2ETests.Fixtures;

public static class BffCookieAuthHelper
{
    public const string TestUserName = "test-user";
    public const string TestUserPassword = "test-user-password";
    private static readonly TimeSpan LoginNavigationTimeout = TimeSpan.FromSeconds(60);

    private static readonly string[] BrowserTokenTerms =
    [
        "access_token",
        "refresh_token",
        "id_token",
        "bearer",
        "jwt"
    ];

    public static async Task LoginAsTestUserAsync(
        IPage page,
        AppHostFixture appHost,
        string returnUrl = "/events")
    {
        await AddSetupSecretBypassCookieAsync(page.Context, appHost.BlazorBaseUrl);

        var loginUrl =
            $"{appHost.BlazorBaseUrl}/auth/login?provider=keycloak&returnUrl={Uri.EscapeDataString(returnUrl)}";

        await page.GotoAsync(loginUrl, new PageGotoOptions
        {
            Timeout = (float)LoginNavigationTimeout.TotalMilliseconds,
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await page.Locator("#username").FillAsync(TestUserName);
        var password = page.Locator("#password");
        await password.FillAsync(TestUserPassword);
        await password.PressAsync("Enter", new LocatorPressOptions
        {
            Timeout = (float)LoginNavigationTimeout.TotalMilliseconds
        });

        await WaitForAuthenticatedStatusAsync(page, appHost.BlazorBaseUrl);

        await AssertAuthenticatedStatusAsync(page, appHost);
        await AssertServerCookieOnlyAsync(page, appHost);
    }

    public static async Task AssertAuthenticatedStatusAsync(IPage page, AppHostFixture appHost)
    {
        var response = await page.Context.APIRequest.GetAsync($"{appHost.BlazorBaseUrl}/auth/status");
        await Assert.That(response).IsNotNull();
        await Assert.That(response.Status).IsEqualTo((int)HttpStatusCode.OK);

        var content = await response.TextAsync();
        using var payload = JsonDocument.Parse(content);
        var root = payload.RootElement;

        await Assert.That(root.TryGetProperty("isAuthenticated", out var isAuthenticated)).IsTrue();
        await Assert.That(isAuthenticated.GetBoolean()).IsTrue();
    }

    private static async Task WaitForAuthenticatedStatusAsync(IPage page, string blazorBaseUrl)
    {
        var deadline = DateTimeOffset.UtcNow.Add(LoginNavigationTimeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await IsAuthenticatedAsync(page, blazorBaseUrl))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException($"Timed out waiting for Keycloak login to establish a BFF auth cookie for {blazorBaseUrl}. Current URL: {page.Url}");
    }

    private static async Task<bool> IsAuthenticatedAsync(IPage page, string blazorBaseUrl)
    {
        try
        {
            var response = await page.Context.APIRequest.GetAsync($"{blazorBaseUrl}/auth/status");
            if (response.Status != (int)HttpStatusCode.OK)
            {
                return false;
            }

            var content = await response.TextAsync();
            using var payload = JsonDocument.Parse(content);
            var root = payload.RootElement;

            return root.TryGetProperty("isAuthenticated", out var isAuthenticated)
                && isAuthenticated.ValueKind == JsonValueKind.True
                && isAuthenticated.GetBoolean();
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static async Task AssertServerCookieOnlyAsync(IPage page, AppHostFixture appHost)
    {
        var cookies = await page.Context.CookiesAsync([appHost.BlazorBaseUrl]);
        var authCookie = cookies.FirstOrDefault(cookie =>
            cookie.Name.Contains(".AspNetCore.Cookies", StringComparison.OrdinalIgnoreCase));

        await Assert.That(authCookie).IsNotNull();
        await Assert.That(authCookie!.HttpOnly).IsTrue();

        await AssertBrowserStorageDoesNotContainTokensAsync(page, appHost.BlazorBaseUrl);
    }

    public static async Task AssertBrowserStorageDoesNotContainTokensAsync(IPage page, string? stableOrigin = null)
    {
        if (!string.IsNullOrWhiteSpace(stableOrigin))
        {
            await page.GotoAsync($"{stableOrigin.TrimEnd('/')}/auth/status", new PageGotoOptions
            {
                Timeout = (float)LoginNavigationTimeout.TotalMilliseconds,
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
        }

        var browserStorageContainsToken = await EvaluateBrowserStorageWithNavigationRetryAsync(page);

        await Assert.That(browserStorageContainsToken).IsFalse();
    }

    private static async Task<bool> EvaluateBrowserStorageWithNavigationRetryAsync(IPage page)
    {
        var deadline = DateTimeOffset.UtcNow.Add(LoginNavigationTimeout);

        while (true)
        {
            try
            {
                return await page.EvaluateAsync<bool>(
                    """
                    (tokenTerms) => {
                        const storages = [window.localStorage, window.sessionStorage];

                        for (const storage of storages) {
                            for (let index = 0; index < storage.length; index += 1) {
                                const key = storage.key(index) ?? '';
                                const value = storage.getItem(key) ?? '';
                                const searchable = `${key}\n${value}`.toLowerCase();

                                if (tokenTerms.some(term => searchable.includes(term))) {
                                    return true;
                                }
                            }
                        }

                        return false;
                    }
                    """,
                    BrowserTokenTerms);
            }
            catch (Exception exception) when (IsTransientNavigationException(exception)
                && DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250));
            }
        }
    }

    private static bool IsTransientNavigationException(Exception exception)
    {
        return exception.Message.Contains("Execution context was destroyed", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("Most likely because of a navigation", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task AddSetupSecretBypassCookieAsync(IBrowserContext context, string blazorBaseUrl)
    {
        // This cookie only bypasses the onboarding gate around auth-entry endpoints;
        // it is not an authentication credential and must never contain a token.
        await context.AddCookiesAsync(
        [
            new Microsoft.Playwright.Cookie
            {
                Name = "setup-secret",
                Value = "e2e-onboarding-gate-bypass",
                Url = blazorBaseUrl,
                HttpOnly = true,
                Secure = blazorBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase),
                SameSite = SameSiteAttribute.Lax
            }
        ]);
    }
}
