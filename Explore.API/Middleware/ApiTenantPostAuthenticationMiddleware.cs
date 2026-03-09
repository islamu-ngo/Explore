// ABOUTME: Completes split-phase tenant handling after authentication for API-key callers and mismatch checks.
// ABOUTME: Sets tenant context from authenticated machine principals and fail-closes when tenant hints conflict.

using Explore.Application.Authentication;
using Explore.Application.Constants;
using Explore.Application.Contracts.Services;
using Explore.Application.Telemetry;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Middleware;

public sealed class ApiTenantPostAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiTenantPostAuthenticationMiddleware> _logger;

    public ApiTenantPostAuthenticationMiddleware(RequestDelegate next, ILogger<ApiTenantPostAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContextAccessor tenantContextAccessor, IProblemDetailsService problemDetailsService, BusinessMetrics metrics)
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var apiKeyPrincipal = context.User.TryGetApiKeyPrincipalContext();
        var authenticatedTenantId = apiKeyPrincipal?.TenantId;
        var requestedTenantId = ResolveRequestedTenantId(context);
        var hasApiKeyHeader = context.Request.Headers.ContainsKey(ApiAuthenticationHeaderNames.ApiKey);

        if (authenticatedTenantId is Guid authenticatedApiKeyTenantId &&
            requestedTenantId is Guid hintedTenantId &&
            authenticatedApiKeyTenantId != Guid.Empty &&
            hintedTenantId != Guid.Empty &&
            authenticatedApiKeyTenantId != hintedTenantId)
        {
            metrics.RecordExternalApiKeyAuthentication(
                "tenant_mismatch",
                authenticatedApiKeyTenantId.ToString(),
                apiKeyPrincipal?.OwnerType.ToString());
            _logger.LogWarning(
                "External API key {KeyId} for tenant {AuthenticatedTenantId} attempted mismatched tenant {RequestedTenantId} on {Path}.",
                apiKeyPrincipal?.KeyId,
                authenticatedApiKeyTenantId,
                hintedTenantId,
                context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status404NotFound;

            await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Tenant mismatch",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                    Detail = "The authenticated tenant does not match the requested tenant context.",
                    Instance = context.Request.Path
                }
            });
            return;
        }

        if (!tenantContextAccessor.IsResolved)
        {
            if (authenticatedTenantId is Guid principalTenantId && principalTenantId != Guid.Empty)
            {
                tenantContextAccessor.SetTenant(principalTenantId);
                await _next(context);
                return;
            }

            if (hasApiKeyHeader)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status401Unauthorized,
                        Title = "API key authentication failed",
                        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                        Detail = "The API key could not be authenticated for this request.",
                        Instance = context.Request.Path
                    }
                });
                return;
            }

            await _next(context);
            return;
        }

        if (authenticatedTenantId is Guid resolvedPrincipalTenantId &&
            tenantContextAccessor.TenantId is Guid resolvedRequestTenantId &&
            resolvedPrincipalTenantId != Guid.Empty &&
            resolvedRequestTenantId != Guid.Empty &&
            resolvedPrincipalTenantId != resolvedRequestTenantId)
        {
            metrics.RecordExternalApiKeyAuthentication(
                "tenant_mismatch",
                resolvedPrincipalTenantId.ToString(),
                apiKeyPrincipal?.OwnerType.ToString());
            _logger.LogWarning(
                "External API key {KeyId} for tenant {AuthenticatedTenantId} conflicted with resolved request tenant {ResolvedTenantId} on {Path}.",
                apiKeyPrincipal?.KeyId,
                resolvedPrincipalTenantId,
                resolvedRequestTenantId,
                context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status404NotFound;

            await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Tenant mismatch",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                    Detail = "The authenticated tenant does not match the requested tenant context.",
                    Instance = context.Request.Path
                }
            });
            return;
        }

        await _next(context);
    }
    private static Guid? ResolveRequestedTenantId(HttpContext context)
    {
        return context.Items.TryGetValue(ApiTenantResolutionMiddleware.RequestedTenantIdItemKey, out var value) &&
               value is Guid tenantId &&
               tenantId != Guid.Empty
            ? tenantId
            : null;
    }
}
