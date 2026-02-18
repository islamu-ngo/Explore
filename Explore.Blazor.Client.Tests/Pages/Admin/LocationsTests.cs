// ABOUTME: Component tests for Locations admin page loading/error/empty/success states.
// ABOUTME: Verifies list rendering and snackbar-based failure handling.

using Explore.Blazor.Client.Pages.Admin;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class LocationsTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly ILocationService _locationService;
    private readonly ISnackbar _snackbar;

    public LocationsTests()
    {
        _ctx = new BlazorTestContext();
        _locationService = Substitute.For<ILocationService>();
        _snackbar = Substitute.For<ISnackbar>();

        _ctx.Services.AddSingleton(_locationService);
        _ctx.Services.AddSingleton(_snackbar);
        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Admin User", "admin@example.com");
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private IRenderedComponent<DynamicComponent> RenderLocations()
    {
        var componentType = typeof(AdminList).Assembly.GetType("Explore.Blazor.Client.Pages.Admin.Locations")
                            ?? throw new InvalidOperationException("Locations component type not found");

        return _ctx.RenderMudComponent<DynamicComponent>(p => p.Add(x => x.Type, componentType));
    }

    [Test]
    public async Task Locations_ShowsLoadingState_WhileFetchIsPending()
    {
        // Arrange
        var pending = new TaskCompletionSource<ICollection<LocationListDto>>();
        _locationService.GetLocations().Returns(pending.Task);

        // Act
        var cut = RenderLocations();

        // Assert
        await Assert.That(cut.Markup).Contains("Loading locations");

        // Cleanup
        pending.TrySetResult(new List<LocationListDto>());
    }

    [Test]
    public async Task Locations_ShowsEmptyState_WhenNoLocationsReturned()
    {
        // Arrange
        _locationService.GetLocations().Returns(new List<LocationListDto>());

        // Act
        var cut = RenderLocations();
        cut.WaitForState(() => cut.Markup.Contains("No locations found", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("No locations found");
        await Assert.That(cut.Markup).Contains("Create your first location to get started.");
    }

    [Test]
    public async Task Locations_ShowsLocationRows_WhenDataExists()
    {
        // Arrange
        _locationService.GetLocations().Returns(
        [
            new LocationListDto
            {
                Id = Guid.NewGuid(),
                FullName = "Main Mosque Hall",
                City = "Brussels",
                Country = "Belgium",
                Address = "1 Unity St"
            }
        ]);

        // Act
        var cut = RenderLocations();
        cut.WaitForState(() => cut.Markup.Contains("Main Mosque Hall", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Main Mosque Hall");
        await Assert.That(cut.Markup).Contains("Brussels");
        await Assert.That(cut.Markup).Contains("Belgium");
    }

    [Test]
    public async Task Locations_UsesSnackbarError_WhenLoadFails()
    {
        // Arrange
        _locationService.GetLocations().ThrowsAsync(new InvalidOperationException("boom"));

        // Act
        var cut = RenderLocations();
        cut.WaitForState(() => cut.Markup.Contains("No locations found", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        _snackbar.Received().Add(Arg.Is<string>(s => s.Contains("Failed to load locations", StringComparison.OrdinalIgnoreCase)), Severity.Error);
    }
}
