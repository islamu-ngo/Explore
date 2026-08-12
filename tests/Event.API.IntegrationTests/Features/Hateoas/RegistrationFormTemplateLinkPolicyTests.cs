// ABOUTME: Verifies registration-form template HAL authorization metadata.
// ABOUTME: Keeps create and instantiate affordances discoverable but gated by the right authority.

using Explore.API.Hateoas;
using Explore.API.Hateoas.Policies;
using Explore.Application.Authorization;
using Explore.Application.DTOs.RegistrationForms;
using Explore.Application.Hateoas;
using Microsoft.AspNetCore.Http;

namespace Event.Api.IntegrationTests.Features.Hateoas;

public sealed class RegistrationFormTemplateLinkPolicyTests
{
    [Test]
    public async Task TemplateLinks_GateCreateByRegistrationFormAndInstantiateByTargetEventWorkflowAuthority()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid templateId = Guid.CreateVersion7();
        var template = new RegistrationFormTemplateDto(
            templateId, tenantId, false, "Template", "Description", "Registration", null,
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());
        var detail = new RegistrationFormTemplateLinkPolicy().GetLinks(template, null).ToArray();
        var collection = new RegistrationFormTemplateCollectionLinkPolicy().GetCollectionLinks(null).ToArray();

        LinkDefinition instantiate = detail.Single(link => link.Rel == LinkRelations.Instantiate);
        LinkDefinition create = collection.Single(link => link.Rel == LinkRelations.Create);

        await Assert.That(instantiate.RouteName).IsEqualTo(RouteNames.InstantiateRegistrationFormTemplate);
        await Assert.That(instantiate.Method).IsEqualTo(HttpMethods.Post);
        await Assert.That(instantiate.PermissionResourceKind).IsEqualTo(ResourceKinds.Event);
        await Assert.That(instantiate.PermissionAction).IsEqualTo(AuthorizationActions.Events.ManageRegistrationWorkflow);
        await Assert.That(instantiate.PermissionResourceId).IsNull();
        await Assert.That(instantiate.PermissionResourceAttributes!["templateId"]).IsEqualTo(templateId.ToString());
        await Assert.That(instantiate.PermissionScope!.TenantId).IsEqualTo(tenantId.ToString());
        await Assert.That(create.PermissionResourceKind).IsEqualTo(ResourceKinds.RegistrationForm);
        await Assert.That(create.PermissionAction).IsEqualTo(AuthorizationActions.RegistrationForms.Create);
    }
}
