// ABOUTME: Defines EventLocation management HAL candidates from the active route context.
// ABOUTME: Binds disclosure edits to the direct mutation's parent-event authorization metadata.

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.Location;
using Explore.Application.Hateoas;

namespace Explore.API.Hateoas.Policies;

public sealed class EventLocationManagementLinkPolicy(IHttpContextAccessor httpContextAccessor)
    : ILinkPolicy<EventLocationManagementDto>
{
    public IEnumerable<LinkDefinition> GetLinks(
        EventLocationManagementDto dto,
        ClaimsPrincipal? user)
    {
        if (dto.EventLocationId == Guid.Empty
            || !TryGetEventId(out Guid eventId)
            || !TryGetUpdateAuthorization(dto, eventId, out AuthorizationCheck authorization))
        {
            yield break;
        }

        yield return new LinkDefinition(
            LinkRelations.Edit,
            RouteNames.UpdateEventLocationDisclosure,
            new { eventId, eventLocationId = dto.EventLocationId },
            HttpMethods.Put,
            "Update location disclosure",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Update,
                ResourceKinds.Event,
                eventId.ToString("D"),
                authorization.ResourceAttributes,
                authorization.Scope);
    }

    private bool TryGetEventId(out Guid eventId)
    {
        object? routeValue = httpContextAccessor.HttpContext?.Request.RouteValues["eventId"];
        if (routeValue is Guid guid && guid != Guid.Empty)
        {
            eventId = guid;
            return true;
        }

        eventId = Guid.Empty;
        return routeValue is string value
            && Guid.TryParse(value, out eventId)
            && eventId != Guid.Empty;
    }

    private static bool TryGetUpdateAuthorization(
        EventLocationManagementDto dto,
        Guid eventId,
        out AuthorizationCheck authorization)
    {
        authorization = dto.UpdateAuthorization!;
        if (authorization is null
            || authorization.ResourceKind != ResourceKinds.Event
            || authorization.Action != AuthorizationActions.Update
            || !Guid.TryParse(authorization.ResourceId, out Guid authorizationEventId)
            || authorizationEventId != eventId
            || !TryGetAttributeGuid(authorization.ResourceAttributes, "eventId", out Guid attributeEventId)
            || attributeEventId != eventId
            || !TryGetAttributeGuid(authorization.ResourceAttributes, "tenantId", out Guid tenantId)
            || !TryGetAttributeGuid(authorization.ResourceAttributes, "actorId", out _)
            || !Guid.TryParse(authorization.Scope?.TenantId, out Guid scopeTenantId)
            || scopeTenantId != tenantId)
        {
            authorization = null!;
            return false;
        }

        return true;
    }

    private static bool TryGetAttributeGuid(
        IReadOnlyDictionary<string, object>? attributes,
        string name,
        out Guid value)
    {
        value = Guid.Empty;
        return attributes?.TryGetValue(name, out object? attribute) == true
            && Guid.TryParse(attribute?.ToString(), out value)
            && value != Guid.Empty;
    }
}

public sealed class EventLocationManagementCollectionLinkPolicy
    : ICollectionLinkPolicy<EventLocationManagementDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(
        EventLocationManagementDto dto,
        ClaimsPrincipal? user) => [];

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user) => [];
}
