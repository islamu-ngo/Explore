// ABOUTME: Component tests for lookup tables section location-related loading/error/success states.
// ABOUTME: Verifies location data appears in consolidated tenant lookup management UI.

using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class LocationsTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IAdminService _adminService;

    public LocationsTests()
    {
        _ctx = new BlazorTestContext();
        _adminService = Substitute.For<IAdminService>();

        _ctx.Services.AddSingleton(_adminService);
        _ctx.Services.AddSingleton(Substitute.For<ISnackbar>());
        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Admin User", "admin@example.com");

        SetupDefaultLookups();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private IRenderedComponent<DynamicComponent> RenderLocations()
    {
        var componentType = typeof(IAdminService).Assembly.GetType("Explore.Blazor.Client.Components.Admin.Tenant.TenantLookupTablesSection")
                            ?? throw new InvalidOperationException("TenantLookupTablesSection component type not found");

        return _ctx.RenderMudComponent<DynamicComponent>(p => p.Add(x => x.Type, componentType));
    }

    private static void SelectTab(IRenderedComponent<DynamicComponent> cut, string tabName)
    {
        var tab = cut.FindAll(".mud-tab").First(x => x.TextContent.Contains(tabName, StringComparison.OrdinalIgnoreCase));
        tab.Click();
    }

    [Test]
    public async Task Locations_ShowsLoadingState_WhileFetchIsPending()
    {
        // Arrange
        var pending = new TaskCompletionSource<ICollection<LocationListDto>>();
        _adminService.GetLocationsAsync().Returns(pending.Task);

        // Act
        var cut = RenderLocations();

        // Assert
        await Assert.That(cut.Markup).Contains("Loading lookup tables");

        // Cleanup
        pending.TrySetResult(new List<LocationListDto>());
    }

    [Test]
    public async Task Locations_ShowsEmptyState_WhenNoLocationsReturned()
    {
        // Arrange
        _adminService.GetLocationsAsync().Returns(new List<LocationListDto>());

        // Act
        var cut = RenderLocations();
        cut.WaitForState(() => cut.Markup.Contains("Lookup Tables", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));
        SelectTab(cut, "Locations");

        // Assert
        await Assert.That(cut.Markup).Contains("Locations");
        await Assert.That(cut.Markup).Contains("Search locations");
    }

    [Test]
    public async Task Locations_ShowsLocationRows_WhenDataExists()
    {
        // Arrange
        _adminService.GetLocationsAsync().Returns(
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
        SelectTab(cut, "Locations");
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
        _adminService.GetLocationsAsync().ThrowsAsync(new InvalidOperationException("boom"));

        // Act
        var cut = RenderLocations();
        cut.WaitForState(() => cut.Markup.Contains("Failed to load lookup data", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Failed to load lookup data: boom");
    }

    private void SetupDefaultLookups()
    {
        _adminService.GetCategoriesAsync().Returns(new List<CategoryListDto>());
        _adminService.GetTagsAsync().Returns(new List<TagListDto>());
        _adminService.GetLocationsAsync().Returns(new List<LocationListDto>());
        _adminService.GetEventTypesAsync().Returns(new List<EventTypeListDto>());
        _adminService.GetEventFormatsAsync().Returns(new List<EventFormatListDto>());
        _adminService.GetEventStatusesAsync().Returns(new List<EventStatusListDto>());
        _adminService.GetVisibilityTypesAsync().Returns(new List<VisibilityTypeListDto>());
        _adminService.GetRegistrationModesAsync().Returns(new List<RegistrationModeListDto>());
        _adminService.GetAudienceGendersAsync().Returns(new List<AudienceGenderListDto>());
        _adminService.GetAudienceAgesAsync().Returns(new List<AudienceAgeListDto>());
        _adminService.GetMadhabsAsync().Returns(new List<MadhabListDto>());
        _adminService.GetLanguagesAsync().Returns(new List<LanguageListDto>());
        _adminService.GetOrganizationPositionsAsync().Returns(new List<OrganizationPositionListDto>());
        _adminService.GetApprovalStatusesAsync().Returns(new List<StatusTypeListDto>());
        _adminService.GetActorTypesAsync().Returns(new List<ActorTypeListDto>());
        _adminService.GetFileTypesAsync().Returns(new List<FileTypeListDto>());
        _adminService.GetDidCustodyTypesAsync().Returns(new List<DidCustodyTypeListDto>());
    }
}
