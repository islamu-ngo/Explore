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
        var componentType = typeof(IAdminService).Assembly.GetType("Explore.Blazor.Client.Pages.Admin.Tenant.Components.TenantLookupTablesSection")
                            ?? throw new InvalidOperationException("TenantLookupTablesSection component type not found");

        return _ctx.RenderMudComponent<DynamicComponent>(p => p.Add(x => x.Type, componentType));
    }

    private static void SelectTab(IRenderedComponent<DynamicComponent> cut, string tabName)
    {
        var tab = cut.FindAll("[role='tab']").First(x => x.TextContent.Contains(tabName, StringComparison.OrdinalIgnoreCase));
        tab.Click();
    }

    [Test]
    public async Task Locations_ShowsLoadingState_WhileFetchIsPending()
    {
        // Arrange
        var pending =
            new TaskCompletionSource<HalCollectionResourceOfLocationListDto>();
        _adminService.GetLocationsAsync().Returns(pending.Task);

        // Act
        var cut = RenderLocations();

        // Assert
        await Assert.That(cut.Markup).Contains("Loading lookup tables");

        // Cleanup
        pending.TrySetResult(LocationCollection());
    }

    [Test]
    public async Task Locations_ShowsEmptyState_WhenNoLocationsReturned()
    {
        // Arrange
        _adminService.GetLocationsAsync().Returns(LocationCollection());

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
        _adminService.GetLocationsAsync().Returns(LocationCollection(
            new LocationListDto
            {
                Id = Guid.NewGuid(),
                FullName = "Main Mosque Hall",
                City = "Brussels",
                Country = "Belgium",
                Address = "1 Unity St"
            }
        ));

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
    public async Task Locations_HidesWriteControlsWithoutHalCapabilities()
    {
        _adminService.GetLocationsAsync().Returns(LocationCollection(
            new LocationListDto
            {
                Id = Guid.CreateVersion7(),
                FullName = "Read-only Hall",
                Address = "1 Safe Street",
                City = "Brussels",
                Country = "Belgium"
            }));

        IRenderedComponent<DynamicComponent> cut = RenderLocations();
        SelectTab(cut, "Locations");
        cut.WaitForState(
            () => cut.Markup.Contains(
                "Read-only Hall",
                StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        bool hasCreate = cut.FindAll("button")
            .Any(button => button.TextContent.Trim() == "Create");
        await Assert.That(hasCreate).IsFalse();
        await Assert.That(cut.FindAll("button[aria-label^='Edit'], button[aria-label^='Delete']")).IsEmpty();
    }

    [Test]
    public async Task Locations_ShowsOnlyAdvertisedWriteControls()
    {
        Guid locationId = Guid.CreateVersion7();
        HalCollectionResourceOfLocationListDto resource = LocationCollection(
            new LocationListDto
            {
                Id = locationId,
                FullName = "Managed Hall",
                Address = "2 Safe Street",
                City = "Brussels",
                Country = "Belgium"
            });
        resource._links = new Dictionary<string, HalLink>
        {
            ["create"] = LocationLink("POST", "/api/location")
        };
        HalResourceOfLocationListDto item =
            resource._embedded!.Items!.Single();
        item._links = new Dictionary<string, HalLink>
        {
            ["self"] = LocationLink("GET", $"/api/location/{locationId:D}"),
            ["edit"] = LocationLink("PATCH", $"/api/location/{locationId:D}"),
            ["delete"] = LocationLink("DELETE", $"/api/location/{locationId:D}")
        };
        _adminService.GetLocationsAsync().Returns(resource);

        IRenderedComponent<DynamicComponent> cut = RenderLocations();
        SelectTab(cut, "Locations");
        cut.WaitForState(
            () => cut.Markup.Contains(
                "Managed Hall",
                StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        await Assert.That(cut.FindAll("[data-testid='location-create']"))
            .HasSingleItem();
        await Assert.That(
                cut.FindAll($"[data-testid='location-edit-{locationId:D}']"))
            .HasSingleItem();
        await Assert.That(
                cut.FindAll($"[data-testid='location-delete-{locationId:D}']"))
            .HasSingleItem();
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
        _adminService.GetLocationsAsync().Returns(LocationCollection());
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

    private static HalCollectionResourceOfLocationListDto LocationCollection(
        params LocationListDto[] items) => new()
        {
            _embedded = new HalCollectionEmbeddedOfLocationListDto
            {
                Items = items.Select(item =>
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(item);
                    return System.Text.Json.JsonSerializer
                        .Deserialize<HalResourceOfLocationListDto>(json)
                        ?? new HalResourceOfLocationListDto();
                }).ToArray()
            }
        };

    private static HalLink LocationLink(string method, string href) => new()
    {
        Method = method,
        Href = href
    };
}
