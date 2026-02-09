using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Explore.Blazor.Client.Pages.Landing;

public partial class LandingPageForNonUsers
{
    [Inject] protected IJSRuntime JS { get; set; } = null!;
    [Inject] protected ILandingPageService LandingPageService { get; set; } = null!;
    [Inject] protected ILogger<LandingPageForNonUsers> Logger { get; set; } = null!;

    private ICollection<EventListDto> _events = new List<EventListDto>();
    private int _membersCount = 1200;
    private int _eventsCount = 0;
    private bool _isLoading = true;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
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
        => await JS.InvokeVoidAsync("eval", "document.getElementById('register').scrollIntoView({behavior:'smooth',block:'start'});");
}
