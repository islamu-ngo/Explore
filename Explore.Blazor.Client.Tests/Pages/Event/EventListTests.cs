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
        _ctx.Services.AddSingleton(Substitute.For<IContactShareConsentService>());
        _ctx.Services.AddSingleton(new Explore.Blazor.Client.Services.SidebarState());
        _ctx.Services.AddSingleton(Substitute.For<IUserSettingsService>());
        _ctx.Services.AddSingleton(new FeatureStateContainer());

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

    /// <summary>
    /// Invokes the private LoadEventsAsync Virtualize provider callback via reflection.
    /// This is an intentional workaround because bUnit cannot directly trigger a
    /// Virtualize component's ItemsProvider delegate. The method is the sole entry
    /// point for paged event loading and must be invoked to test empty-state transitions.
    /// All assertions use rendered markup (public output), not internal state.
    /// </summary>
    private static async Task InvokeLoadEventsAsync(IRenderedComponent<EventList> cut)
    {
        var loadEventsMethod = typeof(EventList)
            .GetMethod("LoadEventsAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LoadEventsAsync not found — Virtualize provider method may have been renamed.");

        var request = new ItemsProviderRequest(startIndex: 0, count: 20, cancellationToken: CancellationToken.None);
        await cut.InvokeAsync(async () =>
        {
            var valueTask = (ValueTask<ItemsProviderResult<EventListDto>>)loadEventsMethod.Invoke(cut.Instance, [request])!;
            await valueTask;
        });
    }

    [Test]
    public async Task DoesNotShowEmptyState_BeforeFirstEventsLoadCompletes()
    {
        // Arrange — result stays pending
        var pendingResult = new TaskCompletionSource<PaginatedResult<EventListDto>>();
        SetupPagedResult(pendingResult.Task);

        // Act
        var cut = _ctx.RenderMudComponent<EventList>();

        // Assert — empty state must not appear while load is pending
        await Assert.That(cut.Markup).DoesNotContain("No events found");

        // Cleanup to avoid dangling async work
        pendingResult.TrySetResult(CreateResult(1, 20, []));
    }

    [Test]
    public async Task ShowsNoEventsState_OnlyAfterInitialLoadCompletesWithEmptyResult()
    {
        // Arrange — start with pending result
        var pendingResult = new TaskCompletionSource<PaginatedResult<EventListDto>>();
        SetupPagedResult(pendingResult.Task);

        var cut = _ctx.RenderMudComponent<EventList>();

        // Pre-condition: while pending, empty state is not shown
        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup).DoesNotContain("No events found");
        });

        // Complete with empty result and trigger the Virtualize provider
        pendingResult.SetResult(CreateResult(1, 20, []));
        await InvokeLoadEventsAsync(cut);

        // Assert — empty state now visible
        await Assert.That(cut.Markup).Contains("No events found");
    }

    [Test]
    public async Task HidesNoEventsState_WhenResultsExist()
    {
        // Arrange — immediate result with one event
        var events = new List<EventListDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Blazor Summit", Description = "Event description" }
        };
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, events)));

        // Act
        var cut = _ctx.RenderMudComponent<EventList>();
        await InvokeLoadEventsAsync(cut);

        // Assert — event visible, empty state hidden
        await Assert.That(cut.Markup).Contains("Blazor Summit");
        await Assert.That(cut.Markup).DoesNotContain("No events found");
    }
}
