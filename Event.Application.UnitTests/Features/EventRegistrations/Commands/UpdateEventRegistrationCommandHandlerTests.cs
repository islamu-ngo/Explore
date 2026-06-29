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
        await _eventRegistrationRepository.DidNotReceive().Update(Arg.Any<EventRegistration>());
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
        await _eventRegistrationRepository.DidNotReceive().Update(Arg.Any<EventRegistration>());
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
        await _eventRegistrationRepository.DidNotReceive().Update(Arg.Any<EventRegistration>());
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
        await _eventRegistrationRepository.Received(1).Update(registration);
        await _cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithSessionChange_UpdatesDerivedEventAndInvalidatesOldAndNewEventCaches()
    {
        var tenantId = Guid.NewGuid();
        var oldEventId = Guid.NewGuid();
        var newEventId = Guid.NewGuid();
        var newSessionId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        var registration = CreateRegistration(Guid.NewGuid(), tenantId, oldEventId);
        registration.ConcurrencyStamp = stamp;
        var newSession = CreateSession(newSessionId, tenantId, newEventId);
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
        await Assert.That(registration.EventId).IsEqualTo(newEventId);
        await _eventRegistrationRepository.Received(1).Update(registration);
        await _cache.Received(1).RemoveAsync($"event:detail:{newEventId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync($"event:detail:{oldEventId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithIntentFromDifferentEvent_ReturnsValidationFailureWithoutSaving()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        var registration = CreateRegistration(Guid.NewGuid(), tenantId, eventId);
        registration.ConcurrencyStamp = stamp;
        var intentId = Guid.NewGuid();
        _eventRegistrationRepository.GetById(registration.Id).Returns(registration);
        _intentRepository.GetById(intentId).Returns(new EventRegistrationIntent
        {
            Id = intentId,
            EventId = Guid.NewGuid(),
            Event = null!,
            UserId = registration.UserId,
            User = null!,
            RegistrationScopeId = 1,
            RegistrationScope = null!,
            TenantId = tenantId,
            Tenant = null!
        });

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
        await Assert.That(result.Errors).Contains("EventRegistrationIntentId must belong to the effective registration event and tenant.");
        await _eventRegistrationRepository.DidNotReceive().Update(Arg.Any<EventRegistration>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
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
