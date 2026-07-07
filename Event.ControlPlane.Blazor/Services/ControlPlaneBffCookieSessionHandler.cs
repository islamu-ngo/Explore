// ABOUTME: Enriches Control Plane BFF cookies with server-verified administrator authority claims.
// ABOUTME: Calls the API from the server-side cookie boundary while keeping browser tokens hidden.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Event.Web.BffHosting.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Event.ControlPlane.Blazor.Services;

public sealed class ControlPlaneBffCookieSessionHandler(
    IHttpClientFactory httpClientFactory,
    ILogger<ControlPlaneBffCookieSessionHandler> logger) : IEventBffCookieSessionHandler
{
    public const string AdminAuthorityHttpClientName = "ControlPlaneAdminAuthority";

    private const string InstanceAdminClaim = "explore:admin:instance";
    private const string TenantAdminClaim = "explore:admin:tenant";
    private const string OrganizationAdminClaim = "explore:admin:organization";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task OnSigningInAsync(CookieSigningInContext context)
    {
        if (context.Principal is null)
        {
            return;
        }

        await TryEnrichAsync(context.Principal, context.Properties, context.HttpContext.RequestAborted);
    }

    public async Task OnTokenRefreshSucceededAsync(
        CookieValidatePrincipalContext context,
        IReadOnlyList<AuthenticationToken> _)
    {
        if (context.Principal is null)
        {
            return;
        }

        if (await TryEnrichAsync(context.Principal, context.Properties, context.HttpContext.RequestAborted))
        {
            context.ReplacePrincipal(context.Principal);
        }
    }

    public Task OnTokenRefreshRejectedAsync(CookieValidatePrincipalContext context, string reason) => Task.CompletedTask;

    public Task<bool> TryRedirectRejectedHtmlNavigationAsync(CookieValidatePrincipalContext context, string reason) =>
        Task.FromResult(false);

    private async Task<bool> TryEnrichAsync(
        ClaimsPrincipal principal,
        AuthenticationProperties? properties,
        CancellationToken cancellationToken)
    {
        var bearerValue = properties?.GetTokenValue("access_token");
        if (string.IsNullOrWhiteSpace(bearerValue))
        {
            logger.LogDebug("Skipping Control Plane admin authority enrichment because the BFF ticket has no bearer value.");
            return false;
        }

        try
        {
            var client = httpClientFactory.CreateClient(AdminAuthorityHttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/User/admin-authority");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerValue);

            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Control Plane admin authority lookup failed with status {StatusCode}.",
                    (int)response.StatusCode);
                return false;
            }

            var authority = await response.Content.ReadFromJsonAsync<AdminAuthorityResponse>(JsonOptions, cancellationToken);
            if (authority is null)
            {
                logger.LogWarning("Control Plane admin authority lookup returned an empty response.");
                return false;
            }

            ReplaceAdminClaims(principal, authority);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Control Plane admin authority enrichment failed.");
            return false;
        }
    }

    private static void ReplaceAdminClaims(ClaimsPrincipal principal, AdminAuthorityResponse authority)
    {
        foreach (var identity in principal.Identities)
        {
            foreach (var claim in identity.FindAll(InstanceAdminClaim).ToArray())
            {
                identity.RemoveClaim(claim);
            }

            foreach (var claim in identity.FindAll(TenantAdminClaim).ToArray())
            {
                identity.RemoveClaim(claim);
            }

            foreach (var claim in identity.FindAll(OrganizationAdminClaim).ToArray())
            {
                identity.RemoveClaim(claim);
            }
        }

        var claims = new List<Claim>();
        if (authority.IsInstanceAdmin)
        {
            claims.Add(new Claim(InstanceAdminClaim, "true"));
        }

        claims.AddRange(authority.AdminTenantIds.Select(id => new Claim(TenantAdminClaim, id.ToString())));
        claims.AddRange(authority.AdminOrganizationIds.Select(id => new Claim(OrganizationAdminClaim, id.ToString())));

        if (claims.Count == 0)
        {
            return;
        }

        var targetIdentity = principal.Identities.FirstOrDefault(identity => identity.IsAuthenticated)
            ?? principal.Identities.FirstOrDefault();
        if (targetIdentity is null)
        {
            principal.AddIdentity(new ClaimsIdentity(claims));
            return;
        }

        targetIdentity.AddClaims(claims);
    }

    private sealed class AdminAuthorityResponse
    {
        public bool IsInstanceAdmin { get; init; }

        public IReadOnlyList<Guid> AdminTenantIds { get; init; } = [];

        public IReadOnlyList<Guid> AdminOrganizationIds { get; init; } = [];
    }
}
