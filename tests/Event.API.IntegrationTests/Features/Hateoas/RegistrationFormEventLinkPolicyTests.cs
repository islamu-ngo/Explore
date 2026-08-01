// ABOUTME: Verifies the event-level registration-workflow management affordance.
// ABOUTME: Guards its exact route, purpose, and tenant-scoped authorization metadata.

using System.Security.Claims;
using Explore.API.Hateoas;
using Explore.API.Hateoas.Policies;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Hateoas;
using Explore.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

public sealed class RegistrationFormEventLinkPolicyTests
{
    [Test]
    public async Task ManagementEvent_AdvertisesTenantScopedRegistrationWorkflow()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        var dto = new EventDto
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Managed event",
            ActorId = Guid.CreateVersion7(),
            ActorDisplayName = "Organizer",
            ActorTypeId = (int)ActorTypeEnum.Organization,
            ActorTypeFullName = "Organization",
            EventStatusId = (int)EventStatusEnum.Draft,
            EventStatusFullName = "Draft",
            EventStatusMasterCode = "DRAFT",
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityTypeFullName = "Public",
            VisibilityTypeMasterCode = "PUBLIC",
            EventFormatId = (int)EventFormatEnum.Local,
            EventFormatFullName = "In person",
            EventFormatMasterCode = "IN_PERSON",
            IsManagementView = true
        };

        LinkDefinition link = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .Single(candidate => candidate.Rel == LinkRelations.ManageRegistrationWorkflow);
        var routeValues = new RouteValueDictionary(link.RouteValues);

        await Assert.That(link.RouteName).IsEqualTo(RouteNames.GetRegistrationWorkflow);
        await Assert.That(link.Method).IsEqualTo(HttpMethods.Get);
        await Assert.That(routeValues["eventId"]).IsEqualTo(eventId);
        await Assert.That(routeValues["purpose"]).IsEqualTo("registration");
        await Assert.That(link.PermissionAction).IsEqualTo(AuthorizationActions.Events.ManageRegistrationWorkflow);
        await Assert.That(link.PermissionResourceKind).IsEqualTo(ResourceKinds.Event);
        await Assert.That(link.PermissionResourceId).IsEqualTo(eventId.ToString());
        await Assert.That(link.PermissionScope?.TenantId).IsEqualTo(tenantId.ToString());
        await Assert.That(link.PermissionResourceAttributes!["eventId"]).IsEqualTo(eventId.ToString());
        await Assert.That(link.PermissionResourceAttributes["tenantId"]).IsEqualTo(tenantId.ToString());
    }
}
