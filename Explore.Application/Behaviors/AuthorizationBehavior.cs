// ABOUTME: MediatR pipeline behavior that enforces authorization before command execution.
// ABOUTME: Checks requests for IAuthorizedRequest, [AuthorizeResource] (optionally enhanced by ISecureRequest), and denies on unauthorized.

using System.Diagnostics;
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
/// <see cref="AuthorizeResourceAttribute"/> to determine authorization requirements.
/// If authorization returns deny, throws <see cref="AuthorizationException"/> (mapped to HTTP 403).
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuthorizationProvider _authorizationProvider;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

    public AuthorizationBehavior(
        IAuthorizationProvider authorizationProvider,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    {
        _authorizationProvider = authorizationProvider;
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
                typeof(TRequest).Name,
                cancellationToken);

            return await next();
        }

        // Path 2: Request class has [AuthorizeResource] attribute
        var attribute = typeof(TRequest).GetCustomAttribute<AuthorizeResourceAttribute>();
        if (attribute is not null)
        {
            // If request also implements ISecureRequest, pull dynamic resource context from the instance
            var resourceId = (request is ISecureRequest secureRequest && secureRequest.ResourceId is not null)
                ? secureRequest.ResourceId
                : typeof(TRequest).Name;

            var resourceAttributes = (request is ISecureRequest sr)
                ? sr.ResourceAttributes
                : null;

            await EnforceAuthorizationAsync(
                attribute.Resource,
                resourceId,
                attribute.Action,
                resourceAttributes,
                typeof(TRequest).Name,
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
        string requestType,
        CancellationToken cancellationToken)
    {
        var correlationId = Activity.Current?.Id ?? string.Empty;

        var isAllowed = await _authorizationProvider.IsAllowedAsync(
            resourceKind,
            resourceId,
            action,
            resourceAttributes,
            cancellationToken);

        if (!isAllowed)
        {
            _logger.LogWarning(
                "Authorization decision: {Decision} request={RequestType} resource={Resource}/{ResourceId} action={Action} correlationId={CorrelationId}",
                "deny", requestType, resourceKind, resourceId, action, correlationId);

            throw new AuthorizationException(resourceKind, action);
        }

        _logger.LogInformation(
            "Authorization decision: {Decision} request={RequestType} resource={Resource}/{ResourceId} action={Action} correlationId={CorrelationId}",
            "allow", requestType, resourceKind, resourceId, action, correlationId);
    }
}
