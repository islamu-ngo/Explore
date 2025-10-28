using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Net.Http.Json;

namespace Explore.Blazor.Client.Services;

public class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;

    public AnonymousAuthenticationStateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Try to get user info from BFF endpoint
            var userInfo = await _httpClient.GetFromJsonAsync<UserInfo>("/bff/me");
            
            if (userInfo?.Name != null)
            {
                // User is authenticated
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userInfo.Name)
                };
                
                // Add additional claims if available
                if (userInfo.Claims != null)
                {
                    claims.AddRange(userInfo.Claims.Select(c => new Claim(c.Type, c.Value)));
                }

                var identity = new ClaimsIdentity(claims, "BFF");
                var user = new ClaimsPrincipal(identity);
                return new AuthenticationState(user);
            }
        }
        catch (HttpRequestException)
        {
            // BFF endpoint not available or user not authenticated
            // Fall through to return anonymous user
        }
        catch (Exception)
        {
            // Any other error, treat as anonymous
        }

        // Return anonymous user
        var anonymous = new ClaimsIdentity();
        var anonymousUser = new ClaimsPrincipal(anonymous);
        return new AuthenticationState(anonymousUser);
    }

    public class UserInfo
    {
        public string? Name { get; set; }
        public List<ClaimInfo>? Claims { get; set; }
    }

    public class ClaimInfo
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}