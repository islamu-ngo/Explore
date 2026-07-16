// ABOUTME: Unit tests for grouped event registration PATCH command handling.
// ABOUTME: Covers validation, concurrency, relationship checks, one-save updates, and cache invalidation.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventRegistrations.Handlers.Commands;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Models.Common;
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
    private readonly IAtprotoRecordRepository _atprotoRecordRepository = Substitute.For<IAtprotoRecordRepository>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly UpdateEventRegistrationCommandHandler _handler;

    public UpdateEventRegistrationCommandHandlerTests()
    {
        _handler = new UpdateEventRegistrationCommandHandler(
            _eventRegistrationRepository,
            _userRepository,
            _eventSessionRepository,
            _approvalStatusRepository,
            _intentRepository,
            _atprotoRecordRepository,
            _cache);
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
}
