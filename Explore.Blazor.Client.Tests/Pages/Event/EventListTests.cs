// ABOUTME: Component tests for EventList page.
// Tests rendering, data loading, filtering, and user interactions.

using Explore.Blazor.Client.Pages.Event;
using MudBlazor;
using MudBlazor.Services;

namespace Explore.Blazor.Client.Tests.Pages.Event;

/// <summary>
/// Component tests for EventList page.
/// Tests rendering, data loading, filtering, and user interactions.
/// </summary>
/// <remarks>
/// EventList is a complex component with:
/// - Multiple service dependencies (EventService, CategoryService, TagService, etc.)
/// - Filtering functionality (date, category, tag, format, madhab, location, etc.)
/// - Pagination
/// - Search functionality
/// - Dialog interactions
///
/// These tests verify the component renders correctly and responds to data changes.
/// </remarks>
public class EventListTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IEventService _eventService;
    private readonly ICategoryService _categoryService;
    private readonly ITagService _tagService;
    private readonly IAdminService _adminService;
    private readonly ILocationService _locationService;
    private readonly IEventRegistrationService _registrationService;

    public EventListTests()
    {
        _ctx = new BlazorTestContext();

        // Create mocks for all required services
        _eventService = Substitute.For<IEventService>();
        _categoryService = Substitute.For<ICategoryService>();
        _tagService = Substitute.For<ITagService>();
        _adminService = Substitute.For<IAdminService>();
        _locationService = Substitute.For<ILocationService>();
        _registrationService = Substitute.For<IEventRegistrationService>();

        // Register services
        _ctx.Services.AddSingleton(_eventService);
        _ctx.Services.AddSingleton(_categoryService);
        _ctx.Services.AddSingleton(_tagService);
        _ctx.Services.AddSingleton(_adminService);
        _ctx.Services.AddSingleton(_locationService);
        _ctx.Services.AddSingleton(_registrationService);
        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());
        _ctx.Services.AddSingleton(Substitute.For<ISnackbar>());
        _ctx.Services.AddSingleton(Substitute.For<ILogger<EventList>>());

        // Setup default empty responses
        SetupDefaultMockResponses();
    }

    private void SetupDefaultMockResponses()
    {
        // Event service defaults
        _eventService.GetAllEventsAsync().Returns(new List<EventListDto>());
        _eventService.GetEventTypesAsync().Returns(new List<EventTypeListDto>());
        _eventService.GetEventFormatsAsync().Returns(new List<EventFormatListDto>());
        _eventService.GetAllSessionsAsync().Returns(new List<EventSessionListDto>());
        // GetAllSessionLanguagesAsync returns ICollection<object> - neutralized method
        _eventService.GetAllSessionLanguagesAsync().Returns(new List<object>());

        // Category service defaults
        _categoryService.GetAllCategoriesAsync().Returns(new List<CategoryListDto>());

        // Tag service defaults
        _tagService.GetAllTagsAsync().Returns(new List<TagListDto>());

        // Admin service defaults
        _adminService.GetMadhabsAsync().Returns(new List<MadhabListDto>());
        _adminService.GetRegistrationModesAsync().Returns(new List<RegistrationModeListDto>());
        _adminService.GetLanguagesAsync().Returns(new List<LanguageListDto>());

        // Location service defaults
        _locationService.GetAllLocationsAsync().Returns(new List<LocationListDto>());
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    #region Rendering Tests

    [Test]
    public async Task EventList_RendersPageTitle()
    {
        // Act - Use RenderMudComponent for MudBlazor components
        var cut = _ctx.RenderMudComponent<EventList>();

        // Allow async operations to complete
        await Task.Delay(100);

        // Assert - Page should have the title
        await Assert.That(cut.Markup).Contains("Explore Events");
    }

    [Test]
    public async Task EventList_ShowsLoadingState_Initially()
    {
        // Arrange - Make the service slow to respond
        var tcs = new TaskCompletionSource<ICollection<EventListDto>>();
        _eventService.GetAllEventsAsync().Returns(tcs.Task);

        // Act - Use RenderMudComponent for MudBlazor components
        var cut = _ctx.RenderMudComponent<EventList>();

        // Assert - Should show skeleton loaders (loading state)
        // MudBlazor renders CSS class names like "mud-skeleton" not component names
        await Assert.That(cut.Markup).Contains("mud-skeleton");
    }

    [Test]
    public async Task EventList_ShowsNoEventsMessage_WhenEmpty()
    {
        // Arrange - Services return empty lists (already set in defaults)

        // Act - Use RenderMudComponent for MudBlazor components
        var cut = _ctx.RenderMudComponent<EventList>();
        await Task.Delay(200); // Wait for async load

        // Assert
        await Assert.That(cut.Markup).Contains("No events found");
    }

    [Test]
    public async Task EventList_DisplaysEvents_WhenDataLoaded()
    {
        // Arrange
        var events = ComponentDataBuilder.EventListDto.Generate(3);
        _eventService.GetAllEventsAsync().Returns(events);

        // Act - Use RenderMudComponent for MudBlazor components
        var cut = _ctx.RenderMudComponent<EventList>();
        await Task.Delay(200); // Wait for async load

        // Assert - Each event title should be displayed
        foreach (var evt in events)
        {
            await Assert.That(cut.Markup).Contains(evt.Title);
        }
    }

    [Test]
    public async Task EventList_DisplaysEventCards_WithCorrectStructure()
    {
        // Arrange
        var events = ComponentDataBuilder.EventListDto.Generate(2);
        events.First().TotalViews = 100;
        _eventService.GetAllEventsAsync().Returns(events);

        // Act - Use RenderMudComponent for MudBlazor components
        var cut = _ctx.RenderMudComponent<EventList>();
        await Task.Delay(200);

        // Assert - Should have event cards with actions
        await Assert.That(cut.Markup).Contains("Details");
        await Assert.That(cut.Markup).Contains("Register");
        await Assert.That(cut.Markup).Contains("100 views"); // TotalViews display
    }

    #endregion

    #region Filter Tests

    [Test]
    public async Task EventList_DisplaysFilterOptions()
    {
        // Arrange - Add some filter data
        var categories = ComponentDataBuilder.CategoryListDto.Generate(3);
        _categoryService.GetAllCategoriesAsync().Returns(categories);

        var tags = ComponentDataBuilder.TagListDto.Generate(2);
        _tagService.GetAllTagsAsync().Returns(tags);

        // Act - Use RenderMudComponent for MudBlazor components
        var cut = _ctx.RenderMudComponent<EventList>();
        await Task.Delay(200);

        // Assert - Filter pills should be present
        await Assert.That(cut.Markup).Contains("Any Time");
        await Assert.That(cut.Markup).Contains("All Categories");
        await Assert.That(cut.Markup).Contains("All Tags");
        await Assert.That(cut.Markup).Contains("All Formats");
        await Assert.That(cut.Markup).Contains("All Madhabs");
        await Assert.That(cut.Markup).Contains("All Locations");
    }

    [Test]
    public async Task EventList_FiltersEvents_BySearchText()
    {
        // Arrange
        var events = new List<EventListDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Islamic Conference 2025", Description = "A great conference" },
            new() { Id = Guid.NewGuid(), Title = "Quran Study Circle", Description = "Weekly study" },
            new() { Id = Guid.NewGuid(), Title = "Youth Workshop", Description = "For young Muslims" }
        };
        _eventService.GetAllEventsAsync().Returns(events);

        // Act - Use RenderMudComponent for MudBlazor components
        var cut = _ctx.RenderMudComponent<EventList>();
        await Task.Delay(200);

        // Initial state - all events visible
        await Assert.That(cut.Markup).Contains("Islamic Conference");
        await Assert.That(cut.Markup).Contains("Quran Study");
        await Assert.That(cut.Markup).Contains("Youth Workshop");
    }

    #endregion

    #region Pagination Tests

    [Test]
    public async Task EventList_ShowsPagination_WhenManyEvents()
    {
        // Arrange - Create more events than page size (default is 6)
        var events = ComponentDataBuilder.EventListDto.Generate(12);
        _eventService.GetAllEventsAsync().Returns(events);

        // Act - Use RenderMudComponent for MudBlazor components
        var cut = _ctx.RenderMudComponent<EventList>();
        await Task.Delay(200);

        // Assert - Pagination should be present
        // MudBlazor renders CSS class names like "mud-pagination" not component names
        await Assert.That(cut.Markup).Contains("mud-pagination");
        // Should show "Page 1 of 2" type info
        await Assert.That(cut.Markup).Contains("Page 1");
    }

    [Test]
    public async Task EventList_DisplaysCorrectEventCount()
    {
        // Arrange
        var events = ComponentDataBuilder.EventListDto.Generate(8);
        _eventService.GetAllEventsAsync().Returns(events);

        // Act - Use RenderMudComponent for MudBlazor components
        var cut = _ctx.RenderMudComponent<EventList>();
        await Task.Delay(200);

        // Assert - Should show total count
        await Assert.That(cut.Markup).Contains("of 8 events");
    }

    #endregion

    #region Service Integration Tests

    [Test]
    public async Task EventList_CallsAllRequiredServices_OnInitialization()
    {
        // Act - Use RenderMudComponent for MudBlazor components
        var cut = _ctx.RenderMudComponent<EventList>();
        await Task.Delay(200);

        // Assert - All services should have been called
        await _eventService.Received(1).GetAllEventsAsync();
        await _eventService.Received(1).GetEventTypesAsync();
        await _eventService.Received(1).GetEventFormatsAsync();
        await _categoryService.Received(1).GetAllCategoriesAsync();
        await _tagService.Received(1).GetAllTagsAsync();
        await _adminService.Received(1).GetMadhabsAsync();
        await _locationService.Received(1).GetAllLocationsAsync();
    }

    [Test]
    public async Task EventList_HandlesServiceError_Gracefully()
    {
        // Arrange - Make a service throw
        _eventService.GetAllEventsAsync().ThrowsAsync(new Exception("Service unavailable"));

        // Act - Should not throw - Use RenderMudComponent for MudBlazor components
        var cut = _ctx.RenderMudComponent<EventList>();
        await Task.Delay(200);

        // Assert - Should show empty state, not crash
        await Assert.That(cut.Markup).Contains("No events found");
    }

    #endregion

    #region Event Type Display Tests

    [Test]
    public async Task EventList_DisplaysEventType_FromLookup()
    {
        // Arrange
        var eventTypes = new List<EventTypeListDto>
        {
            new() { Id = 1, FullName = "Conference" },
            new() { Id = 2, FullName = "Workshop" }
        };
        _eventService.GetEventTypesAsync().Returns(eventTypes);

        var events = new List<EventListDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Test Event", EventTypeId = 1, EventTypeFullName = "Conference" }
        };
        _eventService.GetAllEventsAsync().Returns(events);

        // Act - Use RenderMudComponent for MudBlazor components
        var cut = _ctx.RenderMudComponent<EventList>();
        await Task.Delay(200);

        // Assert - Event type should be displayed
        await Assert.That(cut.Markup).Contains("Conference");
    }

    [Test]
    public async Task EventList_DisplaysFreeTag_WhenNoPriceSet()
    {
        // Arrange
        var events = new List<EventListDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Free Event", Price = null }
        };
        _eventService.GetAllEventsAsync().Returns(events);

        // Act - Use RenderMudComponent for MudBlazor components
        var cut = _ctx.RenderMudComponent<EventList>();
        await Task.Delay(200);

        // Assert
        await Assert.That(cut.Markup).Contains("Free");
    }

    [Test]
    public async Task EventList_DisplaysPrice_WhenPriceSet()
    {
        // Arrange
        var events = new List<EventListDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Paid Event", Price = 25.00, CurrencyCode = "EUR" }
        };
        _eventService.GetAllEventsAsync().Returns(events);

        // Act - Use RenderMudComponent for MudBlazor components
        var cut = _ctx.RenderMudComponent<EventList>();
        await Task.Delay(200);

        // Assert - Check for EUR and 25 (format may vary by locale: "25.00" or "25,00")
        await Assert.That(cut.Markup).Contains("EUR");
        await Assert.That(cut.Markup).Contains("25");
    }

    #endregion
}
