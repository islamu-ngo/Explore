using System.Diagnostics.Metrics;
using AutoMapper;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Services.Scheduling;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Commands;

public class CreateEventCommandHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventActorResolver _actorResolver;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IEventTemplateRepository _eventTemplateRepository;
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly IEventRegistrationPolicyRepository _eventRegistrationPolicyRepository;
    private readonly IEventCustomPropertyRepository _eventCustomPropertyRepository;
    private readonly IEventTemplateInstantiationService _instantiationService;
    private readonly IEventCustomPropertyProjectionUpdater _projectionUpdater;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateEventCommandHandler _handler;

    public CreateEventCommandHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _eventSessionRepository = Substitute.For<IEventSessionRepository>();
        _actorResolver = Substitute.For<IEventActorResolver>();
        _organizationRepository = Substitute.For<IOrganizationRepository>();
        _groupRepository = Substitute.For<IGroupRepository>();
        _audienceAgeRepository = Substitute.For<IAudienceAgeRepository>();
        _audienceGenderRepository = Substitute.For<IAudienceGenderRepository>();
        _eventTypeRepository = Substitute.For<IEventTypeRepository>();
        _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
        _eventTemplateRepository = Substitute.For<IEventTemplateRepository>();
        _eventSeriesRepository = Substitute.For<IEventSeriesRepository>();
        _eventRegistrationPolicyRepository = Substitute.For<IEventRegistrationPolicyRepository>();
        _eventCustomPropertyRepository = Substitute.For<IEventCustomPropertyRepository>();
        _instantiationService = Substitute.For<IEventTemplateInstantiationService>();
        _projectionUpdater = Substitute.For<IEventCustomPropertyProjectionUpdater>();
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _locationRoomRepository = Substitute.For<ILocationRoomRepository>();
        _eventAgendaItemRepository = Substitute.For<IEventAgendaItemRepository>();
        _scheduleProjectionCalculator = Substitute.For<IEventScheduleProjectionCalculator>();
        _userContext = Substitute.For<IUserContext>();
        _tenantContext = Substitute.For<ITenantContext>();
        _mapper = Substitute.For<IMapper>();
        _cache = Substitute.For<HybridCache>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Guid>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var op = callInfo.Arg<Func<CancellationToken, Task<Guid>>>();
                return op(CancellationToken.None);
            });

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter("test"));

        _handler = new CreateEventCommandHandler(
            _eventRepository,
            _eventSessionRepository,
            _actorResolver,
            _audienceAgeRepository,
            _audienceGenderRepository,
            _eventTypeRepository,
            _storageObjectRepository,
            _eventTemplateRepository,
            _eventSeriesRepository,
            _eventRegistrationPolicyRepository,
            _eventCustomPropertyRepository,
            _projectionUpdater,
            _instantiationService,
            _organizationRepository,
            _groupRepository,
            _eventDayRepository,
            _locationRoomRepository,
            _eventAgendaItemRepository,
            _scheduleProjectionCalculator,
            _userContext,
            _tenantContext,
            _mapper,
            _cache,
            new BusinessMetrics(meterFactory),
            _unitOfWork
        );
    }

    [Test]
    public async Task Handle_WithValidRequest_ReturnsSuccessResponse()
    {
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
                EventTypeId = 1,
                AudienceGenderId = 1,
                AudienceAgeId = 1
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isUserReported: true));

        _audienceAgeRepository.Exists(Arg.Any<int>()).Returns(true);
        _audienceGenderRepository.Exists(Arg.Any<int>()).Returns(true);
        _eventTypeRepository.Exists(Arg.Any<int>()).Returns(true);

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

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(eventId);
        await _eventRepository.Received(1).Create(Arg.Any<Explore.Domain.Event>());
        await _eventSessionRepository.Received(1).Create(Arg.Any<EventSession>());
    }

    [Test]
    public async Task Handle_WithOptionalFieldsNull_ReturnsSuccessResponse()
    {
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
                EventTypeId = null,
                AudienceGenderId = null,
                AudienceAgeId = null
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isUserReported: true));

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

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(eventId);
        await _eventRepository.Received(1).Create(Arg.Any<Explore.Domain.Event>());
    }

    [Test]
    public async Task Handle_WhenOrganizationAdminCheckFails_ReturnsFailedResponse()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                OrganizationId = organizationId,
                Title = "Test Event",
                EventTypeId = 1,
                AudienceGenderId = 1,
                AudienceAgeId = 1
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _actorResolver.ResolveAsync(userId, organizationId, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Failure(
                "You do not have permission to create events for this organization.",
                "Your role in the organization does not include event creation permission."));

        _organizationRepository.Exists(organizationId).Returns(true);
        _audienceAgeRepository.Exists(Arg.Any<int>()).Returns(true);
        _audienceGenderRepository.Exists(Arg.Any<int>()).Returns(true);
        _eventTypeRepository.Exists(Arg.Any<int>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("permission");
        await _eventRepository.DidNotReceive().Create(Arg.Any<Explore.Domain.Event>());
    }
}
