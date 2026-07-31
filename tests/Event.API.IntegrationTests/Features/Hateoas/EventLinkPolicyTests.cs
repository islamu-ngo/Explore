// ABOUTME: Unit-level HATEOAS policy tests for event detail affordance metadata.
// ABOUTME: Guards event mutation authorization context and aspect lifecycle affordances.

namespace Event.Api.IntegrationTests.Features.Hateoas;

using System.Security.Claims;
using Explore.API.Hateoas;
using Explore.API.Hateoas.Policies;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Hateoas;
using Explore.Domain.Enums;
using Event.Api.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TUnit.Core;

public sealed class EventLinkPolicyTests
{
    [Test]
    public async Task ManagedCollectionItem_UsesManagementRoutesAndOmitsPublicReports()
    {
        var dto = new EventListDto
        {
            Id = Guid.NewGuid(),
            Title = "Managed event",
            EventTypeFullName = "Conference",
            AudienceGenderFullName = "All",
            AudienceAgeFullName = "All",
            ActorId = Guid.NewGuid(),
            ActorDisplayName = "Organizer",
            ActorTypeFullName = "Organization",
            EventStatusId = (int)EventStatusEnum.Published,
            EventStatusFullName = "Published",
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityTypeFullName = "Public",
            EventFormatFullName = "In person",
            SessionCount = 1,
            IsManagementView = true
        };
        var links = new EventCollectionLinkPolicy()
            .GetItemLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        await Assert.That(links.Single(link => link.Rel == LinkRelations.Self).RouteName)
            .IsEqualTo(RouteNames.GetEventManagementDetails);
        await Assert.That(links.Single(link => link.Rel == "sessions").RouteName)
            .IsEqualTo(RouteNames.GetManagedEventSessionsByEvent);
        await Assert.That(links.Any(link => link.Rel == LinkRelations.EventReportOptions)).IsFalse();
        await Assert.That(links.Any(link => link.Rel == LinkRelations.ReportEvent)).IsFalse();
    }

