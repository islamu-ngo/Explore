using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Explore.Blazor.Client.Pages.User;

public partial class UserProfile : ComponentBase
{
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IProgramService ProgramService { get; set; } = default!;
    [Inject] private IOrganizationReviewService OrganizationReviewService { get; set; } = default!;

    private UserDto? UserData { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool _dataLoaded = false;

    private int EventsAttended { get; set; }
    private int ReviewsGiven { get; set; }
    private ICollection<OrganizationReviewDto> MyReviews { get; set; } = new List<OrganizationReviewDto>();

    protected override async Task OnInitializedAsync()
    {
        Console.WriteLine("[USER PROFILE] OnInitializedAsync starting...");
        await LoadUserData();
    }

    private async Task LoadUserData()
    {
        if (_dataLoaded) return;
        
        IsLoading = true;
        
        try
        {
            Console.WriteLine("[USER PROFILE] Loading user data...");
            
            var userData = await UserService.GetCurrentUserAsync();

            if (userData != null)
            {
                UserData = userData;
                Console.WriteLine($"[USER PROFILE] User data loaded: {UserData.Email}");
                
                try
                {
                    var registrations = await ProgramService.GetRegistrationsByUserAsync(UserData.Id);
                    EventsAttended = registrations.Count;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[USER PROFILE] Error loading registrations: {ex.Message}");
                    EventsAttended = 0;
                }

                try
                {
                    MyReviews = await OrganizationReviewService.GetReviewsByUserId(UserData.Id);
                    ReviewsGiven = MyReviews.Count;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[USER PROFILE] Error loading reviews: {ex.Message}");
                    MyReviews = new List<OrganizationReviewDto>();
                    ReviewsGiven = 0;
                }
                
                _dataLoaded = true;
            }
            else
            {
                Console.WriteLine("[USER PROFILE] WARNING: UserData is null");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[USER PROFILE] Error loading user data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string GetDisplayLocation()
    {
        // Location not available in generated UserDto - return placeholder
        return "Location not set";
    }

    private string GetFullName()
    {
        if (UserData == null) return "User";

        if (!string.IsNullOrEmpty(UserData.FirstName) || !string.IsNullOrEmpty(UserData.LastName))
            return $"{UserData.FirstName} {UserData.LastName}".Trim();

        return UserData.Username ?? "User";
    }

    private string GetInitials()
    {
        if (UserData == null) return "?";
        
        var firstInitial = !string.IsNullOrEmpty(UserData.FirstName) ? UserData.FirstName[0].ToString().ToUpper() : "";
        var lastInitial = !string.IsNullOrEmpty(UserData.LastName) ? UserData.LastName[0].ToString().ToUpper() : "";
        
        if (!string.IsNullOrEmpty(firstInitial) || !string.IsNullOrEmpty(lastInitial))
            return $"{firstInitial}{lastInitial}";
            
        return !string.IsNullOrEmpty(UserData.Username) ? UserData.Username[0].ToString().ToUpper() : "?";
    }
}
