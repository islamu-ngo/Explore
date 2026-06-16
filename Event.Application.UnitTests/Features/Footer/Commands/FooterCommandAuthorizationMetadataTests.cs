// ABOUTME: Verifies tenant-scoped footer mutation commands require tenant update authorization metadata.
// ABOUTME: Prevents footer write paths from regressing to controller-only authentication.

using System.Reflection;
using Explore.Application.Authorization;
using Explore.Application.Features.Footer.Requests.Commands;

namespace Event.Application.UnitTests.Features.Footer.Commands;

public class FooterCommandAuthorizationMetadataTests
{
    [Test]
    [Arguments(typeof(CreateFooterLinkGroupCommand))]
    [Arguments(typeof(UpdateFooterLinkGroupCommand))]
    [Arguments(typeof(DeleteFooterLinkGroupCommand))]
    [Arguments(typeof(ReorderFooterLinkGroupsCommand))]
    [Arguments(typeof(CreateFooterLinkCommand))]
    [Arguments(typeof(UpdateFooterLinkCommand))]
    [Arguments(typeof(DeleteFooterLinkCommand))]
    [Arguments(typeof(UpdateTenantFooterSettingsCommand))]
    public async Task TenantScopedFooterWriteCommandsRequireTenantUpdatePermission(Type commandType)
    {
        var attribute = commandType.GetCustomAttribute<AuthorizeResourceAttribute>();

        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Resource).IsEqualTo(ResourceKinds.Tenant);
        await Assert.That(attribute.Action).IsEqualTo(AuthorizationActions.Update);
        await Assert.That(typeof(ISecureRequest).IsAssignableFrom(commandType)).IsTrue();
    }
}
