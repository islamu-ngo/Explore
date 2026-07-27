// ABOUTME: Unit tests for AuthorizationBehavior MediatR pipeline behavior.
// ABOUTME: Verifies authorization enforcement via IAuthorizedRequest (legacy), [AuthorizeResource], and pass-through.

#pragma warning disable CS0618 // IAuthorizedRequest is obsolete — tests must still exercise the legacy code path

using Explore.Application.Authorization;
using Explore.Application.Behaviors;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventCategories;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.DTOs.EventOrganizerClaim;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.DTOs.EventSessionGroup;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.DTOs.EventTags;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventCustomPropertyProjections.Requests.Queries;
using Explore.Application.Features.EventCategories.Requests.Commands;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Features.EventOrganizerClaims.Requests.Commands;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Commands;
using Explore.Application.Features.EventSessionGroups.Requests.Commands;
using Explore.Application.Features.EventSessionSpeakers.Requests.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Queries;
using Explore.Application.Features.EventTags.Requests.Commands;
using Explore.Application.Features.OrganizationMembers.Requests.Queries;
using Explore.Application.Features.Organizations.Requests.Commands;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Behaviors;

public class AuthorizationBehaviorTests
{
    private readonly IAuthorizationProvider _authService;
    private readonly ILogger<AuthorizationBehavior<TestAuthorizedCommand, BaseCommandResponse<Guid>>> _logger;

    public AuthorizationBehaviorTests()
    {
        _authService = Substitute.For<IAuthorizationProvider>();
        _logger = Substitute.For<ILogger<AuthorizationBehavior<TestAuthorizedCommand, BaseCommandResponse<Guid>>>>();
    }

    [Test]
    public async Task Handle_WithIAuthorizedRequest_WhenAllowed_CallsNext()
    {
        // Arrange
        var behavior = new AuthorizationBehavior<TestAuthorizedCommand, BaseCommandResponse<Guid>>(_authService, _logger);
        var command = new TestAuthorizedCommand();
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true, Id = Guid.NewGuid() };

        _authService.IsAllowedAsync(
            "islamuevent_tenant_setting", "test-resource", "update",
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await behavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(expectedResponse.Id);
    }

