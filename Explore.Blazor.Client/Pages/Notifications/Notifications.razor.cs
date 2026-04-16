// ABOUTME: Code-behind for Notifications inbox page — manages notification list, scope/reason filters, and archive/snooze toggles.
// ABOUTME: Loads notifications on init with pagination; supports scope tabs, reason filter, unread-only, show-archived, and show-snoozed.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Notifications;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Notifications;

public partial class Notifications
{
    private const int PageSize = 20;

    [Inject]
    private INotificationService NotificationService { get; set; } = null!;

    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private bool _isLoading;
    private bool _isMarkingAllRead;
    private bool _hasMore;
    private bool _showUnreadOnly;
    private bool _showArchived;
    private bool _showSnoozed;
    private int _currentPage = 1;
    private int? _selectedScope;
    private int? _selectedReasonId;
    private readonly List<NotificationListDto> _notifications = [];

    private static readonly List<ReasonOption> ReasonOptions =
    [
        new(null, "All reasons"),
        new(2, "Mentions"),
        new(3, "Assignments"),
        new(4, "Subscriptions"),
        new(1, "Direct"),
        new(5, "Membership"),
        new(6, "System"),
    ];

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
            bool? isArchived = _showArchived ? true : null;
            bool? isSnoozed = _showSnoozed ? true : null;
            var result = await NotificationService.GetNotificationsAsync(
                _currentPage, PageSize, isRead, _selectedScope,
                _selectedReasonId, isArchived, isSnoozed);
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
        await ResetAndReloadAsync();
    }

    private async Task HandleReasonChanged(int? reasonId)
    {
        _selectedReasonId = reasonId;
        await ResetAndReloadAsync();
    }

    private async Task HandleToggleUnread()
    {
        _showUnreadOnly = !_showUnreadOnly;
        await ResetAndReloadAsync();
    }

    private async Task HandleToggleArchived()
    {
        _showArchived = !_showArchived;
        await ResetAndReloadAsync();
    }

    private async Task HandleToggleSnoozed()
    {
        _showSnoozed = !_showSnoozed;
        await ResetAndReloadAsync();
    }

    private async Task HandleMarkAllRead()
    {
        _isMarkingAllRead = true;

        try
        {
            await NotificationService.MarkAllAsReadAsync();
            await ResetAndReloadAsync();
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

    private async Task HandleArchiveNotification(NotificationListDto notification)
    {
        if (notification.Id is null) return;

        var archive = notification.IsArchived != true;
        var success = await NotificationService.ArchiveAsync(notification.Id.Value, archive);
        if (success)
        {
            _notifications.Remove(notification);
            Snackbar.Add(archive ? "Notification archived" : "Notification unarchived", Severity.Normal);
        }
    }

    private async Task HandleSnoozeNotification(NotificationListDto notification)
    {
        if (notification.Id is null) return;

        var snoozedUntil = DateTimeOffset.UtcNow.AddHours(3);
        var success = await NotificationService.SnoozeAsync(notification.Id.Value, snoozedUntil);
        if (success)
        {
            _notifications.Remove(notification);
            Snackbar.Add("Notification snoozed for 3 hours", Severity.Normal);
        }
    }

    private async Task ResetAndReloadAsync()
    {
        _currentPage = 1;
        _notifications.Clear();
        await LoadNotificationsAsync();
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

    private sealed record ReasonOption(int? Id, string Label);
}
