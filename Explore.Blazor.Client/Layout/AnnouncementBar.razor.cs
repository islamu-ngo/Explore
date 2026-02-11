// ABOUTME: Dismissable announcement bar component for development/informational messages.
// ABOUTME: Uses localStorage (standard browser API) for persistence, no custom JS required.

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Explore.Blazor.Client.Layout;

public partial class AnnouncementBar
{
    private bool _isVisible = true;

    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadDismissalState();
        }
    }

    private async Task LoadDismissalState()
    {
        try
        {
            var dismissed = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "announcementDismissed");
            if (dismissed == "true")
            {
                _isVisible = false;
                StateHasChanged();
            }
        }
        catch (JSException)
        {
            // Fail gracefully if localStorage is not available (e.g., during pre-rendering).
        }
    }

    private async Task CloseBar()
    {
        _isVisible = false;
        try
        {
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "announcementDismissed", "true");
        }
        catch (JSException)
        {
            // Fail gracefully.
        }
    }
}
