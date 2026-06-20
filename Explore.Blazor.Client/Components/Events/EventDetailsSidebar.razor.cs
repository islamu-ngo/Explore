// ABOUTME: Code-behind for the shared EventList-compatible event details sidebar.
// ABOUTME: Provides formatting and HAL/tag/category helpers while parent pages own loading and mutation callbacks.

using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Pages.Events.Components;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Components.Events;

public partial class EventDetailsSidebar : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public EventListDto? SelectedEvent { get; set; }
    [Parameter] public EventDto? EventDetail { get; set; }
    [Parameter] public ICollection<EventSessionListDto>? EventSessions { get; set; }
    [Parameter] public bool IsLoadingDetail { get; set; }
    [Parameter] public bool DetailImageLoadFailed { get; set; }
    [Parameter] public bool IsDetailImageLoading { get; set; }
    [Parameter] public bool CanNavigatePrevious { get; set; }
    [Parameter] public bool CanNavigateNext { get; set; }
    [Parameter] public bool IsUserRegistered { get; set; }

    [Parameter] public bool ShowInlineRegistration { get; set; }
    [Parameter] public bool RegIsLoading { get; set; }
    [Parameter] public bool RegIsSubmitting { get; set; }
    [Parameter] public bool RegIsComplete { get; set; }
    [Parameter] public bool RegIsAlreadyRegistered { get; set; }
    [Parameter] public bool RegIsWaitlisted { get; set; }
    [Parameter] public bool RegShowConsentOption { get; set; }
    [Parameter] public bool RegShareEmail { get; set; }
    [Parameter] public string RegOrganizerName { get; set; } = string.Empty;
    [Parameter] public ICollection<EventSessionListDto>? RegAvailableSessions { get; set; }
    [Parameter] public IReadOnlySet<Guid> RegSelectedSessionIds { get; set; } = new HashSet<Guid>();
    [Parameter] public bool RegAllSessionsSelected { get; set; }

    [Parameter] public bool TagCategoryPopupVisible { get; set; }
    [Parameter] public TagCategoryMode TagCategoryMode { get; set; }
    [Parameter] public IReadOnlyCollection<Guid> TagCategoryInitialAppliedIds { get; set; } = Array.Empty<Guid>();

    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnNavigatePrevious { get; set; }
    [Parameter] public EventCallback OnNavigateNext { get; set; }
    [Parameter] public EventCallback OnOpenEventPage { get; set; }
    [Parameter] public EventCallback OnCopyEventLink { get; set; }
    [Parameter] public EventCallback OnDetailImageLoaded { get; set; }
    [Parameter] public EventCallback OnDetailImageError { get; set; }
    [Parameter] public EventCallback<EventListDto> OnEditRequested { get; set; }
    [Parameter] public EventCallback<EventListDto> OnDeleteRequested { get; set; }
    [Parameter] public EventCallback OnOpenInlineRegistration { get; set; }
    [Parameter] public EventCallback OnCloseInlineRegistration { get; set; }
    [Parameter] public EventCallback OnSubmitInlineRegistration { get; set; }
    [Parameter] public EventCallback OnShareSelectedEvent { get; set; }
    [Parameter] public EventCallback OnToggleRegAllSessions { get; set; }
    [Parameter] public EventCallback<Guid> OnToggleRegSession { get; set; }
    [Parameter] public EventCallback<bool> RegShareEmailChanged { get; set; }
    [Parameter] public EventCallback OnOpenTagManagement { get; set; }
    [Parameter] public EventCallback OnOpenCategoryManagement { get; set; }
    [Parameter] public EventCallback<bool> TagCategoryPopupVisibleChanged { get; set; }
    [Parameter] public EventCallback<IReadOnlyCollection<Guid>> OnTagCategorySaved { get; set; }

    private bool HasDetailActualImage => SelectedEvent is not null
        && HasUsableFeaturedImage(SelectedEvent)
        && !DetailImageLoadFailed;

    private bool ShouldShowDetailImageSkeleton => IsLoadingDetail || IsDetailImageLoading;

    private bool CanManageSelectedEvent => EventDetail?.HasHalLink("edit")
        ?? SelectedEvent?.HasHalLink("edit")
        ?? false;

    private async Task HandleCloseClickAsync()
    {
        if (ShowInlineRegistration)
        {
            await OnCloseInlineRegistration.InvokeAsync();
            return;
        }

        if (TagCategoryPopupVisible)
        {
            await TagCategoryPopupVisibleChanged.InvokeAsync(false);
            return;
        }

        await OnClose.InvokeAsync();
    }

    private string GetDetailImageSrc()
    {
        if (SelectedEvent is null)
        {
            return string.Empty;
        }

        if (DetailImageLoadFailed || string.IsNullOrWhiteSpace(SelectedEvent.FeaturedImageUri))
        {
            return GetFallbackEventImage(SelectedEvent);
        }

        return SelectedEvent.FeaturedImageUri;
    }

    private string GetDetailImageCssClass(bool hasDetailActualImage, bool showDetailImageSkeleton)
    {
        var cssClass = hasDetailActualImage
            ? "event-list__detail-image-actual"
            : "event-list__detail-image-fallback";

        return showDetailImageSkeleton
            ? $"{cssClass} event-list__detail-image--loading"
            : cssClass;
    }

    private string GetFallbackEventImage(EventListDto eventItem)
    {
        return ImageHelper.GetEventImageUrl(null, eventItem.Title, GetEventColorForEvent(eventItem), width: 300, height: 400);
    }

    private string GetEventColorForEvent(EventListDto eventItem)
    {
        var color = EventColorHelper.GetColorByTypeId(eventItem.EventTypeId);
        return color != EventColorHelper.DefaultColor ? color : EventColorHelper.GetColorByHash(eventItem.Title);
    }

    private static bool HasUsableFeaturedImage(EventListDto eventItem) =>
        !string.IsNullOrWhiteSpace(eventItem.FeaturedImageUri);

    private string GetTruncatedDescription(string? description)
    {
        return StringHelper.TruncateDescription(description);
    }

    private static string GetEventTypeName(EventListDto eventItem)
    {
        return !string.IsNullOrWhiteSpace(eventItem.EventTypeFullName)
            ? eventItem.EventTypeFullName
            : "Event";
    }

    private string GetLocationText(EventListDto eventItem)
    {
        if (eventItem.EventFormatId == 2) return "Online";
        if (!string.IsNullOrEmpty(eventItem.EventFormatFullName)) return eventItem.EventFormatFullName;
        return "Location TBD";
    }

    private string GetAudienceText(EventListDto eventItem)
    {
        var gender = !string.IsNullOrEmpty(eventItem.AudienceGenderFullName) ? eventItem.AudienceGenderFullName : "All genders";
        var age = !string.IsNullOrEmpty(eventItem.AudienceAgeFullName) ? eventItem.AudienceAgeFullName : "All ages";
        return $"{gender} · {age}";
    }

    private static Color GetStatusColor(string? masterCode) => masterCode switch
    {
        "PUBLISHED" => Color.Success,
        "DRAFT" => Color.Default,
        "CANCELLED" => Color.Error,
        "COMPLETED" => Color.Info,
        "POSTPONED" => Color.Warning,
        _ => Color.Default
    };

    private static string GetProgramCountText(EventDto detail, ICollection<EventSessionListDto>? sessions)
    {
        var count = detail.SessionCount ?? sessions?.Count ?? 0;
        return count switch
        {
            0 => "Program not published yet",
            1 => "1 program item",
            _ => $"{count} program items"
        };
    }

    private static string GetRegistrationPolicyText(EventDto detail)
    {
        if (!string.IsNullOrWhiteSpace(detail.RegistrationPolicyFullName))
        {
            return detail.RegistrationPolicyFullName;
        }

        return detail.IsRegistrationRequired == true
            ? "Registration required"
            : "Registration optional";
    }

    private static Color GetFormatColor(EventDto detail)
    {
        if (string.Equals(detail.EventFormatMasterCode, "DIGITAL", StringComparison.OrdinalIgnoreCase))
        {
            return Color.Info;
        }

        return string.Equals(detail.EventFormatMasterCode, "HYBRID", StringComparison.OrdinalIgnoreCase)
            ? Color.Tertiary
            : Color.Default;
    }

    private static string GetFormatIcon(EventDto detail)
    {
        if (string.Equals(detail.EventFormatMasterCode, "DIGITAL", StringComparison.OrdinalIgnoreCase))
        {
            return Icons.Material.Filled.Videocam;
        }

        return string.Equals(detail.EventFormatMasterCode, "HYBRID", StringComparison.OrdinalIgnoreCase)
            ? Icons.Material.Filled.Devices
            : Icons.Material.Filled.LocationOn;
    }

    private string GetSelectedEventCalendarUrl()
    {
        return SelectedEvent?.Id is Guid eventId && eventId != Guid.Empty
            ? $"/api/event/{eventId}/calendar"
            : "#";
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

    private void NavigateToActorProfile(Guid? actorId, int? actorTypeId)
    {
        var url = GetActorProfileUrl(actorId, actorTypeId);
        if (url != null)
        {
            Navigation.NavigateTo(url);
        }
    }

    private bool HasDetailTags()
    {
        return GetDetailTagItems().Any();
    }

    private IEnumerable<string> GetDetailTags()
    {
        return GetDetailTagItems().Select(item => item.Name);
    }

    private bool HasDetailCategories()
    {
        return GetDetailCategoryItems().Any();
    }

    private IEnumerable<string> GetDetailCategories()
    {
        return GetDetailCategoryItems().Select(item => item.Name);
    }

    private IEnumerable<TagCategoryManagerPopup.TagCategoryItem> GetDetailTagItems()
    {
        if (EventDetail?.Tags is { Count: > 0 })
        {
            foreach (var tag in EventDetail.Tags)
            {
                if (tag.Id.HasValue && !string.IsNullOrWhiteSpace(tag.FullName))
                {
                    yield return new TagCategoryManagerPopup.TagCategoryItem(tag.Id.Value, tag.FullName);
                }
            }

            yield break;
        }

        foreach (var item in GetTagCategoryItemsFromAdditionalProperties("tags"))
        {
            yield return item;
        }
    }

    private IEnumerable<TagCategoryManagerPopup.TagCategoryItem> GetDetailCategoryItems()
    {
        if (EventDetail?.Categories is { Count: > 0 })
        {
            foreach (var category in EventDetail.Categories)
            {
                if (category.Id.HasValue && !string.IsNullOrWhiteSpace(category.FullName))
                {
                    yield return new TagCategoryManagerPopup.TagCategoryItem(category.Id.Value, category.FullName);
                }
            }

            yield break;
        }

        foreach (var item in GetTagCategoryItemsFromAdditionalProperties("categories"))
        {
            yield return item;
        }
    }

    private IEnumerable<TagCategoryManagerPopup.TagCategoryItem> GetTagCategoryItemsFromAdditionalProperties(string propertyName)
    {
        if (EventDetail?.AdditionalProperties == null) yield break;
        if (!EventDetail.AdditionalProperties.TryGetValue(propertyName, out var val) || val is not JsonElement jsonArray) yield break;
        if (jsonArray.ValueKind != JsonValueKind.Array) yield break;

        foreach (var item in jsonArray.EnumerateArray())
        {
            var id = item.TryGetProperty("id", out var idProp) && idProp.TryGetGuid(out var g) ? g : (Guid?)null;
            var name = item.TryGetProperty("fullName", out var fn) ? fn.GetString()
                     : item.TryGetProperty("name", out var n) ? n.GetString()
                     : null;
            if (id.HasValue && !string.IsNullOrEmpty(name))
            {
                yield return new TagCategoryManagerPopup.TagCategoryItem(id.Value, name);
            }
        }
    }
}
