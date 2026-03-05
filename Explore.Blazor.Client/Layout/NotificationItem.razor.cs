// ABOUTME: Code-behind for NotificationItem — handles display logic for type icons, scope colors, relative time.
// ABOUTME: Maps NotificationTypeName → Material icon and NotificationScopeName → MudBlazor Color.

using Explore.Blazor.Client.Clients;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Layout;

public partial class NotificationItem
{
    [Parameter, EditorRequired]
    public NotificationListDto Notification { get; set; } = null!;

    [Parameter]
    public EventCallback<NotificationListDto> OnClick { get; set; }

    [Parameter]
    public EventCallback<NotificationListDto> OnDelete { get; set; }

    private bool IsUnread => Notification.IsRead != true;

    private string GetTypeIcon()
    {
        return Notification.NotificationTypeName?.ToLowerInvariant() switch
        {
            "info" => Icons.Material.Outlined.Info,
            "warning" => Icons.Material.Outlined.Warning,
            "success" => Icons.Material.Outlined.CheckCircle,
            "error" => Icons.Material.Outlined.Error,
            "eventupdate" => Icons.Material.Outlined.Event,
            "registration" => Icons.Material.Outlined.HowToReg,
            "announcement" => Icons.Material.Outlined.Campaign,
            "reminder" => Icons.Material.Outlined.Alarm,
            _ => Icons.Material.Outlined.Notifications
        };
    }

    private Color GetScopeColor()
    {
        return Notification.NotificationScopeName?.ToLowerInvariant() switch
        {
            "user" or "personal" => Color.Info,
            "organization" => Color.Warning,
            "group" => Color.Secondary,
            "system" => Color.Default,
            _ => Color.Default
        };
    }

    private static string TruncateBody(string body, int maxLength = 120)
    {
        if (body.Length <= maxLength) return body;
        return string.Concat(body.AsSpan(0, maxLength), "…");
    }

    private static string FormatRelativeTime(DateTimeOffset? dateTime)
    {
        if (dateTime is null) return "";

        var diff = DateTimeOffset.UtcNow - dateTime.Value;

        return diff.TotalMinutes switch
        {
            < 1 => "just now",
            < 60 => $"{(int)diff.TotalMinutes}m ago",
            < 1440 => $"{(int)diff.TotalHours}h ago",
            < 10080 => $"{(int)diff.TotalDays}d ago",
            _ => dateTime.Value.ToString("MMM d")
        };
    }

    private async Task HandleDelete()
    {
        await OnDelete.InvokeAsync(Notification);
    }
}
