// ABOUTME: Unit tests for control-plane tenant plan CQRS read and draft lifecycle handlers.
// ABOUTME: Pins SaaS tier DTO mapping and draft creation before API or UI work begins.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ControlPlane;
using Explore.Application.Features.ControlPlane.Handlers.Commands;
using Explore.Application.Features.ControlPlane.Handlers.Queries;
using Explore.Application.Features.ControlPlane.Plans;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Features.ControlPlane.Requests.Queries;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Settings;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.ControlPlane.Plans;

public sealed class TenantPlanCqrsHandlerTests
{
    private readonly IHierarchicalSettingsResolver _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    [Test]
    public async Task ListPlans_WhenPlansExist_ReturnsLatestVersionSummaries()
    {
        var repository = Substitute.For<ITenantPlanRepository>();
        TenantPlan plan = CreatePlan("community", "Community");
        plan.Versions.Add(CreateVersion(plan, versionNumber: 1, TenantPlanStatusEnum.Draft, 19m, false));
        plan.Versions.Add(CreateVersion(plan, versionNumber: 2, TenantPlanStatusEnum.Published, 29m, true));
        repository.ListWithVersionsAsync(Arg.Any<CancellationToken>()).Returns([plan]);
        var handler = new GetControlPlaneTenantPlanListQueryHandler(repository);

        IReadOnlyList<ControlPlaneTenantPlanListItemDto> result = await handler.Handle(
            new GetControlPlaneTenantPlanListQuery(),
            CancellationToken.None);

        ControlPlaneTenantPlanListItemDto item = result.Single();
        await Assert.That(item.Key).IsEqualTo("community");
        await Assert.That(item.DisplayName).IsEqualTo("Community");
        await Assert.That(item.LatestVersionNumber).IsEqualTo(2);
        await Assert.That(item.PublishedVersionNumber).IsEqualTo(2);
        await Assert.That(item.PriceAmount).IsEqualTo(29m);
        await Assert.That(item.CurrencyCode).IsEqualTo("EUR");
        await Assert.That(item.BillingPeriod).IsEqualTo(TenantPlanBillingPeriods.Monthly);
        await Assert.That(item.IsActiveForProvisioning).IsTrue();
    }

    [Test]
    public async Task Detail_WhenPlanExists_ReturnsVersionSettingsAndQuotas()
    {
        var repository = Substitute.For<ITenantPlanRepository>();
        TenantPlan plan = CreatePlan("enterprise", "Enterprise");
        TenantPlanVersion version = CreateVersion(plan, versionNumber: 3, TenantPlanStatusEnum.Published, 199m, true);
        version.Settings.Add(new TenantPlanVersionSetting
        {
            Id = Guid.NewGuid(),
            SettingKey = GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes,
            JsonValue = "10737418240",
            IsLocked = true
        });
        version.Quotas.Add(new TenantPlanVersionQuota
        {
            Id = Guid.NewGuid(),
            QuotaKey = TenantPlanQuotaKeys.StorageBytes,
            Limit = 10L * 1024 * 1024 * 1024
        });
        plan.Versions.Add(version);
        repository.GetByKeyAsync("enterprise", Arg.Any<CancellationToken>()).Returns(plan);
        var handler = new GetControlPlaneTenantPlanDetailQueryHandler(repository);

        ControlPlaneTenantPlanDetailDto? result = await handler.Handle(
            new GetControlPlaneTenantPlanDetailQuery("enterprise"),
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Key).IsEqualTo("enterprise");
        ControlPlaneTenantPlanVersionDto mappedVersion = result.Versions.Single();
        await Assert.That(mappedVersion.VersionNumber).IsEqualTo(3);
        await Assert.That(mappedVersion.Settings.Single().Key).IsEqualTo(GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes);
        await Assert.That(mappedVersion.Quotas.Single().Key).IsEqualTo(TenantPlanQuotaKeys.StorageBytes);
    }