    [Test]
    public async Task AspectLinks_TransitionFromCreateToEditWhenAspectExists()
    {
        var dto = CreateEventDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var policy = new EventDetailLinkPolicy();

        var createLinks = policy.GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test"))).ToList();

        var createIslamic = createLinks.Single(link => link.Rel == "islamic-aspect:create");
        var createTech = createLinks.Single(link => link.Rel == "tech-aspect:create");
        await Assert.That(createIslamic.RouteName).IsEqualTo(RouteNames.CreateEventIslamicAspect);
        await Assert.That(createIslamic.Method).IsEqualTo(HttpMethods.Post);
        await Assert.That(createTech.RouteName).IsEqualTo(RouteNames.CreateEventTechAspect);
        await Assert.That(createTech.Method).IsEqualTo(HttpMethods.Post);

        dto.AvailableAspects = ["Islamic", "Tech"];
        var editLinks = policy.GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test"))).ToList();

        var editIslamic = editLinks.Single(link => link.Rel == "islamic-aspect:edit");
        var editTech = editLinks.Single(link => link.Rel == "tech-aspect:edit");
        await Assert.That(editIslamic.RouteName).IsEqualTo(RouteNames.UpdateEventIslamicAspect);
        await Assert.That(editIslamic.Method).IsEqualTo(HttpMethods.Patch);
        await Assert.That(editTech.RouteName).IsEqualTo(RouteNames.UpdateEventTechAspect);
        await Assert.That(editTech.Method).IsEqualTo(HttpMethods.Patch);
        await Assert.That(editLinks.Any(link => link.Rel.EndsWith(":create", StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task AddSessionLinks_UseEventSessionPreCreateAuthorizationContext()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var dto = CreateEventDto(eventId, tenantId, Guid.NewGuid());

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        foreach (var rel in new[] { LinkRelations.AddSession, LinkRelations.SessionCreateContext })
        {
            var link = links.Single(definition => definition.Rel == rel);

            await Assert.That(link.PermissionResourceKind).IsEqualTo(ResourceKinds.EventSession);
            await Assert.That(link.PermissionAction).IsEqualTo(AuthorizationActions.Create);
            await Assert.That(link.PermissionResourceId).IsEqualTo(eventId.ToString());
            await Assert.That(link.PermissionScope?.TenantId).IsEqualTo(tenantId.ToString());
            await Assert.That(link.PermissionResourceAttributes).IsNotNull();
            await Assert.That(link.PermissionResourceAttributes!["tenantId"]).IsEqualTo(tenantId.ToString());
            await Assert.That(link.PermissionResourceAttributes["eventId"]).IsEqualTo(eventId.ToString());
            await Assert.That(link.PermissionResourceAttributes["authorizationPhase"]).IsEqualTo(AuthorizationPhases.PreCreate);
        }
    }

    [Test]
    public async Task DraftEventLifecycleLinks_UseEventAuthorizationContext()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var dto = CreateEventDto(eventId, tenantId, organizationId);

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        foreach (var rel in new[] { LinkRelations.Publish, LinkRelations.Cancel, LinkRelations.Archive })
        {
            var link = links.Single(definition => definition.Rel == rel);

            await Assert.That(link.PermissionResourceKind).IsEqualTo(ResourceKinds.Event);
            await Assert.That(link.PermissionAction).IsEqualTo(AuthorizationActions.Update);
            await Assert.That(link.PermissionResourceId).IsEqualTo(eventId.ToString());
            await Assert.That(link.PermissionScope?.TenantId).IsEqualTo(tenantId.ToString());
            await Assert.That(link.PermissionResourceAttributes).IsNotNull();
            await Assert.That(link.PermissionResourceAttributes!["eventId"]).IsEqualTo(eventId.ToString());
            await Assert.That(link.PermissionResourceAttributes["tenantId"]).IsEqualTo(tenantId.ToString());
            await Assert.That(link.PermissionResourceAttributes["organizationId"]).IsEqualTo(organizationId.ToString());
        }
    }

    [Test]
    public async Task DraftUserOwnedEventLifecycleLinks_IncludeUserOwnerAuthorizationContext()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = CreateEventDto(eventId, tenantId, organizationId: null, userId);

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        var publish = links.Single(definition => definition.Rel == LinkRelations.Publish);

        await Assert.That(publish.PermissionResourceAttributes).IsNotNull();
        await Assert.That(publish.PermissionResourceAttributes!["eventId"]).IsEqualTo(eventId.ToString());
        await Assert.That(publish.PermissionResourceAttributes["tenantId"]).IsEqualTo(tenantId.ToString());
        await Assert.That(publish.PermissionResourceAttributes["userId"]).IsEqualTo(userId.ToString());
        await Assert.That(publish.PermissionResourceAttributes.ContainsKey("organizationId")).IsFalse();
    }

    [Test]
    public async Task LightModerationLink_UsesLightModerationAuthorizationAction()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var dto = CreateEventDto(
            eventId,
            tenantId,
            organizationId,
            status: EventStatusEnum.Published,
            statusName: "Published",
            statusCode: "PUBLISHED");

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        var moderate = links.Single(definition => definition.Rel == LinkRelations.ModerateLight);

        await Assert.That(moderate.PermissionResourceKind).IsEqualTo(ResourceKinds.Event);
        await Assert.That(moderate.RouteName).IsEqualTo(RouteNames.ModerateEventLight);
        await Assert.That(moderate.PermissionAction).IsEqualTo(AuthorizationActions.Events.ModerateLight);
        await Assert.That(moderate.PermissionResourceId).IsEqualTo(eventId.ToString());
        await Assert.That(moderate.PermissionResourceAttributes).IsNotNull();
        await Assert.That(moderate.PermissionResourceAttributes!["eventId"]).IsEqualTo(eventId.ToString());
        await Assert.That(moderate.PermissionResourceAttributes["tenantId"]).IsEqualTo(tenantId.ToString());
    }

    [Test]
    public async Task PublishedEvent_AdvertisesHeavyModerationAuthorizationAction()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var dto = CreateEventDto(
            eventId,
            tenantId,
            organizationId,
            status: EventStatusEnum.Published,
            statusName: "Published",
            statusCode: "PUBLISHED");

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        var moderate = links.Single(definition => definition.Rel == LinkRelations.ModerateHeavy);

