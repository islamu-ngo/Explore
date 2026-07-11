// ABOUTME: Generates absolute public URLs from HttpContext, respecting reverse proxy and tenant context.
// ABOUTME: Single source of truth for all external-facing URLs used in OG tags, sharing, and calendar links.

using Explore.Application.Contracts.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Builds absolute, canonical public URLs using the current HTTP request context.
/// Handles reverse proxy headers (X-Forwarded-Host, X-Forwarded-Proto) and path base.
/// </summary>
public class PublicUrlBuilder : IPublicUrlBuilder
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PublicUrlBuilder> _logger;

    public PublicUrlBuilder(
        IHttpContextAccessor httpContextAccessor,
        ILogger<PublicUrlBuilder> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public string GetEventUrl(Guid eventId)
    {
        return $"{GetBaseUrl()}/events/{eventId}";
    }

    /// <inheritdoc />
    public string GetActorUrl(Guid actorId)
    {
        // Actor URLs use a generic actor route that resolves to the correct profile type server-side
        return $"{GetBaseUrl()}/actors/{actorId}";
    }

    /// <inheritdoc />
    public string GetOrganizationUrl(Guid organizationId)
    {
        return $"{GetBaseUrl()}/organizations/{organizationId}";
    }

    /// <inheritdoc />
    public string GetGroupUrl(Guid groupId)
    {
        return $"{GetBaseUrl()}/groups/{groupId}";
    }

    /// <inheritdoc />
    public string GetUserProfileUrl(Guid userId)
    {
        return $"{GetBaseUrl()}/users/{userId}";
    }

    /// <inheritdoc />
    public string GetBaseUrl()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null when building public URL; returning empty base URL");
            return string.Empty;
        }

        var request = httpContext.Request;

        // Respect reverse proxy forwarded headers (set by nginx, Traefik, etc.)
        var scheme = ResolveScheme(request);
        var host = ResolveHost(request);
        var pathBase = request.PathBase.HasValue
            ? request.PathBase.Value!.TrimEnd('/')
            : string.Empty;

        return $"{scheme}://{host}{pathBase}";
    }

    /// <inheritdoc />
    public string GetPublicImageUrl(Guid storageObjectId)
    {
        return $"{GetBaseUrl()}/api/storageobject/{storageObjectId}/public";
    }

    private static string ResolveScheme(HttpRequest request)
    {
        // X-Forwarded-Proto takes precedence (set by reverse proxy)
        var forwardedProto = request.Headers["X-Forwarded-Proto"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedProto))
        {
            return forwardedProto.Split(',')[0].Trim();
        }

        return request.Scheme;
    }

    private static string ResolveHost(HttpRequest request)
    {
        // X-Forwarded-Host takes precedence (set by reverse proxy)
        var forwardedHost = request.Headers["X-Forwarded-Host"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedHost))
        {
            return forwardedHost.Split(',')[0].Trim();
        }

        return request.Host.ToString();
    }
}
