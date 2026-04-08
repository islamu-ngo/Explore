// ABOUTME: Code-behind for Notifications inbox page — manages notification list, scope filter, and unread toggle.
// ABOUTME: Loads notifications on init with pagination; supports scope tabs and unread-only filtering.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Notifications;
using Microsoft.AspNetCore.Components;

namespace Explore.Blazor.Client.Pages.Notifications;

public partial class Notifications
{
    private const int PageSize = 20;

    [Inject]
    private INotificationService NotificationService { get; set; } = null!;

    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    private bool _isLoading;
    private bool _isMarkingAllRead;
    private bool _hasMore;
    private bool _showUnreadOnly;
    private int _currentPage = 1;
    private int? _selectedScope;
    private readonly List<NotificationListDto> _notifications = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadNotificationsAsync();
    }

    private async Task LoadNotificationsAsync()
    {
        _isLoading = true;

        try
        {
            bool? isRead = _showUnreadOnly ? false : null;
            var result = await NotificationService.GetNotificationsAsync(_currentPage, PageSize, isRead, _selectedScope);
            _notifications.AddRange(result.Items);
            _hasMore = result.HasNextPage;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadMoreAsync()
    {
        _currentPage++;
        await LoadNotificationsAsync();
    }

    private async Task HandleScopeChanged(int? scope)
    {
        _selectedScope = scope;
        _currentPage = 1;
        _notifications.Clear();
        await LoadNotificationsAsync();
    }

    private async Task HandleToggleUnread()
    {
        _showUnreadOnly = !_showUnreadOnly;
        _currentPage = 1;
        _notifications.Clear();
        await LoadNotificationsAsync();
    }

    private async Task HandleMarkAllRead()
    {
        _isMarkingAllRead = true;

        try
        {
            await NotificationService.MarkAllAsReadAsync();
            _currentPage = 1;
            _notifications.Clear();
            await LoadNotificationsAsync();
        }
        finally
        {
            _isMarkingAllRead = false;
        }
    }

    private void HandleNotificationClick(NotificationListDto notification)
    {
        var url = GetEntityUrl(notification);
        if (!string.IsNullOrEmpty(url))
        {
            Nav.NavigateTo(url);
        }
    }

    private async Task HandleDeleteNotification(NotificationListDto notification)
    {
        if (notification.Id is null) return;

        var success = await NotificationService.DeleteAsync(notification.Id.Value);
        if (success)
        {
            _notifications.Remove(notification);
        }
    }

    private static string? GetEntityUrl(NotificationListDto notification)
    {
        if (string.IsNullOrEmpty(notification.EntityId) || notification.NotificationEntityTypeName is null)
            return null;

        return notification.NotificationEntityTypeName.ToLowerInvariant() switch
        {
            "event" => $"/events/{notification.EntityId}",
            "organization" => $"/organizations/{notification.EntityId}",
            "group" => $"/groups/{notification.EntityId}",
            "eventsession" => $"/events/{notification.EntityId}",
            _ => null
        };
    }
}
