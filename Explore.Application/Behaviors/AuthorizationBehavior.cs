// ABOUTME: MediatR pipeline behavior that enforces Cerbos authorization before command execution.
// Checks requests for IAuthorizedRequest interface or [CerbosAuthorize] attribute and denies access on EFFECT_DENY.

using System.Reflection;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that enforces authorization before command handlers execute.
/// Inspects requests for either the <see cref="IAuthorizedRequest"/> interface or the
/// <see cref="CerbosAuthorizeAttribute"/> to determine authorization requirements.
/// If Cerbos returns EFFECT_DENY, throws <see cref="AuthorizationException"/> (mapped to HTTP 403).
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICerbosAuthorizationService _authorizationService;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

    public AuthorizationBehavior(
        ICerbosAuthorizationService authorizationService,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Path 1: Request implements IAuthorizedRequest directly
        if (request is IAuthorizedRequest authorizedRequest)
        {
            await EnforceAuthorizationAsync(
                authorizedRequest.ResourceKind,
                authorizedRequest.ResourceId,
                authorizedRequest.Action,
                authorizedRequest.ResourceAttributes,
                cancellationToken);

            return await next();
        }

        // Path 2: Request class has [CerbosAuthorize] attribute
        var attribute = typeof(TRequest).GetCustomAttribute<CerbosAuthorizeAttribute>();
        if (attribute is not null)
        {
            await EnforceAuthorizationAsync(
                attribute.Resource,
                typeof(TRequest).Name,
                attribute.Action,
                resourceAttributes: null,
                cancellationToken);

            return await next();
        }

        // No authorization requirements — pass through
        return await next();
    }

    private async Task EnforceAuthorizationAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        var isAllowed = await _authorizationService.IsAllowedAsync(
            resourceKind,
            resourceId,
            action,
            resourceAttributes,
            cancellationToken);

        if (!isAllowed)
        {
            _logger.LogWarning(
                "Authorization denied: {Resource}/{ResourceId} action={Action}",
                resourceKind, resourceId, action);

            throw new AuthorizationException(resourceKind, action);
        }

        _logger.LogDebug(
            "Authorization granted: {Resource}/{ResourceId} action={Action}",
            resourceKind, resourceId, action);
    }
}
