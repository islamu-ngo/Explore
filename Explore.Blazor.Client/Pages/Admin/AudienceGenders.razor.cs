using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Admin;

public partial class AudienceGenders
{
    [Inject] protected IAudienceGenderService AudienceGenderService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;

    private ICollection<AudienceGenderListDto> _items = new List<AudienceGenderListDto>();
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
            _items = await AudienceGenderService.GetAudienceGendersAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load audience genders: {ex.Message}", Severity.Error);
            _items = new List<AudienceGenderListDto>();
        }
        finally
        {
            _isLoading = false;
        }
    }
}
