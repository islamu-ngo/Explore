// ABOUTME: Luma-inspired group profile page with banner, avatar, events timeline.
// ABOUTME: Loads group details and public events split into upcoming vs past.

using Blazouter.Services;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Groups;

public partial class GroupProfile
{
    [Inject] protected IGroupService GroupService { get; set; } = null!;
    [Inject] protected IEventService EventService { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected RouterStateService RouterState { get; set; } = null!;
    [Inject] protected ILogger<GroupProfile> Logger { get; set; } = null!;

    private Guid Id { get; set; }

    private GroupAdminDetailsModel? _group;
    private bool _isLoading = true;
    private bool _isLoadingEvents;
    private List<EventListDto> _upcomingEvents = new();
    private List<EventListDto> _pastEvents = new();
    private List<KeyValuePair<DateTime, List<EventListDto>>> _pastEventsByDate = new();
    private GroupBrandingSettings _branding = new();
    private string _bannerStyle = GroupBrandingMetadataHelper.BuildBannerStyle(new GroupBrandingSettings(), "#334155");

    private string? _errorMessage;

    [PersistentState]
    public GroupProfileState? PersistedState { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var idStr = RouterState.GetParam("id");
        if (Guid.TryParse(idStr, out var id))
        {
            Id = id;
        }

        if (TryRestoreState())
        {
            return;
        }

        _isLoading = true;
        _errorMessage = null;

        try
        {
            _group = await GroupService.GetGroupDetailsAsync(Id);
            _branding = GroupBrandingMetadataHelper.FromColumns(
                _group?.ActorBannerColor,
                _group?.ActorBannerPictureUri, _group?.ActorBackgroundEffect);
            _bannerStyle = GroupBrandingMetadataHelper.BuildBannerStyle(_branding, "#334155");

            Logger.LogDebug("Loaded group {GroupId}", Id);
            PersistState();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load group data: {ex.Message}";
            Logger.LogError(ex, "Error loading group {GroupId}", Id);
        }
        finally
        {
            _isLoading = false;
        }

        if (_group?.ActorId.HasValue == true)
        {
            _ = InvokeAsync(LoadEventsAsync);
        }
    }

    private async Task LoadEventsAsync()
    {
        _isLoadingEvents = true;
        StateHasChanged();

        try
        {
            var allEvents = await EventService.GetPublicEventsByActorAsync(_group!.ActorId!.Value);

            _upcomingEvents = allEvents
                .Where(e => e.IsPast != true)
                .OrderBy(e => e.FirstSessionDate)
                .ToList();

            _pastEvents = allEvents
                .Where(e => e.IsPast == true)
                .OrderByDescending(e => e.FirstSessionDate)
                .ToList();

            _pastEventsByDate = _pastEvents
                .GroupBy(e => e.FirstSessionDate?.Date ?? DateTime.MinValue)
                .OrderByDescending(g => g.Key)
                .Select(g => new KeyValuePair<DateTime, List<EventListDto>>(g.Key, g.ToList()))
                .ToList();

            Logger.LogDebug("Loaded {Upcoming} upcoming and {Past} past events for group {GroupId}",
                _upcomingEvents.Count, _pastEvents.Count, Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading events for group {GroupId}", Id);
        }
        finally
        {
            _isLoadingEvents = false;
            StateHasChanged();
        }
    }

    private void NavigateToEvent(EventListDto evt)
    {
        if (evt.Id.HasValue)
        {
            Navigation.NavigateTo($"/events/{evt.Id}");
        }
    }

    private string GetGroupPlaceholder()
    {
        return ImageHelper.GetOrganizationPlaceholder(null, _group?.FullName ?? "GRP");
    }

    private static string GetEventImage(EventListDto evt)
    {
        var color = EventColorHelper.GetColorByTypeId(evt.EventTypeId);
        if (color == EventColorHelper.DefaultColor)
        {
            color = EventColorHelper.GetColorByHash(evt.Title);
        }

        return ImageHelper.GetEventImageUrl(evt.FeaturedImageUri, evt.Title, color);
    }

    private static string FormatEventDate(EventListDto evt)
    {
        if (evt.FirstSessionDate == null) return "TBD";

        var start = evt.FirstSessionDate.Value;
        if (evt.LastSessionDate != null && evt.LastSessionDate.Value.Date != start.Date)
        {
            return $"{start:MMM dd} — {evt.LastSessionDate.Value:MMM dd, yyyy}";
        }

        return start.ToString("MMM dd, yyyy");
    }

    private static string GetLocationText(EventListDto evt)
    {
        if (evt.EventFormatId == 2) return "Online";
        if (!string.IsNullOrEmpty(evt.EventFormatFullName)) return evt.EventFormatFullName;
        return "Location TBD";
    }

    private bool TryRestoreState()
    {
        if (PersistedState == null || PersistedState.GroupId != Id)
        {
            return false;
        }

        _group = PersistedState.Group;
        _branding = GroupBrandingMetadataHelper.FromColumns(
            _group?.ActorBannerColor,
            _group?.ActorBannerPictureUri, _group?.ActorBackgroundEffect);
        _bannerStyle = GroupBrandingMetadataHelper.BuildBannerStyle(_branding, "#334155");
        _isLoading = false;
        _errorMessage = null;

        if (_group?.ActorId.HasValue == true)
        {
            _ = InvokeAsync(LoadEventsAsync);
        }

        return true;
    }

    private void PersistState()
    {
        PersistedState = new GroupProfileState
        {
            GroupId = Id,
            Group = _group
        };
    }

    public sealed class GroupProfileState
    {
        public Guid GroupId { get; init; }
        public GroupAdminDetailsModel? Group { get; init; }
    }
}
