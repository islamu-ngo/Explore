using Explore.Blazor.Client.Models.DTOs;
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

    private int EventsAttended { get; set; }
    private int ReviewsGiven { get; set; }
    private List<OrganizationReviewDto> MyReviews { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadUserData();
    }

    private async Task LoadUserData()
    {
        IsLoading = true;
        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                UserData = await UserService.GetCurrentUserAsync();

                if (UserData != null)
                {
                    var registrations = await ProgramService.GetMyRegistrationsAsync();
                    EventsAttended = registrations.Count; // Or filter by Status == "Completed"

                    MyReviews = await OrganizationReviewService.GetReviewsByUserId(UserData.Id);
                    ReviewsGiven = MyReviews.Count;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string GetDisplayLocation()
    {
        if (UserData == null) return "Location not set";
        
        if (!string.IsNullOrEmpty(UserData.City) && !string.IsNullOrEmpty(UserData.Country))
            return $"{UserData.City}, {UserData.Country}";
        if (!string.IsNullOrEmpty(UserData.City))
            return UserData.City;
        if (!string.IsNullOrEmpty(UserData.Country))
            return UserData.Country;
            
        return "Location not set";
    }

    private string GetFullName()
    {
        if (UserData == null) return "User";
        
        if (!string.IsNullOrEmpty(UserData.FirstName) || !string.IsNullOrEmpty(UserData.LastName))
            return $"{UserData.FirstName} {UserData.LastName}".Trim();
            
        return UserData.Username;
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
