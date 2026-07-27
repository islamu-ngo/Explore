// ABOUTME: MediatR pipeline behavior that enforces authorization before command execution.
// ABOUTME: Primary path: [AuthorizeResource] + optional ISecureRequest. Legacy path: IAuthorizedRequest (deprecated, zero production usages).

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventCategories.Requests.Commands;
using Explore.Application.Features.EventOrganizerClaims.Requests.Commands;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Commands;
using Explore.Application.Features.EventSessionGroups.Requests.Commands;
using Explore.Application.Features.EventSessionLanguages.Requests.Commands;
using Explore.Application.Features.EventSessionSpeakers.Requests.Commands;
using Explore.Application.Features.EventTags.Requests.Commands;
using Explore.Domain;
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
    private readonly IOrganizationMemberRepository? _organizationMemberRepository;
    private readonly IStorageObjectRepository? _storageObjectRepository;
    private readonly IEventSessionRepository? _eventSessionRepository;
    private readonly IWebhookOwnershipScopeResolver? _webhookOwnershipScopeResolver;
    private readonly IEventRegistrationRepository? _eventRegistrationRepository;
    private readonly IEventSessionLanguageRepository? _eventSessionLanguageRepository;
    private readonly IEventCategoriesRepository? _eventCategoriesRepository;
    private readonly IEventTagsRepository? _eventTagsRepository;
    private readonly IEventOrganizerClaimRepository? _eventOrganizerClaimRepository;
    private readonly IActorRepository? _actorRepository;
    private readonly IEventSessionAgendaItemRepository? _eventSessionAgendaItemRepository;
    private readonly IEventSessionGroupRepository? _eventSessionGroupRepository;
    private readonly IEventSessionSpeakerRepository? _eventSessionSpeakerRepository;
    private readonly ITenantContext? _tenantContext;

    public AuthorizationBehavior(
        IAuthorizationProvider authorizationProvider,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger,
        IEventRepository? eventRepository = null,
        IOrganizationMemberRepository? organizationMemberRepository = null,
        IStorageObjectRepository? storageObjectRepository = null,
        IEventSessionRepository? eventSessionRepository = null,
        IWebhookOwnershipScopeResolver? webhookOwnershipScopeResolver = null,
        IEventRegistrationRepository? eventRegistrationRepository = null,
        ITenantContext? tenantContext = null,
        IEventSessionLanguageRepository? eventSessionLanguageRepository = null,
        IEventCategoriesRepository? eventCategoriesRepository = null,
        IEventTagsRepository? eventTagsRepository = null,
        IEventOrganizerClaimRepository? eventOrganizerClaimRepository = null,
        IActorRepository? actorRepository = null,
        IEventSessionAgendaItemRepository? eventSessionAgendaItemRepository = null,
        IEventSessionGroupRepository? eventSessionGroupRepository = null,
        IEventSessionSpeakerRepository? eventSessionSpeakerRepository = null)
    {
        _authorizationProvider = authorizationProvider;
        _logger = logger;
        _eventRepository = eventRepository;
        _organizationMemberRepository = organizationMemberRepository;
        _storageObjectRepository = storageObjectRepository;
        _eventSessionRepository = eventSessionRepository;
        _webhookOwnershipScopeResolver = webhookOwnershipScopeResolver;
        _eventRegistrationRepository = eventRegistrationRepository;
        _tenantContext = tenantContext;
        _eventSessionLanguageRepository = eventSessionLanguageRepository;
        _eventCategoriesRepository = eventCategoriesRepository;
        _eventTagsRepository = eventTagsRepository;
        _eventOrganizerClaimRepository = eventOrganizerClaimRepository;
        _actorRepository = actorRepository;
        _eventSessionAgendaItemRepository = eventSessionAgendaItemRepository;
        _eventSessionGroupRepository = eventSessionGroupRepository;
        _eventSessionSpeakerRepository = eventSessionSpeakerRepository;
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
            if (request is UpdateEventSessionLanguageCommand languageRequest)
            {
                var assignment = _eventSessionLanguageRepository is null
                    ? null
                    : await _eventSessionLanguageRepository.GetById(languageRequest.EventSessionLanguageId);
                if (assignment is null)
                {
                    throw new AuthorizationException(attribute.Resource, attribute.Action);
                }

                languageRequest.EventSessionId = assignment.EventSessionId;
                resourceId = assignment.EventSessionId.ToString();
            }
            else if (request is UpdateEventCategoriesCommand categoryRequest)
            {
                var assignment = _eventCategoriesRepository is null
                    ? null
                    : await _eventCategoriesRepository.GetById(categoryRequest.EventCategoryId);
                if (assignment is null)
                {
                    throw new AuthorizationException(attribute.Resource, attribute.Action);
                }

                categoryRequest.EventId = assignment.EventId;
                categoryRequest.TenantId = assignment.TenantId;
                resourceId = assignment.EventId.ToString();
            }
            else if (request is UpdateEventTagsCommand tagRequest)
            {
                var assignment = _eventTagsRepository is null
                    ? null
                    : await _eventTagsRepository.GetById(tagRequest.EventTagId);
                if (assignment is null)
                {
                    throw new AuthorizationException(attribute.Resource, attribute.Action);
                }

                tagRequest.EventId = assignment.EventId;
                tagRequest.TenantId = assignment.TenantId;
                resourceId = assignment.EventId.ToString();
            }
            else if (request is UpdateEventSessionAgendaItemCommand agendaItemRequest)
            {
                var assignmentId = agendaItemRequest.EventSessionAgendaItemId;
                var assignment = _eventSessionAgendaItemRepository is null
                    ? null
                    : await _eventSessionAgendaItemRepository.GetByIdWithDetails(
                        assignmentId,
                        cancellationToken);
                if (assignment?.EventSession is null ||
                    assignment.EventSession.TenantId != assignment.TenantId ||
                    (_tenantContext is not null && assignment.TenantId != _tenantContext.TenantId))
                {
                    throw new AuthorizationException(attribute.Resource, attribute.Action);
                }

                agendaItemRequest.EventSessionAgendaItemId = assignment.Id;
                agendaItemRequest.EventSessionId = assignment.EventSessionId;
                agendaItemRequest.EventId = assignment.EventSession.EventId;
                agendaItemRequest.TenantId = assignment.TenantId;
                resourceId = assignment.Id.ToString();
                resourceAttributes = new Dictionary<string, object>
                {
                    ["eventSessionId"] = assignment.EventSessionId.ToString(),
                    ["eventId"] = assignment.EventSession.EventId.ToString(),
                    ["tenantId"] = assignment.TenantId.ToString()
                };
            }
            else if (request is UpdateEventSessionGroupCommand groupRequest)
            {
                var assignmentId = groupRequest.EventSessionGroupId;
                var assignment = _eventSessionGroupRepository is null
                    ? null
                    : await _eventSessionGroupRepository.GetForUpdateAsync(
                        assignmentId,
                        cancellationToken);
                if (assignment is null ||
                    (_tenantContext is not null && assignment.TenantId != _tenantContext.TenantId))
                {
                    throw new AuthorizationException(attribute.Resource, attribute.Action);
                }

                groupRequest.EventSessionGroupId = assignment.Id;
                groupRequest.EventId = assignment.EventId;
                groupRequest.TenantId = assignment.TenantId;
                resourceId = assignment.Id.ToString();
                resourceAttributes = new Dictionary<string, object>
                {
                    ["eventId"] = assignment.EventId.ToString(),
                    ["tenantId"] = assignment.TenantId.ToString()
                };
            }
            else if (request is UpdateEventSessionSpeakerCommand speakerRequest)
            {
                var assignment = _eventSessionSpeakerRepository is null
                    ? null
                    : await _eventSessionSpeakerRepository.GetById(speakerRequest.EventSessionSpeakerId);
                var session = assignment is null || _eventSessionRepository is null
                    ? null
                    : await _eventSessionRepository.GetSessionWithDetails(assignment.EventSessionId);
                if (assignment is null ||
                    session is null ||
                    assignment.TenantId != session.TenantId ||
                    (_tenantContext is not null && assignment.TenantId != _tenantContext.TenantId))
                {
                    throw new AuthorizationException(attribute.Resource, attribute.Action);
                }

                speakerRequest.EventSessionId = session.Id;
                speakerRequest.EventId = session.EventId;
                speakerRequest.TenantId = session.TenantId;
                resourceId = session.Id.ToString();
                resourceAttributes = new Dictionary<string, object>
                {
                    ["eventSessionId"] = session.Id.ToString(),
                    ["eventId"] = session.EventId.ToString(),
                    ["tenantId"] = session.TenantId.ToString()
                };
            }

            if (request is WithdrawEventOrganizerClaimCommand withdrawRequest)
            {
                resourceId = withdrawRequest.ClaimId.ToString("D");
                resourceAttributes = await ResolvePersistedOrganizerClaimOwnershipAsync(
                    withdrawRequest.ClaimId,
                    cancellationToken);
                if (resourceAttributes is null)
                {
                    throw new AuthorizationException(attribute.Resource, attribute.Action);
                }
            }
            else if (attribute.Resource == ResourceKinds.Webhook &&
                request is IWebhookPersistedOwnerRequest persistedOwnerRequest)
            {
                var ownership = await ResolvePersistedWebhookOwnershipAsync(
                    persistedOwnerRequest,
                    cancellationToken);
                resourceId = persistedOwnerRequest.OwnedResourceId.ToString("D");
                resourceAttributes = CreateWebhookOwnerAttributes(ownership);
            }
            else if (attribute.Resource == ResourceKinds.Webhook &&
                request is IWebhookOwnerScopedRequest ownerScopedRequest)
            {
                var ownership = await ResolveWebhookOwnershipAsync(ownerScopedRequest, cancellationToken);
                resourceId = ownership.OwnerId.ToString("D");
                resourceAttributes = CreateWebhookOwnerAttributes(ownership);
            }

            resourceAttributes = await EnrichResourceAttributesAsync(
                attribute.Resource,
                resourceId,
                resourceAttributes,
                cancellationToken);

            if (attribute.Resource == ResourceKinds.EventRegistration && resourceAttributes is null)
                throw new AuthorizationException(attribute.Resource, attribute.Action);

            BindPersistedUserOwner(request, resourceAttributes);

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

    private async Task<WebhookOwnershipScope> ResolveWebhookOwnershipAsync(
        IWebhookOwnerScopedRequest request,
        CancellationToken cancellationToken)
    {
        if (_webhookOwnershipScopeResolver is null)
        {
            throw new AuthorizationException(ResourceKinds.Webhook, "resolve-owner");
        }

        var resolution = await _webhookOwnershipScopeResolver.ResolveAsync(
            request.OwnerKindId,
            request.OwnerId,
            cancellationToken);
        return resolution.Scope ?? throw new AuthorizationException(ResourceKinds.Webhook, "resolve-owner");
    }

    private async Task<WebhookOwnershipScope> ResolvePersistedWebhookOwnershipAsync(
        IWebhookPersistedOwnerRequest request,
        CancellationToken cancellationToken)
    {
        if (_webhookOwnershipScopeResolver is null)
        {
            throw new AuthorizationException(ResourceKinds.Webhook, "resolve-persisted-owner");
        }

        var resolution = await _webhookOwnershipScopeResolver.ResolvePersistedAsync(
            request.OwnedResourceKind,
            request.OwnedResourceId,
            cancellationToken);
        return resolution.Scope ??
            throw new AuthorizationException(ResourceKinds.Webhook, "resolve-persisted-owner");
    }

    private static Dictionary<string, object> CreateWebhookOwnerAttributes(WebhookOwnershipScope ownership)
    {
        var attributes = new Dictionary<string, object>
        {
            ["ownerKindId"] = (int)ownership.Kind,
            ["ownerKind"] = ownership.Kind.ToString().ToUpperInvariant(),
            ["ownerId"] = ownership.OwnerId.ToString("D")
        };

        AddIfMissing(attributes, "tenantId", ownership.TenantId);
        AddIfMissing(attributes, "instanceId", ownership.InstanceId);
        AddIfMissing(attributes, "organizationId", ownership.OrganizationId);
        AddIfMissing(attributes, "groupId", ownership.GroupId);
        AddIfMissing(attributes, "userId", ownership.UserId);
        return attributes;
    }

    private async Task<IDictionary<string, object>?> EnrichResourceAttributesAsync(
        string resourceKind,
        string resourceId,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        return resourceKind switch
        {
            ResourceKinds.Event => await EnrichEventResourceAttributesAsync(resourceId, resourceAttributes, cancellationToken),
            ResourceKinds.EventOrganizerClaim => TryGetGuidAttribute(resourceAttributes, "eventId", out var eventId)
                ? await EnrichEventResourceAttributesAsync(eventId.ToString("D"), resourceAttributes, cancellationToken)
                : resourceAttributes,
            ResourceKinds.EventSession => await EnrichEventSessionResourceAttributesAsync(resourceId, resourceAttributes, cancellationToken),
            ResourceKinds.EventRegistration => await EnrichEventRegistrationResourceAttributesAsync(resourceId, cancellationToken),
            ResourceKinds.OrganizationMember => await EnrichOrganizationMemberResourceAttributesAsync(resourceId, resourceAttributes, cancellationToken),
            ResourceKinds.StorageObject => await EnrichStorageObjectResourceAttributesAsync(resourceId, resourceAttributes, cancellationToken),
            ResourceKinds.CustomPropertyProjection => await EnrichCustomPropertyProjectionResourceAttributesAsync(resourceAttributes, cancellationToken),
            _ => resourceAttributes
        };
    }

    private async Task<IDictionary<string, object>?> ResolvePersistedOrganizerClaimOwnershipAsync(
        Guid claimId,
        CancellationToken cancellationToken)
    {
        if (claimId == Guid.Empty ||
            _eventOrganizerClaimRepository is null ||
            _actorRepository is null ||
            _tenantContext is null)
        {
            return null;
        }

        var claim = await _eventOrganizerClaimRepository.GetDetailsAsync(
            claimId,
            trackChanges: false,
            cancellationToken);
        if (claim is null || claim.TenantId != _tenantContext.TenantId)
        {
            return null;
        }

        var claimantActor = await _actorRepository.GetActorWithDetails(
            claim.ClaimantActorId,
            cancellationToken);
        if (claimantActor is null || claimantActor.Id != claim.ClaimantActorId)
        {
            return null;
        }

        var attributes = new Dictionary<string, object>
        {
            ["tenantId"] = claim.TenantId.ToString("D"),
            ["eventId"] = claim.EventId.ToString("D"),
            ["claimId"] = claim.Id.ToString("D"),
            ["claimantActorId"] = claim.ClaimantActorId.ToString("D"),
            ["status"] = claim.Status?.MasterCode ?? claim.StatusId.ToString()
        };
        AddIfMissing(attributes, "claimantUserId", claimantActor.UserId);
        AddIfMissing(attributes, "claimantOrganizationId", claimantActor.OrganizationId);
        AddIfMissing(attributes, "claimantGroupId", claimantActor.GroupId);
        return attributes;
    }

    private async Task<IDictionary<string, object>?> EnrichEventRegistrationResourceAttributesAsync(
        string resourceId,
        CancellationToken cancellationToken)
    {
        if (_eventRegistrationRepository is null ||
            _eventRepository is null ||
            _tenantContext is null ||
            !Guid.TryParse(resourceId, out var registrationId))
        {
            return null;
        }

        var registration = await _eventRegistrationRepository.GetByIdWithDetails(
            registrationId,
            cancellationToken);
        if (registration is null || registration.TenantId != _tenantContext.TenantId)
            return null;

        var eventEntity = await _eventRepository.GetEventWithDetails(registration.EventId);
        if (eventEntity is null || eventEntity.TenantId != _tenantContext.TenantId)
            return null;

        var attributes = new Dictionary<string, object>
        {
            ["eventId"] = registration.EventId.ToString("D"),
            ["eventSessionId"] = registration.EventSessionId.ToString("D"),
            ["userId"] = registration.UserId.ToString("D"),
            ["tenantId"] = registration.TenantId.ToString("D")
        };
        AddIfMissing(attributes, "organizationId", eventEntity.Actor?.OrganizationId);
        AddIfMissing(attributes, "groupId", eventEntity.Actor?.GroupId);
        return attributes;
    }

    private static void BindPersistedUserOwner(
        TRequest request,
        IDictionary<string, object>? resourceAttributes)
    {
        if (request is IPersistedUserOwnerBoundRequest ownerBoundRequest &&
            resourceAttributes is not null &&
            TryGetGuidAttribute(resourceAttributes, "userId", out var ownerUserId))
        {
            ownerBoundRequest.ExpectedOwnerUserId = ownerUserId;
        }
    }

    private async Task<IDictionary<string, object>?> EnrichCustomPropertyProjectionResourceAttributesAsync(
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (resourceAttributes is null || resourceAttributes.ContainsKey("tenantId"))
            return resourceAttributes;

        if (TryGetGuidAttribute(resourceAttributes, "eventId", out var eventId))
        {
            if (_eventRepository is null)
                return resourceAttributes;

            var eventEntity = await _eventRepository.GetEventWithDetails(eventId);
            if (eventEntity is null)
                return resourceAttributes;

            var enriched = new Dictionary<string, object>(resourceAttributes);
            AddIfMissing(enriched, "eventId", eventEntity.Id.ToString("D"));
            AddIfMissing(enriched, "tenantId", eventEntity.TenantId.ToString("D"));
            return enriched;
        }

        if (TryGetGuidAttribute(resourceAttributes, "eventSessionId", out var eventSessionId))
        {
            if (_eventSessionRepository is null)
                return resourceAttributes;

            var session = await _eventSessionRepository.GetSessionWithDetails(eventSessionId);
            if (session is null)
                return resourceAttributes;

            var enriched = new Dictionary<string, object>(resourceAttributes);
            AddIfMissing(enriched, "eventSessionId", session.Id.ToString("D"));
            AddIfMissing(enriched, "eventId", session.EventId.ToString("D"));
            AddIfMissing(enriched, "tenantId", session.TenantId.ToString("D"));
            return enriched;
        }

        return resourceAttributes;
    }

    private async Task<IDictionary<string, object>?> EnrichEventResourceAttributesAsync(
        string resourceId,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (_eventRepository is null ||
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

    private async Task<IDictionary<string, object>?> EnrichEventSessionResourceAttributesAsync(
        string resourceId,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (_eventSessionRepository is null ||
            !Guid.TryParse(resourceId, out var eventSessionId))
        {
            return resourceAttributes;
        }

        if (HasEventAuthorizationContext(resourceAttributes))
            return resourceAttributes;

        var session = await _eventSessionRepository.GetSessionWithDetails(eventSessionId);
        if (session is null)
            return resourceAttributes;

        var enriched = resourceAttributes is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(resourceAttributes);

        AddIfMissing(enriched, "eventSessionId", session.Id.ToString());
        AddIfMissing(enriched, "eventId", session.EventId.ToString());
        AddIfMissing(enriched, "tenantId", session.TenantId.ToString());

        return enriched;
    }

    private async Task<IDictionary<string, object>?> EnrichStorageObjectResourceAttributesAsync(
        string resourceId,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (_storageObjectRepository is null ||
            IsStorageObjectCollectionScope(resourceAttributes) ||
            !Guid.TryParse(resourceId, out var storageObjectId))
        {
            return resourceAttributes;
        }

        var storageObject = await _storageObjectRepository.GetById(storageObjectId);
        if (storageObject is null)
            return resourceAttributes;

        var enriched = resourceAttributes is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(resourceAttributes);

        enriched["storageObjectId"] = storageObject.Id.ToString("D");
        enriched["tenantId"] = storageObject.TenantId.ToString("D");
        AddIfMissing(enriched, "visibility", storageObject.Visibility);
        AddIfMissing(enriched, "lifecycleState", storageObject.LifecycleState);
        AddIfMissing(enriched, "createdBy", storageObject.CreatedBy);
        AddIfMissing(enriched, "owningResourceKind", storageObject.OwningResourceKind);
        AddIfMissing(enriched, "owningResourceId", storageObject.OwningResourceId);

        return enriched;
    }

    private async Task<IDictionary<string, object>?> EnrichOrganizationMemberResourceAttributesAsync(
        string resourceId,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (_organizationMemberRepository is null ||
            !Guid.TryParse(resourceId, out var memberId))
        {
            return resourceAttributes;
        }

        if (HasOrganizationMemberAuthorizationContext(resourceAttributes))
            return resourceAttributes;

        var member = await _organizationMemberRepository.GetOrganizationMemberWithDetails(memberId);
        if (member is null)
            return resourceAttributes;

        var enriched = resourceAttributes is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(resourceAttributes);

        AddIfMissing(enriched, "memberId", member.Id.ToString());
        AddIfMissing(enriched, "tenantId", member.TenantId.ToString());
        AddIfMissing(enriched, "organizationId", member.OrganizationTenant.OrganizationId.ToString());
        AddIfMissing(enriched, "userId", member.UserId.ToString());

        return enriched;
    }

    private static bool HasEventAuthorizationContext(IDictionary<string, object>? attributes) =>
        attributes?.ContainsKey("eventId") == true &&
        attributes.ContainsKey("tenantId") &&
        (attributes.ContainsKey("organizationId") || attributes.ContainsKey("userId") || attributes.ContainsKey("groupId"));

    private static bool HasOrganizationMemberAuthorizationContext(IDictionary<string, object>? attributes) =>
        attributes?.ContainsKey("tenantId") == true &&
        attributes.ContainsKey("organizationId") &&
        attributes.ContainsKey("userId");

    private static bool IsStorageObjectCollectionScope(IDictionary<string, object>? attributes) =>
        attributes?.TryGetValue("authorizationScope", out var scope) == true &&
        string.Equals(scope?.ToString(), "collection", StringComparison.Ordinal);

    private static bool TryGetGuidAttribute(
        IDictionary<string, object> attributes,
        string attributeName,
        out Guid value)
    {
        value = Guid.Empty;

        if (!attributes.TryGetValue(attributeName, out var attributeValue))
            return false;

        if (attributeValue is Guid guidValue)
        {
            value = guidValue;
            return true;
        }

        return attributeValue is string stringValue && Guid.TryParse(stringValue, out value);
    }

    private static void AddIfMissing(IDictionary<string, object> attributes, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !attributes.ContainsKey(key))
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
