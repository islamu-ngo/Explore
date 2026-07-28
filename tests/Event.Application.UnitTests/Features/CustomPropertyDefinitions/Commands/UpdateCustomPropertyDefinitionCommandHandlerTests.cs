// ABOUTME: Unit tests for shared custom-property definition updates.
// ABOUTME: Verifies not-found handling, duplicate rejection, and option replacement semantics through the repository contract.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.Exceptions;
using Explore.Application.Features.CustomPropertyDefinitions.Handlers.Commands;
using Explore.Application.Features.CustomPropertyDefinitions.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Definitions;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.CustomPropertyDefinitions.Commands;

public class UpdateCustomPropertyDefinitionCommandHandlerTests
{
    private readonly ICustomPropertyDefinitionRepository _customPropertyDefinitionRepository;
    private readonly ICustomPropertyGovernancePolicy _customPropertyGovernancePolicy;
    private readonly ICustomPropertyQuotaResolver _quotaResolver;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpdateCustomPropertyDefinitionCommandHandler _handler;

    public UpdateCustomPropertyDefinitionCommandHandlerTests()
    {
        _customPropertyDefinitionRepository = Substitute.For<ICustomPropertyDefinitionRepository>();
        _customPropertyGovernancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
        _quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _mapper = Substitute.For<IMapper>();
        _cache = Substitute.For<HybridCache>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        // Execute the lambda so inner repo logic runs in tests
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<CustomPropertyDefinition>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var op = callInfo.Arg<Func<CancellationToken, Task<CustomPropertyDefinition>>>();
                return op(CancellationToken.None);
            });

        _handler = new UpdateCustomPropertyDefinitionCommandHandler(
            _customPropertyDefinitionRepository,
            _customPropertyGovernancePolicy,
            _quotaResolver,
            _currentUserService,
            _mapper,
            _cache,
            _unitOfWork);

        _quotaResolver.GetIntAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(500);
    }

    [Test]
    public async Task Handle_WhenDefinitionNotFound_ReturnsFailure()
    {
        var command = new UpdateCustomPropertyDefinitionCommand
        {
            DefinitionId = Guid.NewGuid(),
            DefinitionDto = CreateDto(),
            ExpectedConcurrencyStamp = ConcurrencyStamp
        };

        _customPropertyDefinitionRepository.GetTrackedDefinitionWithOptions(command.DefinitionId, Arg.Any<CancellationToken>())
            .Returns((CustomPropertyDefinition?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("not found");
    }

    [Test]
    public async Task Handle_WhenScopedMachineKeyExists_ReturnsFailure()
    {
        var tenantId = Guid.NewGuid();
        var existing = CreateExistingDefinition(tenantId);
        var command = new UpdateCustomPropertyDefinitionCommand
        {
            DefinitionId = existing.Id,
            DefinitionDto = CreateDto(),
            ExpectedConcurrencyStamp = ConcurrencyStamp
        };

        _customPropertyDefinitionRepository.GetTrackedDefinitionWithOptions(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        _customPropertyGovernancePolicy.EvaluateDefinition(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new CustomPropertyGovernanceEvaluation
            {
                NormalizedNamespace = "tenant.community",
                NormalizedKey = "prayer_notes",
            });
        _customPropertyDefinitionRepository.ExistsScopedMachineKey(tenantId, EntityTypeName.Organization, "tenant.community", "prayer_notes", existing.Id)
            .Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors.Any(e => e.Contains("same Namespace + Key", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task Handle_WhenConcurrencyStampIsStale_ThrowsConcurrencyConflict()
    {
        var tenantId = Guid.NewGuid();
        var existing = CreateExistingDefinition(tenantId);
        var command = new UpdateCustomPropertyDefinitionCommand
        {
            DefinitionId = existing.Id,
            DefinitionDto = CreateDto(),
            ExpectedConcurrencyStamp = Guid.NewGuid()
        };

        _customPropertyDefinitionRepository.GetTrackedDefinitionWithOptions(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => _handler.Handle(command, CancellationToken.None));

        await Assert.That(exception!.Code).IsEqualTo(ConcurrencyConflictException.ConcurrentUpdate);
        await Assert.That(exception.EntityType).IsEqualTo("custom_property_definition");
        await _customPropertyDefinitionRepository.DidNotReceive().UpdateWithOptions(
            Arg.Any<CustomPropertyDefinition>(),
            Arg.Any<IReadOnlyCollection<CustomPropertyOption>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenConcurrencyStampIsMissing_ReturnsValidationFailure()
    {
        var command = new UpdateCustomPropertyDefinitionCommand
        {
            DefinitionId = Guid.NewGuid(),
            DefinitionDto = CreateDto()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors.Any(error => error.Contains("ExpectedConcurrencyStamp", StringComparison.Ordinal))).IsTrue();
        await _customPropertyDefinitionRepository.DidNotReceive().GetTrackedDefinitionWithOptions(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }


    [Test]
    public async Task Handle_WhenDisplayNameChanges_UsesNamespaceAndKeyAsMachineIdentity()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = CreateExistingDefinition(tenantId);
        existing.Namespace = "tenant.community";
        existing.Key = "prayer_notes";
        existing.DisplayName = "Legacy Prayer Notes";
        var command = new UpdateCustomPropertyDefinitionCommand
        {
            DefinitionId = existing.Id,
            DefinitionDto = CreateDto("Renamed Prayer Notes")
        };

        _currentUserService.UserId.Returns(userId);
        _customPropertyDefinitionRepository.GetTrackedDefinitionWithOptions(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        _customPropertyGovernancePolicy.EvaluateDefinition(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new CustomPropertyGovernanceEvaluation
            {
                NormalizedNamespace = "tenant.community",
                NormalizedKey = "prayer_notes",
            });
        _customPropertyDefinitionRepository.ExistsScopedMachineKey(tenantId, EntityTypeName.Organization, "tenant.community", "prayer_notes", existing.Id)
            .Returns(false);
        _mapper.Map(command.DefinitionDto, existing).Returns(callInfo =>
        {
            existing.DisplayName = command.DefinitionDto.Metadata!.DisplayName!;
            return callInfo.ArgAt<CustomPropertyDefinition>(1);
        });
        _customPropertyDefinitionRepository.UpdateWithOptions(Arg.Any<CustomPropertyDefinition>(), Arg.Any<IReadOnlyCollection<CustomPropertyOption>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<CustomPropertyDefinition>());

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _customPropertyDefinitionRepository.Received(1).ExistsScopedMachineKey(
            tenantId,
            EntityTypeName.Organization,
            "tenant.community",
            "prayer_notes",
            existing.Id);
        await _customPropertyDefinitionRepository.Received(1).UpdateWithOptions(
            Arg.Is<CustomPropertyDefinition>(definition =>
                definition.Id == existing.Id
                && definition.Namespace == "tenant.community"
                && definition.Key == "prayer_notes"
                && definition.DisplayName == "Renamed Prayer Notes"
                && definition.UpdatedBy == userId),
            Arg.Any<IReadOnlyCollection<CustomPropertyOption>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithValidRequest_UpdatesDefinitionAndReplacesOptions()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = CreateExistingDefinition(tenantId);
        var command = new UpdateCustomPropertyDefinitionCommand
        {
            DefinitionId = existing.Id,
            DefinitionDto = CreateDto(),
            ExpectedConcurrencyStamp = ConcurrencyStamp
        };

        _currentUserService.UserId.Returns(userId);
        _customPropertyDefinitionRepository.GetTrackedDefinitionWithOptions(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        _customPropertyGovernancePolicy.EvaluateDefinition(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new CustomPropertyGovernanceEvaluation
            {
                NormalizedNamespace = "tenant.community",
                NormalizedKey = "prayer_notes",
            });
        _customPropertyDefinitionRepository.ExistsScopedMachineKey(tenantId, EntityTypeName.Organization, "tenant.community", "prayer_notes", existing.Id)
            .Returns(false);
        _customPropertyDefinitionRepository.UpdateWithOptions(Arg.Any<CustomPropertyDefinition>(), Arg.Any<IReadOnlyCollection<CustomPropertyOption>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<CustomPropertyDefinition>());

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _customPropertyDefinitionRepository.Received(1).UpdateWithOptions(
            Arg.Is<CustomPropertyDefinition>(definition =>
                definition.Id == existing.Id
                && definition.Namespace == "tenant.community"
                && definition.Key == "prayer_notes"
                && definition.UpdatedBy == userId),
            Arg.Is<IReadOnlyCollection<CustomPropertyOption>>(options =>
                options.Count == 2
                && options.Any(option => option.Key == "onsite" && option.IsDefault)
                && options.Any(option => option.Key == "stream")),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenOptionQuotaExceeded_ReturnsQuotaFailure()
    {
        var tenantId = Guid.NewGuid();
        var existing = CreateExistingDefinition(tenantId);
        var command = new UpdateCustomPropertyDefinitionCommand
        {
            DefinitionId = existing.Id,
            DefinitionDto = CreateDto(),
            ExpectedConcurrencyStamp = ConcurrencyStamp
        };

        _customPropertyDefinitionRepository.GetTrackedDefinitionWithOptions(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        _customPropertyGovernancePolicy.EvaluateDefinition(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new CustomPropertyGovernanceEvaluation
            {
                NormalizedNamespace = "tenant.community",
                NormalizedKey = "prayer_notes",
            });
        _customPropertyDefinitionRepository.ExistsScopedMachineKey(tenantId, EntityTypeName.Organization, "tenant.community", "prayer_notes", existing.Id)
            .Returns(false);
        _quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key, tenantId, Arg.Any<CancellationToken>())
            .Returns(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.QuotaExceeded);
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await Assert.That(result.QuotaExceeded!.QuotaKey).IsEqualTo(CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key);
        await Assert.That(result.QuotaExceeded.Limit).IsEqualTo(1);
        await Assert.That(result.QuotaExceeded.Actual).IsNull();
        await Assert.That(result.QuotaExceeded.Attempted).IsEqualTo(2);
        await Assert.That(result.QuotaExceeded.Scope).IsEqualTo("custom_property_definition_options");
        await Assert.That(result.QuotaExceeded.TenantId).IsEqualTo(tenantId);
        await Assert.That(result.Errors.Any(error => error.Contains(FailureCodes.QuotaExceeded, StringComparison.Ordinal))).IsTrue();
        await _customPropertyDefinitionRepository.DidNotReceive().UpdateWithOptions(
            Arg.Any<CustomPropertyDefinition>(),
            Arg.Any<IReadOnlyCollection<CustomPropertyOption>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    private static UpdateCustomPropertyDefinitionDto CreateDto(
        string displayName = "Prayer Notes")
    {
        return new UpdateCustomPropertyDefinitionDto
        {
            Metadata = new UpdateCustomPropertyDefinitionMetadataDto
            {
                Namespace = "Tenant Community", Key = "Prayer Notes", DisplayName = displayName,
                PropertyType = PropertyType.Option, ExposureLevel = ExposureLevel.OrganizerOnly, IsActive = true
            },
            Options = new UpdateCustomPropertyDefinitionOptionsDto { Items =
            [
                new CreateCustomPropertyOptionDto
                {
                    Namespace = "tenant.community",
                    Key = "onsite",
                    DisplayName = "Onsite",
                    Value = "onsite",
                    IsDefault = true,
                    IsActive = true,
                },
                new CreateCustomPropertyOptionDto
                {
                    Namespace = "tenant.community",
                    Key = "stream",
                    DisplayName = "Stream",
                    Value = "stream",
                    IsActive = true,
                }
            ] }
        };
    }

    private static CustomPropertyDefinition CreateExistingDefinition(Guid tenantId)
    {
        return new CustomPropertyDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Tenant = null,
            EntityTypeName = EntityTypeName.Organization,
            Namespace = "tenant.old",
            Key = "legacy",
            DisplayName = "Legacy",
            PropertyType = PropertyType.Option,
            ExposureLevel = ExposureLevel.Internal,
            ConcurrencyStamp = ConcurrencyStamp,
        };
    }

    private static readonly Guid ConcurrencyStamp = Guid.Parse("11111111-1111-1111-1111-111111111111");
}
