// ABOUTME: Unit tests for grouped event registration PATCH command handling.
// ABOUTME: Covers validation, concurrency, relationship checks, one-save updates, and cache invalidation.

using Explore.Application.Caching;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventRegistrations.Handlers.Commands;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventRegistrations.Commands;

public sealed class UpdateEventRegistrationCommandHandlerTests
{
    private readonly IEventRegistrationRepository _eventRegistrationRepository = Substitute.For<IEventRegistrationRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly IApprovalStatusRepository _approvalStatusRepository = Substitute.For<IApprovalStatusRepository>();
    private readonly IEventRegistrationIntentRepository _intentRepository = Substitute.For<IEventRegistrationIntentRepository>();
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IUnitOfWork _unitOfWork = new ImmediateUnitOfWork();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IRecipientNotificationMaterializer _recipientNotificationMaterializer = Substitute.For<IRecipientNotificationMaterializer>();
    private readonly IEventLifecycleScheduler _eventLifecycleScheduler = Substitute.For<IEventLifecycleScheduler>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly UpdateEventRegistrationCommandHandler _handler;

    public UpdateEventRegistrationCommandHandlerTests()
    {
        _recipientNotificationMaterializer.MaterializeInCurrentTransactionAsync(
                Arg.Any<RecipientNotificationMaterialization>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                RecipientNotificationMaterialization request = call.ArgAt<RecipientNotificationMaterialization>(0);
                return new RecipientNotificationMaterializationResult(
                    new NotificationIntent
                    {
                        Id = request.IntentId,
                        TenantId = request.Intent.TenantId!.Value,
                        TemplateKey = request.Intent.TemplateKey!,
                        DeduplicationKey = request.Intent.DeduplicationKey!
                    },
                    [],
                    null,
                    request.Email);
            });
        _eventRegistrationRepository.UpdateAndAdjustCapacityAsync(
                Arg.Any<EventRegistration>(),
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var registration = call.ArgAt<EventRegistration>(0);
                return new EventRegistrationTransitionResult(
                    Changed: true,
                    ParentIntentId: registration.EventRegistrationIntentId,
                    PreviousStatus: registration.ApprovalStatusId,
                    FinalStatus: registration.ApprovalStatusId,
                    TransitionReason: EventRegistrationTransitionReason.Updated,
                    OccurrenceId: call.ArgAt<Guid>(1),
                    OccurredAt: call.ArgAt<DateTimeOffset>(2),
                    ActorProvenance: call.ArgAt<EventRegistrationActorProvenance>(3),
                    ActorUserId: call.ArgAt<Guid?>(4),
                    ChildTransitions: []);
            });
        _handler = new UpdateEventRegistrationCommandHandler(
            _eventRegistrationRepository,
            _userRepository,
            _eventSessionRepository,
            _approvalStatusRepository,
            _intentRepository,
            _eventRepository,
            _unitOfWork,
            _currentUserService,
            new RegistrationNotificationDeliveryService(new EventLifecycleEmailOutboxFactory()),
            _recipientNotificationMaterializer,
            _eventLifecycleScheduler,
            _cache);
    }

    [Test]
    public async Task HandleWaitlistPromotionMaterializesOneGraphAndReturnsAuthoritativeReplacementId()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid registrationIntentId = Guid.CreateVersion7();
        Guid replacementRegistrationId = Guid.CreateVersion7();
        Guid stamp = Guid.CreateVersion7();
        EventRegistration registration = CreateRegistration(Guid.CreateVersion7(), tenantId, eventId);
        registration.EventRegistrationIntentId = registrationIntentId;
        registration.ApprovalStatusId = (int)ApprovalStatusEnum.Waitlisted;
        registration.ConcurrencyStamp = stamp;
        EventRegistrationIntent intent = CreateIntent(registrationIntentId, tenantId, eventId, registration.UserId);
        _eventRegistrationRepository.GetById(registration.Id).Returns(registration);
        _intentRepository.GetById(registrationIntentId).Returns(intent);
        _eventRepository.GetById(eventId).Returns(CreateEvent(eventId, tenantId));
        _userRepository.GetById(registration.UserId).Returns(CreateUser(registration.UserId));
        _approvalStatusRepository.Exists((int)ApprovalStatusEnum.Approved).Returns(true);
        _eventRegistrationRepository.UpdateAndAdjustCapacityAsync(
                registration,
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(call => new EventRegistrationTransitionResult(
                Changed: true,
                ParentIntentId: registrationIntentId,
                PreviousStatus: (int)ApprovalStatusEnum.Waitlisted,
                FinalStatus: (int)ApprovalStatusEnum.Approved,
                TransitionReason: EventRegistrationTransitionReason.ApprovalStatusChanged,
                OccurrenceId: call.ArgAt<Guid>(1),
                OccurredAt: call.ArgAt<DateTimeOffset>(2),
                ActorProvenance: call.ArgAt<EventRegistrationActorProvenance>(3),
                ActorUserId: call.ArgAt<Guid?>(4),
                ChildTransitions:
                [
                    new EventRegistrationChildTransition(
                        replacementRegistrationId,
                        registration.EventSessionId,
                        (int)ApprovalStatusEnum.Waitlisted,
                        (int)ApprovalStatusEnum.Approved)
                ]));

        var result = await _handler.Handle(
            new UpdateEventRegistrationCommand
            {
                EventRegistrationId = registration.Id,
                ExpectedConcurrencyStamp = stamp,
                EventRegistrationDto = new UpdateEventRegistrationDto
                {
                    ApprovalStatus = new UpdateEventRegistrationApprovalStatusDto
                    {
                        ApprovalStatusId = OptionalUpdate<int?>.Set((int)ApprovalStatusEnum.Approved)
                    }
                }
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(replacementRegistrationId);
        await _recipientNotificationMaterializer.Received(1).MaterializeInCurrentTransactionAsync(
            Arg.Is<RecipientNotificationMaterialization>(request =>
                request.Intent.TemplateKey == "registration.waitlist-promoted"
                && request.Email != null
                && request.Email.Kind == EmailDispatchKind.WaitlistPromoted),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleChildOnlyChangeWithUnchangedParentCreatesNoNotificationGraph()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid registrationIntentId = Guid.CreateVersion7();
        Guid stamp = Guid.CreateVersion7();
        EventRegistration registration = CreateRegistration(Guid.CreateVersion7(), tenantId, eventId);
        registration.EventRegistrationIntentId = registrationIntentId;
        registration.ApprovalStatusId = (int)ApprovalStatusEnum.Approved;
        registration.ConcurrencyStamp = stamp;
        _eventRegistrationRepository.GetById(registration.Id).Returns(registration);
        _intentRepository.GetById(registrationIntentId)
            .Returns(CreateIntent(registrationIntentId, tenantId, eventId, registration.UserId));
        _approvalStatusRepository.Exists((int)ApprovalStatusEnum.Approved).Returns(true);
        _eventRegistrationRepository.UpdateAndAdjustCapacityAsync(
                registration,
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(call => new EventRegistrationTransitionResult(
                Changed: true,
                ParentIntentId: registrationIntentId,
                PreviousStatus: (int)ApprovalStatusEnum.Approved,
                FinalStatus: (int)ApprovalStatusEnum.Approved,
                TransitionReason: EventRegistrationTransitionReason.Updated,
                OccurrenceId: call.ArgAt<Guid>(1),
                OccurredAt: call.ArgAt<DateTimeOffset>(2),
                ActorProvenance: call.ArgAt<EventRegistrationActorProvenance>(3),
                ActorUserId: call.ArgAt<Guid?>(4),
                ChildTransitions: []));

        await _handler.Handle(
            new UpdateEventRegistrationCommand
            {
                EventRegistrationId = registration.Id,
                ExpectedConcurrencyStamp = stamp,
                EventRegistrationDto = new UpdateEventRegistrationDto
                {
                    ApprovalStatus = new UpdateEventRegistrationApprovalStatusDto
                    {
                        ApprovalStatusId = OptionalUpdate<int?>.Set((int)ApprovalStatusEnum.Approved)
                    }
                }
            },
            CancellationToken.None);

        await _recipientNotificationMaterializer.DidNotReceive().MaterializeInCurrentTransactionAsync(
            Arg.Any<RecipientNotificationMaterialization>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithEmptyWrapper_ReturnsFailedResponseWithoutSaving()
    {
        var command = new UpdateEventRegistrationCommand
        {
            EventRegistrationId = Guid.NewGuid(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            EventRegistrationDto = new UpdateEventRegistrationDto()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("At least one event registration update group must be provided.");
        await _eventRegistrationRepository.DidNotReceive().UpdateAndAdjustCapacityAsync(
            Arg.Any<EventRegistration>(),
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRegistrationDoesNotExist_ReturnsNotFoundAndDoesNotPersist()
    {
        var registrationId = Guid.NewGuid();
        _eventRegistrationRepository.GetById(registrationId).Returns((EventRegistration?)null);

        var command = new UpdateEventRegistrationCommand
        {
            EventRegistrationId = registrationId,
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            EventRegistrationDto = new UpdateEventRegistrationDto
            {
                ApprovalStatus = new UpdateEventRegistrationApprovalStatusDto
                {
                    ApprovalStatusId = OptionalUpdate<int?>.Set(1)
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Event Registration not found.");
        await _eventRegistrationRepository.DidNotReceive().UpdateAndAdjustCapacityAsync(
            Arg.Any<EventRegistration>(),
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithStaleConcurrencyStamp_ThrowsConflictWithoutSaving()
    {
        var registrationId = Guid.NewGuid();
        var registration = CreateRegistration(registrationId, Guid.NewGuid(), Guid.NewGuid());
        registration.ConcurrencyStamp = Guid.NewGuid();
        _eventRegistrationRepository.GetById(registrationId).Returns(registration);

        var command = new UpdateEventRegistrationCommand
        {
            EventRegistrationId = registrationId,
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            EventRegistrationDto = new UpdateEventRegistrationDto
            {
                ApprovalStatus = new UpdateEventRegistrationApprovalStatusDto
                {
                    ApprovalStatusId = OptionalUpdate<int?>.Set(1)
                }
            }
        };

        await Assert.That(async () => await _handler.Handle(command, CancellationToken.None))
            .Throws<ConcurrencyConflictException>();
        await _eventRegistrationRepository.DidNotReceive().UpdateAndAdjustCapacityAsync(
            Arg.Any<EventRegistration>(),
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithApprovalStatusClear_SavesOnceAndInvalidatesEventCaches()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        var registration = CreateRegistration(Guid.NewGuid(), tenantId, eventId);
        registration.ApprovalStatusId = 1;
        registration.ConcurrencyStamp = stamp;
        _eventRegistrationRepository.GetById(registration.Id).Returns(registration);

        var command = new UpdateEventRegistrationCommand
        {
            EventRegistrationId = registration.Id,
            ExpectedConcurrencyStamp = stamp,
            EventRegistrationDto = new UpdateEventRegistrationDto
            {
                ApprovalStatus = new UpdateEventRegistrationApprovalStatusDto
                {
                    ApprovalStatusId = OptionalUpdate<int?>.Set(null)
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(registration.ApprovalStatusId).IsNull();
        await _eventRegistrationRepository.Received(1).UpdateAndAdjustCapacityAsync(
            registration,
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithSessionChangeWithinEvent_UpdatesSessionAndInvalidatesEventCache()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var newSessionId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        var registration = CreateRegistration(Guid.NewGuid(), tenantId, eventId);
        registration.ConcurrencyStamp = stamp;
        var newSession = CreateSession(newSessionId, tenantId, eventId);
        _eventRegistrationRepository.GetById(registration.Id).Returns(registration);
        _eventSessionRepository.GetById(newSessionId).Returns(newSession);
        _eventRegistrationRepository.GetRegistrationByUserAndSession(registration.UserId, newSessionId)
            .Returns((EventRegistration?)null);

        var command = new UpdateEventRegistrationCommand
        {
            EventRegistrationId = registration.Id,
            ExpectedConcurrencyStamp = stamp,
            EventRegistrationDto = new UpdateEventRegistrationDto
            {
                Session = new UpdateEventRegistrationSessionDto { EventSessionId = newSessionId }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(registration.EventSessionId).IsEqualTo(newSessionId);
        await Assert.That(registration.EventId).IsEqualTo(eventId);
        await _eventRegistrationRepository.Received(1).UpdateAndAdjustCapacityAsync(
            registration,
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithParentIntentReassignment_ReturnsValidationFailureWithoutSaving()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        var registration = CreateRegistration(Guid.NewGuid(), tenantId, eventId);
        registration.ConcurrencyStamp = stamp;
        var intentId = Guid.NewGuid();
        _eventRegistrationRepository.GetById(registration.Id).Returns(registration);

        var command = new UpdateEventRegistrationCommand
        {
            EventRegistrationId = registration.Id,
            ExpectedConcurrencyStamp = stamp,
            EventRegistrationDto = new UpdateEventRegistrationDto
            {
                Intent = new UpdateEventRegistrationIntentDto
                {
                    EventRegistrationIntentId = OptionalUpdate<Guid?>.Set(intentId)
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors)
            .Contains("Registration user, event, tenant, and parent intent are immutable.");
        await _eventRegistrationRepository.DidNotReceive().UpdateAndAdjustCapacityAsync(
            Arg.Any<EventRegistration>(),
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
        await _intentRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Handle_WithUserReassignment_ReturnsValidationFailureWithoutSaving()
    {
        var stamp = Guid.NewGuid();
        var registration = CreateRegistration(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        registration.EventRegistrationIntentId = Guid.NewGuid();
        registration.ConcurrencyStamp = stamp;
        var replacementUserId = Guid.NewGuid();
        _eventRegistrationRepository.GetById(registration.Id).Returns(registration);
        _userRepository.GetById(replacementUserId).Returns(new User
        {
            Id = replacementUserId,
            Pii = new UserPii
            {
                Email = "replacement@example.com",
                FirstName = "Replacement",
                LastName = "User"
            }
        });

        var command = new UpdateEventRegistrationCommand
        {
            EventRegistrationId = registration.Id,
            ExpectedConcurrencyStamp = stamp,
            EventRegistrationDto = new UpdateEventRegistrationDto
            {
                User = new UpdateEventRegistrationUserDto { UserId = replacementUserId }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors)
            .Contains("Registration user, event, tenant, and parent intent are immutable.");
        await _eventRegistrationRepository.DidNotReceive().UpdateAndAdjustCapacityAsync(
            Arg.Any<EventRegistration>(),
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Handle_WithCrossEventSessionMove_ReturnsValidationFailureWithoutSaving()
    {
        var tenantId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        var registration = CreateRegistration(Guid.NewGuid(), tenantId, Guid.NewGuid());
        registration.ConcurrencyStamp = stamp;
        var replacementSession = CreateSession(Guid.NewGuid(), tenantId, Guid.NewGuid());
        _eventRegistrationRepository.GetById(registration.Id).Returns(registration);
        _eventSessionRepository.GetById(replacementSession.Id).Returns(replacementSession);

        var command = new UpdateEventRegistrationCommand
        {
            EventRegistrationId = registration.Id,
            ExpectedConcurrencyStamp = stamp,
            EventRegistrationDto = new UpdateEventRegistrationDto
            {
                Session = new UpdateEventRegistrationSessionDto
                {
                    EventSessionId = replacementSession.Id
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors)
            .Contains("Registration user, event, tenant, and parent intent are immutable.");
        await _eventRegistrationRepository.DidNotReceive().UpdateAndAdjustCapacityAsync(
            Arg.Any<EventRegistration>(),
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Handle_WithAdministrativeRevocation_UsesCapacityAwareRepository()
    {
        var stamp = Guid.NewGuid();
        var registration = CreateRegistration(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        registration.ApprovalStatusId = (int)ApprovalStatusEnum.Approved;
        registration.ConcurrencyStamp = stamp;
        _eventRegistrationRepository.GetById(registration.Id).Returns(registration);
        _approvalStatusRepository.Exists((int)ApprovalStatusEnum.Revoked).Returns(true);

        var command = new UpdateEventRegistrationCommand
        {
            EventRegistrationId = registration.Id,
            ExpectedConcurrencyStamp = stamp,
            EventRegistrationDto = new UpdateEventRegistrationDto
            {
                ApprovalStatus = new UpdateEventRegistrationApprovalStatusDto
                {
                    ApprovalStatusId = OptionalUpdate<int?>.Set((int)ApprovalStatusEnum.Revoked)
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(registration.ApprovalStatusId).IsEqualTo((int)ApprovalStatusEnum.Revoked);
        await _eventRegistrationRepository.Received(1).UpdateAndAdjustCapacityAsync(
            registration,
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments((int)ApprovalStatusEnum.Cancelled)]
    [Arguments((int)ApprovalStatusEnum.Revoked)]
    public async Task Handle_WhenTerminalRegistrationIsReopened_ReturnsFailureWithoutSaving(
        int terminalApprovalStatusId)
    {
        var stamp = Guid.NewGuid();
        var registration = CreateRegistration(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        registration.ApprovalStatusId = terminalApprovalStatusId;
        registration.ConcurrencyStamp = stamp;
        _eventRegistrationRepository.GetById(registration.Id).Returns(registration);

        var command = new UpdateEventRegistrationCommand
        {
            EventRegistrationId = registration.Id,
            ExpectedConcurrencyStamp = stamp,
            EventRegistrationDto = new UpdateEventRegistrationDto
            {
                ApprovalStatus = new UpdateEventRegistrationApprovalStatusDto
                {
                    ApprovalStatusId = OptionalUpdate<int?>.Set((int)ApprovalStatusEnum.Approved)
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Terminal registration approval statuses cannot be changed.");
        await _eventRegistrationRepository.DidNotReceive().UpdateAndAdjustCapacityAsync(
            Arg.Any<EventRegistration>(),
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    private static EventRegistration CreateRegistration(Guid id, Guid tenantId, Guid eventId)
    {
        return new EventRegistration
        {
            Id = id,
            EventId = eventId,
            Event = null!,
            UserId = Guid.NewGuid(),
            User = null!,
            EventSessionId = Guid.NewGuid(),
            EventSession = null!,
            ApprovalStatusId = 1,
            TenantId = tenantId,
            Tenant = null!
        };
    }

    private static EventSession CreateSession(Guid id, Guid tenantId, Guid eventId)
    {
        return new EventSession
        {
            Id = id,
            EventId = eventId,
            Event = null!,
            TenantId = tenantId,
            Tenant = null!
        };
    }

    private static EventRegistrationIntent CreateIntent(Guid id, Guid tenantId, Guid eventId, Guid userId)
    {
        return new EventRegistrationIntent
        {
            Id = id,
            TenantId = tenantId,
            Tenant = null!,
            EventId = eventId,
            Event = null!,
            UserId = userId,
            User = null!,
            RegistrationScope = null!
        };
    }

    private static Explore.Domain.Event CreateEvent(Guid id, Guid tenantId)
    {
        return new Explore.Domain.Event
        {
            Id = id,
            TenantId = tenantId,
            Tenant = null!,
            Actor = null!,
            Title = "Community Iftar",
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!
        };
    }

    private static User CreateUser(Guid id)
    {
        var user = new User
        {
            Id = id,
            EmailVerified = true,
            Pii = new UserPii
            {
                UserId = id,
                Email = "attendee@example.test",
                FirstName = "Test",
                LastName = "Attendee"
            }
        };
        user.Pii.User = user;
        return user;
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default) => operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => operation(ct);

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => operation(ct);
    }
}
