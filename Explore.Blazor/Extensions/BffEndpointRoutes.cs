using Duende.Bff;
using Duende.Bff.AccessTokenManagement;
using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Extensions;

public static class BffEndpointRoutes
{
    // Controllers with no [Authorize] attributes anywhere in the API.
    private static readonly string[] PublicOnlyEndpoints =
    {
        "/api/v1/ImageProxy",
        "/api/v1/Event",
        "/api/v1/Event/{id}",
        "/api/v1/ActorType",
        "/api/v1/ApprovalStatus",
        "/api/v1/AudienceAge",
        "/api/v1/AudienceGender",
        "/api/v1/DidCustodyType",
        "/api/v1/EventFormat",
        "/api/v1/EventStatus",
        "/api/v1/EventType",
        "/api/v1/FileType",
        "/api/v1/Language",
        "/api/v1/Madhab",
        "/api/v1/OrganizationPosition",
        "/api/v1/OrganizationReview",
        "/api/v1/OrganizationRole",
        "/api/v1/RegistrationMode",
        "/api/v1/TagType",
        "/api/v1/UserRole",
        "/api/v1/VisibilityType"
    };

    // Controllers with [Authorize] on all endpoints (no [AllowAnonymous]).
    private static readonly string[] ProtectedOnlyEndpoints =
    {
        "/api/v1/OrganizationMember",
        "/api/v1/User"
    };

    public static void MapExploreApiBffEndpoints(this IEndpointRouteBuilder app, Uri baseApiUri, ILogger logger)
    {
        foreach (var pattern in PublicOnlyEndpoints)
        {
            app.MapRemoteBffApiEndpoint(pattern, baseApiUri)
                .WithAccessToken(RequiredTokenType.None);
        }

        foreach (var pattern in ProtectedOnlyEndpoints)
        {
            app.MapRemoteBffApiEndpoint(pattern, baseApiUri)
                .WithAccessToken(RequiredTokenType.User)
                .RequireAuthorization();
        }

        // Hybrid endpoints (public reads + authenticated writes) live under /api/v1.
        app.MapRemoteBffApiEndpoint("/api/v1/{**catchall}", baseApiUri)
            .WithAccessToken(RequiredTokenType.UserOrNone);

        logger.LogInformation(
            "BFF: mapped Explore API routes (public-only: {PublicCount}, protected-only: {ProtectedCount}, hybrid default: /api/v1)",
            PublicOnlyEndpoints.Length,
            ProtectedOnlyEndpoints.Length);
    }
}
