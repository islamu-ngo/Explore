// ABOUTME: Defines HAL affordances for registration-form template catalog resources.
// ABOUTME: Keeps template instantiation and creation discoverable through authorization-filtered links.

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.RegistrationForms;
using Explore.Application.Hateoas;

namespace Explore.API.Hateoas.Policies;

public sealed class RegistrationFormTemplateLinkPolicy : ILinkPolicy<RegistrationFormTemplateDto>
{
    public IEnumerable<LinkDefinition> GetLinks(RegistrationFormTemplateDto dto, ClaimsPrincipal? user)
    {
        yield return LinkDefinition.Self(RouteNames.GetRegistrationFormTemplate, new { templateId = dto.Id });
        yield return new LinkDefinition(LinkRelations.Collection, RouteNames.GetRegistrationFormTemplates);
        yield return new LinkDefinition(
            LinkRelations.Instantiate,
            RouteNames.InstantiateRegistrationFormTemplate,
            new { templateId = dto.Id },
            HttpMethods.Post,
            RequiresAuth: true).RequirePermission(
                AuthorizationActions.Events.ManageRegistrationWorkflow,
                ResourceKinds.Event,
                null,
                new Dictionary<string, object> { ["templateId"] = dto.Id.ToString() },
                dto.TenantId is null ? null : new AuthorizationScope(TenantId: dto.TenantId.ToString()));
    }

    private static LinkDefinition Action(string rel, string route, object values, string method, string action, RegistrationFormTemplateDto dto) =>
        new LinkDefinition(rel, route, values, method, RequiresAuth: true).RequirePermission(
            action,
            ResourceKinds.RegistrationForm,
            dto.Id.ToString(),
            new Dictionary<string, object> { ["templateId"] = dto.Id.ToString() },
            new AuthorizationScope(TenantId: dto.TenantId?.ToString()));
}

public sealed class RegistrationFormTemplateCollectionLinkPolicy : ICollectionLinkPolicy<RegistrationFormTemplateDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(RegistrationFormTemplateDto dto, ClaimsPrincipal? user) =>
        new RegistrationFormTemplateLinkPolicy().GetLinks(dto, user);

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Create,
            RouteNames.CreateRegistrationFormTemplate,
            null,
            HttpMethods.Post,
            RequiresAuth: true).RequirePermission(
                AuthorizationActions.RegistrationForms.Create,
                ResourceKinds.RegistrationForm,
                null,
                null,
                null);
    }
}
