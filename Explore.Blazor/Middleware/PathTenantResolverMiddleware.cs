// ABOUTME: Extracts tenant slugs from configured path prefixes and rewrites the request path for Blazor routing.
// ABOUTME: Keeps tenant authority out of the UI host by storing only slug context, not resolved tenant identity.

using Explore.Application.Contracts.Services;
using Explore.Blazor.Services;

namespace Explore.Blazor.Middleware;

public class PathTenantResolverMiddleware
{
    private readonly RequestDelegate _next;

    public PathTenantResolverMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IResolverConfigService resolverConfigService, ITenantRouteContextAccessor tenantRouteContextAccessor)
    {
        var configuration = await resolverConfigService.GetConfigurationAsync(context.RequestAborted);
        if (!configuration.PathEnabled)
        {
            await _next(context);
            return;
        }

        var pathPrefix = NormalizePathPrefix(configuration.PathPrefix);
        if (!context.Request.Path.StartsWithSegments(pathPrefix, out var remainingAfterPrefix))
        {
            await _next(context);
            return;
        }

        var remainingValue = remainingAfterPrefix.Value ?? string.Empty;
        var pathSegments = remainingValue.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Length == 0)
        {
            await _next(context);
            return;
        }

        var slug = pathSegments[0];
        tenantRouteContextAccessor.SetTenantSlug(slug);

        var matchedPathBase = new PathString(pathPrefix + "/" + slug);
        var rewrittenPath = remainingAfterPrefix.StartsWithSegments(new PathString("/" + slug), out var finalPath)
            ? finalPath
            : PathString.Empty;

        context.Request.PathBase = context.Request.PathBase.Add(matchedPathBase);
        context.Request.Path = rewrittenPath.HasValue ? rewrittenPath : new PathString("/");

        await _next(context);
    }

    private static string NormalizePathPrefix(string? pathPrefix)
    {
        if (string.IsNullOrWhiteSpace(pathPrefix))
        {
            return "/t";
        }

        var normalized = pathPrefix.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        return normalized.TrimEnd('/');
    }
}
