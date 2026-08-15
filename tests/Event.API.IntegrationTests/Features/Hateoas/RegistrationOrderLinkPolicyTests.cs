// ABOUTME: Verifies registration-order HAL candidates carry order-scoped authorization metadata.
// ABOUTME: Keeps opaque guest capabilities out of link routes, parameters, and authorization attributes.

using Explore.API.Hateoas;
using Explore.API.Hateoas.Policies;
using Explore.Application.Authorization;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Hateoas;

namespace Event.Api.IntegrationTests.Features.Hateoas;

public sealed class RegistrationOrderLinkPolicyTests
{
    [Test]
    public async Task GetLinks_UsesOrderScopedViewAndCancelCandidates()
    {
        var order = new RegistrationOrderDto
        {
            Id = Guid.CreateVersion7(),
            EventId = Guid.CreateVersion7(),
            AccountUserId = Guid.CreateVersion7()
        };

        var links = new RegistrationOrderLinkPolicy().GetLinks(order, null).ToList();

        await Assert.That(links.Select(link => link.Rel)).IsEquivalentTo(
            [LinkRelations.Self, LinkRelations.ViewParticipants, LinkRelations.Cancel]);
        await Assert.That(links.All(link => link.PermissionResourceKind == ResourceKinds.RegistrationOrder)).IsTrue();
        await Assert.That(links.Single(link => link.Rel == LinkRelations.Self).PermissionAction)
            .IsEqualTo(AuthorizationActions.RegistrationOrders.View);
        await Assert.That(links.Single(link => link.Rel == LinkRelations.Cancel).PermissionAction)
            .IsEqualTo(AuthorizationActions.RegistrationOrders.Cancel);
        await Assert.That(links.Single(link => link.Rel == LinkRelations.ViewParticipants).PermissionAction)
            .IsEqualTo(AuthorizationActions.RegistrationOrders.View);
    }

    [Test]
    public async Task GetLinks_WhenReadyForCheckoutWithoutPromotion_EmitsApplyButNotRemove()
    {
        var order = new RegistrationOrderDto
        {
            Id = Guid.CreateVersion7(),
            EventId = Guid.CreateVersion7(),
            AccountUserId = Guid.CreateVersion7(),
            StatusCode = "READY_FOR_CHECKOUT"
        };

        var links = new RegistrationOrderLinkPolicy().GetLinks(order, null).ToList();

        await Assert.That(links.Select(link => link.Rel)).IsEquivalentTo(
            [LinkRelations.Self, LinkRelations.ViewParticipants, LinkRelations.Continue, LinkRelations.ApplyPromotion, LinkRelations.Finalize, LinkRelations.Cancel]);
        await Assert.That(links.Select(link => link.Rel)).DoesNotContain(LinkRelations.RemovePromotion);
        await Assert.That(links.Single(link => link.Rel == LinkRelations.Continue).RouteName)
            .IsEqualTo(RouteNames.ContinueAuthenticatedRegistrationOrder);
        await Assert.That(links.Single(link => link.Rel == LinkRelations.ApplyPromotion).RouteName)
            .IsEqualTo(RouteNames.ApplyAuthenticatedRegistrationOrderPromotion);
        await Assert.That(links.Single(link => link.Rel == LinkRelations.Finalize).RouteName)
            .IsEqualTo(RouteNames.FinalizeAuthenticatedRegistrationOrder);
        await Assert.That(links.Single(link => link.Rel == LinkRelations.Continue).PermissionAction)
            .IsEqualTo("continue");
        await Assert.That(links.Single(link => link.Rel == LinkRelations.Finalize).PermissionAction)
            .IsEqualTo("finalize");
    }

    [Test]
    public async Task GetLinks_WhenReadyForCheckoutWithPromotion_EmitsRemoveButNotApply()
    {
        var order = new RegistrationOrderDto
        {
            Id = Guid.CreateVersion7(),
            EventId = Guid.CreateVersion7(),
            AccountUserId = Guid.CreateVersion7(),
            StatusCode = "READY_FOR_CHECKOUT",
            AppliedPromotionDisplayLabel = "Launch discount"
        };

        var links = new RegistrationOrderLinkPolicy().GetLinks(order, null).ToList();

        await Assert.That(links.Select(link => link.Rel)).Contains(LinkRelations.RemovePromotion);
        await Assert.That(links.Select(link => link.Rel)).DoesNotContain(LinkRelations.ApplyPromotion);
        await Assert.That(links.Single(link => link.Rel == LinkRelations.RemovePromotion).RouteName)
            .IsEqualTo(RouteNames.RemoveAuthenticatedRegistrationOrderPromotion);
    }

    [Test]
    public async Task GetLinks_WhenAwaitingRequirements_EmitsContinueButNotFinalize()
    {
        var order = new RegistrationOrderDto
        {
            Id = Guid.CreateVersion7(),
            EventId = Guid.CreateVersion7(),
            AccountUserId = Guid.CreateVersion7(),
            StatusCode = "AWAITING_REQUIREMENTS"
        };

        var links = new RegistrationOrderLinkPolicy().GetLinks(order, null).ToList();

        await Assert.That(links.Select(link => link.Rel)).IsEquivalentTo(
            [LinkRelations.Self, LinkRelations.ViewParticipants, LinkRelations.Continue, LinkRelations.RequirementProgress, LinkRelations.Cancel]);
        LinkDefinition progress = links.Single(link => link.Rel == LinkRelations.RequirementProgress);
        await Assert.That(progress.RouteName)
            .IsEqualTo(RouteNames.GetAuthenticatedNativeRegistrationRequirementProgress);
        await Assert.That(progress.Method).IsEqualTo("GET");
    }
}
