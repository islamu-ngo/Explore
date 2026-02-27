// ABOUTME: Component tests for tenant lookup tables section loading/error/success states.
// ABOUTME: Verifies parallel lookup loading and consolidated lookup tab rendering.

using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class LookupTablesTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IAdminService _adminService;
    private readonly ISnackbar _snackbar;

    public LookupTablesTests()
    {
        _ctx = new BlazorTestContext();
        _adminService = Substitute.For<IAdminService>();
        _snackbar = Substitute.For<ISnackbar>();

        _ctx.Services.AddSingleton(_adminService);
        _ctx.Services.AddSingleton(_snackbar);

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Admin User", "admin@example.com");

        SetupDefaultLookups();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private IRenderedComponent<DynamicComponent> RenderLookupTables()
    {
        var componentType = typeof(IAdminService).Assembly.GetType("Explore.Blazor.Client.Pages.Admin.Tenant.Components.TenantLookupTablesSection")
                            ?? throw new InvalidOperationException("TenantLookupTablesSection component type not found");

        return _ctx.RenderMudComponent<DynamicComponent>(p => p.Add(x => x.Type, componentType));
    }

    [Test]
    public async Task LookupTables_ShowsLoadingIndicator_WhileLookupLoadPending()
    {
        // Arrange
        var pendingEventTypes = new TaskCompletionSource<ICollection<EventTypeListDto>>();
        _adminService.GetEventTypesAsync().Returns(pendingEventTypes.Task);

        // Act
        var cut = RenderLookupTables();

        // Assert
        await Assert.That(cut.Markup).Contains("Lookup Tables");
        await Assert.That(cut.Markup).Contains("Loading lookup tables");

        // Cleanup
        pendingEventTypes.TrySetResult(new List<EventTypeListDto>());
    }

    [Test]
    public async Task LookupTables_ShowsLoadedContent_WhenLookupsSucceed()
    {
        // Arrange
        _adminService.GetEventTypesAsync().Returns(
        [
            new EventTypeListDto
            {
                Id = 1,
                FullName = "Conference",
                Description = "Large gathering"
            }
        ]);

        // Act
        var cut = RenderLookupTables();
        cut.WaitForState(() => cut.Markup.Contains("Lookup Tables", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Event Types");
        await Assert.That(cut.Markup).Contains("Tags");
    }

    [Test]
    public async Task LookupTables_UsesSnackbarError_WhenAnyLookupFails()
    {
        // Arrange
        _adminService.GetEventFormatsAsync().ThrowsAsync(new InvalidOperationException("boom"));

        // Act
        var cut = RenderLookupTables();
        cut.WaitForState(() => cut.Markup.Contains("Failed to load lookup data", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Failed to load lookup data: boom");
    }

    private void SetupDefaultLookups()
    {
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
