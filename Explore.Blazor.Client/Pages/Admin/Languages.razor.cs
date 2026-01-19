using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Admin;

public partial class Languages
{
    [Inject] protected ILanguageService LanguageService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;

    private ICollection<LanguageListDto> _items = new List<LanguageListDto>();
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
            _items = await LanguageService.GetLanguagesAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load languages: {ex.Message}", Severity.Error);
            _items = new List<LanguageListDto>();
        }
        finally
        {
            _isLoading = false;
        }
    }
}