    [Test]
    public async Task CreateDraft_WhenDraftIsValid_CreatesDraftVersionWithSettingsAndQuotas()
    {
        var repository = Substitute.For<ITenantPlanRepository>();
        TenantPlan? captured = null;
        repository.GetByKeyAsync("community", Arg.Any<CancellationToken>()).Returns((TenantPlan?)null);
        repository.Create(Arg.Do<TenantPlan>(plan => captured = plan))
            .Returns(call => Task.FromResult(call.Arg<TenantPlan>()));
        var handler = new CreateControlPlaneTenantPlanDraftCommandHandler(repository);

        var result = await handler.Handle(
            new CreateControlPlaneTenantPlanDraftCommand(CreateValidDraft()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await repository.Received(1).Create(Arg.Any<TenantPlan>());
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Key).IsEqualTo("community");
        TenantPlanVersion version = captured.Versions.Single();
        await Assert.That(version.TenantPlanStatusId).IsEqualTo((int)TenantPlanStatusEnum.Draft);
        await Assert.That(version.PriceAmount).IsEqualTo(29m);
        await Assert.That(version.Settings.Count).IsEqualTo(1);
        await Assert.That(version.Quotas.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CreateDraft_WhenDraftContainsSensitiveSetting_ReturnsFailureAndDoesNotPersist()
    {
        var repository = Substitute.For<ITenantPlanRepository>();
        var draft = CreateValidDraft() with
        {
            SettingOverrides = [new TenantPlanSettingOverride("email.smtp_password", "\"secret\"", IsLocked: true)]
        };
        var handler = new CreateControlPlaneTenantPlanDraftCommandHandler(repository);

        var result = await handler.Handle(
            new CreateControlPlaneTenantPlanDraftCommand(draft),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors ?? []).Contains(TenantPlanValidationErrorCodes.SensitiveSettingKey);
        await repository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Test]
    public async Task Assignment_WhenTenantHasActiveAssignment_ReturnsPlanVersionSummary()
    {
        var tenantId = Guid.NewGuid();
        var repository = Substitute.For<ITenantPlanRepository>();
        TenantPlan plan = CreatePlan("enterprise", "Enterprise");
        TenantPlanVersion version = CreateVersion(plan, versionNumber: 4, TenantPlanStatusEnum.Published, 199m, true);
        var assignment = new TenantPlanAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TenantPlan = plan,
            TenantPlanVersion = version,
            TenantPlanAssignmentStatusId = (int)TenantPlanAssignmentStatusEnum.Active,
            TenantPlanAssignmentStatus = new TenantPlanAssignmentStatus
            {
                Id = (int)TenantPlanAssignmentStatusEnum.Active,
                MasterCode = "ACTIVE",
                FullName = "Active",
                IsActiveAssignment = true
            },
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = Guid.NewGuid()
        };
        repository.GetActiveAssignmentForTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns(assignment);
        var handler = new GetControlPlaneTenantPlanAssignmentQueryHandler(repository);

        ControlPlaneTenantPlanAssignmentDto? result = await handler.Handle(
            new GetControlPlaneTenantPlanAssignmentQuery(tenantId),
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TenantId).IsEqualTo(tenantId);
        await Assert.That(result.PlanKey).IsEqualTo("enterprise");
        await Assert.That(result.VersionNumber).IsEqualTo(4);
        await Assert.That(result.StatusCode).IsEqualTo("ACTIVE");
    }

    [Test]
    public async Task CreateVersionDraft_WhenPlanExists_AddsNextDraftVersionWithoutMovingAssignments()
    {
        var repository = Substitute.For<ITenantPlanRepository>();
        TenantPlan plan = CreatePlan("community", "Community");
        TenantPlanVersion currentVersion = CreateVersion(plan, versionNumber: 1, TenantPlanStatusEnum.Published, 29m, true);
        plan.Versions.Add(currentVersion);
        var assignment = new TenantPlanAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            TenantPlan = plan,
            TenantPlanId = plan.Id,
            TenantPlanVersion = currentVersion,
            TenantPlanVersionId = currentVersion.Id,
            TenantPlanAssignmentStatusId = (int)TenantPlanAssignmentStatusEnum.Active,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = Guid.NewGuid()
        };
        repository.GetByKeyAsync("community", Arg.Any<CancellationToken>()).Returns(plan);
        var handler = new CreateControlPlaneTenantPlanVersionDraftCommandHandler(repository);

        var result = await handler.Handle(
            new CreateControlPlaneTenantPlanVersionDraftCommand("community", CreateValidDraft() with
            {
                Pricing = new TenantPlanPricing(39m, "EUR", TenantPlanBillingPeriods.Monthly)
            }),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        TenantPlanVersion draft = plan.Versions.Single(version => version.VersionNumber == 2);
        await Assert.That(draft.TenantPlanStatusId).IsEqualTo((int)TenantPlanStatusEnum.Draft);
        await Assert.That(draft.PriceAmount).IsEqualTo(39m);
        await Assert.That(assignment.TenantPlanVersionId).IsEqualTo(currentVersion.Id);
        await repository.Received(1).CreateVersionAsync(draft, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishVersion_WhenExistingTenantsRemainPinned_DoesNotMoveAssignments()
    {
        var repository = Substitute.For<ITenantPlanRepository>();
        TenantPlan plan = CreatePlan("community", "Community");
        TenantPlanVersion currentVersion = CreateVersion(plan, versionNumber: 1, TenantPlanStatusEnum.Published, 29m, true);
        TenantPlanVersion draftVersion = CreateVersion(plan, versionNumber: 2, TenantPlanStatusEnum.Draft, 39m, true);
        var assignment = new TenantPlanAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            TenantPlan = plan,
            TenantPlanId = plan.Id,
            TenantPlanVersion = currentVersion,
            TenantPlanVersionId = currentVersion.Id,
            TenantPlanAssignmentStatusId = (int)TenantPlanAssignmentStatusEnum.Active,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = Guid.NewGuid()
        };
        repository.GetVersionAsync(draftVersion.Id, Arg.Any<CancellationToken>()).Returns(draftVersion);
        var unitOfWork = new ImmediateUnitOfWork();
        var handler = new PublishControlPlaneTenantPlanVersionCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new PublishControlPlaneTenantPlanVersionCommand(
                draftVersion.Id,
                TenantPlanExistingAssignmentPolicy.LeaveExistingTenantsPinned),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(draftVersion.TenantPlanStatusId).IsEqualTo((int)TenantPlanStatusEnum.Published);
        await Assert.That(assignment.TenantPlanVersionId).IsEqualTo(currentVersion.Id);
        await repository.DidNotReceive().ListActiveAssignmentsForPlanAsync(plan.Id, Arg.Any<CancellationToken>());
        await repository.Received(1).UpdateVersionAsync(draftVersion, Arg.Any<CancellationToken>());
        await Assert.That(unitOfWork.ExecutionCount).IsEqualTo(1);
    }

    [Test]
    public async Task PublishVersion_WhenExistingTenantsAreMoved_UpdatesActiveAssignments()
    {
        var repository = Substitute.For<ITenantPlanRepository>();
        TenantPlan plan = CreatePlan("community", "Community");
        TenantPlanVersion oldVersion = CreateVersion(plan, versionNumber: 1, TenantPlanStatusEnum.Published, 29m, true);
        TenantPlanVersion newVersion = CreateVersion(plan, versionNumber: 2, TenantPlanStatusEnum.Draft, 39m, true);
        var assignment = new TenantPlanAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            TenantPlan = plan,
            TenantPlanId = plan.Id,
            TenantPlanVersion = oldVersion,
            TenantPlanVersionId = oldVersion.Id,
            TenantPlanAssignmentStatusId = (int)TenantPlanAssignmentStatusEnum.Active,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = Guid.NewGuid()
        };
        repository.GetVersionAsync(newVersion.Id, Arg.Any<CancellationToken>()).Returns(newVersion);
        repository.ListActiveAssignmentsForPlanAsync(plan.Id, Arg.Any<CancellationToken>()).Returns([assignment]);
        var unitOfWork = new ImmediateUnitOfWork();
        var handler = new PublishControlPlaneTenantPlanVersionCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new PublishControlPlaneTenantPlanVersionCommand(
                newVersion.Id,
                TenantPlanExistingAssignmentPolicy.MoveExistingTenantsToPublishedVersion),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(newVersion.TenantPlanStatusId).IsEqualTo((int)TenantPlanStatusEnum.Published);
        await Assert.That(assignment.TenantPlanVersionId).IsEqualTo(newVersion.Id);
        await Assert.That(assignment.TenantPlanVersion).IsEqualTo(newVersion);
        await repository.Received(1).UpdateAssignmentAsync(assignment, Arg.Any<CancellationToken>());
        await Assert.That(unitOfWork.ExecutionCount).IsEqualTo(1);
    }

    [Test]
    public async Task SwitchTenantPlan_WhenTenantHasActiveAssignment_SupersedesOldAndCreatesNewActiveAssignment()
    {
        var tenantId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var repository = Substitute.For<ITenantPlanRepository>();
        TenantPlan oldPlan = CreatePlan("community", "Community");
        TenantPlanVersion oldVersion = CreateVersion(oldPlan, versionNumber: 1, TenantPlanStatusEnum.Published, 29m, true);
        TenantPlan newPlan = CreatePlan("enterprise", "Enterprise");
        TenantPlanVersion newVersion = CreateVersion(newPlan, versionNumber: 1, TenantPlanStatusEnum.Published, 199m, true);
        var oldAssignment = new TenantPlanAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TenantPlan = oldPlan,
            TenantPlanId = oldPlan.Id,
            TenantPlanVersion = oldVersion,
            TenantPlanVersionId = oldVersion.Id,
            TenantPlanAssignmentStatusId = (int)TenantPlanAssignmentStatusEnum.Active,
            AssignedAt = DateTime.UtcNow.AddDays(-10),
            AssignedByUserId = Guid.NewGuid()
        };
        TenantPlanAssignment? createdAssignment = null;
        repository.GetVersionAsync(newVersion.Id, Arg.Any<CancellationToken>()).Returns(newVersion);
        repository.GetActiveAssignmentForTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns(oldAssignment);
        repository.CreateAssignmentAsync(Arg.Do<TenantPlanAssignment>(assignment => createdAssignment = assignment), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<TenantPlanAssignment>()));
        var unitOfWork = new ImmediateUnitOfWork();
        var handler = new SwitchControlPlaneTenantPlanAssignmentCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new SwitchControlPlaneTenantPlanAssignmentCommand(tenantId, newVersion.Id, operatorId),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(oldAssignment.TenantPlanAssignmentStatusId).IsEqualTo((int)TenantPlanAssignmentStatusEnum.Superseded);
        await Assert.That(oldAssignment.EndedAt).IsNotNull();
        await Assert.That(createdAssignment).IsNotNull();
        await Assert.That(createdAssignment!.TenantId).IsEqualTo(tenantId);
        await Assert.That(createdAssignment.TenantPlanId).IsEqualTo(newPlan.Id);
        await Assert.That(createdAssignment.TenantPlanVersionId).IsEqualTo(newVersion.Id);
        await Assert.That(createdAssignment.TenantPlanAssignmentStatusId).IsEqualTo((int)TenantPlanAssignmentStatusEnum.Active);
        await Assert.That(createdAssignment.AssignedByUserId).IsEqualTo(operatorId);
        await repository.Received(1).UpdateAssignmentAsync(oldAssignment, Arg.Any<CancellationToken>());
        await Assert.That(unitOfWork.ExecutionCount).IsEqualTo(1);
    }

