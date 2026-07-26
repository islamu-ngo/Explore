// ABOUTME: HAL contract tests for event public actions and organizer claims.
// ABOUTME: Enforces independent collection policies and event-scoped permission metadata.

using System.Reflection;
using Explore.API.Hateoas.Policies;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventOrganizerClaim;
using Explore.Application.Hateoas;
using Explore.Domain.Enums;

namespace Event.Api.IntegrationTests.Features.Hateoas;

public sealed class EventAuthorityLinkPolicyTests
{
    [Test]
    public async Task PublicActionCollectionPolicy_IsIndependentAndUsesStoredRedirectRoute()
    {
        var policyType = typeof(EventPublicActionCollectionLinkPolicy);
        var action = new EventPublicActionDto
        {
            Id = Guid.CreateVersion7(),
            EventId = Guid.CreateVersion7(),
            KindId = (int)EventPublicActionKindEnum.ExternalRegistration,
            Url = "https://registration.example/events/1",
            DestinationDomain = "registration.example"
        };

        var links = new EventPublicActionCollectionLinkPolicy().GetItemLinks(action, null).ToList();
        var destination = links.Single(link => link.Rel == LinkRelations.ExternalRegistration);

        await Assert.That(policyType.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)
            .Any(field => field.FieldType == typeof(EventPublicActionDetailLinkPolicy))).IsFalse();
        await Assert.That(destination.RouteName).IsEqualTo(Explore.API.Hateoas.RouteNames.RedirectEventPublicAction);
        await Assert.That(destination.RouteValues?.ToString()).DoesNotContain(action.Url);
    }

    [Test]
    public async Task OrganizerClaimCollectionPolicy_IsIndependentAndPermissionBound()
    {
        var policyType = typeof(EventOrganizerClaimCollectionLinkPolicy);
        var claim = new EventOrganizerClaimDto
        {
            Id = Guid.CreateVersion7(),
            EventId = Guid.CreateVersion7(),
            EvidenceType = "domain-proof",
            EvidenceReference = "bounded-reference"
        };

        var links = new EventOrganizerClaimCollectionLinkPolicy().GetItemLinks(claim, null).ToList();
        var review = links.Single(link => link.Rel == LinkRelations.ReviewClaim);

        await Assert.That(policyType.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)
            .Any(field => field.FieldType == typeof(EventOrganizerClaimDetailLinkPolicy))).IsFalse();
        await Assert.That(review.PermissionResourceKind).IsEqualTo(ResourceKinds.Event);
        await Assert.That(review.PermissionAction).IsEqualTo(AuthorizationActions.Events.ReviewOrganizerClaim);
    }
}
