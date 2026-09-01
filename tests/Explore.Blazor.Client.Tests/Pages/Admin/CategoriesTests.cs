// ABOUTME: Component tests for lookup tables section category-related loading/error/success states.
// ABOUTME: Verifies category data appears in consolidated tenant lookup management UI.

using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Pages.Admin.Tenant.Components;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class CategoriesTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IAdminService _adminService;

    public CategoriesTests()
    {
        _ctx = new BlazorTestContext();
        _adminService = Substitute.For<IAdminService>();

        _ctx.Services.AddSingleton(_adminService);
        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());
        _ctx.Services.AddSingleton(Substitute.For<ISnackbar>());

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Admin User", "admin@example.com");

        SetupDefaultLookups();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private IRenderedComponent<TenantLookupTablesSection> RenderCategories() =>
        _ctx.RenderMudComponent<TenantLookupTablesSection>();

    [Test]
    public async Task Categories_ShowsLoadingState_WhileFetchIsPending()
    {
        // Arrange
        var pending = new TaskCompletionSource<ICollection<CategoryListDto>>();
        _adminService.GetCategoriesAsync().Returns(pending.Task);

        // Act
        var cut = RenderCategories();

        // Assert
        await Assert.That(cut.Markup).Contains("Loading lookup tables");

        // Cleanup
        pending.TrySetResult(new List<CategoryListDto>());
    }

    [Test]
    public async Task Categories_ShowsErrorAlert_WhenFetchThrows()
    {
        // Arrange
        _adminService.GetCategoriesAsync().ThrowsAsync(new InvalidOperationException("boom"));

        // Act
        var cut = RenderCategories();
        cut.WaitForState(() => cut.Markup.Contains("Failed to load lookup data", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Failed to load lookup data: boom");
    }

    [Test]
    public async Task Categories_ShowsEmptyState_WhenNoRecordsReturned()
    {
        // Arrange
        _adminService.GetCategoriesAsync().Returns(new List<CategoryListDto>());

        // Act
        var cut = RenderCategories();
        cut.WaitForState(() => cut.Markup.Contains("Lookup Tables", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Categories");
        await Assert.That(cut.Markup).Contains("Search categories");
    }

    [Test]
    public async Task Categories_ShowsCategoryRows_WhenDataExists()
    {
        // Arrange
        _adminService.GetCategoriesAsync().Returns(
        [
            new CategoryListDto
            {
                Id = Guid.NewGuid(),
                FullName = "Faith & Spirituality",
                MasterCode = "FTH",
                ParentFullName = "Islamic"
            }
        ]);

        // Act
        var cut = RenderCategories();
        cut.WaitForState(() => cut.Markup.Contains("Spirituality", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Spirituality");
        await Assert.That(cut.Markup).Contains("FTH");
        await Assert.That(cut.Markup).Contains("Islamic");
    }

    private void SetupDefaultLookups()
    {
        _adminService.GetCategoriesAsync().Returns(new List<CategoryListDto>());
        _adminService.GetTagsAsync().Returns(new List<TagListDto>());
        _adminService.GetLocationsAsync()
            .Returns(new HalCollectionResourceOfLocationListDto());
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
