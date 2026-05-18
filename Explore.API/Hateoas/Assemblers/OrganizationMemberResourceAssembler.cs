namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.Hateoas;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Resource assembler for OrganizationMember entities (relationship with payload).
/// Converts OrganizationMemberDto to HAL resources with appropriate links.
/// Note: OrganizationMember uses same DTO for detail and list views.
/// </summary>
public sealed class OrganizationMemberResourceAssembler : ResourceAssemblerBase<OrganizationMemberDto, OrganizationMemberDto>
{
    public OrganizationMemberResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<OrganizationMemberDto> detailLinkPolicy,
        ICollectionLinkPolicy<OrganizationMemberDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    public override async Task<HalCollectionResource<OrganizationMemberDto>> ToCollectionResource(
        IEnumerable<OrganizationMemberDto> items,
        string routeName,
        object? additionalRouteValues,
        HttpContext httpContext)
    {
        var resource = await base.ToCollectionResource(items, routeName, additionalRouteValues, httpContext);
        if (TryGetOrganizationId(additionalRouteValues, out var organizationId))
        {
            var organizationScopedAttributes = new Dictionary<string, object>
            {
                ["organizationId"] = organizationId.ToString()
            };

            var createLinks = await GenerateLinks([
                new LinkDefinition(
                    LinkRelations.Create,
                    RouteNames.AddOrganizationMember,
                    null,
                    "POST",
                    "Add organization member",
                    RequiresAuth: true)
                    .RequirePermission(
                        AuthorizationActions.Create,
                        ResourceKinds.OrganizationMember,
                        organizationId.ToString(),
                        organizationScopedAttributes,
                        new AuthorizationScope(OrganizationId: organizationId.ToString()))
            ], httpContext.User, httpContext);

            foreach (var link in createLinks)
            {
                resource.Links[link.Key] = link.Value;
            }
        }

        return resource;
    }

    /// <summary>
    /// Override to provide embedded resources for organization member details.
    /// </summary>
    protected override Dictionary<string, object>? GetEmbeddedResources(
        OrganizationMemberDto dto,
        HttpContext httpContext)
    {
        // Members link to User, Organization, Role via _links
        return null;
    }

    private static bool TryGetOrganizationId(object? routeValues, out Guid organizationId)
    {
        organizationId = default;
        if (routeValues is null)
        {
            return false;
        }

        object? value = null;
        if (routeValues is IReadOnlyDictionary<string, object> readOnlyDictionary)
        {
            readOnlyDictionary.TryGetValue("organizationId", out value);
        }
        else if (routeValues is IDictionary<string, object> dictionary)
        {
            dictionary.TryGetValue("organizationId", out value);
        }
        else
        {
            value = routeValues.GetType().GetProperty("organizationId")?.GetValue(routeValues);
        }

        return value switch
        {
            Guid typedOrganizationId => SetOrganizationId(typedOrganizationId, out organizationId),
            string text => Guid.TryParse(text, out organizationId),
            _ => false
        };
    }

    private static bool SetOrganizationId(Guid value, out Guid organizationId)
    {
        organizationId = value;
        return value != Guid.Empty;
    }
}
