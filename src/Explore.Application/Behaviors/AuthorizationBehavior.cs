// ABOUTME: MediatR pipeline behavior that enforces authorization before command execution.
// ABOUTME: Delegates request-specific resource lookup to closed generic authorization context enrichers.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Explore.Application.Behaviors;
public sealed class AuthorizationBehavior<TRequest, TResponse>(
    IAuthorizationProvider authorizationProvider,
    ILogger<AuthorizationBehavior<TRequest, TResponse>> logger,
    AuthorizationResourceContextResolver? resourceContextResolver = null,
    IAuthorizationContextEnricher<TRequest>? authorizationContextEnricher = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, AuthorizeResourceAttribute?> AttributeCache = new();
    private static readonly ActivitySource AuthorizationActivitySource = new("Explore.Authorization");
    private readonly AuthorizationResourceContextResolver _resourceContextResolver = resourceContextResolver ?? new();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var attribute = AttributeCache.GetOrAdd(typeof(TRequest), static t => t.GetCustomAttribute<AuthorizeResourceAttribute>());
        if (attribute is null)
        {
            return await next(cancellationToken);
        }
        var resourceId = request is ISecureRequest secureRequest && secureRequest.ResourceId is not null
            ? secureRequest.ResourceId
            : typeof(TRequest).Name;
        var resourceAttributes = request is ISecureRequest secureRequestWithAttributes
            ? secureRequestWithAttributes.ResourceAttributes
            : null;
        IAuthorizationFacts? facts = null;
        if (authorizationContextEnricher is not null)
        {
            var context = await authorizationContextEnricher.ResolveAsync(request, cancellationToken);
            resourceId = context.ResourceId ?? resourceId;
            resourceAttributes = context.Attributes ?? resourceAttributes;
            facts = context.Facts;
        }

        var resolvedContext = await _resourceContextResolver.ResolveAsync(
            request,
            attribute.Resource,
            attribute.Action,
            resourceId,
            resourceAttributes,
            cancellationToken);

        var factsForProvider = resolvedContext.Facts ?? facts;
        await EnforceAuthorizationAsync(
            attribute.Resource,
            resolvedContext.ResourceId ?? resourceId,
            attribute.Action,
            factsForProvider is null ? resolvedContext.Attributes : null,
            factsForProvider,
            typeof(TRequest).Name,
            cancellationToken);
        return await next(cancellationToken);
    }
    private async Task EnforceAuthorizationAsync(
        string resourceKind, string resourceId, string action, IDictionary<string, object>? resourceAttributes, IAuthorizationFacts? facts, string requestType, CancellationToken cancellationToken)
    {
        using var activity = AuthorizationActivitySource.StartActivity("authorization.evaluate");
        activity?.SetTag("resource.kind", resourceKind);
        activity?.SetTag("resource.action", action);
        activity?.SetTag("request.type", requestType);
        var correlationId = Activity.Current?.Id ?? string.Empty;
        var decision = await authorizationProvider.AuthorizeAsync(
            new AuthorizationRequest(
                AuthorizationCapabilityCatalog.Require(resourceKind, action),
                resourceId,
                resourceAttributes is null ? null : new Dictionary<string, object>(resourceAttributes),
                Facts: facts),
            cancellationToken);
        if (!decision.IsAllowed)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Authorization denied");
            logger.LogWarning(
                "Authorization decision: {Decision} request={RequestType} resource={Resource}/{ResourceId} action={Action} correlationId={CorrelationId}",
                "deny", requestType, resourceKind, resourceId, action, correlationId);
            throw new AuthorizationException(resourceKind, action);
        }
        logger.LogDebug(
            "Authorization decision: {Decision} request={RequestType} resource={Resource}/{ResourceId} action={Action} correlationId={CorrelationId}",
            "allow", requestType, resourceKind, resourceId, action, correlationId);
    }
}
