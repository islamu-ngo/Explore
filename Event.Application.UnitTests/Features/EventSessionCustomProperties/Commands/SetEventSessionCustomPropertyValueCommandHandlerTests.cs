// ABOUTME: Unit tests for event-session runtime custom-property single-value writes.
// ABOUTME: Proves service-level ordinal and duplicate checks mirror event-scope writes.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.Features.EventSessionCustomProperties.Handlers.Commands;
using Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessionCustomProperties.Commands;

public class SetEventSessionCustomPropertyValueCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenSingleValueDefinitionReceivesOrdinalOne_ReturnsFailure()
    {
        var definitionId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var repository = Substitute.For<IEventSessionCustomPropertyRepository>();

        repository.GetDefinitionWithDetails(definitionId).Returns(CreateDefinition(definitionId, sessionId, isMulti: false));
        var handler = CreateSut(repository);

        var result = await handler.Handle(
            CreateCommand(definitionId, sessionId, ordinal: 1, textValue: "Arabic"),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That((result.Errors ?? []).Any(error => error.Contains("ordinal 0", StringComparison.Ordinal))).IsTrue();
        await repository.DidNotReceiveWithAnyArgs().SetValue(default!, default);
    }

    [Test]
    public async Task Handle_WhenMultiValueDefinitionReceivesDuplicateNormalizedValue_ReturnsFailure()
    {
        var definitionId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var repository = Substitute.For<IEventSessionCustomPropertyRepository>();
        var definition = CreateDefinition(definitionId, sessionId, isMulti: true);
        SetValues(definition,
        [
            new EventSessionCustomPropertyValue
            {
                Id = Guid.NewGuid(),
                EventSessionCustomPropertyDefinitionId = definitionId,
                EventSessionId = sessionId,
                Ordinal = 0,
                TextValue = "Alpha"
            }
        ]);
        repository.GetDefinitionWithDetails(definitionId).Returns(definition);
        var handler = CreateSut(repository);

        var result = await handler.Handle(
            CreateCommand(definitionId, sessionId, ordinal: 1, textValue: " alpha "),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That((result.Errors ?? []).Any(error => error.Contains("Duplicate normalized values", StringComparison.Ordinal))).IsTrue();
        await repository.DidNotReceiveWithAnyArgs().SetValue(default!, default);
    }

    private static SetEventSessionCustomPropertyValueCommandHandler CreateSut(IEventSessionCustomPropertyRepository repository)
    {
        return new SetEventSessionCustomPropertyValueCommandHandler(
            repository,
            Substitute.For<IEventSessionCustomPropertyProjectionUpdater>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<ITenantContext>(),
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IMapper>());
    }

    private static SetEventSessionCustomPropertyValueCommand CreateCommand(Guid definitionId, Guid sessionId, int ordinal, string textValue)
    {
        return new SetEventSessionCustomPropertyValueCommand
        {
            ValueDto = new SetEventSessionCustomPropertyValueDto
            {
                EventSessionCustomPropertyDefinitionId = definitionId,
                EventSessionId = sessionId,
                Ordinal = ordinal,
                TextValue = textValue
            }
        };
    }

    private static EventSessionCustomPropertyDefinition CreateDefinition(Guid definitionId, Guid sessionId, bool isMulti)
    {
        return new EventSessionCustomPropertyDefinition
        {
            Id = definitionId,
            EventSessionId = sessionId,
            TenantId = Guid.NewGuid(),
            Namespace = "tenant.session",
            Key = "primary_language",
            DisplayName = "Primary Language",
            PropertyType = PropertyType.Text,
            ExposureLevel = ExposureLevel.OrganizerOnly,
            IsActive = true,
            IsMulti = isMulti
        };
    }

    private static void SetValues(EventSessionCustomPropertyDefinition definition, IEnumerable<EventSessionCustomPropertyValue> values)
    {
        var field = typeof(EventSessionCustomPropertyDefinition).GetField("_values", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<EventSessionCustomPropertyValue>)field.GetValue(definition)!;
        list.AddRange(values);
    }
}