        await Assert.That(moderate.PermissionResourceKind).IsEqualTo(ResourceKinds.Event);
        await Assert.That(moderate.RouteName).IsEqualTo(RouteNames.ModerateEventHeavy);
        await Assert.That(moderate.PermissionAction).IsEqualTo(AuthorizationActions.Events.ModerateHeavy);
        await Assert.That(moderate.PermissionResourceId).IsEqualTo(eventId.ToString());
        await Assert.That(moderate.PermissionResourceAttributes).IsNotNull();
        await Assert.That(moderate.PermissionResourceAttributes!["eventId"]).IsEqualTo(eventId.ToString());
        await Assert.That(moderate.PermissionResourceAttributes["tenantId"]).IsEqualTo(tenantId.ToString());
    }

    [Test]
    public async Task DraftEvent_DoesNotAdvertiseLightOrUnmoderate()
    {
        var dto = CreateEventDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.ModerateLight)).IsFalse();
        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.Unmoderate)).IsFalse();
    }

    [Test]
    public async Task ModeratedEligibleEvent_AdvertisesUnmoderateAuthorizationAction()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var dto = CreateEventDto(
            eventId,
            tenantId,
            organizationId,
            status: EventStatusEnum.Moderated,
            statusName: "Moderated",
            statusCode: "MODERATED");
        dto.IsUnmoderationEligible = true;

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        var unmoderate = links.Single(definition => definition.Rel == LinkRelations.Unmoderate);

        await Assert.That(unmoderate.PermissionResourceKind).IsEqualTo(ResourceKinds.Event);
        await Assert.That(unmoderate.RouteName).IsEqualTo(RouteNames.UnmoderateEvent);
        await Assert.That(unmoderate.PermissionAction).IsEqualTo(AuthorizationActions.Events.Unmoderate);
        await Assert.That(unmoderate.PermissionResourceId).IsEqualTo(eventId.ToString());
        await Assert.That(unmoderate.PermissionResourceAttributes).IsNotNull();
        await Assert.That(unmoderate.PermissionResourceAttributes!["eventId"]).IsEqualTo(eventId.ToString());
        await Assert.That(unmoderate.PermissionResourceAttributes["tenantId"]).IsEqualTo(tenantId.ToString());
    }

    [Test]
    public async Task ModeratedIneligibleEvent_DoesNotAdvertiseUnmoderate()
    {
        var dto = CreateEventDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: EventStatusEnum.Moderated,
            statusName: "Moderated",
            statusCode: "MODERATED");
        dto.IsUnmoderationEligible = false;

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.Unmoderate)).IsFalse();
    }

    [Test]
    public async Task IrreversiblyModeratedEvent_DoesNotAdvertiseModerationActions()
    {
        var dto = CreateEventDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: EventStatusEnum.Moderated,
            statusName: "Moderated",
            statusCode: "MODERATED");
        dto.IsUnmoderationEligible = false;

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.ModerateLight)).IsFalse();
        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.ModerateHeavy)).IsFalse();
        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.Unmoderate)).IsFalse();
    }

    [Test]
    public async Task PublishedEventLifecycleLinks_ExposeCancelButNotDraftActions()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var dto = CreateEventDto(
            eventId,
            tenantId,
            organizationId,
            status: EventStatusEnum.Published,
            statusName: "Published",
            statusCode: "PUBLISHED");

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.Cancel)).IsTrue();
        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.Publish)).IsFalse();
        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.Archive)).IsFalse();
    }

    [Test]
    public async Task PublishedEvent_AdvertisesReporterFacingReportAffordances()
    {
        var eventId = Guid.NewGuid();
        var dto = CreateEventDto(
            eventId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: EventStatusEnum.Published,
            statusName: "Published",
            statusCode: "PUBLISHED");

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        var options = links.Single(definition => definition.Rel == LinkRelations.EventReportOptions);
        await Assert.That(options.RouteName).IsEqualTo(RouteNames.GetEventReportOptions);
        await Assert.That(new RouteValueDictionary(options.RouteValues)["eventId"]).IsEqualTo(eventId);
        await Assert.That(options.RequiresAuth).IsFalse();

        var submit = links.Single(definition => definition.Rel == LinkRelations.ReportEvent);
        await Assert.That(submit.RouteName).IsEqualTo(RouteNames.SubmitEventReport);
        await Assert.That(submit.Method).IsEqualTo("POST");
        await Assert.That(submit.RequiresAuth).IsTrue();
        await Assert.That(submit.AdvertiseWhenAnonymous).IsTrue();

        var claim = links.Single(definition => definition.Rel == LinkRelations.ClaimEvent);
        await Assert.That(claim.PermissionResourceKind).IsEqualTo(ResourceKinds.EventOrganizerClaim);
        await Assert.That(claim.PermissionResourceId).IsEqualTo(eventId.ToString());
        await Assert.That(claim.PermissionResourceAttributes!["eventId"]).IsEqualTo(eventId.ToString());
        await Assert.That(claim.PermissionResourceAttributes["tenantId"]).IsEqualTo(dto.TenantId.ToString());

        var claims = links.Single(definition => definition.Rel == LinkRelations.OrganizerClaims);
        await Assert.That(claims.PermissionResourceKind).IsEqualTo(ResourceKinds.EventOrganizerClaim);
    }

    [Test]
    public async Task DraftEvent_DoesNotAdvertiseReportAffordances()
    {
        var dto = CreateEventDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.EventReportOptions)).IsFalse();
        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.ReportEvent)).IsFalse();
    }

    [Test]
    public async Task PublishedNonPublicEvent_DoesNotAdvertisePublicActionOrClaimAffordances()
    {
        var dto = CreateEventDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: EventStatusEnum.Published,
            statusName: "Published",
            statusCode: "PUBLISHED");
        dto.VisibilityTypeId = (int)VisibilityTypeEnum.Unlisted;
        dto.VisibilityTypeFullName = "Unlisted";
        dto.VisibilityTypeMasterCode = "UNLISTED";

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        foreach (var relation in new[]
        {
            LinkRelations.PublicActions,
            LinkRelations.ClaimEvent,
            LinkRelations.SuggestCorrection,
            LinkRelations.ReportExternalLink
        })
        {
            await Assert.That(links.Any(definition => definition.Rel == relation)).IsFalse();
        }
    }

    [Test]
    public async Task IneligibleManagementEvent_UsesManagementSelfAndOmitsPublicAffordances()
    {
        var dto = CreateEventDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: EventStatusEnum.Moderated,
            statusName: "Moderated",
            statusCode: "MODERATED");
        dto.IsPubliclyEligible = false;
        dto.IsManagementView = true;
        dto.ParticipationConfiguration = new EventParticipationConfigurationDto
        {
            ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.PlatformManaged
        };

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        var self = links.Single(link => link.Rel == LinkRelations.Self);
        await Assert.That(self.RouteName).IsEqualTo(RouteNames.GetEventManagementDetails);

        foreach (var relation in new[]
        {
            LinkRelations.Collection,
            LinkRelations.Sessions,
            LinkRelations.Program,
            LinkRelations.ProgramSummary,
            LinkRelations.SessionGroups,
            LinkRelations.PublicActions,
            LinkRelations.EventReportOptions,
            LinkRelations.ReportEvent,
            LinkRelations.SuggestCorrection,
            LinkRelations.ReportExternalLink,
            LinkRelations.ClaimEvent,
            LinkRelations.OrganizerClaims,
            LinkRelations.StartRegistration,
            LinkRelations.SignInToRegister,
            LinkRelations.ExternalRegistration,
            "actor",
            "organizer-subscription",
            "subscribe-organizer"
        })
        {
            await Assert.That(links.Any(link => link.Rel == relation)).IsFalse();
        }

        foreach (var relation in new[]
        {
            LinkRelations.Edit,
            LinkRelations.ModerationHistory,
            LinkRelations.ModerationReports,
            LinkRelations.ManagePublicActions,
            LinkRelations.ConfigureParticipation,
            "delete"
        })
        {
            await Assert.That(links.Any(link => link.Rel == relation)).IsTrue();
        }
    }

    [Test]
    public async Task ParticipationConfigurationLinks_UseModeSpecificAffordances()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var organizerActorId = Guid.NewGuid();
        var organizerUserId = Guid.NewGuid();
        var baseDto = CreateEventDto(
            eventId,
            tenantId,
            Guid.NewGuid(),
            provenanceTypeId: (int)EventProvenanceTypeEnum.OrganizerCreated,
            provenanceTypeCode: "ORGANIZER_CREATED",
            organizerActorId: organizerActorId,
            status: EventStatusEnum.Published,
            statusName: "Published",
            statusCode: "PUBLISHED");
        baseDto.OrganizerActorUserId = organizerUserId;

        baseDto.ParticipationConfiguration = new EventParticipationConfigurationDto { ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.InformationOnly };
        var configuredLinks = new EventDetailLinkPolicy()
            .GetLinks(baseDto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        var configure = configuredLinks.Single(link => link.Rel == LinkRelations.ConfigureParticipation);
        await Assert.That(configure.RouteName).IsEqualTo(RouteNames.ConfigureEventParticipation);
        await Assert.That(configure.Method).IsEqualTo(HttpMethods.Patch);
        await Assert.That(configure.RequiresAuth).IsTrue();
        await Assert.That(configure.PermissionAction).IsEqualTo(AuthorizationActions.Events.ManageRegistrations);
        await Assert.That(configure.PermissionResourceKind).IsEqualTo(ResourceKinds.Event);
        await Assert.That(configure.PermissionResourceAttributes!["actorId"]).IsEqualTo(baseDto.ActorId.ToString());
        await Assert.That(configure.PermissionResourceAttributes["organizerActorId"]).IsEqualTo(organizerActorId.ToString());
        await Assert.That(new RouteValueDictionary(configure.RouteValues)["eventId"]).IsEqualTo(eventId);

        foreach (var mode in new[] { ParticipationHandlingModeEnum.InformationOnly, ParticipationHandlingModeEnum.WalkIn })
        {
            baseDto.ParticipationConfiguration = new EventParticipationConfigurationDto { ParticipationHandlingModeId = (int)mode };
            baseDto.PublicActions.Clear();

            var links = new EventDetailLinkPolicy()
                .GetLinks(baseDto, new ClaimsPrincipal(new ClaimsIdentity("test")))
                .ToList();

            await Assert.That(links.Any(link => link.Rel is LinkRelations.ExternalRegistration or LinkRelations.StartRegistration)).IsFalse();
        }

        baseDto.ParticipationConfiguration = new EventParticipationConfigurationDto { ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.ExternalManaged };
        baseDto.PublicActions =
        [
            new EventPublicActionDto
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                EventId = eventId,
                KindId = (int)EventPublicActionKindEnum.ExternalRegistration,
                Url = "https://example.com/secondary",
                DestinationDomain = "example.com",
                SortOrder = 20,
                IsPrimary = false
            },
            new EventPublicActionDto
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                EventId = eventId,
                KindId = (int)EventPublicActionKindEnum.ExternalRegistration,
                Url = "https://example.com/primary",
                DestinationDomain = "example.com",
                SortOrder = 10,
                IsPrimary = true
            },
            new EventPublicActionDto
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                EventId = eventId,
                KindId = (int)EventPublicActionKindEnum.Livestream,
                Url = "https://example.com/live",
                DestinationDomain = "example.com",
                SortOrder = 1,
                IsPrimary = true
            }
        ];

        var externalLinks = new EventDetailLinkPolicy()
            .GetLinks(baseDto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .Where(link => link.Rel == LinkRelations.ExternalRegistration)
            .ToList();

        await Assert.That(externalLinks.Count).IsEqualTo(1);
        var external = externalLinks.Single();
        await Assert.That(external.RouteName).IsEqualTo(RouteNames.RedirectEventPublicAction);
        await Assert.That(external.Method).IsEqualTo(HttpMethods.Get);
        await Assert.That(external.Title).IsEqualTo("Register on organizer website");
        var externalRouteValues = new RouteValueDictionary(external.RouteValues);
        await Assert.That(externalRouteValues["eventId"]).IsEqualTo(eventId);
        await Assert.That(externalRouteValues["actionId"]).IsEqualTo(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        await Assert.That(externalRouteValues["surface"]).IsEqualTo("event_detail");

        baseDto.ProvenanceTypeId = (int)EventProvenanceTypeEnum.Imported;
        baseDto.ProvenanceTypeCode = "IMPORTED";
        baseDto.OrganizerActorId = null;

        var importedLinks = new EventDetailLinkPolicy()
            .GetLinks(baseDto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .Where(link => link.Rel == LinkRelations.ExternalRegistration)
            .ToList();

        await Assert.That(importedLinks.Single().Title).IsEqualTo("View original event page");

        baseDto.OrganizerActorId = organizerActorId;
        var verifiedImportedLink = new EventDetailLinkPolicy()
            .GetLinks(baseDto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .Single(link => link.Rel == LinkRelations.ExternalRegistration);

        await Assert.That(verifiedImportedLink.Title).IsEqualTo("Register on organizer website");

        baseDto.ParticipationConfiguration = new EventParticipationConfigurationDto { ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.PlatformManaged };
        baseDto.PublicActions.Clear();

        var platformLinks = new EventDetailLinkPolicy()
            .GetLinks(baseDto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .Where(link => link.Rel is LinkRelations.StartRegistration or LinkRelations.SignInToRegister)
            .ToList();

        await Assert.That(platformLinks.Count).IsEqualTo(1);
        var platform = platformLinks.Single();
        await Assert.That(platform.Rel).IsEqualTo(LinkRelations.StartRegistration);
        await Assert.That(platform.RouteName).IsEqualTo(RouteNames.StartAuthenticatedRegistrationOrder);
        await Assert.That(platform.Method).IsEqualTo(HttpMethods.Post);
        await Assert.That(platform.RequiresAuth).IsTrue();
        await Assert.That(platform.PermissionAction).IsEqualTo(AuthorizationActions.Create);
        await Assert.That(platform.PermissionResourceKind).IsEqualTo(ResourceKinds.RegistrationOrder);
        await Assert.That(platform.PermissionResourceId).IsEqualTo(eventId.ToString());
        await Assert.That(platform.PermissionScope?.TenantId).IsEqualTo(tenantId.ToString());
        await Assert.That(platform.PermissionResourceAttributes!["eventId"]).IsEqualTo(eventId.ToString());
        await Assert.That(platform.PermissionResourceAttributes["tenantId"]).IsEqualTo(tenantId.ToString());
        await Assert.That(platform.PermissionResourceAttributes["organizerUserId"]).IsEqualTo(organizerUserId.ToString());
        await Assert.That(platformLinks.Any(link => link.Rel == LinkRelations.SignInToRegister)).IsFalse();
    }

    [Test]
    [Category(TestCategories.Phase43Ticketing)]
    public async Task TicketManagementLinks_ArePlatformManagedAndSeparate()
    {
        var dto = CreateEventDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var policy = new EventDetailLinkPolicy();

        dto.ParticipationConfiguration = new EventParticipationConfigurationDto
        {
            ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.ExternalManaged
        };
        dto.IsManagementView = true;
        var externalLinks = policy.GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test"))).ToList();
        await Assert.That(externalLinks.Any(link => link.Rel == LinkRelations.ManageTicketTypes || link.Rel == LinkRelations.ManageCapacityPools)).IsFalse();
        await Assert.That(externalLinks.Any(link => link.Rel == LinkRelations.ViewRegistrationOrders)).IsFalse();

        dto.ParticipationConfiguration = new EventParticipationConfigurationDto
        {
            ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.InformationOnly
        };
        var listingOnlyLinks = policy.GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test"))).ToList();
        await Assert.That(listingOnlyLinks.Any(link => link.Rel == LinkRelations.ManageTicketTypes || link.Rel == LinkRelations.ManageCapacityPools)).IsFalse();

        dto.ParticipationConfiguration = new EventParticipationConfigurationDto
        {
            ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.PlatformManaged
        };
        var platformLinks = policy.GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test"))).ToList();
        foreach (var rel in new[] { LinkRelations.ManageTicketTypes, LinkRelations.ManageCapacityPools })
        {
            var link = platformLinks.Single(link => link.Rel == rel);
            await Assert.That(link.RouteName).IsEqualTo(RouteNames.GetEventTicketCatalogManagement);
            await Assert.That(link.PermissionAction).IsEqualTo(AuthorizationActions.Events.ManageTickets);
            await Assert.That(link.PermissionResourceKind).IsEqualTo(ResourceKinds.Event);
            await Assert.That(link.PermissionResourceId).IsEqualTo(dto.Id.ToString());
            await Assert.That(link.PermissionScope?.TenantId).IsEqualTo(dto.TenantId.ToString());
            await Assert.That(link.PermissionResourceAttributes!["eventId"]).IsEqualTo(dto.Id.ToString());
            await Assert.That(link.PermissionResourceAttributes["tenantId"]).IsEqualTo(dto.TenantId.ToString());
        }

        var orders = platformLinks.Single(link => link.Rel == LinkRelations.ViewRegistrationOrders);
        await Assert.That(orders.RouteName).IsEqualTo(RouteNames.GetEventRegistrationOrders);
        await Assert.That(new RouteValueDictionary(orders.RouteValues)["eventId"]).IsEqualTo(dto.Id);
        await Assert.That(orders.PermissionAction).IsEqualTo(AuthorizationActions.Events.ManageRegistrations);
        await Assert.That(orders.PermissionResourceKind).IsEqualTo(ResourceKinds.Event);
    }

    [Test]
    public async Task RegistrationOrderLink_UsesTheEventScopedOrganizerCollectionRoute()
    {
        var dto = CreateEventDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: EventStatusEnum.Published,
            statusName: "Published",
            statusCode: "PUBLISHED");
        dto.IsManagementView = true;
        dto.ParticipationConfiguration = new EventParticipationConfigurationDto
        {
            ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.PlatformManaged
        };

        var link = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .Single(definition => definition.Rel == LinkRelations.ViewRegistrationOrders);

        await Assert.That(link.RouteName).IsEqualTo("GetEventRegistrationOrders");
        var routeValues = new RouteValueDictionary(link.RouteValues);
        await Assert.That(routeValues["eventId"]).IsEqualTo(dto.Id);
        await Assert.That(routeValues.ContainsKey("actorId")).IsFalse();
        await Assert.That(link.PermissionAction).IsEqualTo(AuthorizationActions.Events.ManageRegistrations);
    }

    [Test]
    public async Task PlatformManagedParticipation_AnonymousUsesSignInDiscoveryOnProtectedRegistrationRoute()
    {
        var eventId = Guid.NewGuid();
        var dto = CreateEventDto(
            eventId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: EventStatusEnum.Published,
            statusName: "Published",
            statusCode: "PUBLISHED");
        dto.ParticipationConfiguration = new EventParticipationConfigurationDto
        {
            ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.PlatformManaged
        };

        var participationLinks = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity()))
            .Where(link => link.Rel is LinkRelations.StartRegistration or LinkRelations.SignInToRegister)
            .ToList();

        var signIn = participationLinks.Single();
        await Assert.That(signIn.Rel).IsEqualTo(LinkRelations.SignInToRegister);
        await Assert.That(signIn.RouteName).IsEqualTo(RouteNames.StartAuthenticatedRegistrationOrder);
        await Assert.That(signIn.Method).IsEqualTo(HttpMethods.Post);
        await Assert.That(signIn.RequiresAuth).IsTrue();
        await Assert.That(signIn.AdvertiseWhenAnonymous).IsTrue();
        await Assert.That(signIn.PermissionResourceKind).IsNull();
        await Assert.That(participationLinks.Any(link => link.Rel == LinkRelations.StartRegistration)).IsFalse();
    }

    [Test]
    public async Task PlatformManagedGuestMode_UsesGuestOrderRouteWithoutAnAccountCapability()
    {
        var dto = CreateEventDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: EventStatusEnum.Published,
            statusName: "Published",
            statusCode: "PUBLISHED");
        dto.ParticipationConfiguration = new EventParticipationConfigurationDto
        {
            ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.PlatformManaged,
            IdentityAccessModeId = (int)IdentityAccessModeEnum.GuestAllowed
        };

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity()))
            .ToList();

        var guestStart = links.Single(link => link.Rel == LinkRelations.StartGuestRegistration);
        await Assert.That(guestStart.RouteName).IsEqualTo(RouteNames.StartGuestRegistrationOrder);
        await Assert.That(guestStart.Method).IsEqualTo(HttpMethods.Post);
        await Assert.That(guestStart.RequiresAuth).IsFalse();
        await Assert.That(guestStart.PermissionResourceKind).IsNull();
        await Assert.That(links.Any(link => link.Rel == LinkRelations.SignInToRegister)).IsFalse();
    }

    [Test]
    [Arguments(EventStatusEnum.Draft, VisibilityTypeEnum.Public)]
    [Arguments(EventStatusEnum.Published, VisibilityTypeEnum.Private)]
    [Arguments(EventStatusEnum.Published, VisibilityTypeEnum.Unlisted)]
    [Arguments(EventStatusEnum.Moderated, VisibilityTypeEnum.Public)]
    public async Task NonPublicParticipationState_DoesNotAdvertiseParticipationActions(
        EventStatusEnum status,
        VisibilityTypeEnum visibility)
    {
        var dto = CreateEventDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: status,
            statusName: status.ToString(),
            statusCode: status.ToString().ToUpperInvariant());
        dto.VisibilityTypeId = (int)visibility;
        dto.PublicActions =
        [
            new EventPublicActionDto
            {
                Id = Guid.NewGuid(),
                EventId = dto.Id,
                KindId = (int)EventPublicActionKindEnum.ExternalRegistration,
                Url = "https://example.com/register",
                DestinationDomain = "example.com",
                IsPrimary = true
            }
        ];

        foreach (var mode in new[]
        {
            ParticipationHandlingModeEnum.PlatformManaged,
            ParticipationHandlingModeEnum.ExternalManaged
        })
        {
            dto.ParticipationConfiguration = new EventParticipationConfigurationDto
            {
                ParticipationHandlingModeId = (int)mode
            };
            var links = new EventDetailLinkPolicy()
                .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
                .ToList();

            foreach (var relation in new[]
            {
                LinkRelations.StartRegistration,
                LinkRelations.SignInToRegister,
                LinkRelations.ExternalRegistration
            })
            {
                await Assert.That(links.Any(link => link.Rel == relation)).IsFalse();
            }
        }
    }

    private static EventDto CreateEventDto(
        Guid eventId,
        Guid tenantId,
        Guid? organizationId,
        Guid? userId = null,
        int provenanceTypeId = 0,
        string? provenanceTypeCode = null,
        Guid? organizerActorId = null,
        EventStatusEnum status = EventStatusEnum.Draft,
        string statusName = "Draft",
        string statusCode = "DRAFT") => new()
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Program launch",
            ActorId = Guid.NewGuid(),
            ActorDisplayName = "ISLAMU",
            ActorTypeId = userId.HasValue ? (int)ActorTypeEnum.User : (int)ActorTypeEnum.Organization,
            ActorTypeFullName = userId.HasValue ? "User" : "Organization",
            ActorUserId = userId,
            ActorOrganizationId = organizationId,
            ProvenanceTypeId = provenanceTypeId,
            ProvenanceTypeCode = provenanceTypeCode,
            OrganizerActorId = organizerActorId,
            EventStatusId = (int)status,
            EventStatusFullName = statusName,
            EventStatusMasterCode = statusCode,
            IsPubliclyEligible = true,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityTypeFullName = "Public",
            VisibilityTypeMasterCode = "PUBLIC",
            EventFormatId = (int)EventFormatEnum.Local,
            EventFormatFullName = "In person",
            EventFormatMasterCode = "IN_PERSON"
        };
}
