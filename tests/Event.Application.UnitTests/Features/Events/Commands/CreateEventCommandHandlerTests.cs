// ABOUTME: Unit tests for CreateEventCommandHandler aggregate creation workflows.
// ABOUTME: Verifies event graph creation, validation failures, cache invalidation, and schedule invariants.

using System.Diagnostics.Metrics;
using Event.Application.UnitTests.Common;
using Explore.Application.Caching;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSession.Validators;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Services.Lifecycle;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
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
    private readonly IEventSessionSpeakerRepository _eventSessionSpeakerRepository;
    private readonly IEventIslamicAspectRepository _eventIslamicAspectRepository;
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
    private readonly IMadhabRepository _madhabRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IScheduleItemKindRepository _scheduleItemKindRepository;
    private readonly IEventSessionKindRepository _eventSessionKindRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;
    private readonly IEventCategoriesRepository _eventCategoriesRepository;
    private readonly IEventTagsRepository _eventTagsRepository;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly IEventRoleAssignmentRepository _eventRoleAssignmentRepository;
    private readonly IUserContext _userContext;
    private readonly IEventLifecyclePolicyProvider _lifecyclePolicyProvider;
    private readonly IEventLifecycleReadinessEvaluator _lifecycleReadinessEvaluator;
    private readonly ITenantContext _tenantContext;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly CreateEventCommandHandler _handler;

    public CreateEventCommandHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _eventSessionRepository = Substitute.For<IEventSessionRepository>();
        _eventSessionSpeakerRepository = Substitute.For<IEventSessionSpeakerRepository>();
        _eventIslamicAspectRepository = Substitute.For<IEventIslamicAspectRepository>();
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
        _madhabRepository = Substitute.For<IMadhabRepository>();
        _categoryRepository = Substitute.For<ICategoryRepository>();
        _tagRepository = Substitute.For<ITagRepository>();
        _scheduleItemKindRepository = Substitute.For<IScheduleItemKindRepository>();
        _eventSessionKindRepository = Substitute.For<IEventSessionKindRepository>();
        _actorRepository = Substitute.For<IActorRepository>();
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _locationRoomRepository = Substitute.For<ILocationRoomRepository>();
        _eventAgendaItemRepository = Substitute.For<IEventAgendaItemRepository>();
        _eventCategoriesRepository = Substitute.For<IEventCategoriesRepository>();
        _eventTagsRepository = Substitute.For<IEventTagsRepository>();
        _scheduleProjectionCalculator = new EventScheduleProjectionCalculator();
        _eventRoleAssignmentRepository = Substitute.For<IEventRoleAssignmentRepository>();
        _userContext = Substitute.For<IUserContext>();
        _lifecyclePolicyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        _lifecycleReadinessEvaluator = Substitute.For<IEventLifecycleReadinessEvaluator>();
        _tenantContext = Substitute.For<ITenantContext>();
        _cache = Substitute.For<HybridCache>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _outboxRepository = Substitute.For<IOutboxRepository>();

        _eventRepository.Create(Arg.Any<Explore.Domain.Event>()).Returns(callInfo =>
        {
            var entity = callInfo.Arg<Explore.Domain.Event>();
            entity.Id = Guid.CreateVersion7();
            entity.ConcurrencyStamp = Guid.CreateVersion7();
            return entity;
        });
        _eventSessionRepository.Create(Arg.Any<EventSession>())
            .Returns(callInfo => callInfo.Arg<EventSession>());
        _eventIslamicAspectRepository.Upsert(Arg.Any<EventIslamicAspect>())
            .Returns(callInfo => callInfo.Arg<EventIslamicAspect>());
        _locationRepository.Create(Arg.Any<Location>()).Returns(callInfo =>
        {
            var entity = callInfo.Arg<Location>();
            entity.Id = Guid.NewGuid();
            entity.Pii.LocationId = entity.Id;
            return entity;
        });
        _locationRoomRepository.Create(Arg.Any<LocationRoom>()).Returns(callInfo =>
        {
            var entity = callInfo.Arg<LocationRoom>();
            entity.Id = Guid.NewGuid();
            return entity;
        });
        _eventAgendaItemRepository.Create(Arg.Any<EventAgendaItem>())
            .Returns(callInfo => callInfo.Arg<EventAgendaItem>());
        _eventSessionSpeakerRepository.Create(Arg.Any<EventSessionSpeaker>())
            .Returns(callInfo => callInfo.Arg<EventSessionSpeaker>());

        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Guid>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var op = callInfo.Arg<Func<CancellationToken, Task<Guid>>>();
                return op(callInfo.Arg<CancellationToken>());
            });

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter("test"));
        var eventLocationRepository = Substitute.For<IEventLocationRepository>();
        eventLocationRepository.AddAsync(Arg.Any<EventLocation>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<EventLocation>(0));
        var eventLocationAttachmentService = new EventLocationAttachmentService(
            eventLocationRepository,
            _userContext,
            _tenantContext,
            TimeProvider.System);

        _handler = new CreateEventCommandHandler(
            _eventRepository,
            _eventSessionRepository,
            _eventSessionSpeakerRepository,
            _eventIslamicAspectRepository,
            _eventSessionIslamicAspectRepository,
            _eventSessionLanguageRepository,
            _eventRoleAssignmentRepository,
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
            _madhabRepository,
            _categoryRepository,
            _tagRepository,
            _scheduleItemKindRepository,
            _eventSessionKindRepository,
            _actorRepository,
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
            _unitOfWork,
            _outboxRepository,
            _lifecyclePolicyProvider,
            _lifecycleReadinessEvaluator,
            eventLocationAttachmentService,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            TimeProvider.System
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
            EventDto = new CreateEventDto
            {
                Title = "Test Event",
                ParticipationConfiguration = CreateParticipationConfiguration(),
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
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: true));

        _audienceAgeRepository.Exists(Arg.Any<int>()).Returns(true);
        _audienceGenderRepository.Exists(Arg.Any<int>()).Returns(true);
        _eventTypeRepository.Exists(Arg.Any<int>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await _eventRepository.Received(1).Create(Arg.Is<Explore.Domain.Event>(entity =>
            entity.ParticipationConfiguration != null
            && entity.ParticipationConfiguration.ParticipationHandlingModeId == (int)ParticipationHandlingModeEnum.InformationOnly
            && entity.ParticipationConfiguration.AdvanceRegistrationObligationId == (int)AdvanceRegistrationObligationEnum.NotApplicable));
        await _eventSessionRepository.Received(1).Create(Arg.Any<EventSession>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Handle_WhenNestedImageReferenceIsCrossTenant_RejectsBeforeAggregateMutation(
        bool useSessionImage)
    {
        var imageId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        CreateEventGraphSessionDto session = CreateSessionRequest();
        session.FeaturedImageId = useSessionImage ? imageId : null;
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Unsafe nested image",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Sessions = [session],
                Days = useSessionImage
                    ? []
                    :
                    [
                        new CreateEventGraphDayDto
                        {
                            LocalDate = DateOnly.FromDateTime(session.StartTime.UtcDateTime),
                            BannerImageId = imageId
                        }
                    ]
            }
        };
        _tenantContext.TenantId.Returns(tenantId);
        _storageObjectRepository.Exists(imageId).Returns(true);
        _storageObjectRepository.GetById(imageId).Returns(new StorageObject
        {
            Id = imageId,
            TenantId = Guid.NewGuid(),
            Tenant = null!,
            FileType = null!,
            Uri = "storage://unsafe.png",
            Provider = "local",
            FullName = "unsafe.png",
            SafeDisplayName = "unsafe.png",
            Extension = "png",
            ContentType = "image/png",
            Purpose = StorageObjectPurposes.EventImage,
            Visibility = StorageObjectVisibilities.PublicImage,
            LifecycleState = StorageObjectLifecycleStates.Active
        });

        BaseCommandResponse<Guid> result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _actorResolver.DidNotReceiveWithAnyArgs().ResolveAsync(
            default,
            default,
            default,
            default);
        await _eventRepository.DidNotReceive().Create(Arg.Any<Explore.Domain.Event>());
        await _eventSessionRepository.DidNotReceive().Create(Arg.Any<EventSession>());
        await _eventDayRepository.DidNotReceive().Create(Arg.Any<EventDay>());
    }

    [Test]
    public async Task Handle_WithMalformedInput_ReturnsValidationFailureBeforeUserContext()
    {
        _userContext.GetRequiredUserId().Returns(_ => throw new InvalidOperationException("user context should not be read"));

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "",
                ParticipationConfiguration = CreateParticipationConfiguration()
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Event creation failed due to validation errors.");
        _userContext.DidNotReceive().GetRequiredUserId();
        await _actorResolver.DidNotReceiveWithAnyArgs().ResolveAsync(default, default, default, default);
        await _eventRepository.DidNotReceive().Create(Arg.Any<Explore.Domain.Event>());
    }

    [Test]
    public async Task Handle_WithOptionalFieldsNull_ReturnsSuccessResponse()
    {
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Generic Event",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Subtitle = "Test Subtitle",
                Description = "Description",
                EventTypeId = null,
                AudienceGenderId = null,
                AudienceAgeId = null,
                Sessions = [CreateSessionRequest()]
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: true));

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
            EventDto = new CreateEventDto
            {
                Title = "Imported program",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Sessions = [CreateSessionRequest()]
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: true));

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
    public async Task Handle_WithPublishedWithoutSessions_CreatesDefaultSession()
    {
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Published without program items",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                EventStatusId = (int)EventStatusEnum.Published,
                Sessions = []
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: true));

        _eventRepository.Create(Arg.Any<Explore.Domain.Event>())
            .Returns(callInfo =>
            {
                var evt = callInfo.Arg<Explore.Domain.Event>();
                evt.Id = Guid.CreateVersion7();
                evt.ConcurrencyStamp = Guid.CreateVersion7();
                evt.FirstSessionStartUtc = DateTime.UtcNow;
                return evt;
            });

        _lifecyclePolicyProvider.GetEffectivePolicyAsync(Arg.Any<Guid?>(), ValidationProfile.EventPublish, Arg.Any<CancellationToken>())
            .Returns(new EventLifecyclePolicy
            {
                Profile = ValidationProfile.EventPublish,
                RequiredEventFields = new HashSet<Enum>(),
                RequiredSessionFields = new HashSet<Enum>()
            });
        _lifecycleReadinessEvaluator.Evaluate(
                Arg.Is<Explore.Domain.Event>(entity => entity.EventStatusId == (int)EventStatusEnum.Published),
                ValidationProfile.EventPublish,
                Arg.Any<EventLifecyclePolicy>())
            .Returns(LifecycleReadinessResult.Success(ValidationProfile.EventPublish));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await _eventRepository.Received(1).Create(Arg.Is<Explore.Domain.Event>(entity =>
            entity.SessionCount == 0
            && entity.FirstSessionDate == null
            && entity.LastSessionDate == null
            && entity.LastSessionStartUtc == null));
        await _eventSessionRepository.Received(1).Create(Arg.Is<EventSession>(session =>
            session.Title == "Published without program items"
            && session.StartTime == null
            && session.EndTime == null
            && session.EventSessionStatusId == (int)EventSessionStatusEnum.Published));
    }

    [Test]
    public async Task Handle_WithCommunityProfile_PersistsMinimumPublishedEventWithServerOwnedFields()
    {
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Community event",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                EventStatusId = (int)EventStatusEnum.Published,
                Sessions = []
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: true));
        _eventRepository.Create(Arg.Any<Explore.Domain.Event>())
            .Returns(callInfo =>
            {
                var entity = callInfo.Arg<Explore.Domain.Event>();
                entity.Id = Guid.CreateVersion7();
                entity.ConcurrencyStamp = Guid.CreateVersion7();
                return entity;
            });
        _lifecyclePolicyProvider.GetEffectivePolicyAsync(
                tenantId,
                ValidationProfile.EventPublish,
                Arg.Any<CancellationToken>())
            .Returns(new EventLifecyclePolicy
            {
                Profile = ValidationProfile.EventPublishCommunityLexicon,
                RequiredEventFields = new HashSet<Enum>
                {
                    EventFieldKey.Title,
                    EventFieldKey.Tenant,
                    EventFieldKey.Owner,
                    EventFieldKey.Status
                },
                RequiredSessionFields = new HashSet<Enum>()
            });
        _lifecycleReadinessEvaluator.Evaluate(
                Arg.Any<Explore.Domain.Event>(),
                ValidationProfile.EventPublishCommunityLexicon,
                Arg.Any<EventLifecyclePolicy>())
            .Returns(LifecycleReadinessResult.Success(ValidationProfile.EventPublishCommunityLexicon));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _eventRepository.Received(1).Create(Arg.Is<Explore.Domain.Event>(entity =>
            entity.Title == "Community event"
            && entity.TenantId == tenantId
            && entity.ActorId == actorId
            && entity.EventStatusId == (int)EventStatusEnum.Published
            && entity.CreatedBy == userId
            && entity.CreatedAt != default
            && !entity.IsDeleted));
        _lifecycleReadinessEvaluator.Received(1).Evaluate(
            Arg.Any<Explore.Domain.Event>(),
            ValidationProfile.EventPublishCommunityLexicon,
            Arg.Any<EventLifecyclePolicy>());
    }

    [Test]
    public async Task Handle_WithPublishedLocationAndNoExplicitSessions_LinksDefaultSessionToPrimaryRoom()
    {
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Poster venue event",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                EventStatusId = (int)EventStatusEnum.Published,
                Locations =
                [
                    new CreateEventLocationDto
                    {
                        TempKey = "primary-location",
                        FullName = "Islamic Centre Brussels",
                        Address = "Rue Example 10",
                        Postcode = "1000",
                        Country = "Belgium",
                        City = "Brussels"
                    }
                ],
                Rooms =
                [
                    new CreateEventRoomDto
                    {
                        TempKey = "primary-room",
                        LocationTempKey = "primary-location",
                        Name = "Main Hall"
                    }
                ],
                Sessions = []
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: true));

        _eventRepository.Create(Arg.Any<Explore.Domain.Event>())
            .Returns(callInfo =>
            {
                var evt = callInfo.Arg<Explore.Domain.Event>();
                evt.Id = Guid.CreateVersion7();
                evt.ConcurrencyStamp = Guid.CreateVersion7();
                evt.FirstSessionStartUtc = DateTime.UtcNow;
                return evt;
            });

        _lifecyclePolicyProvider.GetEffectivePolicyAsync(Arg.Any<Guid?>(), ValidationProfile.EventPublish, Arg.Any<CancellationToken>())
            .Returns(new EventLifecyclePolicy
            {
                Profile = ValidationProfile.EventPublish,
                RequiredEventFields = new HashSet<Enum>(),
                RequiredSessionFields = new HashSet<Enum>()
            });
        _lifecycleReadinessEvaluator.Evaluate(
                Arg.Is<Explore.Domain.Event>(entity => entity.EventStatusId == (int)EventStatusEnum.Published),
                ValidationProfile.EventPublish,
                Arg.Any<EventLifecyclePolicy>())
            .Returns(LifecycleReadinessResult.Success(ValidationProfile.EventPublish));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _eventSessionRepository.Received(1).Create(Arg.Is<EventSession>(session =>
            session.LocationId.HasValue
            && session.RoomId.HasValue
            && session.StartTime == null
            && session.EndTime == null));
    }



    [Test]
    public async Task Handle_WithDraftSessions_CreatesDraftSessionsWithoutPublicScheduleRollup()
    {
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Draft with internal sessions",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Sessions = [CreateSessionRequest()]
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: true));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _eventRepository.Received(1).Create(Arg.Is<Explore.Domain.Event>(entity =>
            entity.SessionCount == 0
            && entity.FirstSessionDate == null
            && entity.LastSessionDate == null
            && entity.FirstSessionStartUtc == null
            && entity.LastSessionStartUtc == null));
        await _eventSessionRepository.Received(1).Create(Arg.Is<EventSession>(session =>
            session.EventSessionStatusId == (int)EventSessionStatusEnum.Draft));
    }

    [Test]
    public async Task Handle_WithStructuredDraftGraph_CreatesRelatedEventRows()
    {
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var speakerActorId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Structured Poster Event",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                IslamicAspect = new()
                {
                    GenderMode = GenderSegregationMode.Segregated
                },
                Locations =
                [
                    new CreateEventLocationDto
                    {
                        TempKey = "main-location",
                        FullName = "Islamic Centre Brussels",
                        Address = "Rue Example 10",
                        Postcode = "1000",
                        Country = "Belgium",
                        City = "Brussels",
                        Timezone = "Europe/Brussels"
                    }
                ],
                Rooms =
                [
                    new CreateEventRoomDto
                    {
                        TempKey = "main-hall",
                        LocationTempKey = "main-location",
                        Name = "Main Hall"
                    }
                ],
                Sessions =
                [
                    new CreateEventGraphSessionDto
                    {
                        Title = "Keynote",
                        RoomTempKey = "main-hall",
                        StartTime = DateTimeOffset.UtcNow.AddDays(1),
                        EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
                        EventSessionKindId = 2,
                        SpeakerActorIds = [speakerActorId, speakerActorId]
                    }
                ],
                AgendaItems =
                [
                    new CreateEventGraphAgendaItemDto
                    {
                        Title = "Doors open",
                        RoomTempKey = "main-hall",
                        StartTime = DateTimeOffset.UtcNow.AddDays(1).AddMinutes(-30),
                        EndTime = DateTimeOffset.UtcNow.AddDays(1).AddMinutes(-15)
                    }
                ]
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: true));
        _eventSessionKindRepository.Exists(2).Returns(true);
        _actorRepository.Exists(speakerActorId).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _eventIslamicAspectRepository.Received(1).Upsert(Arg.Is<EventIslamicAspect>(aspect =>
            aspect.Id == result.Id
            && aspect.GenderMode == GenderSegregationMode.Segregated));
        await _locationRepository.Received(1).Create(Arg.Is<Location>(location =>
            location.TenantId == tenantId
            && location.FullName == "Islamic Centre Brussels"
            && location.Pii.Address == "Rue Example 10"));
        await _locationRoomRepository.Received(1).Create(Arg.Is<LocationRoom>(room =>
            room.TenantId == tenantId
            && room.Name == "Main Hall"
            && room.LocationId != Guid.Empty));
        await _eventSessionRepository.Received(1).Create(Arg.Is<EventSession>(session =>
            session.Title == "Keynote"
            && session.TenantId == tenantId
            && session.EventSessionKindId == 2
            && session.LocationId != null
            && session.RoomId != null));
        await _eventSessionSpeakerRepository.Received(1).Create(Arg.Is<EventSessionSpeaker>(speaker =>
            speaker.ActorId == speakerActorId
            && speaker.TenantId == tenantId));
        await _eventAgendaItemRepository.Received(1).Create(Arg.Is<EventAgendaItem>(item =>
            item.Title == "Doors open"
            && item.LocationId != null
            && item.RoomId != null));
    }

    [Test]
    public async Task Handle_WithFixedIslamicSessionPrayerFields_ReturnsValidationError()
    {
        var userId = Guid.NewGuid();
        var session = CreateSessionRequest();
        session.IslamicAspect = new EventSessionIslamicAspectDto
        {
            StartTimeType = SessionStartTimeType.Fixed,
            ReferencePrayer = PrayerTime.Dhuhr,
            OffsetMinutes = 0
        };

        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Invalid Islamic Event",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Sessions = [session]
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains(EventSessionIslamicAspectValidationRules.SchedulingStateMessage);
        await _eventRepository.DidNotReceive().Create(Arg.Any<Explore.Domain.Event>());
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
                ParticipationConfiguration = CreateParticipationConfiguration(),
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

    [Test]
    public async Task Handle_OrganizerCreatedEvent_AssignsEventOwnerToCreator()
    {
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Owner Test Event",
                ParticipationConfiguration = CreateParticipationConfiguration(),
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
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: false));

        _audienceAgeRepository.Exists(Arg.Any<int>()).Returns(true);
        _audienceGenderRepository.Exists(Arg.Any<int>()).Returns(true);
        _eventTypeRepository.Exists(Arg.Any<int>()).Returns(true);

        _eventRoleAssignmentRepository.Create(Arg.Any<EventRoleAssignment>())
            .Returns(callInfo => callInfo.Arg<EventRoleAssignment>());

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _eventRoleAssignmentRepository.Received(1).Create(Arg.Is<EventRoleAssignment>(a =>
            a.UserId == userId
            && a.RoleId == (int)RoleEnum.EventOwner
            && a.Status == EventRoleAssignmentStatus.Active
            && a.EventId == result.Id
            && a.TenantId == tenantId
            && a.ExpiresAtUtc == null));
    }

    [Test]
    public async Task Handle_CommunityReportedEvent_DoesNotAssignEventOwnerToCreator()
    {
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Community report",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Sessions = [CreateSessionRequest()]
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: true));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _eventRoleAssignmentRepository.DidNotReceive().Create(Arg.Any<EventRoleAssignment>());
    }

    [Test]
    public async Task Handle_WithPublishedStatus_ValidatesReadinessAndEmitsNotificationFanoutOutboxMessage()
    {
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var createdMessages = new List<OutboxMessage>();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Published Event",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Subtitle = "Test Subtitle",
                Description = "Description",
                EventTypeId = 1,
                AudienceGenderId = 1,
                AudienceAgeId = 1,
                EventStatusId = (int)EventStatusEnum.Published,
                Sessions = [CreateSessionRequest()]
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: true));

        _audienceAgeRepository.Exists(Arg.Any<int>()).Returns(true);
        _audienceGenderRepository.Exists(Arg.Any<int>()).Returns(true);
        _eventTypeRepository.Exists(Arg.Any<int>()).Returns(true);

        _lifecyclePolicyProvider.GetEffectivePolicyAsync(Arg.Any<Guid?>(), ValidationProfile.EventPublish, Arg.Any<CancellationToken>())
            .Returns(new EventLifecyclePolicy
            {
                Profile = ValidationProfile.EventPublish,
                RequiredEventFields = new HashSet<Enum> { EventFieldKey.Title, EventFieldKey.ScheduleSessions },
                RequiredSessionFields = new HashSet<Enum>()
            });
        _lifecycleReadinessEvaluator.Evaluate(Arg.Any<Explore.Domain.Event>(), ValidationProfile.EventPublish, Arg.Any<EventLifecyclePolicy>())
            .Returns(LifecycleReadinessResult.Success(ValidationProfile.EventPublish));
        _outboxRepository.Create(Arg.Any<OutboxMessage>())
            .Returns(callInfo =>
            {
                var message = callInfo.Arg<OutboxMessage>();
                createdMessages.Add(message);
                return message;
            });

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _eventRepository.Received(1).Create(Arg.Is<Explore.Domain.Event>(e => e.EventStatusId == (int)EventStatusEnum.Published));
        await _eventSessionRepository.Received(1).Create(Arg.Is<EventSession>(session =>
            session.EventSessionStatusId == (int)EventSessionStatusEnum.Published));
        await _outboxRepository.Received(1).Create(Arg.Is<OutboxMessage>(message =>
            message.AggregateType == "Event"
            && message.AggregateId == result.Id
            && message.EventType == PublishEventCommandHandler.EventPublishedNotificationFanoutRequestedEventType
            && message.Status == OutboxMessageStatus.Pending
            && message.Payload != null));
        await Assert.That(createdMessages).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Handle_WithPublishedStatusButNotReady_ReturnsReadinessFailureAndDoesNotCreateEvent()
    {
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Published Event",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Subtitle = "Test Subtitle",
                Description = "Description",
                EventTypeId = 1,
                AudienceGenderId = 1,
                AudienceAgeId = 1,
                EventStatusId = (int)EventStatusEnum.Published,
                Sessions = [] // Empty sessions makes it not ready to publish
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: true));

        _audienceAgeRepository.Exists(Arg.Any<int>()).Returns(true);
        _audienceGenderRepository.Exists(Arg.Any<int>()).Returns(true);
        _eventTypeRepository.Exists(Arg.Any<int>()).Returns(true);

        _lifecyclePolicyProvider.GetEffectivePolicyAsync(Arg.Any<Guid?>(), ValidationProfile.EventPublish, Arg.Any<CancellationToken>())
            .Returns(new EventLifecyclePolicy
            {
                Profile = ValidationProfile.EventPublish,
                RequiredEventFields = new HashSet<Enum> { EventFieldKey.Title, EventFieldKey.ScheduleSessions },
                RequiredSessionFields = new HashSet<Enum>()
            });
        _lifecycleReadinessEvaluator.Evaluate(Arg.Any<Explore.Domain.Event>(), ValidationProfile.EventPublish, Arg.Any<EventLifecyclePolicy>())
            .Returns(LifecycleReadinessResult.Failure(ValidationProfile.EventPublish,
            [
                new LifecycleReadinessError("schedule_session_required", EventFieldKey.ScheduleSessions, "schedule.sessions", "Event requires at least one scheduled session.", ReadinessErrorSeverity.Error, ReadinessErrorSource.CommandProfile, ValidationProfile.EventPublish)
            ]));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_publish_readiness_failed");
        await _eventRepository.DidNotReceive().Create(Arg.Any<Explore.Domain.Event>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    [Arguments((int)EventStatusEnum.Cancelled)]
    [Arguments(999)]
    public async Task Handle_WithUnsupportedCreationStatus_ReturnsValidationFailureBeforeSideEffects(int statusId)
    {
        var userId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Unsupported status",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                EventStatusId = statusId,
                Sessions = [CreateSessionRequest()]
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_create_status_not_supported");
        await _actorResolver.DidNotReceiveWithAnyArgs().ResolveAsync(default, default, default, default);
        await _lifecyclePolicyProvider.DidNotReceive().GetEffectivePolicyAsync(Arg.Any<Guid?>(), Arg.Any<ValidationProfile>(), Arg.Any<CancellationToken>());
        await _eventRepository.DidNotReceive().Create(Arg.Any<Explore.Domain.Event>());
        await _eventSessionRepository.DidNotReceive().Create(Arg.Any<EventSession>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task Handle_WithZeroStatus_CreatesDraftByDefault()
    {
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Default draft status",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                EventStatusId = 0,
                Sessions = [CreateSessionRequest()]
            }
        };

        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: true));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _eventRepository.Received(1).Create(Arg.Is<Explore.Domain.Event>(entity =>
            entity.EventStatusId == (int)EventStatusEnum.Draft));
        await _eventSessionRepository.Received(1).Create(Arg.Is<EventSession>(session =>
            session.EventSessionStatusId == (int)EventSessionStatusEnum.Draft));
        await _lifecyclePolicyProvider.DidNotReceive().GetEffectivePolicyAsync(Arg.Any<Guid?>(), Arg.Any<ValidationProfile>(), Arg.Any<CancellationToken>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    [Category("Phase43Ticketing")]
    public async Task PlatformManagedEventCreation_ShouldNotAutoPersistAnXxxFreeTicketCatalog()
    {
        var userId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: false));

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Platform-managed event",
                ParticipationConfiguration = CreateTicketingParticipationConfiguration(ParticipationHandlingModeEnum.PlatformManaged)
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(typeof(CreateEventCommandHandler)
            .GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Any(parameter => parameter.ParameterType == typeof(IEventTicketCatalogRepository)))
            .IsFalse();
    }

    [Test]
    [Category("Phase43Ticketing")]
    public async Task Handle_WithExternalManagedParticipation_DoesNotCreateTicketCatalog()
    {
        var userId = Guid.CreateVersion7();
        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(Guid.CreateVersion7());
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(Guid.CreateVersion7(), isCommunitySubmission: false));

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "External-managed event",
                ParticipationConfiguration = CreateTicketingParticipationConfiguration(ParticipationHandlingModeEnum.ExternalManaged)
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    [Category("Phase43Ticketing")]
    public async Task Handle_WithListingOnlyParticipation_DoesNotCreateTicketCatalog()
    {
        var userId = Guid.CreateVersion7();
        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(Guid.CreateVersion7());
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(Guid.CreateVersion7(), isCommunitySubmission: true));

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Listing-only event",
                ParticipationConfiguration = CreateTicketingParticipationConfiguration(ParticipationHandlingModeEnum.InformationOnly)
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
    }

    private static ConfigureEventParticipationDto CreateParticipationConfiguration() => new()
    {
        ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.InformationOnly,
        AdvanceRegistrationObligationId = (int)AdvanceRegistrationObligationEnum.NotApplicable
    };

    private static ConfigureEventParticipationDto CreateTicketingParticipationConfiguration(ParticipationHandlingModeEnum mode) => mode switch
    {
        ParticipationHandlingModeEnum.PlatformManaged => new()
        {
            ParticipationHandlingModeId = (int)mode,
            AdvanceRegistrationObligationId = (int)AdvanceRegistrationObligationEnum.Required,
            IdentityAccessModeId = (int)IdentityAccessModeEnum.AccountRequired
        },
        ParticipationHandlingModeEnum.ExternalManaged => new()
        {
            ParticipationHandlingModeId = (int)mode,
            AdvanceRegistrationObligationId = (int)AdvanceRegistrationObligationEnum.Optional
        },
        _ => CreateParticipationConfiguration()
    };

    private static CreateEventGraphSessionDto CreateSessionRequest() => new()
    {
        Title = "Opening Session",
        StartTime = DateTimeOffset.UtcNow.AddDays(1),
        EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2)
    };
}
