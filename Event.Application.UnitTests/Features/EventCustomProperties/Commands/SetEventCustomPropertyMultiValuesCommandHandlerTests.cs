// ABOUTME: Unit tests for event runtime custom-property multi-value replacement semantics.
// ABOUTME: Proves ordinal assignment, replacement ordering, single-value rejection, and duplicate normalized-value rejection.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.Features.EventCustomProperties.Handlers.Commands;
using Explore.Application.Features.EventCustomProperties.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Definitions;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventCustomProperties.Commands;

public class SetEventCustomPropertyMultiValuesCommandHandlerTests
{
    private readonly IEventCustomPropertyRepository _repository = Substitute.For<IEventCustomPropertyRepository>();
    private readonly IEventCustomPropertyProjectionUpdater _projectionUpdater = Substitute.For<IEventCustomPropertyProjectionUpdater>();
    private readonly ICustomPropertyQuotaResolver _quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly Guid _definitionId = Guid.NewGuid();
    private readonly Guid _eventId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public SetEventCustomPropertyMultiValuesCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUserService.UserId.Returns(_userId);
        _quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxMultiValueRowsPerValue.Key, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(20);
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task>>();
                ArgumentNullException.ThrowIfNull(operation);
                return operation(CancellationToken.None);
            });
        _mapper.Map<EventCustomPropertyValue>(Arg.Any<SetEventCustomPropertyValueDto>())
            .Returns(callInfo =>
            {
                var dto = callInfo.Arg<SetEventCustomPropertyValueDto>();
                ArgumentNullException.ThrowIfNull(dto);
                return new EventCustomPropertyValue
                {
                    EventCustomPropertyDefinitionId = dto.EventCustomPropertyDefinitionId,
                    EventId = dto.EventId,
                    Ordinal = dto.Ordinal,
                    TextValue = dto.TextValue,
                    NumberValue = dto.NumberValue,
                    BooleanValue = dto.BooleanValue,
                    DateTimeValue = dto.DateTimeValue,
                    OptionId = dto.OptionId
                };
            });
    }

    [Test]
    public async Task Handle_WithThreeValues_SetsSequentialOrdinals()
    {
        _repository.GetDefinitionWithDetails(_definitionId).Returns(CreateDefinition(isMulti: true));
        IReadOnlyCollection<EventCustomPropertyValue>? capturedValues = null;
        _repository.SetMultiValues(_definitionId, _eventId, Arg.Do<IReadOnlyCollection<EventCustomPropertyValue>>(values => capturedValues = values), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await CreateSut().Handle(CreateCommand("alpha", "beta", "gamma"), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(capturedValues).IsNotNull();
        var values = capturedValues ?? throw new InvalidOperationException("Repository payload was not captured.");
        await Assert.That(values.Select(value => value.Ordinal).SequenceEqual([0, 1, 2])).IsTrue();
        await Assert.That(values.Select(value => value.TextValue ?? string.Empty).SequenceEqual(["alpha", "beta", "gamma"])).IsTrue();
    }

    [Test]
    public async Task Handle_WhenReplacingValues_PreservesInputOrderingSemantics()
    {
        _repository.GetDefinitionWithDetails(_definitionId).Returns(CreateDefinition(isMulti: true));
        IReadOnlyCollection<EventCustomPropertyValue>? capturedValues = null;
        _repository.SetMultiValues(_definitionId, _eventId, Arg.Do<IReadOnlyCollection<EventCustomPropertyValue>>(values => capturedValues = values), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await CreateSut().Handle(CreateCommand("third", "first", "second"), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(capturedValues).IsNotNull();
        var values = capturedValues ?? throw new InvalidOperationException("Repository payload was not captured.");
        await Assert.That(values.OrderBy(value => value.Ordinal).Select(value => value.TextValue ?? string.Empty).SequenceEqual(["third", "first", "second"])).IsTrue();
    }

    [Test]
    public async Task Handle_WhenSingleValueDefinitionReceivesSecondValue_ReturnsFailure()
    {
        _repository.GetDefinitionWithDetails(_definitionId).Returns(CreateDefinition(isMulti: false));

        var result = await CreateSut().Handle(CreateCommand("alpha", "beta"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That((result.Errors ?? []).Any(error => error.Contains("Single-value", StringComparison.Ordinal))).IsTrue();
        await _repository.DidNotReceiveWithAnyArgs().SetMultiValues(default, default, default!, default);
    }

    [Test]
    public async Task Handle_WhenDuplicateNormalizedTextValues_ReturnsFailure()
    {
        _repository.GetDefinitionWithDetails(_definitionId).Returns(CreateDefinition(isMulti: true));

        var result = await CreateSut().Handle(CreateCommand("  Alpha ", "alpha"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That((result.Errors ?? []).Any(error => error.Contains("Duplicate normalized values", StringComparison.Ordinal))).IsTrue();
        await _repository.DidNotReceiveWithAnyArgs().SetMultiValues(default, default, default!, default);
    }

    [Test]
    public async Task Handle_WhenMultiValueRowQuotaExceeded_ReturnsFailure()
    {
        _repository.GetDefinitionWithDetails(_definitionId).Returns(CreateDefinition(isMulti: true));
        _quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxMultiValueRowsPerValue.Key, _tenantId, Arg.Any<CancellationToken>())
            .Returns(2);

        var result = await CreateSut().Handle(CreateCommand("alpha", "beta", "gamma"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That((result.Errors ?? []).Any(error => error.Contains("quota_exceeded", StringComparison.Ordinal))).IsTrue();
        await _repository.DidNotReceiveWithAnyArgs().SetMultiValues(default, default, default!, default);
    }

    private SetEventCustomPropertyMultiValuesCommandHandler CreateSut() => new(
        _repository,
        _projectionUpdater,
        _quotaResolver,
        _tenantContext,
        _currentUserService,
        _mapper,
        _unitOfWork);

    private SetEventCustomPropertyMultiValuesCommand CreateCommand(params string[] values)
    {
        return new SetEventCustomPropertyMultiValuesCommand
        {
            DefinitionId = _definitionId,
            EventId = _eventId,
            Values = values.Select(value => new SetEventCustomPropertyValueDto
            {
                EventCustomPropertyDefinitionId = _definitionId,
                EventId = _eventId,
                TextValue = value
            }).ToList()
        };
    }

    private EventCustomPropertyDefinition CreateDefinition(bool isMulti)
    {
        return new EventCustomPropertyDefinition
        {
            Id = _definitionId,
            EventId = _eventId,
            TenantId = _tenantId,
            Namespace = "tenant.event",
            Key = "tags",
            DisplayName = "Tags",
            PropertyType = PropertyType.Text,
            ExposureLevel = ExposureLevel.OrganizerOnly,
            IsActive = true,
            IsMulti = isMulti
        };
    }
}
