// ABOUTME: Loads featured events for authenticated users on the landing page.
// ABOUTME: Persists initial SSR payload to prevent hydration loading flashes.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Explore.Blazor.Client.Pages.Landing;

public partial class LandingPageForUsers
{
    [Inject] protected IJSRuntime JS { get; set; } = null!;
    [Inject] protected ILandingPageService LandingPageService { get; set; } = null!;
    [Inject] protected ILogger<LandingPageForUsers> Logger { get; set; } = null!;

    private ICollection<EventListDto> _events = new List<EventListDto>();
    private bool _isLoading = true;

    [PersistentState]
    public LandingPageForUsersState? PersistedState { get; set; }

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
        try
        {
            _events = await LandingPageService.GetFeaturedEventsAsync(9);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading landing page events");
        }
        finally
        {
            _isLoading = false;
        }
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

    private string TruncateText(string? text, int maxLength)
    {
        return StringHelper.TruncateText(text, maxLength);
    }

    private bool TryRestoreState()
    {
        if (PersistedState == null)
        {
            return false;
        }

        _events = PersistedState.Events;
        _isLoading = false;
        return true;
    }

    private void PersistState()
    {
        PersistedState = new LandingPageForUsersState
        {
            Events = _events.ToList()
        };
    }

    public sealed class LandingPageForUsersState
    {
        public List<EventListDto> Events { get; init; } = new();
    }
}
