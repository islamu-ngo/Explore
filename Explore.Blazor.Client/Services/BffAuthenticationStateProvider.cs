using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using Explore.Blazor.Client.Models;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Authentication state provider for Blazor WebAssembly that syncs with the BFF server.
/// First tries to restore auth state from PersistentComponentState (persisted by server during hydration),
/// then falls back to calling the /bff/me endpoint if no persisted state is available.
/// </summary>
public class PersistentAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> DefaultUnauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly Task<AuthenticationState> _authenticationStateTask;
    private readonly ILogger<PersistentAuthenticationStateProvider> _logger;

    public PersistentAuthenticationStateProvider(
        PersistentComponentState state, 
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PersistentAuthenticationStateProvider>();
        
        // Try to restore auth state from PersistentComponentState first
        if (state.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo) && userInfo is not null)
        {
            _logger.LogInformation("[CLIENT AUTH] Restored auth state from PersistentComponentState for user: {Name}", userInfo.Name);
            _authenticationStateTask = Task.FromResult(CreateAuthenticationState(userInfo));
        }
        else
        {
            // No persisted state found - fetch from BFF endpoint
            _logger.LogInformation("[CLIENT AUTH] No persisted auth state found, fetching from /bff/me");
            _authenticationStateTask = FetchAuthStateFromBffAsync(httpClientFactory);
        }
    }

    private async Task<AuthenticationState> FetchAuthStateFromBffAsync(IHttpClientFactory httpClientFactory)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("BffClient");
            
            _logger.LogDebug("[CLIENT AUTH] Making request to /bff/me");
            var response = await httpClient.GetAsync("/bff/me");
            
            if (response.IsSuccessStatusCode)
            {
                var bffUserInfo = await response.Content.ReadFromJsonAsync<BffUserInfoResponse>();
                
                if (bffUserInfo?.Name != null)
                {
                    _logger.LogInformation("[CLIENT AUTH] Authenticated via /bff/me: {Name}", bffUserInfo.Name);
                    
                    // Convert BFF response to UserInfo
                    var userInfo = new UserInfo
                    {
                        UserId = bffUserInfo.Claims?.FirstOrDefault(c => c.Type == "sub")?.Value 
                                ?? bffUserInfo.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                                ?? Guid.NewGuid().ToString(),
                        Name = bffUserInfo.Name,
                        Email = bffUserInfo.Claims?.FirstOrDefault(c => c.Type == "email" || c.Type == ClaimTypes.Email)?.Value,
                        Claims = bffUserInfo.Claims?
                            .Where(c => c.Type != "sub" && c.Type != "name" && c.Type != "email" 
                                     && c.Type != ClaimTypes.NameIdentifier && c.Type != ClaimTypes.Name && c.Type != ClaimTypes.Email)
                            .GroupBy(c => c.Type)
                            .ToDictionary(g => g.Key, g => g.First().Value) ?? new Dictionary<string, string>()
                    };
                    
                    return CreateAuthenticationState(userInfo);
                }
                else
                {
                    _logger.LogDebug("[CLIENT AUTH] /bff/me returned null name - user is anonymous");
                }
            }
            else
            {
                _logger.LogWarning("[CLIENT AUTH] /bff/me returned {StatusCode}", response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "[CLIENT AUTH] HTTP error fetching auth state from /bff/me");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CLIENT AUTH] Unexpected error fetching auth state from /bff/me");
        }

        // Return anonymous user
        _logger.LogDebug("[CLIENT AUTH] Returning anonymous authentication state");
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    private AuthenticationState CreateAuthenticationState(UserInfo userInfo)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userInfo.UserId),
            new Claim(ClaimTypes.Name, userInfo.Name),
        };

        if (!string.IsNullOrEmpty(userInfo.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, userInfo.Email));
        }

        // Add additional claims from the persisted state
        foreach (var claim in userInfo.Claims)
        {
            claims.Add(new Claim(claim.Key, claim.Value));
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "BffAuthentication")));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _authenticationStateTask;

    // Response model for /bff/me endpoint
    private class BffUserInfoResponse
    {
        public string? Name { get; set; }
        public List<BffClaimResponse>? Claims { get; set; }
    }

    private class BffClaimResponse
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
