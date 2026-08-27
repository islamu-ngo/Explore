// ABOUTME: Unit tests for AuthorizationBehavior MediatR pipeline behavior.
// ABOUTME: Verifies authorization enforcement via [AuthorizeResource], ISecureRequest, enrichers, and pass-through.

using Explore.Application.Authorization;
using Explore.Application.Behaviors;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventCategories;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.DTOs.EventOrganizerClaim;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.DTOs.EventSessionGroup;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.DTOs.EventSessionTemplate;
using Explore.Application.DTOs.EventTags;
using Explore.Application.DTOs.EventTemplate;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Exceptions;
using Explore.Application.Features.CustomPropertyDefinitions.Authorization;
using Explore.Application.Features.CustomPropertyDefinitions.Requests.Commands;
using Explore.Application.Features.EventCategories.Authorization;
using Explore.Application.Features.EventCategories.Requests.Commands;
using Explore.Application.Features.EventCustomProperties.Authorization;
using Explore.Application.Features.EventCustomProperties.Requests.Commands;
using Explore.Application.Features.EventCustomPropertyProjections.Requests.Queries;
using Explore.Application.Features.EventOrganizerClaims.Authorization;
using Explore.Application.Features.EventOrganizerClaims.Requests.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.EventSessionAgendaItems.Authorization;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Commands;
using Explore.Application.Features.EventSessionCustomProperties.Authorization;
using Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;
using Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Queries;
using Explore.Application.Features.EventSessionGroups.Authorization;
using Explore.Application.Features.EventSessionGroups.Requests.Commands;
using Explore.Application.Features.EventSessionLanguages.Authorization;
using Explore.Application.Features.EventSessionLanguages.Requests.Commands;
using Explore.Application.Features.EventSessionSpeakers.Authorization;
using Explore.Application.Features.EventSessionSpeakers.Requests.Commands;
using Explore.Application.Features.EventSessionTemplates.Authorization;
using Explore.Application.Features.EventSessionTemplates.Requests.Commands;
using Explore.Application.Features.EventTags.Authorization;
using Explore.Application.Features.EventTags.Requests.Commands;
using Explore.Application.Features.EventTemplates.Authorization;
using Explore.Application.Features.EventTemplates.Requests.Commands;
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
    private static readonly AuthorizationDecision AllowedDecision =
        AuthorizationDecision.Allow(AuthorizationProviderMetadata.Runtime);
    private static readonly AuthorizationDecision DeniedDecision =
        AuthorizationDecision.Deny(AuthorizationProviderMetadata.Runtime);

    private readonly IAuthorizationProvider _authService;

    public AuthorizationBehaviorTests()
    {
        _authService = Substitute.For<IAuthorizationProvider>();
    }

    [Test]
    public async Task Handle_WithAuthorizeResourceAttribute_WhenAllowed_CallsNext()
    {
        // Arrange
        var attrBehavior = new AuthorizationBehavior<TestAttributeCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestAttributeCommand, BaseCommandResponse<Guid>>>>());
        var command = new TestAttributeCommand();
        var expectedResponse = BaseCommandResponse.Success(Guid.Empty);

        _authService.AuthorizeAsync(
            Arg.Is<AuthorizationRequest>(request =>
                request != null &&
                request.ResourceKind == "islamuevent_instance_setting" &&
                request.Action == "update"),
            Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);

        // Act
        var result = await attrBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task Handle_WithAuthorizeResourceAttribute_WhenDenied_ThrowsAuthorizationException()
    {
        // Arrange
        var attrBehavior = new AuthorizationBehavior<TestAttributeCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestAttributeCommand, BaseCommandResponse<Guid>>>>());
        var command = new TestAttributeCommand();

        _authService.AuthorizeAsync(
            Arg.Any<AuthorizationRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(DeniedDecision);

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(async () =>
            await attrBehavior.Handle(command, _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)), CancellationToken.None));
    }

    [Test]
    public async Task Handle_WithNoAuthRequirement_PassesThrough()
    {
        // Arrange
        var plainBehavior = new AuthorizationBehavior<TestPlainCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestPlainCommand, BaseCommandResponse<Guid>>>>());
        var command = new TestPlainCommand();
        var expectedResponse = BaseCommandResponse.Success(Guid.Empty);

        // Act
        var result = await plainBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await _authService.DidNotReceive().AuthorizeAsync(
            Arg.Any<AuthorizationRequest>(),
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
        var expectedResponse = BaseCommandResponse.Success(Guid.Empty);

        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                "islamuevent_organization", command.OrganizationId.ToString(), "update"),
            Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);

        // Act
        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
                "islamuevent_organization",
                command.OrganizationId.ToString(),
                "update"),
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

        _authService.AuthorizeAsync(
            Arg.Any<AuthorizationRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(DeniedDecision);

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(async () =>
            await secureBehavior.Handle(command, _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)), CancellationToken.None));
    }

    [Test]
    public async Task Handle_WithAuthorizeResourceAndISecureRequest_PassesDeclaredFacts()
    {
        // Arrange
        var secureBehavior = new AuthorizationBehavior<TestSecureCommandWithAttributes, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestSecureCommandWithAttributes, BaseCommandResponse<Guid>>>>());
        var command = new TestSecureCommandWithAttributes();
        var expectedResponse = BaseCommandResponse.Success(Guid.Empty);
        var expectedFacts = new OrganizationAuthorizationFacts(command.TenantId, command.OrganizationId);

        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                "islamuevent_organization", command.OrganizationId.ToString(), "delete", expectedFacts),
            Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);

        // Act
        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
                "islamuevent_organization",
                command.OrganizationId.ToString(),
                "delete",
                expectedFacts),
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
            EventDto = new CreateEventDto
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
        var expectedResponse = BaseCommandResponse.Success(Guid.Empty);

        // No event row exists yet, so the requested owning organization and group are the only context the
        // create rules can weigh; the pre-create fact record is what carries them.
        var expectedFacts = new PreCreateAuthorizationFacts(
            Guid.Empty,
            ParentEventId: null,
            OrganizationId: organizationId,
            GroupId: groupId);

        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.Event,
                CreateEventCommand.PreCreateResourceId,
                AuthorizationActions.Create,
                expectedFacts),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);

        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
            ResourceKinds.Event,
            CreateEventCommand.PreCreateResourceId,
            AuthorizationActions.Create,
            expectedFacts),
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
        var expectedResponse = BaseCommandResponse.Success(Guid.Empty);

        var expectedFacts = new PreCreateAuthorizationFacts(Guid.Empty, null, null, null);

        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.Organization,
                CreateOrganizationCommand.PreCreateResourceId,
                AuthorizationActions.Create,
                expectedFacts),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);

        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
            ResourceKinds.Organization,
            CreateOrganizationCommand.PreCreateResourceId,
            AuthorizationActions.Create,
            expectedFacts),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishEventCommand_DeclaresThePublishAuthorizationAction()
    {
        var metadata = (AuthorizeResourceAttribute?)Attribute.GetCustomAttribute(
            typeof(PublishEventCommand),
            typeof(AuthorizeResourceAttribute));

        await Assert.That(metadata).IsNotNull();
        await Assert.That(metadata!.Resource).IsEqualTo(ResourceKinds.Event);
        await Assert.That(metadata.Action).IsEqualTo(AuthorizationActions.Events.Publish);
    }

    [Test]
    public async Task Handle_WithPublishEventCommand_PassesEventScopedFacts()
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
        var expectedResponse = BaseCommandResponse.Success(Guid.Empty);

        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.Event,
                eventId.ToString(),
                AuthorizationActions.Events.Publish,
                new EventScopedAuthorizationFacts(Guid.Empty, eventId)),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);

        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.Event,
                eventId.ToString(),
                AuthorizationActions.Events.Publish,
                new EventScopedAuthorizationFacts(Guid.Empty, eventId)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApprovePublishEventCommandUsesPrivilegedActionAndEventScopedFacts()
    {
        var metadata = (AuthorizeResourceAttribute?)Attribute.GetCustomAttribute(
            typeof(ApprovePublishEventCommand),
            typeof(AuthorizeResourceAttribute));
        var secureBehavior = new AuthorizationBehavior<ApprovePublishEventCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<ApprovePublishEventCommand, BaseCommandResponse<Guid>>>>());
        Guid eventId = Guid.NewGuid();
        var command = new ApprovePublishEventCommand
        {
            Id = eventId,
            Request = new PublishEventRequestDto { ExpectedConcurrencyStamp = Guid.NewGuid() }
        };
        _authService.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);

        var result = await secureBehavior.Handle(
            command,
            _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)),
            CancellationToken.None);

        await Assert.That(metadata).IsNotNull();
        await Assert.That(metadata!.Resource).IsEqualTo(ResourceKinds.Event);
        await Assert.That(metadata.Action).IsEqualTo(AuthorizationActions.Events.ApprovePublish);
        await Assert.That(result.IsSuccess).IsTrue();
        await _authService.Received(1).AuthorizeAsync(
            Arg.Is<AuthorizationRequest>(request =>
                request != null
                && request.ResourceKind == ResourceKinds.Event
                && request.Action == AuthorizationActions.Events.ApprovePublish
                && request.ResourceId == eventId.ToString()
                && Equals(request.Facts, new EventScopedAuthorizationFacts(Guid.Empty, eventId))),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithEventResource_EnrichesMissingEventAuthorizationContext()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var secureBehavior = new AuthorizationBehavior<UpdateEventCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventCommand, BaseCommandResponse<Guid>>>>(),
            new AuthorizationResourceContextResolver(eventRepository: eventRepository));
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
        var expectedResponse = BaseCommandResponse.Success(Guid.Empty);

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
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.Event,
                eventId.ToString(),
                AuthorizationActions.Update,
                new EventAuthorizationFacts(
                    tenantId,
                    eventId,
                    actorId,
                    UserId: null,
                    OrganizationId: organizationId,
                    GroupId: null,
                    OrganizerActorId: organizerActorId,
                    OrganizerUserId: organizerUserId,
                    OrganizerOrganizationId: null,
                    OrganizerGroupId: null,
                    ProvenanceType: UnsetProvenanceCode,
                    SubmittedByUserId: null)),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);

        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await eventRepository.Received(1).GetEventWithDetails(eventId);
    }

    [Test]
    public async Task Handle_WithRegistrationFormResource_ReplacesCallerDeclaredFactsFromPersistedEvent()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var eventId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var organizerActorId = Guid.NewGuid();
        var organizerUserId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        tenantContext.TenantId.Returns(tenantId);
        eventRepository.GetEventWithDetails(eventId).Returns(new Explore.Domain.Event
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Registration authoring",
            ActorId = actorId,
            Actor = new Actor
            {
                Id = actorId,
                ActorTypeId = 2,
                ActorType = null!,
                Pii = new ActorPii { DisplayName = "Event actor" }
            },
            OrganizerActorId = organizerActorId,
            OrganizerActor = new Actor
            {
                Id = organizerActorId,
                UserId = organizerUserId,
                ActorTypeId = 1,
                ActorType = null!,
                Pii = new ActorPii { DisplayName = "Verified organizer" }
            },
            Tenant = null!,
            EventStatus = null!,
            EventFormat = null!,
            VisibilityType = null!
        });
        // The caller names a tenant it does not own. The resolver reloads the parent event, so the facts the
        // provider sees describe the persisted event and carry nothing the caller supplied.
        var command = new TestRegistrationFormSecureCommand(formId, eventId, attackerId);
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.RegistrationForm,
                formId.ToString("D"),
                AuthorizationActions.RegistrationForms.Update,
                new EventAuthorizationFacts(
                    tenantId,
                    eventId,
                    actorId,
                    UserId: null,
                    OrganizationId: null,
                    GroupId: null,
                    OrganizerActorId: organizerActorId,
                    OrganizerUserId: organizerUserId,
                    OrganizerOrganizationId: null,
                    OrganizerGroupId: null,
                    ProvenanceType: UnsetProvenanceCode,
                    SubmittedByUserId: null)),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);
        var behavior = new AuthorizationBehavior<TestRegistrationFormSecureCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestRegistrationFormSecureCommand, BaseCommandResponse<Guid>>>>(),
            new AuthorizationResourceContextResolver(eventRepository: eventRepository, tenantContext: tenantContext));

        var result = await behavior.Handle(
            command,
            _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await eventRepository.Received(1).GetEventWithDetails(eventId);
    }

    [Test]
    public async Task Handle_WithRegistrationFormResource_WhenPersistedEventIsMissing_DeniesBeforeProvider()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var eventId = Guid.NewGuid();
        var command = new TestRegistrationFormSecureCommand(Guid.NewGuid(), eventId);
        var behavior = new AuthorizationBehavior<TestRegistrationFormSecureCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestRegistrationFormSecureCommand, BaseCommandResponse<Guid>>>>(),
            new AuthorizationResourceContextResolver(eventRepository: eventRepository));

        await Assert.ThrowsAsync<AuthorizationException>(async () =>
            await behavior.Handle(
                command,
                _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)),
                CancellationToken.None));
        await _authService.DidNotReceiveWithAnyArgs().AuthorizeAsync(default!, default);
    }

    [Test]
    public async Task Handle_WithRegistrationFormResource_WhenPersistedEventTenantMismatches_DeniesBeforeProvider()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var eventId = Guid.NewGuid();
        tenantContext.TenantId.Returns(Guid.NewGuid());
        eventRepository.GetEventWithDetails(eventId).Returns(CreateAuthorizationEvent(
            eventId,
            Guid.NewGuid(),
            Guid.NewGuid()));
        var command = new TestRegistrationFormSecureCommand(Guid.NewGuid(), eventId);
        var behavior = new AuthorizationBehavior<TestRegistrationFormSecureCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestRegistrationFormSecureCommand, BaseCommandResponse<Guid>>>>(),
            new AuthorizationResourceContextResolver(eventRepository: eventRepository, tenantContext: tenantContext));

        await Assert.ThrowsAsync<AuthorizationException>(async () =>
            await behavior.Handle(
                command,
                _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)),
                CancellationToken.None));
        await _authService.DidNotReceiveWithAnyArgs().AuthorizeAsync(default!, default);
    }

    [Test]
    public async Task Handle_WithPlatformAuthorizationScenarios_PinsProviderNeutralOutcomes()
    {
        foreach (var scenario in PlatformAuthorizationScenarios())
        {
            var authService = Substitute.For<IAuthorizationProvider>();
            var behavior = new AuthorizationBehavior<TestAuthorizationPlatformCommand, BaseCommandResponse<Guid>>(
                authService,
                Substitute.For<ILogger<AuthorizationBehavior<TestAuthorizationPlatformCommand, BaseCommandResponse<Guid>>>>());
            var command = new TestAuthorizationPlatformCommand(scenario.ResourceId, scenario.Facts);
            var nextCalled = false;
            var expectedResponse = BaseCommandResponse.Success(Guid.Empty);

            authService.AuthorizeAsync(
                MatchesAuthorizationRequest(
                    ResourceKinds.Event,
                    scenario.ResourceId,
                    AuthorizationActions.Update,
                    scenario.Facts),
                    Arg.Any<CancellationToken>())
                .Returns(scenario.AllowedByProvider ? AllowedDecision : DeniedDecision);

            async Task<BaseCommandResponse<Guid>> Act() => await behavior.Handle(
                command,
                _ =>
                {
                    nextCalled = true;
                    return Task.FromResult(expectedResponse);
                },
                CancellationToken.None);

            if (scenario.ExpectedOutcome == AuthorizationScenarioOutcome.Allowed)
            {
                var result = await Act();

                await Assert.That(result.IsSuccess).IsTrue();
                await Assert.That(nextCalled).IsTrue();
            }
            else
            {
                await Assert.ThrowsAsync<AuthorizationException>(Act);
                await Assert.That(nextCalled).IsFalse();
            }

            await authService.Received(1).AuthorizeAsync(
                MatchesAuthorizationRequest(
                ResourceKinds.Event,
                scenario.ResourceId,
                AuthorizationActions.Update,
                scenario.Facts),
                Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task Handle_WithRegistrationFormPipelineDenialScenarios_DeniesBeforeProvider()
    {
        foreach (var scenario in RegistrationFormPipelineDenialScenarios())
        {
            var authService = Substitute.For<IAuthorizationProvider>();
            var eventRepository = Substitute.For<IEventRepository>();
            var tenantContext = Substitute.For<ITenantContext>();
            var behavior = new AuthorizationBehavior<TestRegistrationFormSecureCommand, BaseCommandResponse<Guid>>(
                authService,
                Substitute.For<ILogger<AuthorizationBehavior<TestRegistrationFormSecureCommand, BaseCommandResponse<Guid>>>>(),
                new AuthorizationResourceContextResolver(eventRepository: eventRepository, tenantContext: tenantContext));
            var nextCalled = false;

            tenantContext.TenantId.Returns(scenario.AmbientTenantId);
            if (scenario.PersistedEvent is not null)
            {
                eventRepository.GetEventWithDetails(scenario.EventId).Returns(scenario.PersistedEvent);
            }

            await Assert.ThrowsAsync<AuthorizationException>(() => behavior.Handle(
                new TestRegistrationFormSecureCommand(Guid.NewGuid(), scenario.EventId),
                _ =>
                {
                    nextCalled = true;
                    return Task.FromResult(BaseCommandResponse.Success(Guid.Empty));
                },
                CancellationToken.None));

            await Assert.That(nextCalled).IsFalse();
            await authService.DidNotReceiveWithAnyArgs().AuthorizeAsync(default!, default);
        }
    }

    [Test]
    public async Task Handle_WithEventSessionResource_EnrichesMissingEventAuthorizationContext()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var secureBehavior = new AuthorizationBehavior<TestEventSessionSecureCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestEventSessionSecureCommand, BaseCommandResponse<Guid>>>>(),
            new AuthorizationResourceContextResolver(eventSessionRepository: eventSessionRepository));
        var eventSessionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new TestEventSessionSecureCommand(eventSessionId);
        var expectedResponse = BaseCommandResponse.Success(Guid.Empty);

        eventSessionRepository.GetSessionWithDetails(eventSessionId).Returns(new EventSession
        {
            Id = eventSessionId,
            EventId = eventId,
            TenantId = tenantId,
            Event = null!,
            Tenant = null!
        });
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.EventSession,
                eventSessionId.ToString(),
                AuthorizationActions.Update,
                new EventScopedAuthorizationFacts(tenantId, eventId, eventSessionId)),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);

        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await eventSessionRepository.Received(1).GetSessionWithDetails(eventSessionId);
    }

    [Test]
    public async Task Handle_WithOrganizationMemberResource_EnrichesMissingMemberAuthorizationContext()
    {
        var memberRepository = Substitute.For<IOrganizationMemberRepository>();
        var secureBehavior = new AuthorizationBehavior<GetOrganizationMemberDetailsRequest, OrganizationMemberDto?>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<GetOrganizationMemberDetailsRequest, OrganizationMemberDto?>>>(),
            new AuthorizationResourceContextResolver(organizationMemberRepository: memberRepository));
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
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.OrganizationMember,
                memberId.ToString(),
                AuthorizationActions.OrganizationMembers.View,
                new OrganizationMemberAuthorizationFacts(tenantId, organizationId, memberId, userId)),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);

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
            new AuthorizationResourceContextResolver(storageObjectRepository: storageObjectRepository));
        var storageObjectId = Guid.NewGuid();
        var persistedTenantId = Guid.NewGuid();
        var command = new UpdateStorageObjectCommand
        {
            StorageObjectId = storageObjectId,
            StorageObjectDto = new UpdateStorageObjectDto
            {
                Metadata = new StorageObjectMetadataUpdateDto
                {
                    FullName = "file.png",
                    SafeDisplayName = "file.png"
                }
            }
        };
        var expectedResponse = BaseCommandResponse.Success(Guid.Empty);

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
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.StorageObject,
                storageObjectId.ToString(),
                AuthorizationActions.Update,
                new PersistedStorageObjectAuthorizationFacts(
                    persistedTenantId,
                    storageObjectId,
                    StorageObjectVisibilities.PublicImage,
                    StorageObjectLifecycleStates.Active,
                    CreatedBy: null,
                    OwningResourceKind: null,
                    OwningResourceId: null)),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);

        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await storageObjectRepository.Received(1).GetById(storageObjectId);
    }

    [Test]
    public async Task Handle_WithCustomPropertyProjectionEventResource_EnrichesTenantAuthorizationContext()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var secureBehavior = new AuthorizationBehavior<GetEventCustomPropertyProjectionsForEventQuery, BaseCommandResponse<IReadOnlyList<Explore.Application.DTOs.CustomPropertyProjection.EventCustomPropertyProjectionDto>>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<GetEventCustomPropertyProjectionsForEventQuery, BaseCommandResponse<IReadOnlyList<Explore.Application.DTOs.CustomPropertyProjection.EventCustomPropertyProjectionDto>>>>>(),
            new AuthorizationResourceContextResolver(eventRepository: eventRepository));
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = new GetEventCustomPropertyProjectionsForEventQuery { EventId = eventId };
        var expectedResponse = BaseCommandResponse.Success<IReadOnlyList<Explore.Application.DTOs.CustomPropertyProjection.EventCustomPropertyProjectionDto>>([]);

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
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.CustomPropertyProjection,
                eventId.ToString("D"),
                AuthorizationActions.CustomPropertyProjections.View,
                new CustomPropertyProjectionAuthorizationFacts(tenantId, eventId)),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);

        var result = await secureBehavior.Handle(request, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await eventRepository.Received(1).GetEventWithDetails(eventId);
    }

    [Test]
    public async Task Handle_WithCustomPropertyProjectionSessionResource_EnrichesTenantAuthorizationContext()
    {
        var sessionRepository = Substitute.For<IEventSessionRepository>();
        var secureBehavior = new AuthorizationBehavior<GetEventSessionCustomPropertyProjectionsForSessionQuery, BaseCommandResponse<IReadOnlyList<Explore.Application.DTOs.CustomPropertyProjection.EventSessionCustomPropertyProjectionDto>>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<GetEventSessionCustomPropertyProjectionsForSessionQuery, BaseCommandResponse<IReadOnlyList<Explore.Application.DTOs.CustomPropertyProjection.EventSessionCustomPropertyProjectionDto>>>>>(),
            new AuthorizationResourceContextResolver(eventSessionRepository: sessionRepository));
        var eventId = Guid.NewGuid();
        var eventSessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = new GetEventSessionCustomPropertyProjectionsForSessionQuery { EventSessionId = eventSessionId };
        var expectedResponse = BaseCommandResponse.Success<IReadOnlyList<Explore.Application.DTOs.CustomPropertyProjection.EventSessionCustomPropertyProjectionDto>>([]);

        sessionRepository.GetSessionWithDetails(eventSessionId).Returns(new EventSession
        {
            Id = eventSessionId,
            EventId = eventId,
            TenantId = tenantId,
            Event = null!,
            Tenant = null!
        });
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.CustomPropertyProjection,
                eventSessionId.ToString("D"),
                AuthorizationActions.CustomPropertyProjections.View,
                new CustomPropertyProjectionAuthorizationFacts(tenantId, eventId, eventSessionId)),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);

        var result = await secureBehavior.Handle(request, _ => Task.FromResult(expectedResponse), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await sessionRepository.Received(1).GetSessionWithDetails(eventSessionId);
    }

    [Test]
    public async Task Handle_WithCustomPropertyDefinitionUpdate_BindsPersistedTenantBeforeAuthorization()
    {
        var repository = Substitute.For<ICustomPropertyDefinitionRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var definitionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var expectedConcurrencyStamp = Guid.NewGuid();
        var expectedFacts = new TenantScopedAuthorizationFacts(tenantId);
        tenantContext.TenantId.Returns(tenantId);
        repository.GetDefinitionWithDetails(definitionId).Returns(new CustomPropertyDefinition
        {
            Id = definitionId,
            TenantId = tenantId,
            Tenant = null,
            EntityTypeName = Explore.Domain.Enums.EntityTypeName.Organization,
            Namespace = "tenant.community",
            Key = "prayer_notes",
            DisplayName = "Prayer notes"
        });
        var command = new UpdateCustomPropertyDefinitionCommand
        {
            DefinitionId = definitionId,
            DefinitionDto = new UpdateCustomPropertyDefinitionDto
            {
                Metadata = new UpdateCustomPropertyDefinitionMetadataDto { DisplayName = "Renamed" }
            },
            ExpectedConcurrencyStamp = expectedConcurrencyStamp
        };
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.Tenant,
                tenantId.ToString(),
                AuthorizationActions.Update,
                expectedFacts),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);
        var behavior = new AuthorizationBehavior<UpdateCustomPropertyDefinitionCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateCustomPropertyDefinitionCommand, BaseCommandResponse<Guid>>>>(),
            authorizationContextEnricher: new UpdateCustomPropertyDefinitionAuthorizationContextEnricher(repository, tenantContext));

        var result = await behavior.Handle(
            command,
            _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(command.TenantId).IsEqualTo(Guid.Empty);
        await Assert.That(command.DefinitionId).IsEqualTo(definitionId);
        await Assert.That(command.ExpectedConcurrencyStamp).IsEqualTo(expectedConcurrencyStamp);
        await repository.Received(1).GetDefinitionWithDetails(definitionId);
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
            ResourceKinds.Tenant,
            tenantId.ToString(),
            AuthorizationActions.Update,
            expectedFacts),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithEventCustomPropertyDefinitionUpdate_BindsPersistedTenantBeforeAuthorization()
    {
        var repository = Substitute.For<IEventCustomPropertyRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var definitionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var expectedConcurrencyStamp = Guid.NewGuid();
        var expectedFacts = new TenantScopedAuthorizationFacts(tenantId);
        tenantContext.TenantId.Returns(tenantId);
        repository.GetDefinitionWithDetails(definitionId).Returns(new EventCustomPropertyDefinition
        {
            Id = definitionId,
            EventId = Guid.NewGuid(),
            TenantId = tenantId,
            Namespace = "tenant.community",
            Key = "prayer_notes",
            DisplayName = "Prayer notes"
        });
        var command = new UpdateEventCustomPropertyDefinitionCommand
        {
            DefinitionId = definitionId,
            DefinitionDto = new UpdateEventCustomPropertyDefinitionDto
            {
                Metadata = new UpdateCustomPropertyDefinitionMetadataDto { DisplayName = "Renamed" }
            },
            ExpectedConcurrencyStamp = expectedConcurrencyStamp
        };
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.Tenant,
                tenantId.ToString(),
                AuthorizationActions.Update,
                expectedFacts),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);
        var behavior = new AuthorizationBehavior<UpdateEventCustomPropertyDefinitionCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventCustomPropertyDefinitionCommand, BaseCommandResponse<Guid>>>>(),
            authorizationContextEnricher: new UpdateEventCustomPropertyDefinitionAuthorizationContextEnricher(repository, tenantContext));

        var result = await behavior.Handle(
            command,
            _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(command.TenantId).IsEqualTo(Guid.Empty);
        await Assert.That(command.DefinitionId).IsEqualTo(definitionId);
        await Assert.That(command.ExpectedConcurrencyStamp).IsEqualTo(expectedConcurrencyStamp);
        await repository.Received(1).GetDefinitionWithDetails(definitionId);
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
            ResourceKinds.Tenant,
            tenantId.ToString(),
            AuthorizationActions.Update,
            expectedFacts),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithEventSessionCustomPropertyDefinitionUpdate_BindsPersistedTenantBeforeAuthorization()
    {
        var repository = Substitute.For<IEventSessionCustomPropertyRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var definitionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var expectedConcurrencyStamp = Guid.NewGuid();
        var expectedFacts = new TenantScopedAuthorizationFacts(tenantId);
        tenantContext.TenantId.Returns(tenantId);
        repository.GetDefinitionWithDetails(definitionId).Returns(new EventSessionCustomPropertyDefinition
        {
            Id = definitionId,
            EventSessionId = Guid.NewGuid(),
            TenantId = tenantId,
            Namespace = "tenant.community",
            Key = "speaker_notes",
            DisplayName = "Speaker notes"
        });
        var command = new UpdateEventSessionCustomPropertyDefinitionCommand
        {
            DefinitionId = definitionId,
            DefinitionDto = new UpdateEventSessionCustomPropertyDefinitionDto
            {
                Metadata = new UpdateCustomPropertyDefinitionMetadataDto { DisplayName = "Renamed" }
            },
            ExpectedConcurrencyStamp = expectedConcurrencyStamp
        };
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.Tenant,
                tenantId.ToString(),
                AuthorizationActions.Update,
                expectedFacts),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);
        var behavior = new AuthorizationBehavior<UpdateEventSessionCustomPropertyDefinitionCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventSessionCustomPropertyDefinitionCommand, BaseCommandResponse<Guid>>>>(),
            authorizationContextEnricher: new UpdateEventSessionCustomPropertyDefinitionAuthorizationContextEnricher(repository, tenantContext));

        var result = await behavior.Handle(
            command,
            _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(command.TenantId).IsEqualTo(Guid.Empty);
        await Assert.That(command.DefinitionId).IsEqualTo(definitionId);
        await Assert.That(command.ExpectedConcurrencyStamp).IsEqualTo(expectedConcurrencyStamp);
        await repository.Received(1).GetDefinitionWithDetails(definitionId);
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
            ResourceKinds.Tenant,
            tenantId.ToString(),
            AuthorizationActions.Update,
            expectedFacts),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithEventTemplateUpdate_BindsPersistedTenantBeforeAuthorization()
    {
        var repository = Substitute.For<IEventTemplateRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var templateId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var expectedConcurrencyStamp = Guid.NewGuid();
        var expectedFacts = new TenantScopedAuthorizationFacts(tenantId);
        tenantContext.TenantId.Returns(tenantId);
        repository.GetTemplateWithDetails(templateId).Returns(new EventTemplate
        {
            Id = templateId,
            TenantId = tenantId,
            TemplateKey = "conference",
            DisplayName = "Conference"
        });
        var command = new UpdateEventTemplateCommand
        {
            TemplateId = templateId,
            ExpectedConcurrencyStamp = expectedConcurrencyStamp,
            TemplateDto = new UpdateEventTemplateDto
            {
                Metadata = new UpdateEventTemplateMetadataDto { DisplayName = "Conference 2027" }
            }
        };
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.Tenant,
                tenantId.ToString(),
                AuthorizationActions.Update,
                expectedFacts),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);
        var behavior = new AuthorizationBehavior<UpdateEventTemplateCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventTemplateCommand, BaseCommandResponse<Guid>>>>(),
            authorizationContextEnricher: new UpdateEventTemplateAuthorizationContextEnricher(repository, tenantContext));

        var result = await behavior.Handle(
            command,
            _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(command.TenantId).IsEqualTo(Guid.Empty);
        await Assert.That(command.TemplateId).IsEqualTo(templateId);
        await Assert.That(command.ExpectedConcurrencyStamp).IsEqualTo(expectedConcurrencyStamp);
        await repository.Received(1).GetTemplateWithDetails(templateId);
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
            ResourceKinds.Tenant,
            tenantId.ToString(),
            AuthorizationActions.Update,
            expectedFacts),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithEventSessionTemplateUpdate_BindsPersistedTenantBeforeAuthorization()
    {
        var repository = Substitute.For<IEventSessionTemplateRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var sessionTemplateId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var expectedConcurrencyStamp = Guid.NewGuid();
        var expectedFacts = new TenantScopedAuthorizationFacts(tenantId);
        tenantContext.TenantId.Returns(tenantId);
        repository.GetSessionTemplateWithDetails(sessionTemplateId).Returns(new EventSessionTemplate
        {
            Id = sessionTemplateId,
            EventTemplateId = Guid.NewGuid(),
            TenantId = tenantId,
            SessionTemplateKey = "keynote",
            DisplayName = "Keynote"
        });
        var command = new UpdateEventSessionTemplateCommand
        {
            SessionTemplateId = sessionTemplateId,
            ExpectedConcurrencyStamp = expectedConcurrencyStamp,
            SessionTemplateDto = new UpdateEventSessionTemplateDto
            {
                Metadata = new UpdateEventSessionTemplateMetadataDto { DisplayName = "Opening Keynote" }
            }
        };
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.Tenant,
                tenantId.ToString(),
                AuthorizationActions.Update,
                expectedFacts),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);
        var behavior = new AuthorizationBehavior<UpdateEventSessionTemplateCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventSessionTemplateCommand, BaseCommandResponse<Guid>>>>(),
            authorizationContextEnricher: new UpdateEventSessionTemplateAuthorizationContextEnricher(repository, tenantContext));

        var result = await behavior.Handle(
            command,
            _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(command.TenantId).IsEqualTo(Guid.Empty);
        await Assert.That(command.SessionTemplateId).IsEqualTo(sessionTemplateId);
        await Assert.That(command.ExpectedConcurrencyStamp).IsEqualTo(expectedConcurrencyStamp);
        await repository.Received(1).GetSessionTemplateWithDetails(sessionTemplateId);
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
            ResourceKinds.Tenant,
            tenantId.ToString(),
            AuthorizationActions.Update,
            expectedFacts),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithEventSessionLanguageUpdate_BindsPersistedSessionBeforeAuthorization()
    {
        var assignmentRepository = Substitute.For<IEventSessionLanguageRepository>();
        var sessionRepository = Substitute.For<IEventSessionRepository>();
        const int assignmentId = 7;
        var originalSessionId = Guid.NewGuid();
        var persistedSessionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var expectedConcurrencyStamp = Guid.NewGuid();
        var expectedFacts = new EventScopedAuthorizationFacts(tenantId, eventId, persistedSessionId);
        assignmentRepository.GetById(assignmentId).Returns(new EventSessionLanguage
        {
            Id = assignmentId,
            EventSessionId = persistedSessionId,
            EventSession = null!,
            LanguageId = 1,
            Language = null!,
            TenantId = tenantId,
            Tenant = null!
        });
        sessionRepository.GetSessionWithDetails(persistedSessionId).Returns(new EventSession
        {
            Id = persistedSessionId,
            EventId = eventId,
            Event = null!,
            TenantId = tenantId,
            Tenant = null!
        });
        var command = new UpdateEventSessionLanguageCommand
        {
            EventSessionLanguageId = assignmentId,
            EventSessionId = originalSessionId,
            ExpectedConcurrencyStamp = expectedConcurrencyStamp,
            EventSessionLanguageDto = new UpdateEventSessionLanguageDto
            {
                Language = new UpdateEventSessionLanguageLanguageDto { LanguageId = 2 }
            }
        };
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.EventSession,
                persistedSessionId.ToString(),
                AuthorizationActions.Update,
                expectedFacts),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);
        var behavior = new AuthorizationBehavior<UpdateEventSessionLanguageCommand, BaseCommandResponse<int>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventSessionLanguageCommand, BaseCommandResponse<int>>>>(),
            new AuthorizationResourceContextResolver(eventSessionRepository: sessionRepository),
            authorizationContextEnricher: new UpdateEventSessionLanguageAuthorizationContextEnricher(assignmentRepository));

        var result = await behavior.Handle(
            command,
            _ => Task.FromResult(BaseCommandResponse.Success(0)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(command.EventSessionId).IsEqualTo(originalSessionId);
        await Assert.That(command.EventSessionLanguageId).IsEqualTo(assignmentId);
        await Assert.That(command.ExpectedConcurrencyStamp).IsEqualTo(expectedConcurrencyStamp);
        await assignmentRepository.Received(1).GetById(assignmentId);
        await sessionRepository.Received(1).GetSessionWithDetails(persistedSessionId);
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
            ResourceKinds.EventSession,
            persistedSessionId.ToString(),
            AuthorizationActions.Update,
            expectedFacts),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithEventCategoryUpdate_BindsPersistedParentEventBeforeAuthorization()
    {
        var assignmentRepository = Substitute.For<IEventCategoriesRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var assignmentId = Guid.NewGuid();
        var persistedEventId = Guid.NewGuid();
        var persistedTenantId = Guid.NewGuid();
        var originalEventId = Guid.NewGuid();
        var originalTenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var expectedConcurrencyStamp = Guid.NewGuid();
        var command = new UpdateEventCategoriesCommand
        {
            EventCategoryId = assignmentId,
            EventId = originalEventId,
            TenantId = originalTenantId,
            ExpectedConcurrencyStamp = expectedConcurrencyStamp,
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
        var authorizationEvent = CreateAuthorizationEvent(
            persistedEventId,
            persistedTenantId,
            organizationId);
        var expectedFacts = new EventAuthorizationFacts(
            persistedTenantId,
            persistedEventId,
            authorizationEvent.ActorId,
            UserId: null,
            OrganizationId: organizationId,
            GroupId: null,
            OrganizerActorId: null,
            OrganizerUserId: null,
            OrganizerOrganizationId: null,
            OrganizerGroupId: null,
            ProvenanceType: UnsetProvenanceCode,
            SubmittedByUserId: null);
        eventRepository.GetEventWithDetails(persistedEventId).Returns(authorizationEvent);
        _authService.AuthorizeAsync(
                MatchesAuthorizationRequest(
                    ResourceKinds.Event,
                    persistedEventId.ToString(),
                    AuthorizationActions.Update),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);
        var behavior = new AuthorizationBehavior<UpdateEventCategoriesCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventCategoriesCommand, BaseCommandResponse<Guid>>>>(),
            new AuthorizationResourceContextResolver(eventRepository: eventRepository),
            authorizationContextEnricher: new UpdateEventCategoriesAuthorizationContextEnricher(assignmentRepository));

        await behavior.Handle(command, _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)), CancellationToken.None);

        await Assert.That(command.EventId).IsEqualTo(originalEventId);
        await Assert.That(command.TenantId).IsEqualTo(originalTenantId);
        await Assert.That(command.EventCategoryId).IsEqualTo(assignmentId);
        await Assert.That(command.ExpectedConcurrencyStamp).IsEqualTo(expectedConcurrencyStamp);
        await assignmentRepository.Received(1).GetById(assignmentId);
        await eventRepository.Received(1).GetEventWithDetails(persistedEventId);
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
            ResourceKinds.Event,
            persistedEventId.ToString(),
            AuthorizationActions.Update,
            expectedFacts),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithEventTagUpdate_BindsPersistedParentEventBeforeAuthorization()
    {
        var assignmentRepository = Substitute.For<IEventTagsRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var assignmentId = Guid.NewGuid();
        var persistedEventId = Guid.NewGuid();
        var persistedTenantId = Guid.NewGuid();
        var originalEventId = Guid.NewGuid();
        var originalTenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var expectedConcurrencyStamp = Guid.NewGuid();
        var command = new UpdateEventTagsCommand
        {
            EventTagId = assignmentId,
            EventId = originalEventId,
            TenantId = originalTenantId,
            ExpectedConcurrencyStamp = expectedConcurrencyStamp,
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
        var authorizationEvent = CreateAuthorizationEvent(
            persistedEventId,
            persistedTenantId,
            organizationId);
        var expectedFacts = new EventAuthorizationFacts(
            persistedTenantId,
            persistedEventId,
            authorizationEvent.ActorId,
            UserId: null,
            OrganizationId: organizationId,
            GroupId: null,
            OrganizerActorId: null,
            OrganizerUserId: null,
            OrganizerOrganizationId: null,
            OrganizerGroupId: null,
            ProvenanceType: UnsetProvenanceCode,
            SubmittedByUserId: null);
        eventRepository.GetEventWithDetails(persistedEventId).Returns(authorizationEvent);
        _authService.AuthorizeAsync(
                MatchesAuthorizationRequest(
                    ResourceKinds.Event,
                    persistedEventId.ToString(),
                    AuthorizationActions.Update),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);
        var behavior = new AuthorizationBehavior<UpdateEventTagsCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventTagsCommand, BaseCommandResponse<Guid>>>>(),
            new AuthorizationResourceContextResolver(eventRepository: eventRepository),
            authorizationContextEnricher: new UpdateEventTagsAuthorizationContextEnricher(assignmentRepository));

        await behavior.Handle(command, _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)), CancellationToken.None);

        await Assert.That(command.EventId).IsEqualTo(originalEventId);
        await Assert.That(command.TenantId).IsEqualTo(originalTenantId);
        await Assert.That(command.EventTagId).IsEqualTo(assignmentId);
        await Assert.That(command.ExpectedConcurrencyStamp).IsEqualTo(expectedConcurrencyStamp);
        await assignmentRepository.Received(1).GetById(assignmentId);
        await eventRepository.Received(1).GetEventWithDetails(persistedEventId);
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
            ResourceKinds.Event,
            persistedEventId.ToString(),
            AuthorizationActions.Update,
            expectedFacts),
            Arg.Any<CancellationToken>());
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
        var expectedFacts = new EventScopedAuthorizationFacts(tenantId, eventId, sessionId);
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
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.EventSessionAgendaItem,
                assignmentId.ToString(),
                AuthorizationActions.Update,
                expectedFacts),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);
        var behavior = new AuthorizationBehavior<UpdateEventSessionAgendaItemCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventSessionAgendaItemCommand, BaseCommandResponse<Guid>>>>(),
            authorizationContextEnricher: new UpdateEventSessionAgendaItemAuthorizationContextEnricher(assignmentRepository, tenantContext));

        await behavior.Handle(command, _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)), CancellationToken.None);

        await Assert.That(command.EventSessionAgendaItemId).IsEqualTo(assignmentId);
        await Assert.That(command.EventSessionId).IsEqualTo(Guid.Empty);
        await Assert.That(command.EventId).IsEqualTo(Guid.Empty);
        await Assert.That(command.TenantId).IsEqualTo(Guid.Empty);
        await assignmentRepository.Received(1).GetByIdWithDetails(assignmentId, Arg.Any<CancellationToken>());
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
            ResourceKinds.EventSessionAgendaItem,
            assignmentId.ToString(),
            AuthorizationActions.Update,
            expectedFacts),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithEventSessionGroupUpdate_BindsPersistedContextBeforeAuthorization()
    {
        var assignmentRepository = Substitute.For<IEventSessionGroupRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var assignmentId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var expectedConcurrencyStamp = Guid.NewGuid();
        var expectedFacts = new EventScopedAuthorizationFacts(tenantId, eventId);
        tenantContext.TenantId.Returns(tenantId);
        var command = new UpdateEventSessionGroupCommand
        {
            EventSessionGroupId = assignmentId,
            EventSessionGroup = new UpdateEventSessionGroupRequestDto
            {
                Metadata = new UpdateEventSessionGroupMetadataDto { Name = "Program section" }
            },
            ExpectedConcurrencyStamp = expectedConcurrencyStamp,
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
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.EventSessionGroup,
                assignmentId.ToString(),
                AuthorizationActions.Update,
                expectedFacts),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);
        var behavior = new AuthorizationBehavior<UpdateEventSessionGroupCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventSessionGroupCommand, BaseCommandResponse<Guid>>>>(),
            authorizationContextEnricher: new UpdateEventSessionGroupAuthorizationContextEnricher(assignmentRepository, tenantContext));

        await behavior.Handle(command, _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)), CancellationToken.None);

        await Assert.That(command.EventSessionGroupId).IsEqualTo(assignmentId);
        await Assert.That(command.EventId).IsEqualTo(Guid.Empty);
        await Assert.That(command.TenantId).IsNotEqualTo(tenantId);
        await Assert.That(command.ExpectedConcurrencyStamp).IsEqualTo(expectedConcurrencyStamp);
        await assignmentRepository.Received(1).GetForUpdateAsync(assignmentId, Arg.Any<CancellationToken>());
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
            ResourceKinds.EventSessionGroup,
            assignmentId.ToString(),
            AuthorizationActions.Update,
            expectedFacts),
            Arg.Any<CancellationToken>());
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
        var expectedConcurrencyStamp = Guid.NewGuid();
        var expectedFacts = new EventScopedAuthorizationFacts(tenantId, eventId, sessionId);
        tenantContext.TenantId.Returns(tenantId);
        var command = new UpdateEventSessionSpeakerCommand
        {
            EventSessionSpeakerId = assignmentId,
            ExpectedConcurrencyStamp = expectedConcurrencyStamp,
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
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.EventSession,
                sessionId.ToString(),
                AuthorizationActions.Update,
                expectedFacts),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);
        var behavior = new AuthorizationBehavior<UpdateEventSessionSpeakerCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<UpdateEventSessionSpeakerCommand, BaseCommandResponse<Guid>>>>(),
            authorizationContextEnricher: new UpdateEventSessionSpeakerAuthorizationContextEnricher(assignmentRepository, sessionRepository, tenantContext));

        await behavior.Handle(command, _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)), CancellationToken.None);

        await Assert.That(command.EventSessionId).IsEqualTo(Guid.Empty);
        await Assert.That(command.EventId).IsEqualTo(Guid.Empty);
        await Assert.That(command.TenantId).IsEqualTo(Guid.Empty);
        await Assert.That(command.EventSessionSpeakerId).IsEqualTo(assignmentId);
        await Assert.That(command.ExpectedConcurrencyStamp).IsEqualTo(expectedConcurrencyStamp);
        await assignmentRepository.Received(1).GetById(assignmentId);
        await sessionRepository.Received(1).GetSessionWithDetails(sessionId);
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
            ResourceKinds.EventSession,
            sessionId.ToString(),
            AuthorizationActions.Update,
            expectedFacts),
            Arg.Any<CancellationToken>());
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
            authorizationContextEnricher: new UpdateEventSessionAgendaItemAuthorizationContextEnricher(assignmentRepository));

        await Assert.ThrowsAsync<AuthorizationException>(() => behavior.Handle(
            command,
            _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)),
            CancellationToken.None));
        await _authService.DidNotReceiveWithAnyArgs().AuthorizeAsync(default!, default);
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
            authorizationContextEnricher: new UpdateEventSessionSpeakerAuthorizationContextEnricher(assignmentRepository, sessionRepository, tenantContext));

        await Assert.ThrowsAsync<AuthorizationException>(() => behavior.Handle(
            command,
            _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)),
            CancellationToken.None));
        await _authService.DidNotReceiveWithAnyArgs().AuthorizeAsync(default!, default);
    }

    [Test]
    public async Task Handle_WithOrganizerClaim_PreservesClaimMetadataAndEnrichesParentEvent()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var eventId = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var authorizationEvent = CreateAuthorizationEvent(eventId, tenantId, organizationId);
        eventRepository.GetEventWithDetails(eventId).Returns(authorizationEvent);

        // Reviewing a claim is decided by authority over the parent event, so the resolver replaces the
        // request's event reference with the persisted event's facts. The claim identifier is not a policy
        // input for review and is deliberately absent.
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.EventOrganizerClaim,
                eventId.ToString(),
                AuthorizationActions.Events.ReviewOrganizerClaim,
                new EventAuthorizationFacts(
                    tenantId,
                    eventId,
                    authorizationEvent.ActorId,
                    UserId: null,
                    OrganizationId: organizationId,
                    GroupId: null,
                    OrganizerActorId: null,
                    OrganizerUserId: null,
                    OrganizerOrganizationId: null,
                    OrganizerGroupId: null,
                    ProvenanceType: UnsetProvenanceCode,
                    SubmittedByUserId: null)),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);
        var behavior = new AuthorizationBehavior<ReviewEventOrganizerClaimCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<ReviewEventOrganizerClaimCommand, BaseCommandResponse<Guid>>>>(),
            new AuthorizationResourceContextResolver(eventRepository: eventRepository));
        var request = new ReviewEventOrganizerClaimCommand
        {
            EventId = eventId,
            ClaimId = claimId,
            Review = new ReviewEventOrganizerClaimDto
            {
                Decision = EventOrganizerClaimReviewDecisionDto.Reject,
                ReasonCode = "NOT_VERIFIED",
                ExpectedConcurrencyStamp = Guid.NewGuid()
            }
        };

        var result = await behavior.Handle(
            request,
            _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
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
        var organizationId = Guid.NewGuid();
        var expectedConcurrencyStamp = Guid.NewGuid();
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
        var authorizationEvent = CreateAuthorizationEvent(
            persistedEventId,
            tenantId,
            organizationId);
        var expectedFacts = new EventOrganizerClaimAuthorizationFacts(
            tenantId,
            persistedEventId,
            claim.Id,
            claimantActorId,
            ClaimantUserId: claimantUserId,
            ClaimantOrganizationId: null,
            ClaimantGroupId: null,
            Status: claim.Status?.MasterCode ?? claim.StatusId.ToString());
        eventRepository.GetEventWithDetails(persistedEventId).Returns(authorizationEvent);
        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                ResourceKinds.EventOrganizerClaim,
                claim.Id.ToString("D"),
                AuthorizationActions.Events.WithdrawOrganizerClaim,
                expectedFacts),
                Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);
        var behavior = new AuthorizationBehavior<WithdrawEventOrganizerClaimCommand, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<WithdrawEventOrganizerClaimCommand, BaseCommandResponse<Guid>>>>(),
            new AuthorizationResourceContextResolver(eventRepository: eventRepository),
            authorizationContextEnricher: new WithdrawEventOrganizerClaimAuthorizationContextEnricher(claimRepository, actorRepository, tenantContext));
        var request = new WithdrawEventOrganizerClaimCommand
        {
            EventId = routeEventId,
            ClaimId = claim.Id,
            ExpectedConcurrencyStamp = expectedConcurrencyStamp
        };

        var result = await behavior.Handle(
            request,
            _ => Task.FromResult(BaseCommandResponse.Success(Guid.Empty)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(request.EventId).IsEqualTo(routeEventId);
        await Assert.That(request.ClaimId).IsEqualTo(claim.Id);
        await Assert.That(request.ExpectedConcurrencyStamp).IsEqualTo(expectedConcurrencyStamp);
        await claimRepository.Received(1).GetDetailsAsync(claim.Id, false, Arg.Any<CancellationToken>());
        await actorRepository.Received(1).GetActorWithDetails(claimantActorId, Arg.Any<CancellationToken>());
        await eventRepository.Received(1).GetEventWithDetails(persistedEventId);
        await eventRepository.DidNotReceive().GetEventWithDetails(routeEventId);
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
            ResourceKinds.EventOrganizerClaim,
            claim.Id.ToString("D"),
            AuthorizationActions.Events.WithdrawOrganizerClaim,
            expectedFacts),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithAuthorizeResourceAndISecureRequest_NullResourceId_FallsBackToTypeName()
    {
        // Arrange
        var secureBehavior = new AuthorizationBehavior<TestSecureCommandWithNullId, BaseCommandResponse<Guid>>(
            _authService,
            Substitute.For<ILogger<AuthorizationBehavior<TestSecureCommandWithNullId, BaseCommandResponse<Guid>>>>());
        var command = new TestSecureCommandWithNullId();
        var expectedResponse = BaseCommandResponse.Success(Guid.Empty);

        _authService.AuthorizeAsync(
            MatchesAuthorizationRequest(
                "islamuevent_organization", nameof(TestSecureCommandWithNullId), "delete"),
            Arg.Any<CancellationToken>())
            .Returns(AllowedDecision);

        // Act
        var result = await secureBehavior.Handle(command, _ => Task.FromResult(expectedResponse), CancellationToken.None);


        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await _authService.Received(1).AuthorizeAsync(
            MatchesAuthorizationRequest(
                "islamuevent_organization",
                nameof(TestSecureCommandWithNullId),
                "delete"),
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
            LinkedUserId = userId,
            Event = null!,
            EventSession = null!,
            LinkedUser = null!,
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

    /// <summary>
    /// Matches the request the pipeline hands to the provider. When <paramref name="expectedFacts"/> is
    /// supplied the match is exact: the facts the provider sees must be that record and nothing more, so a
    /// request that publishes an extra identifier fails the test rather than silently widening the input.
    /// </summary>
    private static AuthorizationRequest MatchesAuthorizationRequest(
        string resourceKind,
        string resourceId,
        string action,
        IAuthorizationFacts? expectedFacts = null) =>
        Arg.Is<AuthorizationRequest>(request =>
            request != null &&
            request.ResourceKind == resourceKind &&
            request.ResourceId == resourceId &&
            request.Action == action &&
            (expectedFacts == null || Equals(request.Facts, expectedFacts)));

    /// <summary>
    /// Provenance code published for the test events below, which carry no provenance master row and so
    /// fall back to the unset identifier.
    /// </summary>
    private const string UnsetProvenanceCode = "0";

    /// <summary>
    /// Pins the pipeline's provider-neutral outcomes: the behavior forwards whatever facts the request
    /// declares and honours the provider's verdict, including when the declared facts are absent or name a
    /// different resource than the one being addressed.
    /// </summary>
    private static IReadOnlyList<PlatformAuthorizationScenario> PlatformAuthorizationScenarios()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        return
        [
            new(
                "normal_allow",
                eventId.ToString("D"),
                new EventScopedAuthorizationFacts(tenantId, eventId),
                true,
                AuthorizationScenarioOutcome.Allowed),
            new(
                "normal_deny",
                eventId.ToString("D"),
                new EventScopedAuthorizationFacts(tenantId, eventId),
                false,
                AuthorizationScenarioOutcome.ProviderDenied),
            new(
                "missing_facts",
                eventId.ToString("D"),
                null,
                false,
                AuthorizationScenarioOutcome.ProviderDenied),
            new(
                "missing_tenant",
                eventId.ToString("D"),
                new EventScopedAuthorizationFacts(Guid.Empty, eventId),
                false,
                AuthorizationScenarioOutcome.ProviderDenied),
            new(
                "wrong_resource_facts",
                eventId.ToString("D"),
                new EventScopedAuthorizationFacts(Guid.NewGuid(), Guid.NewGuid()),
                false,
                AuthorizationScenarioOutcome.ProviderDenied)
        ];
    }

    private static IReadOnlyList<RegistrationFormPipelineDenialScenario> RegistrationFormPipelineDenialScenarios()
    {
        var missingEventId = Guid.NewGuid();
        var wrongTenantEventId = Guid.NewGuid();
        var ambientTenantId = Guid.NewGuid();

        return
        [
            new("missing_resource", missingEventId, ambientTenantId, null),
            new(
                "wrong_tenant",
                wrongTenantEventId,
                ambientTenantId,
                CreateAuthorizationEvent(wrongTenantEventId, Guid.NewGuid(), Guid.NewGuid()))
        ];
    }
}

public enum AuthorizationScenarioOutcome
{
    Allowed,
    ProviderDenied
}

public sealed record PlatformAuthorizationScenario(
    string Name,
    string ResourceId,
    IAuthorizationFacts? Facts,
    bool AllowedByProvider,
    AuthorizationScenarioOutcome ExpectedOutcome);

public sealed record RegistrationFormPipelineDenialScenario(
    string Name,
    Guid EventId,
    Guid AmbientTenantId,
    Explore.Domain.Event? PersistedEvent);

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

// Test command with [AuthorizeResource] enum + ISecureRequest providing ResourceId and typed facts
[AuthorizeResource("islamuevent_organization", AuthorizationActions.Delete)]
public class TestSecureCommandWithAttributes : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid OrganizationId { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; } = Guid.NewGuid();

    string? ISecureRequest.ResourceId => OrganizationId.ToString();
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new OrganizationAuthorizationFacts(TenantId, OrganizationId);
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

[AuthorizeResource(ResourceKinds.RegistrationForm, AuthorizationActions.RegistrationForms.Update)]
public sealed class TestRegistrationFormSecureCommand(
    Guid formId,
    Guid eventId,
    Guid tenantId = default) : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    string? ISecureRequest.ResourceId => formId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new EventScopedAuthorizationFacts(tenantId, eventId);
}

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Update)]
public sealed class TestAuthorizationPlatformCommand(
    string resourceId,
    IAuthorizationFacts? facts = null) : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    string? ISecureRequest.ResourceId => resourceId;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts => facts;
}
