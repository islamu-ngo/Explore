// ABOUTME: Unit tests for event registration creation capacity and waitlist behavior.
// ABOUTME: Verifies handlers call the capacity-aware repository contract and surface waitlist outcomes.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Features.EventRegistrations.Handlers.Commands;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventRegistrations.Commands;

public sealed class CreateEventRegistrationCommandHandlerTests
{
    private readonly IEventRegistrationIntentRepository _intentRepository = Substitute.For<IEventRegistrationIntentRepository>();
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEventDayRepository _eventDayRepository = Substitute.For<IEventDayRepository>();
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly IApprovalStatusRepository _approvalStatusRepository = Substitute.For<IApprovalStatusRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IContactShareConsentService _consentService = Substitute.For<IContactShareConsentService>();
    private readonly CreateEventRegistrationCommandHandler _handler;

    public CreateEventRegistrationCommandHandlerTests()
    {
        _handler = new CreateEventRegistrationCommandHandler(
            _intentRepository,
            _eventRepository,
            _userRepository,
            _eventDayRepository,
            _eventSessionRepository,
            _approvalStatusRepository,
            _tenantContext,
            CreateBusinessMetrics(),
            _consentService,
            new EventLifecycleEmailOutboxFactory(),
            Substitute.For<ILogger<CreateEventRegistrationCommandHandler>>());
    }

    [Test]
    public async Task HandleWhenCapacityIsAvailableReturnsConfirmedRegistration()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = CreateSessionRegistrationCommand(eventId, userId, sessionId);
        SetupValidRegistration(tenantId, eventId, userId, sessionId);

