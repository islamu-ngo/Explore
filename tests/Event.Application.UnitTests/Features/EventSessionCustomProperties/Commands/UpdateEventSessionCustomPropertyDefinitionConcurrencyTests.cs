// ABOUTME: Regression tests for session-local custom-property definition stale-write detection.
// ABOUTME: Ensures update handlers reject stale edit forms before mutating tracked definitions.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventSessionCustomProperties.Handlers.Commands;
using Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessionCustomProperties.Commands;

public class UpdateEventSessionCustomPropertyDefinitionConcurrencyTests
{
    [Test]
    public async Task Handle_WhenOptionsAreOmitted_UpdatesDefinitionAndProjectionWithoutReplacingOptions()
    {
        var repository = Substitute.For<IEventSessionCustomPropertyRepository>();
        var projectionUpdater = Substitute.For<IEventSessionCustomPropertyProjectionUpdater>();
        var governancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var mapper = Substitute.For<IMapper>();
        var cache = Substitute.For<HybridCache>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        var handler = new UpdateEventSessionCustomPropertyDefinitionCommandHandler(
            repository,
            projectionUpdater,
            governancePolicy,
            quotaResolver,
            currentUserService,
            mapper,
            cache,
            unitOfWork);

        var definition = CreateDefinition();
        var command = new UpdateEventSessionCustomPropertyDefinitionCommand
        {
            DefinitionId = definition.Id,
            DefinitionDto = new UpdateEventSessionCustomPropertyDefinitionDto
            {
                Metadata = new UpdateCustomPropertyDefinitionMetadataDto { DisplayName = "Renamed" }
            },
            ExpectedConcurrencyStamp = definition.ConcurrencyStamp,
            TenantId = definition.TenantId
        };
        repository.GetTrackedDefinitionWithOptions(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);
        governancePolicy.EvaluateDefinition(definition.Namespace, definition.Key)
            .Returns(new CustomPropertyGovernanceEvaluation
            {
                NormalizedNamespace = definition.Namespace,
                NormalizedKey = definition.Key
            });
        repository.ExistsDefinitionKey(definition.EventSessionId, definition.Namespace, definition.Key, definition.Id).Returns(false);
        mapper.Map(Arg.Any<CreateEventSessionCustomPropertyDefinitionDto>(), definition).Returns(definition);

        var result = await handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await repository.Received(1).Update(definition);
        await repository.DidNotReceive().UpdateWithOptions(
            Arg.Any<EventSessionCustomPropertyDefinition>(),
            Arg.Any<IReadOnlyCollection<EventSessionCustomPropertyOption>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
        await projectionUpdater.Received(1).UpdateForDefinitionAsync(definition.Id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenConcurrencyStampIsStale_ThrowsConcurrencyConflict()
    {
        var repository = Substitute.For<IEventSessionCustomPropertyRepository>();
        var projectionUpdater = Substitute.For<IEventSessionCustomPropertyProjectionUpdater>();
        var governancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var mapper = Substitute.For<IMapper>();
        var cache = Substitute.For<HybridCache>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new UpdateEventSessionCustomPropertyDefinitionCommandHandler(
            repository,
            projectionUpdater,
            governancePolicy,
            quotaResolver,
            currentUserService,
            mapper,
            cache,
            unitOfWork);

        var definition = CreateDefinition();
        var command = new UpdateEventSessionCustomPropertyDefinitionCommand
        {
            DefinitionId = definition.Id,
            DefinitionDto = CreateDto(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            TenantId = definition.TenantId
        };

        repository.GetTrackedDefinitionWithOptions(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);

        var exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => handler.Handle(command, CancellationToken.None));

        await Assert.That(exception!.Code).IsEqualTo(ConcurrencyConflictException.ConcurrentUpdate);
        await Assert.That(exception.EntityType).IsEqualTo("event_session_custom_property_definition");
        await repository.DidNotReceive().UpdateWithOptions(
            Arg.Any<EventSessionCustomPropertyDefinition>(),
            Arg.Any<IReadOnlyCollection<EventSessionCustomPropertyOption>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
        await projectionUpdater.DidNotReceive().UpdateForDefinitionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static UpdateEventSessionCustomPropertyDefinitionDto CreateDto()
    {
        return new UpdateEventSessionCustomPropertyDefinitionDto
        {
            Metadata = new UpdateCustomPropertyDefinitionMetadataDto
            {
                Namespace = "Tenant Community",
                Key = "Prayer Notes",
                DisplayName = "Prayer Notes",
                ExposureLevel = ExposureLevel.OrganizerOnly,
                IsActive = true
            },
            Validation = new UpdateCustomPropertyDefinitionValidationDto { PropertyType = PropertyType.Text }
        };
    }

    private static EventSessionCustomPropertyDefinition CreateDefinition()
    {
        return new EventSessionCustomPropertyDefinition
        {
            Id = Guid.NewGuid(),
            EventSessionId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Namespace = "tenant.community",
            Key = "prayer_notes",
            DisplayName = "Prayer Notes",
            PropertyType = PropertyType.Text,
            ExposureLevel = ExposureLevel.OrganizerOnly,
            ConcurrencyStamp = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        };
    }
}
