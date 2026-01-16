using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Explore.Blazor.Client.Pages.User;

/// <summary>
/// Code-behind for the User Profile page.
/// Displays user information, event attendance stats, and reviews.
/// </summary>
public partial class UserProfile : ComponentBase
{
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private IOrganizationReviewService OrganizationReviewService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private ILogger<UserProfile> Logger { get; set; } = default!;

    // State
    private UserDto? UserData { get; set; }
    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }

    // Statistics
    private int EventsAttended { get; set; }
    private int ReviewsGiven { get; set; }
    private ICollection<OrganizationReviewDto> MyReviews { get; set; } = new List<OrganizationReviewDto>();

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("[UserProfile] OnInitializedAsync starting...");
        await LoadUserDataAsync();
    }

    private async Task LoadUserDataAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Logger.LogInformation("[UserProfile] Loading user data...");

            // First, try to get the user from the API
            var userData = await UserService.GetCurrentUserAsync();

            if (userData == null)
            {
                Logger.LogWarning("[UserProfile] User data not found, attempting sync...");

                // Sync user from Keycloak to local database
                var syncResult = await UserService.SyncUserAsync();

                if (syncResult?.Success == true)
                {
                    Logger.LogInformation("[UserProfile] User synced successfully, retrying load...");
                    // Small delay to ensure database write is complete
                    await Task.Delay(200);
                    userData = await UserService.GetCurrentUserAsync();
                }
                else
                {
                    Logger.LogWarning("[UserProfile] User sync failed: {Message}", syncResult?.Message ?? "Unknown error");
                }
            }

            if (userData != null)
            {
                UserData = userData;
                Logger.LogInformation("[UserProfile] User data loaded: {Email}", UserData.Email);

                // Load statistics in parallel
                await LoadStatisticsAsync(userData.Id);
            }
            else
            {
                Logger.LogWarning("[UserProfile] UserData is still null after sync attempt");
                ErrorMessage = "Unable to load user profile. Please try refreshing the page.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[UserProfile] Error loading user data");
            ErrorMessage = $"An error occurred while loading your profile: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadStatisticsAsync(Guid userId)
    {
        // Load event registrations and reviews in parallel
        var registrationsTask = LoadEventRegistrationsAsync(userId);
        var reviewsTask = LoadReviewsAsync(userId);

        await Task.WhenAll(registrationsTask, reviewsTask);
    }

    private async Task LoadEventRegistrationsAsync(Guid userId)
    {
        try
        {
            var registrations = await EventService.GetRegistrationsByUserAsync(userId);
            EventsAttended = registrations?.Count ?? 0;
            Logger.LogInformation("[UserProfile] Loaded {Count} event registrations", EventsAttended);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[UserProfile] Error loading registrations");
            EventsAttended = 0;
        }
    }

    private async Task LoadReviewsAsync(Guid userId)
    {
        try
        {
            MyReviews = await OrganizationReviewService.GetReviewsByUserId(userId);
            ReviewsGiven = MyReviews?.Count ?? 0;
            Logger.LogInformation("[UserProfile] Loaded {Count} reviews", ReviewsGiven);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[UserProfile] Error loading reviews");
            MyReviews = new List<OrganizationReviewDto>();
            ReviewsGiven = 0;
        }
    }

    /// <summary>
    /// Gets the display location from user data.
    /// Returns placeholder if location is not available.
    /// </summary>
    private string GetDisplayLocation()
    {
        // Location is not currently available in UserDto
        // This can be extended when location support is added
        return "Location not set";
    }

    /// <summary>
    /// Gets the full name of the user.
    /// Falls back to username or "User" if name not available.
    /// </summary>
    private string GetFullName()
    {
        if (UserData == null) return "User";

        var firstName = UserData.FirstName?.Trim() ?? string.Empty;
        var lastName = UserData.LastName?.Trim() ?? string.Empty;

        var fullName = $"{firstName} {lastName}".Trim();

        if (!string.IsNullOrEmpty(fullName))
            return fullName;

        return UserData.Username ?? "User";
    }

    /// <summary>
    /// Gets the initials for the avatar display.
    /// </summary>
    private string GetInitials()
    {
        if (UserData == null) return "?";

        var firstInitial = !string.IsNullOrEmpty(UserData.FirstName)
            ? UserData.FirstName[0].ToString().ToUpper()
            : string.Empty;

        var lastInitial = !string.IsNullOrEmpty(UserData.LastName)
            ? UserData.LastName[0].ToString().ToUpper()
            : string.Empty;

        var initials = $"{firstInitial}{lastInitial}";

        if (!string.IsNullOrEmpty(initials))
            return initials;

        return !string.IsNullOrEmpty(UserData.Username)
            ? UserData.Username[0].ToString().ToUpper()
            : "?";
    }

    /// <summary>
    /// Gets the username for display.
    /// </summary>
    private string GetUsername()
    {
        if (UserData == null) return "user";
        return UserData.Username ?? UserData.Email?.Split('@').FirstOrDefault() ?? "user";
    }
}
