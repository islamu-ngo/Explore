// ABOUTME: Unit tests for explicit audited custom-property purge command behavior.
// ABOUTME: Verifies dependency blocking and audit creation without touching normal soft-delete semantics.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.CustomPropertyDefinitions.Handlers.Commands;
using Explore.Application.Features.CustomPropertyDefinitions.Requests.Commands;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.CustomPropertyDefinitions.Commands;

public sealed class PurgeCustomPropertyDefinitionCommandHandlerTests
{
    private readonly ICustomPropertyDefinitionRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly HybridCache _cache;
    private readonly PurgeCustomPropertyDefinitionCommandHandler _handler;

    public PurgeCustomPropertyDefinitionCommandHandlerTests()
    {
        _repository = Substitute.For<ICustomPropertyDefinitionRepository>();
        _auditLogRepository = Substitute.For<IAuditLogRepository>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _cache = Substitute.For<HybridCache>();

        _handler = new PurgeCustomPropertyDefinitionCommandHandler(
            _repository,
            _auditLogRepository,
            _currentUserService,
            _unitOfWork,
            _cache);
    }

    [Test]
    public async Task Handle_WhenValuesExist_BlocksPurgeAndDoesNotAudit()
    {
        var definitionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new PurgeCustomPropertyDefinitionCommand
        {
            Id = definitionId,
            Reason = "operator cleanup"
        };
        _repository.GetPurgeDependencies(definitionId, Arg.Any<CancellationToken>())
            .Returns(new CustomPropertyPurgeDependencySummary(
                definitionId,
                tenantId,
                "custom_property_definition",
                OptionCount: 2,
                ValueCount: 1,
                ProjectionCount: 0,
                AuditLogCount: 0,
                SyncProvenanceCount: 0));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Id).IsNotNull();
        await Assert.That(result.Id!.Purged).IsFalse();
        await Assert.That(result.Id.ValueCount).IsEqualTo(1);
        await Assert.That(result.Errors).Contains(error => error.Contains("historical custom-property value", StringComparison.Ordinal));
        await _repository.DidNotReceiveWithAnyArgs().PurgeDefinition(default, default);
        await _auditLogRepository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Test]
    public async Task Handle_WhenDependencyFree_PurgesAndWritesAudit()
    {
        var definitionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var command = new PurgeCustomPropertyDefinitionCommand
        {
            Id = definitionId,
            Reason = "operator cleanup"
        };
        _currentUserService.UserId.Returns(actorId);
        _repository.GetPurgeDependencies(definitionId, Arg.Any<CancellationToken>())
            .Returns(new CustomPropertyPurgeDependencySummary(
                definitionId,
                tenantId,
                "custom_property_definition",
                OptionCount: 2,
                ValueCount: 0,
                ProjectionCount: 0,
                AuditLogCount: 0,
                SyncProvenanceCount: 0));
        _repository.PurgeDefinition(definitionId, Arg.Any<CancellationToken>()).Returns(true);
        _auditLogRepository.Create(Arg.Any<AuditLog>()).Returns(call => call.Arg<AuditLog>());
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<bool>>>().Invoke(CancellationToken.None));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsNotNull();
        await Assert.That(result.Id!.Purged).IsTrue();
        await Assert.That(result.Id.AuditLogId).IsNotNull();
        await _repository.Received(1).PurgeDefinition(definitionId, Arg.Any<CancellationToken>());
        await _auditLogRepository.Received(1).Create(Arg.Is<AuditLog>(audit =>
            audit.TenantId == tenantId
            && audit.ActorId == actorId
            && audit.EntityType == "custom_property_definition"
            && audit.EntityId == definitionId.ToString()
            && audit.Action == "CustomPropertyDefinitionPurged"));
    }

    [Test]
    public async Task Handle_WhenPurgePreflightBecomesStale_ReturnsBlockedResponseAndDoesNotAudit()
    {
        var definitionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new PurgeCustomPropertyDefinitionCommand
        {
            Id = definitionId,
            Reason = "operator cleanup"
        };
        var cleanSummary = new CustomPropertyPurgeDependencySummary(
            definitionId,
            tenantId,
            "custom_property_definition",
            OptionCount: 1,
            ValueCount: 0,
            ProjectionCount: 0,
            AuditLogCount: 0,
            SyncProvenanceCount: 0);
        var blockedSummary = cleanSummary with { ValueCount = 1 };

        _repository.GetPurgeDependencies(definitionId, Arg.Any<CancellationToken>())
            .Returns(cleanSummary, blockedSummary);
        _repository.PurgeDefinition(definitionId, Arg.Any<CancellationToken>()).Returns(false);
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<bool>>>().Invoke(CancellationToken.None));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Custom-property definition purge blocked.");
        await Assert.That(result.Id).IsNotNull();
        await Assert.That(result.Id!.Purged).IsFalse();
        await Assert.That(result.Id.ValueCount).IsEqualTo(1);
        await Assert.That(result.Errors).Contains(error => error.Contains("historical custom-property value", StringComparison.Ordinal));
        await _auditLogRepository.DidNotReceiveWithAnyArgs().Create(default!);
    }
}
