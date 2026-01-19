using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Admin;

public partial class AudienceAges
{
    [Inject] protected IAudienceAgeService AudienceAgeService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;

    private ICollection<AudienceAgeListDto> _items = new List<AudienceAgeListDto>();
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
            _items = await AudienceAgeService.GetAudienceAgesAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load audience ages: {ex.Message}", Severity.Error);
            _items = new List<AudienceAgeListDto>();
        }
        finally
        {
            _isLoading = false;
        }
    }
}
