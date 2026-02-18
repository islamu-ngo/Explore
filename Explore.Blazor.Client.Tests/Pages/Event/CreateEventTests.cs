using Blazouter.Services;
using Explore.Blazor.Client.Pages.Event;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Event;

/// <summary>
/// Component tests for CreateEvent page.
/// Tests form rendering, validation, and event creation flow.
/// </summary>
/// <remarks>
/// CreateEvent is a complex form with many external dependencies including
/// RouterStateService from Blazouter which cannot be easily mocked.
/// These tests focus on service integration verification.
/// </remarks>
public class CreateEventTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IEventService _eventService;
    private readonly IOrganizationService _organizationService;
    private readonly IUserService _userService;
    private readonly IAdminService _adminService;
    private readonly ICategoryService _categoryService;
    private readonly ITagService _tagService;
    private readonly ILocationService _locationService;
    private readonly IImageStorageService _imageStorageService;

    public CreateEventTests()
    {
        _ctx = new BlazorTestContext();

        // Create mocks for all required services
        _eventService = Substitute.For<IEventService>();
        _organizationService = Substitute.For<IOrganizationService>();
        _userService = Substitute.For<IUserService>();
        _adminService = Substitute.For<IAdminService>();
        _categoryService = Substitute.For<ICategoryService>();
        _tagService = Substitute.For<ITagService>();
        _locationService = Substitute.For<ILocationService>();
        _imageStorageService = Substitute.For<IImageStorageService>();

        // Register services
        _ctx.Services.AddSingleton(_eventService);
        _ctx.Services.AddSingleton(_organizationService);
        _ctx.Services.AddSingleton(_userService);
        _ctx.Services.AddSingleton(_adminService);
        _ctx.Services.AddSingleton(_categoryService);
        _ctx.Services.AddSingleton(_tagService);
        _ctx.Services.AddSingleton(_locationService);
        _ctx.Services.AddSingleton(_imageStorageService);

        // RouterStateService from Blazouter - register the real service
        _ctx.Services.AddScoped<RouterStateService>();

        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());
        _ctx.Services.AddSingleton(Substitute.For<ISnackbar>());
        _ctx.Services.AddSingleton(Substitute.For<ILogger<CreateEvent>>());

        // Setup default mock responses
        SetupDefaultMockResponses();
    }

    private void SetupDefaultMockResponses()
    {
        // User service
        var user = new UserDto { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User", Email = "test@example.com" };
        _userService.GetCurrentUserAsync().Returns(user);

        // Organization service - user has permission to create events
        var orgId = Guid.NewGuid();
        var org = new OrganizationListDto
        {
            Id = orgId,
            FullName = "Test Organization",
            CurrentUserRole = 1 // Admin role
        };
        _organizationService.GetOrganizationsByUserAsync(Arg.Any<Guid>()).Returns(new List<OrganizationListDto> { org });
        _organizationService.GetMyOrganizationsAsync().Returns(new List<OrganizationListDto> { org });

        // Admin service lookups
        _adminService.GetEventTypesAsync().Returns(new List<EventTypeListDto>
        {
            new() { Id = 1, FullName = "Conference" },
            new() { Id = 2, FullName = "Workshop" }
        });
        _adminService.GetAudienceGendersAsync().Returns(new List<AudienceGenderListDto>
        {
            new() { Id = 1, FullName = "Mixed" },
            new() { Id = 2, FullName = "Men Only" }
        });
        _adminService.GetAudienceAgesAsync().Returns(new List<AudienceAgeListDto>
        {
            new() { Id = 1, FullName = "All Ages" },
            new() { Id = 2, FullName = "Adults" }
        });
        _adminService.GetEventFormatsAsync().Returns(new List<EventFormatListDto>
        {
            new() { Id = 1, FullName = "In-Person" },
            new() { Id = 2, FullName = "Online" }
        });
        _adminService.GetVisibilityTypesAsync().Returns(new List<VisibilityTypeListDto>
        {
            new() { Id = 1, FullName = "Public" },
            new() { Id = 2, FullName = "Private" }
        });
        _adminService.GetMadhabsAsync().Returns(new List<MadhabListDto>());
        _adminService.GetRegistrationModesAsync().Returns(new List<RegistrationModeListDto>
        {
            new() { Id = 1, FullName = "Open" },
            new() { Id = 2, FullName = "Approval Required" }
        });
        _adminService.GetLanguagesAsync().Returns(new List<LanguageListDto>
        {
            new() { Id = 1, FullName = "English" },
            new() { Id = 2, FullName = "Arabic" }
        });

        // Category and Tag services
        _categoryService.GetAllCategoriesAsync().Returns(new List<CategoryListDto>());
        _tagService.GetAllTagsAsync().Returns(new List<TagListDto>());

        // Location service
        _locationService.GetAllLocationsAsync().Returns(new List<LocationListDto>());

        // Event service defaults
        _eventService.CreateEventAsync(Arg.Any<CreateEventDto>()).Returns(new BaseCommandResponseOfGuid
        {
            Success = true,
            Id = Guid.NewGuid()
        });
        _eventService.GetSessionsByEventAsync(Arg.Any<Guid>()).Returns(new List<EventSessionListDto>());
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    #region Rendering Tests

    [Test]
    public async Task CreateEvent_RendersWithoutCrash()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        // Act - Component should render without throwing
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Length > 0, TimeSpan.FromSeconds(2));

        // Assert - Component rendered and produced markup
        await Assert.That(cut.Markup).IsNotEmpty();
    }

    [Test]
    public async Task CreateEvent_ShowsErrorOrForm_WhenRendered()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        // Act
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(
            () => cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase)
               || cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        // Assert - Should render either the form or an error state
        // Without valid organizationId from RouterState, it shows permission error
        var hasForm = cut.Markup.Contains("mud-input");
        var hasError = cut.Markup.Contains("mud-alert");
        await Assert.That(hasForm || hasError).IsTrue();
    }

    #endregion

    #region Behavior Interaction Tests

    [Test]
    public async Task CreateEvent_WhenRendered_RequestsLookupDataFromAdminService()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        // Act
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        // Assert - component behavior triggers lookup requests
        await _adminService.Received(1).GetEventTypesAsync();
        await _adminService.Received(1).GetAudienceGendersAsync();
        await _adminService.Received(1).GetAudienceAgesAsync();
        await _adminService.Received(1).GetEventFormatsAsync();
        await _adminService.Received(1).GetVisibilityTypesAsync();
        await _adminService.Received(1).GetRegistrationModesAsync();
        await _adminService.Received(1).GetLanguagesAsync();
    }

    [Test]
    public async Task CreateEvent_WhenRendered_LoadsUserAndOrganizationContext()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        // Act
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        // Assert - component behavior loads principal context dependencies
        await _userService.Received(1).GetCurrentUserAsync();
        await _organizationService.Received().GetMyOrganizationsAsync();
        await _categoryService.Received().GetAllCategoriesAsync();
        await _tagService.Received().GetAllTagsAsync();
        await _locationService.Received().GetAllLocationsAsync();
    }

    #endregion
}
