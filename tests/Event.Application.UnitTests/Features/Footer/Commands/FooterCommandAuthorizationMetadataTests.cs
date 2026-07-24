// ABOUTME: Verifies tenant-scoped footer mutation commands require tenant update authorization metadata.
// ABOUTME: Prevents footer write paths from regressing to controller-only authentication.

using System.Reflection;
using Explore.Application.Authorization;
using Explore.Application.Features.Footer.Requests.Commands;

namespace Event.Application.UnitTests.Features.Footer.Commands;

public class FooterCommandAuthorizationMetadataTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid LinkId = Guid.NewGuid();

    [Test]
    [Arguments(typeof(CreateFooterLinkGroupCommand))]
    [Arguments(typeof(UpdateFooterLinkGroupCommand))]
    [Arguments(typeof(DeleteFooterLinkGroupCommand))]
    [Arguments(typeof(ReorderFooterLinkGroupsCommand))]
    [Arguments(typeof(CreateFooterLinkCommand))]
    [Arguments(typeof(UpdateFooterLinkCommand))]
    [Arguments(typeof(DeleteFooterLinkCommand))]
    [Arguments(typeof(PatchTenantFooterSettingsCommand))]
    public async Task TenantScopedFooterWriteCommandsRequireTenantUpdatePermission(Type commandType)
    {
        var attribute = commandType.GetCustomAttribute<AuthorizeResourceAttribute>();

        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Resource).IsEqualTo(ResourceKinds.Tenant);
        await Assert.That(attribute.Action).IsEqualTo(AuthorizationActions.Update);
        await Assert.That(typeof(ISecureRequest).IsAssignableFrom(commandType)).IsTrue();
    }

    public static IEnumerable<(ISecureRequest Command, string? ExpectedContextKey)> TenantScopedFooterCommands()
    {
        yield return (new CreateFooterLinkGroupCommand { UserId = UserId, TenantId = TenantId, Title = "Main" }, null);
        yield return (new UpdateFooterLinkGroupCommand { UserId = UserId, TenantId = TenantId, GroupId = GroupId, Title = "Main", IsActive = true }, "groupId");
        yield return (new DeleteFooterLinkGroupCommand { UserId = UserId, TenantId = TenantId, GroupId = GroupId }, "groupId");
        yield return (new ReorderFooterLinkGroupsCommand { UserId = UserId, TenantId = TenantId, OrderedGroupIds = [GroupId] }, null);
        yield return (new CreateFooterLinkCommand { UserId = UserId, TenantId = TenantId, GroupId = GroupId, Label = "Home", Url = "https://example.test", OpenInNewTab = false }, "groupId");
        yield return (new UpdateFooterLinkCommand { UserId = UserId, TenantId = TenantId, LinkId = LinkId, Label = "Home", Url = "https://example.test", OpenInNewTab = false, IsActive = true }, "linkId");
        yield return (new DeleteFooterLinkCommand { UserId = UserId, TenantId = TenantId, LinkId = LinkId }, "linkId");
        yield return (new PatchTenantFooterSettingsCommand
        {
            UserId = UserId,
            TenantId = TenantId,
            Patch = new()
            {
                General = new()
                {
                    Enabled = Explore.Application.Models.Common.OptionalUpdate<bool>.Set(true)
                }
            }
        }, "settingGroup");
    }

    [Test]
    [MethodDataSource(nameof(TenantScopedFooterCommands))]
    public async Task TenantScopedFooterWriteCommandsExposeTenantAuthorizationContext(
        (ISecureRequest Command, string? ExpectedContextKey) testCase)
    {
        var (command, expectedContextKey) = testCase;

        await Assert.That(command.ResourceId).IsEqualTo(TenantId.ToString("D"));
        await Assert.That(command.ResourceAttributes).IsNotNull();
        await Assert.That(command.ResourceAttributes!["tenantId"]).IsEqualTo(TenantId.ToString("D"));

        if (expectedContextKey is not null)
        {
            await Assert.That(command.ResourceAttributes.ContainsKey(expectedContextKey)).IsTrue();
        }
    }
}
