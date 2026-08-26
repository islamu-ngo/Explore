// ABOUTME: Failing public-contract specifications for nested Location writes during Event creation.
// ABOUTME: Removes model coordinate authority while preserving governed EventLocation disclosure reads.

using System.Diagnostics.Metrics;
using System.Reflection;
using Event.Application.UnitTests.Common;
using Explore.Application.Caching;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.Location;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Geocoding;
using Explore.Application.Features.Events.Requests.Commands;
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

public sealed class CreateEventLocationWriteContractTests
{
    private static readonly string[] CoordinatePropertyNames = ["Latitude", "Longitude"];

    [Test]
    public async Task NestedEventLocationWriteDoesNotExposeRawCoordinatesAndAllAuthorizedDisclosureReadsRetainBoth()
    {
        string[] forbiddenWriteMembers = typeof(CreateEventLocationDto)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.Name is "Latitude" or "Longitude")
            .Select(property => $"{nameof(CreateEventLocationDto)}.{property.Name}")
            .ToArray();
        Type[] authorizedDisclosureTypes =
        [
            typeof(EventLocationPublicFieldsDto),
            typeof(EventLocationAttendeeFieldsDto),
            typeof(EventLocationManagementFieldsDto)
        ];
        string[] missingDisclosureMembers = authorizedDisclosureTypes
            .SelectMany(type => CoordinatePropertyNames
                .Where(name => type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public) is null)
                .Select(name => $"{type.Name}.{name}"))
            .ToArray();

        await Assert.That(forbiddenWriteMembers.Concat(missingDisclosureMembers)).IsEmpty();
    }

    [Test]
    public async Task HandleWithNestedManualLocationCapturesRepositoryCreateWithoutModelCoordinates()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        Location? persisted = null;
        var locations = Substitute.For<ILocationRepository>();
        locations.Create(Arg.Do<Location>(location =>
            {
                persisted = location;
                location.Id = Guid.CreateVersion7();
            }), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Location>()
                ?? throw new InvalidOperationException("The location repository received a null entity."));
        using var metricsFixture = new MetricsFixture();
        List<IReadOnlyDictionary<string, object?>> metricTags = [];
        using var metricListener = new MeterListener();
        metricListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == metricsFixture.MeterName
                && instrument.Name == "explore.events.created")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        metricListener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
            metricTags.Add(tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value)));
        metricListener.Start();
        var handler = CreateHandler(locations, tenantId, userId, metricsFixture.Metrics);
        var nestedLocation = new CreateEventLocationDto
        {
            TempKey = "primary-location",
            FullName = "Model venue",
            Address = "Rue Model 10",
            Postcode = "1000",
            Country = "Belgium",
            City = "Brussels"
        };
        SetCoordinateIfPresent(nestedLocation, "Latitude", 50.8503);
        SetCoordinateIfPresent(nestedLocation, "Longitude", 4.3517);

        var response = await handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Nested location contract",
                ParticipationConfiguration = new ConfigureEventParticipationDto
                {
                    ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.InformationOnly,
                    AdvanceRegistrationObligationId = (int)AdvanceRegistrationObligationEnum.NotApplicable
                },
                Locations = [nestedLocation]
            }
        }, CancellationToken.None);

        var violations = new List<string>();
        if (!response.IsSuccess)
        {
            violations.Add("the public CreateEventCommand workflow rejected the valid fixture");
        }
        if (persisted is null)
        {
            violations.Add("the public CreateEventCommand workflow did not call ILocationRepository.Create");
        }
        else
        {
            if (persisted.TenantId != tenantId)
            {
                violations.Add("nested Location persistence did not use the trusted tenant context");
            }
            if (persisted.Pii is null)
            {
                violations.Add("nested Location persistence omitted the required PII child");
            }
            else
            {
                if (persisted.Pii.Address != "Rue Model 10" || persisted.Pii.Postcode != "1000")
                {
                    violations.Add("nested Location persistence did not preserve the exact manual address and postcode");
                }
                if (persisted.Pii.Latitude is not null || persisted.Pii.Longitude is not null)
                {
                    violations.Add("model-supplied coordinates reached nested Location persistence");
                }
            }
            if (persisted.AddressSource != LocationAddressSourceEnum.Manual
                || persisted.AddressVisibility != LocationAddressVisibilityEnum.CreatorPrivate
                || persisted.AddressOrganizationId is not null
                || persisted.CreatedBy != userId)
            {
                violations.Add("nested Location persistence did not apply only the trusted typed manual decision");
            }
        }

        await Assert.That(violations).IsEmpty();
        IReadOnlyDictionary<string, object?> emittedTags = metricTags.Single();
        string metricObservable = string.Join('|', emittedTags.Select(tag => $"{tag.Key}={tag.Value}"));
        await Assert.That(metricObservable).DoesNotContain(tenantId.ToString("D"));
        await Assert.That(metricObservable).DoesNotContain(userId.ToString("D"));
        await Assert.That(metricObservable).DoesNotContain(nestedLocation.Address);
        await Assert.That(metricObservable).DoesNotContain(nestedLocation.Postcode);
    }

    [Test]
    public async Task NestedLocationCreatesUseTheExactTransactionTokenForEveryLocation()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        var locations = Substitute.For<ILocationRepository>();
        using var transactionCancellation = new CancellationTokenSource();
        locations.Create(Arg.Any<Location>(), transactionCancellation.Token).Returns(call =>
        {
            Location location = call.Arg<Location>()
                ?? throw new InvalidOperationException("The location repository received a null entity.");
            location.Id = Guid.CreateVersion7();
            return location;
        });
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<Guid>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<Guid>>>()
                    ?? throw new InvalidOperationException("The unit of work received a null operation.");
                return operation(transactionCancellation.Token);
            });
        using var metricsFixture = new MetricsFixture();
        var handler = CreateHandler(
            locations,
            tenantId,
            userId,
            metricsFixture.Metrics,
            suppliedUnitOfWork: unitOfWork);

        var response = await handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Transaction token contract",
                ParticipationConfiguration = new ConfigureEventParticipationDto
                {
                    ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.InformationOnly,
                    AdvanceRegistrationObligationId = (int)AdvanceRegistrationObligationEnum.NotApplicable
                },
                Locations =
                [
                    NewNestedLocation("first", "First address"),
                    NewNestedLocation("second", "Second address")
                ]
            }
        }, CancellationToken.None);

        await Assert.That(response.IsSuccess).IsTrue();
        var createCalls = locations.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILocationRepository.Create))
            .ToArray();
        await Assert.That(createCalls.Length).IsEqualTo(2);
        await Assert.That(createCalls.All(call =>
            call.GetArguments().Length == 2
            && call.GetArguments()[1] is CancellationToken token
            && token == transactionCancellation.Token)).IsTrue();
    }

    [Test]
    public async Task MultipleNestedLocationsAreAllPreflightedBeforeAnyEventGraphWrite()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        var locations = Substitute.For<ILocationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var governance = Substitute.For<IAddressGovernancePolicyResolver>();
        int decisionIndex = 0;
        governance.ResolveAsync(Arg.Any<AddressGovernancePolicyRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++decisionIndex == 1
                ? AddressGovernancePolicyDecision.Allowed(
                    AddressCreationMode.OpenWithModeration,
                    LocationAddressVisibilityEnum.CreatorPrivate)
                : AddressGovernancePolicyDecision.Denied(AddressCreationMode.Disabled));
        using var metricsFixture = new MetricsFixture();
        var handler = CreateHandler(
            locations,
            tenantId,
            userId,
            metricsFixture.Metrics,
            governance,
            unitOfWork);

        var response = await handler.Handle(new CreateEventCommand
        {
            EventDto = new CreateEventDto
            {
                Title = "Atomic nested governance",
                ParticipationConfiguration = new ConfigureEventParticipationDto
                {
                    ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.InformationOnly,
                    AdvanceRegistrationObligationId = (int)AdvanceRegistrationObligationEnum.NotApplicable
                },
                Locations =
                [
                    NewNestedLocation("first", "First address"),
                    NewNestedLocation("second", "Second address")
                ]
            }
        }, CancellationToken.None);

        await Assert.That(response.IsSuccess).IsFalse();
        await Assert.That(response.Message).IsEqualTo("Event creation failed.");
        await governance!.Received(2).ResolveAsync(
            Arg.Is<AddressGovernancePolicyRequest>(request =>
                request != null
                && request.TenantId == tenantId
                && request.UserId == userId
                && request.OrganizationId == null),
            Arg.Any<CancellationToken>());
        await locations.DidNotReceive().Create(Arg.Any<Location>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<Guid>>>(),
            Arg.Any<CancellationToken>());
    }

    private static CreateEventCommandHandler CreateHandler(
        ILocationRepository locations,
        Guid tenantId,
        Guid userId,
        BusinessMetrics metrics,
        IAddressGovernancePolicyResolver? governance = null,
        IUnitOfWork? suppliedUnitOfWork = null)
    {
        var events = Substitute.For<IEventRepository>();
        events.Create(Arg.Any<Explore.Domain.Event>()).Returns(call =>
        {
            var entity = call.Arg<Explore.Domain.Event>()
                ?? throw new InvalidOperationException("The event repository received a null entity.");
            entity.Id = Guid.CreateVersion7();
            entity.ConcurrencyStamp = Guid.CreateVersion7();
            return entity;
        });
        var unitOfWork = suppliedUnitOfWork ?? Substitute.For<IUnitOfWork>();
        if (suppliedUnitOfWork is null)
        {
            unitOfWork.ExecuteInTransactionAsync(
                    Arg.Any<Func<CancellationToken, Task<Guid>>>(),
                    Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var operation = call.Arg<Func<CancellationToken, Task<Guid>>>()
                        ?? throw new InvalidOperationException("The unit of work received a null operation.");
                    return operation(call.Arg<CancellationToken>());
                });
        }
        var userContext = Substitute.For<IUserContext>();
        userContext.GetRequiredUserId().Returns(userId);
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        var actorResolver = Substitute.For<IEventActorResolver>();
        actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(userId, isCommunitySubmission: true));
        if (governance is null)
        {
            governance = Substitute.For<IAddressGovernancePolicyResolver>();
            governance.ResolveAsync(Arg.Any<AddressGovernancePolicyRequest>(), Arg.Any<CancellationToken>())
                .Returns(AddressGovernancePolicyDecision.Allowed(
                    AddressCreationMode.OpenWithModeration,
                    LocationAddressVisibilityEnum.CreatorPrivate));
        }
        var eventLocationRepository = Substitute.For<IEventLocationRepository>();
        eventLocationRepository.AddAsync(Arg.Any<EventLocation>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<EventLocation>(0));

        return new CreateEventCommandHandler(
            events,
            Substitute.For<IEventSessionRepository>(),
            Substitute.For<IEventSessionSpeakerRepository>(),
            Substitute.For<IEventIslamicAspectRepository>(),
            Substitute.For<IEventSessionIslamicAspectRepository>(),
            Substitute.For<IEventSessionLanguageRepository>(),
            Substitute.For<IEventRoleAssignmentRepository>(),
            actorResolver,
            Substitute.For<IAudienceAgeRepository>(),
            Substitute.For<IAudienceGenderRepository>(),
            Substitute.For<IEventTypeRepository>(),
            Substitute.For<IStorageObjectRepository>(),
            Substitute.For<IEventTemplateRepository>(),
            Substitute.For<IEventSeriesRepository>(),
            Substitute.For<IEventRegistrationPolicyRepository>(),
            Substitute.For<IEventCustomPropertyRepository>(),
            Substitute.For<IEventCustomPropertyProjectionUpdater>(),
            Substitute.For<IEventTemplateInstantiationService>(),
            Substitute.For<IEventSessionTemplateRepository>(),
            Substitute.For<IEventSessionCustomPropertyRepository>(),
            Substitute.For<IEventSessionCustomPropertyProjectionUpdater>(),
            Substitute.For<IEventSessionTemplateInstantiationService>(),
            Substitute.For<IOrganizationRepository>(),
            Substitute.For<IGroupRepository>(),
            locations,
            Substitute.For<IRegistrationModeRepository>(),
            Substitute.For<ILanguageRepository>(),
            Substitute.For<IMadhabRepository>(),
            Substitute.For<ICategoryRepository>(),
            Substitute.For<ITagRepository>(),
            Substitute.For<IScheduleItemKindRepository>(),
            Substitute.For<IEventSessionKindRepository>(),
            Substitute.For<IActorRepository>(),
            Substitute.For<IEventDayRepository>(),
            Substitute.For<ILocationRoomRepository>(),
            Substitute.For<IEventAgendaItemRepository>(),
            Substitute.For<IEventCategoriesRepository>(),
            Substitute.For<IEventTagsRepository>(),
            new EventScheduleProjectionCalculator(),
            governance,
            userContext,
            tenantContext,
            Substitute.For<HybridCache>(),
            metrics,
            unitOfWork,
            Substitute.For<IOutboxRepository>(),
            Substitute.For<IEventLifecyclePolicyProvider>(),
            Substitute.For<IEventLifecycleReadinessEvaluator>(),
            new EventLocationAttachmentService(eventLocationRepository, userContext, tenantContext, TimeProvider.System),
            AtprotoPublicationPlannerTestFactory.Disabled(),
            TimeProvider.System);
    }

    private static CreateEventLocationDto NewNestedLocation(string key, string address) => new()
    {
        TempKey = key,
        FullName = $"{key} venue",
        Address = address,
        Postcode = "1000",
        Country = "Belgium",
        City = "Brussels"
    };

    private sealed class MetricsFixture : IDisposable
    {
        private static long s_meterSequence;
        private readonly Meter _meter;

        public MetricsFixture()
        {
            var meterFactory = Substitute.For<IMeterFactory>();
            MeterName = $"location-write-contract-{Interlocked.Increment(ref s_meterSequence)}";
            _meter = new Meter(MeterName);
            meterFactory.Create(Arg.Any<MeterOptions>()).Returns(_meter);
            Metrics = new BusinessMetrics(meterFactory);
        }

        public BusinessMetrics Metrics { get; }
        public string MeterName { get; }

        public void Dispose()
        {
            Metrics.Dispose();
            _meter.Dispose();
        }
    }

    private static void SetCoordinateIfPresent(object target, string propertyName, double value)
    {
        PropertyInfo? property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property?.SetMethod?.IsPublic == true)
        {
            property.SetValue(target, value);
        }
    }
}
