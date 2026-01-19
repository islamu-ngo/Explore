using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Admin;

public partial class EventTypes
{
    [Inject] protected IEventTypeService EventTypeService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;

    private ICollection<EventTypeListDto> _items = new List<EventTypeListDto>();
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        _isLoading = true;
        try
        {
            _items = await EventTypeService.GetEventTypesAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load event types: {ex.Message}", Severity.Error);
            _items = new List<EventTypeListDto>();
        }
        finally
        {
            _isLoading = false;
        }
    }
}