    [Test]
    public async Task UpdateDraft_WhenVersionIsDraft_ReplacesPricingSettingsAndQuotas()
    {
        var repository = Substitute.For<ITenantPlanRepository>();
        TenantPlan plan = CreatePlan("community", "Community");
        TenantPlanVersion draft = CreateVersion(plan, versionNumber: 2, TenantPlanStatusEnum.Draft, 29m, true);
        draft.Settings.Add(new TenantPlanVersionSetting
        {
            Id = Guid.NewGuid(),
            SettingKey = GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes,
            JsonValue = "1073741824",
            IsLocked = false
        });
        draft.Quotas.Add(new TenantPlanVersionQuota
        {
            Id = Guid.NewGuid(),
            QuotaKey = TenantPlanQuotaKeys.StorageBytes,
            Limit = 1024
        });
        repository.GetVersionForUpdateAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);
        var handler = new UpdateControlPlaneTenantPlanVersionDraftCommandHandler(repository);

        var result = await handler.Handle(
            new UpdateControlPlaneTenantPlanVersionDraftCommand(draft.Id, new PatchControlPlaneTenantPlanVersionDraftDto
            {
                Pricing = new PatchTenantPlanPricingDto
                {
                    Amount = 49m,
                    CurrencyCode = "EUR",
                    BillingPeriod = TenantPlanBillingPeriods.Monthly
                },
                SettingOverrides = new PatchTenantPlanSettingOverridesDto
                {
                    Values = CreateValidDraft().SettingOverrides
                },
                QuotaLimits = new PatchTenantPlanQuotaLimitsDto
                {
                    Values = CreateValidDraft().QuotaLimits
                }
            }),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(draft.PriceAmount).IsEqualTo(49m);
        await Assert.That(draft.Settings.Single().SettingKey).IsEqualTo(GovernanceSettingKeys.AiAssistant.Enabled);
        await Assert.That(draft.Quotas.Single().QuotaKey).IsEqualTo(TenantPlanQuotaKeys.AiDailyTenantMessages);
        await repository.Received(1).UpdateVersionAsync(draft, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateDraft_WhenContentGroupsAreOmitted_PreservesSettingsAndQuotas()
    {
        var repository = Substitute.For<ITenantPlanRepository>();
        TenantPlan plan = CreatePlan("community", "Community");
        TenantPlanVersion draft = CreateVersion(plan, versionNumber: 2, TenantPlanStatusEnum.Draft, 29m, true);
        var existingSetting = new TenantPlanVersionSetting
        {
            Id = Guid.NewGuid(),
            SettingKey = GovernanceSettingKeys.AiAssistant.Enabled,
            JsonValue = "true",
            IsLocked = false
        };
        var existingQuota = new TenantPlanVersionQuota
        {
            Id = Guid.NewGuid(),
            QuotaKey = TenantPlanQuotaKeys.AiDailyTenantMessages,
            Limit = 1000
        };
        draft.Settings.Add(existingSetting);
        draft.Quotas.Add(existingQuota);
        repository.GetVersionForUpdateAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);
        var handler = new UpdateControlPlaneTenantPlanVersionDraftCommandHandler(repository);

        var result = await handler.Handle(
            new UpdateControlPlaneTenantPlanVersionDraftCommand(
                draft.Id,
                new PatchControlPlaneTenantPlanVersionDraftDto
                {
                    Pricing = new PatchTenantPlanPricingDto
                    {
                        Amount = 49m,
                        CurrencyCode = "EUR",
                        BillingPeriod = TenantPlanBillingPeriods.Monthly
                    }
                }),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(draft.PriceAmount).IsEqualTo(49m);
        await Assert.That(draft.Settings.Single()).IsEqualTo(existingSetting);
        await Assert.That(draft.Quotas.Single()).IsEqualTo(existingQuota);
        await repository.Received(1).UpdateVersionAsync(draft, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ArchiveVersion_WhenVersionExists_MarksArchivedAndStopsProvisioning()
    {
        var repository = Substitute.For<ITenantPlanRepository>();
        TenantPlan plan = CreatePlan("community", "Community");
        TenantPlanVersion version = CreateVersion(plan, versionNumber: 2, TenantPlanStatusEnum.Published, 39m, true);
        repository.GetVersionAsync(version.Id, Arg.Any<CancellationToken>()).Returns(version);
        var handler = new ArchiveControlPlaneTenantPlanVersionCommandHandler(repository);

        var result = await handler.Handle(
            new ArchiveControlPlaneTenantPlanVersionCommand(version.Id),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(version.TenantPlanStatusId).IsEqualTo((int)TenantPlanStatusEnum.Archived);
        await Assert.That(version.IsActiveForProvisioning).IsFalse();
        await repository.Received(1).UpdateVersionAsync(version, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ClonePlan_WhenSourceVersionExists_CreatesDraftPlanWithCopiedContent()
    {
        var repository = Substitute.For<ITenantPlanRepository>();
        TenantPlan sourcePlan = CreatePlan("community", "Community");
        TenantPlanVersion sourceVersion = CreateVersion(sourcePlan, versionNumber: 2, TenantPlanStatusEnum.Published, 39m, true);
        sourceVersion.Settings.Add(new TenantPlanVersionSetting
        {
            Id = Guid.NewGuid(),
            SettingKey = GovernanceSettingKeys.AiAssistant.Enabled,
            JsonValue = "true",
            IsLocked = false
        });
        sourceVersion.Quotas.Add(new TenantPlanVersionQuota
        {
            Id = Guid.NewGuid(),
            QuotaKey = TenantPlanQuotaKeys.AiDailyTenantMessages,
            Limit = 1000
        });
        TenantPlan? captured = null;
        repository.GetVersionAsync(sourceVersion.Id, Arg.Any<CancellationToken>()).Returns(sourceVersion);
        repository.GetByKeyAsync("enterprise", Arg.Any<CancellationToken>()).Returns((TenantPlan?)null);
        repository.Create(Arg.Do<TenantPlan>(plan => captured = plan))
            .Returns(call => Task.FromResult(call.Arg<TenantPlan>()));
        var handler = new CloneControlPlaneTenantPlanCommandHandler(repository);

        var result = await handler.Handle(
            new CloneControlPlaneTenantPlanCommand(sourceVersion.Id, "enterprise", "Enterprise"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Key).IsEqualTo("enterprise");
        await Assert.That(captured.DisplayName).IsEqualTo("Enterprise");
        TenantPlanVersion clonedVersion = captured.Versions.Single();
        await Assert.That(clonedVersion.TenantPlanStatusId).IsEqualTo((int)TenantPlanStatusEnum.Draft);
        await Assert.That(clonedVersion.PriceAmount).IsEqualTo(39m);
        await Assert.That(clonedVersion.Settings.Single().SettingKey).IsEqualTo(GovernanceSettingKeys.AiAssistant.Enabled);
        await Assert.That(clonedVersion.Quotas.Single().QuotaKey).IsEqualTo(TenantPlanQuotaKeys.AiDailyTenantMessages);
    }

    [Test]
    public async Task ValidateDraft_ReturnsValidatorErrorsWithoutPersistence()
    {
        var handler = new ValidateControlPlaneTenantPlanDraftQueryHandler();

        TenantPlanValidationResult result = await handler.Handle(
            new ValidateControlPlaneTenantPlanDraftQuery(CreateValidDraft() with
            {
                SettingOverrides = [new TenantPlanSettingOverride("email.smtp_password", "\"secret\"", IsLocked: true)]
            }),
            CancellationToken.None);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.Code)).Contains(TenantPlanValidationErrorCodes.SensitiveSettingKey);
    }

    [Test]
    public async Task PreviewDiff_ReturnsSettingChangesWithoutPersistence()
    {
        var handler = new PreviewControlPlaneTenantPlanDiffQueryHandler();
        var current = new TenantPlanEffectiveConfiguration(
        [
            new TenantPlanEffectiveSetting(GovernanceSettingKeys.AiAssistant.Enabled, "false", IsLocked: false)
        ]);

        TenantPlanDiffResult result = await handler.Handle(
            new PreviewControlPlaneTenantPlanDiffQuery(current, CreateValidDraft()),
            CancellationToken.None);

        TenantPlanSettingChange change = result.SettingChanges.Single();
        await Assert.That(change.Key).IsEqualTo(GovernanceSettingKeys.AiAssistant.Enabled);
        await Assert.That(change.ChangeType).IsEqualTo(TenantPlanChangeType.Changed);
        await Assert.That(change.BeforeValue).IsEqualTo("false");
        await Assert.That(change.AfterValue).IsEqualTo("true");
    }

    [Test]
    public async Task RollbackAssignment_WhenPreviousAssignmentExists_ReactivatesPreviousAndRollsBackCurrent()
    {
        var tenantId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var repository = Substitute.For<ITenantPlanRepository>();
        TenantPlan oldPlan = CreatePlan("community", "Community");
        TenantPlanVersion oldVersion = CreateVersion(oldPlan, versionNumber: 1, TenantPlanStatusEnum.Published, 29m, true);
        TenantPlan newPlan = CreatePlan("enterprise", "Enterprise");
        TenantPlanVersion newVersion = CreateVersion(newPlan, versionNumber: 1, TenantPlanStatusEnum.Published, 199m, true);
        var previousAssignment = new TenantPlanAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TenantPlan = oldPlan,
            TenantPlanId = oldPlan.Id,
            TenantPlanVersion = oldVersion,
            TenantPlanVersionId = oldVersion.Id,
            TenantPlanAssignmentStatusId = (int)TenantPlanAssignmentStatusEnum.Superseded,
            AssignedAt = DateTime.UtcNow.AddDays(-20),
            EndedAt = DateTime.UtcNow.AddDays(-5),
            AssignedByUserId = Guid.NewGuid()
        };
        var currentAssignment = new TenantPlanAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TenantPlan = newPlan,
            TenantPlanId = newPlan.Id,
            TenantPlanVersion = newVersion,
            TenantPlanVersionId = newVersion.Id,
            TenantPlanAssignmentStatusId = (int)TenantPlanAssignmentStatusEnum.Active,
            AssignedAt = DateTime.UtcNow.AddDays(-5),
            AssignedByUserId = operatorId
        };
        repository.GetActiveAssignmentForTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns(currentAssignment);
        repository.GetPreviousEligibleAssignmentForTenantAsync(
                tenantId,
                currentAssignment.Id,
                Arg.Any<CancellationToken>())
            .Returns(previousAssignment);
        var unitOfWork = new ImmediateUnitOfWork();
        var handler = new RollbackControlPlaneTenantPlanAssignmentCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new RollbackControlPlaneTenantPlanAssignmentCommand(tenantId, previousAssignment.Id, operatorId),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(currentAssignment.TenantPlanAssignmentStatusId).IsEqualTo((int)TenantPlanAssignmentStatusEnum.RolledBack);
        await Assert.That(currentAssignment.EndedAt).IsNotNull();
        await Assert.That(previousAssignment.TenantPlanAssignmentStatusId).IsEqualTo((int)TenantPlanAssignmentStatusEnum.Active);
        await Assert.That(previousAssignment.EndedAt).IsNull();
        await repository.Received(1).UpdateAssignmentAsync(currentAssignment, Arg.Any<CancellationToken>());
        await repository.Received(1).UpdateAssignmentAsync(previousAssignment, Arg.Any<CancellationToken>());
        await Assert.That(unitOfWork.ExecutionCount).IsEqualTo(1);
    }

    [Test]
    public async Task RollbackAssignment_WhenNoEligiblePreviousAssignmentExists_FailsClosed()
    {
        Guid tenantId = Guid.NewGuid();
        TenantPlanAssignment currentAssignment = CreateActiveAssignment(tenantId, Guid.NewGuid());
        var repository = Substitute.For<ITenantPlanRepository>();
        repository.GetActiveAssignmentForTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(currentAssignment);
        repository.GetPreviousEligibleAssignmentForTenantAsync(
                tenantId,
                currentAssignment.Id,
                Arg.Any<CancellationToken>())
            .Returns((TenantPlanAssignment?)null);
        var unitOfWork = new ImmediateUnitOfWork();
        var handler = new RollbackControlPlaneTenantPlanAssignmentCommandHandler(repository, unitOfWork);

        BaseCommandResponse<Guid> result = await handler.Handle(
            new RollbackControlPlaneTenantPlanAssignmentCommand(tenantId, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors ?? []).Contains("tenant_plan_rollback_assignment_not_found");
        await repository.DidNotReceiveWithAnyArgs().UpdateAssignmentAsync(default!, default);
        await Assert.That(unitOfWork.ExecutionCount).IsEqualTo(1);
    }

    [Test]
    public async Task ApplyPlan_WhenSystemSettingIsLocked_FailsAndDoesNotWriteTenantSettings()
    {
        var tenantId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var tenantPlans = Substitute.For<ITenantPlanRepository>();
        var tenantSettings = Substitute.For<ITenantSettingRepository>();
        var systemSettings = Substitute.For<ISystemSettingRepository>();
        var unitOfWork = new ImmediateUnitOfWork();
        TenantPlanAssignment assignment = CreateActiveAssignment(tenantId, assignmentId);
        assignment.TenantPlanVersion.Settings.Add(new TenantPlanVersionSetting
        {
            Id = Guid.NewGuid(),
            SettingKey = GovernanceSettingKeys.AiAssistant.Enabled,
            JsonValue = "true",
            IsLocked = true
        });
        tenantPlans.GetAssignmentAsync(assignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        systemSettings.IsLocked(GovernanceSettingKeys.AiAssistant.Enabled).Returns(true);
        var handler = CreateApplyPlanHandler(
            tenantPlans,
            tenantSettings,
            systemSettings,
            unitOfWork,
            ImmediateSettingMutationLock.Instance,
            Substitute.For<IPublicationPolicyMutationBoundary>());

        var result = await handler.Handle(
            new ApplyControlPlaneTenantPlanAssignmentCommand(tenantId, assignmentId, Guid.NewGuid()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("tenant_plan_setting_locked");
        await Assert.That(result.Errors ?? []).Contains("tenant_plan_setting_locked");
        await tenantSettings.DidNotReceiveWithAnyArgs().UpsertManyForTenantAsync(default, default!, default);
        await Assert.That(unitOfWork.ExecutionCount).IsEqualTo(1);
    }

    [Test]
    public async Task ApplyPlan_WhenPublishedPlanHasNoSettingsOrStorageQuota_SucceedsAsNoOp()
    {
        var tenantId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var tenantPlans = Substitute.For<ITenantPlanRepository>();
        var tenantSettings = Substitute.For<ITenantSettingRepository>();
        var systemSettings = Substitute.For<ISystemSettingRepository>();
        var unitOfWork = new ImmediateUnitOfWork();
        var mutationLock = new RejectingEmptyBatchMutationLock();
        TenantPlanAssignment assignment = CreateActiveAssignment(tenantId, assignmentId);
        tenantPlans.GetAssignmentAsync(assignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        var handler = CreateApplyPlanHandler(
            tenantPlans,
            tenantSettings,
            systemSettings,
            unitOfWork,
            mutationLock,
            Substitute.For<IPublicationPolicyMutationBoundary>());

        var result = await handler.Handle(
            new ApplyControlPlaneTenantPlanAssignmentCommand(tenantId, assignmentId, Guid.NewGuid()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(assignmentId);
        await Assert.That(assignment.TenantPlanAssignmentStatusId)
            .IsEqualTo((int)TenantPlanAssignmentStatusEnum.Active);
        await Assert.That(unitOfWork.ExecutionCount).IsEqualTo(0);
        await Assert.That(mutationLock.ExecutionCount).IsEqualTo(0);
        await tenantSettings.DidNotReceiveWithAnyArgs()
            .UpsertManyForTenantAsync(default, default!, default);
        await tenantPlans.DidNotReceiveWithAnyArgs()
            .UpdateAssignmentAsync(default!, default);
        _settingsResolver.DidNotReceiveWithAnyArgs().InvalidateCache(default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    [Test]
    public async Task ApplyPlan_WhenPlanHasSettings_UpsertsTenantSettingsInTransaction()
    {
        var tenantId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var tenantPlans = Substitute.For<ITenantPlanRepository>();
        var tenantSettings = Substitute.For<ITenantSettingRepository>();
        var systemSettings = Substitute.For<ISystemSettingRepository>();
        var unitOfWork = new ImmediateUnitOfWork();
        IReadOnlyCollection<TenantSettingOverrideUpsert>? captured = null;
        TenantPlanAssignment assignment = CreateActiveAssignment(tenantId, assignmentId);
        assignment.TenantPlanVersion.Settings.Add(new TenantPlanVersionSetting
        {
            Id = Guid.NewGuid(),
            SettingKey = GovernanceSettingKeys.AiAssistant.Enabled,
            JsonValue = "true",
            IsLocked = true
        });
        tenantPlans.GetAssignmentAsync(assignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        systemSettings.IsLocked(GovernanceSettingKeys.AiAssistant.Enabled).Returns(false);
        tenantSettings
            .UpsertManyForTenantAsync(
                tenantId,
                Arg.Do<IReadOnlyCollection<TenantSettingOverrideUpsert>>(overrides => captured = overrides),
                operatorId,
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var handler = CreateApplyPlanHandler(
            tenantPlans,
            tenantSettings,
            systemSettings,
            unitOfWork,
            ImmediateSettingMutationLock.Instance,
            Substitute.For<IPublicationPolicyMutationBoundary>());

        var result = await handler.Handle(
            new ApplyControlPlaneTenantPlanAssignmentCommand(tenantId, assignmentId, operatorId),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(captured).IsNotNull();
        TenantSettingOverrideUpsert upsert = captured!.Single();
        await Assert.That(upsert.SettingKey).IsEqualTo(GovernanceSettingKeys.AiAssistant.Enabled);
        await Assert.That(upsert.Value).IsEqualTo("true");
        await Assert.That(upsert.IsLocked).IsTrue();
        await Assert.That(unitOfWork.ExecutionCount).IsEqualTo(1);
        _settingsResolver.Received(1).InvalidateCache(SettingScope.Tenant, tenantId);
        await _mediator.Received(1).Publish(
            Arg.Is<SettingChangedNotification>(notification =>
                notification.Key == GovernanceSettingKeys.AiAssistant.Enabled
                && notification.TenantId == tenantId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyPlan_WhenStorageQuotaExceedsInstanceCeiling_FailsBeforeWriting()
    {
        var tenantId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var tenantPlans = Substitute.For<ITenantPlanRepository>();
        var tenantSettings = Substitute.For<ITenantSettingRepository>();
        var systemSettings = Substitute.For<ISystemSettingRepository>();
        var unitOfWork = new ImmediateUnitOfWork();
        var calls = new List<string>();
        var mutationLock = new RecordingBatchMutationLock(calls);
        TenantPlanAssignment assignment = CreateActiveAssignment(tenantId, assignmentId);
        assignment.TenantPlanVersion.Quotas.Add(new TenantPlanVersionQuota
        {
            Id = Guid.NewGuid(),
            QuotaKey = TenantPlanQuotaKeys.StorageBytes,
            Limit = 2L * 1024 * 1024 * 1024
        });
        tenantPlans.GetAssignmentAsync(assignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        systemSettings.GetByKey(
                GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes,
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Add("quota-read");
                return new SystemSetting
                {
                    SettingKey = GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes,
                    Value = "1073741824",
                    ValueType = SettingValueType.Long
                };
            });
        var handler = CreateApplyPlanHandler(
            tenantPlans,
            tenantSettings,
            systemSettings,
            unitOfWork,
            mutationLock,
            Substitute.For<IPublicationPolicyMutationBoundary>());

        var result = await handler.Handle(
            new ApplyControlPlaneTenantPlanAssignmentCommand(tenantId, assignmentId, Guid.NewGuid()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors ?? []).Contains("tenant_plan_quota_ceiling_exceeded");
        await tenantSettings.DidNotReceiveWithAnyArgs().UpsertManyForTenantAsync(default, default!, default);
        await Assert.That(mutationLock.Keys).Contains(GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes);
        await Assert.That(calls.Count).IsEqualTo(2);
        await Assert.That(calls[0]).IsEqualTo("lock");
        await Assert.That(calls[1]).IsEqualTo("quota-read");
        await Assert.That(unitOfWork.ExecutionCount).IsEqualTo(1);
        _settingsResolver.DidNotReceiveWithAnyArgs().InvalidateCache(default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    [Test]
    public async Task ApplyPlan_WhenPlanMixesGuardedAndUnguardedSettings_PartitionsOneCanonicalBoundaryBatchAndOneRepositoryBatch()
    {
        var tenantId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var tenantPlans = Substitute.For<ITenantPlanRepository>();
        var tenantSettings = Substitute.For<ITenantSettingRepository>();
        var systemSettings = Substitute.For<ISystemSettingRepository>();
        var boundary = Substitute.For<IPublicationPolicyMutationBoundary>();
        var unitOfWork = new ImmediateUnitOfWork();
        TenantPlanAssignment assignment = CreateActiveAssignment(tenantId, assignmentId);
        AddMixedPlanSettings(assignment);
        PublicationPolicyTenantMutationRequest? boundaryRequest = null;
        IReadOnlyCollection<TenantSettingOverrideUpsert>? unguardedUpserts = null;
        tenantPlans.GetAssignmentAsync(assignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        boundary.ApplyTenantAsync(Arg.Any<PublicationPolicyTenantMutationRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                boundaryRequest = call.ArgAt<PublicationPolicyTenantMutationRequest>(0);
                return Task.FromResult(SucceededBoundaryResult());
            });
        tenantSettings.UpsertManyForTenantAsync(
                tenantId,
                Arg.Do<IReadOnlyCollection<TenantSettingOverrideUpsert>>(upserts => unguardedUpserts = upserts),
                actorId,
                cancellation.Token)
            .Returns(Task.CompletedTask);
        var handler = CreateApplyPlanHandler(
            tenantPlans, tenantSettings, systemSettings, unitOfWork,
            ImmediateSettingMutationLock.Instance, boundary);

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ApplyControlPlaneTenantPlanAssignmentCommand(tenantId, assignmentId, actorId),
            cancellation.Token);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(boundaryRequest).IsNotNull();
        await Assert.That(boundaryRequest!.TenantId).IsEqualTo(tenantId);
        await Assert.That(boundaryRequest.ActorUserId).IsEqualTo(actorId);
        await Assert.That(boundaryRequest.OccurredAtUtc.Kind).IsEqualTo(DateTimeKind.Utc);
        await Assert.That(boundaryRequest.OccurredAtUtc).IsNotEqualTo(default(DateTime));
        await Assert.That(boundaryRequest.LockedSystemBehavior)
            .IsEqualTo(PublicationPolicyLockedSystemBehavior.Reject);
        await Assert.That(boundaryRequest.Mutations.Select(mutation => mutation.Key).SequenceEqual(
        [
            GovernanceSettingKeys.Events.RequireApproval,
            GovernanceSettingKeys.Events.GroupSubmissionEnabled
        ])).IsTrue();
        await Assert.That(boundaryRequest.Mutations.All(mutation =>
            mutation.Kind == PublicationPolicyMutationKind.Set
            && mutation.TenantId == tenantId
            && mutation.IsLocked is null)).IsTrue();
        await Assert.That(unguardedUpserts).IsNotNull();
        await Assert.That(unguardedUpserts!.Select(upsert => upsert.SettingKey).SequenceEqual(
        [
            GovernanceSettingKeys.AiAssistant.Enabled,
            GovernanceSettingKeys.PublicExperience.AnnouncementBarEnabled
        ])).IsTrue();
        await boundary.Received(1).ApplyTenantAsync(Arg.Any<PublicationPolicyTenantMutationRequest>(), cancellation.Token);
        await tenantSettings.Received(1).UpsertManyForTenantAsync(
            tenantId, Arg.Any<IReadOnlyCollection<TenantSettingOverrideUpsert>>(), actorId, cancellation.Token);
    }

    [Test]
    [Arguments("event_reporting_intake_unsafe_publication_policy")]
    [Arguments("event_reporting_intake_policy_invalid")]
    public async Task ApplyPlan_WhenGuardedPreflightRejects_ReturnsExactBoundaryFailureWithoutWritesOrEffects(
        string failureCode)
    {
        var tenantId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var tenantPlans = Substitute.For<ITenantPlanRepository>();
        var tenantSettings = Substitute.For<ITenantSettingRepository>();
        var systemSettings = Substitute.For<ISystemSettingRepository>();
        var boundary = Substitute.For<IPublicationPolicyMutationBoundary>();
        var unitOfWork = new ImmediateUnitOfWork();
        TenantPlanAssignment assignment = CreateActiveAssignment(tenantId, assignmentId);
        AddMixedPlanSettings(assignment);
        tenantPlans.GetAssignmentAsync(assignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        boundary.ApplyTenantAsync(Arg.Any<PublicationPolicyTenantMutationRequest>(), Arg.Any<CancellationToken>())
            .Returns(FailedBoundaryResult(failureCode));
        var handler = CreateApplyPlanHandler(
            tenantPlans, tenantSettings, systemSettings, unitOfWork,
            ImmediateSettingMutationLock.Instance, boundary);

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ApplyControlPlaneTenantPlanAssignmentCommand(tenantId, assignmentId, Guid.NewGuid()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Id).IsEqualTo(assignmentId);
        await Assert.That(result.FailureCode).IsEqualTo(failureCode);
        await Assert.That(result.Errors).IsEquivalentTo([failureCode]);
        await tenantSettings.DidNotReceiveWithAnyArgs().UpsertManyForTenantAsync(default, default!, default);
        await tenantPlans.DidNotReceiveWithAnyArgs().UpdateAssignmentAsync(default!, default);
        _settingsResolver.DidNotReceiveWithAnyArgs().InvalidateCache(default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    [Test]
    public async Task ApplyPlan_WhenNonPolicySystemSettingIsLocked_FailsBeforeBoundaryPreflight()
    {
        var tenantId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var tenantPlans = Substitute.For<ITenantPlanRepository>();
        var tenantSettings = Substitute.For<ITenantSettingRepository>();
        var systemSettings = Substitute.For<ISystemSettingRepository>();
        var boundary = Substitute.For<IPublicationPolicyMutationBoundary>();
        TenantPlanAssignment assignment = CreateActiveAssignment(tenantId, assignmentId);
        AddMixedPlanSettings(assignment);
        tenantPlans.GetAssignmentAsync(assignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        systemSettings.IsLocked(GovernanceSettingKeys.AiAssistant.Enabled, Arg.Any<CancellationToken>()).Returns(true);
        var handler = CreateApplyPlanHandler(
            tenantPlans, tenantSettings, systemSettings, new ImmediateUnitOfWork(),
            ImmediateSettingMutationLock.Instance, boundary);

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ApplyControlPlaneTenantPlanAssignmentCommand(tenantId, assignmentId, Guid.NewGuid()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("tenant_plan_setting_locked");
        await Assert.That(result.Errors).IsEquivalentTo(["tenant_plan_setting_locked"]);
        await boundary.DidNotReceiveWithAnyArgs().ApplyTenantAsync(default!, default);
        await tenantSettings.DidNotReceiveWithAnyArgs().UpsertManyForTenantAsync(default, default!, default);
        await tenantPlans.DidNotReceiveWithAnyArgs().UpdateAssignmentAsync(default!, default);
        _settingsResolver.DidNotReceiveWithAnyArgs().InvalidateCache(default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    [Test]
    public async Task ApplyPlan_WhenMixedPlanIsAccepted_CommitsAllWritesAndPublishesCanonicalMergedEffectsAfterCommit()
    {
        var tenantId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var calls = new List<string>();
        var tenantPlans = Substitute.For<ITenantPlanRepository>();
        var tenantSettings = Substitute.For<ITenantSettingRepository>();
        var systemSettings = Substitute.For<ISystemSettingRepository>();
        var boundary = Substitute.For<IPublicationPolicyMutationBoundary>();
        var unitOfWork = new TransactionalUnitOfWork(calls);
        TenantPlanAssignment assignment = CreateActiveAssignment(tenantId, assignmentId);
        AddMixedPlanSettings(assignment);
        PublicationPolicyTenantMutationRequest? boundaryRequest = null;
        tenantPlans.GetAssignmentAsync(assignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        boundary.ApplyTenantAsync(Arg.Any<PublicationPolicyTenantMutationRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (!unitOfWork.IsInTransaction)
                    throw new InvalidOperationException("Boundary escaped the tenant-plan transaction.");

                boundaryRequest = call.ArgAt<PublicationPolicyTenantMutationRequest>(0);
                calls.Add("guarded-write");
                DateTime timestamp = boundaryRequest.OccurredAtUtc;
                return Task.FromResult(SucceededBoundaryResult(
                    Notification(GovernanceSettingKeys.Events.RequireApproval, tenantId, actorId, timestamp),
                    Notification(GovernanceSettingKeys.Events.GroupSubmissionEnabled, tenantId, actorId, timestamp)));
            });
        tenantSettings.UpsertManyForTenantAsync(
                tenantId,
                Arg.Any<IReadOnlyCollection<TenantSettingOverrideUpsert>>(),
                actorId,
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (!unitOfWork.IsInTransaction)
                    throw new InvalidOperationException("Unguarded writes escaped the tenant-plan transaction.");

                calls.Add("unguarded-write");
                return Task.CompletedTask;
            });
        tenantPlans.UpdateAssignmentAsync(assignment, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (!unitOfWork.IsInTransaction)
                throw new InvalidOperationException("Assignment timestamp escaped the tenant-plan transaction.");

            calls.Add("assignment-write");
            return Task.CompletedTask;
        });
        _settingsResolver.When(resolver => resolver.InvalidateCache(SettingScope.Tenant, tenantId))
            .Do(_ => calls.Add("cache"));
        _mediator.Publish(Arg.Any<SettingChangedNotification>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                calls.Add($"publish:{call.ArgAt<SettingChangedNotification>(0).Key}");
                return Task.CompletedTask;
            });
        var handler = CreateApplyPlanHandler(
            tenantPlans, tenantSettings, systemSettings, unitOfWork,
            ImmediateSettingMutationLock.Instance, boundary);

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ApplyControlPlaneTenantPlanAssignmentCommand(tenantId, assignmentId, actorId),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(unitOfWork.CommitCount).IsEqualTo(1);
        await Assert.That(unitOfWork.RollbackCount).IsEqualTo(0);
        await Assert.That(boundaryRequest).IsNotNull();
        await Assert.That(assignment.UpdatedAt).IsEqualTo(boundaryRequest!.OccurredAtUtc);
        await Assert.That(assignment.UpdatedBy).IsEqualTo(actorId);
        await Assert.That(calls.IndexOf("commit")).IsGreaterThan(calls.IndexOf("guarded-write"));
        await Assert.That(calls.IndexOf("commit")).IsGreaterThan(calls.IndexOf("unguarded-write"));
        await Assert.That(calls.IndexOf("commit")).IsGreaterThan(calls.IndexOf("assignment-write"));
        await Assert.That(calls.IndexOf("cache")).IsGreaterThan(calls.IndexOf("commit"));
        await Assert.That(calls.Where(call => call.StartsWith("publish:", StringComparison.Ordinal)).SequenceEqual(
        [
            $"publish:{GovernanceSettingKeys.Events.RequireApproval}",
            $"publish:{GovernanceSettingKeys.Events.GroupSubmissionEnabled}",
            $"publish:{GovernanceSettingKeys.AiAssistant.Enabled}",
            $"publish:{GovernanceSettingKeys.PublicExperience.AnnouncementBarEnabled}"
        ])).IsTrue();
        await tenantPlans.Received(1).UpdateAssignmentAsync(assignment, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyPlan_WhenBoundaryStoreThrows_RollsBackAndPropagatesWithoutEffects()
    {
        var tenantId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var tenantPlans = Substitute.For<ITenantPlanRepository>();
        var tenantSettings = Substitute.For<ITenantSettingRepository>();
        var systemSettings = Substitute.For<ISystemSettingRepository>();
        var boundary = Substitute.For<IPublicationPolicyMutationBoundary>();
        var unitOfWork = new TransactionalUnitOfWork();
        TenantPlanAssignment assignment = CreateActiveAssignment(tenantId, assignmentId);
        AddMixedPlanSettings(assignment);
        tenantPlans.GetAssignmentAsync(assignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        boundary.ApplyTenantAsync(Arg.Any<PublicationPolicyTenantMutationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PublicationPolicyMutationResult>(new InvalidOperationException("store failure")));
        var handler = CreateApplyPlanHandler(
            tenantPlans, tenantSettings, systemSettings, unitOfWork,
            ImmediateSettingMutationLock.Instance, boundary);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new ApplyControlPlaneTenantPlanAssignmentCommand(tenantId, assignmentId, Guid.NewGuid()),
            CancellationToken.None));

        await Assert.That(unitOfWork.CommitCount).IsEqualTo(0);
        await Assert.That(unitOfWork.RollbackCount).IsEqualTo(1);
        await tenantSettings.DidNotReceiveWithAnyArgs().UpsertManyForTenantAsync(default, default!, default);
        await tenantPlans.DidNotReceiveWithAnyArgs().UpdateAssignmentAsync(default!, default);
        _settingsResolver.DidNotReceiveWithAnyArgs().InvalidateCache(default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    [Test]
    public async Task ApplyPlan_WhenPlanHasNoGuardedSettings_PreservesRepositoryApplyWithoutCallingBoundary()
    {
        var tenantId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantPlans = Substitute.For<ITenantPlanRepository>();
        var tenantSettings = Substitute.For<ITenantSettingRepository>();
        var systemSettings = Substitute.For<ISystemSettingRepository>();
        var boundary = Substitute.For<IPublicationPolicyMutationBoundary>();
        IReadOnlyCollection<TenantSettingOverrideUpsert>? captured = null;
        TenantPlanAssignment assignment = CreateActiveAssignment(tenantId, assignmentId);
        AddUnguardedPlanSettings(assignment);
        tenantPlans.GetAssignmentAsync(assignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        tenantSettings.UpsertManyForTenantAsync(
                tenantId,
                Arg.Do<IReadOnlyCollection<TenantSettingOverrideUpsert>>(upserts => captured = upserts),
                actorId,
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var handler = CreateApplyPlanHandler(
            tenantPlans, tenantSettings, systemSettings, new ImmediateUnitOfWork(),
            ImmediateSettingMutationLock.Instance, boundary);

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ApplyControlPlaneTenantPlanAssignmentCommand(tenantId, assignmentId, actorId),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Select(upsert => upsert.SettingKey).SequenceEqual(
        [
            GovernanceSettingKeys.AiAssistant.Enabled,
            GovernanceSettingKeys.PublicExperience.AnnouncementBarEnabled
        ])).IsTrue();
        await boundary.DidNotReceiveWithAnyArgs().ApplyTenantAsync(default!, default);
        _settingsResolver.Received(1).InvalidateCache(SettingScope.Tenant, tenantId);
        await _mediator.Received(2).Publish(Arg.Any<SettingChangedNotification>(), Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments("duplicate")]
    [Arguments("malformed")]
    public async Task ApplyPlan_WhenGuardedPlanInputIsInvalid_ReturnsInvariantCodeBeforeAnyWrite(string inputKind)
    {
        var tenantId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var tenantPlans = Substitute.For<ITenantPlanRepository>();
        var tenantSettings = Substitute.For<ITenantSettingRepository>();
        var systemSettings = Substitute.For<ISystemSettingRepository>();
        var boundary = Substitute.For<IPublicationPolicyMutationBoundary>();
        TenantPlanAssignment assignment = CreateActiveAssignment(tenantId, assignmentId);
        AddUnguardedPlanSettings(assignment);
        IReadOnlyList<TenantPlanVersionSetting> guardedSettings = inputKind == "duplicate"
            ?
            [
                PlanSetting(GovernanceSettingKeys.Events.RequireApproval, "true"),
                PlanSetting(GovernanceSettingKeys.Events.RequireApproval, "false")
            ]
            :
            [PlanSetting(GovernanceSettingKeys.Events.RequireApproval, "{")];
        foreach (TenantPlanVersionSetting guardedSetting in guardedSettings)
        {
            assignment.TenantPlanVersion.Settings.Add(guardedSetting);
        }
        PublicationPolicyTenantMutationRequest? boundaryRequest = null;
        tenantPlans.GetAssignmentAsync(assignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        boundary.ApplyTenantAsync(Arg.Any<PublicationPolicyTenantMutationRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                boundaryRequest = call.ArgAt<PublicationPolicyTenantMutationRequest>(0);
                return FailedBoundaryResult("event_reporting_intake_policy_invalid");
            });
        var handler = CreateApplyPlanHandler(
            tenantPlans, tenantSettings, systemSettings, new ImmediateUnitOfWork(),
            ImmediateSettingMutationLock.Instance, boundary);

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ApplyControlPlaneTenantPlanAssignmentCommand(tenantId, assignmentId, Guid.NewGuid()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Id).IsEqualTo(assignmentId);
        await Assert.That(result.FailureCode).IsEqualTo("event_reporting_intake_policy_invalid");
        await Assert.That(result.Errors).IsEquivalentTo(["event_reporting_intake_policy_invalid"]);
        await Assert.That(boundaryRequest).IsNotNull();
        await Assert.That(inputKind == "duplicate"
            ? boundaryRequest!.Mutations.Count(mutation => mutation.Key == GovernanceSettingKeys.Events.RequireApproval) == 2
            : boundaryRequest!.Mutations.Single().JsonValue == "{").IsTrue();
        await tenantSettings.DidNotReceiveWithAnyArgs().UpsertManyForTenantAsync(default, default!, default);
        await tenantPlans.DidNotReceiveWithAnyArgs().UpdateAssignmentAsync(default!, default);
        _settingsResolver.DidNotReceiveWithAnyArgs().InvalidateCache(default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    private ApplyControlPlaneTenantPlanAssignmentCommandHandler CreateApplyPlanHandler(
        ITenantPlanRepository tenantPlans,
        ITenantSettingRepository tenantSettings,
        ISystemSettingRepository systemSettings,
        IUnitOfWork unitOfWork,
        ISettingMutationLock mutationLock,
        IPublicationPolicyMutationBoundary publicationPolicyBoundary) => new(
            tenantPlans,
            tenantSettings,
            systemSettings,
            new TenantPlanStorageQuotaCeilingPolicy(systemSettings),
            unitOfWork,
            mutationLock,
            publicationPolicyBoundary,
            _settingsResolver,
            _mediator);

    private static PublicationPolicyMutationResult SucceededBoundaryResult(
        params SettingChangedNotification[] notifications) => new(
            Success: true,
            FailureCode: null,
            Message: string.Empty,
            DeferredNotifications: [.. notifications]);

    private static Task<PublicationPolicyMutationResult> FailedBoundaryResult(string failureCode) =>
        Task.FromResult(new PublicationPolicyMutationResult(
            Success: false,
            FailureCode: failureCode,
            Message: string.Empty,
            DeferredNotifications: []));

    private static SettingChangedNotification Notification(
        string key,
        Guid tenantId,
        Guid actorId,
        DateTime occurredAtUtc) => new(
            key,
            oldValue: null,
            newValue: "true",
            SettingSource.TenantOverride,
            tenantId,
            actorId,
            occurredAtUtc);

    private static void AddMixedPlanSettings(TenantPlanAssignment assignment)
    {
        assignment.TenantPlanVersion.Settings.Add(PlanSetting(
            GovernanceSettingKeys.Events.GroupSubmissionEnabled, "true"));
        assignment.TenantPlanVersion.Settings.Add(PlanSetting(
            GovernanceSettingKeys.AiAssistant.Enabled, "true", isLocked: true));
        assignment.TenantPlanVersion.Settings.Add(PlanSetting(
            GovernanceSettingKeys.Events.RequireApproval, "true"));
        assignment.TenantPlanVersion.Settings.Add(PlanSetting(
            GovernanceSettingKeys.PublicExperience.AnnouncementBarEnabled, "false"));
    }

    private static void AddUnguardedPlanSettings(TenantPlanAssignment assignment)
    {
        assignment.TenantPlanVersion.Settings.Add(PlanSetting(
            GovernanceSettingKeys.AiAssistant.Enabled, "true", isLocked: true));
        assignment.TenantPlanVersion.Settings.Add(PlanSetting(
            GovernanceSettingKeys.PublicExperience.AnnouncementBarEnabled, "false"));
    }

    private static TenantPlanVersionSetting PlanSetting(string key, string value, bool isLocked = false) => new()
    {
        Id = Guid.NewGuid(),
        SettingKey = key,
        JsonValue = value,
        IsLocked = isLocked
    };

    private static TenantPlanDraft CreateValidDraft() => new(
        Key: "community",
        Name: "Community",
        Pricing: new TenantPlanPricing(29m, "EUR", TenantPlanBillingPeriods.Monthly),
        IsActiveForProvisioning: true,
        SettingOverrides:
        [
            new TenantPlanSettingOverride(GovernanceSettingKeys.AiAssistant.Enabled, "true", IsLocked: false)
        ],
        QuotaLimits:
        [
            new TenantPlanQuotaLimit(TenantPlanQuotaKeys.AiDailyTenantMessages, 1000)
        ]);

    private static TenantPlan CreatePlan(string key, string displayName) => new()
    {
        Id = Guid.NewGuid(),
        Key = key,
        DisplayName = displayName,
        CreatedAt = DateTime.UtcNow
    };

    private static TenantPlanVersion CreateVersion(
        TenantPlan plan,
        int versionNumber,
        TenantPlanStatusEnum status,
        decimal priceAmount,
        bool activeForProvisioning)
    {
        return new TenantPlanVersion
        {
            Id = Guid.NewGuid(),
            TenantPlan = plan,
            TenantPlanId = plan.Id,
            VersionNumber = versionNumber,
            TenantPlanStatusId = (int)status,
            TenantPlanStatus = new TenantPlanStatus
            {
                Id = (int)status,
                MasterCode = status.ToString().ToUpperInvariant(),
                FullName = status.ToString(),
                AllowsProvisioning = status == TenantPlanStatusEnum.Published
            },
            PriceAmount = priceAmount,
            CurrencyCode = "EUR",
            BillingPeriod = TenantPlanBillingPeriods.Monthly,
            IsActiveForProvisioning = activeForProvisioning,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static TenantPlanAssignment CreateActiveAssignment(Guid tenantId, Guid assignmentId)
    {
        TenantPlan plan = CreatePlan("community", "Community");
        TenantPlanVersion version = CreateVersion(plan, versionNumber: 1, TenantPlanStatusEnum.Published, 29m, true);

        return new TenantPlanAssignment
        {
            Id = assignmentId,
            TenantId = tenantId,
            TenantPlan = plan,
            TenantPlanId = plan.Id,
            TenantPlanVersion = version,
            TenantPlanVersionId = version.Id,
            TenantPlanAssignmentStatusId = (int)TenantPlanAssignmentStatusEnum.Active,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = Guid.NewGuid()
        };
    }

    private sealed class ImmediateSettingMutationLock : ISettingMutationLock
    {
        internal static readonly ImmediateSettingMutationLock Instance = new();

        public Task<T> ExecuteAsync<T>(
            string canonicalSettingKey,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);

        public Task<T> ExecuteManyAsync<T>(
            IEnumerable<string> canonicalSettingKeys,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);
    }

    private sealed class RecordingBatchMutationLock(List<string> calls) : ISettingMutationLock
    {
        public IReadOnlyList<string> Keys { get; private set; } = [];

        public Task<T> ExecuteAsync<T>(
            string canonicalSettingKey,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);

        public Task<T> ExecuteManyAsync<T>(
            IEnumerable<string> canonicalSettingKeys,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            Keys = canonicalSettingKeys.ToArray();
            calls.Add("lock");
            return operation(cancellationToken);
        }
    }

    private sealed class RejectingEmptyBatchMutationLock : ISettingMutationLock
    {
        public int ExecutionCount { get; private set; }

        public Task<T> ExecuteAsync<T>(
            string canonicalSettingKey,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);

        public Task<T> ExecuteManyAsync<T>(
            IEnumerable<string> canonicalSettingKeys,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            if (!canonicalSettingKeys.Any())
            {
                throw new ArgumentException("At least one setting key is required.", nameof(canonicalSettingKeys));
            }

            return operation(cancellationToken);
        }
    }

    private sealed class TransactionalUnitOfWork(List<string>? calls = null) : IUnitOfWork
    {
        public bool IsInTransaction { get; private set; }
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default)
        {
            IsInTransaction = true;
            try
            {
                await operation(ct);
                CommitCount++;
                calls?.Add("commit");
            }
            catch
            {
                RollbackCount++;
                throw;
            }
            finally
            {
                IsInTransaction = false;
            }
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
        {
            IsInTransaction = true;
            try
            {
                T result = await operation(ct);
                CommitCount++;
                calls?.Add("commit");
                return result;
            }
            catch
            {
                RollbackCount++;
                throw;
            }
            finally
            {
                IsInTransaction = false;
            }
        }

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) =>
            ExecuteInTransactionAsync(operation, ct);
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public int ExecutionCount { get; private set; }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default)
        {
            ExecutionCount++;
            await operation(ct);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
        {
            ExecutionCount++;
            return await operation(ct);
        }

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) =>
            ExecuteInTransactionAsync(operation, ct);
    }
}
