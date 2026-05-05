// ABOUTME: Browser-driven BFF cookie authentication helper for E2E tests.
// ABOUTME: Verifies Keycloak login without exposing bearer tokens to browser storage.

using System.Text.Json;

namespace Explore.Blazor.Client.E2ETests.Fixtures;

public static class BffCookieAuthHelper
{
    public const string TestUserName = "test-user";
    public const string TestUserPassword = "test-user-password";

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

        await page.GotoAsync(loginUrl);
        await page.Locator("#username").FillAsync(TestUserName);
        await page.Locator("#password").FillAsync(TestUserPassword);
        await page.Locator("#kc-login").ClickAsync();

        await page.WaitForURLAsync(url =>
            url.StartsWith(appHost.BlazorBaseUrl, StringComparison.OrdinalIgnoreCase)
            && !url.Contains("/signin-oidc", StringComparison.OrdinalIgnoreCase)
            && !url.Contains("/auth/", StringComparison.OrdinalIgnoreCase)
            && !url.Contains("/setup", StringComparison.OrdinalIgnoreCase));

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

    public static async Task AssertServerCookieOnlyAsync(IPage page, AppHostFixture appHost)
    {
        var cookies = await page.Context.CookiesAsync([appHost.BlazorBaseUrl]);
        var authCookie = cookies.FirstOrDefault(cookie =>
            cookie.Name.Contains(".AspNetCore.Cookies", StringComparison.OrdinalIgnoreCase));

        await Assert.That(authCookie).IsNotNull();
        await Assert.That(authCookie!.HttpOnly).IsTrue();

        var browserStorageContainsToken = await page.EvaluateAsync<bool>(
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

        await Assert.That(browserStorageContainsToken).IsFalse();
    }

    private static async Task AddSetupSecretBypassCookieAsync(IBrowserContext context, string blazorBaseUrl)
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
