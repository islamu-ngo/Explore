// ABOUTME: Unit tests for shared custom-property definition updates.
// ABOUTME: Verifies not-found handling, duplicate rejection, and option replacement semantics through the repository contract.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.Features.CustomPropertyDefinitions.Handlers.Commands;
using Explore.Application.Features.CustomPropertyDefinitions.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.CustomPropertyDefinitions.Commands;

public class UpdateCustomPropertyDefinitionCommandHandlerTests
{
    private readonly ICustomPropertyDefinitionRepository _customPropertyDefinitionRepository;
    private readonly ICustomPropertyGovernancePolicy _customPropertyGovernancePolicy;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpdateCustomPropertyDefinitionCommandHandler _handler;

    public UpdateCustomPropertyDefinitionCommandHandlerTests()
    {
        _customPropertyDefinitionRepository = Substitute.For<ICustomPropertyDefinitionRepository>();
        _customPropertyGovernancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
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
            _currentUserService,
            _mapper,
            _cache,
            _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenDefinitionNotFound_ReturnsFailure()
    {
        var command = new UpdateCustomPropertyDefinitionCommand
        {
            DefinitionDto = CreateDto()
        };

        _customPropertyDefinitionRepository.GetTrackedDefinitionWithOptions(command.DefinitionDto.Id, Arg.Any<CancellationToken>())
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
            DefinitionDto = CreateDto(existing.Id)
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
    public async Task Handle_WithValidRequest_UpdatesDefinitionAndReplacesOptions()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = CreateExistingDefinition(tenantId);
        var command = new UpdateCustomPropertyDefinitionCommand
        {
            DefinitionDto = CreateDto(existing.Id)
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

    private static UpdateCustomPropertyDefinitionDto CreateDto(Guid? id = null)
    {
        return new UpdateCustomPropertyDefinitionDto
        {
            Id = id ?? Guid.NewGuid(),
            EntityTypeName = EntityTypeName.Organization,
            Namespace = "Tenant Community",
            Key = "Prayer Notes",
            DisplayName = "Prayer Notes",
            PropertyType = PropertyType.Option,
            ExposureLevel = ExposureLevel.OrganizerOnly,
            IsActive = true,
            Options =
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
            ]
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
        };
    }
}