    [Test]
    public async Task Handle_WithIAuthorizedRequest_WhenDenied_ThrowsAuthorizationException()
    {
        // Arrange
        var behavior = new AuthorizationBehavior<TestAuthorizedCommand, BaseCommandResponse<Guid>>(_authService, _logger);
        var command = new TestAuthorizedCommand();

        _authService.IsAllowedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>())
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(async () =>
            await behavior.Handle(command, _ => Task.FromResult(new BaseCommandResponse<Guid>()), CancellationToken.None));
    }

    [Test]
    public async Task Handle_WithAuthorizeResourceAttribute_WhenAllowed_CallsNext()
    {
        // Arrange
        var attrBehavior = new AuthorizationBehavior<TestAttributeCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestAttributeCommand, BaseCommandResponse<Guid>>>>());
        var command = new TestAttributeCommand();
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        _authService.IsAllowedAsync(
            "islamuevent_instance_setting", Arg.Any<string>(), "update",
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await attrBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task Handle_WithAuthorizeResourceAttribute_WhenDenied_ThrowsAuthorizationException()
    {
        // Arrange
        var attrBehavior = new AuthorizationBehavior<TestAttributeCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestAttributeCommand, BaseCommandResponse<Guid>>>>());
        var command = new TestAttributeCommand();

        _authService.IsAllowedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>())
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(async () =>
            await attrBehavior.Handle(command, _ => Task.FromResult(new BaseCommandResponse<Guid>()), CancellationToken.None));
    }

    [Test]
    public async Task Handle_WithNoAuthRequirement_PassesThrough()
    {
        // Arrange
        var plainBehavior = new AuthorizationBehavior<TestPlainCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestPlainCommand, BaseCommandResponse<Guid>>>>());
        var command = new TestPlainCommand();
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        // Act
        var result = await plainBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _authService.DidNotReceive().IsAllowedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithAuthorizeResourceAndISecureRequest_WhenAllowed_UsesResourceIdFromRequest()
    {
        // Arrange
        var secureBehavior = new AuthorizationBehavior<TestSecureCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestSecureCommand, BaseCommandResponse<Guid>>>>());
        var command = new TestSecureCommand();
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        _authService.IsAllowedAsync(
            "islamuevent_organization", command.OrganizationId.ToString(), "update",
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _authService.Received(1).IsAllowedAsync(
            "islamuevent_organization",
            command.OrganizationId.ToString(),
            "update",
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithAuthorizeResourceAndISecureRequest_WhenDenied_ThrowsAuthorizationException()
    {
        // Arrange
        var secureBehavior = new AuthorizationBehavior<TestSecureCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestSecureCommand, BaseCommandResponse<Guid>>>>());
        var command = new TestSecureCommand();

        _authService.IsAllowedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>())
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(async () =>
            await secureBehavior.Handle(command, _ => Task.FromResult(new BaseCommandResponse<Guid>()), CancellationToken.None));
    }

    [Test]
    public async Task Handle_WithAuthorizeResourceAndISecureRequest_PassesResourceAttributes()
    {
        // Arrange
        var secureBehavior = new AuthorizationBehavior<TestSecureCommandWithAttributes, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestSecureCommandWithAttributes, BaseCommandResponse<Guid>>>>());
        var command = new TestSecureCommandWithAttributes();
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        _authService.IsAllowedAsync(
            "islamuevent_organization", command.OrganizationId.ToString(), "delete",
            Arg.Is<IDictionary<string, object>?>(d => d != null && d.ContainsKey("tenantId")),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _authService.Received(1).IsAllowedAsync(
            "islamuevent_organization",
            command.OrganizationId.ToString(),
            "delete",
            Arg.Is<IDictionary<string, object>?>(d => d != null && d.ContainsKey("tenantId")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithCreateEventCommand_UsesPreCreateResourceContext()
    {
        var secureBehavior = new AuthorizationBehavior<CreateEventCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<CreateEventCommand, BaseCommandResponse<Guid>>>>());
        var organizationId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            Request = new CreateEventRequest
            {
                Title = "Community Dinner",
            ParticipationConfiguration = new ConfigureEventParticipationDto
                {
                    ParticipationHandlingModeId = 1,
                    AdvanceRegistrationObligationId = 1
                },
                OrganizationId = organizationId,
                GroupId = groupId
            }
        };
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        _authService.IsAllowedAsync(
                ResourceKinds.Event,
                CreateEventCommand.PreCreateResourceId,
                AuthorizationActions.Create,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    attributes != null
                    && attributes["authorizationPhase"].Equals(CreateEventCommand.PreCreateAuthorizationPhase)
                    && attributes["organizationId"].Equals(organizationId.ToString())
                    && attributes["groupId"].Equals(groupId.ToString())),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _authService.Received(1).IsAllowedAsync(
            ResourceKinds.Event,
            CreateEventCommand.PreCreateResourceId,
            AuthorizationActions.Create,
            Arg.Is<IDictionary<string, object>?>(attributes =>
                attributes != null
                && attributes["authorizationPhase"].Equals(CreateEventCommand.PreCreateAuthorizationPhase)
                && attributes["organizationId"].Equals(organizationId.ToString())
                && attributes["groupId"].Equals(groupId.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithCreateOrganizationCommand_UsesPreCreateResourceContext()
    {
        var secureBehavior = new AuthorizationBehavior<CreateOrganizationCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<CreateOrganizationCommand, BaseCommandResponse<Guid>>>>());
        var command = new CreateOrganizationCommand
        {
            CreatorUserId = Guid.NewGuid(),
            OrganizationDto = new()
            {
                FullName = "Community Organization",
                Email = "org@example.com",
                Address = "1 Main Street",
                City = "Brussels",
                Country = "Belgium",
                Postcode = 1000
            }
        };
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        _authService.IsAllowedAsync(
                ResourceKinds.Organization,
                CreateOrganizationCommand.PreCreateResourceId,
                AuthorizationActions.Create,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    attributes != null
                    && attributes["authorizationPhase"].Equals(CreateOrganizationCommand.PreCreateAuthorizationPhase)),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _authService.Received(1).IsAllowedAsync(
            ResourceKinds.Organization,
            CreateOrganizationCommand.PreCreateResourceId,
            AuthorizationActions.Create,
            Arg.Is<IDictionary<string, object>?>(attributes =>
                attributes != null
                && attributes["authorizationPhase"].Equals(CreateOrganizationCommand.PreCreateAuthorizationPhase)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithPublishEventCommand_PassesEventIdResourceAttribute()
    {
        var secureBehavior = new AuthorizationBehavior<PublishEventCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<PublishEventCommand, BaseCommandResponse<Guid>>>>());
        var eventId = Guid.NewGuid();
        var command = new PublishEventCommand
        {
            Id = eventId,
            Request = new PublishEventRequestDto { ExpectedConcurrencyStamp = Guid.NewGuid() }
        };
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        _authService.IsAllowedAsync(
                ResourceKinds.Event,
                eventId.ToString(),
                AuthorizationActions.Update,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    attributes != null && attributes["eventId"].Equals(eventId.ToString())),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _authService.Received(1).IsAllowedAsync(
            ResourceKinds.Event,
            eventId.ToString(),
            AuthorizationActions.Update,
            Arg.Is<IDictionary<string, object>?>(attributes =>
                attributes != null && attributes["eventId"].Equals(eventId.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithEventResource_EnrichesMissingEventAuthorizationContext()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var secureBehavior = new AuthorizationBehavior<UpdateEventCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventCommand, BaseCommandResponse<Guid>>>>(),
            eventRepository);
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var organizerActorId = Guid.NewGuid();
        var organizerUserId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var command = new UpdateEventCommand
        {
            EventId = eventId,
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            UpdateEventDto = new UpdateEventDto
            {
                Title = new UpdateEventTitleDto { Value = "Test" }
            }
        };
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        eventRepository.GetEventWithDetails(eventId).Returns(new Explore.Domain.Event
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Community Program",
            ActorId = actorId,
            OrganizerActorId = organizerActorId,
            OrganizerActor = new Actor
            {
                Id = organizerActorId,
                UserId = organizerUserId,
                ActorTypeId = 1,
                ActorType = null!,
                Pii = new ActorPii { DisplayName = "Verified organizer" }
            },
            Actor = new Actor
            {
                Id = actorId,
                OrganizationId = organizationId,
                ActorTypeId = 2,
                ActorType = null!,
                Pii = new ActorPii { DisplayName = "ISLAMU" }
            },
            Tenant = null!,
            EventStatus = null!,
            EventFormat = null!,
            VisibilityType = null!
        });
        _authService.IsAllowedAsync(
                ResourceKinds.Event,
                eventId.ToString(),
                AuthorizationActions.Update,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    attributes != null
                    && attributes["eventId"].Equals(eventId.ToString())
                    && attributes["tenantId"].Equals(tenantId.ToString())
                    && attributes["actorId"].Equals(actorId.ToString())
                    && attributes["organizerActorId"].Equals(organizerActorId.ToString())
                    && attributes["organizerUserId"].Equals(organizerUserId.ToString())
                    && attributes["organizationId"].Equals(organizationId.ToString())),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await eventRepository.Received(1).GetEventWithDetails(eventId);
    }

    [Test]
    public async Task Handle_WithEventSessionResource_EnrichesMissingEventAuthorizationContext()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var secureBehavior = new AuthorizationBehavior<TestEventSessionSecureCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestEventSessionSecureCommand, BaseCommandResponse<Guid>>>>(),
            eventSessionRepository: eventSessionRepository);
        var eventSessionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new TestEventSessionSecureCommand(eventSessionId);
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        eventSessionRepository.GetSessionWithDetails(eventSessionId).Returns(new EventSession
        {
            Id = eventSessionId,
            EventId = eventId,
            TenantId = tenantId,
            Event = null!,
            Tenant = null!
        });
        _authService.IsAllowedAsync(
                ResourceKinds.EventSession,
                eventSessionId.ToString(),
                AuthorizationActions.Update,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    attributes != null
                    && attributes["eventSessionId"].Equals(eventSessionId.ToString())
                    && attributes["eventId"].Equals(eventId.ToString())
                    && attributes["tenantId"].Equals(tenantId.ToString())),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await eventSessionRepository.Received(1).GetSessionWithDetails(eventSessionId);
    }

    [Test]
    public async Task Handle_WithDeleteEventRegistration_EnrichesPersistedRegistrationAuthorizationContext()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var registrationRepository = Substitute.For<IEventRegistrationRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var registrationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var eventSessionId = Guid.NewGuid();
        var attendeeUserId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        tenantContext.TenantId.Returns(tenantId);
        registrationRepository.GetByIdWithDetails(registrationId, Arg.Any<CancellationToken>())
            .Returns(CreateRegistration(registrationId, tenantId, eventId, eventSessionId, attendeeUserId));
        eventRepository.GetEventWithDetails(eventId)
            .Returns(CreateAuthorizationEvent(eventId, tenantId, organizationId));

        var behavior = new AuthorizationBehavior<DeleteEventRegistrationCommand, bool>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<DeleteEventRegistrationCommand, bool>>>(),
            eventRepository: eventRepository,
            eventRegistrationRepository: registrationRepository,
            tenantContext: tenantContext);
        var command = new DeleteEventRegistrationCommand { Id = registrationId };

        _authService.IsAllowedAsync(
                ResourceKinds.EventRegistration,
                registrationId.ToString(),
                AuthorizationActions.Delete,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    HasRegistrationAuthorizationContext(
                        attributes,
                        tenantId,
                        eventId,
                        eventSessionId,
                        attendeeUserId,
                        organizationId)),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await behavior.Handle(command, _ => Task.FromResult(true), CancellationToken.None);

        await Assert.That(result).IsTrue();
        await Assert.That(command.ExpectedOwnerUserId).IsEqualTo(attendeeUserId);
        await registrationRepository.Received(1)
            .GetByIdWithDetails(registrationId, Arg.Any<CancellationToken>());
        await eventRepository.Received(1).GetEventWithDetails(eventId);
    }

    [Test]
    public async Task Handle_WithUpdateEventRegistration_EnrichesPersistedRegistrationAuthorizationContext()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var registrationRepository = Substitute.For<IEventRegistrationRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var registrationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var eventSessionId = Guid.NewGuid();
        var attendeeUserId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        tenantContext.TenantId.Returns(tenantId);
        registrationRepository.GetByIdWithDetails(registrationId, Arg.Any<CancellationToken>())
            .Returns(CreateRegistration(registrationId, tenantId, eventId, eventSessionId, attendeeUserId));
        eventRepository.GetEventWithDetails(eventId)
            .Returns(CreateAuthorizationEvent(eventId, tenantId, organizationId));

        var behavior = new AuthorizationBehavior<UpdateEventRegistrationCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventRegistrationCommand, BaseCommandResponse<Guid>>>>(),
            eventRepository: eventRepository,
            eventRegistrationRepository: registrationRepository,
            tenantContext: tenantContext);
        var command = new UpdateEventRegistrationCommand
        {
            EventRegistrationId = registrationId,
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            EventRegistrationDto = new UpdateEventRegistrationDto()
        };
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true, Id = registrationId };

        _authService.IsAllowedAsync(
                ResourceKinds.EventRegistration,
                registrationId.ToString(),
                AuthorizationActions.Update,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    HasRegistrationAuthorizationContext(
                        attributes,
                        tenantId,
                        eventId,
                        eventSessionId,
                        attendeeUserId,
                        organizationId)),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await behavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await registrationRepository.Received(1)
            .GetByIdWithDetails(registrationId, Arg.Any<CancellationToken>());
        await eventRepository.Received(1).GetEventWithDetails(eventId);
    }

    [Test]
    public async Task Handle_WithEventRegistrationHiddenByTenantBoundary_DeniesBeforeHandler()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var registrationRepository = Substitute.For<IEventRegistrationRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var registrationId = Guid.NewGuid();
        var persistedTenantId = Guid.NewGuid();
        tenantContext.TenantId.Returns(Guid.NewGuid());
        registrationRepository.GetByIdWithDetails(registrationId, Arg.Any<CancellationToken>())
            .Returns(CreateRegistration(
                registrationId,
                persistedTenantId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()));

        var behavior = new AuthorizationBehavior<DeleteEventRegistrationCommand, bool>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<DeleteEventRegistrationCommand, bool>>>(),
            eventRepository: eventRepository,
            eventRegistrationRepository: registrationRepository,
            tenantContext: tenantContext);
        var handlerCalled = false;

        await Assert.ThrowsAsync<AuthorizationException>(() => behavior.Handle(
            new DeleteEventRegistrationCommand { Id = registrationId },
            _ =>
            {
                handlerCalled = true;
                return Task.FromResult(true);
            },
            CancellationToken.None));

        await Assert.That(handlerCalled).IsFalse();
        await _authService.DidNotReceive().IsAllowedAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
        await eventRepository.DidNotReceive().GetEventWithDetails(Arg.Any<Guid>());
    }

    [Test]
    public async Task Handle_WithOrganizationMemberResource_EnrichesMissingMemberAuthorizationContext()
    {
        var memberRepository = Substitute.For<IOrganizationMemberRepository>();
        var secureBehavior = new AuthorizationBehavior<GetOrganizationMemberDetailsRequest, OrganizationMemberDto?>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<GetOrganizationMemberDetailsRequest, OrganizationMemberDto?>>>(),
            organizationMemberRepository: memberRepository);
        var memberId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new GetOrganizationMemberDetailsRequest
        {
            Id = memberId,
            TenantId = tenantId
        };
        var expectedResponse = new OrganizationMemberDto { Id = memberId };

        memberRepository.GetOrganizationMemberWithDetails(memberId).Returns(new OrganizationMember
        {
            Id = memberId,
            TenantId = tenantId,
            Tenant = null!,
            OrganizationTenantId = Guid.NewGuid(),
            OrganizationTenant = new OrganizationTenant
            {
                OrganizationId = organizationId,
                Organization = new Organization { Id = organizationId, Pii = new OrganizationPii { FullName = "Organization" } },
                TenantId = tenantId,
                Tenant = null!,
                ApprovalStatusId = 1,
                ApprovalStatus = null!
            },
            UserId = userId,
            User = null!,
            RoleId = 2,
            Role = null!
        });
        _authService.IsAllowedAsync(
                ResourceKinds.OrganizationMember,
                memberId.ToString(),
                AuthorizationActions.OrganizationMembers.View,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    attributes != null
                    && attributes["tenantId"].Equals(tenantId.ToString())
                    && attributes["organizationId"].Equals(organizationId.ToString())
                    && attributes["userId"].Equals(userId.ToString())),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await secureBehavior.Handle(request, _ => Task.FromResult<OrganizationMemberDto?>(expectedResponse), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await memberRepository.Received(1).GetOrganizationMemberWithDetails(memberId);
    }

    [Test]
    public async Task Handle_WithStorageObjectResource_EnrichesPersistedStorageAuthorizationContext()
    {
        var storageObjectRepository = Substitute.For<IStorageObjectRepository>();
        var secureBehavior = new AuthorizationBehavior<UpdateStorageObjectCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateStorageObjectCommand, BaseCommandResponse<Guid>>>>(),
            storageObjectRepository: storageObjectRepository);
        var storageObjectId = Guid.NewGuid();
        var persistedTenantId = Guid.NewGuid();
        var clientTenantId = Guid.NewGuid();
        var command = new UpdateStorageObjectCommand
        {
            StorageObjectDto = new UpdateStorageObjectDto
            {
                Id = storageObjectId,
                TenantId = clientTenantId,
                FileTypeId = 1,
                Uri = "/api/storageobject/018f0000-0000-7000-8000-000000000001/content",
                ObjectKey = "tenants/current/file.png",
                Provider = StorageProviders.Local,
                FullName = "file.png",
                SafeDisplayName = "file.png",
                Extension = "png",
                ContentType = "image/png",
                Size = 1024,
                Visibility = StorageObjectVisibilities.PublicImage,
                Purpose = StorageObjectPurposes.LegacyImage,
                LifecycleState = StorageObjectLifecycleStates.Active
            }
        };
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        storageObjectRepository.GetById(storageObjectId).Returns(new StorageObject
        {
            Id = storageObjectId,
            TenantId = persistedTenantId,
            FileTypeId = 1,
            FileType = null!,
            Tenant = null!,
            Uri = "/api/storageobject/018f0000-0000-7000-8000-000000000001/content",
            ObjectKey = "tenants/current/file.png",
            Provider = StorageProviders.Local,
            FullName = "file.png",
            SafeDisplayName = "file.png",
            Extension = "png",
            ContentType = "image/png",
            Size = 1024,
            Visibility = StorageObjectVisibilities.PublicImage,
            Purpose = StorageObjectPurposes.LegacyImage,
            LifecycleState = StorageObjectLifecycleStates.Active
        });
        _authService.IsAllowedAsync(
                ResourceKinds.StorageObject,
                storageObjectId.ToString(),
                AuthorizationActions.Update,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    attributes != null
                    && attributes["storageObjectId"].Equals(storageObjectId.ToString("D"))
                    && attributes["tenantId"].Equals(persistedTenantId.ToString("D"))
                    && !attributes["tenantId"].Equals(clientTenantId.ToString("D"))),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await storageObjectRepository.Received(1).GetById(storageObjectId);
    }

    [Test]
    public async Task Handle_WithCustomPropertyProjectionEventResource_EnrichesTenantAuthorizationContext()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var secureBehavior = new AuthorizationBehavior<GetEventCustomPropertyProjectionsForEventQuery, BaseCommandResponse<IReadOnlyList<Explore.Application.DTOs.CustomPropertyProjection.EventCustomPropertyProjectionDto>>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<GetEventCustomPropertyProjectionsForEventQuery, BaseCommandResponse<IReadOnlyList<Explore.Application.DTOs.CustomPropertyProjection.EventCustomPropertyProjectionDto>>>>>(),
            eventRepository);
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = new GetEventCustomPropertyProjectionsForEventQuery { EventId = eventId };
        var expectedResponse = new BaseCommandResponse<IReadOnlyList<Explore.Application.DTOs.CustomPropertyProjection.EventCustomPropertyProjectionDto>>
        {
            Success = true,
            Id = []
        };

        eventRepository.GetEventWithDetails(eventId).Returns(new Explore.Domain.Event
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Community Program",
            ActorId = Guid.NewGuid(),
            Actor = null!,
            Tenant = null!,
            EventStatus = null!,
            EventFormat = null!,
            VisibilityType = null!
        });
        _authService.IsAllowedAsync(
                ResourceKinds.CustomPropertyProjection,
                eventId.ToString("D"),
                AuthorizationActions.CustomPropertyProjections.View,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    attributes != null
                    && attributes["eventId"].Equals(eventId.ToString("D"))
                    && attributes["tenantId"].Equals(tenantId.ToString("D"))),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await secureBehavior.Handle(request, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await eventRepository.Received(1).GetEventWithDetails(eventId);
    }

    [Test]
    public async Task Handle_WithCustomPropertyProjectionSessionResource_EnrichesTenantAuthorizationContext()
    {
        var sessionRepository = Substitute.For<IEventSessionRepository>();
        var secureBehavior = new AuthorizationBehavior<GetEventSessionCustomPropertyProjectionsForSessionQuery, BaseCommandResponse<IReadOnlyList<Explore.Application.DTOs.CustomPropertyProjection.EventSessionCustomPropertyProjectionDto>>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<GetEventSessionCustomPropertyProjectionsForSessionQuery, BaseCommandResponse<IReadOnlyList<Explore.Application.DTOs.CustomPropertyProjection.EventSessionCustomPropertyProjectionDto>>>>>(),
            eventSessionRepository: sessionRepository);
        var eventId = Guid.NewGuid();
        var eventSessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = new GetEventSessionCustomPropertyProjectionsForSessionQuery { EventSessionId = eventSessionId };
        var expectedResponse = new BaseCommandResponse<IReadOnlyList<Explore.Application.DTOs.CustomPropertyProjection.EventSessionCustomPropertyProjectionDto>>
        {
            Success = true,
            Id = []
        };

        sessionRepository.GetSessionWithDetails(eventSessionId).Returns(new EventSession
        {
            Id = eventSessionId,
            EventId = eventId,
            TenantId = tenantId,
            Event = null!,
            Tenant = null!
        });
        _authService.IsAllowedAsync(
                ResourceKinds.CustomPropertyProjection,
                eventSessionId.ToString("D"),
                AuthorizationActions.CustomPropertyProjections.View,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    attributes != null
                    && attributes["eventSessionId"].Equals(eventSessionId.ToString("D"))
                    && attributes["eventId"].Equals(eventId.ToString("D"))
                    && attributes["tenantId"].Equals(tenantId.ToString("D"))),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await secureBehavior.Handle(request, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await sessionRepository.Received(1).GetSessionWithDetails(eventSessionId);
    }

    [Test]
    public async Task Handle_WithEventCategoryUpdate_BindsPersistedParentEventBeforeAuthorization()
    {
        var assignmentRepository = Substitute.For<IEventCategoriesRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var assignmentId = Guid.NewGuid();
        var persistedEventId = Guid.NewGuid();
        var persistedTenantId = Guid.NewGuid();
        var command = new UpdateEventCategoriesCommand
        {
            EventCategoryId = assignmentId,
            EventId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            EventCategoriesDto = new UpdateEventCategoriesDto
            {
                Category = new UpdateEventCategoriesCategoryDto { CategoryId = Guid.NewGuid() }
            }
        };
        assignmentRepository.GetById(assignmentId).Returns(new EventCategories
        {
            Id = assignmentId,
            EventId = persistedEventId,
            TenantId = persistedTenantId,
            Event = null!,
            CategoryId = Guid.NewGuid(),
            Category = null!,
            Tenant = null!
        });
        eventRepository.GetEventWithDetails(persistedEventId).Returns(CreateAuthorizationEvent(
            persistedEventId,
            persistedTenantId,
            Guid.NewGuid()));
        _authService.IsAllowedAsync(
                ResourceKinds.Event,
                persistedEventId.ToString(),
                AuthorizationActions.Update,
                Arg.Any<IDictionary<string, object>?>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var behavior = new AuthorizationBehavior<UpdateEventCategoriesCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventCategoriesCommand, BaseCommandResponse<Guid>>>>(),
            eventRepository,
            eventCategoriesRepository: assignmentRepository);

        await behavior.Handle(command, _ => Task.FromResult(new BaseCommandResponse<Guid> { Success = true }), CancellationToken.None);

        await Assert.That(command.EventId).IsEqualTo(persistedEventId);
        await Assert.That(command.TenantId).IsEqualTo(persistedTenantId);
    }

    [Test]
    public async Task Handle_WithEventTagUpdate_BindsPersistedParentEventBeforeAuthorization()
    {
        var assignmentRepository = Substitute.For<IEventTagsRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var assignmentId = Guid.NewGuid();
        var persistedEventId = Guid.NewGuid();
        var persistedTenantId = Guid.NewGuid();
        var command = new UpdateEventTagsCommand
        {
            EventTagId = assignmentId,
            EventId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            EventTagsDto = new UpdateEventTagsDto
            {
                Tag = new UpdateEventTagsTagDto { TagId = Guid.NewGuid() }
            }
        };
        assignmentRepository.GetById(assignmentId).Returns(new EventTags
        {
            Id = assignmentId,
            EventId = persistedEventId,
            TenantId = persistedTenantId,
            Event = null!,
            TagId = Guid.NewGuid(),
            Tag = null!,
            Tenant = null!
        });
        eventRepository.GetEventWithDetails(persistedEventId).Returns(CreateAuthorizationEvent(
            persistedEventId,
            persistedTenantId,
            Guid.NewGuid()));
        _authService.IsAllowedAsync(
                ResourceKinds.Event,
                persistedEventId.ToString(),
                AuthorizationActions.Update,
                Arg.Any<IDictionary<string, object>?>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var behavior = new AuthorizationBehavior<UpdateEventTagsCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventTagsCommand, BaseCommandResponse<Guid>>>>(),
            eventRepository,
            eventTagsRepository: assignmentRepository);

        await behavior.Handle(command, _ => Task.FromResult(new BaseCommandResponse<Guid> { Success = true }), CancellationToken.None);

        await Assert.That(command.EventId).IsEqualTo(persistedEventId);
        await Assert.That(command.TenantId).IsEqualTo(persistedTenantId);
    }

    [Test]
    public async Task Handle_WithEventSessionAgendaItemUpdate_BindsPersistedContextBeforeAuthorization()
    {
        var assignmentRepository = Substitute.For<IEventSessionAgendaItemRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var assignmentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        tenantContext.TenantId.Returns(tenantId);
        var command = new UpdateEventSessionAgendaItemCommand
        {
            EventSessionAgendaItemId = assignmentId,
            AgendaItemDto = new UpdateEventSessionAgendaItemDto
            {
                Content = new UpdateEventSessionAgendaItemContentDto { Title = "Agenda item" }
            }
        };
        assignmentRepository.GetByIdWithDetails(assignmentId, Arg.Any<CancellationToken>()).Returns(
            new EventSessionAgendaItem
            {
                Id = assignmentId,
                EventSessionId = sessionId,
                EventSession = new EventSession
                {
                    Id = sessionId,
                    EventId = eventId,
                    Event = null!,
                    TenantId = tenantId,
                    Tenant = null!
                },
                StartTime = DateTimeOffset.UtcNow,
                EndTime = DateTimeOffset.UtcNow.AddHours(1),
                Title = "Agenda item",
                TenantId = tenantId,
                Tenant = null!
            });
        _authService.IsAllowedAsync(
                ResourceKinds.EventSessionAgendaItem,
                assignmentId.ToString(),
                AuthorizationActions.Update,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    attributes != null
                    && attributes["eventSessionId"].Equals(sessionId.ToString())
                    && attributes["eventId"].Equals(eventId.ToString())
                    && attributes["tenantId"].Equals(tenantId.ToString())),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var behavior = new AuthorizationBehavior<UpdateEventSessionAgendaItemCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventSessionAgendaItemCommand, BaseCommandResponse<Guid>>>>(),
            tenantContext: tenantContext,
            eventSessionAgendaItemRepository: assignmentRepository);

        await behavior.Handle(command, _ => Task.FromResult(new BaseCommandResponse<Guid> { Success = true }), CancellationToken.None);

        await Assert.That(command.EventSessionAgendaItemId).IsEqualTo(assignmentId);
        await Assert.That(command.EventSessionId).IsEqualTo(sessionId);
        await Assert.That(command.EventId).IsEqualTo(eventId);
        await Assert.That(command.TenantId).IsEqualTo(tenantId);
    }

    [Test]
    public async Task Handle_WithEventSessionGroupUpdate_BindsPersistedContextBeforeAuthorization()
    {
        var assignmentRepository = Substitute.For<IEventSessionGroupRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var assignmentId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        tenantContext.TenantId.Returns(tenantId);
        var command = new UpdateEventSessionGroupCommand
        {
            EventSessionGroupId = assignmentId,
            EventSessionGroup = new UpdateEventSessionGroupRequestDto
            {
                Metadata = new UpdateEventSessionGroupMetadataDto { Name = "Program section" }
            },
            TenantId = Guid.NewGuid()
        };
        assignmentRepository.GetForUpdateAsync(assignmentId, Arg.Any<CancellationToken>()).Returns(
            new EventSessionGroup
            {
                Id = assignmentId,
                EventId = eventId,
                Event = null!,
                Name = "Program section",
                TenantId = tenantId,
                Tenant = null!
            });
        _authService.IsAllowedAsync(
                ResourceKinds.EventSessionGroup,
                assignmentId.ToString(),
                AuthorizationActions.Update,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    attributes != null
                    && attributes["eventId"].Equals(eventId.ToString())
                    && attributes["tenantId"].Equals(tenantId.ToString())),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var behavior = new AuthorizationBehavior<UpdateEventSessionGroupCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventSessionGroupCommand, BaseCommandResponse<Guid>>>>(),
            tenantContext: tenantContext,
            eventSessionGroupRepository: assignmentRepository);

        await behavior.Handle(command, _ => Task.FromResult(new BaseCommandResponse<Guid> { Success = true }), CancellationToken.None);

        await Assert.That(command.EventSessionGroupId).IsEqualTo(assignmentId);
        await Assert.That(command.EventId).IsEqualTo(eventId);
        await Assert.That(command.TenantId).IsEqualTo(tenantId);
    }

    [Test]
    public async Task Handle_WithEventSessionSpeakerUpdate_BindsPersistedParentSessionBeforeAuthorization()
    {
        var assignmentRepository = Substitute.For<IEventSessionSpeakerRepository>();
        var sessionRepository = Substitute.For<IEventSessionRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var assignmentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        tenantContext.TenantId.Returns(tenantId);
        var command = new UpdateEventSessionSpeakerCommand
        {
            EventSessionSpeakerId = assignmentId,
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            SpeakerDto = new UpdateEventSessionSpeakerDto
            {
                Actor = new UpdateEventSessionSpeakerActorDto { ActorId = Guid.NewGuid() }
            }
        };
        assignmentRepository.GetById(assignmentId).Returns(new EventSessionSpeaker
        {
            Id = assignmentId,
            EventSessionId = sessionId,
            EventSession = null!,
            ActorId = Guid.NewGuid(),
            Actor = null!,
            TenantId = tenantId,
            Tenant = null!
        });
        sessionRepository.GetSessionWithDetails(sessionId).Returns(new EventSession
        {
            Id = sessionId,
            EventId = eventId,
            Event = null!,
            TenantId = tenantId,
            Tenant = null!
        });
        _authService.IsAllowedAsync(
                ResourceKinds.EventSession,
                sessionId.ToString(),
                AuthorizationActions.Update,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    attributes != null
                    && attributes["eventSessionId"].Equals(sessionId.ToString())
                    && attributes["eventId"].Equals(eventId.ToString())
                    && attributes["tenantId"].Equals(tenantId.ToString())),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var behavior = new AuthorizationBehavior<UpdateEventSessionSpeakerCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventSessionSpeakerCommand, BaseCommandResponse<Guid>>>>(),
            eventSessionRepository: sessionRepository,
            tenantContext: tenantContext,
            eventSessionSpeakerRepository: assignmentRepository);

        await behavior.Handle(command, _ => Task.FromResult(new BaseCommandResponse<Guid> { Success = true }), CancellationToken.None);

        await Assert.That(command.EventSessionId).IsEqualTo(sessionId);
        await Assert.That(command.EventId).IsEqualTo(eventId);
        await Assert.That(command.TenantId).IsEqualTo(tenantId);
    }

    [Test]
    public async Task Handle_WithMissingEventSessionAgendaItem_DeniesBeforeAuthorization()
    {
        var assignmentRepository = Substitute.For<IEventSessionAgendaItemRepository>();
        var command = new UpdateEventSessionAgendaItemCommand
        {
            EventSessionAgendaItemId = Guid.NewGuid(),
            AgendaItemDto = new UpdateEventSessionAgendaItemDto
            {
                Content = new UpdateEventSessionAgendaItemContentDto { Title = "Agenda item" }
            }
        };
        var behavior = new AuthorizationBehavior<UpdateEventSessionAgendaItemCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventSessionAgendaItemCommand, BaseCommandResponse<Guid>>>>(),
            eventSessionAgendaItemRepository: assignmentRepository);

        await Assert.ThrowsAsync<AuthorizationException>(() => behavior.Handle(
            command,
            _ => Task.FromResult(new BaseCommandResponse<Guid> { Success = true }),
            CancellationToken.None));
        await _authService.DidNotReceiveWithAnyArgs().IsAllowedAsync(default!, default!, default!, default, default);
    }

    [Test]
    public async Task Handle_WithCrossTenantEventSessionSpeaker_DeniesBeforeAuthorization()
    {
        var assignmentRepository = Substitute.For<IEventSessionSpeakerRepository>();
        var sessionRepository = Substitute.For<IEventSessionRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var assignmentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var persistedTenantId = Guid.NewGuid();
        tenantContext.TenantId.Returns(Guid.NewGuid());
        assignmentRepository.GetById(assignmentId).Returns(new EventSessionSpeaker
        {
            Id = assignmentId,
            EventSessionId = sessionId,
            EventSession = null!,
            ActorId = Guid.NewGuid(),
            Actor = null!,
            TenantId = persistedTenantId,
            Tenant = null!
        });
        sessionRepository.GetSessionWithDetails(sessionId).Returns(new EventSession
        {
            Id = sessionId,
            EventId = Guid.NewGuid(),
            Event = null!,
            TenantId = persistedTenantId,
            Tenant = null!
        });
        var command = new UpdateEventSessionSpeakerCommand
        {
            EventSessionSpeakerId = assignmentId,
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            SpeakerDto = new UpdateEventSessionSpeakerDto
            {
                Actor = new UpdateEventSessionSpeakerActorDto { ActorId = Guid.NewGuid() }
            }
        };
        var behavior = new AuthorizationBehavior<UpdateEventSessionSpeakerCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventSessionSpeakerCommand, BaseCommandResponse<Guid>>>>(),
            eventSessionRepository: sessionRepository,
            tenantContext: tenantContext,
            eventSessionSpeakerRepository: assignmentRepository);

        await Assert.ThrowsAsync<AuthorizationException>(() => behavior.Handle(
            command,
            _ => Task.FromResult(new BaseCommandResponse<Guid> { Success = true }),
            CancellationToken.None));
        await _authService.DidNotReceiveWithAnyArgs().IsAllowedAsync(default!, default!, default!, default, default);
    }

    [Test]
    public async Task Handle_WithOrganizerClaim_PreservesClaimMetadataAndEnrichesParentEvent()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var eventId = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        eventRepository.GetEventWithDetails(eventId).Returns(CreateAuthorizationEvent(
            eventId,
            tenantId,
            organizationId));
        _authService.IsAllowedAsync(
                ResourceKinds.EventOrganizerClaim,
                eventId.ToString(),
                AuthorizationActions.Events.ReviewOrganizerClaim,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    attributes != null
                    && attributes["claimId"].Equals(claimId.ToString())
                    && attributes["eventId"].Equals(eventId.ToString())
                    && attributes["tenantId"].Equals(tenantId.ToString())
                    && attributes["organizationId"].Equals(organizationId.ToString())),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var behavior = new AuthorizationBehavior<ReviewEventOrganizerClaimCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<ReviewEventOrganizerClaimCommand, BaseCommandResponse<Guid>>>>(),
            eventRepository);
        var request = new ReviewEventOrganizerClaimCommand
        {
            EventId = eventId,
            ClaimId = claimId,
            Review = new ReviewEventOrganizerClaimDto
            {
                Decision = EventOrganizerClaimReviewDecision.Reject,
                ReasonCode = "NOT_VERIFIED",
                ExpectedConcurrencyStamp = Guid.NewGuid()
            }
        };

        var result = await behavior.Handle(
            request,
            _ => Task.FromResult(new BaseCommandResponse<Guid> { Success = true }),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await eventRepository.Received(1).GetEventWithDetails(eventId);
    }

    [Test]
    public async Task Handle_WithdrawOrganizerClaim_UsesPersistedClaimantOwnershipInsteadOfRouteFields()
    {
        var persistedEventId = Guid.NewGuid();
        var routeEventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var claimantActorId = Guid.NewGuid();
        var claimantUserId = Guid.NewGuid();
        var claim = EventOrganizerClaim.CreatePending(
            tenantId,
            persistedEventId,
            claimantActorId,
            "domain-proof",
            "bounded-reference",
            DateTime.UtcNow);
        var claimantActor = new Actor
        {
            Id = claimantActorId,
            UserId = claimantUserId,
            ActorTypeId = 1,
            ActorType = null!,
            Pii = new ActorPii { DisplayName = "Claimant" }
        };
        var claimRepository = Substitute.For<IEventOrganizerClaimRepository>();
        var actorRepository = Substitute.For<IActorRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        claimRepository.GetDetailsAsync(claim.Id, false, Arg.Any<CancellationToken>()).Returns(claim);
        actorRepository.GetActorWithDetails(claimantActorId, Arg.Any<CancellationToken>()).Returns(claimantActor);
        eventRepository.GetEventWithDetails(persistedEventId).Returns(CreateAuthorizationEvent(
            persistedEventId,
            tenantId,
            Guid.NewGuid()));
        _authService.IsAllowedAsync(
                ResourceKinds.EventOrganizerClaim,
                claim.Id.ToString("D"),
                AuthorizationActions.Events.WithdrawOrganizerClaim,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    attributes != null
                    && attributes["eventId"].Equals(persistedEventId.ToString("D"))
                    && attributes["claimId"].Equals(claim.Id.ToString("D"))
                    && attributes["claimantActorId"].Equals(claimantActorId.ToString("D"))
                    && attributes["claimantUserId"].Equals(claimantUserId.ToString("D"))),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var behavior = new AuthorizationBehavior<WithdrawEventOrganizerClaimCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<WithdrawEventOrganizerClaimCommand, BaseCommandResponse<Guid>>>>(),
            eventRepository: eventRepository,
            tenantContext: tenantContext,
            eventOrganizerClaimRepository: claimRepository,
            actorRepository: actorRepository);
        var request = new WithdrawEventOrganizerClaimCommand
        {
            EventId = routeEventId,
            ClaimId = claim.Id,
            ExpectedConcurrencyStamp = Guid.NewGuid()
        };

        var result = await behavior.Handle(
            request,
            _ => Task.FromResult(new BaseCommandResponse<Guid> { Success = true }),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await eventRepository.Received(1).GetEventWithDetails(persistedEventId);
        await eventRepository.DidNotReceive().GetEventWithDetails(routeEventId);
    }

    [Test]
    public async Task Handle_WithAuthorizeResourceAndISecureRequest_NullResourceId_FallsBackToTypeName()
    {
        // Arrange
        var secureBehavior = new AuthorizationBehavior<TestSecureCommandWithNullId, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestSecureCommandWithNullId, BaseCommandResponse<Guid>>>>());
        var command = new TestSecureCommandWithNullId();
        var expectedResponse = new BaseCommandResponse<Guid> { Success = true };

        _authService.IsAllowedAsync(
            "islamuevent_organization", nameof(TestSecureCommandWithNullId), "delete",
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);


        // Assert
        await Assert.That(result.Success).IsTrue();
        await _authService.Received(1).IsAllowedAsync(
            "islamuevent_organization",
            nameof(TestSecureCommandWithNullId),
            "delete",
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void AuthorizeResourceAttribute_EnumConstructor_ConvertsActionToString()
    {
        // Arrange & Act
        var attribute = new AuthorizeResourceAttribute("islamuevent_organization", AuthorizationActions.Update);

        // Assert
        Assert.That(attribute.Resource == "islamuevent_organization");
        Assert.That(attribute.Action == "update");
    }

    private static EventRegistration CreateRegistration(
        Guid id,
        Guid tenantId,
        Guid eventId,
        Guid eventSessionId,
        Guid userId) => new()
        {
            Id = id,
            TenantId = tenantId,
            EventId = eventId,
            EventSessionId = eventSessionId,
            UserId = userId,
            Event = null!,
            EventSession = null!,
            User = null!,
            Tenant = null!
        };

    private static Explore.Domain.Event CreateAuthorizationEvent(
        Guid eventId,
        Guid tenantId,
        Guid organizationId)
    {
        var actorId = Guid.NewGuid();
        return new Explore.Domain.Event
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Registration authorization event",
            ActorId = actorId,
            Actor = new Actor
            {
                Id = actorId,
                OrganizationId = organizationId,
                ActorTypeId = 2,
                ActorType = null!,
                Pii = new ActorPii { DisplayName = "Organizer" }
            },
            Tenant = null!,
            EventStatus = null!,
            EventFormat = null!,
            VisibilityType = null!
        };
    }

    private static bool HasRegistrationAuthorizationContext(
        IDictionary<string, object>? attributes,
        Guid tenantId,
        Guid eventId,
        Guid eventSessionId,
        Guid attendeeUserId,
        Guid organizationId) =>
        attributes is not null
        && attributes["tenantId"].Equals(tenantId.ToString("D"))
        && attributes["eventId"].Equals(eventId.ToString("D"))
        && attributes["eventSessionId"].Equals(eventSessionId.ToString("D"))
        && attributes["userId"].Equals(attendeeUserId.ToString("D"))
        && attributes["organizationId"].Equals(organizationId.ToString("D"));
}

