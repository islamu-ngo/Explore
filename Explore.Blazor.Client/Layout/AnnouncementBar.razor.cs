// ABOUTME: Dismissable announcement bar component for development/informational messages.
// ABOUTME: Notifies parent layout via EventCallback when visibility changes (no JS required).

using Microsoft.AspNetCore.Components;

namespace Explore.Blazor.Client.Layout;

public partial class AnnouncementBar
{
    private bool _isVisible = true;

    /// <summary>
    /// Fires when the announcement bar is shown or dismissed.
    /// The bool parameter is true when visible, false when hidden.
    /// MainLayout uses this to update the theme's AppbarHeight so
    /// --mud-appbar-height on :root reflects the full header height.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnVisibilityChanged { get; set; }

    private async Task CloseBar()
    {
        _isVisible = false;
        await OnVisibilityChanged.InvokeAsync(false);
    }
}
