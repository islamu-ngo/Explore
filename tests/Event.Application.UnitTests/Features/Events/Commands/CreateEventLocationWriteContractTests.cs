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
                location.Pii!.LocationId = location.Id;
            }))
            .Returns(call => call.Arg<Location>()
                ?? throw new InvalidOperationException("The location repository received a null entity."));
        using var metricsFixture = new MetricsFixture();
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
        }

        await Assert.That(violations).IsEmpty();
    }

    private static CreateEventCommandHandler CreateHandler(
        ILocationRepository locations,
        Guid tenantId,
        Guid userId,
        BusinessMetrics metrics)
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
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Guid>>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<Guid>>>()
                    ?? throw new InvalidOperationException("The unit of work received a null operation.");
                return operation(call.Arg<CancellationToken>());
            });
        var userContext = Substitute.For<IUserContext>();
        userContext.GetRequiredUserId().Returns(userId);
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        var actorResolver = Substitute.For<IEventActorResolver>();
        actorResolver.ResolveAsync(userId, null, null, Arg.Any<CancellationToken>())
            .Returns(EventActorResult.Success(Guid.CreateVersion7(), isCommunitySubmission: true));
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

    private sealed class MetricsFixture : IDisposable
    {
        private readonly Meter _meter;

        public MetricsFixture()
        {
            var meterFactory = Substitute.For<IMeterFactory>();
            _meter = new Meter("location-write-contract");
            meterFactory.Create(Arg.Any<MeterOptions>()).Returns(_meter);
            Metrics = new BusinessMetrics(meterFactory);
        }

        public BusinessMetrics Metrics { get; }

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
