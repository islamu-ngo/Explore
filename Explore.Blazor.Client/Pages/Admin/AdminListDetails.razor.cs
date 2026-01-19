using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Blazouter.Services;

namespace Explore.Blazor.Client.Pages.Admin;

public partial class AdminListDetails
{
    [Inject] protected IAdminService AdminService { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected RouterStateService RouterState { get; set; } = null!;
    [Inject] protected ILogger<AdminListDetails> Logger { get; set; } = null!;

    private Guid OrganizationId { get; set; }

    private OrganizationDto? organization;
    private bool _isLoading = true;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        // Get route parameter from Blazouter
        var orgIdStr = RouterState.GetParam("organizationId");
        if (Guid.TryParse(orgIdStr, out var id))
        {
            OrganizationId = id;
        }
        await LoadOrganizationDetails();
    }

    private async Task LoadOrganizationDetails()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            Logger.LogDebug("Loading organization {OrganizationId}", OrganizationId);
            organization = await AdminService.GetOrganizationDetailsAsync(OrganizationId);

            if (organization == null)
            {
                _errorMessage = "Organization not found";
                Snackbar.Add(_errorMessage, Severity.Warning);
            }
            else
            {
                Logger.LogDebug("Loaded organization: {OrganizationName}", organization.FullName);
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

    private static string GetStatusName(int statusTypeId)
    {
        return statusTypeId switch
        {
            1 => "Pending",
            2 => "Approved",
            3 => "Rejected",
            _ => "Unknown"
        };
    }

    private Color StatusColor(int statusTypeId) => statusTypeId switch
    {
        1 => Color.Info,
        2 => Color.Success,
        3 => Color.Error,
        _ => Color.Default
    };
}
