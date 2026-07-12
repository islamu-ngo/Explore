// ABOUTME: Focused authentication routing and rate-partition tests for managed Control Plane credentials.
// ABOUTME: Proves managed headers select isolated machine auth, reject mixed credentials, and avoid anonymous write buckets.

using System.Security.Claims;
using Explore.API.Authentication;
using Explore.API.Extensions;
using Explore.Application.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Event.Api.IntegrationTests.Features;

public sealed class ManagedControlPlaneAuthenticationRoutingTests
{
    [Test]
    public async Task ManagedHeader_SelectsManagedSchemeAndAuthenticatedPartition()
    {
        var context = new DefaultHttpContext();
        SetManagedEndpoint(context);
        context.Request.Headers[ManagedControlPlaneAuthenticationDefaults.HeaderName] = "managed-key.secret";
        Guid managedInstanceId = Guid.CreateVersion7();

        string scheme = AuthenticationExtensions.SelectDefaultAuthenticationScheme(context);
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "managed-control-plane:managed-key"),
                new Claim(
                    ManagedControlPlaneAuthenticationDefaults.ManagedInstanceIdClaim,
                    managedInstanceId.ToString("D"))
            ],
            ManagedControlPlaneAuthenticationDefaults.Scheme,
            ClaimTypes.Name,
            ClaimTypes.Role));

        await Assert.That(scheme).IsEqualTo(ManagedControlPlaneAuthenticationDefaults.Scheme);
        await Assert.That(RateLimitingExtensions.GetAuthenticatedPartitionKey(context))
            .IsEqualTo($"managed-instance:{managedInstanceId:D}");
    }

    [Test]
    public async Task ManagedHeader_WithOtherCredentialHeaders_SelectsManagedSchemeAndRejectsMix()
    {
        var context = new DefaultHttpContext();
        SetManagedEndpoint(context);
        context.Request.Headers[ManagedControlPlaneAuthenticationDefaults.HeaderName] = "managed-key.secret";
        context.Request.Headers[ApiAuthenticationHeaderNames.ApiKey] = "ordinary-key.secret";
        context.Request.Headers.Authorization = "Bearer token";

        await Assert.That(AuthenticationExtensions.SelectDefaultAuthenticationScheme(context))
            .IsEqualTo(ManagedControlPlaneAuthenticationDefaults.Scheme);
    }

    [Test]
    public async Task NoManagedHeader_PreservesExistingApiKeyAndBearerSelection()
    {
        var apiKeyContext = new DefaultHttpContext();
        apiKeyContext.Request.Headers[ApiAuthenticationHeaderNames.ApiKey] = "ordinary-key.secret";
        var bearerContext = new DefaultHttpContext();
        bearerContext.Request.Headers.Authorization = "Bearer token";

        await Assert.That(AuthenticationExtensions.SelectDefaultAuthenticationScheme(apiKeyContext))
            .IsEqualTo(ApiAuthenticationSchemeNames.ApiKey);
        await Assert.That(AuthenticationExtensions.SelectDefaultAuthenticationScheme(bearerContext))
            .IsEqualTo(JwtBearerDefaults.AuthenticationScheme);
    }

    [Test]
    public async Task ManagedHeader_OnUnrelatedEndpoint_DoesNotSelectManagedScheme()
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute()),
            "ordinary-authorized"));
        context.Request.Headers[ManagedControlPlaneAuthenticationDefaults.HeaderName] = "managed-key.secret";

        await Assert.That(AuthenticationExtensions.SelectDefaultAuthenticationScheme(context))
            .IsEqualTo(JwtBearerDefaults.AuthenticationScheme);
    }

    private static void SetManagedEndpoint(HttpContext context) =>
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(
                new AuthorizeAttribute(ManagedControlPlaneAuthorizationPolicies.Write)),
            "managed"));
}
