// ABOUTME: Unit tests for event registration update command handling.
// ABOUTME: Verifies validation failures, missing rows, and successful mapper-backed updates.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Features.EventRegistrations.Handlers.Commands;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventRegistrations.Commands;

public sealed class UpdateEventRegistrationCommandHandlerTests
{
    private readonly IEventRegistrationRepository _eventRegistrationRepository = Substitute.For<IEventRegistrationRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly IApprovalStatusRepository _approvalStatusRepository = Substitute.For<IApprovalStatusRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly UpdateEventRegistrationCommandHandler _handler;

    public UpdateEventRegistrationCommandHandlerTests()
    {
        _handler = new UpdateEventRegistrationCommandHandler(
            _eventRegistrationRepository,
            _userRepository,
            _eventSessionRepository,
            _approvalStatusRepository,
            _mapper);
    }

    [Test]
    public async Task Handle_WithValidRegistration_MapsAndPersistsExistingEntity()
    {
        var dto = CreateValidDto();
        var registration = CreateRegistration(dto.Id);
        SetupValidLookups(dto);
        _eventRegistrationRepository.GetById(dto.Id).Returns(registration);

        var result = await _handler.Handle(new UpdateEventRegistrationCommand { EventRegistrationDto = dto }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(dto.Id);
        await Assert.That(result.Message).IsEqualTo("Event Registration updated successfully.");
        _mapper.Received(1).Map(dto, registration);
        await _eventRegistrationRepository.Received(1).Update(registration);
    }

    [Test]
    public async Task Handle_WhenRegistrationDoesNotExist_ReturnsNotFoundAndDoesNotPersist()
    {
        var dto = CreateValidDto();
        SetupValidLookups(dto);
        _eventRegistrationRepository.GetById(dto.Id).Returns((EventRegistration?)null);

        var result = await _handler.Handle(new UpdateEventRegistrationCommand { EventRegistrationDto = dto }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Event Registration not found.");
        _mapper.DidNotReceive().Map(Arg.Any<UpdateEventRegistrationDto>(), Arg.Any<EventRegistration>());
        await _eventRegistrationRepository.DidNotReceive().Update(Arg.Any<EventRegistration>());
    }

    [Test]
    public async Task Handle_WhenLookupValidationFails_ReturnsErrorsAndSkipsRepositoryRead()
    {
        var dto = CreateValidDto();
        _userRepository.Exists(dto.UserId).Returns(false);
        _eventSessionRepository.Exists(dto.EventSessionId).Returns(true);
        _approvalStatusRepository.Exists(dto.ApprovalStatusId!.Value).Returns(true);

        var result = await _handler.Handle(new UpdateEventRegistrationCommand { EventRegistrationDto = dto }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Event Registration update failed.");
        await Assert.That(result.Errors).Contains("User Id not found");
        await _eventRegistrationRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await _eventRegistrationRepository.DidNotReceive().Update(Arg.Any<EventRegistration>());
    }

    private void SetupValidLookups(UpdateEventRegistrationDto dto)
    {
        _userRepository.Exists(dto.UserId).Returns(true);
        _eventSessionRepository.Exists(dto.EventSessionId).Returns(true);
        _approvalStatusRepository.Exists(dto.ApprovalStatusId!.Value).Returns(true);
    }

    private static UpdateEventRegistrationDto CreateValidDto() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        EventSessionId = Guid.NewGuid(),
        ApprovalStatusId = 1,
        TenantId = Guid.NewGuid()
    };

    private static EventRegistration CreateRegistration(Guid id) => new()
    {
        Id = id,
        EventId = Guid.NewGuid(),
        Event = null!,
        UserId = Guid.NewGuid(),
        User = null!,
        EventSessionId = Guid.NewGuid(),
        EventSession = null!,
        ApprovalStatusId = 1,
        TenantId = Guid.NewGuid(),
        Tenant = null!
    };
}
