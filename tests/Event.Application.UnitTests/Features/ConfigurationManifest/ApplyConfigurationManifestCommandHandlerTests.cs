// ABOUTME: Verifies configuration-manifest bootstrap applies one atomic lock-scoped transaction.
// ABOUTME: Covers fresh preflight, safe rollback audit, and post-commit effects without manifest values.

using System.Collections.Immutable;
using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.ConfigurationManifest.Application;
using Explore.Application.Features.PaidEventPolicies;
using Explore.Application.Features.ConfigurationManifest.Handlers.Commands;
using Explore.Application.Features.ConfigurationManifest.Requests.Commands;
using Explore.Application.Features.ConfigurationManifest.Compilation;
using Explore.Application.Features.ConfigurationManifest.Contracts;
using Explore.Application.Features.ConfigurationManifest.Ingestion;
using Explore.Application.Features.ConfigurationManifest.Preflight;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Settings;
using Explore.Domain.Settings.Definitions;
using Explore.Domain.Settings.Documents;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.ConfigurationManifest;

public sealed class ApplyConfigurationManifestCommandHandlerTests
{
    [Test]
    public async Task ApplyAsyncRejectsNullSource()
    {
        var fixture = new Fixture();

        await Assert.That(() => fixture.Handler.ApplyAsync(
                null!,
                CancellationToken.None))
            .Throws<ArgumentNullException>();

        await fixture.Preflight.DidNotReceiveWithAnyArgs()
            .EvaluateAsync(default!, default);
    }

