// ABOUTME: Focused tests for the side-effect-free managed tenant provisioning preflight assessment.
// ABOUTME: Proves early mode rejection, resolved preview projection, and stable Event-owned policy blockers.

using System.Text.Json;
using System.Text.Json.Nodes;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Management;
using Explore.Application.Features.ControlPlane.Plans;
using Explore.Application.Features.Management;
using Explore.Application.Features.Management.Handlers;
using Explore.Application.Features.Management.Requests;
using Explore.Application.Management;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Modules;
using Explore.Domain.Settings.Documents.Payloads;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Management;

public sealed class ManagedTenantProvisioningPreflightTests
{
    [Test]
    public async Task Evaluate_WhenSingleTenant_ReturnsBlockerBeforeRegistrationOrCatalogReads()
    {
        var fixture = new PreflightFixture(DeploymentMode.SingleTenant);

        ManagementTenantProvisioningPreflightDto result = await fixture.Handler.Handle(
            new GetManagedTenantProvisioningPreflightQuery(fixture.ManagedInstanceId, fixture.Request),
            CancellationToken.None);

        await Assert.That(result.Ready).IsFalse();
        await Assert.That(result.Capacity).IsNull();
        await Assert.That(result.ResolvedPlan).IsNull();
        await Assert.That(result.Blockers.Single().Code)
            .IsEqualTo("tenant_provisioning_requires_multi_tenant");
        await fixture.RegistrationRepository.DidNotReceive()
            .GetCurrentAsync(Arg.Any<CancellationToken>());
        await fixture.TenantRepository.DidNotReceive().GetTenantBySlug(Arg.Any<string>());
        await fixture.PlanRepository.DidNotReceive()
            .GetVersionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await fixture.OperationRepository.DidNotReceive()
            .CountActiveReservationsAsync(Arg.Any<CancellationToken>(), Arg.Any<Guid?>());
        await fixture.TenantRepository.DidNotReceive().Create(Arg.Any<Tenant>());
    }

