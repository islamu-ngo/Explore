// ABOUTME: Unit tests for the first shared custom-property definition command handler slice.
// ABOUTME: Verifies governance-policy failures, duplicate-key rejection, and successful option-aware persistence.

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

public class CreateCustomPropertyDefinitionCommandHandlerTests
{
    private readonly ICustomPropertyDefinitionRepository _customPropertyDefinitionRepository;
    private readonly ICustomPropertyGovernancePolicy _customPropertyGovernancePolicy;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateCustomPropertyDefinitionCommandHandler _handler;

    public CreateCustomPropertyDefinitionCommandHandlerTests()
    {
        _customPropertyDefinitionRepository = Substitute.For<ICustomPropertyDefinitionRepository>();
        _customPropertyGovernancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
        _tenantContext = Substitute.For<ITenantContext>();
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

        _handler = new CreateCustomPropertyDefinitionCommandHandler(
            _customPropertyDefinitionRepository,
            _customPropertyGovernancePolicy,
            _tenantContext,
            _currentUserService,
            _mapper,
            _cache,
            _unitOfWork);
    }

    [Test]
    public async Task Handle_WithGovernanceErrors_ReturnsFailure()
    {
        var tenantId = Guid.NewGuid();
        var command = new CreateCustomPropertyDefinitionCommand
        {
            DefinitionDto = CreateValidDto()
        };

        _tenantContext.TenantId.Returns(tenantId);
        _customPropertyGovernancePolicy.EvaluateDefinition(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new CustomPropertyGovernanceEvaluation
            {
                NormalizedNamespace = "sector.islamic",
                NormalizedKey = "madhab_id",
                Errors = ["Layer 3 custom properties cannot redefine reserved Layer 2 semantics."]
            });

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Layer 3 custom properties cannot redefine reserved Layer 2 semantics.");
        await _customPropertyDefinitionRepository.DidNotReceiveWithAnyArgs().CreateWithOptions(default!, default!, default, default);
    }

    [Test]
    public async Task Handle_WithDuplicateScopedMachineKey_ReturnsFailure()
    {
        var tenantId = Guid.NewGuid();
        var command = new CreateCustomPropertyDefinitionCommand
        {
            DefinitionDto = CreateValidDto()
        };

        _tenantContext.TenantId.Returns(tenantId);
        _customPropertyGovernancePolicy.EvaluateDefinition(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new CustomPropertyGovernanceEvaluation
            {
                NormalizedNamespace = "tenant.community",
                NormalizedKey = "prayer_notes",
            });
        _customPropertyDefinitionRepository.ExistsScopedMachineKey(tenantId, EntityTypeName.Organization, "tenant.community", "prayer_notes")
            .Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors.Any(e => e.Contains("same Namespace + Key", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task Handle_WithValidOptionProperty_CreatesDefinitionAndOptions()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdId = Guid.NewGuid();
        var command = new CreateCustomPropertyDefinitionCommand
        {
            DefinitionDto = CreateValidDto()
        };

        var mappedEntity = new CustomPropertyDefinition
        {
            Id = createdId,
            Tenant = null,
            Namespace = command.DefinitionDto.Namespace,
            Key = command.DefinitionDto.Key,
            DisplayName = command.DefinitionDto.DisplayName,
            Description = command.DefinitionDto.Description,
            EntityTypeName = command.DefinitionDto.EntityTypeName,
            PropertyType = command.DefinitionDto.PropertyType,
            ExposureLevel = command.DefinitionDto.ExposureLevel,
        };

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _mapper.Map<CustomPropertyDefinition>(command.DefinitionDto).Returns(mappedEntity);
        _customPropertyGovernancePolicy.EvaluateDefinition(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new CustomPropertyGovernanceEvaluation
            {
                NormalizedNamespace = "tenant.community",
                NormalizedKey = "prayer_notes",
            });
        _customPropertyDefinitionRepository.ExistsScopedMachineKey(tenantId, EntityTypeName.Organization, "tenant.community", "prayer_notes")
            .Returns(false);
        _customPropertyDefinitionRepository.CreateWithOptions(Arg.Any<CustomPropertyDefinition>(), Arg.Any<IReadOnlyCollection<CustomPropertyOption>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<CustomPropertyDefinition>());

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(createdId);
        await _customPropertyDefinitionRepository.Received(1).CreateWithOptions(
            Arg.Is<CustomPropertyDefinition>(definition =>
                definition.TenantId == tenantId
                && definition.Namespace == "tenant.community"
                && definition.Key == "prayer_notes"
                && definition.CreatedBy == userId),
            Arg.Is<IReadOnlyCollection<CustomPropertyOption>>(options =>
                options.Count == 2
                && options.Any(option => option.Namespace == "tenant.community" && option.Key == "onsite" && option.IsDefault)),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    private static CreateCustomPropertyDefinitionDto CreateValidDto()
    {
        return new CreateCustomPropertyDefinitionDto
        {
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
}