// Test command implementing IAuthorizedRequest
public class TestAuthorizedCommand : IRequest<BaseCommandResponse<Guid>>, IAuthorizedRequest
{
    public string ResourceKind => "islamuevent_tenant_setting";
    public string ResourceId => "test-resource";
    public string Action => "update";
}

// Test command with [AuthorizeResource] attribute
[AuthorizeResource("islamuevent_instance_setting", "update")]
public class TestAttributeCommand : IRequest<BaseCommandResponse<Guid>>
{
}

// Test command with no authorization requirement
public class TestPlainCommand : IRequest<BaseCommandResponse<Guid>>
{
}

// Test command with [AuthorizeResource] enum + ISecureRequest providing dynamic ResourceId
[AuthorizeResource("islamuevent_organization", AuthorizationActions.Update)]
public class TestSecureCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid OrganizationId { get; set; } = Guid.NewGuid();

    string? ISecureRequest.ResourceId => OrganizationId.ToString();
}

// Test command with [AuthorizeResource] enum + ISecureRequest providing ResourceId and ResourceAttributes
[AuthorizeResource("islamuevent_organization", AuthorizationActions.Delete)]
public class TestSecureCommandWithAttributes : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid OrganizationId { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; } = Guid.NewGuid();

    string? ISecureRequest.ResourceId => OrganizationId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        new Dictionary<string, object> { ["tenantId"] = TenantId.ToString() };
}

// Test command with [AuthorizeResource] enum + ISecureRequest but null ResourceId — should fall back to type name
[AuthorizeResource("islamuevent_organization", AuthorizationActions.Delete)]
public class TestSecureCommandWithNullId : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    // Uses default null ResourceId — should fall back to type name
}

[AuthorizeResource(ResourceKinds.EventSession, AuthorizationActions.Update)]
public sealed class TestEventSessionSecureCommand(Guid eventSessionId) : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    string? ISecureRequest.ResourceId => eventSessionId.ToString();
}
