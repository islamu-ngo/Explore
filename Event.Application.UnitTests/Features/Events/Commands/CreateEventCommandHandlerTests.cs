using System.Diagnostics.Metrics;
using AutoMapper;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Constants;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Commands;

public class CreateEventCommandHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly CreateEventCommandHandler _handler;

    public CreateEventCommandHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _eventSessionRepository = Substitute.For<IEventSessionRepository>();
        _actorRepository = Substitute.For<IActorRepository>();
        _organizationRepository = Substitute.For<IOrganizationRepository>();
        _organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        _audienceAgeRepository = Substitute.For<IAudienceAgeRepository>();
        _audienceGenderRepository = Substitute.For<IAudienceGenderRepository>();
        _eventTypeRepository = Substitute.For<IEventTypeRepository>();
        _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
        _userContext = Substitute.For<IUserContext>();
        _tenantContext = Substitute.For<ITenantContext>();
        _mapper = Substitute.For<IMapper>();
        _cache = Substitute.For<HybridCache>();

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter("test"));

        _handler = new CreateEventCommandHandler(
            _eventRepository,
            _eventSessionRepository,
            _actorRepository,
            _organizationRepository,
            _organizationMemberRepository,
            _audienceAgeRepository,
            _audienceGenderRepository,
            _eventTypeRepository,
            _storageObjectRepository,
            _userContext,
            _tenantContext,
            _mapper,
            _cache,
            new BusinessMetrics(meterFactory)
        );
    }

    [Test]
    public async Task Handle_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Test Event",
                Subtitle = "Test Subtitle",
                Description = "Description",
                FirstSessionDate = DateTimeOffset.UtcNow.AddDays(1),
                LastSessionDate = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
                EventTypeId = 1, // Set valid IDs
                AudienceGenderId = 1,
                AudienceAgeId = 1
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);

        // Mock Actor Resolution
        var actor = new Actor { Id = actorId, UserId = userId, DisplayName = "Test Actor", ActorType = null!, Tenant = null! };
        _actorRepository.GetActorByUserId(userId).Returns(actor);

        // Mock Validation Dependencies
        _audienceAgeRepository.Exists(Arg.Any<int>()).Returns(true);
        _audienceGenderRepository.Exists(Arg.Any<int>()).Returns(true);
        _eventTypeRepository.Exists(Arg.Any<int>()).Returns(true);

        // Mock Mapping and Creation
        var eventEntity = new Explore.Domain.Event
        {
            Id = eventId,
            Title = "Test Event",
            Actor = null!,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!
        };
        _mapper.Map<Explore.Domain.Event>(command.EventDto).Returns(eventEntity);
        _eventRepository.Create(Arg.Any<Explore.Domain.Event>()).Returns(eventEntity);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(eventId);
        await _eventRepository.Received(1).Create(Arg.Any<Explore.Domain.Event>());
        await _eventSessionRepository.Received(1).Create(Arg.Any<EventSession>());
    }

    [Test]
    public async Task Handle_WithOptionalFieldsNull_ReturnsSuccessResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Generic Event",
                Subtitle = "Test Subtitle",
                Description = "Description",
                FirstSessionDate = DateTimeOffset.UtcNow.AddDays(1),
                LastSessionDate = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
                EventTypeId = null, // Generic import
                AudienceGenderId = null,
                AudienceAgeId = null
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);

        // Mock Actor Resolution
        var actor = new Actor { Id = actorId, UserId = userId, DisplayName = "Test Actor", ActorType = null!, Tenant = null! };
        _actorRepository.GetActorByUserId(userId).Returns(actor);

        // Mock Validation Dependencies (Ensure Exists is NOT called for nulls, or if called, we don't care because validator skips check)
        // Note: The validator code only calls Exists if id.HasValue.

        // Mock Mapping and Creation
        var eventEntity = new Explore.Domain.Event
        {
            Id = eventId,
            Title = "Test Event",
            Actor = null!,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!
        };
        _mapper.Map<Explore.Domain.Event>(command.EventDto).Returns(eventEntity);
        _eventRepository.Create(Arg.Any<Explore.Domain.Event>()).Returns(eventEntity);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(eventId);
        await _eventRepository.Received(1).Create(Arg.Any<Explore.Domain.Event>());
    }

    [Test]
    public async Task Handle_WhenOrganizationAdminCheckFails_ReturnsFailedResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                OrganizationId = organizationId,
                Title = "Test Event",
                EventTypeId = 1, // Set valid IDs to pass validation
                AudienceGenderId = 1,
                AudienceAgeId = 1
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);

        // Mock Admin Check Failure
        _organizationMemberRepository.HasPermissionInOrganization(organizationId, userId, PermissionCodes.EventCreate).Returns(false);

        // Mock Validation Dependencies
        _organizationRepository.Exists(organizationId).Returns(true);
        _audienceAgeRepository.Exists(Arg.Any<int>()).Returns(true);
        _audienceGenderRepository.Exists(Arg.Any<int>()).Returns(true);
        _eventTypeRepository.Exists(Arg.Any<int>()).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("permission");
        await _eventRepository.DidNotReceive().Create(Arg.Any<Explore.Domain.Event>());
    }
}
