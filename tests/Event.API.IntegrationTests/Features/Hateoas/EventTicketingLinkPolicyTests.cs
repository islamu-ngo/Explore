// ABOUTME: Unit-level HATEOAS policy tests for event ticket catalog management.
// ABOUTME: Guards lifecycle-specific catalog actions and parent-event ticket authorization metadata.

using Explore.API.Hateoas;
using Explore.API.Hateoas.Policies;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventTicketing;
using Explore.Application.Hateoas;
using Explore.Domain.Enums;
using Microsoft.AspNetCore.Http;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

public sealed class EventTicketingLinkPolicyTests
{
    [Test]
    public async Task EmptyCatalog_AdvertisesDraftCreationAgainstParentEvent()
    {
        Guid eventId = Guid.CreateVersion7();
        var dto = new EventTicketCatalogManagementDto { EventId = eventId };

        var link = new EventTicketCatalogManagementLinkPolicy()
            .GetLinks(dto, null)
            .Single(candidate => candidate.Rel == LinkRelations.CreateDraft);

        await Assert.That(link.RouteName).IsEqualTo(RouteNames.CreateEventTicketCatalogDraft);
        await Assert.That(link.Method).IsEqualTo(HttpMethods.Post);
        await Assert.That(link.PermissionResourceKind).IsEqualTo(ResourceKinds.Event);
        await Assert.That(link.PermissionAction).IsEqualTo(AuthorizationActions.Events.ManageTickets);
        await Assert.That(link.PermissionResourceId).IsEqualTo(eventId.ToString("D"));
    }

    [Test]
    public async Task DraftCatalog_AdvertisesPublishAndPerItemManagementActions()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid ticketTypeId = Guid.CreateVersion7();
        Guid poolId = Guid.CreateVersion7();
        var dto = new EventTicketCatalogManagementDto
        {
            EventId = eventId,
            CatalogId = Guid.CreateVersion7(),
            StatusId = (int)TicketCatalogStatusEnum.Draft,
            TicketTypes = [new EventTicketTypeDto { Id = ticketTypeId }],
            CapacityPools = [new EventCapacityPoolDto { Id = poolId }]
        };
        var policy = new EventTicketCatalogManagementLinkPolicy();

        var rootLinks = policy.GetLinks(dto, null).ToList();
        var ticketLinks = policy.GetTicketTypeLinks(dto, dto.TicketTypes[0]).ToList();
        var poolLinks = policy.GetCapacityPoolLinks(dto, dto.CapacityPools[0]).ToList();

        await Assert.That(rootLinks.Any(link => link.Rel == LinkRelations.Publish)).IsTrue();
        await Assert.That(rootLinks.Any(link => link.Rel == LinkRelations.CreateTicketType)).IsTrue();
        await Assert.That(rootLinks.Any(link => link.Rel == LinkRelations.CreateCapacityPool)).IsTrue();
        await Assert.That(ticketLinks.Select(link => link.RouteName)).Contains(RouteNames.UpdateEventTicketType);
        await Assert.That(ticketLinks.Select(link => link.RouteName)).Contains(RouteNames.DeleteEventTicketType);
        await Assert.That(poolLinks.Select(link => link.RouteName)).Contains(RouteNames.UpdateEventCapacityPool);
        await Assert.That(poolLinks.Select(link => link.RouteName)).Contains(RouteNames.DeleteEventCapacityPool);
    }

    [Test]
    public async Task PublishedCatalog_OnlyAdvertisesDraftClone()
    {
        var dto = new EventTicketCatalogManagementDto
        {
            EventId = Guid.CreateVersion7(),
            CatalogId = Guid.CreateVersion7(),
            StatusId = (int)TicketCatalogStatusEnum.Published,
            TicketTypes = [new EventTicketTypeDto { Id = Guid.CreateVersion7() }]
        };
        var policy = new EventTicketCatalogManagementLinkPolicy();

        var rootLinks = policy.GetLinks(dto, null).ToList();
        var ticketLinks = policy.GetTicketTypeLinks(dto, dto.TicketTypes[0]).ToList();

        await Assert.That(rootLinks.Any(link => link.RouteName == RouteNames.CloneEventTicketCatalogDraft)).IsTrue();
        await Assert.That(rootLinks.Any(link => link.Rel == LinkRelations.Publish)).IsFalse();
        await Assert.That(ticketLinks).IsEmpty();
    }
}
