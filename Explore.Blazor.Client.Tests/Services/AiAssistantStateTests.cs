// ABOUTME: Behavioral tests for AI assistant shell availability state.
// ABOUTME: Verifies tenant policy, authentication audience, and user navbar preference composition.

using Explore.Blazor.Client.Services;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class AiAssistantStateTests
{
    [Test]
    public async Task SetPolicy_WhenAuthenticatedAndTenantAvailable_MakesAssistantAvailable()
    {
        var state = new AiAssistantState();

        state.SetPolicy(tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);

        await Assert.That(state.IsAvailable).IsTrue();
    }

    [Test]
    public async Task SetPolicy_WhenAnonymousRequiresPublicPolicy()
    {
        var state = new AiAssistantState();

        state.SetPolicy(tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: false);
        await Assert.That(state.IsAvailable).IsFalse();

        state.SetPolicy(tenantAvailable: true, allowAnonymousAccess: true, isAuthenticated: false);
        await Assert.That(state.IsAvailable).IsTrue();
    }

    [Test]
    public async Task SetUserNavbarPreference_WhenDisabled_HidesAndClosesAssistant()
    {
        var state = new AiAssistantState();
        state.SetPolicy(tenantAvailable: true, allowAnonymousAccess: false, isAuthenticated: true);
        state.Open();

        state.SetUserNavbarPreference(false);

        await Assert.That(state.IsAvailable).IsFalse();
        await Assert.That(state.IsOpen).IsFalse();
    }
}
