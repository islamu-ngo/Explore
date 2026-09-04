using Blazouter.Services;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Admin;

public partial class AdminListDetails
{
    [Inject] protected IOrganizationService OrganizationService { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected RouterStateService RouterState { get; set; } = null!;
    [Inject] protected ILogger<AdminListDetails> Logger { get; set; } = null!;

    private Guid OrganizationId { get; set; }

    private OrganizationDto? Organization;

    private bool _isLoading = true;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        var orgIdStr = RouterState.GetParam("organizationId");
        if (Guid.TryParse(orgIdStr, out var id))
        {
            OrganizationId = id;
            await LoadOrganizationDetails();
        }
        else
        {
            _errorMessage = "Invalid Organization ID provided.";
            _isLoading = false;
        }
    }

    private async Task LoadOrganizationDetails()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            Organization = await OrganizationService.GetOrganizationByIdAsync(OrganizationId);

            if (Organization == null)
            {
                _errorMessage = "Organization not found";
                Snackbar.Add(_errorMessage, Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading organization: {ex.Message}";
            Logger.LogError(ex, "Failed to load organization {OrganizationId}", OrganizationId);
            Snackbar.Add(_errorMessage, Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void NavigateBack()
    {
        NavigationManager.NavigateTo("/admin");
    }

    private static string GetStatusName(int statusTypeId) => statusTypeId switch
    {
        1 => "Pending",
        2 => "Approved",
        3 => "Rejected",
        _ => "Unknown"
    };

    private Color StatusColor(int statusTypeId) => statusTypeId switch
    {
        1 => Color.Info,
        2 => Color.Success,
        3 => Color.Error,
        _ => Color.Default
    };
}
