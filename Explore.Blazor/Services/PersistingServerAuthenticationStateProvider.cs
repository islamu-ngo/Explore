using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Claims;
using Explore.Blazor.Client.Models;

namespace Explore.Blazor.Services;

/// <summary>
/// Server-side authentication state provider that persists auth state for WebAssembly hydration.
/// This enables seamless authentication state transfer when switching from Server to WASM in InteractiveAuto mode.
/// </summary>
public sealed class PersistingServerAuthenticationStateProvider : ServerAuthenticationStateProvider, IDisposable
{
    private readonly PersistentComponentState _state;
    private readonly ILogger<PersistingServerAuthenticationStateProvider> _logger;
    private readonly PersistingComponentStateSubscription _subscription;

    private Task<AuthenticationState>? _authenticationStateTask;

    public PersistingServerAuthenticationStateProvider(
        PersistentComponentState state,
        ILogger<PersistingServerAuthenticationStateProvider> logger)
    {
        _state = state;
        _logger = logger;

        // Subscribe to persist auth state when the page is being rendered
        _subscription = state.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveWebAssembly);

        // Listen to auth state changes
        AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        _authenticationStateTask = task;
    }

    private async Task OnPersistingAsync()
    {
        if (_authenticationStateTask is null)
        {
            _logger.LogDebug("[PERSIST AUTH] No authentication state task available");
            throw new UnreachableException($"Authentication state not set in {nameof(OnPersistingAsync)}().");
        }

        var authenticationState = await _authenticationStateTask;
        var principal = authenticationState.User;

        if (principal.Identity?.IsAuthenticated == true)
        {
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                      ?? principal.FindFirst("sub")?.Value;
            var name = principal.Identity.Name;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value 
                     ?? principal.FindFirst("email")?.Value;

            if (userId != null && name != null)
            {
                // Collect additional claims to persist
                var claims = new Dictionary<string, string>();
                foreach (var claim in principal.Claims)
                {
                    // Skip claims we're already handling explicitly
                    if (claim.Type == ClaimTypes.NameIdentifier || 
                        claim.Type == "sub" ||
                        claim.Type == ClaimTypes.Name ||
                        claim.Type == "name" ||
                        claim.Type == ClaimTypes.Email ||
                        claim.Type == "email")
                    {
                        continue;
                    }
                    
                    // Store the first value for each claim type
                    if (!claims.ContainsKey(claim.Type))
                    {
                        claims[claim.Type] = claim.Value;
                    }
                }

                var userInfo = new UserInfo
                {
                    UserId = userId,
                    Name = name,
                    Email = email,
                    Claims = claims
                };

                _logger.LogInformation("[PERSIST AUTH] Persisting auth state for user: {Name} (ID: {UserId})", name, userId);
                _state.PersistAsJson(nameof(UserInfo), userInfo);
            }
            else
            {
                _logger.LogWarning("[PERSIST AUTH] User is authenticated but missing required claims (UserId: {UserId}, Name: {Name})", userId, name);
            }
        }
        else
        {
            _logger.LogDebug("[PERSIST AUTH] User is not authenticated, not persisting auth state");
        }
    }

    public void Dispose()
    {
        _subscription.Dispose();
        AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}
