// ABOUTME: Code-behind for NotificationPanel — receives notification list and event callbacks from parent.
// ABOUTME: Pure presentational component; data loading is managed by NotificationBell.

using Explore.Blazor.Client.Clients;
using Microsoft.AspNetCore.Components;

namespace Explore.Blazor.Client.Layout;

public partial class NotificationPanel
{
    [Parameter, EditorRequired]
    public List<NotificationListDto> Notifications { get; set; } = [];

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public bool HasMore { get; set; }

    [Parameter]
    public EventCallback OnLoadMore { get; set; }

    [Parameter]
    public EventCallback<NotificationListDto> OnNotificationClick { get; set; }

    [Parameter]
    public EventCallback<NotificationListDto> OnDeleteNotification { get; set; }

    [Parameter]
    public int? SelectedScope { get; set; }

    [Parameter]
    public EventCallback<int?> OnScopeChanged { get; set; }

    [Parameter]
    public EventCallback OnViewAll { get; set; }
}
