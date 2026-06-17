// ABOUTME: Event card component supporting three layout modes (CompactGrid, DetailedList, SingleRow).
// ABOUTME: Extracted from EventList inline rendering for reusability and settings-driven customization.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Events.Components;

public partial class EventCard : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    /// <summary>The event data to display.</summary>
    [Parameter, EditorRequired] public EventListDto Event { get; set; } = null!;

    /// <summary>Which layout mode to render (CompactGrid, DetailedList, SingleRow).</summary>
    [Parameter] public LayoutMode Layout { get; set; } = LayoutMode.DetailedList;

    /// <summary>Whether this card is visually selected (e.g. detail drawer is open for it).</summary>
    [Parameter] public bool IsSelected { get; set; }

    /// <summary>Invoked when the card body is clicked.</summary>
    [Parameter] public EventCallback<EventListDto> OnClick { get; set; }

    /// <summary>Invoked when the Edit action menu item is clicked.</summary>
    [Parameter] public EventCallback<EventListDto> OnEditRequested { get; set; }

    /// <summary>Invoked when the Delete action menu item is clicked.</summary>
    [Parameter] public EventCallback<EventListDto> OnDeleteRequested { get; set; }

    /// <summary>Invoked when the share action is clicked.</summary>
    [Parameter] public EventCallback<EventListDto> OnShareRequested { get; set; }

    /// <summary>Card field visibility overrides. Key=setting key (e.g. "event_list.card.show_date"), Value=visible.</summary>
    [Parameter] public IReadOnlyDictionary<string, bool>? CardFieldVisibility { get; set; }

    /// <summary>Returns whether a card field should be visible (defaults to true when not configured).</summary>
    private bool IsFieldVisible(string key)
    {
        if (CardFieldVisibility == null) return true;
        return !CardFieldVisibility.TryGetValue(key, out var visible) || visible;
    }

    private string CardCssClass
    {
        get
        {
            var css = $"event-card event-card--{Layout}";
            css += Layout == LayoutMode.CompactGrid ? " rounded-lg" : " rounded-xl";
            if (IsSelected) css += " event-card--selected";
            return css;
        }
    }

    private bool _imageLoadFailed;
    private Guid? _lastRenderedEventId;

    private bool HasActualImage => !string.IsNullOrEmpty(Event.FeaturedImageUri) && !_imageLoadFailed;

    private string ImageSource
    {
        get
        {
            if (!string.IsNullOrEmpty(Event.FeaturedImageUri))
                return Event.FeaturedImageUri;

            return GetFallbackImageSource();
        }
    }

    private string DisplayImageSource => _imageLoadFailed ? GetFallbackImageSource() : ImageSource;

    private string ImageFitStyle => HasActualImage
        ? "object-fit: contain; object-position: left top;"
        : "object-fit: cover !important; object-position: center !important;";

    private string GetFallbackImageSource()
    {
        var color = GetEventColor();
        return ImageHelper.GetEventImageUrl(null, Event.Title, color, width: 300, height: 400);
    }

    private string GetEventColor()
    {
        var color = EventColorHelper.GetColorByTypeId(Event.EventTypeId);
        if (color == EventColorHelper.DefaultColor)
            color = EventColorHelper.GetColorByHash(Event.Title);
        return color;
    }

    protected override void OnParametersSet()
    {
        if (_lastRenderedEventId != Event.Id)
        {
            _lastRenderedEventId = Event.Id;
            _imageLoadFailed = false;
        }
    }

    private void HandleImageError() => _imageLoadFailed = true;

    private string EventTypeName =>
        !string.IsNullOrEmpty(Event.EventTypeFullName) ? Event.EventTypeFullName : "Event";

    private string LocationText
    {
        get
        {
            if (Event.EventFormatId == 2) return "Online";
            if (!string.IsNullOrEmpty(Event.EventFormatFullName)) return Event.EventFormatFullName;
            return "Location TBD";
        }
    }

    private string TruncatedDescription => StringHelper.TruncateDescription(Event.Description);

    private bool HasManagementMenu => Event.HasManagementLinks();
    private bool HasCardActions => HasManagementMenu || OnShareRequested.HasDelegate;
    private bool CanEdit => Event.HasHalLink("edit");
    private bool CanDelete => Event.HasHalLink("delete");
    private string ShareButtonLabel => $"Share event: {Event.Title}";

    // ── Icon Mapping Helpers ──

    private string VisibilityIcon => Event.VisibilityTypeId switch
    {
        1 => Icons.Material.Outlined.Public,        // Public
        2 => Icons.Material.Outlined.Lock,           // Private
        3 => Icons.Material.Outlined.VisibilityOff,  // Unlisted
        4 => Icons.Material.Outlined.Group,           // Members Only
        _ => Icons.Material.Outlined.Public
    };

    private string AudienceGenderIcon => Event.AudienceGenderId switch
    {
        1 => Icons.Material.Outlined.Male,            // Man
        2 => Icons.Material.Outlined.Female,          // Woman
        3 => Icons.Material.Outlined.PeopleAlt,       // Both Segregated
        4 => Icons.Material.Outlined.Groups,           // Both Free Mixing
        _ => Icons.Material.Outlined.Groups
    };

    private string FormatIcon => Event.EventFormatId switch
    {
        1 => Icons.Material.Outlined.Place,           // Local (In-Person)
        2 => Icons.Material.Outlined.Videocam,        // Digital (Online)
        3 => Icons.Material.Outlined.SyncAlt,          // Hybrid
        _ => Icons.Material.Outlined.Place
    };

    private string FormatTooltip => Event.EventFormatId switch
    {
        1 => Event.EventFormatFullName ?? "In-Person",
        2 => "Online",
        3 => "Hybrid",
        _ => LocationText
    };

    /// <summary>Count of visible optional fields for CompactGrid progressive disclosure.</summary>
    private int CompactExtraFieldCount
    {
        get
        {
            var count = 0;
            if (IsFieldVisible("event_list.card.show_location")) count++;
            if (IsFieldVisible("event_list.card.show_organizer")) count++;
            if (IsFieldVisible("event_list.card.show_price")) count++;
            if (IsFieldVisible("event_list.card.show_status")) count++;
            if (IsFieldVisible("event_list.card.show_description")) count++;
            return count;
        }
    }

    private static string? GetActorProfileUrl(Guid? actorId, int? actorTypeId)
    {
        if (actorId == null || actorTypeId == null) return null;
        return actorTypeId.Value switch
        {
            2 => $"/organization/profile/{actorId.Value}",
            4 => $"/group/profile/{actorId.Value}",
            _ => null
        };
    }

    private Task HandleClick() => OnClick.InvokeAsync(Event);

    private Task HandleEdit() => OnEditRequested.InvokeAsync(Event);

    private Task HandleDelete() => OnDeleteRequested.InvokeAsync(Event);

    private Task ShareEventAsync() => OnShareRequested.InvokeAsync(Event);

    private void NavigateToActorProfile(Guid? actorId, int? actorTypeId)
    {
        var url = GetActorProfileUrl(actorId, actorTypeId);
        if (url != null)
            Navigation.NavigateTo(url);
    }
}
