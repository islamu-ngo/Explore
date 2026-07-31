// ABOUTME: Service tests for registration-order lifecycle transport and safe guest recovery failures.
// ABOUTME: Proves missing capability transport metadata never escapes into the page flow.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Shell;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class RegistrationOrderServiceTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly IEventApiClient _apiClient;
    private readonly RegistrationOrderService _service;

    public RegistrationOrderServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        var eventService = Substitute.For<IEventService>();
        var capabilityStore = Substitute.For<IGuestRegistrationOrderCapabilityStore>();
        var logger = Substitute.For<ILogger<RegistrationOrderService>>();
        _ctx.Services.AddScoped<IWorkspaceRegistry, WorkspaceRegistry>();
        _ctx.Services.AddScoped<WorkspaceRouteClassifier>();
        _ctx.Services.AddScoped<UiShellState>();
        var shellState = _ctx.Services.GetRequiredService<UiShellState>();
        _service = new RegistrationOrderService(_apiClient, eventService, shellState, capabilityStore, logger);
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task StartGuestAsync_WhenCapabilityHeaderIsMissing_ReturnsNullInsteadOfEscaping()
    {
        _apiClient.StartGuestRegistrationOrderWithCapabilityAsync(
                Arg.Any<Guid>(),
                Arg.Any<StartRegistrationOrderRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GuestRegistrationOrderStartResult>(
                new InvalidOperationException("Guest registration capability was not returned.")));

        var result = await _service.StartGuestAsync(Guid.CreateVersion7(), new StartRegistrationOrderRequest());

        await Assert.That(result).IsNull();
        await _apiClient.Received(1).StartGuestRegistrationOrderWithCapabilityAsync(
            Arg.Any<Guid>(),
            Arg.Any<StartRegistrationOrderRequest>(),
            Arg.Any<CancellationToken>());
    }
}
