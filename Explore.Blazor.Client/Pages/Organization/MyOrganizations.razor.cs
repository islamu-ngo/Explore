using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Organization;

/// <summary>
/// Displays organizations that the current user is a member of.
/// Allows searching, filtering, and navigation to organization management.
/// </summary>
public partial class MyOrganizations : ComponentBase
{
    [Inject] private IOrganizationService OrganizationService { get; set; } = default!;
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<MyOrganizations> Logger { get; set; } = default!;

    private ICollection<OrganizationListDto>? _organizations;
    private bool _isLoading = true;
    private string? _errorMessage;
    private string _searchString = string.Empty;

    /// <summary>
    /// Filtered organizations based on search string.
    /// </summary>
    private IEnumerable<OrganizationListDto> FilteredOrganizations =>
        _organizations?
            .Where(x => string.IsNullOrWhiteSpace(_searchString) ||
                        x.FullName.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ||
                        x.Email.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
        ?? Enumerable.Empty<OrganizationListDto>();

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("[MyOrganizations] OnInitializedAsync starting...");
        await LoadOrganizationsAsync();
    }

    /// <summary>
    /// Loads organizations for the current user.
    /// The API extracts user ID from JWT token, so no client-side user ID extraction needed.
    /// </summary>
    private async Task LoadOrganizationsAsync()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            Logger.LogInformation("[MyOrganizations] Loading organizations...");

            // GetMyOrganizationsAsync calls /api/v1/organization/my which extracts user from JWT
            var orgs = await OrganizationService.GetMyOrganizationsAsync();

            if (orgs == null || orgs.Count == 0)
            {
                Logger.LogInformation("[MyOrganizations] No organizations found, attempting user sync...");
                var syncResult = await UserService.SyncUserAsync();

                if (syncResult?.Success == true)
                {
                    await Task.Delay(200); // Brief delay for sync to complete
                    orgs = await OrganizationService.GetMyOrganizationsAsync();
                }
            }

            _organizations = orgs ?? new List<OrganizationListDto>();
            Logger.LogInformation("[MyOrganizations] Loaded {Count} organizations", _organizations.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[MyOrganizations] Error loading organizations");
            _errorMessage = "Unable to load your organizations. Please try again.";
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Retry loading organizations after an error.
    /// </summary>
    private async Task RetryLoadAsync()
    {
        _errorMessage = null;
        await LoadOrganizationsAsync();
    }

    /// <summary>
    /// Check if user can create events for this organization.
    /// Roles: Creator (1), CoOwner (2), Admin (3)
    /// </summary>
    private static bool CanCreateEvents(OrganizationListDto org)
    {
        return org.CurrentUserRole.HasValue &&
               org.CurrentUserRole.Value is 1 or 2 or 3;
    }

    /// <summary>
    /// Gets a consistent color based on organization name.
    /// </summary>
    private static Color GetOrganizationColor(string? name)
    {
        if (string.IsNullOrEmpty(name)) return Color.Primary;
        var colors = new[] { Color.Primary, Color.Secondary, Color.Tertiary, Color.Info, Color.Success, Color.Warning };
        return colors[Math.Abs(name.GetHashCode()) % colors.Length];
    }

    /// <summary>
    /// Gets the display color for a user's role in the organization.
    /// </summary>
    private static Color GetRoleColor(int roleId)
    {
        return roleId switch
        {
            1 => Color.Warning,  // Creator
            2 => Color.Success,  // CoOwner
            3 => Color.Info,     // Admin
            4 => Color.Default,  // Moderator
            5 => Color.Default,  // Member
            6 => Color.Default,  // Viewer
            _ => Color.Default
        };
    }

    /// <summary>
    /// Gets the display name for a role ID.
    /// </summary>
    private static string GetRoleName(int roleId)
    {
        return roleId switch
        {
            1 => "Creator",
            2 => "Co-Owner",
            3 => "Admin",
            4 => "Moderator",
            5 => "Member",
            6 => "Viewer",
            _ => "Member"
        };
    }

    /// <summary>
    /// Gets initials from organization name for avatar display.
    /// </summary>
    private static string GetInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "O";

        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length switch
        {
            0 => "O",
            1 => words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant(),
            _ => $"{words[0][0]}{words[1][0]}".ToUpperInvariant()
        };
    }
}