    [Test]
    public async Task Evaluate_WhenMultiTenantAndPolicyValid_ProjectsResolvedProvisionalAssessment()
    {
        var fixture = new PreflightFixture();

        ManagementTenantProvisioningPreflightDto result = await fixture.Handler.Handle(
            new GetManagedTenantProvisioningPreflightQuery(fixture.ManagedInstanceId, fixture.Request),
            CancellationToken.None);

        string expectedHash = ManagedTenantProvisioningRequestCodec.ComputeHash(
            ManagedTenantProvisioningRequestCodec.Normalize(fixture.Request));
        await Assert.That(result.Ready).IsTrue();
        await Assert.That(result.Blockers).IsEmpty();
        await Assert.That(result.ManagedInstanceId).IsEqualTo(fixture.ManagedInstanceId);
        await Assert.That(result.EventInstanceId).IsEqualTo(fixture.EventInstanceId);
        await Assert.That(result.NormalizedRequestHash).IsEqualTo(expectedHash);
        await Assert.That(result.RequiresSchedulingRevalidation).IsTrue();
        await Assert.That(result.Capacity!.MaximumTenants).IsEqualTo(10);
        await Assert.That(result.Capacity.ActiveTenants).IsEqualTo(2);
        await Assert.That(result.Capacity.ReservedOperations).IsEqualTo(1);
        await Assert.That(result.Capacity.AvailableSlots).IsEqualTo(7);
        await Assert.That(result.ResolvedPlan!.Key).IsEqualTo("standard");
        await Assert.That(result.ResolvedPlan.VersionId).IsEqualTo(fixture.PlanVersionId);
        await Assert.That(result.ResolvedPlan.Quotas.Select(quota => quota.Key))
            .IsEquivalentTo([TenantPlanQuotaKeys.StorageBytes]);
        await Assert.That(result.AcceptedModules)
            .IsEquivalentTo(["module-a", "module-b"]);
        await Assert.That(result.AcceptedDomain!.Subdomain).IsEqualTo("tenant-one");
        await Assert.That(result.AcceptedBranding!.DisplayName).IsEqualTo("Tenant Brand");
        await Assert.That(result.AcceptedSettings.Select(setting => setting.Key))
            .IsEquivalentTo(["appearance.theme_mode", GovernanceSettingKeys.Domains.TenantSubdomain]);
        await Assert.That(result.CorrelationId).IsEqualTo("correlation-1");
        await fixture.TenantRepository.DidNotReceive().Create(Arg.Any<Tenant>());
        await fixture.OperationRepository.DidNotReceive().Create(Arg.Any<ManagedTenantProvisioningOperation>());
        await fixture.PlanRepository.DidNotReceive()
            .CreateAssignmentAsync(Arg.Any<TenantPlanAssignment>(), Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments("registration", "managed_registration_api_incompatible")]
    [Arguments("plan", "tenant_plan_not_found")]
    [Arguments("module", "tenant_module_unavailable")]
    [Arguments("domain", "tenant_domain_conflict")]
    [Arguments("storage", "tenant_plan_quota_ceiling_exceeded")]
    [Arguments("setting", "tenant_setting_locked")]
    public async Task Evaluate_WhenPolicyIsIneligible_ReturnsStableTypedBlocker(
        string scenario,
        string expectedCode)
    {
        var fixture = new PreflightFixture();
        fixture.ConfigureFailure(scenario);

        ManagementTenantProvisioningPreflightDto result = await fixture.Handler.Handle(
            new GetManagedTenantProvisioningPreflightQuery(fixture.ManagedInstanceId, fixture.Request),
            CancellationToken.None);

        await Assert.That(result.Ready).IsFalse();
        await Assert.That(result.Blockers.Select(blocker => blocker.Code)).Contains(expectedCode);
        await fixture.TenantRepository.DidNotReceive().Create(Arg.Any<Tenant>());
        await fixture.OperationRepository.DidNotReceive().Create(Arg.Any<ManagedTenantProvisioningOperation>());
    }

    [Test]
    public async Task RequestJson_WithUnknownNestedMember_IsRejected()
    {
        var fixture = new PreflightFixture();
        JsonObject request = JsonSerializer.SerializeToNode(fixture.Request)!.AsObject();
        request[nameof(ManagementTenantProvisioningRequest.Administrator)]!
            .AsObject()["unexpected"] = true;

        await Assert.That(() => request.Deserialize<ManagementTenantProvisioningRequest>())
            .Throws<JsonException>();
    }

    private sealed class PreflightFixture
    {
        private readonly ISystemSettingRepository _systemSettingRepository =
            Substitute.For<ISystemSettingRepository>();
        private readonly IModuleDefinitionRepository _moduleRepository =
            Substitute.For<IModuleDefinitionRepository>();
        private readonly ITenantSettingRepository _tenantSettingRepository =
            Substitute.For<ITenantSettingRepository>();
        private readonly ManagedControlPlaneOptions _options = new()
        {
            Enabled = true,
            MaximumTenantCount = 10,
            TenantAdministratorSignInUrl = new Uri("https://event.example.test/sign-in")
        };

        public PreflightFixture(DeploymentMode mode = DeploymentMode.MultiTenant)
        {
            ManagedInstanceId = Guid.CreateVersion7();
            EventInstanceId = Guid.CreateVersion7();
            PlanVersionId = Guid.Parse("01980000-0000-7000-8000-000000000001");
            Request = CreateRequest(PlanVersionId);

            var modeProvider = Substitute.For<IDeploymentModeProvider>();
            modeProvider.GetCurrentModeAsync(Arg.Any<CancellationToken>()).Returns(mode);

            RegistrationRepository = Substitute.For<IManagedControlPlaneRegistrationRepository>();
            RegistrationRepository.GetCurrentAsync(Arg.Any<CancellationToken>())
                .Returns(CreateRegistration());

            TenantRepository = Substitute.For<ITenantRepository>();
            TenantRepository.GetTenantBySlug("tenant-one").Returns((Tenant?)null);
            TenantRepository.GetActiveTenantCountAsync().Returns(2);

            PlanRepository = Substitute.For<ITenantPlanRepository>();
            PlanRepository.GetVersionAsync(PlanVersionId, Arg.Any<CancellationToken>())
                .Returns(CreatePlanVersion());

            _moduleRepository.GetActiveByKeysAsync(
                    Arg.Any<IReadOnlyCollection<string>>(),
                    Arg.Any<CancellationToken>())
                .Returns(
                [
                    CreateModule("module-b"),
                    CreateModule("module-a")
                ]);

            var brandingLockService = Substitute.For<ITenantBrandingSettingsDocumentLockService>();
            brandingLockService.GetLockStateAsync(Arg.Any<CancellationToken>())
                .Returns(TenantBrandingSettingsDocumentLockState.AllowAll);
            brandingLockService.ValidateAllowedChanges(
                    Arg.Any<BrandingSettings>(),
                    Arg.Any<BrandingSettings>(),
                    Arg.Any<TenantBrandingSettingsDocumentLockState>())
                .Returns(Array.Empty<string>());

            OperationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
            OperationRepository.CountActiveReservationsAsync(
                    Arg.Any<CancellationToken>(),
                    Arg.Any<Guid?>())
                .Returns(1);

            var options = Microsoft.Extensions.Options.Options.Create(_options);
            var preflight = new ManagedTenantProvisioningPreflight(
                TenantRepository,
                PlanRepository,
                _moduleRepository,
                _tenantSettingRepository,
                _systemSettingRepository,
                brandingLockService,
                new TenantPlanStorageQuotaCeilingPolicy(_systemSettingRepository),
                options);
            var capacityPolicy = new TenantActivationCapacityPolicy(
                Substitute.For<IInstanceBootstrapStateRepository>(),
                TenantRepository,
                OperationRepository,
                options);
            Handler = new GetManagedTenantProvisioningPreflightQueryHandler(
                options,
                modeProvider,
                RegistrationRepository,
                capacityPolicy,
                preflight);
        }

        public Guid ManagedInstanceId { get; }
        public Guid EventInstanceId { get; }
        public Guid PlanVersionId { get; }
        public ManagementTenantProvisioningRequest Request { get; private set; }
        public GetManagedTenantProvisioningPreflightQueryHandler Handler { get; }
        public IManagedControlPlaneRegistrationRepository RegistrationRepository { get; }
        public ITenantRepository TenantRepository { get; }
        public ITenantPlanRepository PlanRepository { get; }
        public IManagedTenantProvisioningOperationRepository OperationRepository { get; }

        public void ConfigureFailure(string scenario)
        {
            switch (scenario)
            {
                case "registration":
                    ManagedControlPlaneRegistration registration = CreateRegistration();
                    registration.ManagementApiVersion = "0.9";
                    RegistrationRepository.GetCurrentAsync(Arg.Any<CancellationToken>())
                        .Returns(registration);
                    break;
                case "plan":
                    PlanRepository.GetVersionAsync(PlanVersionId, Arg.Any<CancellationToken>())
                        .Returns((TenantPlanVersion?)null);
                    break;
                case "module":
                    _moduleRepository.GetActiveByKeysAsync(
                            Arg.Any<IReadOnlyCollection<string>>(),
                            Arg.Any<CancellationToken>())
                        .Returns([CreateModule("module-a")]);
                    break;
                case "domain":
                    _tenantSettingRepository.GetByDomainHostAsync(
                            "tenant-one",
                            Arg.Any<CancellationToken>())
                        .Returns(new TenantSetting
                        {
                            Id = Guid.CreateVersion7(),
                            TenantId = Guid.CreateVersion7(),
                            Tenant = null!,
                            SettingKey = GovernanceSettingKeys.Domains.TenantSubdomain,
                            Value = "\"tenant-one\"",
                            CreatedAt = DateTime.UtcNow
                        });
                    break;
                case "storage":
                    _systemSettingRepository.GetByKey(
                            GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes,
                            Arg.Any<CancellationToken>())
                        .Returns(new SystemSetting
                        {
                            Id = Guid.CreateVersion7(),
                            SettingKey = GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes,
                            Value = "50",
                            CreatedAt = DateTime.UtcNow
                        });
                    break;
                case "setting":
                    _systemSettingRepository.IsLocked(
                            "appearance.theme_mode",
                            Arg.Any<CancellationToken>())
                        .Returns(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scenario));
            }
        }

        private ManagedControlPlaneRegistration CreateRegistration() => new()
        {
            Id = Guid.CreateVersion7(),
            ManagedInstanceId = ManagedInstanceId,
            EventInstanceId = EventInstanceId,
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
        };

        private TenantPlanVersion CreatePlanVersion() => new()
        {
            Id = PlanVersionId,
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
            IsActiveForProvisioning = true,
            Quotas =
            [
                new TenantPlanVersionQuota
                {
                    Id = Guid.CreateVersion7(),
                    TenantPlanVersionId = PlanVersionId,
                    QuotaKey = TenantPlanQuotaKeys.StorageBytes,
                    Limit = 100,
                    CreatedAt = DateTime.UtcNow
                }
            ]
        };

        private static ModuleDefinition CreateModule(string key) => new()
        {
            Id = Guid.CreateVersion7(),
            ModuleKey = key,
            Name = key,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        private static ManagementTenantProvisioningRequest CreateRequest(Guid planVersionId) => new()
        {
            ExternalRequestId = "request-1",
            ExternalCustomerReference = "customer-1",
            TenantName = "Tenant One",
            TenantSlug = "tenant-one",
            Administrator = new ManagementTenantAdministratorDto
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
            Plan = new ManagementTenantPlanDto
            {
                Key = "standard",
                VersionId = planVersionId,
                Quotas = [new ManagementTenantQuotaDto(TenantPlanQuotaKeys.StorageBytes, 100)]
            },
            ApprovedModules = ["module-b", "module-a"],
            Domain = new ManagementTenantDomainIntentDto { Subdomain = "tenant-one" },
            Branding = new ManagementTenantBrandingIntentDto { DisplayName = "Tenant Brand" },
            InitialSettings =
            [
                new ManagementTenantInitialSettingDto("appearance.theme_mode", "\"dark\"")
            ],
            Callback = new ManagementTenantCallbackMetadataDto { CorrelationId = "correlation-1" }
        };
    }
}
