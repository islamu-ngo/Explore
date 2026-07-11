using Explore.Blazor.Client.Models;
using Microsoft.AspNetCore.Components;

namespace Explore.Blazor.Client.Pages.Events.Components;

public partial class EventCardSkeleton : ComponentBase
{
    [Parameter] public LayoutMode Layout { get; set; } = LayoutMode.DetailedList;

    private string CardCssClass
    {
        get
        {
            var css = $"event-card event-card--skeleton event-card--{Layout}";
            css += Layout == LayoutMode.CompactGrid ? " rounded-lg" : " rounded-xl";
            return css;
        }
    }
}
