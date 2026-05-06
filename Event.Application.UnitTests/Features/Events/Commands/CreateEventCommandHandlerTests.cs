using System.Diagnostics.Metrics;
using Explore.Application.Caching;
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
    private readonly IEventSessionIslamicAspectRepository _eventSessionIslamicAspectRepository;
    private readonly IEventSessionLanguageRepository _eventSessionLanguageRepository;
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
    private readonly IEventSessionTemplateRepository _eventSessionTemplateRepository;
    private readonly IEventSessionCustomPropertyRepository _eventSessionCustomPropertyRepository;
    private readonly IEventSessionCustomPropertyProjectionUpdater _eventSessionCustomPropertyProjectionUpdater;
    private readonly IEventSessionTemplateInstantiationService _eventSessionTemplateInstantiationService;
    private readonly ILocationRepository _locationRepository;
    private readonly IRegistrationModeRepository _registrationModeRepository;
    private readonly ILanguageRepository _languageRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IScheduleItemKindRepository _scheduleItemKindRepository;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;
    private readonly IEventCategoriesRepository _eventCategoriesRepository;
    private readonly IEventTagsRepository _eventTagsRepository;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateEventCommandHandler _handler;

    public CreateEventCommandHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _eventSessionRepository = Substitute.For<IEventSessionRepository>();
        _eventSessionIslamicAspectRepository = Substitute.For<IEventSessionIslamicAspectRepository>();
        _eventSessionLanguageRepository = Substitute.For<IEventSessionLanguageRepository>();
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
        _eventSessionTemplateRepository = Substitute.For<IEventSessionTemplateRepository>();
        _eventSessionCustomPropertyRepository = Substitute.For<IEventSessionCustomPropertyRepository>();
        _eventSessionCustomPropertyProjectionUpdater = Substitute.For<IEventSessionCustomPropertyProjectionUpdater>();
        _eventSessionTemplateInstantiationService = Substitute.For<IEventSessionTemplateInstantiationService>();
        _locationRepository = Substitute.For<ILocationRepository>();
        _registrationModeRepository = Substitute.For<IRegistrationModeRepository>();
        _languageRepository = Substitute.For<ILanguageRepository>();
        _categoryRepository = Substitute.For<ICategoryRepository>();
        _tagRepository = Substitute.For<ITagRepository>();
        _scheduleItemKindRepository = Substitute.For<IScheduleItemKindRepository>();
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _locationRoomRepository = Substitute.For<ILocationRoomRepository>();
        _eventAgendaItemRepository = Substitute.For<IEventAgendaItemRepository>();
        _eventCategoriesRepository = Substitute.For<IEventCategoriesRepository>();
        _eventTagsRepository = Substitute.For<IEventTagsRepository>();
        _scheduleProjectionCalculator = new EventScheduleProjectionCalculator();
        _userContext = Substitute.For<IUserContext>();
        _tenantContext = Substitute.For<ITenantContext>();
        _cache = Substitute.For<HybridCache>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _eventRepository.Create(Arg.Any<Explore.Domain.Event>()).Returns(callInfo =>
        {
            var entity = callInfo.Arg<Explore.Domain.Event>();
            entity.Id = Guid.NewGuid();
            return entity;
        });
        _eventSessionRepository.Create(Arg.Any<EventSession>())
            .Returns(callInfo => callInfo.Arg<EventSession>());

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
            _eventSessionIslamicAspectRepository,
            _eventSessionLanguageRepository,
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
            _eventSessionTemplateRepository,
            _eventSessionCustomPropertyRepository,
            _eventSessionCustomPropertyProjectionUpdater,
            _eventSessionTemplateInstantiationService,
            _organizationRepository,
            _groupRepository,
            _locationRepository,
            _registrationModeRepository,
            _languageRepository,
            _categoryRepository,
            _tagRepository,
            _scheduleItemKindRepository,
            _eventDayRepository,
            _locationRoomRepository,
            _eventAgendaItemRepository,
            _eventCategoriesRepository,
            _eventTagsRepository,
            _scheduleProjectionCalculator,
            _userContext,
            _tenantContext,
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
        var tenantId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            Request = new CreateEventRequest
            {
                Title = "Test Event",
                Subtitle = "Test Subtitle",
                Description = "Description",
                EventTypeId = 1,
                AudienceGenderId = 1,
                AudienceAgeId = 1,
                Sessions = [CreateSessionRequest()]
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isUserReported: true));

        _audienceAgeRepository.Exists(Arg.Any<int>()).Returns(true);
        _audienceGenderRepository.Exists(Arg.Any<int>()).Returns(true);
        _eventTypeRepository.Exists(Arg.Any<int>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await _eventRepository.Received(1).Create(Arg.Any<Explore.Domain.Event>());
        await _eventSessionRepository.Received(1).Create(Arg.Any<EventSession>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithOptionalFieldsNull_ReturnsSuccessResponse()
    {
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            Request = new CreateEventRequest
            {
                Title = "Generic Event",
                Subtitle = "Test Subtitle",
                Description = "Description",
                EventTypeId = null,
                AudienceGenderId = null,
                AudienceAgeId = null,
                Sessions = [CreateSessionRequest()]
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isUserReported: true));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await _eventRepository.Received(1).Create(Arg.Any<Explore.Domain.Event>());
    }

    [Test]
    public async Task Handle_WithMinimalImportShapedRequest_ReturnsSuccessWithoutOrganizationCentricFields()
    {
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            Request = new CreateEventRequest
            {
                Title = "Imported program",
                Sessions = [CreateSessionRequest()]
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isUserReported: true));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await _actorResolver.Received(1).ResolveAsync(userId, null, null, Arg.Any<CancellationToken>());
        await _eventRepository.Received(1).Create(Arg.Is<Explore.Domain.Event>(entity =>
            entity.ActorId == actorId
            && entity.EventTypeId == null
            && entity.AudienceGenderId == null
            && entity.AudienceAgeId == null));
    }

    [Test]
    public async Task Handle_WithDraftWithoutSessions_ReturnsSuccessWithoutCreatingSessions()
    {
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            Request = new CreateEventRequest
            {
                Title = "Draft without program items",
                Sessions = []
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isUserReported: true));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await _eventRepository.Received(1).Create(Arg.Is<Explore.Domain.Event>(entity =>
            entity.SessionCount == 0
            && entity.FirstSessionDate == null
            && entity.LastSessionDate == null
            && entity.FirstSessionStartUtc == null
            && entity.LastSessionStartUtc == null));
        await _eventSessionRepository.DidNotReceive().Create(Arg.Any<EventSession>());
    }

    [Test]
    public async Task Handle_WhenOrganizationAdminCheckFails_ReturnsFailedResponse()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            Request = new CreateEventRequest
            {
                OrganizationId = organizationId,
                Title = "Test Event",
                EventTypeId = 1,
                AudienceGenderId = 1,
                AudienceAgeId = 1,
                Sessions = [CreateSessionRequest()]
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

    private static CreateEventSessionRequest CreateSessionRequest() => new()
    {
        Title = "Opening Session",
        StartTime = DateTimeOffset.UtcNow.AddDays(1),
        EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2)
    };
}
