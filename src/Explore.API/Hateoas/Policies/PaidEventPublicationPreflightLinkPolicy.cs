// ABOUTME: HAL policy for event-scoped paid publication preflight resources.
// ABOUTME: Emits publish only when readiness says the catalog can be safely published.

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventTicketing;
using Explore.Application.Hateoas;

namespace Explore.API.Hateoas.Policies;

public sealed class PaidEventPublicationPreflightLinkPolicy : ILinkPolicy<PaidEventPublicationPreflightDto>
{
    public IEnumerable<LinkDefinition> GetLinks(PaidEventPublicationPreflightDto dto, ClaimsPrincipal? user)
    {
        yield return LinkDefinition.Self(RouteNames.GetPaidEventPublicationPreflight, new { eventId = dto.EventId });
        if (!dto.IsReady)
        {
            yield break;
        }

        LinkDefinition publish = new(
            LinkRelations.Publish,
            RouteNames.PublishEventTicketCatalog,
            new { eventId = dto.EventId },
            HttpMethods.Post,
            "Publish ticket catalog",
            RequiresAuth: true);
        yield return dto.IsPaidCatalog
            ? publish.RequirePermission(AuthorizationActions.Events.ManagePaidEventCommerce, ResourceKinds.Event, dto.EventId.ToString("D"), BuildEventAttributes(dto), BuildScope(dto))
            : publish.RequirePermission(AuthorizationActions.Events.ManageTickets, ResourceKinds.Event, dto.EventId.ToString("D"), BuildEventAttributes(dto), BuildScope(dto));
    }

    private static Dictionary<string, object> BuildEventAttributes(PaidEventPublicationPreflightDto dto)
    {
        var attributes = new Dictionary<string, object>
        {
            ["eventId"] = dto.EventId.ToString("D"),
            ["tenantId"] = dto.TenantId.ToString("D"),
            ["actorId"] = dto.ActorId.ToString("D")
        };

        AddIfPresent(attributes, "userId", dto.ActorUserId);
        AddIfPresent(attributes, "organizationId", dto.ActorOrganizationId);
        AddIfPresent(attributes, "groupId", dto.ActorGroupId);
        AddIfPresent(attributes, "organizerActorId", dto.OrganizerActorId);
        AddIfPresent(attributes, "organizerUserId", dto.OrganizerUserId);
        AddIfPresent(attributes, "organizerOrganizationId", dto.OrganizerOrganizationId);
        AddIfPresent(attributes, "organizerGroupId", dto.OrganizerGroupId);
        return attributes;
    }

    private static AuthorizationScope BuildScope(PaidEventPublicationPreflightDto dto) => new(TenantId: dto.TenantId.ToString("D"));

    private static void AddIfPresent(Dictionary<string, object> attributes, string key, Guid? value)
    {
        if (value.HasValue)
        {
            attributes[key] = value.Value.ToString("D");
        }
    }
}

public sealed class PaidEventPublicationPreflightCollectionLinkPolicy : ICollectionLinkPolicy<PaidEventPublicationPreflightDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(PaidEventPublicationPreflightDto dto, ClaimsPrincipal? user) => [];
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user) => [];
}
