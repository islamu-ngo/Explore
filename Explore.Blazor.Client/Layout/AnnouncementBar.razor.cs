using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Explore.Blazor.Client.Layout;

public partial class AnnouncementBar
{
    private bool _isVisible = true;
    private ElementReference _barRef;

    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadDismissalState();
        }

        await UpdateAnnouncementHeight();
    }

    private async Task LoadDismissalState()
    {
        try
        {
            var dismissed = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "announcementDismissed");
            if (dismissed == "true")
            {
                _isVisible = false;
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
        await UpdateAnnouncementHeight();
        try
        {
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "announcementDismissed", "true");
        }
        catch (JSException)
        {
            // Fail gracefully.
        }
    }

    private async Task UpdateAnnouncementHeight()
    {
        try
        {
            if (_isVisible)
            {
                await JSRuntime.InvokeVoidAsync("ExploreLayout.setAnnouncementBarHeight", _barRef);
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("ExploreLayout.clearAnnouncementBarHeight");
            }
        }
        catch (JSException)
        {
            // Fail gracefully if JS interop isn't available.
        }
    }
}
