// ABOUTME: MediatR pipeline behavior that enforces authorization before command execution.
// ABOUTME: Primary path: [AuthorizeResource] + optional ISecureRequest. Legacy path: IAuthorizedRequest (deprecated, zero production usages).

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
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
    private static readonly ConcurrentDictionary<Type, AuthorizeResourceAttribute?> AttributeCache = new();
    private static readonly ActivitySource AuthorizationActivitySource = new("Explore.Authorization");

    private readonly IAuthorizationProvider _authorizationProvider;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;
    private readonly IEventRepository? _eventRepository;

    public AuthorizationBehavior(
        IAuthorizationProvider authorizationProvider,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger,
        IEventRepository? eventRepository = null)
    {
        _authorizationProvider = authorizationProvider;
        _logger = logger;
        _eventRepository = eventRepository;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Path 1 (Legacy): Request implements IAuthorizedRequest directly — deprecated, zero production usages.
        // Retained for backward compatibility. New commands should use [AuthorizeResource] attribute instead.
#pragma warning disable CS0618 // IAuthorizedRequest is obsolete — bridge code must still support it
        if (request is IAuthorizedRequest authorizedRequest)
        {
            await EnforceAuthorizationAsync(
                authorizedRequest.ResourceKind,
                authorizedRequest.ResourceId,
                authorizedRequest.Action,
                authorizedRequest.ResourceAttributes,
                typeof(TRequest).Name,
                cancellationToken);

            return await next(cancellationToken);
        }
#pragma warning restore CS0618

        // Path 2: Request class has [AuthorizeResource] attribute (cached per type)
        var attribute = AttributeCache.GetOrAdd(
            typeof(TRequest),
            static t => t.GetCustomAttribute<AuthorizeResourceAttribute>());
        if (attribute is not null)
        {
            // If request also implements ISecureRequest, pull dynamic resource context from the instance
            var resourceId = (request is ISecureRequest secureRequest && secureRequest.ResourceId is not null)
                ? secureRequest.ResourceId
                : typeof(TRequest).Name;

            var resourceAttributes = (request is ISecureRequest sr)
                ? sr.ResourceAttributes
                : null;
            resourceAttributes = await EnrichResourceAttributesAsync(
                attribute.Resource,
                resourceId,
                resourceAttributes,
                cancellationToken);

            await EnforceAuthorizationAsync(
                attribute.Resource,
                resourceId,
                attribute.Action,
                resourceAttributes,
                typeof(TRequest).Name,
                cancellationToken);

            return await next(cancellationToken);
        }

        // No authorization requirements — pass through
        return await next(cancellationToken);
    }

    private async Task<IDictionary<string, object>?> EnrichResourceAttributesAsync(
        string resourceKind,
        string resourceId,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (_eventRepository is null ||
            resourceKind != ResourceKinds.Event ||
            !Guid.TryParse(resourceId, out var eventId))
        {
            return resourceAttributes;
        }

        if (HasEventAuthorizationContext(resourceAttributes))
            return resourceAttributes;

        var eventEntity = await _eventRepository.GetEventWithDetails(eventId);
        if (eventEntity is null)
            return resourceAttributes;

        var enriched = resourceAttributes is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(resourceAttributes);

        AddIfMissing(enriched, "eventId", eventEntity.Id.ToString());
        AddIfMissing(enriched, "tenantId", eventEntity.TenantId.ToString());
        AddIfMissing(enriched, "actorId", eventEntity.ActorId.ToString());
        AddIfMissing(enriched, "userId", eventEntity.Actor?.UserId);
        AddIfMissing(enriched, "organizationId", eventEntity.Actor?.OrganizationId);
        AddIfMissing(enriched, "groupId", eventEntity.Actor?.GroupId);

        return enriched;
    }

    private static bool HasEventAuthorizationContext(IDictionary<string, object>? attributes) =>
        attributes?.ContainsKey("eventId") == true &&
        attributes.ContainsKey("tenantId") &&
        (attributes.ContainsKey("organizationId") || attributes.ContainsKey("userId") || attributes.ContainsKey("groupId"));

    private static void AddIfMissing(IDictionary<string, object> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
            attributes[key] = value;
    }

    private static void AddIfMissing(IDictionary<string, object> attributes, string key, Guid? value)
    {
        if (value.HasValue && value.Value != Guid.Empty && !attributes.ContainsKey(key))
            attributes[key] = value.Value.ToString();
    }

    private async Task EnforceAuthorizationAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        string requestType,
        CancellationToken cancellationToken)
    {
        using var activity = AuthorizationActivitySource.StartActivity("authorization.evaluate");
        activity?.SetTag("resource.kind", resourceKind);
        activity?.SetTag("resource.action", action);
        activity?.SetTag("request.type", requestType);

        var correlationId = Activity.Current?.Id ?? string.Empty;

        var isAllowed = await _authorizationProvider.IsAllowedAsync(
            resourceKind,
            resourceId,
            action,
            resourceAttributes,
            cancellationToken);

        if (!isAllowed)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Authorization denied");

            _logger.LogWarning(
                "Authorization decision: {Decision} request={RequestType} resource={Resource}/{ResourceId} action={Action} correlationId={CorrelationId}",
                "deny", requestType, resourceKind, resourceId, action, correlationId);

            throw new AuthorizationException(resourceKind, action);
        }

        _logger.LogDebug(
            "Authorization decision: {Decision} request={RequestType} resource={Resource}/{ResourceId} action={Action} correlationId={CorrelationId}",
            "allow", requestType, resourceKind, resourceId, action, correlationId);
    }
}
