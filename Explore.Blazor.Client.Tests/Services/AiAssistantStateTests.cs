// ABOUTME: Behavioral tests for AI assistant shell availability state.
// ABOUTME: Verifies tenant enabled/available flags, authentication audience, and user navbar preference composition.

using Explore.Blazor.Client.Services;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class AiAssistantStateTests
{
    [Test]
    public async Task SetPolicy_WhenAuthenticatedAndTenantAvailable_MakesAssistantAvailable()
    {
        var state = new AiAssistantState();

        state.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);

        await Assert.That(state.IsAvailable).IsTrue();
        await Assert.That(state.IsButtonVisible).IsTrue();
    }

    [Test]
    public async Task SetPolicy_WhenTenantEnabledButNotAvailable_ButtonVisibleButNotAvailable()
    {
        var state = new AiAssistantState();

        state.SetPolicy(tenantEnabled: true, tenantAvailable: false, allowAnonymousAccess: false, isAuthenticated: true);

        await Assert.That(state.IsAvailable).IsFalse();
        await Assert.That(state.IsButtonVisible).IsTrue();
    }

    [Test]
    public async Task SetPolicy_WhenTenantDisabled_NeitherAvailableNorButtonVisible()
    {
        var state = new AiAssistantState();

        state.SetPolicy(tenantEnabled: false, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);

        await Assert.That(state.IsAvailable).IsFalse();
        await Assert.That(state.IsButtonVisible).IsFalse();
    }

    [Test]
    public async Task SetPolicy_WhenAnonymousRequiresPublicPolicy()
    {
        var state = new AiAssistantState();

        state.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: false);
        await Assert.That(state.IsAvailable).IsFalse();
        await Assert.That(state.IsButtonVisible).IsFalse();

        state.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: true, isAuthenticated: false);
        await Assert.That(state.IsAvailable).IsTrue();
        await Assert.That(state.IsButtonVisible).IsTrue();
    }

    [Test]
    public async Task SetUserNavbarPreference_WhenDisabled_HidesAndClosesAssistant()
    {
        var state = new AiAssistantState();
        state.SetPolicy(tenantEnabled: true, tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
        state.Open();

        state.SetUserNavbarPreference(false);

        await Assert.That(state.IsAvailable).IsFalse();
        await Assert.That(state.IsButtonVisible).IsFalse();
        await Assert.That(state.IsOpen).IsFalse();
    }

    [Test]
    public async Task SetUserNavbarPreference_WhenDisabledButTenantEnabled_ButtonHidesButRailCannotOpen()
    {
        var state = new AiAssistantState();
        state.SetPolicy(tenantEnabled: true, tenantAvailable: false, allowAnonymousAccess: false, isAuthenticated: true);

        state.SetUserNavbarPreference(false);

        await Assert.That(state.IsButtonVisible).IsFalse();
        await Assert.That(state.IsAvailable).IsFalse();
        await Assert.That(state.IsOpen).IsFalse();
    }
}
