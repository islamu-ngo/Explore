// ABOUTME: Tests route-driven workspace state and session-only last-route tracking.
// ABOUTME: Verifies navigation changes preserve query strings without creating durable state.

namespace Explore.Blazor.Client.Tests.Services.Shell;

using Explore.Blazor.Client.Services.Shell;

public sealed class UiShellStateTests : IDisposable
{
    private readonly BlazorTestContext _context = new();

    public void Dispose() => _context.Dispose();

    [Test]
    public async Task NavigationPublishesWorkspaceAndPreservesLastRouteQuery()
    {
        var navigation = _context.Services.GetRequiredService<NavigationManager>();
        var classifier = new WorkspaceRouteClassifier(new WorkspaceRegistry());
        using var state = new UiShellState(navigation, classifier);
        var changes = 0;
        state.Changed += () => changes++;

        navigation.NavigateTo("/events?q=iftar&format=online");
        navigation.NavigateTo("/settings?section=appearance");

        await Assert.That(state.ActiveWorkspace).IsEqualTo(WorkspaceKey.Settings);
        await Assert.That(state.GetLastRoute(WorkspaceKey.Events)).IsEqualTo("/events?q=iftar&format=online");
        await Assert.That(state.GetLastRoute(WorkspaceKey.Settings)).IsEqualTo("/settings?section=appearance");
        await Assert.That(changes).IsEqualTo(2);
    }
}
