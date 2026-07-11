// ABOUTME: Tests HAL link policy metadata for tenant branding typed settings documents.
// ABOUTME: Ensures replace-settings is server-authorized and routed through typed document endpoints.

using Explore.API.Hateoas;
using Explore.API.Hateoas.Policies;
using Explore.Application.Authorization;
using Explore.Application.DTOs.TenantSettingsDocuments;
using Explore.Application.Hateoas;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

public sealed class TenantBrandingSettingsDocumentLinkPolicyTests
{
    [Test]
    public async Task GetLinks_ShouldEmitSelfAndPermissionBoundReplaceSettingsLinks()
    {
        var tenantId = Guid.NewGuid();
        var dto = new TenantBrandingSettingsDocumentDto
        {
            DocumentKey = "tenant.branding",
            SchemaVersion = 1,
            DefaultsVersion = "2026-05-branding",
            Payload = new TenantBrandingSettingsPayloadDto(),
            Source = "Tenant",
            SourceScopeId = tenantId,
            ConcurrencyStamp = Guid.NewGuid(),
            IsLockedByInstance = true
        };
        var policy = new TenantBrandingSettingsDocumentLinkPolicy();

        var links = policy.GetLinks(dto, user: null).ToArray();

        var self = links.Single(link => link.Rel == LinkRelations.Self);
        await Assert.That(self.RouteName).IsEqualTo(RouteNames.GetTenantBrandingSettingsDocument);
        await Assert.That(self.Method).IsEqualTo("GET");

        var replace = links.Single(link => link.Rel == "self/replace-settings");
        await Assert.That(replace.RouteName).IsEqualTo(RouteNames.ReplaceTenantBrandingSettingsDocument);
        await Assert.That(replace.Method).IsEqualTo("PUT");
        await Assert.That(replace.RequiresAuth).IsTrue();
        await Assert.That(replace.PermissionResourceKind).IsEqualTo(ResourceKinds.TenantSetting);
        await Assert.That(replace.PermissionAction).IsEqualTo(AuthorizationActions.Update);
        await Assert.That(replace.PermissionResourceId).IsEqualTo($"{tenantId}:tenant.branding");
        await Assert.That(replace.PermissionResourceAttributes).IsNotNull();
        await Assert.That(replace.PermissionResourceAttributes!["tenantId"]).IsEqualTo(tenantId.ToString());
        await Assert.That(replace.PermissionResourceAttributes["documentKey"]).IsEqualTo("tenant.branding");
        await Assert.That(replace.PermissionResourceAttributes["isLockedByInstance"]).IsEqualTo(true);
        await Assert.That(replace.PermissionScope).IsNotNull();
        await Assert.That(replace.PermissionScope!.TenantId).IsEqualTo(tenantId.ToString());
    }
}
