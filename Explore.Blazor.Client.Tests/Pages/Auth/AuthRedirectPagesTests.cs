using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Bunit.TestDoubles;
using Explore.Blazor.Client.Pages.Events;
using Explore.Blazor.Client.Tests.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

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
        cut.WaitForAssertion(() =>
        {
            if (!nav.Uri.EndsWith("/auth/challenge?provider=keycloak&returnUrl=%2F", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected navigation to keycloak challenge with default returnUrl.");
            }
        });

        await Assert.That(nav.Uri).EndsWith("/auth/challenge?provider=keycloak&returnUrl=%2F");
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
        cut.WaitForAssertion(() =>
        {
            if (!nav.Uri.EndsWith("/auth/challenge?provider=keycloak&returnUrl=%2Fadmin%2Ftenant%2Fsettings", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected navigation to keycloak challenge with forwarded returnUrl.");
            }
        });

        await Assert.That(nav.Uri).EndsWith("/auth/challenge?provider=keycloak&returnUrl=%2Fadmin%2Ftenant%2Fsettings");
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
        cut.WaitForAssertion(() =>
        {
            if (!nav.Uri.EndsWith("/auth/challenge?provider=google&returnUrl=%2Fsetup", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected navigation to forced Google challenge URL.");
            }
        });

        await Assert.That(nav.Uri).EndsWith("/auth/challenge?provider=google&returnUrl=%2Fsetup");
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
        cut.WaitForAssertion(() =>
        {
            if (!nav.Uri.EndsWith("/auth/challenge?provider=atproto&returnUrl=%2Fdashboard&login_hint=user.bsky.social", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected automatic redirect to ATProto challenge with login_hint.");
            }
        });

        await Assert.That(nav.Uri).EndsWith("/auth/challenge?provider=atproto&returnUrl=%2Fdashboard&login_hint=user.bsky.social");
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

        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("BffSelfClient").Returns(client);
        _ctx.Services.AddSingleton(factory);
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
