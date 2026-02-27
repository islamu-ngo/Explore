// ABOUTME: Focused bUnit tests for EventList loading and empty-state behavior.
// ABOUTME: Verifies stable UX state transitions with Virtualize-backed API paging.

using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Pages.Events;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Event;

public class EventListTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IEventService _eventService;
    private readonly ICategoryService _categoryService;
    private readonly ITagService _tagService;
    private readonly IAdminService _adminService;
    private readonly ILocationService _locationService;
    private readonly IEventRegistrationService _registrationService;
    private readonly IPublicExperienceService _publicExperienceService;

    public EventListTests()
    {
        _ctx = new BlazorTestContext();

        _eventService = Substitute.For<IEventService>();
        _categoryService = Substitute.For<ICategoryService>();
        _tagService = Substitute.For<ITagService>();
        _adminService = Substitute.For<IAdminService>();
        _locationService = Substitute.For<ILocationService>();
        _registrationService = Substitute.For<IEventRegistrationService>();
        _publicExperienceService = Substitute.For<IPublicExperienceService>();

        _ctx.Services.AddSingleton(_eventService);
        _ctx.Services.AddSingleton(_categoryService);
        _ctx.Services.AddSingleton(_tagService);
        _ctx.Services.AddSingleton(_adminService);
        _ctx.Services.AddSingleton(_locationService);
        _ctx.Services.AddSingleton(_registrationService);
        _ctx.Services.AddSingleton(_publicExperienceService);

        _ctx.Services.AddSingleton(Substitute.For<IUserService>());
        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());
        _ctx.Services.AddSingleton(Substitute.For<ISnackbar>());
        _ctx.Services.AddSingleton(Substitute.For<ILogger<EventList>>());
        _ctx.Services.AddSingleton(Substitute.For<IAuthStateService>());
        _ctx.Services.AddSingleton(new Explore.Blazor.Client.Services.SidebarState());

        SetupDefaultLookupResponses();
        _publicExperienceService.GetSettingsAsync().Returns(new PublicExperienceSettingsModel
        {
            IsIslamicModuleEnabled = true,
            IsTechModuleEnabled = true
        });
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private void SetupDefaultLookupResponses()
    {
        _adminService.GetEventTypesAsync().Returns(new List<EventTypeListDto>());
        _adminService.GetAudienceGendersAsync().Returns(new List<AudienceGenderListDto>());
        _adminService.GetAudienceAgesAsync().Returns(new List<AudienceAgeListDto>());
        _adminService.GetEventStatusesAsync().Returns(new List<EventStatusListDto>());
        _eventService.GetEventFormatsAsync().Returns(new List<EventFormatListDto>());
        _categoryService.GetAllCategoriesAsync().Returns(new List<CategoryListDto>());
        _tagService.GetAllTagsAsync().Returns(new List<TagListDto>());
        _adminService.GetMadhabsAsync().Returns(new List<MadhabListDto>());
        _locationService.GetAllLocationsAsync().Returns(new List<LocationListDto>());
        _adminService.GetRegistrationModesAsync().Returns(new List<RegistrationModeListDto>());
        _adminService.GetLanguagesAsync().Returns(new List<LanguageListDto>());
        _eventService.GetRegistrationsByUserAsync(Arg.Any<Guid>()).Returns(new List<EventRegistrationListDto>());
    }

    private static PaginatedResult<EventListDto> CreateResult(int pageNumber, int pageSize, List<EventListDto> items)
    {
        return new PaginatedResult<EventListDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = items.Count
        };
    }

    private void SetupPagedResult(Task<PaginatedResult<EventListDto>> resultTask)
    {
        _eventService.GetEventsPagedAsync(
            Arg.Any<int>(),                    // pageNumber
            Arg.Any<int>(),                    // pageSize
            Arg.Any<string?>(),                // searchTerm
            Arg.Any<Guid?>(),                  // categoryId
            Arg.Any<List<Guid>?>(),            // includedCategoryIds
            Arg.Any<List<Guid>?>(),            // excludedCategoryIds
            Arg.Any<string?>(),                // categoryInclusionMode
            Arg.Any<string?>(),                // categoryExclusionMode
            Arg.Any<List<Guid>?>(),            // includedTagIds
            Arg.Any<List<Guid>?>(),            // excludedTagIds
            Arg.Any<string?>(),                // inclusionMode
            Arg.Any<string?>(),                // exclusionMode
            Arg.Any<List<int>?>(),             // formatIds
            Arg.Any<List<int>?>(),             // madhabIds
            Arg.Any<List<Guid>?>(),            // locationIds
            Arg.Any<List<int>?>(),             // registrationModeIds
            Arg.Any<List<int>?>(),             // languageIds
            Arg.Any<DateTimeOffset?>(),        // dateFrom
            Arg.Any<DateTimeOffset?>(),        // dateTo
            Arg.Any<string?>(),                // sortBy
            Arg.Any<bool?>(),                  // sortDescending
            Arg.Any<List<int>?>(),             // eventTypeIds
            Arg.Any<List<int>?>(),             // audienceGenderIds
            Arg.Any<List<int>?>(),             // audienceAgeIds
            Arg.Any<List<int>?>(),             // eventStatusIds
            Arg.Any<List<int>?>(),             // genderModeIds
            Arg.Any<bool?>(),                  // includesQuranRecitation
            Arg.Any<List<int>?>(),             // referencePrayerIds
            Arg.Any<List<int>?>(),             // islamicPrimaryLanguageIds
            Arg.Any<bool?>(),                  // hasIslamicAspect
            Arg.Any<int?>(),                   // skillLevelId
            Arg.Any<bool?>(),                  // isCodingCompetition
            Arg.Any<bool?>(),                  // isHackathon
            Arg.Any<bool?>(),                  // requiresLaptop
            Arg.Any<string?>(),                // techStackTag
            Arg.Any<bool?>(),                  // hasTechAspect
            Arg.Any<CancellationToken>())
            .Returns(resultTask);
    }

    private static async Task InvokeLoadEventsAsync(IRenderedComponent<EventList> cut)
    {
        var loadEventsMethod = typeof(EventList).GetMethod("LoadEventsAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (loadEventsMethod is null)
        {
            throw new InvalidOperationException("LoadEventsAsync method was not found on EventList.");
        }

        var request = new ItemsProviderRequest(startIndex: 0, count: 20, cancellationToken: CancellationToken.None);
        await cut.InvokeAsync(async () =>
        {
            var valueTask = (ValueTask<ItemsProviderResult<EventListDto>>)loadEventsMethod.Invoke(cut.Instance, [request])!;
            await valueTask;
        });
    }

    [Test]
    public async Task EventList_DoesNotShowEmptyState_BeforeFirstEventsLoadCompletes()
    {
        // Arrange
        var pendingResult = new TaskCompletionSource<PaginatedResult<EventListDto>>();
        SetupPagedResult(pendingResult.Task);

        // Act
        var cut = _ctx.RenderMudComponent<EventList>();

        // Assert
        if (cut.Markup.Contains("No events found"))
        {
            throw new InvalidOperationException("Empty state should not be displayed before first events load completes.");
        }

        // Cleanup to avoid dangling async work
        pendingResult.TrySetResult(CreateResult(1, 20, []));
        await Task.CompletedTask;
    }

    [Test]
    public async Task EventList_ShowsNoEventsState_OnlyAfterInitialLoadCompletesWithEmptyResult()
    {
        // Arrange
        var pendingResult = new TaskCompletionSource<PaginatedResult<EventListDto>>();
        SetupPagedResult(pendingResult.Task);

        // Act
        var cut = _ctx.RenderMudComponent<EventList>();

        // Assert pre-condition: while pending, empty state is not shown
        cut.WaitForAssertion(() =>
        {
            if (cut.Markup.Contains("No events found"))
            {
                throw new InvalidOperationException("Empty state should not be visible before first event load completes.");
            }
        });

        // Complete with empty result and manually trigger the virtualized provider
        pendingResult.SetResult(CreateResult(1, 20, []));
        await InvokeLoadEventsAsync(cut);

        if (!cut.Markup.Contains("No events found"))
        {
            throw new InvalidOperationException("Expected empty-state message after completed empty load.");
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task EventList_HidesNoEventsState_WhenResultsExist()
    {
        // Arrange
        var events = new List<EventListDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Blazor Summit", Description = "Event description" }
        };
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, events)));

        // Act
        var cut = _ctx.RenderMudComponent<EventList>();

        // Assert
        await InvokeLoadEventsAsync(cut);

        if (!cut.Markup.Contains("Blazor Summit"))
        {
            throw new InvalidOperationException("Expected event title in rendered markup.");
        }

        if (cut.Markup.Contains("No events found"))
        {
            throw new InvalidOperationException("Empty-state message should not be visible when events exist.");
        }

        await Task.CompletedTask;
    }
}
