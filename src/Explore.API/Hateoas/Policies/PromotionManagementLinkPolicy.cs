// ABOUTME: HAL policy for event-scoped organizer promotion management resources.
// ABOUTME: Uses paid-commerce event authority and promotion state to expose only valid server actions.

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.Features.Promotions;
using Explore.Application.Hateoas;
using Explore.Domain.Enums;

namespace Explore.API.Hateoas.Policies;

public sealed class PromotionManagementLinkPolicy : ILinkPolicy<PromotionManagementDto>
{
    public IEnumerable<LinkDefinition> GetLinks(PromotionManagementDto dto, ClaimsPrincipal? user)
    {
        yield return PaidCommerce(LinkDefinition.Self(
            RouteNames.GetEventPromotion,
            new { eventId = dto.EventId, promotionDefinitionId = dto.DefinitionId }), dto);

        yield return PaidCommerce(new LinkDefinition(
            LinkRelations.Collection,
            RouteNames.GetEventPromotions,
            new { eventId = dto.EventId, ticketCatalogVersionId = dto.TicketCatalogVersionId },
            HttpMethods.Get,
            "Event promotions",
            RequiresAuth: true), dto);

        if (dto.StatusId == (int)PromotionDefinitionStatusEnum.Draft)
        {
            yield return PaidCommerce(new LinkDefinition(
                LinkRelations.Publish,
                RouteNames.PublishEventPromotion,
                new { eventId = dto.EventId, promotionDefinitionId = dto.DefinitionId },
                HttpMethods.Post,
                "Publish promotion",
                RequiresAuth: true), dto);
        }

        if (dto.StatusId == (int)PromotionDefinitionStatusEnum.Published)
        {
            yield return PaidCommerce(new LinkDefinition(
                LinkRelations.RevisePromotion,
                RouteNames.ReviseEventPromotion,
                new { eventId = dto.EventId, promotionDefinitionId = dto.DefinitionId },
                HttpMethods.Put,
                "Revise promotion",
                RequiresAuth: true), dto);
            yield return PaidCommerce(new LinkDefinition(
                LinkRelations.Revoke,
                RouteNames.RevokeEventPromotion,
                new { eventId = dto.EventId, promotionDefinitionId = dto.DefinitionId },
                HttpMethods.Post,
                "Revoke promotion",
                RequiresAuth: true), dto);
            yield return PaidCommerce(new LinkDefinition(
                LinkRelations.RotatePromotionCode,
                RouteNames.RotateEventPromotionCode,
                new { eventId = dto.EventId, promotionDefinitionId = dto.DefinitionId },
                HttpMethods.Post,
                "Rotate promotion code",
                RequiresAuth: true), dto);
        }
    }

    internal static LinkDefinition PaidCommerce(LinkDefinition link, PromotionManagementDto dto) =>
        link.RequirePermission(
            AuthorizationActions.Events.ManagePaidEventCommerce,
            ResourceKinds.Event,
            dto.EventId.ToString("D"),
            BuildEventAttributes(dto),
            new AuthorizationScope(TenantId: dto.TenantId.ToString("D")));

    private static Dictionary<string, object> BuildEventAttributes(PromotionManagementDto dto)
    {
        var attributes = new Dictionary<string, object>
        {
            ["eventId"] = dto.EventId.ToString("D"),
            ["tenantId"] = dto.TenantId.ToString("D")
        };
        AddIfPresent(attributes, "actorId", dto.ActorId);
        AddIfPresent(attributes, "userId", dto.ActorUserId);
        AddIfPresent(attributes, "organizationId", dto.ActorOrganizationId);
        AddIfPresent(attributes, "groupId", dto.ActorGroupId);
        AddIfPresent(attributes, "organizerActorId", dto.OrganizerActorId);
        AddIfPresent(attributes, "organizerUserId", dto.OrganizerUserId);
        AddIfPresent(attributes, "organizerOrganizationId", dto.OrganizerOrganizationId);
        AddIfPresent(attributes, "organizerGroupId", dto.OrganizerGroupId);
        return attributes;
    }

    private static void AddIfPresent(Dictionary<string, object> attributes, string key, Guid? value)
    {
        if (value.HasValue)
        {
            attributes[key] = value.Value.ToString("D");
        }
    }
}

public sealed class PromotionManagementCollectionLinkPolicy : ICollectionLinkPolicy<PromotionManagementDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(PromotionManagementDto dto, ClaimsPrincipal? user) =>
        new PromotionManagementLinkPolicy().GetLinks(dto, user);

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        yield break;
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(
        ClaimsPrincipal? user,
        ICollectionAuthorizationContext? authorizationContext)
    {
        if (authorizationContext is not PromotionCollectionAuthorizationContext context)
        {
            yield break;
        }

        yield return new LinkDefinition(
                LinkRelations.CreatePromotion,
                RouteNames.CreateEventPromotionDraft,
                context,
                HttpMethods.Post,
                "Create promotion",
                RequiresAuth: true)
            .RequirePermission(
                AuthorizationActions.Events.ManagePaidEventCommerce,
                ResourceKinds.Event,
                context.AuthorizationResourceId,
                context.AuthorizationResourceAttributes,
                new AuthorizationScope(TenantId: context.TenantId.ToString("D")));
    }
}

public sealed record PromotionCollectionAuthorizationContext(
    Guid EventId,
    Guid TicketCatalogVersionId,
    Guid TenantId) : ICollectionAuthorizationContext
{
    public string AuthorizationResourceId => EventId.ToString("D");

    public IReadOnlyDictionary<string, object> AuthorizationResourceAttributes =>
        new Dictionary<string, object>
        {
            ["eventId"] = EventId.ToString("D"),
            ["ticketCatalogVersionId"] = TicketCatalogVersionId.ToString("D"),
            ["tenantId"] = TenantId.ToString("D")
        };
}
