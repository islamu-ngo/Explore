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
using Explore.Application.Features.Geocoding;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Services.Federation;
using Explore.Application.Services.Lifecycle;
using Explore.Application.Settings;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Commands;

public class CreateEventCommandHandlerTests
{
    private static long s_meterSequence;
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
    private readonly IAddressGovernancePolicyResolver _addressGovernancePolicyResolver;
    private readonly IEventRoleAssignmentRepository _eventRoleAssignmentRepository;
    private readonly IUserContext _userContext;
    private readonly IEventLifecyclePolicyProvider _lifecyclePolicyProvider;
    private readonly IEventLifecycleReadinessEvaluator _lifecycleReadinessEvaluator;
    private readonly ITenantContext _tenantContext;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IHierarchicalSettingsResolver _atprotoSettingsResolver;
    private readonly CreateEventCommandHandler _handler;
    private readonly string _meterName =
        $"create-event-command-handler-tests-{Interlocked.Increment(ref s_meterSequence)}";

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
        _addressGovernancePolicyResolver = Substitute.For<IAddressGovernancePolicyResolver>();
        _addressGovernancePolicyResolver
            .ResolveAsync(Arg.Any<AddressGovernancePolicyRequest>(), Arg.Any<CancellationToken>())
            .Returns(AddressGovernancePolicyDecision.Allowed(
                AddressCreationMode.OpenWithModeration,
                LocationAddressVisibilityEnum.CreatorPrivate));
        _eventRoleAssignmentRepository = Substitute.For<IEventRoleAssignmentRepository>();
        _userContext = Substitute.For<IUserContext>();
        _lifecyclePolicyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        _lifecycleReadinessEvaluator = Substitute.For<IEventLifecycleReadinessEvaluator>();
        _tenantContext = Substitute.For<ITenantContext>();
        _cache = Substitute.For<HybridCache>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _outboxRepository = Substitute.For<IOutboxRepository>();
        _atprotoSettingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _atprotoSettingsResolver.ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IEnumerable<string>>().Select(key => new ResolvedSetting
            {
                Key = key,
                Value = key == GovernanceSettingKeys.Federation.AtprotoEventValidationProfile
                    ? "\"platform\""
                    : "false",
                Source = SettingSource.SystemDefault
            }).ToArray());

        _eventRepository.Create(Arg.Any<Explore.Domain.Event>()).Returns(callInfo =>
        {
            var entity = callInfo.Arg<Explore.Domain.Event>();
            entity.Id = Guid.CreateVersion7();
            entity.ConcurrencyStamp = Guid.CreateVersion7();
            return entity;
        });
        _eventSessionRepository.Create(Arg.Any<EventSession>())
            .Returns(callInfo =>
            {
                var entity = callInfo.Arg<EventSession>();
                if (entity.Id == Guid.Empty)
                {
                    entity.Id = Guid.CreateVersion7();
                }
                return entity;
            });
        _eventIslamicAspectRepository.Upsert(Arg.Any<EventIslamicAspect>())
            .Returns(callInfo => callInfo.Arg<EventIslamicAspect>());
        _locationRepository.Create(Arg.Any<Location>(), Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            var entity = callInfo.Arg<Location>();
            entity.Id = Guid.NewGuid();
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
        _eventDayRepository.Create(Arg.Any<EventDay>())
            .Returns(callInfo =>
            {
                var entity = callInfo.Arg<EventDay>();
                entity.Id = Guid.CreateVersion7();
                return entity;
            });
        _eventSessionIslamicAspectRepository.Create(Arg.Any<EventSessionIslamicAspect>())
            .Returns(callInfo => callInfo.Arg<EventSessionIslamicAspect>());
        _eventSessionLanguageRepository.Create(Arg.Any<EventSessionLanguage>())
            .Returns(callInfo => callInfo.Arg<EventSessionLanguage>());
        _eventCategoriesRepository.Create(Arg.Any<Explore.Domain.EventCategories>())
            .Returns(callInfo => callInfo.Arg<Explore.Domain.EventCategories>());
        _eventTagsRepository.Create(Arg.Any<Explore.Domain.EventTags>())
            .Returns(callInfo => callInfo.Arg<Explore.Domain.EventTags>());

        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Guid>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var op = callInfo.Arg<Func<CancellationToken, Task<Guid>>>();
                return op(callInfo.Arg<CancellationToken>());
            });

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(_meterName));
        var eventLocationRepository = Substitute.For<IEventLocationRepository>();
        eventLocationRepository.AddAsync(Arg.Any<EventLocation>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<EventLocation>(0));
        var eventLocationAttachmentService = new EventLocationAttachmentService(
            eventLocationRepository,
            _userContext,
            _tenantContext,
            TimeProvider.System);

        var atprotoPublicationPlanner = new Explore.Application.Features.Federation.Atproto.Services.AtprotoEventPublicationPlanner(
            new AtprotoEventGovernanceResolver(_atprotoSettingsResolver),
            Substitute.For<IEventRepository>(),
            Substitute.For<IAtprotoRecordRepository>(),
            Substitute.For<IUserAuthenticationTokenRepository>(),
            Substitute.For<IUserExternalLoginRepository>(),
            Substitute.For<IAtprotoPublicationPayloadBuilder>(),
            Substitute.For<IPdsSyncOutboxRepository>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Explore.Application.Features.Federation.Atproto.Services.AtprotoEventPublicationPlanner>.Instance);

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
            _addressGovernancePolicyResolver,
            _userContext,
            _tenantContext,
            _cache,
            new BusinessMetrics(meterFactory),
            _unitOfWork,
            _outboxRepository,
            _lifecyclePolicyProvider,
            _lifecycleReadinessEvaluator,
            eventLocationAttachmentService,
            atprotoPublicationPlanner,
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

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await _eventRepository.Received(1).Create(Arg.Is<Explore.Domain.Event>(entity =>
            entity.ParticipationConfiguration != null
            && entity.ParticipationConfiguration.ParticipationHandlingModeId == (int)ParticipationHandlingModeEnum.InformationOnly
            && entity.ParticipationConfiguration.AdvanceRegistrationObligationId == (int)AdvanceRegistrationObligationEnum.NotApplicable));
        await _eventSessionRepository.Received(1).Create(Arg.Any<EventSession>());
        await _cache.Received(1).RemoveAsync($"event:detail:{result.Id}", Arg.Any<CancellationToken>());
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
        session = session with { FeaturedImageId = useSessionImage ? imageId : null };
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

        await Assert.That(result.IsSuccess).IsFalse();
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

        await Assert.That(result.IsSuccess).IsFalse();
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

        await Assert.That(result.IsSuccess).IsTrue();
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

        await Assert.That(result.IsSuccess).IsTrue();
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

        await Assert.That(result.IsSuccess).IsTrue();
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

        await Assert.That(result.IsSuccess).IsTrue();
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

        await Assert.That(result.IsSuccess).IsTrue();
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

        await Assert.That(result.IsSuccess).IsTrue();
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

        await Assert.That(result.IsSuccess).IsTrue();
        await _eventIslamicAspectRepository.Received(1).Upsert(Arg.Is<EventIslamicAspect>(aspect =>
            aspect.Id == result.Id
            && aspect.GenderMode == GenderSegregationMode.Segregated));
        await _locationRepository.Received(1).Create(Arg.Is<Location>(location =>
            location.TenantId == tenantId
            && location.FullName == "Islamic Centre Brussels"
            && location.Pii.Address == "Rue Example 10"), Arg.Any<CancellationToken>());
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
        session = session with { IslamicAspect = new EventSessionIslamicAspectDto { StartTimeType = SessionStartTimeType.Fixed, ReferencePrayer = PrayerTime.Dhuhr, OffsetMinutes = 0 } };






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

        await Assert.That(result.IsSuccess).IsFalse();
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

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).Contains("permission");
        await Assert.That(result.Errors).IsEquivalentTo([
            "Your role in the organization does not include event creation permission."
        ]);
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

        await Assert.That(result.IsSuccess).IsTrue();
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

        await Assert.That(result.IsSuccess).IsTrue();
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

        await Assert.That(result.IsSuccess).IsTrue();
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
        await _atprotoSettingsResolver.Received(2).ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>());
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

        await Assert.That(result.IsSuccess).IsFalse();
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

        await Assert.That(result.IsSuccess).IsFalse();
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

        await Assert.That(result.IsSuccess).IsTrue();
        await _eventRepository.Received(1).Create(Arg.Is<Explore.Domain.Event>(entity =>
            entity.EventStatusId == (int)EventStatusEnum.Draft));
        await _eventSessionRepository.Received(1).Create(Arg.Is<EventSession>(session =>
            session.EventSessionStatusId == (int)EventSessionStatusEnum.Draft));
        await _lifecyclePolicyProvider.DidNotReceive().GetEffectivePolicyAsync(Arg.Any<Guid?>(), Arg.Any<ValidationProfile>(), Arg.Any<CancellationToken>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task Handle_WithVerifiedOrganizationLocation_PropagatesGovernanceAuthorityIntoTheLocation()
    {
        var userId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var organizationId = Guid.CreateVersion7();
        AddressGovernancePolicyRequest? capturedRequest = null;
        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _organizationRepository.Exists(organizationId).Returns(true);
        _actorResolver.ResolveAsync(userId, organizationId, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: false));
        _addressGovernancePolicyResolver.ResolveAsync(
                Arg.Do<AddressGovernancePolicyRequest>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(AddressGovernancePolicyDecision.Allowed(
                AddressCreationMode.OrganizationGoverned,
                LocationAddressVisibilityEnum.OrganizationScoped,
                organizationId));

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Verified organizer venue",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                OrganizationId = organizationId,
                Locations = [CreateLocationRequest("venue", "Verified address")]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(capturedRequest).IsNotNull();
        await Assert.That(capturedRequest!.TenantId).IsEqualTo(tenantId);
        await Assert.That(capturedRequest.ActorId).IsEqualTo(actorId);
        await Assert.That(capturedRequest.UserId).IsEqualTo(userId);
        await Assert.That(capturedRequest.OrganizationId).IsEqualTo(organizationId);
        await _locationRepository.Received(1).Create(
            Arg.Is<Location>(location =>
                location.AddressVisibility == LocationAddressVisibilityEnum.OrganizationScoped
                && location.AddressOrganizationId == organizationId
                && location.CreatedBy == actorId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithGroupPublisher_DoesNotPromoteTheGroupOrganizationIntoAddressGovernance()
    {
        var userId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();
        AddressGovernancePolicyRequest? capturedRequest = null;
        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(Guid.CreateVersion7());
        _groupRepository.Exists(groupId).Returns(true);
        _actorResolver.ResolveAsync(userId, null, groupId, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: false));
        _addressGovernancePolicyResolver.ResolveAsync(
                Arg.Do<AddressGovernancePolicyRequest>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(AddressGovernancePolicyDecision.Allowed(
                AddressCreationMode.OpenWithModeration,
                LocationAddressVisibilityEnum.CreatorPrivate));

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Group venue",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                GroupId = groupId,
                Locations = [CreateLocationRequest("group-venue", "Group address")]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(capturedRequest!.OrganizationId).IsNull();
        await _actorResolver.Received(1).ResolveAsync(userId, null, groupId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenAddressGovernanceFailsOrThrows_RejectsBeforeOpeningTheTransaction()
    {
        var userId = Guid.CreateVersion7();
        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(Guid.CreateVersion7());
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(Guid.CreateVersion7(), isCommunitySubmission: true));
        _addressGovernancePolicyResolver.ResolveAsync(
                Arg.Any<AddressGovernancePolicyRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AddressGovernancePolicyDecision>(
                new InvalidOperationException("governance unavailable")));

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Governance failure",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Locations = [CreateLocationRequest("venue", "Address")]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Event creation failed.");
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<Guid>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenAddressGovernanceIsCancelled_PropagatesCancellationWithoutMutation()
    {
        var userId = Guid.CreateVersion7();
        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(Guid.CreateVersion7());
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(Guid.CreateVersion7(), isCommunitySubmission: true));
        _addressGovernancePolicyResolver.ResolveAsync(
                Arg.Any<AddressGovernancePolicyRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AddressGovernancePolicyDecision>(
                new OperationCanceledException()));

        await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Cancelled governance",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Locations = [CreateLocationRequest("venue", "Address")]
            }
        }, CancellationToken.None));
        await _eventRepository.DidNotReceive().Create(Arg.Any<Explore.Domain.Event>());
    }

    [Test]
    public async Task Handle_WithPublishedSchedule_MapsAllAggregateFieldsAndChronologicalRollup()
    {
        var userId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var seriesId = Guid.CreateVersion7();
        const int registrationPolicyId = 17;
        var backgroundImageId = Guid.CreateVersion7();
        var laterStart = new DateTimeOffset(2032, 6, 4, 18, 0, 0, TimeSpan.Zero);
        var earlierStart = new DateTimeOffset(2032, 6, 2, 9, 0, 0, TimeSpan.Zero);
        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: false));
        _eventSeriesRepository.Exists(seriesId).Returns(true);
        _eventRegistrationPolicyRepository.Exists(registrationPolicyId).Returns(true);
        _madhabRepository.Exists(12).Returns(true);
        ConfigureEligibleImage(backgroundImageId, tenantId);
        ConfigurePublishedReadiness(tenantId);

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Mapped Event",
                Subtitle = "Mapped subtitle",
                Description = "Mapped description",
                Content = "Mapped content",
                Slug = "supplied-event-slug",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                EventStatusId = (int)EventStatusEnum.Published,
                VisibilityTypeId = 7,
                EventFormatId = 8,
                MadhabId = 11,
                IslamicAspect = new() { MadhabId = 12 },
                Timezone = "Europe/Brussels",
                BackgroundColor = "#112233",
                BackgroundEffect = "gradient",
                BackgroundImageId = backgroundImageId,
                EventSeriesId = seriesId,
                SeriesOrder = 3,
                RegistrationPolicyId = registrationPolicyId,
                Sessions =
                [
                    CreateSessionRequest(laterStart, laterStart.AddHours(1), "Later"),
                    CreateSessionRequest(earlierStart, earlierStart.AddHours(2), "Earlier")
                ]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _eventRepository.Received(1).Create(Arg.Is<Explore.Domain.Event>(entity =>
            entity.Title == "Mapped Event"
            && entity.Subtitle == "Mapped subtitle"
            && entity.Description == "Mapped description"
            && entity.Content == "Mapped content"
            && entity.Slug == "supplied-event-slug"
            && entity.PublicCode.Length == 12
            && entity.VisibilityTypeId == 7
            && entity.EventFormatId == 8
            && entity.MadhabId == 11
            && entity.Timezone == "Europe/Brussels"
            && entity.EventTimeZoneId == "Europe/Brussels"
            && entity.BackgroundColor == "#112233"
            && entity.BackgroundEffect == "gradient"
            && entity.BackgroundImageId == backgroundImageId
            && entity.EventSeriesId == seriesId
            && entity.SeriesOrder == 3
            && entity.RegistrationPolicyId == registrationPolicyId
            && entity.FirstSessionDate == new DateOnly(2032, 6, 2)
            && entity.LastSessionDate == new DateOnly(2032, 6, 4)
            && entity.FirstSessionStartUtc == earlierStart.UtcDateTime
            && entity.LastSessionStartUtc == laterStart.UtcDateTime
            && entity.SessionCount == 2
            && entity.ActorId == actorId
            && entity.SubmittedByUserId == null
            && entity.OrganizerActorId == actorId
            && entity.TenantId == tenantId
            && entity.CreatedBy == userId
            && entity.TotalViews == 0
            && !entity.IsDeleted));
    }

    [Test]
    public async Task Handle_WithDefaultedAggregateFields_GeneratesSlugAndUsesContractDefaults()
    {
        ConfigureCommunityIdentity();
        _madhabRepository.Exists(13).Returns(true);

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Default Aggregate Values",
                Slug = "  ",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                VisibilityTypeId = 0,
                EventFormatId = 0,
                IslamicAspect = new() { MadhabId = 13 }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _eventRepository.Received(1).Create(Arg.Is<Explore.Domain.Event>(entity =>
            entity.Slug == "default-aggregate-values"
            && entity.VisibilityTypeId == 1
            && entity.EventFormatId == 1
            && entity.MadhabId == 13
            && entity.EventProvenanceTypeId == (int)EventProvenanceTypeEnum.CommunityReported
            && entity.SubmittedByUserId != null
            && entity.OrganizerActorId == null));
    }

    [Test]
    public async Task Handle_WhenFeaturedImageDisappearsAfterEligibility_SucceedsWithoutAssigningStorageActor()
    {
        var (userId, actorId, tenantId) = ConfigureCommunityIdentity();
        var imageId = Guid.CreateVersion7();
        var image = CreateEligibleImage(imageId, tenantId);
        _storageObjectRepository.Exists(imageId).Returns(true);
        _storageObjectRepository.GetById(imageId).Returns(image, (StorageObject?)null);

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Vanishing image",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                FeaturedImageId = imageId
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _storageObjectRepository.DidNotReceive().Update(Arg.Any<StorageObject>());
        await Assert.That(actorId).IsNotEqualTo(Guid.Empty);
        await Assert.That(userId).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task Handle_WithFeaturedImage_AssignsTheResolvedPublisherActorAndUpdatesStorage()
    {
        var (_, actorId, tenantId) = ConfigureCommunityIdentity();
        var imageId = Guid.CreateVersion7();
        var image = ConfigureEligibleImage(imageId, tenantId);

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Owned image",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                FeaturedImageId = imageId
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _storageObjectRepository.Received(1).Update(Arg.Is<StorageObject>(value =>
            ReferenceEquals(value, image) && value.ActorId == actorId));
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

        await Assert.That(result.IsSuccess).IsTrue();
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

        await Assert.That(result.IsSuccess).IsTrue();
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

        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task Handle_WhenCancellationArrivesAfterGovernancePreflight_StopsBeforeBuildingTheAggregate()
    {
        using var cancellation = new CancellationTokenSource();
        var userId = Guid.CreateVersion7();
        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(Guid.CreateVersion7());
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(Guid.CreateVersion7(), isCommunitySubmission: true));
        _addressGovernancePolicyResolver.ResolveAsync(
                Arg.Any<AddressGovernancePolicyRequest>(),
                cancellation.Token)
            .Returns(_ =>
            {
                cancellation.Cancel();
                return AddressGovernancePolicyDecision.Allowed(
                    AddressCreationMode.OpenWithModeration,
                    LocationAddressVisibilityEnum.CreatorPrivate);
            });

        await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Cancelled after preflight",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Locations = [CreateLocationRequest("venue", "Address")]
            }
        }, cancellation.Token));
        await _eventRepository.DidNotReceive().Create(Arg.Any<Explore.Domain.Event>());
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<Guid>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenCancellationArrivesDuringReadiness_StopsBeforeTheTransaction()
    {
        using var cancellation = new CancellationTokenSource();
        var (_, _, tenantId) = ConfigureCommunityIdentity();
        var policy = new EventLifecyclePolicy
        {
            Profile = ValidationProfile.EventPublish,
            RequiredEventFields = new HashSet<Enum>(),
            RequiredSessionFields = new HashSet<Enum>()
        };
        _lifecyclePolicyProvider.GetEffectivePolicyAsync(
                tenantId, ValidationProfile.EventPublish, cancellation.Token)
            .Returns(policy);
        _lifecycleReadinessEvaluator.Evaluate(
                Arg.Any<Explore.Domain.Event>(), ValidationProfile.EventPublish, policy)
            .Returns(_ =>
            {
                cancellation.Cancel();
                return LifecycleReadinessResult.Success(ValidationProfile.EventPublish);
            });

        await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Cancelled readiness",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                EventStatusId = (int)EventStatusEnum.Published
            }
        }, cancellation.Token));
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<Guid>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_AfterCommit_EmitsCreatedCountWithoutIdentityTags()
    {
        ConfigureCommunityIdentity();
        long measurement = 0;
        List<KeyValuePair<string, object?>[]> measuredTags = [];
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, activeListener) =>
        {
            if (instrument.Meter.Name == _meterName && instrument.Name == "explore.events.created")
            {
                activeListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            measurement += value;
            measuredTags.Add(tags.ToArray());
        });
        listener.Start();

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Measured event",
                ParticipationConfiguration = CreateParticipationConfiguration()
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(measurement).IsGreaterThanOrEqualTo(1);
        await Assert.That(measuredTags.SelectMany(tags => tags)).IsEmpty();
    }

    [Test]
    public async Task Handle_WithPublishedEventAndSessionTemplates_PersistsProvenanceDefinitionsValuesAndProjections()
    {
        var (userId, _, tenantId) = ConfigureCommunityIdentity();
        var eventTemplateId = Guid.CreateVersion7();
        var sessionTemplateId = Guid.CreateVersion7();
        var eventTemplate = new EventTemplate
        {
            Id = eventTemplateId,
            TenantId = tenantId,
            TemplateKey = "event-template",
            DisplayName = "Event Template",
            Version = 4,
            IsPublished = true,
            IsActive = true
        };
        var sessionTemplate = new EventSessionTemplate
        {
            Id = sessionTemplateId,
            TenantId = tenantId,
            EventTemplateId = eventTemplateId,
            SessionTemplateKey = "session-template",
            DisplayName = "Session Template",
            Version = 6,
            IsPublished = true,
            IsActive = true
        };
        var eventDefinition = new EventCustomPropertyDefinition
        {
            Id = Guid.CreateVersion7(),
            Namespace = "event",
            Key = "audience",
            DisplayName = "Audience",
            DefaultOptionId = Guid.CreateVersion7()
        };
        var eventValue = new EventCustomPropertyValue
        {
            Id = Guid.CreateVersion7(),
            EventCustomPropertyDefinitionId = eventDefinition.Id,
            EventId = Guid.CreateVersion7(),
            TenantId = tenantId,
            TextValue = "families"
        };
        var sessionDefinition = new EventSessionCustomPropertyDefinition
        {
            Id = Guid.CreateVersion7(),
            Namespace = "session",
            Key = "track",
            DisplayName = "Track",
            DefaultOptionId = Guid.CreateVersion7()
        };
        var sessionValue = new EventSessionCustomPropertyValue
        {
            Id = Guid.CreateVersion7(),
            EventSessionCustomPropertyDefinitionId = sessionDefinition.Id,
            EventSessionId = Guid.CreateVersion7(),
            TenantId = tenantId,
            TextValue = "main"
        };
        var eventRuntimeDefaultOptionId = Guid.CreateVersion7();
        var sessionRuntimeDefaultOptionId = Guid.CreateVersion7();
        _eventTemplateRepository.Exists(eventTemplateId).Returns(true);
        _eventTemplateRepository.GetTemplateWithDetails(eventTemplateId).Returns(eventTemplate);
        _eventSessionTemplateRepository.Exists(sessionTemplateId).Returns(true);
        _eventSessionTemplateRepository.GetSessionTemplateWithDetails(sessionTemplateId).Returns(sessionTemplate);
        _instantiationService.InstantiateFromTemplate(
                Arg.Any<Guid>(), tenantId, eventTemplate, userId.ToString())
            .Returns(new InstantiationResult([
                new RuntimeDefinitionWithOptions(
                    eventDefinition,
                    [],
                    eventRuntimeDefaultOptionId,
                    eventValue)
            ]));
        _eventSessionTemplateInstantiationService.InstantiateFromSessionTemplate(
                Arg.Any<Guid>(), tenantId, sessionTemplate, userId.ToString())
            .Returns(new SessionInstantiationResult([
                new SessionRuntimeDefinitionWithOptions(
                    sessionDefinition,
                    [],
                    sessionRuntimeDefaultOptionId,
                    sessionValue)
            ]));
        var start = new DateTimeOffset(2035, 5, 20, 12, 0, 0, TimeSpan.Zero);

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Templated event",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                TemplateId = eventTemplateId,
                Sessions = [new CreateEventGraphSessionDto
                {
                    Title = "Templated session",
                    StartTime = start,
                    EndTime = start.AddHours(1),
                    SessionTemplateId = sessionTemplateId
                }]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _eventRepository.Received(1).Update(Arg.Is<Explore.Domain.Event>(entity =>
            entity.SourceTemplateId == eventTemplateId
            && entity.SourceTemplateKey == "event-template"
            && entity.SourceTemplateVersion == 4
            && entity.InstantiatedFromTemplateAt.HasValue
            && entity.LastSyncedFromTemplateAt == entity.InstantiatedFromTemplateAt));
        await _eventCustomPropertyRepository.Received(1).CreateWithOptions(
            Arg.Is<EventCustomPropertyDefinition>(definition =>
                ReferenceEquals(definition, eventDefinition) && definition.DefaultOptionId == null),
            Arg.Is<IReadOnlyList<EventCustomPropertyOption>>(options => options.Count == 0),
            eventRuntimeDefaultOptionId,
            Arg.Any<CancellationToken>());
        await _eventCustomPropertyRepository.Received(1).SetValue(eventValue, Arg.Any<CancellationToken>());
        await _projectionUpdater.Received(1).RefreshForEventAsync(result.Id, Arg.Any<CancellationToken>());
        await _eventSessionRepository.Received(1).Update(Arg.Is<EventSession>(session =>
            session.SourceTemplateId == sessionTemplateId
            && session.SourceTemplateKey == "session-template"
            && session.SourceTemplateVersion == 6
            && session.InstantiatedFromTemplateAt.HasValue
            && session.LastSyncedFromTemplateAt == session.InstantiatedFromTemplateAt));
        await _eventSessionCustomPropertyRepository.Received(1).CreateWithOptions(
            Arg.Is<EventSessionCustomPropertyDefinition>(definition =>
                ReferenceEquals(definition, sessionDefinition) && definition.DefaultOptionId == null),
            Arg.Is<IReadOnlyList<EventSessionCustomPropertyOption>>(options => options.Count == 0),
            sessionRuntimeDefaultOptionId,
            Arg.Any<CancellationToken>());
        await _eventSessionCustomPropertyRepository.Received(1).SetValue(sessionValue, Arg.Any<CancellationToken>());
        await _eventSessionCustomPropertyProjectionUpdater.Received(1)
            .RefreshForEventSessionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithInactiveTemplates_DoesNotPersistTemplateProvenanceOrRuntimeProperties()
    {
        var (_, _, tenantId) = ConfigureCommunityIdentity();
        var eventTemplateId = Guid.CreateVersion7();
        var sessionTemplateId = Guid.CreateVersion7();
        _eventTemplateRepository.Exists(eventTemplateId).Returns(true);
        _eventTemplateRepository.GetTemplateWithDetails(eventTemplateId).Returns(new EventTemplate
        {
            Id = eventTemplateId,
            TenantId = tenantId,
            TemplateKey = "inactive-event",
            DisplayName = "Inactive Event",
            Version = 1,
            IsPublished = true,
            IsActive = false
        });
        _eventSessionTemplateRepository.Exists(sessionTemplateId).Returns(true);
        _eventSessionTemplateRepository.GetSessionTemplateWithDetails(sessionTemplateId).Returns(new EventSessionTemplate
        {
            Id = sessionTemplateId,
            TenantId = tenantId,
            EventTemplateId = eventTemplateId,
            SessionTemplateKey = "unpublished-session",
            DisplayName = "Unpublished Session",
            Version = 1,
            IsPublished = false,
            IsActive = true
        });
        var start = new DateTimeOffset(2035, 6, 20, 12, 0, 0, TimeSpan.Zero);

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Skipped templates",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                TemplateId = eventTemplateId,
                Sessions = [new CreateEventGraphSessionDto
                {
                    StartTime = start,
                    EndTime = start.AddHours(1),
                    SessionTemplateId = sessionTemplateId
                }]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        _instantiationService.DidNotReceiveWithAnyArgs().InstantiateFromTemplate(default, default, default!, default!);
        await _eventSessionRepository.DidNotReceive().Update(Arg.Any<EventSession>());
        _eventSessionTemplateInstantiationService.DidNotReceiveWithAnyArgs()
            .InstantiateFromSessionTemplate(default, default, default!, default!);
        await _projectionUpdater.DidNotReceive().RefreshForEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventSessionCustomPropertyProjectionUpdater.DidNotReceive()
            .RefreshForEventSessionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithStructuredGraph_PersistsOrderedDaysRoomsSessionsAgendaAndDistinctAssignments()
    {
        var (_, _, tenantId) = ConfigureCommunityIdentity();
        var languageId = 21;
        var categoryId = Guid.CreateVersion7();
        var tagId = Guid.CreateVersion7();
        var speakerId = Guid.CreateVersion7();
        var early = new DateTimeOffset(2033, 3, 10, 8, 0, 0, TimeSpan.Zero);
        var late = new DateTimeOffset(2033, 3, 12, 14, 0, 0, TimeSpan.Zero);
        _languageRepository.Exists(languageId).Returns(true);
        _categoryRepository.Exists(categoryId).Returns(true);
        _tagRepository.Exists(tagId).Returns(true);
        _actorRepository.Exists(speakerId).Returns(true);
        var createdDays = new List<EventDay>();
        var createdRooms = new List<LocationRoom>();
        var createdSessions = new List<EventSession>();
        var createdAgenda = new List<EventAgendaItem>();
        _eventDayRepository.Create(Arg.Do<EventDay>(createdDays.Add))
            .Returns(call =>
            {
                var day = call.Arg<EventDay>();
                day.Id = Guid.CreateVersion7();
                return day;
            });
        _locationRoomRepository.Create(Arg.Do<LocationRoom>(createdRooms.Add))
            .Returns(call =>
            {
                var room = call.Arg<LocationRoom>();
                room.Id = Guid.CreateVersion7();
                return room;
            });
        _eventSessionRepository.Create(Arg.Do<EventSession>(createdSessions.Add))
            .Returns(call =>
            {
                var session = call.Arg<EventSession>();
                session.Id = Guid.CreateVersion7();
                return session;
            });
        _eventAgendaItemRepository.Create(Arg.Do<EventAgendaItem>(createdAgenda.Add))
            .Returns(call => call.Arg<EventAgendaItem>());

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Structured graph authority",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Locations =
                [
                    CreateLocationRequest("first-location", "First address"),
                    CreateLocationRequest("second-location", "Second address")
                ],
                Rooms =
                [
                    new CreateEventRoomDto
                    {
                        TempKey = "later-room",
                        LocationTempKey = "second-location",
                        Name = "Later Room",
                        Slug = "supplied-room-slug",
                        Description = "Room description",
                        Capacity = 200,
                        SortOrder = 5
                    },
                    new CreateEventRoomDto
                    {
                        TempKey = "first-room",
                        LocationTempKey = "first-location",
                        Name = "First Room",
                        Slug = " ",
                        SortOrder = 1
                    }
                ],
                Days =
                [
                    new CreateEventGraphDayDto
                    {
                        TempKey = " special-day ",
                        LocalDate = new DateOnly(2033, 3, 11),
                        Label = "Special",
                        Description = "Special description",
                        BannerText = "Special banner",
                        IsPublished = false,
                        SortOrder = 9,
                        AllowsDayScopeRegistration = true
                    }
                ],
                Sessions =
                [
                    new CreateEventGraphSessionDto
                    {
                        Title = "Late",
                        Slug = "supplied-session-slug",
                        StartTime = late,
                        EndTime = late.AddHours(1),
                        RoomTempKey = "later-room",
                        LanguageIds = [languageId, languageId],
                        SpeakerActorIds = [speakerId, speakerId]
                    },
                    new CreateEventGraphSessionDto
                    {
                        Title = null,
                        StartTime = early,
                        EndTime = early.AddHours(2),
                        RoomTempKey = "first-room",
                        LocationTempKey = "first-location",
                        DayTempKey = "SPECIAL-DAY",
                        IslamicAspect = new EventSessionIslamicAspectDto
                        {
                            StartTimeType = SessionStartTimeType.RelativeToPrayer,
                            ReferencePrayer = PrayerTime.Fajr,
                            OffsetMinutes = 15,
                            RequiresWudu = true,
                            RitualRequirementsJson = "{}"
                        }
                    }
                ],
                AgendaItems =
                [
                    new CreateEventGraphAgendaItemDto
                    {
                        Title = "Later agenda",
                        StartTime = late.AddMinutes(15),
                        EndTime = late.AddMinutes(30),
                        RoomTempKey = "later-room",
                        SortOrder = 4
                    },
                    new CreateEventGraphAgendaItemDto
                    {
                        Title = "Earlier agenda",
                        StartTime = early.AddMinutes(10),
                        EndTime = early.AddMinutes(20),
                        RoomTempKey = "first-room",
                        DayTempKey = "special-day",
                        SortOrder = 2
                    }
                ],
                CategoryIds = [categoryId, categoryId],
                TagIds = [tagId, tagId]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(createdDays.Select(day => day.LocalDate)).IsEquivalentTo([
            new DateOnly(2033, 3, 10),
            new DateOnly(2033, 3, 11),
            new DateOnly(2033, 3, 12)
        ]);
        var automaticFirstDay = createdDays.Single(day => day.LocalDate == new DateOnly(2033, 3, 10));
        var specialDay = createdDays.Single(day => day.LocalDate == new DateOnly(2033, 3, 11));
        var automaticLastDay = createdDays.Single(day => day.LocalDate == new DateOnly(2033, 3, 12));
        await Assert.That(automaticFirstDay.IsPublished).IsTrue();
        await Assert.That(automaticFirstDay.SortOrder).IsEqualTo(0);
        await Assert.That(automaticLastDay.SortOrder).IsEqualTo(2);
        await Assert.That(specialDay.Label).IsEqualTo("Special");
        await Assert.That(specialDay.Description).IsEqualTo("Special description");
        await Assert.That(specialDay.BannerText).IsEqualTo("Special banner");
        await Assert.That(specialDay.IsPublished).IsFalse();
        await Assert.That(specialDay.SortOrder).IsEqualTo(9);
        await Assert.That(specialDay.AllowsDayScopeRegistration).IsTrue();
        await Assert.That(createdRooms).Count().IsEqualTo(2);
        await Assert.That(createdRooms[0].Slug).IsEqualTo("supplied-room-slug");
        await Assert.That(createdRooms[0].Description).IsEqualTo("Room description");
        await Assert.That(createdRooms[0].Capacity).IsEqualTo(200);
        await Assert.That(createdRooms[1].Slug).IsEqualTo("first-room");
        await Assert.That(createdSessions.Select(session => session.Title)).IsEquivalentTo([
            "Structured graph authority",
            "Late"
        ]);
        await Assert.That(createdSessions[0].StartTime).IsEqualTo(early);
        await Assert.That(createdSessions[0].SortOrder).IsEqualTo(0);
        await Assert.That(createdSessions[0].Slug).IsEqualTo("structured-graph-authority-session-1");
        await Assert.That(createdSessions[0].EventDayId).IsEqualTo(specialDay.Id);
        await Assert.That(createdSessions[1].StartTime).IsEqualTo(late);
        await Assert.That(createdSessions[1].SortOrder).IsEqualTo(1);
        await Assert.That(createdSessions[1].Slug).IsEqualTo("supplied-session-slug");
        await Assert.That(createdAgenda.Select(item => item.Title)).IsEquivalentTo([
            "Earlier agenda",
            "Later agenda"
        ]);
        await Assert.That(createdAgenda[0].EventDayId).IsEqualTo(specialDay.Id);
        await _eventSessionIslamicAspectRepository.Received(1).Create(Arg.Is<EventSessionIslamicAspect>(aspect =>
            aspect.EventSessionId == createdSessions[0].Id
            && aspect.StartTimeType == SessionStartTimeType.RelativeToPrayer
            && aspect.ReferencePrayer == PrayerTime.Fajr
            && aspect.OffsetMinutes == 15
            && aspect.RequiresWudu
            && aspect.RitualRequirementsJson == "{}"));
        await _eventSessionLanguageRepository.Received(1).Create(Arg.Is<EventSessionLanguage>(language =>
            language.EventSessionId == createdSessions[1].Id
            && language.LanguageId == languageId
            && language.TenantId == tenantId));
        await _eventSessionSpeakerRepository.Received(1).Create(Arg.Is<EventSessionSpeaker>(speaker =>
            speaker.EventSessionId == createdSessions[1].Id
            && speaker.ActorId == speakerId
            && speaker.TenantId == tenantId));
        await _eventCategoriesRepository.Received(1).Create(Arg.Is<Explore.Domain.EventCategories>(assignment =>
            assignment.EventId == result.Id && assignment.CategoryId == categoryId && assignment.TenantId == tenantId));
        await _eventTagsRepository.Received(1).Create(Arg.Is<Explore.Domain.EventTags>(assignment =>
            assignment.EventId == result.Id && assignment.TagId == tagId && assignment.TenantId == tenantId));
    }

    [Test]
    public async Task Handle_WithRoomScheduleConflict_ReturnsExactFailureAndStopsTransactionalGraphWork()
    {
        ConfigureCommunityIdentity();
        var conflictingSessionId = Guid.CreateVersion7();
        var categoryId = Guid.CreateVersion7();
        _categoryRepository.Exists(categoryId).Returns(true);
        var start = new DateTimeOffset(2034, 1, 10, 10, 0, 0, TimeSpan.Zero);
        _eventSessionRepository.GetOverlappingSessionsInRoomAsync(
                Arg.Any<Guid>(),
                start,
                start.AddHours(1),
                null,
                Arg.Any<CancellationToken>())
            .Returns([new EventSession { Id = conflictingSessionId, Event = null!, Tenant = null! }]);

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Conflicting graph",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Locations = [CreateLocationRequest("venue", "Address")],
                Rooms = [new CreateEventRoomDto
                {
                    TempKey = "room",
                    LocationTempKey = "venue",
                    Name = "Room"
                }],
                Sessions = [new CreateEventGraphSessionDto
                {
                    Title = "Conflict",
                    RoomTempKey = "room",
                    StartTime = start,
                    EndTime = start.AddHours(1)
                }],
                CategoryIds = [categoryId]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Event creation failed.");
        await Assert.That(result.FailureCode).IsEqualTo("room_schedule_conflict");
        await Assert.That(result.Errors).Count().IsEqualTo(1);
        await Assert.That(result.Errors[0]).IsEqualTo(
            "The selected room already has 1 overlapping session(s) in the requested time range.");
        await _eventSessionRepository.DidNotReceive().Create(Arg.Any<EventSession>());
        await _eventCategoriesRepository.DidNotReceive().Create(Arg.Any<Explore.Domain.EventCategories>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithExistingLocationAndRoomOnlyReferences_UsesEachPublicReferenceFallback()
    {
        ConfigureCommunityIdentity();
        var existingLocationId = Guid.CreateVersion7();
        _locationRepository.Exists(existingLocationId).Returns(true);
        var capturedSessions = new List<EventSession>();
        _eventSessionRepository.Create(Arg.Do<EventSession>(capturedSessions.Add)).Returns(call =>
        {
            var session = call.Arg<EventSession>();
            session.Id = Guid.CreateVersion7();
            return session;
        });
        var start = new DateTimeOffset(2036, 2, 2, 10, 0, 0, TimeSpan.Zero);

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Reference fallbacks",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Locations = [CreateLocationRequest("nested-location", "Nested address")],
                Rooms = [new CreateEventRoomDto
                {
                    TempKey = "nested-room",
                    LocationTempKey = "nested-location",
                    Name = "Nested Room"
                }],
                Sessions =
                [
                    new CreateEventGraphSessionDto
                    {
                        Title = "Existing location",
                        LocationId = existingLocationId,
                        StartTime = start,
                        EndTime = start.AddHours(1)
                    },
                    new CreateEventGraphSessionDto
                    {
                        Title = "Room-derived location",
                        RoomTempKey = "nested-room",
                        StartTime = start.AddHours(2),
                        EndTime = start.AddHours(3)
                    }
                ]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        var existingLocationSession = capturedSessions.Single(session => session.Title == "Existing location");
        var roomDerivedSession = capturedSessions.Single(session => session.Title == "Room-derived location");
        await Assert.That(existingLocationSession.LocationId).IsEqualTo(existingLocationId);
        await Assert.That(roomDerivedSession.LocationId).IsNotNull();
        await Assert.That(roomDerivedSession.RoomId).IsNotNull();
        await Assert.That(roomDerivedSession.LocationId).IsNotEqualTo(existingLocationId);
    }

    [Test]
    public async Task Handle_WithPublishedLocationAndNoRooms_UsesTheFirstLocationForTheDefaultSession()
    {
        var (_, _, tenantId) = ConfigureCommunityIdentity();
        ConfigurePublishedReadiness(tenantId);
        Location? createdLocation = null;
        _locationRepository.Create(Arg.Do<Location>(location => createdLocation = location), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var location = call.Arg<Location>();
            location.Id = Guid.CreateVersion7();
            return location;
        });

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Location-only default",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                EventStatusId = (int)EventStatusEnum.Published,
                Locations = [CreateLocationRequest("only-location", "Only address")]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(createdLocation).IsNotNull();
        await _eventSessionRepository.Received(1).Create(Arg.Is<EventSession>(session =>
            session.LocationId == createdLocation!.Id && session.RoomId == null));
    }

    [Test]
    public async Task Handle_WithPublishedDefaultSession_UsesLowestSortRoomAndThatRoomsLocation()
    {
        var (_, _, tenantId) = ConfigureCommunityIdentity();
        ConfigurePublishedReadiness(tenantId);
        var locations = new List<Location>();
        var rooms = new List<LocationRoom>();
        _locationRepository.Create(Arg.Do<Location>(locations.Add), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var location = call.Arg<Location>();
            location.Id = Guid.CreateVersion7();
            return location;
        });
        _locationRoomRepository.Create(Arg.Do<LocationRoom>(rooms.Add)).Returns(call =>
        {
            var room = call.Arg<LocationRoom>();
            room.Id = Guid.CreateVersion7();
            return room;
        });

        var result = await _handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Default room selection",
                ParticipationConfiguration = CreateParticipationConfiguration(),
                EventStatusId = (int)EventStatusEnum.Published,
                Locations =
                [
                    CreateLocationRequest("first-location", "First address"),
                    CreateLocationRequest("selected-location", "Selected address")
                ],
                Rooms =
                [
                    new CreateEventRoomDto
                    {
                        TempKey = "high-sort-room",
                        LocationTempKey = "first-location",
                        Name = "High Sort",
                        SortOrder = 20
                    },
                    new CreateEventRoomDto
                    {
                        TempKey = "low-sort-room",
                        LocationTempKey = "selected-location",
                        Name = "Low Sort",
                        SortOrder = 1
                    }
                ]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        var selectedRoom = rooms.Single(room => room.Name == "Low Sort");
        var selectedLocation = locations.Single(location => location.Pii.Address == "Selected address");
        await _eventSessionRepository.Received(1).Create(Arg.Is<EventSession>(session =>
            session.RoomId == selectedRoom.Id
            && session.LocationId == selectedLocation.Id
            && session.Slug == "default-room-selection-session-1"));
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

    private (Guid UserId, Guid ActorId, Guid TenantId) ConfigureCommunityIdentity()
    {
        var userId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        _userContext.GetRequiredUserId().Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(actorId, isCommunitySubmission: true));
        return (userId, actorId, tenantId);
    }

    private void ConfigurePublishedReadiness(Guid tenantId)
    {
        var policy = new EventLifecyclePolicy
        {
            Profile = ValidationProfile.EventPublish,
            RequiredEventFields = new HashSet<Enum>(),
            RequiredSessionFields = new HashSet<Enum>()
        };
        _lifecyclePolicyProvider.GetEffectivePolicyAsync(
                tenantId,
                ValidationProfile.EventPublish,
                Arg.Any<CancellationToken>())
            .Returns(policy);
        _lifecycleReadinessEvaluator.Evaluate(
                Arg.Any<Explore.Domain.Event>(),
                ValidationProfile.EventPublish,
                policy)
            .Returns(LifecycleReadinessResult.Success(ValidationProfile.EventPublish));
    }

    private StorageObject ConfigureEligibleImage(Guid imageId, Guid tenantId)
    {
        var image = CreateEligibleImage(imageId, tenantId);
        _storageObjectRepository.Exists(imageId).Returns(true);
        _storageObjectRepository.GetById(imageId).Returns(image);
        return image;
    }

    private static StorageObject CreateEligibleImage(Guid imageId, Guid tenantId) => new()
    {
        Id = imageId,
        TenantId = tenantId,
        Tenant = null!,
        FileType = null!,
        Uri = $"storage://{imageId:N}.png",
        Provider = "local",
        FullName = "event.png",
        SafeDisplayName = "event.png",
        Extension = "png",
        ContentType = "image/png",
        Purpose = StorageObjectPurposes.EventImage,
        Visibility = StorageObjectVisibilities.PublicImage,
        LifecycleState = StorageObjectLifecycleStates.Active
    };

    private static CreateEventLocationDto CreateLocationRequest(string tempKey, string address) => new()
    {
        TempKey = tempKey,
        FullName = $"{tempKey} venue",
        Address = address,
        Postcode = "1000",
        Country = "Belgium",
        City = "Brussels"
    };

    private static CreateEventGraphSessionDto CreateSessionRequest(
        DateTimeOffset start,
        DateTimeOffset end,
        string title) => new()
    {
        Title = title,
        StartTime = start,
        EndTime = end
    };

    private static CreateEventGraphSessionDto CreateSessionRequest()
    {
        var start = new DateTimeOffset(2031, 1, 15, 10, 0, 0, TimeSpan.Zero);
        return CreateSessionRequest(start, start.AddHours(2), "Opening Session");
    }
}
