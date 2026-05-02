// ABOUTME: Code-behind for the OrganizationDetails page.
// ABOUTME: Uses HAL _links from API response to determine edit permissions instead of client-side role checks.

using Blazouter.Services;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Organizations;

public partial class OrganizationDetails
{
    [Inject] protected IOrganizationService OrganizationService { get; set; } = null!;
    [Inject] protected IEventService EventService { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
    [Inject] protected RouterStateService RouterState { get; set; } = null!;
    [Inject] protected ILogger<OrganizationDetails> Logger { get; set; } = null!;

    private Guid Id { get; set; }

    private OrganizationDto? organization;
    private bool isLoading = true;
    private bool isEditMode = false;
    private bool isSaving = false;
    private bool canEdit = false;
    private string? errorMessage;
    private string? successMessage;

    private ICollection<EventListDto> _orgEvents = new List<EventListDto>();

    private UpdateOrganizationDto editModel = new();
    private AppearanceSettings _appearance = new();
    private MudForm? form;
    private bool formValid;

    private IEnumerable<EventListDto> UpcomingEvents =>
        _orgEvents.Where(e => e.IsPast != true)
                  .OrderBy(e => e.FirstSessionDate ?? DateTimeOffset.MaxValue);

    private IEnumerable<EventListDto> PastEvents =>
        _orgEvents.Where(e => e.IsPast == true)
                  .OrderByDescending(e => e.FirstSessionDate ?? DateTimeOffset.MinValue);

    protected override async Task OnInitializedAsync()
    {
        // Get route parameter from Blazouter
        var idStr = RouterState.GetParam("id");
        if (Guid.TryParse(idStr, out var id))
        {
            Id = id;
        }

        try
        {
            await LoadOrganization();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading organization");
            errorMessage = "Failed to load organization details.";
            isLoading = false;
        }
    }

    private void InitializeEditModel()
    {
        if (organization != null)
        {
            editModel = new UpdateOrganizationDto
            {
                FullName = organization.FullName,
                Email = organization.Email,
                WebsiteUrl = organization.WebsiteUrl,
                Country = organization.Country,
                City = organization.City,
                Postcode = int.TryParse(organization.Postcode, out var pc) ? pc : 0,
                Address = organization.Address
            };

            _appearance = new AppearanceSettings
            {
                BackgroundColor = organization.ActorBackgroundColor ?? string.Empty,
                ImageUri = organization.ActorBannerPictureUri ?? string.Empty,
                BackgroundEffect = organization.ActorBackgroundEffect ?? "None"
            };
        }
    }

    private void CheckEditPermissions()
    {
        canEdit = organization?.HasHalLink("edit") ?? false;
    }

    private async Task LoadOrganization()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            Logger.LogDebug("Loading organization {OrganizationId}", Id);
            organization = await OrganizationService.GetOrganizationByIdAsync(Id);

            if (organization != null)
            {
                Logger.LogDebug("Loaded organization: {OrganizationName}", organization.FullName);
                CheckEditPermissions();
                _orgEvents = await EventService.GetPublicEventsByOrganizationAsync(Id);
                InitializeEditModel();
            }
            else
            {
                errorMessage = "Organization not found.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load organization: {ex.Message}";
            Logger.LogError(ex, "Failed to load organization {OrganizationId}", Id);
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ToggleEditMode()
    {
        if (isEditMode && organization != null)
        {
            // Revert changes
            InitializeEditModel();
        }

        isEditMode = !isEditMode;
        errorMessage = string.Empty;
        successMessage = string.Empty;
    }

    private async Task SaveChanges()
    {
        if (!formValid) return;

        try
        {
            isSaving = true;
            errorMessage = string.Empty;
            successMessage = string.Empty;

            var success = await OrganizationService.UpdateOrganizationAsync(Id, editModel!);

            if (success?.Success == true)
            {
                successMessage = "Organization updated successfully!";
                isEditMode = false;
                await LoadOrganization(); // Reload to show updated data
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating organization {OrganizationId}", Id);
            errorMessage = $"Failed to update organization: {ex.Message}";
        }
        finally
        {
            isSaving = false;
        }
    }

    private string? ValidateEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return "Email is required";

        if (!email.Contains("@"))
            return "Invalid email format";

        return null;
    }

    private Color GetStatusColor(int statusTypeId)
    {
        return statusTypeId switch
        {
            1 => Color.Warning,  // Pending
            2 => Color.Success,  // Approved
            3 => Color.Error,    // Rejected
            _ => Color.Default
        };
    }
}
