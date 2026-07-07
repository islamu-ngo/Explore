// ABOUTME: Link-policy contract tests for control-plane tenant plan HAL affordances.
// ABOUTME: Protects SaaS plan template actions from drifting away from server authorization metadata.

using Explore.API.Hateoas;
using Explore.API.Hateoas.Policies;
using Explore.Application.Authorization;
using Explore.Application.DTOs.ControlPlane;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Features.ControlPlane.Requests.Queries;
using Explore.Application.Hateoas;
using Microsoft.AspNetCore.Routing;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

public sealed class ControlPlaneTenantPlanHateoasTests
{
    [Test]
    public async Task DetailLinks_ExposePlanReadAuthorizationMetadata()
    {
        var policy = new ControlPlaneTenantPlanDetailLinkPolicy();

        var links = policy.GetLinks(CreateDetail(), user: null).ToArray();

        var self = links.Single(link => link.Rel == LinkRelations.Self);
        await Assert.That(self.RouteName).IsEqualTo(RouteNames.GetControlPlaneTenantPlanByKey);
        await Assert.That(self.Method).IsEqualTo("GET");
        await Assert.That(self.RequiresAuth).IsTrue();
        await Assert.That(self.PermissionResourceKind).IsEqualTo(ResourceKinds.InstanceSetting);
        await Assert.That(self.PermissionAction).IsEqualTo(AuthorizationActions.InstanceSettings.View);
        await Assert.That(self.PermissionResourceId).IsEqualTo(GetControlPlaneTenantPlanListQuery.SettingKey);
        await Assert.That(self.PermissionResourceAttributes?["settingKey"]).IsEqualTo(GetControlPlaneTenantPlanListQuery.SettingKey);
        await Assert.That(self.PermissionResourceAttributes?["planKey"]).IsEqualTo("community");

        var collection = links.Single(link => link.Rel == LinkRelations.Collection);
        await Assert.That(collection.RouteName).IsEqualTo(RouteNames.GetControlPlaneTenantPlans);
        await Assert.That(collection.PermissionResourceKind).IsEqualTo(ResourceKinds.InstanceSetting);
        await Assert.That(collection.PermissionAction).IsEqualTo(AuthorizationActions.InstanceSettings.View);
        await Assert.That(collection.PermissionResourceId).IsEqualTo(GetControlPlaneTenantPlanListQuery.SettingKey);
    }

    [Test]
    public async Task CollectionLinks_ExposeCreateDraftAuthorizationMetadata()
    {
        var policy = new ControlPlaneTenantPlanCollectionLinkPolicy();

        var itemLinks = policy.GetItemLinks(CreateListItem(), user: null).ToArray();
        var collectionLinks = policy.GetCollectionLinks(user: null).ToArray();

        var self = itemLinks.Single(link => link.Rel == LinkRelations.Self);
        await Assert.That(self.RouteName).IsEqualTo(RouteNames.GetControlPlaneTenantPlanByKey);
        await Assert.That(self.PermissionResourceKind).IsEqualTo(ResourceKinds.InstanceSetting);
        await Assert.That(self.PermissionAction).IsEqualTo(AuthorizationActions.InstanceSettings.View);
        await Assert.That(self.PermissionResourceAttributes?["planKey"]).IsEqualTo("community");

        var create = collectionLinks.Single(link => link.Rel == LinkRelations.Create);
        await Assert.That(create.RouteName).IsEqualTo(RouteNames.CreateControlPlaneTenantPlanDraft);
        await Assert.That(create.Method).IsEqualTo("POST");
        await Assert.That(create.RequiresAuth).IsTrue();
        await Assert.That(create.PermissionResourceKind).IsEqualTo(ResourceKinds.InstanceSetting);
        await Assert.That(create.PermissionAction).IsEqualTo(AuthorizationActions.InstanceSettings.Update);
        await Assert.That(create.PermissionResourceId).IsEqualTo(CreateControlPlaneTenantPlanDraftCommand.SettingKey);
        await Assert.That(create.PermissionResourceAttributes?["settingKey"]).IsEqualTo(CreateControlPlaneTenantPlanDraftCommand.SettingKey);

        var validate = collectionLinks.Single(link => link.Rel == "validate");
        await Assert.That(validate.RouteName).IsEqualTo(RouteNames.ValidateControlPlaneTenantPlanDraft);
        await Assert.That(validate.Method).IsEqualTo("POST");
        await Assert.That(validate.PermissionAction).IsEqualTo(AuthorizationActions.InstanceSettings.View);
        await Assert.That(validate.PermissionResourceId).IsEqualTo(ValidateControlPlaneTenantPlanDraftQuery.SettingKey);

        var previewDiff = collectionLinks.Single(link => link.Rel == "preview-diff");
        await Assert.That(previewDiff.RouteName).IsEqualTo(RouteNames.PreviewControlPlaneTenantPlanDiff);
        await Assert.That(previewDiff.Method).IsEqualTo("POST");
        await Assert.That(previewDiff.PermissionAction).IsEqualTo(AuthorizationActions.InstanceSettings.View);
        await Assert.That(previewDiff.PermissionResourceId).IsEqualTo(PreviewControlPlaneTenantPlanDiffQuery.SettingKey);
    }

