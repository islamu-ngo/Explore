// ABOUTME: Unit tests for event runtime custom-property single-value writes.
// ABOUTME: Proves service-level rejection of non-zero ordinals for single-value definitions.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.Features.EventCustomProperties.Handlers.Commands;
using Explore.Application.Features.EventCustomProperties.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventCustomProperties.Commands;

public class SetEventCustomPropertyValueCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenSingleValueDefinitionReceivesOrdinalOne_ReturnsFailure()
    {
        var definitionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var repository = Substitute.For<IEventCustomPropertyRepository>();

        repository.GetDefinitionWithDetails(definitionId).Returns(CreateDefinition(definitionId, eventId, isMulti: false));
        var handler = CreateSut(repository);

        var result = await handler.Handle(
            CreateCommand(definitionId, eventId, ordinal: 1, textValue: "Arabic"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That((result.Errors ?? []).Any(error => error.Contains("ordinal 0", StringComparison.Ordinal))).IsTrue();
        await repository.DidNotReceiveWithAnyArgs().SetValue(default!, default);
    }

    [Test]
    public async Task Handle_WhenMultiValueDefinitionReceivesDuplicateNormalizedValue_ReturnsFailure()
    {
        var definitionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var repository = Substitute.For<IEventCustomPropertyRepository>();
        var definition = CreateDefinition(definitionId, eventId, isMulti: true);
        SetValues(definition,
        [
            new EventCustomPropertyValue
            {
                Id = Guid.NewGuid(),
                EventCustomPropertyDefinitionId = definitionId,
                EventId = eventId,
                Ordinal = 0,
                TextValue = "Alpha"
            }
        ]);
        repository.GetDefinitionWithDetails(definitionId).Returns(definition);
        var handler = CreateSut(repository);

        var result = await handler.Handle(
            CreateCommand(definitionId, eventId, ordinal: 1, textValue: " alpha "),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That((result.Errors ?? []).Any(error => error.Contains("Duplicate normalized values", StringComparison.Ordinal))).IsTrue();
        await repository.DidNotReceiveWithAnyArgs().SetValue(default!, default);
    }

    [Test]
    public async Task Handle_WhenMultiValueDefinitionUpdatesSameOrdinalWithSameValue_Succeeds()
    {
        var definitionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var repository = Substitute.For<IEventCustomPropertyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();
        var definition = CreateDefinition(definitionId, eventId, isMulti: true);
        SetValues(definition,
        [
            new EventCustomPropertyValue
            {
                Id = Guid.NewGuid(),
                EventCustomPropertyDefinitionId = definitionId,
                EventId = eventId,
                Ordinal = 1,
                TextValue = "Alpha"
            }
        ]);
        repository.GetDefinitionWithDetails(definitionId).Returns(definition);
        mapper.Map<EventCustomPropertyValue>(Arg.Any<SetEventCustomPropertyValueDto>()).Returns(new EventCustomPropertyValue
        {
            EventCustomPropertyDefinitionId = definitionId,
            EventId = eventId,
            Ordinal = 1,
            TextValue = " alpha "
        });
        repository.SetValue(Arg.Any<EventCustomPropertyValue>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var saved = callInfo.ArgAt<EventCustomPropertyValue>(0);
                ArgumentNullException.ThrowIfNull(saved);
                return Task.FromResult(saved);
            });
        unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<EventCustomPropertyValue>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<EventCustomPropertyValue>>>();
                ArgumentNullException.ThrowIfNull(operation);
                return operation(CancellationToken.None);
            });
        var handler = CreateSut(repository, unitOfWork: unitOfWork, mapper: mapper);

        var result = await handler.Handle(
            CreateCommand(definitionId, eventId, ordinal: 1, textValue: " alpha "),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await repository.Received(1).SetValue(Arg.Any<EventCustomPropertyValue>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenNumberDefinitionReceivesTextValue_ReturnsFailure()
    {
        var definitionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var repository = Substitute.For<IEventCustomPropertyRepository>();
        var definition = CreateDefinition(definitionId, eventId, isMulti: false, propertyType: PropertyType.Number);
        repository.GetDefinitionWithDetails(definitionId).Returns(definition);
        var handler = CreateSut(repository);

        var result = await handler.Handle(
            CreateCommand(definitionId, eventId, ordinal: 0, textValue: "not-a-number"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That((result.Errors ?? []).Any(error => error.Contains("NumberValue", StringComparison.Ordinal))).IsTrue();
        await repository.DidNotReceiveWithAnyArgs().SetValue(default!, default);
    }

    [Test]
    public async Task Handle_WhenInactiveDefinitionReceivesValue_ReturnsFailure()
    {
        var definitionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var repository = Substitute.For<IEventCustomPropertyRepository>();
        var definition = CreateDefinition(definitionId, eventId, isMulti: false);
        definition.IsActive = false;
        repository.GetDefinitionWithDetails(definitionId).Returns(definition);
        var handler = CreateSut(repository);

        var result = await handler.Handle(
            CreateCommand(definitionId, eventId, ordinal: 0, textValue: "Arabic"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That((result.Errors ?? []).Any(error => error.Contains("not active", StringComparison.Ordinal))).IsTrue();
        await repository.DidNotReceiveWithAnyArgs().SetValue(default!, default);
    }

    private static SetEventCustomPropertyValueCommandHandler CreateSut(
        IEventCustomPropertyRepository repository,
        IUnitOfWork? unitOfWork = null,
        IMapper? mapper = null)
    {
        return new SetEventCustomPropertyValueCommandHandler(
            repository,
            Substitute.For<IEventCustomPropertyProjectionUpdater>(),
            unitOfWork ?? Substitute.For<IUnitOfWork>(),
            Substitute.For<ITenantContext>(),
            Substitute.For<ICurrentUserService>(),
            mapper ?? Substitute.For<IMapper>());
    }

    private static SetEventCustomPropertyValueCommand CreateCommand(Guid definitionId, Guid eventId, int ordinal, string textValue)
    {
        return new SetEventCustomPropertyValueCommand
        {
            ValueDto = new SetEventCustomPropertyValueDto
            {
                EventCustomPropertyDefinitionId = definitionId,
                EventId = eventId,
                Ordinal = ordinal,
                TextValue = textValue
            }
        };
    }

    private static EventCustomPropertyDefinition CreateDefinition(
        Guid definitionId,
        Guid eventId,
        bool isMulti,
        PropertyType propertyType = PropertyType.Text)
    {
        return new EventCustomPropertyDefinition
        {
            Id = definitionId,
            EventId = eventId,
            TenantId = Guid.NewGuid(),
            Namespace = "tenant.event",
            Key = "primary_language",
            DisplayName = "Primary Language",
            PropertyType = propertyType,
            ExposureLevel = ExposureLevel.OrganizerOnly,
            IsActive = true,
            IsMulti = isMulti
        };
    }

    private static void SetValues(EventCustomPropertyDefinition definition, IEnumerable<EventCustomPropertyValue> values)
    {
        var field = typeof(EventCustomPropertyDefinition).GetField("_values", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<EventCustomPropertyValue>)field.GetValue(definition)!;
        list.AddRange(values);
    }
}
