using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Bunit.TestDoubles;
using Explore.Blazor.Client.Pages.Events;
using Explore.Blazor.Client.Tests.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Explore.Blazor.Client.Tests.Pages.Auth;

public class AuthRedirectPagesTests : IDisposable
{
    private readonly BlazorTestContext _ctx;

    public AuthRedirectPagesTests()
    {
        _ctx = new BlazorTestContext();
        ConfigureAuthProviderClient(new
        {
            providers = new[]
            {
                new { name = "Keycloak", displayName = "Keycloak", type = "button", recommended = true }
            }
        });
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private static Type GetPageComponentType(string componentName)
    {
        var componentType = typeof(EventList).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == componentName && typeof(IComponent).IsAssignableFrom(t));

        return componentType ?? throw new InvalidOperationException($"Could not find component type '{componentName}'.");
    }

    [Test]
    public async Task LoginRedirect_NavigatesToAuthChallenge_WhenNoQueryString()
    {
        // Arrange
        var nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("/login");

        // Act
        var cut = _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("LoginRedirect")));

        // Assert
        await WaitForNavigationAsync(nav, uri => IsChallengeNavigation(uri, "keycloak", "/"));

        await AssertChallengeNavigationAsync(nav.Uri, "keycloak", "/");
    }

    [Test]
    public async Task LoginRedirect_ForwardsQueryString_ToAuthChallenge()
    {
        // Arrange
        var nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("/login?returnUrl=%2Fadmin%2Ftenant%2Fsettings");

        // Act
        var cut = _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("LoginRedirect")));

        // Assert
        await WaitForNavigationAsync(nav, uri => IsChallengeNavigation(uri, "keycloak", "/admin/tenant/settings"));

        await AssertChallengeNavigationAsync(nav.Uri, "keycloak", "/admin/tenant/settings");
    }

    [Test]
    public async Task LoginRedirect_WithMultipleProviders_ShouldNotAutoRedirect()
    {
        // Arrange
        ConfigureAuthProviderClient(new
        {
            providers = new[]
            {
                new { name = "Keycloak", displayName = "Keycloak", type = "button", recommended = true },
                new { name = "Google", displayName = "Google", type = "button", recommended = false },
                new { name = "Atproto", displayName = "AT Protocol", type = "handle_input", recommended = false }
            }
        });

        var nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("/login?returnUrl=%2Fevents");

        // Act
        var cut = _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("LoginRedirect")));

        // Assert
        await Assert.That(nav.Uri).EndsWith("/login?returnUrl=%2Fevents");
        await Assert.That(cut.Markup).Contains("Continue with Keycloak");
        await Assert.That(cut.Markup).Contains("Continue with Google");
        await Assert.That(cut.Markup).Contains("Continue with ATProto");
        await Assert.That(cut.Markup.IndexOf("Continue with Keycloak", StringComparison.Ordinal))
            .IsLessThan(cut.Markup.IndexOf("Continue with Google", StringComparison.Ordinal));
        await Assert.That(cut.Markup.IndexOf("Continue with Google", StringComparison.Ordinal))
            .IsLessThan(cut.Markup.IndexOf("Continue with ATProto", StringComparison.Ordinal));
    }

    [Test]
    public async Task LoginRedirect_WithForcedProvider_ShouldAutoRedirectToThatProvider()
    {
        // Arrange
        ConfigureAuthProviderClient(new
        {
            providers = new[]
            {
                new { name = "Keycloak", displayName = "Keycloak", type = "button", recommended = true },
                new { name = "Google", displayName = "Google", type = "button", recommended = false }
            }
        });

        var nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("/login?provider=google&returnUrl=%2Fsetup");

        // Act
        var cut = _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("LoginRedirect")));

        // Assert
        await WaitForNavigationAsync(nav, uri => IsChallengeNavigation(uri, "google", "/setup"));

        await AssertChallengeNavigationAsync(nav.Uri, "google", "/setup");
    }

    [Test]
    public async Task LoginRedirect_WithChallengeError_ShouldNotAutoRedirect()
    {
        // Arrange — single provider + forced provider + challengeError=1
        // This scenario occurs when OIDC callback fails (OnRemoteFailure).
        // The login page must NOT auto-redirect back to the same provider, or it loops.
        var nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("/login?returnUrl=%2Fsetup&challengeError=1&provider=keycloak");

        // Act
        var cut = _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("LoginRedirect")));

        // Assert — page stays on /login, shows error message, does NOT redirect
        await Assert.That(nav.Uri).EndsWith("/login?returnUrl=%2Fsetup&challengeError=1&provider=keycloak");
        await Assert.That(cut.Markup).Contains("The last login attempt failed");
        await Assert.That(cut.Markup).Contains("Continue with Keycloak");
    }

    [Test]
    public async Task LoginRedirect_WithChallengeErrorAndErrorDetail_DoesNotRenderErrorDetail()
    {
        var nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("/login?returnUrl=%2Fsetup&challengeError=1&provider=keycloak&errorDetail=secretLen%3D24%7CclientId%3Dislamu-event-blazor");

        var cut = _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("LoginRedirect")));

        await Assert.That(cut.Markup).Contains("The last login attempt failed");
        await Assert.That(cut.Markup).Contains("Continue with Keycloak");
        await Assert.That(cut.Markup).DoesNotContain("secretLen");
        await Assert.That(cut.Markup).DoesNotContain("clientId");
        await Assert.That(cut.Markup).DoesNotContain("islamu-event-blazor");
    }

    [Test]
    public async Task LoginRedirect_WithSingleAtprotoAndLoginHint_ShouldAutoRedirectToAtprotoChallenge()
    {
        // Arrange
        ConfigureAuthProviderClient(new
        {
            providers = new[]
            {
                new { name = "Atproto", displayName = "AT Protocol", type = "handle_input", recommended = false }
            }
        });

        var nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("/login?returnUrl=%2Fdashboard&login_hint=user.bsky.social");

        // Act
        var cut = _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("LoginRedirect")));

        // Assert
        await WaitForNavigationAsync(nav, uri => IsChallengeNavigation(uri, "atproto", "/dashboard", "user.bsky.social"));

        await AssertChallengeNavigationAsync(nav.Uri, "atproto", "/dashboard", "user.bsky.social");
    }

    [Test]
    public async Task LoginRedirect_WithSingleAtprotoWithoutLoginHint_ShouldNotAutoRedirect()
    {
        // Arrange
        ConfigureAuthProviderClient(new
        {
            providers = new[]
            {
                new { name = "Atproto", displayName = "AT Protocol", type = "handle_input", recommended = false }
            }
        });

        var nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("/login?returnUrl=%2Fdashboard");

        // Act
        var cut = _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("LoginRedirect")));

        // Assert
        await Assert.That(nav.Uri).EndsWith("/login?returnUrl=%2Fdashboard");
        await Assert.That(cut.Markup).Contains("Continue with ATProto");
        await Assert.That(cut.Markup).DoesNotContain("ATProto handle");
    }

    [Test]
    public async Task LoginRedirect_ClickingAtproto_ShouldRevealHandleInput()
    {
        // Arrange
        ConfigureAuthProviderClient(new
        {
            providers = new[]
            {
                new { name = "Keycloak", displayName = "Keycloak", type = "button", recommended = true },
                new { name = "Google", displayName = "Google", type = "button", recommended = false },
                new { name = "Atproto", displayName = "AT Protocol", type = "handle_input", recommended = false }
            }
        });

        var nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("/login?returnUrl=%2Fdashboard");

        var cut = _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("LoginRedirect")));

        // Act
        var atprotoButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Continue with ATProto", StringComparison.Ordinal));
        atprotoButton.Click();

        // Assert
        await Assert.That(nav.Uri).EndsWith("/login?returnUrl=%2Fdashboard");
        await Assert.That(cut.Markup).Contains("ATProto handle");
    }

    [Test]
    public async Task LogoutRedirect_NavigatesToAuthSignout_WhenNoQueryString()
    {
        // Arrange
        var nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("/logout");

        // Act
        _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("LogoutRedirect")));

        // Assert
        await Assert.That(nav.Uri).EndsWith("/auth/signout");
    }

    [Test]
    public async Task LogoutRedirect_ForwardsQueryString_ToAuthSignout()
    {
        // Arrange
        var nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("/logout?returnUrl=%2F");

        // Act
        _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("LogoutRedirect")));

        // Assert
        await Assert.That(nav.Uri).EndsWith("/auth/signout?returnUrl=%2F");
    }

    private void ConfigureAuthProviderClient(object responsePayload)
    {
        var handler = new StubHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(responsePayload)
            };

            return response;
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/")
        };
        _ctx.Services.AddSingleton(RestService.For<IBffAuthApi>(client));
    }

    private static async Task WaitForNavigationAsync(BunitNavigationManager navigationManager, Func<string, bool> predicate, int timeoutMs = 5000)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            if (predicate(navigationManager.Uri))
            {
                return;
            }

            await Task.Delay(10);
        }

        if (predicate(navigationManager.Uri))
        {
            return;
        }

        throw new InvalidOperationException($"Expected navigation did not occur. Last URI: {navigationManager.Uri}");
    }

    private static bool IsChallengeNavigation(string uri, string provider, string returnUrl, string? loginHint = null)
    {
        try
        {
            var parsedUri = new Uri(uri, UriKind.Absolute);
            var query = QueryHelpers.ParseQuery(parsedUri.Query);

            if (!string.Equals(parsedUri.AbsolutePath, "/auth/challenge", StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(query["provider"].ToString(), provider, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(query["returnUrl"].ToString(), returnUrl, StringComparison.Ordinal))
            {
                return false;
            }

            if (loginHint is null)
            {
                return !query.ContainsKey("login_hint");
            }

            return string.Equals(query["login_hint"].ToString(), loginHint, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static async Task AssertChallengeNavigationAsync(string uri, string provider, string returnUrl, string? loginHint = null)
    {
        var parsedUri = new Uri(uri, UriKind.Absolute);
        var query = QueryHelpers.ParseQuery(parsedUri.Query);

        await Assert.That(parsedUri.AbsolutePath).IsEqualTo("/auth/challenge");
        await Assert.That(query["provider"].ToString()).IsEqualTo(provider);
        await Assert.That(query["returnUrl"].ToString()).IsEqualTo(returnUrl);

        if (loginHint is null)
        {
            await Assert.That(query.ContainsKey("login_hint")).IsFalse();
        }
        else
        {
            await Assert.That(query["login_hint"].ToString()).IsEqualTo(loginHint);
        }
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}
