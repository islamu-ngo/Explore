using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace Explore.Blazor;

public class AuthorizationHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancelationToken)
    {
        var httpContext = httpContextAccessor.HttpContext ?? 
                          throw new InvalidOperationException("No HttpContext available!");
        string? accessToken = await httpContext!.GetTokenAsync("Keycloak","access_token");

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancelationToken);
    }
}