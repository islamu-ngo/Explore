// ABOUTME: Luma-inspired organization profile page with banner, avatar, events timeline.
// ABOUTME: Loads org details, reviews, and public events split into upcoming vs past.

using Blazouter.Services;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Events;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Organizations;

public partial class OrganizationProfile
{
    [Inject] protected IOrganizationService OrganizationService { get; set; } = null!;
    [Inject] protected IOrganizationReviewService OrganizationReviewService { get; set; } = null!;
    [Inject] protected IEventService EventService { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected RouterStateService RouterState { get; set; } = null!;
    [Inject] protected ILogger<OrganizationProfile> Logger { get; set; } = null!;

    private Guid Id { get; set; }

    private OrganizationDto? _organization;
    private bool _isLoading = true;
    private bool _isLoadingEvents;
    private List<OrganizationReviewDto> _reviews = new();
    private List<EventListDto> _upcomingEvents = new();
    private List<EventListDto> _pastEvents = new();
    private List<KeyValuePair<DateTime, List<EventListDto>>> _pastEventsByDate = new();
    private EventPreviewWorkspace? _eventPreviewWorkspace;
    private AppearanceSettings _appearance = new();
    private string _bannerStyle = AppearanceStyleBuilder.BuildBannerStyle(new AppearanceSettings(), "#1f6feb");

    private string? _errorMessage;

    private IReadOnlyList<EventListDto> ProfileEvents => _upcomingEvents.Concat(_pastEvents).ToList();

    [PersistentState]
    public OrganizationProfileState? PersistedState { get; set; }

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
            _organization = await OrganizationService.GetOrganizationByIdAsync(Id);

            if (_organization is not null)
            {
                Id = _organization.Id.GetValueOrDefault(Id);
                _reviews = (await OrganizationReviewService.GetReviewsByOrganizationId(Id)).ToList();
            }
            else
            {
                _reviews = new List<OrganizationReviewDto>();
            }

            _appearance = new AppearanceSettings
            {
                BackgroundColor = _organization?.ActorBackgroundColor ?? string.Empty,
                ImageUri = _organization?.ActorBannerPictureUri ?? string.Empty,
                BackgroundEffect = _organization?.ActorBackgroundEffect ?? "None"
            };
            _bannerStyle = AppearanceStyleBuilder.BuildBannerStyle(_appearance, "#1f6feb");

            Logger.LogDebug("Loaded organization {OrganizationId} with {ReviewCount} reviews", Id, _reviews.Count);

            PersistState();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load organization data: {ex.Message}";
            Logger.LogError(ex, "Error loading organization {OrganizationId}", Id);
        }
        finally
        {
            _isLoading = false;
        }

        // Load events in parallel after the main data is ready
        if (_organization?.ActorId.HasValue == true)
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
            var allEvents = await EventService.GetPublicEventsByActorAsync(_organization!.ActorId!.Value);

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

            Logger.LogDebug("Loaded {Upcoming} upcoming and {Past} past events for actor {ActorId}",
                _upcomingEvents.Count, _pastEvents.Count, _organization.ActorId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading events for organization {OrganizationId}", Id);
        }
        finally
        {
            _isLoadingEvents = false;
            StateHasChanged();
        }
    }

    private void ShowAllReviews()
    {
        Navigation.NavigateTo($"/organization/reviews/{Id}");
    }

    private Task HandleEventSelected(EventListDto evt) =>
        _eventPreviewWorkspace?.SelectEventAsync(evt) ?? Task.CompletedTask;

    private Task HandleEventShare(EventListDto evt) =>
        _eventPreviewWorkspace?.ShareEventAsync(evt) ?? Task.CompletedTask;

    private string GetOrganizationPlaceholder()
    {
        if (_organization == null)
            return ImageHelper.GetOrganizationPlaceholder(null, "ORG");

        return ImageHelper.GetOrganizationPlaceholder(null, _organization.FullName);
    }

    private string? GetProfileImageUri()
    {
        if (!string.IsNullOrWhiteSpace(_organization?.ActorProfilePictureUri))
        {
            return _organization.ActorProfilePictureUri;
        }

        return null;
    }

    private int GetAverageRating()
    {
        if (!_reviews.Any(r => r.Rating.HasValue)) return 0;
        return (int)Math.Round(_reviews.Where(r => r.Rating.HasValue).Average(r => r.Rating!.Value));
    }

    private bool TryRestoreState()
    {
        if (PersistedState == null || PersistedState.OrganizationId != Id)
        {
            return false;
        }

        _organization = PersistedState.Organization;
        _reviews = PersistedState.Reviews;
        _appearance = new AppearanceSettings
        {
            BackgroundColor = _organization?.ActorBackgroundColor ?? string.Empty,
            ImageUri = _organization?.ActorBannerPictureUri ?? string.Empty,
            BackgroundEffect = _organization?.ActorBackgroundEffect ?? "None"
        };
        _bannerStyle = AppearanceStyleBuilder.BuildBannerStyle(_appearance, "#1f6feb");
        _isLoading = false;
        _errorMessage = null;

        // Load events asynchronously even when restoring
        if (_organization?.ActorId.HasValue == true)
        {
            _ = InvokeAsync(LoadEventsAsync);
        }

        return true;
    }

    private void PersistState()
    {
        PersistedState = new OrganizationProfileState
        {
            OrganizationId = Id,
            Organization = _organization,
            Reviews = _reviews.ToList()
        };
    }

    public sealed class OrganizationProfileState
    {
        public Guid OrganizationId { get; init; }
        public OrganizationDto? Organization { get; init; }
        public List<OrganizationReviewDto> Reviews { get; init; } = new();
    }
}