    [Test]
    public async Task DetailLinks_ExposePlanVersionActionAuthorizationMetadata()
    {
        var policy = new ControlPlaneTenantPlanDetailLinkPolicy();
        var detail = CreateDetail();
        var versionId = detail.Versions[0].Id;

        var links = policy.GetLinks(detail, user: null).ToArray();

        var createVersion = links.Single(link => link.Rel == "create-version-draft");
        await Assert.That(createVersion.RouteName).IsEqualTo(RouteNames.CreateControlPlaneTenantPlanVersionDraft);
        await Assert.That(createVersion.Method).IsEqualTo("POST");
        await Assert.That(RouteValues(createVersion)["key"]).IsEqualTo("community");
        await Assert.That(createVersion.PermissionAction).IsEqualTo(AuthorizationActions.InstanceSettings.Update);
        await Assert.That(createVersion.PermissionResourceId).IsEqualTo(CreateControlPlaneTenantPlanVersionDraftCommand.SettingKey);
        await Assert.That(createVersion.PermissionResourceAttributes?["planKey"]).IsEqualTo("community");

        var updateVersion = links.Single(link => link.Rel == "update-version-draft");
        await Assert.That(updateVersion.RouteName).IsEqualTo(RouteNames.UpdateControlPlaneTenantPlanVersionDraft);
        await Assert.That(updateVersion.Method).IsEqualTo("PUT");
        await Assert.That(RouteValues(updateVersion)["versionId"]).IsEqualTo(versionId);
        await Assert.That(updateVersion.PermissionResourceId).IsEqualTo(UpdateControlPlaneTenantPlanVersionDraftCommand.SettingKey);
        await Assert.That(updateVersion.PermissionResourceAttributes?["versionId"]).IsEqualTo(versionId);

        var publish = links.Single(link => link.Rel == LinkRelations.Publish);
        await Assert.That(publish.RouteName).IsEqualTo(RouteNames.PublishControlPlaneTenantPlanVersion);
        await Assert.That(publish.Method).IsEqualTo("POST");
        await Assert.That(publish.PermissionResourceId).IsEqualTo(PublishControlPlaneTenantPlanVersionCommand.SettingKey);
        await Assert.That(publish.PermissionAction).IsEqualTo(AuthorizationActions.InstanceSettings.Update);

        var archive = links.Single(link => link.Rel == LinkRelations.Archive);
        await Assert.That(archive.RouteName).IsEqualTo(RouteNames.ArchiveControlPlaneTenantPlanVersion);
        await Assert.That(archive.Method).IsEqualTo("POST");
        await Assert.That(archive.PermissionResourceId).IsEqualTo(ArchiveControlPlaneTenantPlanVersionCommand.SettingKey);

        var clone = links.Single(link => link.Rel == "clone");
        await Assert.That(clone.RouteName).IsEqualTo(RouteNames.CloneControlPlaneTenantPlan);
        await Assert.That(RouteValues(clone)["sourceVersionId"]).IsEqualTo(versionId);
        await Assert.That(clone.PermissionResourceId).IsEqualTo(CloneControlPlaneTenantPlanCommand.SettingKey);
        await Assert.That(clone.PermissionResourceAttributes?["sourceVersionId"]).IsEqualTo(versionId);

        var validate = links.Single(link => link.Rel == "validate");
        await Assert.That(validate.RouteName).IsEqualTo(RouteNames.ValidateControlPlaneTenantPlanDraft);
        await Assert.That(validate.PermissionAction).IsEqualTo(AuthorizationActions.InstanceSettings.View);
        await Assert.That(validate.PermissionResourceId).IsEqualTo(ValidateControlPlaneTenantPlanDraftQuery.SettingKey);

        var previewDiff = links.Single(link => link.Rel == "preview-diff");
        await Assert.That(previewDiff.RouteName).IsEqualTo(RouteNames.PreviewControlPlaneTenantPlanDiff);
        await Assert.That(previewDiff.PermissionAction).IsEqualTo(AuthorizationActions.InstanceSettings.View);
        await Assert.That(previewDiff.PermissionResourceId).IsEqualTo(PreviewControlPlaneTenantPlanDiffQuery.SettingKey);
    }

    private static ControlPlaneTenantPlanDetailDto CreateDetail() => new()
    {
        Id = Guid.NewGuid(),
        Key = "community",
        DisplayName = "Community",
        Versions =
        [
            new ControlPlaneTenantPlanVersionDto
            {
                Id = Guid.NewGuid(),
                VersionNumber = 1,
                StatusCode = "PUBLISHED",
                PriceAmount = 29m,
                CurrencyCode = "EUR",
                BillingPeriod = "monthly",
                IsActiveForProvisioning = true
            }
        ]
    };

    private static ControlPlaneTenantPlanListItemDto CreateListItem() => new()
    {
        Id = Guid.NewGuid(),
        Key = "community",
        DisplayName = "Community",
        LatestVersionNumber = 1,
        PublishedVersionNumber = 1,
        PriceAmount = 29m,
        CurrencyCode = "EUR",
        BillingPeriod = "monthly",
        IsActiveForProvisioning = true
    };

    private static RouteValueDictionary RouteValues(LinkDefinition link) => new(link.RouteValues);
}
