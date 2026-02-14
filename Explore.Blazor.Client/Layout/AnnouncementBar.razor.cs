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
            // Measure height and set CSS variable for sticky headers
            await UpdateHeightVariable();
        }
    }

    private async Task CloseBar()
    {
        _isVisible = false;
        await UpdateHeightVariable();
    }

    private async Task UpdateHeightVariable()
    {
        try
        {
            // If visible, measure. If not, set to 0.
            // We use a simple JS script here or rely on the fact that if it's removed from DOM, height is 0.
            // But we need to update the variable.
            var height = _isVisible ? await JSRuntime.InvokeAsync<double>("eval", "document.querySelector('.announcement-bar')?.offsetHeight || 0") : 0;
            await JSRuntime.InvokeVoidAsync("document.documentElement.style.setProperty", "--announcement-bar-height", $"{height}px");
        }
        catch
        {
            // Ignore JS errors
        }
    }
}
