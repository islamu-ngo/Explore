// ABOUTME: Unit tests for event-session runtime custom-property multi-value replacement semantics.
// ABOUTME: Mirrors event-level coverage for session scope ordering and duplicate rules.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.Features.EventSessionCustomProperties.Handlers.Commands;
using Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Definitions;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessionCustomProperties.Commands;

public class SetEventSessionCustomPropertyMultiValuesCommandHandlerTests
{
    private readonly IEventSessionCustomPropertyRepository _repository = Substitute.For<IEventSessionCustomPropertyRepository>();
    private readonly IEventSessionCustomPropertyProjectionUpdater _projectionUpdater = Substitute.For<IEventSessionCustomPropertyProjectionUpdater>();
    private readonly ICustomPropertyQuotaResolver _quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly Guid _definitionId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public SetEventSessionCustomPropertyMultiValuesCommandHandlerTests()
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
        _mapper.Map<EventSessionCustomPropertyValue>(Arg.Any<SetEventSessionCustomPropertyValueDto>())
            .Returns(callInfo =>
            {
                var dto = callInfo.Arg<SetEventSessionCustomPropertyValueDto>();
                ArgumentNullException.ThrowIfNull(dto);
                return new EventSessionCustomPropertyValue
                {
                    EventSessionCustomPropertyDefinitionId = dto.EventSessionCustomPropertyDefinitionId,
                    EventSessionId = dto.EventSessionId,
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
        IReadOnlyCollection<EventSessionCustomPropertyValue>? capturedValues = null;
        _repository.SetMultiValues(_definitionId, _sessionId, Arg.Do<IReadOnlyCollection<EventSessionCustomPropertyValue>>(values => capturedValues = values), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await CreateSut().Handle(CreateCommand("alpha", "beta", "gamma"), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(capturedValues).IsNotNull();
        var values = capturedValues ?? throw new InvalidOperationException("Repository payload was not captured.");
        await Assert.That(values.Select(value => value.Ordinal).SequenceEqual([0, 1, 2])).IsTrue();
        await Assert.That(values.Select(value => value.TextValue ?? string.Empty).SequenceEqual(["alpha", "beta", "gamma"])).IsTrue();
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
    public async Task Handle_WhenReplacingValues_PreservesInputOrderingSemantics()
    {
        _repository.GetDefinitionWithDetails(_definitionId).Returns(CreateDefinition(isMulti: true));
        IReadOnlyCollection<EventSessionCustomPropertyValue>? capturedValues = null;
        _repository.SetMultiValues(_definitionId, _sessionId, Arg.Do<IReadOnlyCollection<EventSessionCustomPropertyValue>>(values => capturedValues = values), Arg.Any<CancellationToken>())
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
    public async Task Handle_WhenMultiValueRowQuotaExceeded_ReturnsFailure()
    {
        _repository.GetDefinitionWithDetails(_definitionId).Returns(CreateDefinition(isMulti: true));
        _quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxMultiValueRowsPerValue.Key, _tenantId, Arg.Any<CancellationToken>())
            .Returns(2);

        var result = await CreateSut().Handle(CreateCommand("alpha", "beta", "gamma"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.QuotaExceeded);
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await Assert.That(result.QuotaExceeded!.QuotaKey).IsEqualTo(CustomPropertyQuotaSettingDefinitions.MaxMultiValueRowsPerValue.Key);
        await Assert.That(result.QuotaExceeded.Limit).IsEqualTo(2);
        await Assert.That(result.QuotaExceeded.Actual).IsNull();
        await Assert.That(result.QuotaExceeded.Attempted).IsEqualTo(3);
        await Assert.That(result.QuotaExceeded.Scope).IsEqualTo("event_session_custom_property_multi_values");
        await Assert.That(result.QuotaExceeded.TenantId).IsEqualTo(_tenantId);
        await Assert.That((result.Errors ?? []).Any(error => error.Contains(FailureCodes.QuotaExceeded, StringComparison.Ordinal))).IsTrue();
        await _repository.DidNotReceiveWithAnyArgs().SetMultiValues(default, default, default!, default);
    }

    [Test]
    public async Task Handle_WhenMultiValueRowCountEqualsQuota_Succeeds()
    {
        _repository.GetDefinitionWithDetails(_definitionId).Returns(CreateDefinition(isMulti: true));
        _quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxMultiValueRowsPerValue.Key, _tenantId, Arg.Any<CancellationToken>())
            .Returns(3);

        var result = await CreateSut().Handle(CreateCommand("alpha", "beta", "gamma"), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.FailureCode).IsNotEqualTo(FailureCodes.QuotaExceeded);
        await Assert.That(result.QuotaExceeded).IsNull();
        await _repository.Received(1).SetMultiValues(
            _definitionId,
            _sessionId,
            Arg.Is<IReadOnlyCollection<EventSessionCustomPropertyValue>>(values => values.Count == 3),
            Arg.Any<CancellationToken>());
    }

    private SetEventSessionCustomPropertyMultiValuesCommandHandler CreateSut() => new(
        _repository,
        _projectionUpdater,
        _quotaResolver,
        _tenantContext,
        _currentUserService,
        _mapper,
        _unitOfWork);

    private SetEventSessionCustomPropertyMultiValuesCommand CreateCommand(params string[] values)
    {
        return new SetEventSessionCustomPropertyMultiValuesCommand
        {
            DefinitionId = _definitionId,
            EventSessionId = _sessionId,
            Values = values.Select(value => new SetEventSessionCustomPropertyValueDto
            {
                EventSessionCustomPropertyDefinitionId = _definitionId,
                EventSessionId = _sessionId,
                TextValue = value
            }).ToList()
        };
    }

    private EventSessionCustomPropertyDefinition CreateDefinition(bool isMulti)
    {
        return new EventSessionCustomPropertyDefinition
        {
            Id = _definitionId,
            EventSessionId = _sessionId,
            TenantId = _tenantId,
            Namespace = "tenant.session",
            Key = "tags",
            DisplayName = "Tags",
            PropertyType = PropertyType.Text,
            ExposureLevel = ExposureLevel.OrganizerOnly,
            IsActive = true,
            IsMulti = isMulti
        };
    }
}
