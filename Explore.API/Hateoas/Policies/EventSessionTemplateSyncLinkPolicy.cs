// ABOUTME: HATEOAS link policy for event-session template sync affordances.
// ABOUTME: Emits diff, apply, and history links from an API-layer sync resource descriptor using named routes.

using System.Security.Claims;
using Explore.API.Hateoas.Resources;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.Hateoas;

namespace Explore.API.Hateoas.Policies;

public sealed class EventSessionTemplateSyncLinkPolicy : ILinkPolicy<EventSessionTemplateSyncResource>
{
    public IEnumerable<LinkDefinition> GetLinks(EventSessionTemplateSyncResource dto, ClaimsPrincipal? user)
    {
        var attributes = new Dictionary<string, object>
        {
            ["sessionId"] = dto.SessionId,
            ["templateVersion"] = dto.TargetTemplateVersion
        };

        yield return new LinkDefinition(
            "sync-diff",
            RouteNames.GetEventSessionTemplateSyncDiff,
            new { sessionId = dto.SessionId, templateVersion = dto.TargetTemplateVersion },
            HttpMethods.Get,
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.CustomPropertyTemplates.SyncDiff,
                ResourceKinds.CustomPropertyTemplate,
                dto.SessionId.ToString(),
                attributes);

        if (dto.HasChanges)
        {
            yield return new LinkDefinition(
                "sync-apply",
                RouteNames.ApplyEventSessionTemplateSync,
                new { sessionId = dto.SessionId },
                HttpMethods.Post,
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.CustomPropertyTemplates.SyncApply,
                    ResourceKinds.CustomPropertyTemplate,
                    dto.SessionId.ToString(),
                    attributes);
        }

        yield return new LinkDefinition(
            "sync-history",
            RouteNames.GetEventSessionTemplateSyncHistory,
            new { sessionId = dto.SessionId },
            HttpMethods.Get,
            RequiresAuth: true);
    }
}
