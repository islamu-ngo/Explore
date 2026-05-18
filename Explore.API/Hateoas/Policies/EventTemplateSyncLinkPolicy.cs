// ABOUTME: HATEOAS link policy for event template sync affordances.
// ABOUTME: Emits diff, apply, and history links from an API-layer sync resource descriptor using named routes.

using System.Security.Claims;
using Explore.API.Hateoas.Resources;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.Hateoas;

namespace Explore.API.Hateoas.Policies;

public sealed class EventTemplateSyncLinkPolicy : ILinkPolicy<EventTemplateSyncResource>
{
    public IEnumerable<LinkDefinition> GetLinks(EventTemplateSyncResource dto, ClaimsPrincipal? user)
    {
        var attributes = new Dictionary<string, object>
        {
            ["eventId"] = dto.EventId,
            ["templateVersion"] = dto.TargetTemplateVersion
        };

        yield return new LinkDefinition(
            "sync-diff",
            RouteNames.GetEventTemplateSyncDiff,
            new { eventId = dto.EventId, templateVersion = dto.TargetTemplateVersion },
            HttpMethods.Get,
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.CustomPropertyTemplates.SyncDiff,
                ResourceKinds.CustomPropertyTemplate,
                dto.EventId.ToString(),
                attributes);

        if (dto.HasChanges)
        {
            yield return new LinkDefinition(
                "sync-apply",
                RouteNames.ApplyEventTemplateSync,
                new { eventId = dto.EventId },
                HttpMethods.Post,
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.CustomPropertyTemplates.SyncApply,
                    ResourceKinds.CustomPropertyTemplate,
                    dto.EventId.ToString(),
                    attributes);
        }

        yield return new LinkDefinition(
            "sync-history",
            RouteNames.GetEventTemplateSyncHistory,
            new { eventId = dto.EventId },
            HttpMethods.Get,
            RequiresAuth: true);
    }
}
