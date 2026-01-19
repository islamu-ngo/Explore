using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Admin;

public partial class Madhabs
{
    [Inject] protected IMadhabService MadhabService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;

    private ICollection<MadhabListDto> _items = new List<MadhabListDto>();
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
            _items = await MadhabService.GetMadhabsAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load madhabs: {ex.Message}", Severity.Error);
            _items = new List<MadhabListDto>();
        }
        finally
        {
            _isLoading = false;
        }
    }
}
