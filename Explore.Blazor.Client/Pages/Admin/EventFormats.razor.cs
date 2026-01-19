using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Admin;

public partial class EventFormats
{
    [Inject] protected IEventFormatService EventFormatService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;

    private ICollection<EventFormatListDto> _items = new List<EventFormatListDto>();
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
            _items = await EventFormatService.GetEventFormatsAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load event formats: {ex.Message}", Severity.Error);
            _items = new List<EventFormatListDto>();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private string GetFormatIcon(string? masterCode)
    {
        return masterCode?.ToUpperInvariant() switch
        {
            "LOCAL" or "IN_PERSON" or "INPERSON" => Icons.Material.Filled.Place,
            "DIGITAL" or "ONLINE" or "VIRTUAL" => Icons.Material.Filled.Videocam,
            "HYBRID" => Icons.Material.Filled.Sync,
            _ => Icons.Material.Filled.Event
        };
    }
}