        _intentRepository.CreateWithChildrenAndCapacityAsync(
                Arg.Any<EventRegistrationIntent>(),
                Arg.Any<IReadOnlyList<EventRegistration>>(),
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<CancellationToken>(),
                Arg.Any<EmailDispatchOutbox?>())
            .Returns(callInfo =>
            {
                var intent = callInfo.ArgAt<EventRegistrationIntent>(0);
                return new EventRegistrationIntentCreationResult(intent, []);
            });

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event Registration created successfully.");
        await _intentRepository.Received(1).CreateWithChildrenAndCapacityAsync(
            Arg.Is<EventRegistrationIntent>(intent =>
                intent != null
                && intent.ApprovalStatusId == (int)ApprovalStatusEnum.Approved
                && intent.TenantId == tenantId),
            Arg.Is<IReadOnlyList<EventRegistration>>(children =>
                children != null
                && children.Count == 1
                && children[0].EventSessionId == sessionId
                && children[0].ApprovalStatusId == (int)ApprovalStatusEnum.Approved),
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            Arg.Any<CancellationToken>(),
            Arg.Is<EmailDispatchOutbox>(outbox =>
                outbox != null
                && outbox.TenantId == tenantId
                && outbox.Kind == EmailDispatchKind.RegistrationConfirmation
                && outbox.SourceType == "event_registration_intent"
                && outbox.EventId == eventId
                && outbox.UserId == userId
                && outbox.RecipientEmail == "registrant@example.test"
                && outbox.Status == EmailDispatchStatus.Pending));
    }

    [Test]
    public async Task HandleWhenSessionIsFullReturnsWaitlistMessage()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = CreateSessionRegistrationCommand(eventId, userId, sessionId);
        SetupValidRegistration(tenantId, eventId, userId, sessionId);

        _intentRepository.CreateWithChildrenAndCapacityAsync(
                Arg.Any<EventRegistrationIntent>(),
                Arg.Any<IReadOnlyList<EventRegistration>>(),
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<CancellationToken>(),
                Arg.Any<EmailDispatchOutbox?>())
            .Returns(callInfo =>
            {
                var intent = callInfo.ArgAt<EventRegistrationIntent>(0);
                intent.ApprovalStatusId = (int)ApprovalStatusEnum.Waitlisted;
                return new EventRegistrationIntentCreationResult(intent, [sessionId]);
            });

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event Registration added to the waitlist.");
    }

    [Test]
    public async Task HandleWhenEventScopeOmitsSelectedSessionsCreatesRowsForEveryEventSession()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var firstSessionId = Guid.NewGuid();
        var secondSessionId = Guid.NewGuid();
        var command = new CreateEventRegistrationCommand
        {
            EventRegistrationDto = new CreateEventRegistrationDto
            {
                EventId = eventId,
                UserId = userId,
                RegistrationScopeId = (int)RegistrationScopeEnum.Event,
                SelectedSessionIds = null
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _eventRepository.Exists(eventId).Returns(true);
        _userRepository.Exists(userId).Returns(true);
        _userRepository.GetById(userId).Returns(CreateUser(userId));
        _eventRepository.GetById(eventId).Returns(CreateEvent(eventId, tenantId, EventRegistrationPolicyEnum.WholeEventOnly));
        _eventSessionRepository.GetSessionsByEvent(eventId).Returns([
            CreateEventSession(eventId, tenantId, firstSessionId),
            CreateEventSession(eventId, tenantId, secondSessionId)
        ]);
        _intentRepository.FindExistingAsync(
                eventId,
                userId,
                (int)RegistrationScopeEnum.Event,
                null,
                Arg.Any<CancellationToken>())
            .Returns((EventRegistrationIntent?)null);
        _intentRepository.CreateWithChildrenAndCapacityAsync(
                Arg.Any<EventRegistrationIntent>(),
                Arg.Any<IReadOnlyList<EventRegistration>>(),
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<CancellationToken>(),
                Arg.Any<EmailDispatchOutbox?>())
            .Returns(callInfo =>
            {
                var intent = callInfo.ArgAt<EventRegistrationIntent>(0);
                return new EventRegistrationIntentCreationResult(intent, []);
            });

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _intentRepository.Received(1).CreateWithChildrenAndCapacityAsync(
            Arg.Any<EventRegistrationIntent>(),
            Arg.Is<IReadOnlyList<EventRegistration>>(children =>
                children != null
                && children.Count == 2
                && children.Select(child => child.EventSessionId).ToHashSet().SetEquals(new[] { firstSessionId, secondSessionId })),
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            Arg.Any<CancellationToken>(),
            Arg.Any<EmailDispatchOutbox?>());
    }

    private void SetupValidRegistration(Guid tenantId, Guid eventId, Guid userId, Guid sessionId)
    {
        _tenantContext.TenantId.Returns(tenantId);
        _eventRepository.Exists(eventId).Returns(true);
        _userRepository.Exists(userId).Returns(true);
        _userRepository.GetById(userId).Returns(CreateUser(userId));
        _eventRepository.GetById(eventId).Returns(CreateEvent(eventId, tenantId));
        _eventSessionRepository.GetSessionsByEvent(eventId).Returns([CreateEventSession(eventId, tenantId, sessionId)]);
        _intentRepository.FindExistingAsync(
                eventId,
                userId,
                (int)RegistrationScopeEnum.SessionSelection,
                null,
                Arg.Any<CancellationToken>())
            .Returns((EventRegistrationIntent?)null);
    }

    private static CreateEventRegistrationCommand CreateSessionRegistrationCommand(Guid eventId, Guid userId, Guid sessionId)
    {
        return new CreateEventRegistrationCommand
        {
            EventRegistrationDto = new CreateEventRegistrationDto
            {
                EventId = eventId,
                UserId = userId,
                RegistrationScopeId = (int)RegistrationScopeEnum.SessionSelection,
                SelectedSessionIds = [sessionId]
            }
        };
    }

    private static Explore.Domain.Event CreateEvent(
        Guid eventId,
        Guid tenantId,
        EventRegistrationPolicyEnum policy = EventRegistrationPolicyEnum.SessionSelectionOnly)
    {
        return new Explore.Domain.Event
        {
            Id = eventId,
            Title = "Capacity Test Event",
            Actor = null!,
            TenantId = tenantId,
            Tenant = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventStatusId = (int)EventStatusEnum.Published,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Local,
            EventFormat = null!,
            RegistrationPolicyId = (int)policy
        };
    }

    private static EventSession CreateEventSession(Guid eventId, Guid tenantId, Guid sessionId)
    {
        return new EventSession
        {
            Id = sessionId,
            EventId = eventId,
            Event = null!,
            TenantId = tenantId,
            Tenant = null!,
            StartTime = DateTimeOffset.UtcNow.AddDays(7),
            EndTime = DateTimeOffset.UtcNow.AddDays(7).AddHours(2)
        };
    }

    private static User CreateUser(Guid userId)
    {
        var user = new User
        {
            Id = userId,
            Pii = new UserPii
            {
                UserId = userId,
                Email = "registrant@example.test",
                FirstName = "Test",
                LastName = "Registrant"
            }
        };

        user.Pii.User = user;
        return user;
    }

    private static BusinessMetrics CreateBusinessMetrics()
    {
        var services = new ServiceCollection();
        services.AddMetrics();
        var provider = services.BuildServiceProvider();
        return new BusinessMetrics(provider.GetRequiredService<IMeterFactory>());
    }
}
