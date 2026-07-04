// ABOUTME: Unit tests for AuthorizationBehavior MediatR pipeline behavior.
// ABOUTME: Verifies authorization enforcement via IAuthorizedRequest (legacy), [AuthorizeResource], and pass-through.

#pragma warning disable CS0618 // IAuthorizedRequest is obsolete — tests must still exercise the legacy code path

using Explore.Application.Authorization;
using Explore.Application.Behaviors;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventCustomPropertyProjections.Requests.Queries;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Queries;
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
            Actor = new Actor
            {
                Id = actorId,
                TenantId = tenantId,
                OrganizationId = organizationId,
                ActorTypeId = 2,
                ActorType = null!,
                Tenant = null!,
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
            OrganizationId = organizationId,
            Organization = null!,
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