    [Test]
    public async Task Handle_BootstrapRerunsPreflightInsideLockAndDefersEffectsUntilCommit()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));

        var result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(Source()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Message)
            .IsEqualTo("Configuration manifest bootstrap completed.");
        await Assert.That(fixture.UnitOfWork.SerializableExecutionCount)
            .IsEqualTo(1);
        await Assert.That(fixture.Lock.ExecutionCount).IsEqualTo(1);
        await Assert.That(fixture.Lock.Keys).Contains(EventSettingDefinitions.RequireApproval.Key);
        await Assert.That(fixture.Lock.Keys).Contains(EventSettingDefinitions.UserSubmissionEnabled.Key);
        await Assert.That(fixture.Lock.Keys).Contains("tenant.slug.primary");
        await fixture.Preflight.Received(2).EvaluateAsync(
            Arg.Any<ConfigurationManifestApplyPlan>(),
            Arg.Any<CancellationToken>());
        await fixture.TenantCreation.Received(1).CreateInCurrentTransactionAsync(
            Arg.Is<TenantCreationRequest>(request =>
                request.Slug == "primary"
                && request.TenantStatusId == (int)TenantStatusEnum.Provisioning
                && request.ActorUserId == null),
            Arg.Any<CancellationToken>());
        await fixture.PolicyBoundary.Received(1)
            .ApplyTenantInCurrentTransactionAsync(
            Arg.Is<PublicationPolicyTenantMutationRequest>(request =>
                request.ActorUserId == null
                && request.Mutations.Length == 1),
            Arg.Any<CancellationToken>());
        await fixture.Settings.Received(1).CreateInCurrentTransactionAsync(
            Arg.Is<ConfigurationManifestTenantSettingMutationInput>(input =>
                input.Mutations.Count == 1
                && input.Mutations.Single().Key
                    == PublicExperienceSettingDefinitions.EventCatalogLabel.Key
                && input.ActorUserId == null),
            Arg.Any<CancellationToken>());
        await fixture.InstanceSettings.DidNotReceiveWithAnyArgs()
            .ApplyInCurrentTransactionAsync(default!, default);
        await fixture.Audit.Received(1).CreateAsync(
            Arg.Is<ConfigurationManifestOperation>(operation =>
                operation.Id == result.Id
                && operation.Status == ConfigurationManifestOperationStatus.Applied
                && operation.CreatedTenantCount == 1),
            Arg.Is<IReadOnlyCollection<ConfigurationManifestTenantResult>>(tenantResults =>
                tenantResults.Count == 1
                && tenantResults.Single().Status
                    == ConfigurationManifestTenantResultStatus.Created),
            Arg.Any<CancellationToken>());
        await Assert.That(fixture.Publisher.PublishedInsideLock).IsFalse();
        await Assert.That(fixture.Publisher.Notifications.Count).IsEqualTo(2);
        fixture.Documents.Received(1).InvalidateTenantDocumentCache(
            Arg.Any<Guid>(),
            SettingsDocumentKeys.Tenant.Branding);
        fixture.SettingsResolver.Received(1).InvalidateCache(
            SettingScope.Tenant,
            Arg.Any<Guid>());
    }

    [Test]
    public async Task Handle_ReportingIntakeUsesPublicationPolicyBoundary()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(includeReportingIntake: true)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await fixture.PolicyBoundary.Received(1)
            .ApplyTenantInCurrentTransactionAsync(
            Arg.Is<PublicationPolicyTenantMutationRequest>(request =>
                request.Mutations.Any(mutation =>
                    mutation.Key == GovernanceSettingKeys
                        .EventReporting.IntakeEnabled)),
            Arg.Any<CancellationToken>());
        await fixture.Settings.DidNotReceive()
            .CreateInCurrentTransactionAsync(
                Arg.Is<ConfigurationManifestTenantSettingMutationInput>(input =>
                    input.Mutations.Any(mutation =>
                        mutation.Key == GovernanceSettingKeys
                            .EventReporting.IntakeEnabled)),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_PaidPolicyUsesCanonicalBoundaryInsideManifestLock()
    {
        var fixture = new Fixture();
        bool invokedInsideLock = false;
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));
        fixture.PaidPolicyBoundary.ReviseTenantInCurrentTransactionAsync(
                Arg.Any<TenantPaidEventPolicyMutationInput>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                invokedInsideLock = fixture.Lock.IsInside;
                return new PaidEventPolicyMutationResult(
                    Success: true,
                    Guid.CreateVersion7(),
                    FailureCode: null,
                    "Paid-event policy revised.",
                    []);
            });

        var result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(Source(includePaidPolicy: true)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        TenantPaidEventPolicyMutationInput request =
            fixture.PaidPolicyBoundary.ReceivedCalls()
                .Single()
                .GetArguments()[0] as TenantPaidEventPolicyMutationInput
            ?? throw new InvalidOperationException("Paid-policy request was not captured.");
        await Assert.That(invokedInsideLock).IsTrue();
        await Assert.That(request.ExpectedInstancePolicyVersion).IsEqualTo(1);
        await Assert.That(request.RequireAbsentTenantPolicy).IsTrue();
        await Assert.That(fixture.Lock.Keys)
            .Contains(PaidEventPolicyMutationLockKeys.Instance);
        await Assert.That(fixture.Lock.Keys)
            .Contains(PaidEventPolicyMutationLockKeys.ForTenant(request.TenantId));
    }

    [Test]
    public async Task Handle_TenantPaidPolicyFailureReturnsWriteConflict()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));
        fixture.PaidPolicyBoundary.ReviseTenantInCurrentTransactionAsync(
                Arg.Any<TenantPaidEventPolicyMutationInput>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaidEventPolicyMutationResult(
                Success: false,
                PolicyVersionId: null,
                FailureCode: "provider-specific",
                Message: "Tenant policy rejected.",
                Errors: []));

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(includePaidPolicy: true)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(ConfigurationManifestApplicationFailureCodes.WriteConflict);
        await Assert.That(result.Message).IsEqualTo("Tenant policy rejected.");
        await fixture.FailureRecorder.Received(1).RecordAsync(
            Arg.Is<ConfigurationManifestOperation>(operation =>
                operation.FailedTenantCount == 1
                && operation.ReasonCode
                    == ConfigurationManifestApplicationFailureCodes.WriteConflict),
            Arg.Any<CancellationToken>());
        await fixture.Settings.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
    }

    [Test]
    public async Task Handle_PublicationPolicyFailureUsesWriteConflictFallback()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));
        fixture.PolicyBoundary.ApplyTenantInCurrentTransactionAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new PublicationPolicyMutationResult(
                Success: false,
                FailureCode: null,
                Message: "Publication policy rejected.",
                DeferredNotifications: []));

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(Source()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(ConfigurationManifestApplicationFailureCodes.WriteConflict);
        await Assert.That(result.Message).IsEqualTo("Publication policy rejected.");
        await fixture.Settings.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
    }

    [Test]
    public async Task Handle_PublicationPolicyFailurePreservesBoundaryFailureCode()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));
        fixture.PolicyBoundary.ApplyTenantInCurrentTransactionAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new PublicationPolicyMutationResult(
                Success: false,
                FailureCode:
                    ConfigurationManifestApplicationFailureCodes.SettingLocked,
                Message: "Publication policy is locked.",
                DeferredNotifications: []));

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(Source()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(ConfigurationManifestApplicationFailureCodes.SettingLocked);
        await Assert.That(result.Message).IsEqualTo("Publication policy is locked.");
    }

    [Test]
    public async Task Handle_InstanceSettingFailureUsesWriteConflictFallback()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));
        fixture.InstanceSettings.ApplyInCurrentTransactionAsync(
                Arg.Any<ConfigurationManifestInstanceSettingMutationInput>(),
                Arg.Any<CancellationToken>())
            .Returns(new ConfigurationManifestInstanceSettingMutationResult(
                Success: false,
                FailureCode: null,
                Message: "Instance settings rejected.",
                DeferredNotifications: []));

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(includeInstanceSetting: true)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(ConfigurationManifestApplicationFailureCodes.WriteConflict);
        await Assert.That(result.Message).IsEqualTo("Instance settings rejected.");
        await fixture.TenantCreation.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
    }

    [Test]
    public async Task Handle_InstanceSettingFailurePreservesBoundaryFailureCode()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));
        fixture.InstanceSettings.ApplyInCurrentTransactionAsync(
                Arg.Any<ConfigurationManifestInstanceSettingMutationInput>(),
                Arg.Any<CancellationToken>())
            .Returns(new ConfigurationManifestInstanceSettingMutationResult(
                Success: false,
                FailureCode:
                    ConfigurationManifestApplicationFailureCodes.SettingLocked,
                Message: "Instance settings are locked.",
                DeferredNotifications: []));

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(includeInstanceSetting: true)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(ConfigurationManifestApplicationFailureCodes.SettingLocked);
        await Assert.That(result.Message).IsEqualTo("Instance settings are locked.");
    }

    [Test]
    public async Task Handle_ProposedInstancePolicyAppliesBeforeTenantAgainstNewRevision()
    {
        var fixture = new Fixture();
        var mutationOrder = new List<string>();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));
        fixture.PaidPolicyBoundary.ReviseInstanceInCurrentTransactionAsync(
                Arg.Any<InstancePaidEventPolicyMutationInput>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                mutationOrder.Add("instance");
                return new PaidEventPolicyMutationResult(
                    Success: true,
                    Guid.CreateVersion7(),
                    FailureCode: null,
                    "Paid-event policy revised.",
                    []);
            });
        fixture.PaidPolicyBoundary.ReviseTenantInCurrentTransactionAsync(
                Arg.Any<TenantPaidEventPolicyMutationInput>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                mutationOrder.Add("tenant");
                return new PaidEventPolicyMutationResult(
                    Success: true,
                    Guid.CreateVersion7(),
                    FailureCode: null,
                    "Paid-event policy revised.",
                    []);
            });

        var result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(
                    includePaidPolicy: true,
                    includeInstancePaidPolicy: true)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(mutationOrder.SequenceEqual(
            ["instance", "tenant"],
            StringComparer.Ordinal)).IsTrue();
        InstancePaidEventPolicyMutationInput instanceRequest =
            fixture.PaidPolicyBoundary.ReceivedCalls()
                .Single(call => call.GetMethodInfo().Name
                    == nameof(IPaidEventPolicyMutationBoundary
                        .ReviseInstanceInCurrentTransactionAsync))
                .GetArguments()[0] as InstancePaidEventPolicyMutationInput
            ?? throw new InvalidOperationException(
                "Instance paid-policy request was not captured.");
        TenantPaidEventPolicyMutationInput tenantRequest =
            fixture.PaidPolicyBoundary.ReceivedCalls()
                .Single(call => call.GetMethodInfo().Name
                    == nameof(IPaidEventPolicyMutationBoundary
                        .ReviseTenantInCurrentTransactionAsync))
                .GetArguments()[0] as TenantPaidEventPolicyMutationInput
            ?? throw new InvalidOperationException(
                "Tenant paid-policy request was not captured.");
        await Assert.That(instanceRequest.ExpectedActivePolicyVersion)
            .IsEqualTo(1);
        await Assert.That(tenantRequest.ExpectedInstancePolicyVersion)
            .IsEqualTo(2);
    }

    [Test]
    public async Task Handle_CompilationFailureReturnsFailureCodeAsError()
    {
        var fixture = new Fixture();

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(ConfigurationManifestMode.Off)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors).IsEquivalentTo(
        [
            result.FailureCode!
        ]);
        await fixture.Preflight.DidNotReceiveWithAnyArgs()
            .EvaluateAsync(default!, default);
    }

    [Test]
    [Arguments(
        "InstanceSectionDigest",
        "Applied manifest bootstrap state has no instance-section digest.")]
    [Arguments(
        "BootstrapGeneration",
        "Applied manifest bootstrap state has no generation.")]
    public async Task Handle_RejectsCorruptPersistedBootstrapState(
        string propertyName,
        string expectedMessage)
    {
        var fixture = new Fixture();
        DateTime occurredAt = DateTime.UtcNow.AddMinutes(-1);
        ConfigurationManifestOperation bootstrap =
            ConfigurationManifestOperation.Create(
                Guid.CreateVersion7(),
                ConfigurationManifestAuditMode.Bootstrap,
                ConfigurationManifestContractMetadata.ApiVersion,
                ConfigurationManifestContractMetadata.Kind,
                "existing-bootstrap",
                new string('c', ConfigurationManifestOperation.DigestLength),
                ConfigurationManifestOperationStatus.Applied,
                requestedTenantCount: 1,
                createdTenantCount: 1,
                skippedExistingTenantCount: 0,
                failedTenantCount: 0,
                reasonCode: null,
                reason: null,
                occurredAt,
                occurredAt.AddSeconds(1),
                instanceSectionDigest:
                    new string('d', ConfigurationManifestOperation.DigestLength),
                bootstrapGeneration: 1);
        bootstrap.GetType()
            .GetProperty(propertyName)!
            .SetValue(bootstrap, null);
        fixture.Audit.GetLatestAppliedBootstrapAsync(
                Arg.Any<CancellationToken>())
            .Returns(bootstrap);

        InvalidOperationException? exception = null;
        try
        {
            _ = await fixture.Handler.Handle(
                new ApplyConfigurationManifestCommand(Source()),
                CancellationToken.None);
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        await Assert.That(exception?.Message).IsEqualTo(expectedMessage);
        await fixture.Preflight.DidNotReceiveWithAnyArgs()
            .EvaluateAsync(default!, default);
    }

    [Test]
    public async Task Handle_FirstBootstrapPersistsInstanceSectionDigestAndGeneration()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(includeInstancePaidPolicy: true)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        ConfigurationManifestOperation? persisted =
            fixture.PersistedOperation;
        await Assert.That(persisted).IsNotNull();
        if (persisted is null)
            return;

        var digestProperty = persisted.GetType()
            .GetProperty("InstanceSectionDigest");
        var generationProperty = persisted.GetType()
            .GetProperty("BootstrapGeneration");
        await Assert.That(digestProperty).IsNotNull();
        await Assert.That(generationProperty).IsNotNull();
        if (digestProperty is null || generationProperty is null)
            return;

        string? instanceSectionDigest =
            digestProperty.GetValue(persisted) as string;
        await Assert.That(instanceSectionDigest).IsNotNull();
        await Assert.That(instanceSectionDigest?.Length)
            .IsEqualTo(ConfigurationManifestOperation.DigestLength);
        await Assert.That(generationProperty.GetValue(persisted))
            .IsEqualTo(1);
        await Assert.That(persisted.InstanceChangedDocumentKeyNames)
            .Contains(ConfigurationManifestDocumentKeys.InstancePaidEventPolicy);
    }

    [Test]
    public async Task Handle_InstanceSettingsPersistScopeFactsAndDispatchAfterCommit()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(
                call.Arg<ConfigurationManifestApplyPlan>()));

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(includeInstanceSetting: true)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await fixture.InstanceSettings.Received(1)
            .ApplyInCurrentTransactionAsync(
                Arg.Is<ConfigurationManifestInstanceSettingMutationInput>(
                    input => input.Mutations.Count == 1
                        && input.Mutations[0].Key
                        == AppearanceSettingDefinitions.DefaultThemeMode.Key),
                Arg.Any<CancellationToken>());
        await Assert.That(
                fixture.PersistedOperation?.InstanceChangedSettingKeyNames)
            .Contains(AppearanceSettingDefinitions.DefaultThemeMode.Key);
        fixture.SettingsResolver.Received(1)
            .InvalidateCache(SettingScope.Instance);
        await Assert.That(fixture.Publisher.Notifications.Any(notification =>
                notification is SettingChangedNotification changed
                && changed.Key
                    == AppearanceSettingDefinitions.DefaultThemeMode.Key
                && changed.Scope == SettingSource.SystemDefault
                && changed.TenantId is null))
            .IsTrue();
    }

    [Test]
    public async Task Handle_SameInstanceSectionRerunDoesNotReapplyInstancePolicy()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));

        BaseCommandResponse<Guid> first = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(includeInstancePaidPolicy: true)),
            CancellationToken.None);
        BaseCommandResponse<Guid> second = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(includeInstancePaidPolicy: true)),
            CancellationToken.None);

        await Assert.That(first.IsSuccess).IsTrue();
        await Assert.That(second.IsSuccess).IsTrue();
        await fixture.PaidPolicyBoundary.Received(1)
            .ReviseInstanceInCurrentTransactionAsync(
                Arg.Any<InstancePaidEventPolicyMutationInput>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ChangedInstanceSectionAfterBootstrapRejectsBeforeSecondWrite()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));

        BaseCommandResponse<Guid> first = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(
                    includeInstancePaidPolicy: true,
                    instancePaymentsEnabled: true,
                    digestCharacter: 'a')),
            CancellationToken.None);
        BaseCommandResponse<Guid> second = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(
                    includeInstancePaidPolicy: true,
                    instancePaymentsEnabled: false,
                    digestCharacter: 'b')),
            CancellationToken.None);

        await Assert.That(first.IsSuccess).IsTrue();
        await Assert.That(second.IsSuccess).IsFalse();
        await Assert.That(second.FailureCode)
            .IsEqualTo(
                "configuration_manifest_instance_already_bootstrapped");
        await fixture.PaidPolicyBoundary.Received(1)
            .ReviseInstanceInCurrentTransactionAsync(
                Arg.Any<InstancePaidEventPolicyMutationInput>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_UnchangedInstanceSectionCanAddTenantWithoutHistoricalReapply()
    {
        var fixture = new Fixture();
        Guid existingTenantId = Guid.CreateVersion7();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                ConfigurationManifestApplyPlan plan =
                    call.Arg<ConfigurationManifestApplyPlan>();
                if (plan.Tenants.Length == 1)
                {
                    return Valid(plan);
                }

                ConfigurationManifestApplyPlan bound = plan with
                {
                    Instance = plan.Instance with
                    {
                        PaidEventPolicy =
                            plan.Instance.PaidEventPolicy! with
                            {
                                ProposedRevision = null,
                                ExpectedActivePolicyVersion = 1
                            }
                    }
                };
                return new ConfigurationManifestPreflightResult(
                    bound,
                    bound.Tenants.Select(tenant =>
                            tenant.Slug == "primary"
                                ? new ConfigurationManifestPreflightTenant(
                                    tenant,
                                    ConfigurationManifestTenantDisposition
                                        .SkippedExisting,
                                    existingTenantId)
                                : new ConfigurationManifestPreflightTenant(
                                    tenant,
                                    ConfigurationManifestTenantDisposition
                                        .Create,
                                    tenant.PlannedTenantId))
                        .ToImmutableArray(),
                    []);
            });

        BaseCommandResponse<Guid> first = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(includeInstancePaidPolicy: true)),
            CancellationToken.None);
        BaseCommandResponse<Guid> second = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(
                    includeInstancePaidPolicy: true,
                    digestCharacter: 'b',
                    tenantSlugs: ["primary", "secondary"])),
            CancellationToken.None);

        await Assert.That(first.IsSuccess).IsTrue();
        await Assert.That(second.IsSuccess).IsTrue();
        await Assert.That(fixture.PersistedOperation?.CreatedTenantCount)
            .IsEqualTo(1);
        await Assert.That(
                fixture.PersistedOperation?.SkippedExistingTenantCount)
            .IsEqualTo(1);
        await fixture.PaidPolicyBoundary.Received(1)
            .ReviseInstanceInCurrentTransactionAsync(
                Arg.Any<InstancePaidEventPolicyMutationInput>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_SameSectionAfterDay2ChangeUsesCurrentAuthorityRevision()
    {
        var fixture = new Fixture();
        Guid existingTenantId = Guid.CreateVersion7();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                ConfigurationManifestApplyPlan plan =
                    call.Arg<ConfigurationManifestApplyPlan>();
                if (plan.Tenants.Length == 1)
                {
                    return Valid(plan);
                }

                ConfigurationManifestApplyPlan rebound = plan with
                {
                    Instance = plan.Instance with
                    {
                        PaidEventPolicy =
                            plan.Instance.PaidEventPolicy! with
                            {
                                ProposedRevision = null,
                                ExpectedActivePolicyVersion = 7
                            }
                    }
                };
                return new ConfigurationManifestPreflightResult(
                    rebound,
                    rebound.Tenants.Select(tenant =>
                            tenant.Slug == "primary"
                                ? new ConfigurationManifestPreflightTenant(
                                    tenant,
                                    ConfigurationManifestTenantDisposition
                                        .SkippedExisting,
                                    existingTenantId)
                                : new ConfigurationManifestPreflightTenant(
                                    tenant,
                                    ConfigurationManifestTenantDisposition
                                        .Create,
                                    tenant.PlannedTenantId))
                        .ToImmutableArray(),
                    []);
            });

        BaseCommandResponse<Guid> first = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(
                    includePaidPolicy: true,
                    includeInstancePaidPolicy: true)),
            CancellationToken.None);
        BaseCommandResponse<Guid> second = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(
                    includePaidPolicy: true,
                    includeInstancePaidPolicy: true,
                    digestCharacter: 'b',
                    tenantSlugs: ["primary", "secondary"])),
            CancellationToken.None);

        await Assert.That(first.IsSuccess).IsTrue();
        await Assert.That(second.IsSuccess).IsTrue();
        await fixture.PaidPolicyBoundary.Received(1)
            .ReviseInstanceInCurrentTransactionAsync(
                Arg.Any<InstancePaidEventPolicyMutationInput>(),
                Arg.Any<CancellationToken>());
        await fixture.PaidPolicyBoundary.Received(1)
            .ReviseTenantInCurrentTransactionAsync(
                Arg.Is<TenantPaidEventPolicyMutationInput>(input =>
                    input.ExpectedInstancePolicyVersion == 7),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_OneInvalidTenantPreventsEveryInstanceAndTenantWrite()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                ConfigurationManifestApplyPlan plan =
                    call.Arg<ConfigurationManifestApplyPlan>();
                return new ConfigurationManifestPreflightResult(
                    plan,
                    plan.Tenants.Select(tenant =>
                            new ConfigurationManifestPreflightTenant(
                                tenant,
                                ConfigurationManifestTenantDisposition
                                    .Create,
                                tenant.PlannedTenantId))
                        .ToImmutableArray(),
                    [
                        new ConfigurationManifestPreflightError(
                            ManifestIndex: 1,
                            EventSettingDefinitions.RequireApproval.Key,
                            ConfigurationManifestApplicationFailureCodes
                                .SettingLocked,
                            "A locked setting cannot be applied.")
                    ]);
            });

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(
                    includeInstancePaidPolicy: true,
                    tenantSlugs: ["primary", "secondary"])),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(fixture.Lock.ExecutionCount).IsEqualTo(0);
        await fixture.PaidPolicyBoundary.DidNotReceiveWithAnyArgs()
            .ReviseInstanceInCurrentTransactionAsync(default!, default);
        await fixture.TenantCreation.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
        await fixture.PolicyBoundary.DidNotReceiveWithAnyArgs()
            .ApplyTenantInCurrentTransactionAsync(default!, default);
        await fixture.Settings.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
    }

    [Test]
    public async Task Handle_WriteFailureRollsBackThenRecordsOnlySafeFailedOperation()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));
        fixture.TenantCreation.CreateInCurrentTransactionAsync(
                Arg.Any<TenantCreationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<TenantCreationOutcome>>(_ =>
                throw new InvalidOperationException("manifest-value-must-not-escape"));

        var result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(Source()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(ConfigurationManifestApplicationFailureCodes.ApplyFailed);
        await Assert.That(result.Errors).IsEquivalentTo(
        [
            ConfigurationManifestApplicationFailureCodes.ApplyFailed
        ]);
        await fixture.Audit.DidNotReceiveWithAnyArgs()
            .CreateAsync(default!, default!, default);
        await fixture.FailureRecorder.Received(1).RecordAsync(
            Arg.Is<ConfigurationManifestOperation>(operation =>
                operation.Id == result.Id
                && operation.Status == ConfigurationManifestOperationStatus.Failed
                && operation.Reason != null
                && !operation.Reason.Contains("manifest-value", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
        await Assert.That(fixture.Publisher.Notifications).IsEmpty();
        fixture.Documents.DidNotReceiveWithAnyArgs()
            .InvalidateTenantDocumentCache(default, default!);
    }

    [Test]
    public async Task Handle_InitialPreflightFailureRecordsSafeAuditWithoutOpeningTransaction()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                ConfigurationManifestApplyPlan plan =
                    call.Arg<ConfigurationManifestApplyPlan>();
                return new ConfigurationManifestPreflightResult(
                    plan,
                    Valid(plan).Tenants,
                    [
                        new ConfigurationManifestPreflightError(
                            ManifestIndex: 0,
                            EventSettingDefinitions.RequireApproval.Key,
                            ConfigurationManifestApplicationFailureCodes.SettingLocked,
                            "A locked setting cannot be applied.")
                    ]);
            });

        var result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(Source()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(ConfigurationManifestApplicationFailureCodes.SettingLocked);
        await Assert.That(result.Errors).IsEquivalentTo(
        [
            $"{ConfigurationManifestApplicationFailureCodes.SettingLocked}:"
                + EventSettingDefinitions.RequireApproval.Key
        ]);
        await Assert.That(fixture.Lock.ExecutionCount).IsEqualTo(0);
        await fixture.TenantCreation.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
        await fixture.FailureRecorder.Received(1).RecordAsync(
            Arg.Is<ConfigurationManifestOperation>(operation =>
                operation.Status == ConfigurationManifestOperationStatus.Failed
                && operation.FailedTenantCount == 1),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_InitialPreflightCountsOnlyDistinctTenantIndexes()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => InvalidWithInstanceAndTenantErrors(
                call.Arg<ConfigurationManifestApplyPlan>()));

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(tenantSlugs: ["primary", "secondary"])),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await fixture.FailureRecorder.Received(1).RecordAsync(
            Arg.Is<ConfigurationManifestOperation>(operation =>
                operation.RequestedTenantCount == 2
                && operation.FailedTenantCount == 2),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_LockTimePreflightCountsOnlyDistinctTenantIndexes()
    {
        var fixture = new Fixture();
        int evaluation = 0;
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => evaluation++ == 0
                ? Valid(call.Arg<ConfigurationManifestApplyPlan>())
                : InvalidWithInstanceAndTenantErrors(
                    call.Arg<ConfigurationManifestApplyPlan>()));

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(tenantSlugs: ["primary", "secondary"])),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await fixture.FailureRecorder.Received(1).RecordAsync(
            Arg.Is<ConfigurationManifestOperation>(operation =>
                operation.RequestedTenantCount == 2
                && operation.FailedTenantCount == 2),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_TenantAppearingBeforeLockedPreflightIsSkippedWholesale()
    {
        var fixture = new Fixture();
        Guid existingTenantId = Guid.CreateVersion7();
        int evaluation = 0;
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                ConfigurationManifestApplyPlan plan =
                    call.Arg<ConfigurationManifestApplyPlan>();
                if (evaluation++ == 0)
                {
                    return Valid(plan);
                }

                return new ConfigurationManifestPreflightResult(
                    plan,
                    plan.Tenants.Select(tenant =>
                            new ConfigurationManifestPreflightTenant(
                                tenant,
                                ConfigurationManifestTenantDisposition.SkippedExisting,
                                existingTenantId))
                        .ToImmutableArray(),
                    []);
            });

        var result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(Source()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await fixture.TenantCreation.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
        await fixture.PolicyBoundary.DidNotReceiveWithAnyArgs()
            .ApplyTenantInCurrentTransactionAsync(default!, default);
        await fixture.Settings.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
        await fixture.Audit.Received(1).CreateAsync(
            Arg.Is<ConfigurationManifestOperation>(operation =>
                operation.CreatedTenantCount == 0
                && operation.SkippedExistingTenantCount == 1),
            Arg.Is<IReadOnlyCollection<ConfigurationManifestTenantResult>>(tenantResults =>
                tenantResults.Single().TenantId == existingTenantId
                && tenantResults.Single().Status
                    == ConfigurationManifestTenantResultStatus.SkippedExisting),
            Arg.Any<CancellationToken>());
        await Assert.That(fixture.Publisher.Notifications).IsEmpty();
        await Assert.That(fixture.Outbox.Messages).IsEmpty();
    }

    [Test]
    public async Task Handle_CommittedNoEffectReplayDoesNotAttemptEffectDelivery()
    {
        var fixture = new Fixture();
        ConfigurationManifestOperation? committed = null;
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                ConfigurationManifestApplyPlan plan =
                    call.Arg<ConfigurationManifestApplyPlan>();
                committed = ConfigurationManifestOperation.Create(
                    plan.OperationId,
                    ConfigurationManifestAuditMode.Bootstrap,
                    plan.ApiVersion,
                    plan.Kind,
                    plan.ManifestName,
                    plan.Digest,
                    ConfigurationManifestOperationStatus.Applied,
                    requestedTenantCount: 1,
                    createdTenantCount: 0,
                    skippedExistingTenantCount: 1,
                    failedTenantCount: 0,
                    reasonCode: null,
                    reason: null,
                    plan.OccurredAt,
                    plan.OccurredAt.AddSeconds(1),
                    instanceSectionDigest: plan.InstanceSectionDigest,
                    bootstrapGeneration: 1);
                return Valid(plan);
            });
        fixture.Audit.GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => committed);
        fixture.Audit.GetResultsByOperationIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(
            [
                ConfigurationManifestTenantResult.Create(
                    committed?.Id ?? Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    ConfigurationManifestTenantResultStatus.SkippedExisting,
                    [],
                    [],
                    DateTime.UtcNow)
            ]);

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(Source()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(fixture.Outbox.Messages).IsEmpty();
        await Assert.That(fixture.Publisher.Notifications).IsEmpty();
        await fixture.TenantCreation.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
    }

    [Test]
    public async Task Handle_LockTimeSingleTenantErrorPreservesPreflightFailure()
    {
        var fixture = new Fixture();
        int evaluation = 0;
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                ConfigurationManifestApplyPlan plan =
                    call.Arg<ConfigurationManifestApplyPlan>();
                if (evaluation++ == 0)
                {
                    return Valid(plan);
                }

                return new ConfigurationManifestPreflightResult(
                    plan,
                    [],
                    [
                        new ConfigurationManifestPreflightError(
                            ManifestIndex: 0,
                            EventSettingDefinitions.RequireApproval.Key,
                            ConfigurationManifestApplicationFailureCodes.SettingLocked,
                            "A locked setting cannot be applied.")
                    ]);
            });

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(tenantSlugs: ["primary", "secondary"])),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(ConfigurationManifestApplicationFailureCodes.SettingLocked);
        await Assert.That(result.Message)
            .IsEqualTo("A locked setting cannot be applied.");
        await fixture.FailureRecorder.Received(1).RecordAsync(
            Arg.Is<ConfigurationManifestOperation>(operation =>
                operation.ReasonCode
                    == ConfigurationManifestApplicationFailureCodes.SettingLocked
                && operation.FailedTenantCount == 1),
            Arg.Any<CancellationToken>());
        await fixture.TenantCreation.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
        await fixture.InstanceSettings.DidNotReceiveWithAnyArgs()
            .ApplyInCurrentTransactionAsync(default!, default);
    }

    [Test]
    public async Task Handle_PreflightIndexEqualToRequestedCountIsNotCountedAsFailedTenant()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                ConfigurationManifestApplyPlan plan =
                    call.Arg<ConfigurationManifestApplyPlan>();
                return new ConfigurationManifestPreflightResult(
                    plan,
                    [],
                    [
                        new ConfigurationManifestPreflightError(
                            ManifestIndex: plan.Tenants.Length,
                            "out-of-range",
                            ConfigurationManifestApplicationFailureCodes.SettingLocked,
                            "An invalid preflight index was returned.")
                    ]);
            });

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(tenantSlugs: ["primary", "secondary"])),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await fixture.FailureRecorder.Received(1).RecordAsync(
            Arg.Is<ConfigurationManifestOperation>(operation =>
                operation.RequestedTenantCount == 2
                && operation.FailedTenantCount == 0),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ValidateOnlyReturnsInMemorySuccessWithoutAnyWrites()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));

        var result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(ConfigurationManifestMode.ValidateOnly)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Configuration manifest validated.");
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(fixture.UnitOfWork.SerializableExecutionCount).IsEqualTo(0);
        await Assert.That(fixture.Lock.ExecutionCount).IsEqualTo(0);
        await fixture.Audit.DidNotReceiveWithAnyArgs()
            .CreateAsync(default!, default!, default);
        await fixture.FailureRecorder.DidNotReceiveWithAnyArgs()
            .RecordAsync(default!, default);
        await fixture.TenantCreation.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
        await fixture.InstanceSettings.DidNotReceiveWithAnyArgs()
            .ApplyInCurrentTransactionAsync(default!, default);
        await fixture.PolicyBoundary.DidNotReceiveWithAnyArgs()
            .ApplyTenantInCurrentTransactionAsync(default!, default);
        await fixture.PaidPolicyBoundary.DidNotReceiveWithAnyArgs()
            .ReviseTenantInCurrentTransactionAsync(default!, default);
        await fixture.Settings.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
        await Assert.That(fixture.Outbox.PendingReadCount).IsEqualTo(0);
        await Assert.That(fixture.Outbox.Messages).IsEmpty();
        await Assert.That(fixture.Publisher.Notifications).IsEmpty();
    }

    [Test]
    public async Task Handle_ValidateOnlyPreflightFailureDoesNotPersistFailure()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => InvalidWithInstanceAndTenantErrors(
                call.Arg<ConfigurationManifestApplyPlan>()));

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(
                    ConfigurationManifestMode.ValidateOnly,
                    tenantSlugs: ["primary", "secondary"])),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(fixture.UnitOfWork.SerializableExecutionCount).IsEqualTo(0);
        await Assert.That(fixture.Lock.ExecutionCount).IsEqualTo(0);
        await fixture.Audit.DidNotReceiveWithAnyArgs()
            .CreateAsync(default!, default!, default);
        await fixture.FailureRecorder.DidNotReceiveWithAnyArgs()
            .RecordAsync(default!, default);
        await Assert.That(fixture.Outbox.PendingReadCount).IsEqualTo(0);
        await Assert.That(fixture.Outbox.Messages).IsEmpty();
    }

    [Test]
    public async Task Handle_PostCommitEffectFailureDoesNotRecordFalseRollbackAudit()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));
        fixture.Publisher.Failure =
            new InvalidOperationException("simulated post-commit notification failure");

        await Assert.That(() => fixture.Handler.Handle(
                new ApplyConfigurationManifestCommand(Source()),
                CancellationToken.None))
            .Throws<AggregateException>();

        await fixture.Audit.Received(1).CreateAsync(
            Arg.Is<ConfigurationManifestOperation>(operation =>
                operation.Status == ConfigurationManifestOperationStatus.Applied),
            Arg.Any<IReadOnlyCollection<ConfigurationManifestTenantResult>>(),
            Arg.Any<CancellationToken>());
        await fixture.FailureRecorder.DidNotReceiveWithAnyArgs()
            .RecordAsync(default!, default);
        await Assert.That(fixture.Publisher.AttemptCount).IsEqualTo(2);
    }

    [Test]
    public async Task Handle_RetryFindsAlreadyCommittedOperationByIdentity()
    {
        var fixture = new Fixture();
        ConfigurationManifestOperation? committed = null;
        IReadOnlyList<ConfigurationManifestTenantResult> committedResults = [];
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                ConfigurationManifestApplyPlan plan =
                    call.Arg<ConfigurationManifestApplyPlan>();
                committed = ConfigurationManifestOperation.Create(
                    plan.OperationId,
                    ConfigurationManifestAuditMode.Bootstrap,
                    plan.ApiVersion,
                    plan.Kind,
                    plan.ManifestName,
                    plan.Digest,
                    ConfigurationManifestOperationStatus.Applied,
                    requestedTenantCount: 1,
                    createdTenantCount: 1,
                    skippedExistingTenantCount: 0,
                    failedTenantCount: 0,
                    reasonCode: null,
                    reason: null,
                    plan.OccurredAt,
                    plan.OccurredAt.AddSeconds(1),
                    instanceSectionDigest: plan.InstanceSectionDigest,
                    bootstrapGeneration: 1);
                committedResults =
                [
                    ConfigurationManifestTenantResult.Create(
                        plan.OperationId,
                        plan.Tenants[0].PlannedTenantId,
                        ConfigurationManifestTenantResultStatus.Created,
                        plan.Tenants[0].ChangedSettingKeyNames,
                        plan.Tenants[0].ChangedDocumentKeyNames,
                        plan.OccurredAt.AddSeconds(1))
                ];
                fixture.Outbox.Seed(ConfigurationManifestEffectOutbox.Create(
                    plan.EffectOutboxId,
                    plan.OperationId,
                    plan.OccurredAt.AddSeconds(1)));
                return Valid(plan);
            });
        fixture.Audit.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => committed);
        fixture.Audit.GetResultsByOperationIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => committedResults);

        var result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(Source()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(committed!.Id);
        await fixture.Preflight.Received(1).EvaluateAsync(
            Arg.Any<ConfigurationManifestApplyPlan>(),
            Arg.Any<CancellationToken>());
        await fixture.TenantCreation.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
        await fixture.Audit.DidNotReceiveWithAnyArgs()
            .CreateAsync(default!, default!, default);
        await Assert.That(fixture.Publisher.Notifications.Count).IsEqualTo(2);
        fixture.SettingsResolver.Received(1).InvalidateCache(
            SettingScope.Tenant,
            Arg.Any<Guid>());
        fixture.Documents.Received(1).InvalidateTenantDocumentCache(
            Arg.Any<Guid>(),
            SettingsDocumentKeys.Tenant.Branding);
    }

    [Test]
    public async Task Handle_InstanceOnlyCommittedReplayDeliversPendingEffects()
    {
        var fixture = new Fixture();
        ConfigurationManifestOperation? committed = null;
        IReadOnlyList<ConfigurationManifestTenantResult> results = [];
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                ConfigurationManifestApplyPlan plan =
                    call.Arg<ConfigurationManifestApplyPlan>();
                DateTime completedAt = plan.OccurredAt.AddSeconds(1);
                committed = ConfigurationManifestOperation.Create(
                    plan.OperationId,
                    ConfigurationManifestAuditMode.Bootstrap,
                    plan.ApiVersion,
                    plan.Kind,
                    plan.ManifestName,
                    plan.Digest,
                    ConfigurationManifestOperationStatus.Applied,
                    requestedTenantCount: 1,
                    createdTenantCount: 0,
                    skippedExistingTenantCount: 1,
                    failedTenantCount: 0,
                    reasonCode: null,
                    reason: null,
                    plan.OccurredAt,
                    completedAt,
                    instanceSectionDigest: plan.InstanceSectionDigest,
                    bootstrapGeneration: 1,
                    instanceChangedSettingKeyNames:
                        plan.Instance.ChangedSettingKeyNames);
                results =
                [
                    ConfigurationManifestTenantResult.Create(
                        plan.OperationId,
                        plan.Tenants[0].PlannedTenantId,
                        ConfigurationManifestTenantResultStatus
                            .SkippedExisting,
                        [],
                        [],
                        completedAt)
                ];
                fixture.Outbox.Seed(
                    ConfigurationManifestEffectOutbox.Create(
                        plan.EffectOutboxId,
                        plan.OperationId,
                        completedAt));
                return Valid(plan);
            });
        fixture.Audit.GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => committed);
        fixture.Audit.GetResultsByOperationIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => results);

        BaseCommandResponse<Guid> result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(
                Source(includeInstanceSetting: true)),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(fixture.Outbox.Messages.Single().Status)
            .IsEqualTo(OutboxMessageStatus.Completed);
        fixture.SettingsResolver.Received(1)
            .InvalidateCache(SettingScope.Instance);
        await fixture.TenantCreation.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
    }

    [Test]
    public async Task Handle_RetryableLockDelegateReplaysCommittedEffectsWithoutDuplicateWrites()
    {
        var fixture = new Fixture();
        fixture.Lock.RetryDelegateOnce = true;
        ConfigurationManifestOperation? committed = null;
        IReadOnlyList<ConfigurationManifestTenantResult> committedResults = [];
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));
        fixture.Audit.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => committed);
        fixture.Audit.GetResultsByOperationIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => committedResults);
        fixture.Audit.CreateAsync(
                Arg.Any<ConfigurationManifestOperation>(),
                Arg.Any<IReadOnlyCollection<ConfigurationManifestTenantResult>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                committed = call.Arg<ConfigurationManifestOperation>();
                committedResults =
                    call.Arg<IReadOnlyCollection<ConfigurationManifestTenantResult>>()
                        .ToArray();
                return committed;
            });

        var result = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(Source()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(fixture.Lock.DelegateExecutionCount).IsEqualTo(2);
        await fixture.TenantCreation.Received(1).CreateInCurrentTransactionAsync(
            Arg.Any<TenantCreationRequest>(),
            Arg.Any<CancellationToken>());
        await fixture.Audit.Received(1).CreateAsync(
            Arg.Any<ConfigurationManifestOperation>(),
            Arg.Any<IReadOnlyCollection<ConfigurationManifestTenantResult>>(),
            Arg.Any<CancellationToken>());
        await Assert.That(fixture.Publisher.Notifications.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Handle_CancellationInsideLockPropagatesWithoutFailureAudit()
    {
        var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();
        int evaluation = 0;
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (evaluation++ == 0)
                {
                    cancellation.Cancel();
                    return Valid(call.Arg<ConfigurationManifestApplyPlan>());
                }

                throw new OperationCanceledException(cancellation.Token);
            });

        await Assert.That(() => fixture.Handler.Handle(
                new ApplyConfigurationManifestCommand(Source()),
                cancellation.Token))
            .Throws<OperationCanceledException>();

        await fixture.FailureRecorder.DidNotReceiveWithAnyArgs()
            .RecordAsync(default!, default);
        await fixture.Audit.DidNotReceiveWithAnyArgs()
            .CreateAsync(default!, default!, default);
    }

    [Test]
    public async Task Handle_CancellationAfterCommitAttemptsAllEffectsAndPreservesAppliedAudit()
    {
        var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));
        fixture.Publisher.OnFirstPublish = cancellation.Cancel;

        await Assert.That(() => fixture.Handler.Handle(
                new ApplyConfigurationManifestCommand(Source()),
                cancellation.Token))
            .Throws<OperationCanceledException>();

        await fixture.Audit.Received(1).CreateAsync(
            Arg.Is<ConfigurationManifestOperation>(operation =>
                operation.Status == ConfigurationManifestOperationStatus.Applied),
            Arg.Any<IReadOnlyCollection<ConfigurationManifestTenantResult>>(),
            Arg.Any<CancellationToken>());
        await fixture.FailureRecorder.DidNotReceiveWithAnyArgs()
            .RecordAsync(default!, default);
        await Assert.That(fixture.Publisher.AttemptCount).IsEqualTo(2);
        fixture.SettingsResolver.Received(1).InvalidateCache(
            SettingScope.Tenant,
            Arg.Any<Guid>());
        fixture.Documents.Received(1).InvalidateTenantDocumentCache(
            Arg.Any<Guid>(),
            SettingsDocumentKeys.Tenant.Branding);
    }

    [Test]
    public async Task Handle_LaterInvocationDrainsFailedEffectOutboxBeforeNewPreflight()
    {
        var fixture = new Fixture();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Valid(call.Arg<ConfigurationManifestApplyPlan>()));
        fixture.Publisher.Failure =
            new InvalidOperationException("simulated first delivery failure");

        await Assert.That(() => fixture.Handler.Handle(
                new ApplyConfigurationManifestCommand(Source()),
                CancellationToken.None))
            .Throws<AggregateException>();

        OutboxMessage pending = fixture.Outbox.Messages.Single();
        await Assert.That(pending.Status).IsEqualTo(OutboxMessageStatus.Pending);
        fixture.Publisher.Failure = null;
        fixture.Publisher.Notifications.Clear();
        fixture.Preflight.EvaluateAsync(
                Arg.Any<ConfigurationManifestApplyPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                ConfigurationManifestApplyPlan plan =
                    call.Arg<ConfigurationManifestApplyPlan>();
                return new ConfigurationManifestPreflightResult(
                    plan,
                    [],
                    [
                        new ConfigurationManifestPreflightError(
                            ManifestIndex: 0,
                            EventSettingDefinitions.RequireApproval.Key,
                            ConfigurationManifestApplicationFailureCodes.SettingLocked,
                            "A locked setting cannot be applied.")
                    ]);
            });

        var second = await fixture.Handler.Handle(
            new ApplyConfigurationManifestCommand(Source()),
            CancellationToken.None);

        await Assert.That(second.IsSuccess).IsFalse();
        await Assert.That(pending.Status).IsEqualTo(OutboxMessageStatus.Completed);
        await Assert.That(fixture.Publisher.Notifications.Count).IsEqualTo(2);
    }

    private static ConfigurationManifestPreflightResult Valid(
        ConfigurationManifestApplyPlan plan)
    {
        if (plan.BootstrapState is { } bootstrapState)
        {
            if (!string.Equals(
                    bootstrapState.InstanceSectionDigest,
                    plan.InstanceSectionDigest,
                    StringComparison.Ordinal))
            {
                return new ConfigurationManifestPreflightResult(
                    plan,
                    [],
                    [
                        new ConfigurationManifestPreflightError(
                            ManifestIndex: -1,
                            "spec.instance",
                            ConfigurationManifestApplicationFailureCodes
                                .InstanceAlreadyBootstrapped,
                            "The instance section was already bootstrapped.")
                    ]);
            }

            plan = plan with
            {
                Instance = plan.Instance with
                {
                    GuardedSettings = [],
                    UnguardedSettings = [],
                    PaidEventPolicy = plan.Instance.PaidEventPolicy is null
                        ? null
                        : plan.Instance.PaidEventPolicy with
                        {
                            ProposedRevision = null,
                            ExpectedActivePolicyVersion = null
                        },
                    ChangedSettingKeyNames = [],
                    ChangedDocumentKeyNames = []
                }
            };
        }

        ConfigurationManifestApplyPlan boundPlan =
            plan.Instance.PaidEventPolicy is null
                ? plan
                : plan with
                {
                    Instance = plan.Instance with
                    {
                        PaidEventPolicy =
                            plan.Instance.PaidEventPolicy with
                            {
                                ExpectedActivePolicyVersion = 1
                            }
                    }
                };
        return new ConfigurationManifestPreflightResult(
            boundPlan,
            boundPlan.Tenants.Select(tenant => new ConfigurationManifestPreflightTenant(
                    tenant,
                    ConfigurationManifestTenantDisposition.Create,
                    tenant.PlannedTenantId))
                .ToImmutableArray(),
            []);
    }

    private static ConfigurationManifestPreflightResult
        InvalidWithInstanceAndTenantErrors(ConfigurationManifestApplyPlan plan) =>
        new(
            plan,
            [],
            [
                new ConfigurationManifestPreflightError(
                    ManifestIndex: -1,
                    "spec.instance",
                    ConfigurationManifestApplicationFailureCodes.SettingLocked,
                    "An instance setting is locked."),
                new ConfigurationManifestPreflightError(
                    ManifestIndex: 0,
                    "tenant-setting-a",
                    ConfigurationManifestApplicationFailureCodes.SettingLocked,
                    "A tenant setting is locked."),
                new ConfigurationManifestPreflightError(
                    ManifestIndex: 0,
                    "tenant-setting-b",
                    ConfigurationManifestApplicationFailureCodes.SettingLocked,
                    "A tenant setting is locked."),
                new ConfigurationManifestPreflightError(
                    ManifestIndex: 1,
                    "tenant-setting-c",
                    ConfigurationManifestApplicationFailureCodes.SettingLocked,
                    "A tenant setting is locked."),
                new ConfigurationManifestPreflightError(
                    ManifestIndex: 99,
                    "out-of-range",
                    ConfigurationManifestApplicationFailureCodes.SettingLocked,
                    "An invalid preflight index was returned.")
            ]);

    private static ConfigurationManifestReadResult Source(
        ConfigurationManifestMode mode =
            ConfigurationManifestMode.Bootstrap,
        bool includePaidPolicy = false,
        bool includeInstancePaidPolicy = false,
        bool includeInstanceSetting = false,
        bool includeReportingIntake = false,
        bool instancePaymentsEnabled = true,
        char digestCharacter = 'a',
        IReadOnlyList<string>? tenantSlugs = null)
    {
        var settings = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            [EventSettingDefinitions.RequireApproval.Key] = Json("true"),
            [PublicExperienceSettingDefinitions.EventCatalogLabel.Key] = Json("\"Community Events\"")
        };
        if (includeReportingIntake)
        {
            settings[GovernanceSettingKeys.EventReporting.IntakeEnabled] =
                Json("true");
        }
        var documents = new Dictionary<string, ConfigurationManifestDocumentV1Alpha1>(
            StringComparer.Ordinal);
        if (includePaidPolicy)
        {
            documents[ConfigurationManifestDocumentKeys.TenantPaidEventPolicy] = new()
            {
                SchemaVersion = 1,
                Payload = Json(
                    """
                    {
                      "isPaymentsEnabled": false,
                      "allowedOrganizerKindIds": [2],
                      "requiresLocalVerification": true,
                      "allowedCurrencyCodes": ["USD"],
                      "defaultCurrencyCode": "USD",
                      "refundProtectionIds": [1, 2, 3, 4, 5, 6, 7],
                      "currencyRiskLimits": [],
                      "requiresFirstPaidEventReview": true,
                      "farFutureReviewThresholdDays": 90
                    }
                    """)
            };
        }

        var instanceDocuments =
            new Dictionary<string, ConfigurationManifestDocumentV1Alpha1>(
                StringComparer.Ordinal);
        if (includeInstancePaidPolicy)
        {
            instanceDocuments[
                ConfigurationManifestDocumentKeys.InstancePaidEventPolicy] = new()
            {
                SchemaVersion = 1,
                Payload = Json(
                    $$"""
                    {
                      "isPaymentsEnabled": {{instancePaymentsEnabled.ToString().ToLowerInvariant()}},
                      "allowedOrganizerKindIds": [2],
                      "requiresLocalVerification": true,
                      "allowedCurrencyCodes": ["USD"],
                      "defaultCurrencyCode": "USD",
                      "refundProtectionIds": [1, 2, 3, 4, 5, 6, 7],
                      "currencyRiskLimits": [],
                      "requiresFirstPaidEventReview": true,
                      "farFutureReviewThresholdDays": 90
                    }
                    """)
            };
        }

        var manifest = new ConfigurationManifestV1Alpha1
        {
            Schema = ConfigurationManifestContractMetadata.SchemaId,
            ApiVersion = ConfigurationManifestContractMetadata.ApiVersion,
            Kind = ConfigurationManifestContractMetadata.Kind,
            Metadata = new ConfigurationManifestMetadataV1Alpha1 { Name = "deployment" },
            Spec = new ConfigurationManifestSpecV1Alpha1
            {
                Instance = new ConfigurationManifestInstanceV1Alpha1
                {
                    Settings = includeInstanceSetting
                        ? new Dictionary<string, JsonElement>(
                            StringComparer.Ordinal)
                        {
                            [AppearanceSettingDefinitions.DefaultThemeMode.Key]
                                = Json("\"dark\"")
                        }
                        : new Dictionary<string, JsonElement>(
                            StringComparer.Ordinal),
                    Documents = instanceDocuments
                },
                Tenants = (tenantSlugs ?? ["primary"])
                    .Select(slug => new ConfigurationManifestTenantV1Alpha1
                    {
                        Metadata =
                            new ConfigurationManifestTenantMetadataV1Alpha1
                            {
                                Name = slug
                            },
                        Spec = new ConfigurationManifestTenantSpecV1Alpha1
                        {
                            DisplayName = $"{slug} Community",
                            Settings = settings,
                            Documents = documents
                        }
                    })
                    .ToArray()
            }
        };
        return new ConfigurationManifestReadResult(
            manifest,
            mode,
            new string(
                digestCharacter,
                ConfigurationManifestOperation.DigestLength),
            ByteLength: 512);
    }

    private static JsonElement Json(string value)
    {
        using JsonDocument document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            Preflight = Substitute.For<IConfigurationManifestPreflight>();
            TenantCreation = Substitute.For<ITenantCreationService>();
            PolicyBoundary = Substitute.For<IPublicationPolicyMutationBoundary>();
            PaidPolicyBoundary = Substitute.For<IPaidEventPolicyMutationBoundary>();
            InstanceSettings = Substitute.For<
                IConfigurationManifestInstanceSettingMutationBoundary>();
            Settings = Substitute.For<
                IConfigurationManifestTenantSettingMutationBoundary>();
            Audit = Substitute.For<IConfigurationManifestOperationRepository>();
            FailureRecorder = Substitute.For<IConfigurationManifestFailureRecorder>();
            Documents = Substitute.For<ITypedSettingsDocumentResolver>();
            SettingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
            Lock = new RecordingSettingMutationLock();
            UnitOfWork = new RecordingUnitOfWork();
            Publisher = new RecordingPublisher(Lock);
            Outbox = new RecordingManifestEffectOutboxRepository();
            TenantCreation.CreateInCurrentTransactionAsync(
                    Arg.Any<TenantCreationRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    TenantCreationRequest request = call.Arg<TenantCreationRequest>();
                    var tenant = new Tenant
                    {
                        Id = request.TenantId,
                        FullName = request.FullName,
                        Slug = request.Slug,
                        TenantStatusId = request.TenantStatusId,
                        TenantStatus = null!
                    };
                    TenantSettingsDocument branding = TenantSettingsDocument.Create(
                        request.TenantId,
                        SettingsDocumentKeys.Tenant.Branding,
                        request.Branding.SchemaVersion,
                        request.Branding.DefaultsVersion,
                        request.Branding.PayloadJson);
                    TenantSettingsDocument identity = TenantSettingsDocument.Create(
                        request.TenantId,
                        SettingsDocumentKeys.Tenant.DirectoryOperatorIdentity,
                        request.DirectoryOperatorIdentity.SchemaVersion,
                        request.DirectoryOperatorIdentity.DefaultsVersion,
                        request.DirectoryOperatorIdentity.PayloadJson);
                    return new TenantCreationOutcome(tenant, branding, identity);
                });
            PolicyBoundary.ApplyTenantInCurrentTransactionAsync(
                    Arg.Any<PublicationPolicyTenantMutationRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(new PublicationPolicyMutationResult(
                    Success: true,
                    FailureCode: null,
                    Message: "Updated.",
                    DeferredNotifications: []));
            InstanceSettings.ApplyInCurrentTransactionAsync(
                    Arg.Any<ConfigurationManifestInstanceSettingMutationInput>(),
                    Arg.Any<CancellationToken>())
                .Returns(new ConfigurationManifestInstanceSettingMutationResult(
                    Success: true,
                    FailureCode: null,
                    Message: "Updated.",
                    DeferredNotifications: []));
            PaidPolicyBoundary.ReviseTenantInCurrentTransactionAsync(
                    Arg.Any<TenantPaidEventPolicyMutationInput>(),
                    Arg.Any<CancellationToken>())
                .Returns(new PaidEventPolicyMutationResult(
                    Success: true,
                    Guid.CreateVersion7(),
                    FailureCode: null,
                    "Paid-event policy revised.",
                    []));
            PaidPolicyBoundary.ReviseInstanceInCurrentTransactionAsync(
                    Arg.Any<InstancePaidEventPolicyMutationInput>(),
                    Arg.Any<CancellationToken>())
                .Returns(new PaidEventPolicyMutationResult(
                    Success: true,
                    Guid.CreateVersion7(),
                    FailureCode: null,
                    "Paid-event policy revised.",
                    []));
            Audit.CreateAsync(
                    Arg.Any<ConfigurationManifestOperation>(),
                    Arg.Any<IReadOnlyCollection<ConfigurationManifestTenantResult>>(),
                    Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    _persistedOperation = call.Arg<ConfigurationManifestOperation>();
                    _persistedResults =
                        call.Arg<IReadOnlyCollection<ConfigurationManifestTenantResult>>()
                            .ToArray();
                    return _persistedOperation;
                });
            Audit.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(call => _persistedOperation?.Id == call.Arg<Guid>()
                    ? _persistedOperation
                    : null);
            Audit.GetLatestAppliedBootstrapAsync(
                    Arg.Any<CancellationToken>())
                .Returns(_ => _persistedOperation?.Status
                    == ConfigurationManifestOperationStatus.Applied
                    ? _persistedOperation
                    : null);
            Audit.GetResultsByOperationIdAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<CancellationToken>())
                .Returns(call => _persistedOperation?.Id == call.Arg<Guid>()
                    ? _persistedResults
                    : []);
            var effectDispatcher = new ConfigurationManifestEffectDispatcher(
                Audit,
                SettingsResolver,
                Documents,
                Publisher);
            Handler = new ApplyConfigurationManifestCommandHandler(
                Preflight,
                Lock,
                UnitOfWork,
                TenantCreation,
                PolicyBoundary,
                PaidPolicyBoundary,
                InstanceSettings,
                Settings,
                Audit,
                FailureRecorder,
                new ConfigurationManifestEffectDelivery(Outbox, effectDispatcher),
                Substitute.For<ILogger<ApplyConfigurationManifestCommandHandler>>());
        }

        private ConfigurationManifestOperation? _persistedOperation;
        private IReadOnlyList<ConfigurationManifestTenantResult> _persistedResults = [];

        public IConfigurationManifestPreflight Preflight { get; }
        public ITenantCreationService TenantCreation { get; }
        public IPublicationPolicyMutationBoundary PolicyBoundary { get; }
        public IPaidEventPolicyMutationBoundary PaidPolicyBoundary { get; }
        public IConfigurationManifestInstanceSettingMutationBoundary
            InstanceSettings { get; }
        public IConfigurationManifestTenantSettingMutationBoundary Settings { get; }
        public IConfigurationManifestOperationRepository Audit { get; }
        public IConfigurationManifestFailureRecorder FailureRecorder { get; }
        public IHierarchicalSettingsResolver SettingsResolver { get; }
        public RecordingManifestEffectOutboxRepository Outbox { get; }
        public ITypedSettingsDocumentResolver Documents { get; }
        public ConfigurationManifestOperation? PersistedOperation =>
            _persistedOperation;
        public RecordingSettingMutationLock Lock { get; }
        public RecordingUnitOfWork UnitOfWork { get; }
        public RecordingPublisher Publisher { get; }
        public ApplyConfigurationManifestCommandHandler Handler { get; }
    }

    private sealed class RecordingManifestEffectOutboxRepository
        : IConfigurationManifestEffectOutboxRepository
    {
        private readonly Dictionary<Guid, OutboxMessage> _messages = [];

        public IReadOnlyCollection<OutboxMessage> Messages => _messages.Values;
        public int PendingReadCount { get; private set; }

        public void Seed(OutboxMessage message) => _messages[message.Id] = message;

        public Task<OutboxMessage> Create(OutboxMessage message)
        {
            _messages.Add(message.Id, message);
            return Task.FromResult(message);
        }

        public Task<OutboxMessage?> GetByIdAsync(
            Guid messageId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_messages.GetValueOrDefault(messageId));

        public Task<IReadOnlyList<OutboxMessage>> GetPendingManifestEffectsAsync(
            int batchSize,
            CancellationToken cancellationToken)
        {
            PendingReadCount++;
            return Task.FromResult<IReadOnlyList<OutboxMessage>>(
                _messages.Values
                    .Where(message => message.Status == OutboxMessageStatus.Pending)
                    .OrderBy(message => message.CreatedAt)
                    .Take(batchSize)
                    .ToArray());
        }

        public Task<DateTime?> TryClaimForProcessing(
            Guid id,
            DateTime claimedAt,
            CancellationToken cancellationToken)
        {
            if (!_messages.TryGetValue(id, out OutboxMessage? message)
                || message.Status != OutboxMessageStatus.Pending)
            {
                return Task.FromResult<DateTime?>(null);
            }

            message.Status = OutboxMessageStatus.Processing;
            message.NextRetryAt = claimedAt.AddMinutes(5);
            return Task.FromResult(message.NextRetryAt);
        }

        public Task<bool> MarkAsCompleted(
            Guid id,
            DateTime processingLeaseExpiresAt,
            CancellationToken cancellationToken)
        {
            if (!_messages.TryGetValue(id, out OutboxMessage? message)
                || message.Status != OutboxMessageStatus.Processing
                || message.NextRetryAt != processingLeaseExpiresAt)
            {
                return Task.FromResult(false);
            }

            message.Status = OutboxMessageStatus.Completed;
            message.ProcessedAt = DateTime.UtcNow;
            message.NextRetryAt = null;
            return Task.FromResult(true);
        }

        public Task<OutboxFailureTransition> MarkAsFailed(
            Guid id,
            DateTime processingLeaseExpiresAt,
            string error,
            bool isRetryable,
            int retryDelaySeconds,
            DateTime failedAt,
            CancellationToken cancellationToken)
        {
            OutboxMessage message = _messages[id];
            message.Status = OutboxMessageStatus.Pending;
            message.RetryCount++;
            message.LastError = error;
            message.NextRetryAt = failedAt.AddSeconds(retryDelaySeconds);
            return Task.FromResult(OutboxFailureTransition.RetryScheduled);
        }
    }

    private sealed class RecordingSettingMutationLock : ISettingMutationLock
    {
        public bool IsInside { get; private set; }
        public int ExecutionCount { get; private set; }
        public int DelegateExecutionCount { get; private set; }
        public bool RetryDelegateOnce { get; set; }
        public IReadOnlyList<string> Keys { get; private set; } = [];

        public Task<T> ExecuteAsync<T>(
            string canonicalSettingKey,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) =>
            ExecuteManyAsync([canonicalSettingKey], operation, cancellationToken);

        public async Task<T> ExecuteManyAsync<T>(
            IEnumerable<string> canonicalSettingKeys,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            Keys = canonicalSettingKeys.ToArray();
            ExecutionCount++;
            IsInside = true;
            try
            {
                DelegateExecutionCount++;
                T result = await operation(cancellationToken);
                if (!RetryDelegateOnce)
                {
                    return result;
                }

                DelegateExecutionCount++;
                return await operation(cancellationToken);
            }
            finally
            {
                IsInside = false;
            }
        }

        public async Task<T> ExecuteOrderedGroupsAsync<T>(
            IEnumerable<IEnumerable<string>> canonicalSettingKeyGroups,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            Keys = canonicalSettingKeyGroups.SelectMany(group => group)
                .ToArray();
            ExecutionCount++;
            IsInside = true;
            try
            {
                DelegateExecutionCount++;
                T result = await operation(cancellationToken);
                if (!RetryDelegateOnce)
                {
                    return result;
                }

                DelegateExecutionCount++;
                return await operation(cancellationToken);
            }
            finally
            {
                IsInside = false;
            }
        }
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int SerializableExecutionCount { get; private set; }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default) =>
            await operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) =>
            operation(ct);

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
        {
            SerializableExecutionCount++;
            return operation(ct);
        }
    }

    private sealed class RecordingPublisher(RecordingSettingMutationLock mutationLock) : IPublisher
    {
        public List<INotification> Notifications { get; } = [];
        public bool PublishedInsideLock { get; private set; }
        public Exception? Failure { get; set; }
        public Action? OnFirstPublish { get; set; }
        public int AttemptCount { get; private set; }

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            AttemptCount++;
            if (AttemptCount == 1)
            {
                OnFirstPublish?.Invoke();
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null)
            {
                throw Failure;
            }

            PublishedInsideLock |= mutationLock.IsInside;
            Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish(
            object notification,
            CancellationToken cancellationToken = default) =>
            Publish((INotification)notification, cancellationToken);
    }
}
