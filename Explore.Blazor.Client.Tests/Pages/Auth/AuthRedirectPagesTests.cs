using Bunit.TestDoubles;
using Explore.Blazor.Client.Pages.Event;
using Explore.Blazor.Client.Tests.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Blazor.Client.Tests.Pages.Auth;

public class AuthRedirectPagesTests : IDisposable
{
    private readonly BlazorTestContext _ctx;

    public AuthRedirectPagesTests()
    {
        _ctx = new BlazorTestContext();
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
        var nav = _ctx.Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("/login");

        // Act
        _ctx.RenderComponent<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("LoginRedirect")));

        // Assert
        await Assert.That(nav.Uri).EndsWith("/auth/challenge");
    }

    [Test]
    public async Task LoginRedirect_ForwardsQueryString_ToAuthChallenge()
    {
        // Arrange
        var nav = _ctx.Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("/login?returnUrl=%2Fadmin%2Ftenant%2Fsettings");

        // Act
        _ctx.RenderComponent<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("LoginRedirect")));

        // Assert
        await Assert.That(nav.Uri).EndsWith("/auth/challenge?returnUrl=%2Fadmin%2Ftenant%2Fsettings");
    }

    [Test]
    public async Task LogoutRedirect_NavigatesToAuthSignout_WhenNoQueryString()
    {
        // Arrange
        var nav = _ctx.Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("/logout");

        // Act
        _ctx.RenderComponent<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("LogoutRedirect")));

        // Assert
        await Assert.That(nav.Uri).EndsWith("/auth/signout");
    }

    [Test]
    public async Task LogoutRedirect_ForwardsQueryString_ToAuthSignout()
    {
        // Arrange
        var nav = _ctx.Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("/logout?returnUrl=%2F");

        // Act
        _ctx.RenderComponent<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("LogoutRedirect")));

        // Assert
        await Assert.That(nav.Uri).EndsWith("/auth/signout?returnUrl=%2F");
    }
}
