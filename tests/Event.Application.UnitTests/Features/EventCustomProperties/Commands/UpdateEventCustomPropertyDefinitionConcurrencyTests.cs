// ABOUTME: Regression tests for event-local custom-property definition stale-write detection.
// ABOUTME: Ensures update handlers reject stale edit forms before mutating tracked definitions.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventCustomProperties.Handlers.Commands;
using Explore.Application.Features.EventCustomProperties.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventCustomProperties.Commands;

public class UpdateEventCustomPropertyDefinitionConcurrencyTests
{
    [Test]
    public async Task Handle_WhenConcurrencyStampIsStale_ThrowsConcurrencyConflict()
    {
        var repository = Substitute.For<IEventCustomPropertyRepository>();
        var projectionUpdater = Substitute.For<IEventCustomPropertyProjectionUpdater>();
        var governancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var mapper = Substitute.For<IMapper>();
        var cache = Substitute.For<HybridCache>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new UpdateEventCustomPropertyDefinitionCommandHandler(
            repository,
            projectionUpdater,
            governancePolicy,
            quotaResolver,
            currentUserService,
            mapper,
            cache,
            unitOfWork);

        var definition = CreateDefinition();
        var command = new UpdateEventCustomPropertyDefinitionCommand
        {
            DefinitionId = definition.Id,
            DefinitionDto = CreateDto(definition.Id, definition.EventId, Guid.NewGuid()),
            ExpectedConcurrencyStamp = Guid.NewGuid()
        };

        repository.GetTrackedDefinitionWithOptions(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);

        var exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => handler.Handle(command, CancellationToken.None));

        await Assert.That(exception!.Code).IsEqualTo(ConcurrencyConflictException.ConcurrentUpdate);
        await Assert.That(exception.EntityType).IsEqualTo("event_custom_property_definition");
        await repository.DidNotReceive().UpdateWithOptions(
            Arg.Any<EventCustomPropertyDefinition>(),
            Arg.Any<IReadOnlyCollection<EventCustomPropertyOption>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
        await projectionUpdater.DidNotReceive().UpdateForDefinitionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static UpdateEventCustomPropertyDefinitionDto CreateDto(
        Guid definitionId,
        Guid eventId,
        Guid expectedConcurrencyStamp)
    {
        return new UpdateEventCustomPropertyDefinitionDto
        {
            EventId = eventId,
            Namespace = "Tenant Community",
            Key = "Prayer Notes",
            DisplayName = "Prayer Notes",
            PropertyType = PropertyType.Text,
            ExposureLevel = ExposureLevel.OrganizerOnly,
            IsActive = true,
        };
    }

    private static EventCustomPropertyDefinition CreateDefinition()
    {
        return new EventCustomPropertyDefinition
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Namespace = "tenant.community",
            Key = "prayer_notes",
            DisplayName = "Prayer Notes",
            PropertyType = PropertyType.Text,
            ExposureLevel = ExposureLevel.OrganizerOnly,
            ConcurrencyStamp = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        };
    }
}
