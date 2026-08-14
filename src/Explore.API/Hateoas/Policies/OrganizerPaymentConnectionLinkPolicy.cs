// ABOUTME: HAL policy for private event organizer payment connection management resources.
// ABOUTME: Exposes self and onboarding only through exact paid-commerce event authorization.

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.OrganizerPaymentConnections;
using Explore.Application.Hateoas;

namespace Explore.API.Hateoas.Policies;

public sealed class OrganizerPaymentConnectionLinkPolicy : ILinkPolicy<EventOrganizerPaymentConnectionManagementDto>
{
    public IEnumerable<LinkDefinition> GetLinks(EventOrganizerPaymentConnectionManagementDto dto, ClaimsPrincipal? user)
    {
        yield return LinkDefinition.Self(RouteNames.GetEventOrganizerPaymentConnection, new { eventId = dto.EventId });
        yield return PaidCommerce(new LinkDefinition(
            LinkRelations.StartOnboarding,
            RouteNames.StartEventOrganizerPaymentOnboarding,
            new { eventId = dto.EventId },
            HttpMethods.Post,
            "Start organizer payment onboarding",
            RequiresAuth: true), dto);
    }

    private static LinkDefinition PaidCommerce(LinkDefinition link, EventOrganizerPaymentConnectionManagementDto dto) =>
        link.RequirePermission(
            AuthorizationActions.Events.ManagePaidEventCommerce,
            ResourceKinds.Event,
            dto.EventId.ToString("D"),
            BuildEventAttributes(dto),
            new AuthorizationScope(TenantId: dto.TenantId.ToString("D")));

    private static Dictionary<string, object> BuildEventAttributes(EventOrganizerPaymentConnectionManagementDto dto)
    {
        var attributes = new Dictionary<string, object>
        {
            ["eventId"] = dto.EventId.ToString("D"),
            ["tenantId"] = dto.TenantId.ToString("D"),
            ["actorId"] = dto.ActorId.ToString("D"),
            ["organizerActorId"] = dto.OrganizerActorId.ToString("D")
        };
        AddIfPresent(attributes, "userId", dto.ActorUserId);
        AddIfPresent(attributes, "organizationId", dto.ActorOrganizationId);
        AddIfPresent(attributes, "groupId", dto.ActorGroupId);
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

public sealed class OrganizerPaymentConnectionCollectionLinkPolicy : ICollectionLinkPolicy<EventOrganizerPaymentConnectionManagementDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(EventOrganizerPaymentConnectionManagementDto dto, ClaimsPrincipal? user) => [];
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user) => [];
}
