using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Explore.Blazor.Services;

public interface ICircuitAccessTokenService
{
    string? AccessToken { get; }
    void SetToken(string? token);
}

public class CircuitAccessTokenService : ICircuitAccessTokenService
{
    public string? AccessToken { get; private set; }
    
    public void SetToken(string? token)
    {
        AccessToken = token;
    }
}

public class AccessTokenForwardingHandler : DelegatingHandler
{
    private readonly ICircuitAccessTokenService _tokenService;
    private readonly ILogger<AccessTokenForwardingHandler> _logger;

    public AccessTokenForwardingHandler(
        ICircuitAccessTokenService tokenService,
        ILogger<AccessTokenForwardingHandler> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = _tokenService.AccessToken;
        if (!string.IsNullOrEmpty(token) && !request.Headers.Contains("Authorization"))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            _logger.LogDebug("Added Bearer token to request");
        }
        
        return base.SendAsync(request, cancellationToken);
    }
}
