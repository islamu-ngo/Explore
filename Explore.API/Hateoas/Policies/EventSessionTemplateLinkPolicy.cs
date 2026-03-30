// ABOUTME: HATEOAS link policies for EventSessionTemplate detail and collection views.
// ABOUTME: Controls which links appear based on resource state and user permissions.

namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventSessionTemplate;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for EventSessionTemplateDto (detail view).
/// Determines which links should be included based on resource state and user authorization.
/// </summary>
public sealed class EventSessionTemplateDetailLinkPolicy : ILinkPolicy<EventSessionTemplateDto>
{
    public IEnumerable<LinkDefinition> GetLinks(EventSessionTemplateDto dto, ClaimsPrincipal? user)
    {
        // Self link - always included
        yield return LinkDefinition.Self(
            RouteNames.GetEventSessionTemplateById,
            new { id = dto.Id });

        // Collection link
        yield return LinkDefinition.Collection(
            RouteNames.GetEventSessionTemplates,
            new { eventTemplateId = dto.EventTemplateId });

        // Edit link - requires Update permission
        yield return LinkDefinition.Edit(
            RouteNames.UpdateEventSessionTemplate,
            new { id = dto.Id })
            .RequirePermission(
                PermissionAction.Update,
                dto,
                dto.Id.ToString(),
                new Dictionary<string, object>
                {
                    ["eventSessionTemplateId"] = dto.Id.ToString(),
                    ["tenantId"] = dto.TenantId.ToString()
                });

        // Delete link - requires Delete permission
        yield return LinkDefinition.Delete(
            RouteNames.DeleteEventSessionTemplate,
            new { id = dto.Id })
            .RequirePermission(
                PermissionAction.Delete,
                dto,
                dto.Id.ToString(),
                new Dictionary<string, object>
                {
                    ["eventSessionTemplateId"] = dto.Id.ToString(),
                    ["tenantId"] = dto.TenantId.ToString()
                });
    }
}

/// <summary>
/// Link policy for EventSessionTemplateListDto (collection items).
/// </summary>
public sealed class EventSessionTemplateCollectionLinkPolicy : ICollectionLinkPolicy<EventSessionTemplateListDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(EventSessionTemplateListDto dto, ClaimsPrincipal? user)
    {
        // Self link for the item
        yield return LinkDefinition.Self(
            RouteNames.GetEventSessionTemplateById,
            new { id = dto.Id });
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Create link - requires Create permission
        yield return LinkDefinition.Create(RouteNames.CreateEventSessionTemplate)
            .RequirePermission(PermissionAction.Create, typeof(EventSessionTemplateDto), "eventSessionTemplate");
    }
}
