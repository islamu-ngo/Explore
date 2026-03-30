// ABOUTME: HATEOAS link policies for EventTemplate detail and collection views.
// ABOUTME: Controls which links appear based on resource state and user permissions.

namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventTemplate;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for EventTemplateDto (detail view).
/// Determines which links should be included based on resource state and user authorization.
/// </summary>
public sealed class EventTemplateDetailLinkPolicy : ILinkPolicy<EventTemplateDto>
{
    public IEnumerable<LinkDefinition> GetLinks(EventTemplateDto dto, ClaimsPrincipal? user)
    {
        // Self link - always included
        yield return LinkDefinition.Self(
            RouteNames.GetEventTemplateById,
            new { id = dto.Id });

        // Collection link
        yield return LinkDefinition.Collection(
            RouteNames.GetEventTemplates,
            new { eventTypeId = dto.EventTypeId });

        // Edit link - requires Update permission
        yield return LinkDefinition.Edit(
            RouteNames.UpdateEventTemplate,
            new { id = dto.Id })
            .RequirePermission(
                PermissionAction.Update,
                dto,
                dto.Id.ToString(),
                new Dictionary<string, object>
                {
                    ["eventTemplateId"] = dto.Id.ToString(),
                    ["tenantId"] = dto.TenantId.ToString()
                });

        // Delete link - requires Delete permission
        yield return LinkDefinition.Delete(
            RouteNames.DeleteEventTemplate,
            new { id = dto.Id })
            .RequirePermission(
                PermissionAction.Delete,
                dto,
                dto.Id.ToString(),
                new Dictionary<string, object>
                {
                    ["eventTemplateId"] = dto.Id.ToString(),
                    ["tenantId"] = dto.TenantId.ToString()
                });
    }
}

/// <summary>
/// Link policy for EventTemplateListDto (collection items).
/// </summary>
public sealed class EventTemplateCollectionLinkPolicy : ICollectionLinkPolicy<EventTemplateListDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(EventTemplateListDto dto, ClaimsPrincipal? user)
    {
        // Self link for the item
        yield return LinkDefinition.Self(
            RouteNames.GetEventTemplateById,
            new { id = dto.Id });
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Create link - requires Create permission
        yield return LinkDefinition.Create(RouteNames.CreateEventTemplate)
            .RequirePermission(PermissionAction.Create, typeof(EventTemplateDto), "eventTemplate");
    }
}
