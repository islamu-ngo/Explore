// ABOUTME: Focused tests for managed tenant provisioning validation, idempotency, lifecycle, and mode rejection.
// ABOUTME: Proves SingleTenant fails before scheduling or execution and canonical request hashes stay stable.

using System.Text.Json;
using System.Text.Json.Nodes;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.ManagedProviderProvisioning;
using Explore.Application.DTOs.Management;
using Explore.Application.DTOs.Management.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.ControlPlane.Plans;
using Explore.Application.Features.ManagedProviderProvisioning;
using Explore.Application.Features.Management;
using Explore.Application.Features.Management.Handlers;
using Explore.Application.Features.Management.Requests;
using Explore.Application.Management;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Modules;
using Explore.Domain.Settings.Documents.Payloads;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Management;

public sealed class ManagedTenantProvisioningTests
{
    [Test]
    public async Task Schedule_WhenSingleTenant_RejectsBeforePolicyOrOutboxMutation()
    {
        var modeProvider = Substitute.For<IDeploymentModeProvider>();
        modeProvider.GetCurrentModeAsync(Arg.Any<CancellationToken>())
            .Returns(DeploymentMode.SingleTenant);
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        var tenantRepository = Substitute.For<ITenantRepository>();
        var planRepository = Substitute.For<ITenantPlanRepository>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var options = Microsoft.Extensions.Options.Options.Create(new ManagedControlPlaneOptions
        {
            Enabled = true,
            MaximumTenantCount = 10
        });
        var systemSettingRepository = Substitute.For<ISystemSettingRepository>();
        var preflight = new ManagedTenantProvisioningPreflight(
            tenantRepository,
            planRepository,
            Substitute.For<IModuleDefinitionRepository>(),
            Substitute.For<ITenantSettingRepository>(),
            systemSettingRepository,
            Substitute.For<ITenantBrandingSettingsDocumentLockService>(),
            new TenantPlanStorageQuotaCeilingPolicy(systemSettingRepository),
            options);
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        var handler = new ScheduleManagedTenantProvisioningCommandHandler(
            modeProvider,
            Substitute.For<IManagedControlPlaneRegistrationRepository>(),
            operationRepository,
            Substitute.For<IExternalBindingRepository>(),
            tenantRepository,
            outboxRepository,
            Substitute.For<ISettingMutationLock>(),
            new TenantActivationCapacityPolicy(
                bootstrapRepository,
                tenantRepository,
                operationRepository,
                options),
            preflight);

        var result = await handler.Handle(
            new ScheduleManagedTenantProvisioningCommand(Guid.CreateVersion7(), CreateRequest()),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("tenant_provisioning_requires_multi_tenant");
        await planRepository.DidNotReceive().GetVersionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task Schedule_WhenCachedModeIsMultiTenantButPersistedModeChanged_RejectsInsideSharedLock()
    {
        Guid managedInstanceId = Guid.CreateVersion7();
        var modeProvider = Substitute.For<IDeploymentModeProvider>();
        modeProvider.GetCurrentModeAsync(Arg.Any<CancellationToken>())
            .Returns(DeploymentMode.MultiTenant);
        var registrationRepository = Substitute.For<IManagedControlPlaneRegistrationRepository>();
        registrationRepository.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(new ManagedControlPlaneRegistration
        {
            Id = Guid.CreateVersion7(),
            ManagedInstanceId = managedInstanceId,
            EventInstanceId = Guid.CreateVersion7(),
            ControlPlaneEndpoint = "https://control.example.test",
            ManagementApiVersion = ManagedControlPlaneContract.ManagementApiVersion,
            EventVersion = "1.0.0",
            DeploymentMode = DeploymentMode.MultiTenant,
            RequestHash = new string('a', 64),
            EventToControlPlaneKeyId = "event-key",
            EventToControlPlaneSecretHash = "event-hash",
            ControlPlaneToEventKeyId = "control-key",
            ControlPlaneToEventSecretHash = "control-hash",
            CredentialSecretBindingId = Guid.CreateVersion7(),
            EventToControlPlaneCredentialExpiresAt = DateTime.UtcNow.AddDays(1),
            ControlPlaneToEventCredentialExpiresAt = DateTime.UtcNow.AddDays(1),
            Status = ManagedControlPlaneRegistrationStatus.Registered,
            CreatedAt = DateTime.UtcNow
        });
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetTenantBySlug("tenant-one").Returns((Tenant?)null);
        var planRepository = Substitute.For<ITenantPlanRepository>();
        Guid planVersionId = Guid.Parse("01980000-0000-7000-8000-000000000001");
        planRepository.GetVersionAsync(planVersionId, Arg.Any<CancellationToken>()).Returns(new TenantPlanVersion
        {
            Id = planVersionId,
            TenantPlanId = Guid.CreateVersion7(),
            TenantPlan = new TenantPlan
            {
                Id = Guid.CreateVersion7(),
                Key = "standard",
                DisplayName = "Standard"
            },
            VersionNumber = 1,
            TenantPlanStatusId = (int)TenantPlanStatusEnum.Published,
            CurrencyCode = "EUR",
            BillingPeriod = "month",
            IsActiveForProvisioning = true
        });
        var moduleRepository = Substitute.For<IModuleDefinitionRepository>();
        moduleRepository.GetActiveByKeysAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ModuleDefinition>());
        var brandingLockService = Substitute.For<ITenantBrandingSettingsDocumentLockService>();
        brandingLockService.GetLockStateAsync(Arg.Any<CancellationToken>())
            .Returns(TenantBrandingSettingsDocumentLockState.AllowAll);
        brandingLockService.ValidateAllowedChanges(
                Arg.Any<BrandingSettings>(),
                Arg.Any<BrandingSettings>(),
                Arg.Any<TenantBrandingSettingsDocumentLockState>())
            .Returns(Array.Empty<string>());
        var options = Microsoft.Extensions.Options.Options.Create(new ManagedControlPlaneOptions
        {
            Enabled = true,
            MaximumTenantCount = 10
        });
        var preflight = new ManagedTenantProvisioningPreflight(
            tenantRepository,
            planRepository,
            moduleRepository,
            Substitute.For<ITenantSettingRepository>(),
            Substitute.For<ISystemSettingRepository>(),
            brandingLockService,
            new TenantPlanStorageQuotaCeilingPolicy(Substitute.For<ISystemSettingRepository>()),
            options);
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = Guid.CreateVersion7(),
            IsCompleted = true,
            SelectedDeploymentMode = DeploymentMode.SingleTenant.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mutationLock = new RecordingSettingMutationLock();
        var handler = new ScheduleManagedTenantProvisioningCommandHandler(
            modeProvider,
            registrationRepository,
            operationRepository,
            Substitute.For<IExternalBindingRepository>(),
            tenantRepository,
            outboxRepository,
            mutationLock,
            new TenantActivationCapacityPolicy(
                bootstrapRepository,
                tenantRepository,
                operationRepository,
                options),
            preflight);

        ManagementTenantProvisioningRequest request = CreateRequest(modules: [], quotas: []);
        var result = await handler.Handle(
            new ScheduleManagedTenantProvisioningCommand(managedInstanceId, request),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("tenant_provisioning_requires_multi_tenant");
        await Assert.That(mutationLock.Keys).IsEquivalentTo([GovernanceSettingKeys.Deployment.Mode]);
        await tenantRepository.DidNotReceive().GetActiveTenantCountAsync();
        await operationRepository.DidNotReceive().CountActiveReservationsAsync(Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task Schedule_WhenPlanPolicyChangesBeforeLockAcquisition_RejectsWithoutOperationOrOutboxMutation()
    {
        Guid managedInstanceId = Guid.CreateVersion7();
        Guid planVersionId = Guid.Parse("01980000-0000-7000-8000-000000000001");
        var planVersion = new TenantPlanVersion
        {
            Id = planVersionId,
            TenantPlanId = Guid.CreateVersion7(),
            TenantPlan = new TenantPlan
            {
                Id = Guid.CreateVersion7(),
                Key = "standard",
                DisplayName = "Standard"
            },
            VersionNumber = 1,
            TenantPlanStatusId = (int)TenantPlanStatusEnum.Published,
            CurrencyCode = "EUR",
            BillingPeriod = "month",
            IsActiveForProvisioning = true
        };
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        operationRepository.Create(Arg.Any<ManagedTenantProvisioningOperation>())
            .Returns(call => call.Arg<ManagedTenantProvisioningOperation>());
        var outboxRepository = Substitute.For<IOutboxRepository>();
        outboxRepository.Create(Arg.Any<OutboxMessage>())
            .Returns(call => call.Arg<OutboxMessage>());
        var mutationLock = new RecordingSettingMutationLock(
            () => planVersion.IsActiveForProvisioning = false);
        var handler = CreateValidScheduleHandler(
            managedInstanceId,
            operationRepository,
            outboxRepository,
            planVersion: planVersion,
            mutationLock: mutationLock);

        var result = await handler.Handle(
            new ScheduleManagedTenantProvisioningCommand(
                managedInstanceId,
                CreateRequest(modules: [], quotas: [])),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("tenant_plan_not_provisionable");
        await operationRepository.DidNotReceive().Create(Arg.Any<ManagedTenantProvisioningOperation>());
        await outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task Schedule_WhenCustomerReferenceBelongsToDifferentRequest_RejectsBeforeOutboxMutation()
    {
        Guid managedInstanceId = Guid.CreateVersion7();
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        operationRepository.GetByManagedInstanceAndExternalCustomerReferenceAsync(
                managedInstanceId,
                "customer-1",
                Arg.Any<CancellationToken>())
            .Returns(new ManagedTenantProvisioningOperation
            {
                Id = Guid.CreateVersion7(),
                ManagedInstanceId = managedInstanceId,
                ExternalRequestId = "earlier-request",
                ExternalCustomerReference = "customer-1",
                RequestHash = new string('a', 64),
                RequestJson = "{}",
                TenantSlug = "earlier-tenant",
                CurrentOutboxMessageId = Guid.CreateVersion7(),
                Status = ManagedTenantProvisioningStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
        var handler = CreateValidScheduleHandler(managedInstanceId, operationRepository, outboxRepository);

        var result = await handler.Handle(
            new ScheduleManagedTenantProvisioningCommand(
                managedInstanceId,
                CreateRequest(modules: [], quotas: [], externalRequestId: "new-request")),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("tenant_provisioning_customer_conflict");
        await outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task Schedule_WhenRegisteredApiVersionIsIncompatible_RejectsBeforePolicyOrOutboxMutation()
    {
        Guid managedInstanceId = Guid.CreateVersion7();
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var handler = CreateValidScheduleHandler(
            managedInstanceId,
            operationRepository,
            outboxRepository,
            registrationApiVersion: "management.v0");

        var result = await handler.Handle(
            new ScheduleManagedTenantProvisioningCommand(
                managedInstanceId,
                CreateRequest(modules: [], quotas: [])),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("managed_registration_api_incompatible");
        await outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task Schedule_WhenTerminalRequestIsRetried_ReusesOperationWithNewOutboxGeneration()
    {
        Guid managedInstanceId = Guid.CreateVersion7();
        Guid operationId = Guid.CreateVersion7();
        Guid oldOutboxMessageId = Guid.CreateVersion7();
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        ManagementTenantProvisioningRequest request = CreateRequest(modules: [], quotas: []);
        ManagementTenantProvisioningRequest normalized =
            ManagedTenantProvisioningRequestCodec.Normalize(request);
        var operation = new ManagedTenantProvisioningOperation
        {
            Id = operationId,
            ManagedInstanceId = managedInstanceId,
            ExternalRequestId = normalized.ExternalRequestId,
            ExternalCustomerReference = normalized.ExternalCustomerReference,
            RequestHash = ManagedTenantProvisioningRequestCodec.ComputeHash(normalized),
            RequestJson = null,
            TenantSlug = normalized.TenantSlug,
            CurrentOutboxMessageId = oldOutboxMessageId,
            Status = ManagedTenantProvisioningStatus.Failed,
            FailureCode = "transient_failure",
            FailedAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow.AddMinutes(-2)
        };
        operationRepository.GetByManagedInstanceAndExternalRequestAsync(
                managedInstanceId,
                normalized.ExternalRequestId,
                Arg.Any<CancellationToken>())
            .Returns(operation);
        operationRepository.GetByManagedInstanceAndIdAsNoTrackingAsync(
                managedInstanceId,
                operationId,
                Arg.Any<CancellationToken>())
            .Returns(operation);
        operationRepository.TryRetryAsync(
                operationId,
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                operation.CurrentOutboxMessageId = call.ArgAt<Guid>(1);
                operation.RequestJson = call.ArgAt<string>(2);
                operation.FailureCode = null;
                operation.FailedAt = null;
                operation.Status = ManagedTenantProvisioningStatus.Pending;
                return true;
            });
        OutboxMessage? createdOutbox = null;
        outboxRepository.Create(Arg.Do<OutboxMessage>(message => createdOutbox = message))
            .Returns(call => call.Arg<OutboxMessage>());
        var handler = CreateValidScheduleHandler(managedInstanceId, operationRepository, outboxRepository);

        var result = await handler.Handle(
            new ScheduleManagedTenantProvisioningCommand(managedInstanceId, request),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.OperationId).IsEqualTo(operationId);
        await Assert.That(createdOutbox).IsNotNull();
        await Assert.That(createdOutbox!.Id).IsNotEqualTo(oldOutboxMessageId);
        await Assert.That(createdOutbox.AggregateId).IsEqualTo(operationId);
        await Assert.That(operation.CurrentOutboxMessageId).IsEqualTo(createdOutbox.Id);
        await operationRepository.Received(1).TryRetryAsync(
            operationId,
            createdOutbox.Id,
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Schedule_WhenOwnTenantCommittedBeforeOperationCompletion_RequeuesWithoutNewSlotChecks()
    {
        Guid managedInstanceId = Guid.CreateVersion7();
        Guid operationId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var externalBindingRepository = Substitute.For<IExternalBindingRepository>();
        var tenantRepository = Substitute.For<ITenantRepository>();
        ManagementTenantProvisioningRequest request = CreateRequest(modules: [], quotas: []);
        ManagementTenantProvisioningRequest normalized =
            ManagedTenantProvisioningRequestCodec.Normalize(request);
        var operation = new ManagedTenantProvisioningOperation
        {
            Id = operationId,
            ManagedInstanceId = managedInstanceId,
            ExternalRequestId = normalized.ExternalRequestId,
            ExternalCustomerReference = normalized.ExternalCustomerReference,
            RequestHash = ManagedTenantProvisioningRequestCodec.ComputeHash(normalized),
            RequestJson = null,
            TenantSlug = normalized.TenantSlug,
            CurrentOutboxMessageId = Guid.CreateVersion7(),
            Status = ManagedTenantProvisioningStatus.Failed,
            FailureCode = "tenant_provisioning_dispatch_exhausted",
            FailedAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow.AddMinutes(-2)
        };
        operationRepository.GetByManagedInstanceAndExternalRequestAsync(
                managedInstanceId,
                normalized.ExternalRequestId,
                Arg.Any<CancellationToken>())
            .Returns(operation);
        operationRepository.GetByManagedInstanceAndIdAsNoTrackingAsync(
                managedInstanceId,
                operationId,
                Arg.Any<CancellationToken>())
            .Returns(operation);
        operationRepository.TryRetryAsync(
                operationId,
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                operation.CurrentOutboxMessageId = call.ArgAt<Guid>(1);
                operation.RequestJson = call.ArgAt<string>(2);
                operation.Status = ManagedTenantProvisioningStatus.Pending;
                operation.FailureCode = null;
                operation.FailedAt = null;
                return true;
            });
        externalBindingRepository.GetByExternalKeyAsync(
                "islamu-event-control-plane",
                "control-plane",
                ExternalBindingTypes.External.ProviderCustomer,
                normalized.ExternalCustomerReference,
                null,
                Arg.Any<CancellationToken>())
            .Returns(new ExternalBinding
            {
                ProviderKey = "islamu-event-control-plane",
                ExternalSystem = "control-plane",
                ExternalType = ExternalBindingTypes.External.ProviderCustomer,
                ExternalId = normalized.ExternalCustomerReference,
                InternalType = ExternalBindingTypes.Internal.Tenant,
                InternalId = tenantId
            });
        externalBindingRepository.GetByExternalKeyAsync(
                "islamu-event-control-plane",
                "control-plane",
                ExternalBindingTypes.External.ManagedTenantProvisioningOperation,
                operationId.ToString("D"),
                tenantId,
                Arg.Any<CancellationToken>())
            .Returns(new ExternalBinding
            {
                ProviderKey = "islamu-event-control-plane",
                ExternalSystem = "control-plane",
                ExternalType = ExternalBindingTypes.External.ManagedTenantProvisioningOperation,
                ExternalId = operationId.ToString("D"),
                InternalType = ExternalBindingTypes.Internal.Tenant,
                InternalId = tenantId,
                ScopeTenantId = tenantId
            });
        tenantRepository.GetByIdAsNoTrackingAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new Tenant
            {
                Id = tenantId,
                FullName = "Tenant One",
                Slug = normalized.TenantSlug,
                TenantStatus = null!,
                TenantStatusId = (int)TenantStatusEnum.Active
            });
        tenantRepository.GetTenantBySlug(normalized.TenantSlug).Returns(new Tenant
        {
            Id = tenantId,
            FullName = "Tenant One",
            Slug = normalized.TenantSlug,
            TenantStatus = null!,
            TenantStatusId = (int)TenantStatusEnum.Active
        });
        tenantRepository.GetActiveTenantCountAsync().Returns(10);
        outboxRepository.Create(Arg.Any<OutboxMessage>())
            .Returns(call => call.Arg<OutboxMessage>());
        var handler = CreateValidScheduleHandler(
            managedInstanceId,
            operationRepository,
            outboxRepository,
            externalBindingRepository: externalBindingRepository,
            tenantRepository: tenantRepository);
        tenantRepository.ClearReceivedCalls();
        operationRepository.ClearReceivedCalls();

        var result = await handler.Handle(
            new ScheduleManagedTenantProvisioningCommand(managedInstanceId, request),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.OperationId).IsEqualTo(operationId);
        await tenantRepository.DidNotReceive().GetTenantBySlug(normalized.TenantSlug);
        await tenantRepository.DidNotReceive().GetActiveTenantCountAsync();
        await operationRepository.DidNotReceive().CountActiveReservationsAsync(
            Arg.Any<CancellationToken>(),
            Arg.Any<Guid?>());
    }

    [Test]
    public async Task Schedule_WhenCommittedCustomerBindingLacksOperationProvenance_FailsClosed()
    {
        Guid managedInstanceId = Guid.CreateVersion7();
        Guid operationId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var externalBindingRepository = Substitute.For<IExternalBindingRepository>();
        var tenantRepository = Substitute.For<ITenantRepository>();
        ManagementTenantProvisioningRequest request = CreateRequest(modules: [], quotas: []);
        ManagementTenantProvisioningRequest normalized =
            ManagedTenantProvisioningRequestCodec.Normalize(request);
        var operation = new ManagedTenantProvisioningOperation
        {
            Id = operationId,
            ManagedInstanceId = managedInstanceId,
            ExternalRequestId = normalized.ExternalRequestId,
            ExternalCustomerReference = normalized.ExternalCustomerReference,
            RequestHash = ManagedTenantProvisioningRequestCodec.ComputeHash(normalized),
            TenantSlug = normalized.TenantSlug,
            CurrentOutboxMessageId = Guid.CreateVersion7(),
            Status = ManagedTenantProvisioningStatus.Failed,
            FailureCode = "tenant_provisioning_dispatch_exhausted",
            FailedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        operationRepository.GetByManagedInstanceAndExternalRequestAsync(
                managedInstanceId,
                normalized.ExternalRequestId,
                Arg.Any<CancellationToken>())
            .Returns(operation);
        externalBindingRepository.GetByExternalKeyAsync(
                "islamu-event-control-plane",
                "control-plane",
                ExternalBindingTypes.External.ProviderCustomer,
                normalized.ExternalCustomerReference,
                null,
                Arg.Any<CancellationToken>())
            .Returns(new ExternalBinding
            {
                ProviderKey = "islamu-event-control-plane",
                ExternalSystem = "control-plane",
                ExternalType = ExternalBindingTypes.External.ProviderCustomer,
                ExternalId = normalized.ExternalCustomerReference,
                InternalType = ExternalBindingTypes.Internal.Tenant,
                InternalId = tenantId
            });
        tenantRepository.GetByIdAsNoTrackingAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new Tenant
            {
                Id = tenantId,
                FullName = "Foreign Tenant",
                Slug = normalized.TenantSlug,
                TenantStatus = null!
            });
        var handler = CreateValidScheduleHandler(
            managedInstanceId,
            operationRepository,
            outboxRepository,
            externalBindingRepository: externalBindingRepository,
            tenantRepository: tenantRepository);

        var result = await handler.Handle(
            new ScheduleManagedTenantProvisioningCommand(managedInstanceId, request),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo("tenant_provisioning_operation_provenance_conflict");
        await operationRepository.DidNotReceive().TryRetryAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task CanonicalHash_WhenCollectionsHaveDifferentOrder_IsIdentical()
    {
        ManagementTenantProvisioningRequest first = CreateRequest();
        ManagementTenantProvisioningRequest second = CreateRequest(
            modules: ["module-a", "module-b"],
            quotas: [new("storage.bytes", 100), new("ai.daily_tenant_messages", 10)]);

        string firstHash = ManagedTenantProvisioningRequestCodec.ComputeHash(
            ManagedTenantProvisioningRequestCodec.Normalize(first));
        string secondHash = ManagedTenantProvisioningRequestCodec.ComputeHash(
            ManagedTenantProvisioningRequestCodec.Normalize(second));

        await Assert.That(firstHash).IsEqualTo(secondHash);
    }

    [Test]
    public async Task CanonicalHash_WhenNestedJsonOrderAndDomainSpellingDiffer_IsIdentical()
    {
        ManagementTenantProvisioningRequest first = CreateRequest(
            initialSettings:
            [
                new ManagementTenantInitialSettingDto(
                    "appearance.theme.mode",
                    "{\"outer\":{\"b\":2,\"a\":1},\"items\":[{\"z\":3,\"y\":2}]}")
            ],
            domain: new ManagementTenantDomainIntentDto { CustomDomain = " EXAMPLE.COM. " });
        ManagementTenantProvisioningRequest second = CreateRequest(
            initialSettings:
            [
                new ManagementTenantInitialSettingDto(
                    "appearance.theme.mode",
                    "{\"items\":[{\"y\":2,\"z\":3}],\"outer\":{\"a\":1,\"b\":2}}")
            ],
            domain: new ManagementTenantDomainIntentDto { CustomDomain = "example.com" });

        ManagementTenantProvisioningRequest firstNormalized =
            ManagedTenantProvisioningRequestCodec.Normalize(first);
        ManagementTenantProvisioningRequest secondNormalized =
            ManagedTenantProvisioningRequestCodec.Normalize(second);

        await Assert.That(firstNormalized.Domain!.CustomDomain).IsEqualTo("example.com");
        await Assert.That(ManagedTenantProvisioningRequestCodec.ComputeHash(firstNormalized))
            .IsEqualTo(ManagedTenantProvisioningRequestCodec.ComputeHash(secondNormalized));
    }

    [Test]
    public async Task Preflight_WhenPublishedPlanWasDeactivated_RejectsExecutionRecheck()
    {
        Guid planVersionId = Guid.Parse("01980000-0000-7000-8000-000000000001");
        var tenantRepository = Substitute.For<ITenantRepository>();
        var planRepository = Substitute.For<ITenantPlanRepository>();
        planRepository.GetVersionAsync(planVersionId, Arg.Any<CancellationToken>()).Returns(new TenantPlanVersion
        {
            Id = planVersionId,
            TenantPlanId = Guid.CreateVersion7(),
            TenantPlan = new TenantPlan
            {
                Id = Guid.CreateVersion7(),
                Key = "standard",
                DisplayName = "Standard"
            },
            VersionNumber = 1,
            TenantPlanStatusId = (int)TenantPlanStatusEnum.Published,
            CurrencyCode = "EUR",
            BillingPeriod = "month",
            IsActiveForProvisioning = false
        });
        var systemSettingRepository = Substitute.For<ISystemSettingRepository>();
        var preflight = new ManagedTenantProvisioningPreflight(
            tenantRepository,
            planRepository,
            Substitute.For<IModuleDefinitionRepository>(),
            Substitute.For<ITenantSettingRepository>(),
            systemSettingRepository,
            Substitute.For<ITenantBrandingSettingsDocumentLockService>(),
            new TenantPlanStorageQuotaCeilingPolicy(systemSettingRepository),
            Microsoft.Extensions.Options.Options.Create(new ManagedControlPlaneOptions { Enabled = true }));

        ManagedTenantProvisioningPreflightResult result = await preflight.EvaluateAsync(
            ManagedTenantProvisioningRequestCodec.Normalize(CreateRequest(modules: [], quotas: [])),
            requireProvisionablePlan: true,
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("tenant_plan_not_provisionable");
    }

    [Test]
    public async Task Capacity_WhenPendingOperationUsesLastSlot_RejectsWithoutTenantMutation()
    {
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = Guid.CreateVersion7(),
            IsCompleted = true,
            SelectedDeploymentMode = DeploymentMode.MultiTenant.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetActiveTenantCountAsync().Returns(1);
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        operationRepository.CountActiveReservationsAsync(Arg.Any<CancellationToken>(), null).Returns(1);
        var policy = new TenantActivationCapacityPolicy(
            bootstrapRepository,
            tenantRepository,
            operationRepository,
            Microsoft.Extensions.Options.Options.Create(new ManagedControlPlaneOptions
            {
                Enabled = true,
                MaximumTenantCount = 2
            }));

        TenantActivationCapacityAssessment result = await policy.EvaluateAsync(
            requireMultiTenant: true,
            cancellationToken: CancellationToken.None);

        await Assert.That(result.Allowed).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("tenant_provisioning_capacity_exhausted");
        await Assert.That(result.Active).IsEqualTo(1);
        await Assert.That(result.Reserved).IsEqualTo(1);
        await Assert.That(result.Available).IsEqualTo(0);
        await tenantRepository.DidNotReceive().Create(Arg.Any<Tenant>());
    }

    [Test]
    public async Task Operation_Complete_ClearsRequestSnapshotAndExposesResult()
    {
        DateTime now = DateTime.SpecifyKind(new DateTime(2026, 7, 12, 10, 0, 0), DateTimeKind.Utc);
        var operation = new ManagedTenantProvisioningOperation
        {
            Id = Guid.CreateVersion7(),
            ManagedInstanceId = Guid.CreateVersion7(),
            ExternalRequestId = "request-1",
            ExternalCustomerReference = "customer-1",
            RequestHash = new string('a', 64),
            RequestJson = "{}",
            TenantSlug = "tenant-one",
            Status = ManagedTenantProvisioningStatus.Pending,
            CreatedAt = now
        };
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();

        operation.Start(now.AddSeconds(1));
        operation.Complete(tenantId, userId, now.AddSeconds(2));

        await Assert.That(operation.Status).IsEqualTo(ManagedTenantProvisioningStatus.Succeeded);
        await Assert.That(operation.RequestJson).IsNull();
        await Assert.That(operation.TenantId).IsEqualTo(tenantId);
        await Assert.That(operation.TenantAdministratorUserId).IsEqualTo(userId);
        await Assert.That(operation.CanCancel).IsFalse();
    }

    [Test]
    public async Task Process_DelegatesPersistedModeDecisionToLockedProvisioner()
    {
        Guid operationId = Guid.CreateVersion7();
        Guid outboxMessageId = Guid.CreateVersion7();
        var operation = new ManagedTenantProvisioningOperation
        {
            Id = operationId,
            ManagedInstanceId = Guid.CreateVersion7(),
            ExternalRequestId = "request-1",
            ExternalCustomerReference = "customer-1",
            RequestHash = new string('a', 64),
            RequestJson = ManagedTenantProvisioningRequestCodec.Serialize(
                ManagedTenantProvisioningRequestCodec.Normalize(CreateRequest())),
            TenantSlug = "tenant-one",
            CurrentOutboxMessageId = outboxMessageId,
            Status = ManagedTenantProvisioningStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        operationRepository.GetByIdAsNoTrackingAsync(operationId, Arg.Any<CancellationToken>())
            .Returns(operation);
        operationRepository.TryStartAsync(
                operationId,
                outboxMessageId,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                operation.Start(call.ArgAt<DateTime>(2));
                return true;
            });
        operationRepository.TryFailAsync(
                operationId,
                outboxMessageId,
                Arg.Any<string>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                operation.Fail(call.ArgAt<string>(2), call.ArgAt<DateTime>(3));
                return true;
            });
        var provisioner = Substitute.For<IManagedProviderClientProvisioner>();
        provisioner.EnsureAsync(
                Arg.Any<ManagedProviderClientProvisioningDto>(),
                Arg.Any<ManagementTenantProvisioningRequest>(),
                operationId,
                outboxMessageId,
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<ManagedProviderClientProvisioningResultDto>
            {
                Success = false,
                FailureCode = "tenant_provisioning_requires_multi_tenant"
            });
        var handler = new ProcessManagedTenantProvisioningOperationCommandHandler(
            operationRepository,
            provisioner);

        bool result = await handler.Handle(
            new ProcessManagedTenantProvisioningOperationCommand(operationId, outboxMessageId),
            CancellationToken.None);

        await Assert.That(result).IsTrue();
        await Assert.That(operation.Status).IsEqualTo(ManagedTenantProvisioningStatus.Failed);
        await Assert.That(operation.FailureCode).IsEqualTo("tenant_provisioning_requires_multi_tenant");
        await provisioner.Received(1).EnsureAsync(
            Arg.Any<ManagedProviderClientProvisioningDto>(),
            Arg.Any<ManagementTenantProvisioningRequest>(),
            operationId,
            outboxMessageId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Validator_WhenCollectionsAreExplicitNull_ReturnsFailuresWithoutThrowing()
    {
        var validator = new ManagementTenantProvisioningRequestValidator();

        var result = await validator.ValidateAsync(CreateRequest(explicitNullCollections: true));

        await Assert.That(result.IsValid).IsFalse();
        string[] properties = result.Errors.Select(error => error.PropertyName).ToArray();
        await Assert.That(properties).Contains(nameof(ManagementTenantProvisioningRequest.ApprovedModules));
        await Assert.That(properties).Contains(nameof(ManagementTenantProvisioningRequest.InitialSettings));
        await Assert.That(properties).Contains("Plan.Quotas");
    }

    [Test]
    public async Task Validator_WhenCollectionElementsAreNull_ReturnsFailuresWithoutThrowing()
    {
        var validator = new ManagementTenantProvisioningRequestValidator();
        JsonObject requestJson = JsonSerializer.SerializeToNode(CreateRequest())!.AsObject();
        requestJson[nameof(ManagementTenantProvisioningRequest.ApprovedModules)] = new JsonArray { null };
        requestJson[nameof(ManagementTenantProvisioningRequest.InitialSettings)] = new JsonArray { null };
        requestJson[nameof(ManagementTenantProvisioningRequest.Plan)]!
            .AsObject()[nameof(ManagementTenantPlanDto.Quotas)] = new JsonArray { null };
        ManagementTenantProvisioningRequest request =
            requestJson.Deserialize<ManagementTenantProvisioningRequest>()
            ?? throw new InvalidOperationException("Managed tenant provisioning request did not deserialize.");

        var result = await validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsFalse();
        string[] properties = result.Errors.Select(error => error.PropertyName).ToArray();
        await Assert.That(properties).Contains("ApprovedModules[0]");
        await Assert.That(properties).Contains("Plan.Quotas[0]");
        await Assert.That(properties).Contains("InitialSettings[0]");
    }

    [Test]
    public async Task Validator_WhenRequiredNestedDtosAreNull_ReturnsFailuresWithoutThrowing()
    {
        var validator = new ManagementTenantProvisioningRequestValidator();

        var result = await validator.ValidateAsync(CreateRequest(explicitNullNestedDtos: true));

        await Assert.That(result.IsValid).IsFalse();
        string[] properties = result.Errors.Select(error => error.PropertyName).ToArray();
        await Assert.That(properties).Contains(nameof(ManagementTenantProvisioningRequest.Administrator));
        await Assert.That(properties).Contains(nameof(ManagementTenantProvisioningRequest.Plan));
    }

    [Test]
    public async Task DeadLetterReconciliation_WhenProcessing_FailsOnceAndClearsRequestSnapshot()
    {
        Guid operationId = Guid.CreateVersion7();
        Guid outboxMessageId = Guid.CreateVersion7();
        var operation = new ManagedTenantProvisioningOperation
        {
            Id = operationId,
            ManagedInstanceId = Guid.CreateVersion7(),
            ExternalRequestId = "request-1",
            ExternalCustomerReference = "customer-1",
            RequestHash = new string('a', 64),
            RequestJson = "{\"administrator\":{\"email\":\"private@example.test\"}}",
            TenantSlug = "tenant-one",
            CurrentOutboxMessageId = outboxMessageId,
            Status = ManagedTenantProvisioningStatus.Processing,
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            CreatedAt = DateTime.UtcNow.AddMinutes(-11)
        };
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        operationRepository.GetByIdAsNoTrackingAsync(operationId, Arg.Any<CancellationToken>())
            .Returns(operation);
        operationRepository.TryFailAsync(
                operationId,
                outboxMessageId,
                ReconcileManagedTenantProvisioningDeadLetterCommandHandler.FailureCode,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                operation.Fail(
                    ReconcileManagedTenantProvisioningDeadLetterCommandHandler.FailureCode,
                    call.ArgAt<DateTime>(3));
                return true;
            });
        var handler = new ReconcileManagedTenantProvisioningDeadLetterCommandHandler(operationRepository);
        var command = new ReconcileManagedTenantProvisioningDeadLetterCommand(
            operationId,
            outboxMessageId);

        await handler.Handle(command, CancellationToken.None);
        DateTime? failedAt = operation.FailedAt;
        await handler.Handle(command, CancellationToken.None);

        await Assert.That(operation.Status).IsEqualTo(ManagedTenantProvisioningStatus.Failed);
        await Assert.That(operation.FailureCode)
            .IsEqualTo(ReconcileManagedTenantProvisioningDeadLetterCommandHandler.FailureCode);
        await Assert.That(operation.RequestJson).IsNull();
        await Assert.That(operation.FailedAt).IsEqualTo(failedAt);
        await operationRepository.Received(1).TryFailAsync(
            operationId,
            outboxMessageId,
            ReconcileManagedTenantProvisioningDeadLetterCommandHandler.FailureCode,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeadLetterReconciliation_WhenOldAttemptRetriesAfterReset_DoesNotFailNewAttempt()
    {
        Guid operationId = Guid.CreateVersion7();
        Guid oldOutboxMessageId = Guid.CreateVersion7();
        Guid newOutboxMessageId = Guid.CreateVersion7();
        var operation = new ManagedTenantProvisioningOperation
        {
            Id = operationId,
            ManagedInstanceId = Guid.CreateVersion7(),
            ExternalRequestId = "request-1",
            ExternalCustomerReference = "customer-1",
            RequestHash = new string('a', 64),
            RequestJson = "{}",
            TenantSlug = "tenant-one",
            CurrentOutboxMessageId = newOutboxMessageId,
            Status = ManagedTenantProvisioningStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        operationRepository.GetByIdAsNoTrackingAsync(operationId, Arg.Any<CancellationToken>())
            .Returns(operation);
        var handler = new ReconcileManagedTenantProvisioningDeadLetterCommandHandler(operationRepository);

        bool result = await handler.Handle(
            new ReconcileManagedTenantProvisioningDeadLetterCommand(operationId, oldOutboxMessageId),
            CancellationToken.None);

        await Assert.That(result).IsTrue();
        await operationRepository.DidNotReceive().TryFailAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Process_WhenGenerationChangesBetweenReadAndFailureCas_TreatsOldWorkerAsStale()
    {
        Guid operationId = Guid.CreateVersion7();
        Guid oldOutboxMessageId = Guid.CreateVersion7();
        Guid newOutboxMessageId = Guid.CreateVersion7();
        var operation = new ManagedTenantProvisioningOperation
        {
            Id = operationId,
            ManagedInstanceId = Guid.CreateVersion7(),
            ExternalRequestId = "request-1",
            ExternalCustomerReference = "customer-1",
            RequestHash = new string('a', 64),
            RequestJson = ManagedTenantProvisioningRequestCodec.Serialize(
                ManagedTenantProvisioningRequestCodec.Normalize(CreateRequest())),
            TenantSlug = "tenant-one",
            CurrentOutboxMessageId = oldOutboxMessageId,
            Status = ManagedTenantProvisioningStatus.Processing,
            CreatedAt = DateTime.UtcNow
        };
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        operationRepository.GetByIdAsNoTrackingAsync(operationId, Arg.Any<CancellationToken>())
            .Returns(operation);
        operationRepository.TryFailAsync(
                operationId,
                oldOutboxMessageId,
                Arg.Any<string>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                operation.CurrentOutboxMessageId = newOutboxMessageId;
                operation.Status = ManagedTenantProvisioningStatus.Pending;
                return false;
            });
        var provisioner = Substitute.For<IManagedProviderClientProvisioner>();
        provisioner.EnsureAsync(
                Arg.Any<ManagedProviderClientProvisioningDto>(),
                Arg.Any<ManagementTenantProvisioningRequest>(),
                operationId,
                oldOutboxMessageId,
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<ManagedProviderClientProvisioningResultDto>
            {
                Success = false,
                FailureCode = "transient_policy_change"
            });
        var handler = new ProcessManagedTenantProvisioningOperationCommandHandler(
            operationRepository,
            provisioner);

        bool result = await handler.Handle(
            new ProcessManagedTenantProvisioningOperationCommand(operationId, oldOutboxMessageId),
            CancellationToken.None);

        await Assert.That(result).IsTrue();
        await Assert.That(operation.CurrentOutboxMessageId).IsEqualTo(newOutboxMessageId);
        await Assert.That(operation.Status).IsEqualTo(ManagedTenantProvisioningStatus.Pending);
    }

    [Test]
    public async Task Process_WhenAtomicCompletionLosesSameMessageRace_PropagatesForTransactionRollback()
    {
        Guid operationId = Guid.CreateVersion7();
        Guid outboxMessageId = Guid.CreateVersion7();
        var operation = new ManagedTenantProvisioningOperation
        {
            Id = operationId,
            ManagedInstanceId = Guid.CreateVersion7(),
            ExternalRequestId = "request-1",
            ExternalCustomerReference = "customer-1",
            RequestHash = new string('a', 64),
            RequestJson = ManagedTenantProvisioningRequestCodec.Serialize(
                ManagedTenantProvisioningRequestCodec.Normalize(CreateRequest())),
            TenantSlug = "tenant-one",
            CurrentOutboxMessageId = outboxMessageId,
            Status = ManagedTenantProvisioningStatus.Processing,
            CreatedAt = DateTime.UtcNow
        };
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        operationRepository.GetByIdAsNoTrackingAsync(operationId, Arg.Any<CancellationToken>())
            .Returns(operation);
        var provisioner = Substitute.For<IManagedProviderClientProvisioner>();
        provisioner.EnsureAsync(
                Arg.Any<ManagedProviderClientProvisioningDto>(),
                Arg.Any<ManagementTenantProvisioningRequest>(),
                operationId,
                outboxMessageId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<BaseCommandResponse<ManagedProviderClientProvisioningResultDto>>(
                new ConcurrencyConflictException(
                    ConcurrencyConflictException.ConcurrentUpdate,
                    "The same-message lease lost the atomic completion race.",
                    nameof(ManagedTenantProvisioningOperation),
                    operationId.ToString("D"))));
        var handler = new ProcessManagedTenantProvisioningOperationCommandHandler(
            operationRepository,
            provisioner);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => handler.Handle(
            new ProcessManagedTenantProvisioningOperationCommand(operationId, outboxMessageId),
            CancellationToken.None));

        await operationRepository.DidNotReceive().TryCompleteAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    private static ManagementTenantProvisioningRequest CreateRequest(
        IReadOnlyList<string>? modules = null,
        IReadOnlyList<ManagementTenantQuotaDto>? quotas = null,
        bool explicitNullCollections = false,
        IReadOnlyList<ManagementTenantInitialSettingDto>? initialSettings = null,
        ManagementTenantDomainIntentDto? domain = null,
        string externalRequestId = "request-1",
        string externalCustomerReference = "customer-1",
        bool explicitNullNestedDtos = false) => new()
        {
            ExternalRequestId = externalRequestId,
            ExternalCustomerReference = externalCustomerReference,
            TenantName = "Tenant One",
            TenantSlug = "tenant-one",
            Administrator = explicitNullNestedDtos ? null! : new ManagementTenantAdministratorDto
            {
                ExternalIdentity = new ManagementTenantExternalIdentityDto
                {
                    IdentityProvider = "keycloak",
                    Subject = "subject-1",
                    Email = "admin@example.test",
                    FirstName = "Admin",
                    LastName = "User",
                    EmailVerified = true
                }
            },
            Plan = explicitNullNestedDtos ? null! : new ManagementTenantPlanDto
            {
                Key = "standard",
                VersionId = Guid.Parse("01980000-0000-7000-8000-000000000001"),
                Quotas = explicitNullCollections ? null! : quotas ??
            [
                new ManagementTenantQuotaDto("ai.daily_tenant_messages", 10),
                new ManagementTenantQuotaDto("storage.bytes", 100)
            ]
            },
            ApprovedModules = explicitNullCollections ? null! : modules ?? ["module-b", "module-a"],
            Domain = domain,
            InitialSettings = explicitNullCollections ? null! : initialSettings ?? []
        };

    private static ScheduleManagedTenantProvisioningCommandHandler CreateValidScheduleHandler(
        Guid managedInstanceId,
        IManagedTenantProvisioningOperationRepository operationRepository,
        IOutboxRepository outboxRepository,
        string? registrationApiVersion = null,
        IExternalBindingRepository? externalBindingRepository = null,
        ITenantRepository? tenantRepository = null,
        TenantPlanVersion? planVersion = null,
        ISettingMutationLock? mutationLock = null)
    {
        var modeProvider = Substitute.For<IDeploymentModeProvider>();
        modeProvider.GetCurrentModeAsync(Arg.Any<CancellationToken>()).Returns(DeploymentMode.MultiTenant);
        var registrationRepository = Substitute.For<IManagedControlPlaneRegistrationRepository>();
        registrationRepository.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(
            new ManagedControlPlaneRegistration
            {
                Id = Guid.CreateVersion7(),
                ManagedInstanceId = managedInstanceId,
                EventInstanceId = Guid.CreateVersion7(),
                ControlPlaneEndpoint = new UriBuilder(Uri.UriSchemeHttps, "control.example.test").Uri.AbsoluteUri,
                ManagementApiVersion = registrationApiVersion ?? ManagedControlPlaneContract.ManagementApiVersion,
                EventVersion = "1.0.0",
                DeploymentMode = DeploymentMode.MultiTenant,
                RequestHash = new string('a', 64),
                EventToControlPlaneKeyId = "event-key",
                EventToControlPlaneSecretHash = "event-hash",
                ControlPlaneToEventKeyId = "control-key",
                ControlPlaneToEventSecretHash = "control-hash",
                CredentialSecretBindingId = Guid.CreateVersion7(),
                EventToControlPlaneCredentialExpiresAt = DateTime.UtcNow.AddDays(1),
                ControlPlaneToEventCredentialExpiresAt = DateTime.UtcNow.AddDays(1),
                Status = ManagedControlPlaneRegistrationStatus.Registered,
                CreatedAt = DateTime.UtcNow
            });
        tenantRepository ??= Substitute.For<ITenantRepository>();
        tenantRepository.GetTenantBySlug("tenant-one").Returns((Tenant?)null);
        var planRepository = Substitute.For<ITenantPlanRepository>();
        Guid planVersionId = Guid.Parse("01980000-0000-7000-8000-000000000001");
        planVersion ??= new TenantPlanVersion
        {
            Id = planVersionId,
            TenantPlanId = Guid.CreateVersion7(),
            TenantPlan = new TenantPlan
            {
                Id = Guid.CreateVersion7(),
                Key = "standard",
                DisplayName = "Standard"
            },
            VersionNumber = 1,
            TenantPlanStatusId = (int)TenantPlanStatusEnum.Published,
            CurrencyCode = "EUR",
            BillingPeriod = "month",
            IsActiveForProvisioning = true
        };
        planRepository.GetVersionAsync(planVersionId, Arg.Any<CancellationToken>()).Returns(planVersion);
        var moduleRepository = Substitute.For<IModuleDefinitionRepository>();
        moduleRepository.GetActiveByKeysAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ModuleDefinition>());
        var brandingLockService = Substitute.For<ITenantBrandingSettingsDocumentLockService>();
        brandingLockService.GetLockStateAsync(Arg.Any<CancellationToken>())
            .Returns(TenantBrandingSettingsDocumentLockState.AllowAll);
        brandingLockService.ValidateAllowedChanges(
                Arg.Any<BrandingSettings>(),
                Arg.Any<BrandingSettings>(),
                Arg.Any<TenantBrandingSettingsDocumentLockState>())
            .Returns(Array.Empty<string>());
        var options = Microsoft.Extensions.Options.Options.Create(new ManagedControlPlaneOptions
        {
            Enabled = true,
            MaximumTenantCount = 10
        });
        var systemSettingRepository = Substitute.For<ISystemSettingRepository>();
        var preflight = new ManagedTenantProvisioningPreflight(
            tenantRepository,
            planRepository,
            moduleRepository,
            Substitute.For<ITenantSettingRepository>(),
            systemSettingRepository,
            brandingLockService,
            new TenantPlanStorageQuotaCeilingPolicy(systemSettingRepository),
            options);
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = Guid.CreateVersion7(),
            IsCompleted = true,
            SelectedDeploymentMode = DeploymentMode.MultiTenant.ToString(),
            CreatedAt = DateTime.UtcNow
        });

        return new ScheduleManagedTenantProvisioningCommandHandler(
            modeProvider,
            registrationRepository,
            operationRepository,
            externalBindingRepository ?? Substitute.For<IExternalBindingRepository>(),
            tenantRepository,
            outboxRepository,
            mutationLock ?? new RecordingSettingMutationLock(),
            new TenantActivationCapacityPolicy(
                bootstrapRepository,
                tenantRepository,
                operationRepository,
                options),
            preflight);
    }

    private sealed class RecordingSettingMutationLock : ISettingMutationLock
    {
        private readonly Action? _beforeOperation;

        internal RecordingSettingMutationLock(Action? beforeOperation = null)
        {
            _beforeOperation = beforeOperation;
        }

        internal List<string> Keys { get; } = [];

        public Task<T> ExecuteAsync<T>(
            string canonicalSettingKey,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            Keys.Add(canonicalSettingKey);
            _beforeOperation?.Invoke();
            return operation(cancellationToken);
        }

        public Task<T> ExecuteManyAsync<T>(
            IEnumerable<string> canonicalSettingKeys,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            Keys.AddRange(canonicalSettingKeys);
            _beforeOperation?.Invoke();
            return operation(cancellationToken);
        }
    }
}
