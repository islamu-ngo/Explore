// ABOUTME: Loads public landing metrics and featured events for anonymous visitors.
// ABOUTME: Persists prerendered payload to avoid hydration loading flicker.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Pages.Landing;

public partial class LandingPageForNonUsers
{
    [Inject] protected IBrowserActionInterop BrowserActionInterop { get; set; } = null!;
    [Inject] protected ILandingPageService LandingPageService { get; set; } = null!;
    [Inject] protected ILogger<LandingPageForNonUsers> Logger { get; set; } = null!;

    private ICollection<EventListDto> _events = new List<EventListDto>();
    private int _membersCount = 1200;
    private int _eventsCount = 0;
    private bool _isLoading = true;
    private string? _errorMessage;

    [PersistentState]
    public LandingPageForNonUsersState? PersistedState { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (TryRestoreState())
        {
            return;
        }

        await LoadDataAsync();
        PersistState();
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            Logger.LogDebug("Loading landing page data from API");

            // Load events, members count, and events count in parallel
            var eventsTask = LandingPageService.GetFeaturedEventsAsync(12);
            var membersTask = LandingPageService.GetTotalMembersCountAsync();
            var eventsCountTask = LandingPageService.GetUpcomingEventsCountAsync();

            await Task.WhenAll(eventsTask, membersTask, eventsCountTask);

            _events = await eventsTask;
            _membersCount = await membersTask;
            _eventsCount = await eventsCountTask;

            Logger.LogDebug("Loaded {EventCount} events, {MembersCount} members, {UpcomingCount} upcoming events", _events.Count, _membersCount, _eventsCount);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading landing page data: {ex.Message}";
            Logger.LogError(ex, "Error loading landing page data");
            // Keep default values on error
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static string TruncateText(string text, int maxLength)
    {
        return StringHelper.TruncateText(text, maxLength);
    }

    private IEnumerable<List<T>> Chunk<T>(IEnumerable<T> source, int size)
    {
        var bucket = new List<T>(size);
        foreach (var item in source)
        {
            bucket.Add(item);
            if (bucket.Count == size)
            {
                yield return bucket;
                bucket = new List<T>(size);
            }
        }
        if (bucket.Count > 0) yield return bucket;
    }

    private async Task ScrollToRegister()
        => await BrowserActionInterop.ScrollToElementByIdAsync("register");

    private bool TryRestoreState()
    {
        if (PersistedState == null)
        {
            return false;
        }

        _events = PersistedState.Events;
        _membersCount = PersistedState.MembersCount;
        _eventsCount = PersistedState.EventsCount;
        _isLoading = false;
        _errorMessage = null;
        return true;
    }

    private void PersistState()
    {
        PersistedState = new LandingPageForNonUsersState
        {
            Events = _events.ToList(),
            MembersCount = _membersCount,
            EventsCount = _eventsCount
        };
    }

    public sealed class LandingPageForNonUsersState
    {
        public List<EventListDto> Events { get; init; } = new();
        public int MembersCount { get; init; }
        public int EventsCount { get; init; }
    }
}
