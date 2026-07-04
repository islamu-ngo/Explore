// ABOUTME: Unit tests for current-user scoping in generic event registration read handlers.
// ABOUTME: Ensures broad read routes cannot query another user's registrations through Application handlers.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventRegistrations.Handlers.Queries;
using Explore.Application.Features.EventRegistrations.Requests.Queries;
using Explore.Domain;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventRegistrations.Queries;

public class EventRegistrationSelfReadQueryHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IEventRegistrationRepository _eventRegistrationRepository = Substitute.For<IEventRegistrationRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    [Test]
    public async Task ListHandler_UsesCurrentUserIdForPagedRepositoryRead()
    {
        var currentUserId = Guid.NewGuid();
        var registrations = new List<EventRegistration> { CreateRegistration(currentUserId) };
        var dtos = new List<EventRegistrationListDto> { new() { Id = registrations[0].Id, UserId = currentUserId } };
        _currentUserService.UserId.Returns(currentUserId);
        _eventRegistrationRepository.GetRegistrationsByUserWithDetailsPaged(
                currentUserId,
                2,
                5,
                Arg.Any<CancellationToken>())
            .Returns((registrations, 1));
        _mapper.Map<List<EventRegistrationListDto>>(registrations).Returns(dtos);
        var handler = new GetEventRegistrationListRequestHandler(
            _eventRegistrationRepository,
            _mapper,
            _currentUserService);

        var result = await handler.Handle(
            new GetEventRegistrationListRequest { PageNumber = 2, PageSize = 5 },
            CancellationToken.None);

        await Assert.That(result.Items).IsEquivalentTo(dtos);
        await Assert.That(result.TotalCount).IsEqualTo(1);
        await _eventRegistrationRepository.Received(1).GetRegistrationsByUserWithDetailsPaged(
            currentUserId,
            2,
            5,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ByUserHandler_WhenRequestedUserDiffersFromCurrentUser_ThrowsAuthorizationException()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        var handler = new GetRegistrationsByUserRequestHandler(
            _eventRegistrationRepository,
            _mapper,
            _currentUserService);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            handler.Handle(new GetRegistrationsByUserRequest { UserId = Guid.NewGuid() }, CancellationToken.None));

        await _eventRegistrationRepository.DidNotReceiveWithAnyArgs().GetRegistrationsByUser(default);
    }

    [Test]
    public async Task BySessionHandler_UsesCurrentUserAndSessionForRepositoryRead()
    {
        var currentUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var registration = CreateRegistration(currentUserId, sessionId);
        var dto = new EventRegistrationListDto
        {
            Id = registration.Id,
            UserId = currentUserId,
            EventSessionId = sessionId
        };
        _currentUserService.UserId.Returns(currentUserId);
        _eventRegistrationRepository.GetRegistrationByUserAndSession(
                currentUserId,
                sessionId,
                Arg.Any<CancellationToken>())
            .Returns(registration);
        _mapper.Map<EventRegistrationListDto>(registration).Returns(dto);
        var handler = new GetRegistrationsBySessionRequestHandler(
            _eventRegistrationRepository,
            _mapper,
            _currentUserService);

        var result = await handler.Handle(
            new GetRegistrationsBySessionRequest { EventSessionId = sessionId },
            CancellationToken.None);

        await Assert.That(result).IsEquivalentTo(new List<EventRegistrationListDto> { dto });
        await _eventRegistrationRepository.Received(1).GetRegistrationByUserAndSession(
            currentUserId,
            sessionId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DetailHandler_WhenRegistrationBelongsToAnotherUser_ThrowsAuthorizationException()
    {
        var currentUserId = Guid.NewGuid();
        var registration = CreateRegistration(Guid.NewGuid());
        _currentUserService.UserId.Returns(currentUserId);
        _eventRegistrationRepository.GetByIdWithDetails(registration.Id, Arg.Any<CancellationToken>())
            .Returns(registration);
        var handler = new GetEventRegistrationDetailsRequestHandler(
            _eventRegistrationRepository,
            _mapper,
            _currentUserService);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            handler.Handle(new GetEventRegistrationDetailsRequest { Id = registration.Id }, CancellationToken.None));
    }

    private static EventRegistration CreateRegistration(Guid userId, Guid? sessionId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventId = Guid.NewGuid(),
            EventSessionId = sessionId ?? Guid.NewGuid(),
            Event = null!,
            User = null!,
            EventSession = null!,
            Tenant = null!
        };
}
