using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Explore.Blazor.Client.Pages.User;

public partial class UserProfile : ComponentBase
{
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private UserProfileData? UserData { get; set; }
    private bool IsLoading { get; set; } = true;

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
                // Get user info from BFF endpoint
                var response = await HttpClient.GetFromJsonAsync<BffUserResponse>("/bff/me");

                if (response != null)
                {
                    UserData = MapToUserProfileData(user, response);
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

    private UserProfileData MapToUserProfileData(ClaimsPrincipal user, BffUserResponse bffResponse)
    {
        // Use GroupBy to handle duplicate claim types (e.g., multiple roles)
        // Take the first value for each claim type
        var claims = bffResponse.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.First().Value);

        // Helper function to get claim value
        string GetClaim(string claimType) => claims.TryGetValue(claimType, out var value) ? value : string.Empty;

        return new UserProfileData
        {
            Name = bffResponse.Name ?? GetClaim("name") ?? GetClaim("preferred_username") ?? "User",
            Username = GetClaim("preferred_username"),
            Email = GetClaim("email"),
            EmailVerified = GetClaim("email_verified") == "true",
            GivenName = GetClaim("given_name"),
            FamilyName = GetClaim("family_name"),
            
            // Mock data (not available from Keycloak or not asked yet)
            City = "Amsterdam",
            Country = "Netherlands",
            JoinDate = DateTime.Now.AddYears(-2).AddMonths(-6),
            EventsAttended = 42,
            ReviewsGiven = 18,
            YearsActive = 2.5
        };
    }

    private class BffUserResponse
    {
        public string? Name { get; set; }
        public List<ClaimData> Claims { get; set; } = new();
    }

    private class ClaimData
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    private class UserProfileData
    {
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string GivenName { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        
        // Mock stats
        public int EventsAttended { get; set; }
        public int ReviewsGiven { get; set; }
        public double YearsActive { get; set; }

        public string DisplayLocation
        {
            get
            {
                if (!string.IsNullOrEmpty(City) && !string.IsNullOrEmpty(Country))
                    return $"{City}, {Country}";
                if (!string.IsNullOrEmpty(City))
                    return City;
                if (!string.IsNullOrEmpty(Country))
                    return Country;
                return "Location not set";
            }
        }

        public string FullName
        {
            get
            {
                if (!string.IsNullOrEmpty(GivenName) || !string.IsNullOrEmpty(FamilyName))
                    return $"{GivenName} {FamilyName}".Trim();
                return Name;
            }
        }
    }
}
