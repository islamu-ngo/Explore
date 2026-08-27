// ABOUTME: Verifies manifest preflight classifies existing tenants and collects every safe blocker.
// ABOUTME: Proves skipped tenants bypass settings, branding, and publication-policy inspection wholesale.

using System.Collections.Immutable;
using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.ConfigurationManifest.Compilation;
using Explore.Application.Features.ConfigurationManifest.Contracts;
using Explore.Application.Features.ConfigurationManifest.Ingestion;
using Explore.Application.Features.ConfigurationManifest.Preflight;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Definitions;
using Explore.Domain.Settings.Documents;
using Explore.Domain.Settings.Documents.Payloads;
using NSubstitute;

namespace Event.Application.UnitTests.Features.ConfigurationManifest;

public sealed class ConfigurationManifestPreflightTests
{
    private static readonly DateTime OccurredAt = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task EvaluateAsync_ExistingTenantSkipsWholesaleWithoutConfigurationReads()
    {
        ITenantRepository tenants = Substitute.For<ITenantRepository>();
        ISystemSettingRepository systemSettings = Substitute.For<ISystemSettingRepository>();
        ITenantBrandingSettingsDocumentLockService brandingLocks =
            Substitute.For<ITenantBrandingSettingsDocumentLockService>();
        ICoordinatedSettingMutationStore policyStore =
            Substitute.For<ICoordinatedSettingMutationStore>();
        IPaidEventPolicyRepository paidEventPolicies =
            Substitute.For<IPaidEventPolicyRepository>();
        Tenant existing = NewTenant("existing");
        tenants.GetBySlugsAsNoTrackingAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([existing]);
        var preflight = new ConfigurationManifestPreflight(
            tenants,
            systemSettings,
            brandingLocks,
            policyStore,
            paidEventPolicies);

        ConfigurationManifestPreflightResult result = await preflight.EvaluateAsync(
            Plan(TenantPlan("existing")),
            CancellationToken.None);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Tenants).HasSingleItem();
        await Assert.That(result.Tenants[0].Disposition)
            .IsEqualTo(ConfigurationManifestTenantDisposition.SkippedExisting);
        await Assert.That(result.Tenants[0].TenantId).IsEqualTo(existing.Id);
        await systemSettings.DidNotReceiveWithAnyArgs()
            .IsLocked(default!, default);
        await brandingLocks.DidNotReceiveWithAnyArgs()
            .GetLockStateAsync(default);
        await policyStore.DidNotReceiveWithAnyArgs()
            .ReadTenantSnapshotAsync(default, default);
    }

    [Test]
    public async Task EvaluateAsync_CreateCandidateCollectsLockedSettingBrandingAndUnsafePolicy()
    {
        ITenantRepository tenants = Substitute.For<ITenantRepository>();
        ISystemSettingRepository systemSettings = Substitute.For<ISystemSettingRepository>();
        ITenantBrandingSettingsDocumentLockService brandingLocks =
            Substitute.For<ITenantBrandingSettingsDocumentLockService>();
        ICoordinatedSettingMutationStore policyStore =
            Substitute.For<ICoordinatedSettingMutationStore>();
        IPaidEventPolicyRepository paidEventPolicies =
            Substitute.For<IPaidEventPolicyRepository>();
        tenants.GetBySlugsAsNoTrackingAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        systemSettings.IsLocked(
                TenantSettingDefinitions.WhiteLabelingEnabled.Key,
                Arg.Any<CancellationToken>())
            .Returns(true);
        brandingLocks.GetLockStateAsync(Arg.Any<CancellationToken>())
            .Returns(new TenantBrandingSettingsDocumentLockState(false, false, true, true));
        brandingLocks.ValidateAllowedChanges(
                Arg.Any<BrandingSettings>(),
                Arg.Any<BrandingSettings>(),
                Arg.Any<TenantBrandingSettingsDocumentLockState>())
            .Returns(["logoUrl"]);
        policyStore.ReadTenantSnapshotAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(UnsafeCurrentPolicy());
        var preflight = new ConfigurationManifestPreflight(
            tenants,
            systemSettings,
            brandingLocks,
            policyStore,
            paidEventPolicies);
        ConfigurationManifestTenantPlan tenant = TenantPlan(
            "new-tenant",
            guarded:
            [
                new ConfigurationManifestSettingWrite(
                    EventSettingDefinitions.UserSubmissionEnabled.Key,
                    "false")
            ],
            unguarded:
            [
                new ConfigurationManifestSettingWrite(
                    TenantSettingDefinitions.WhiteLabelingEnabled.Key,
                    "true")
            ],
            brandingJson:
                """
                {"displayName":"Primary Community","logoUrl":"https://cdn.example.org/logo.svg","faviconUrl":null,"customCssUrl":null}
                """);

        ConfigurationManifestPreflightResult result = await preflight.EvaluateAsync(
            Plan(tenant),
            CancellationToken.None);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.Code)).IsEquivalentTo(
        [
            ReportingIntakePolicyReasonCodes.UnsafePublicationPolicy,
            ConfigurationManifestApplicationFailureCodes.DocumentLocked,
            ConfigurationManifestApplicationFailureCodes.SettingLocked
        ]);
        await Assert.That(result.Errors.Select(error => error.Key)).IsEquivalentTo(
        [
            EventSettingDefinitions.UserSubmissionEnabled.Key,
            SettingsDocumentKeys.Tenant.Branding,
            TenantSettingDefinitions.WhiteLabelingEnabled.Key
        ]);
        await Assert.That(result.Errors.All(error => !error.Message.Contains(
                "cdn.example.org",
                StringComparison.Ordinal)))
            .IsTrue();
    }

    [Test]
    public async Task EvaluateAsync_OrdersErrorsByManifestPositionThenKey()
    {
        ITenantRepository tenants = Substitute.For<ITenantRepository>();
        ISystemSettingRepository systemSettings = Substitute.For<ISystemSettingRepository>();
        ITenantBrandingSettingsDocumentLockService brandingLocks =
            Substitute.For<ITenantBrandingSettingsDocumentLockService>();
        ICoordinatedSettingMutationStore policyStore =
            Substitute.For<ICoordinatedSettingMutationStore>();
        IPaidEventPolicyRepository paidEventPolicies =
            Substitute.For<IPaidEventPolicyRepository>();
        tenants.GetBySlugsAsNoTrackingAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        systemSettings.IsLocked(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        brandingLocks.GetLockStateAsync(Arg.Any<CancellationToken>())
            .Returns(TenantBrandingSettingsDocumentLockState.AllowAll);
        brandingLocks.ValidateAllowedChanges(
                Arg.Any<BrandingSettings>(),
                Arg.Any<BrandingSettings>(),
                Arg.Any<TenantBrandingSettingsDocumentLockState>())
            .Returns([]);
        var preflight = new ConfigurationManifestPreflight(
            tenants,
            systemSettings,
            brandingLocks,
            policyStore,
            paidEventPolicies);
        ConfigurationManifestTenantPlan second = TenantPlan(
            "second",
            manifestIndex: 1,
            unguarded:
            [
                new ConfigurationManifestSettingWrite(
                    TenantSettingDefinitions.WhiteLabelingEnabled.Key,
                    "true")
            ]);
        ConfigurationManifestTenantPlan first = TenantPlan(
            "first",
            manifestIndex: 0,
            unguarded:
            [
                new ConfigurationManifestSettingWrite(
                    PublicExperienceSettingDefinitions.EventCatalogLabel.Key,
                    "\"Events\""),
                new ConfigurationManifestSettingWrite(
                    TenantSettingDefinitions.WhiteLabelingEnabled.Key,
                    "true")
            ]);

        ConfigurationManifestPreflightResult result = await preflight.EvaluateAsync(
            Plan(second, first),
            CancellationToken.None);

        await Assert.That(result.Errors.Select(error => (error.ManifestIndex, error.Key)))
            .IsEquivalentTo(
            [
                (0, PublicExperienceSettingDefinitions.EventCatalogLabel.Key),
                (0, TenantSettingDefinitions.WhiteLabelingEnabled.Key),
                (1, TenantSettingDefinitions.WhiteLabelingEnabled.Key)
            ]);
    }

    [Test]
    public async Task EvaluateAsync_PaidPolicyWithoutActiveInstancePolicy_FailsClosed()
    {
        ITenantRepository tenants = Substitute.For<ITenantRepository>();
        ISystemSettingRepository systemSettings = Substitute.For<ISystemSettingRepository>();
        ITenantBrandingSettingsDocumentLockService brandingLocks =
            Substitute.For<ITenantBrandingSettingsDocumentLockService>();
        ICoordinatedSettingMutationStore policyStore =
            Substitute.For<ICoordinatedSettingMutationStore>();
        IPaidEventPolicyRepository paidEventPolicies =
            Substitute.For<IPaidEventPolicyRepository>();
        tenants.GetBySlugsAsNoTrackingAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        brandingLocks.GetLockStateAsync(Arg.Any<CancellationToken>())
            .Returns(TenantBrandingSettingsDocumentLockState.AllowAll);
        brandingLocks.ValidateAllowedChanges(
                Arg.Any<BrandingSettings>(),
                Arg.Any<BrandingSettings>(),
                Arg.Any<TenantBrandingSettingsDocumentLockState>())
            .Returns([]);
        var preflight = new ConfigurationManifestPreflight(
            tenants,
            systemSettings,
            brandingLocks,
            policyStore,
            paidEventPolicies);

        ConfigurationManifestPreflightResult result = await preflight.EvaluateAsync(
            Plan(TenantPlan("paid", paidEventPolicy: PaidPolicy())),
            CancellationToken.None);

        ConfigurationManifestPreflightError error = result.Errors.Single();
        await Assert.That(error.Key)
            .IsEqualTo(ConfigurationManifestDocumentKeys.InstancePaidEventPolicy);
        await Assert.That(error.Code)
            .IsEqualTo("configuration_manifest_paid_policy_unavailable");
    }

    [Test]
    public async Task EvaluateAsync_PaidPolicyWithStaleInstanceRevision_RejectsBeforeMutation()
    {
        var fixture = PaidPolicyPreflightFixture();
        PaidEventPolicyVersion instancePolicy = PaidEventPolicyVersion.CreateDefaultInstance()
            .CreateRevision(
                isPaymentsEnabled: true,
                allowedOrganizerKinds: [ActorTypeEnum.Organization],
                requiresLocalVerification: false,
                allowedCurrencyCodes: ["USD"],
                defaultCurrencyCode: "USD",
                refundProtections: Enum.GetValues<PaidEventRefundProtection>(),
                currencyRiskLimits: [],
                requiresFirstPaidEventReview: false,
                farFutureReviewThresholdDays: null);
        fixture.PaidEventPolicies.GetActiveInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(instancePolicy);

        ConfigurationManifestPreflightResult result =
            await fixture.Preflight.EvaluateAsync(
                Plan(TenantPlan("paid", paidEventPolicy: PaidPolicy())),
                CancellationToken.None);

        ConfigurationManifestPreflightError error = result.Errors.Single();
        await Assert.That(error.Code)
            .IsEqualTo("configuration_manifest_paid_policy_stale");
        await fixture.PaidEventPolicies.DidNotReceive()
            .GetActiveTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EvaluateAsync_UnboundPaidPolicyAuthorityBindsActiveInstanceVersion()
    {
        var fixture = PaidPolicyPreflightFixture();
        PaidEventPolicyVersion instancePolicy =
            PaidEventPolicyVersion.CreateDefaultInstance().CreateRevision(
                isPaymentsEnabled: true,
                allowedOrganizerKinds: [ActorTypeEnum.Organization],
                requiresLocalVerification: false,
                allowedCurrencyCodes: ["USD"],
                defaultCurrencyCode: "USD",
                refundProtections: Enum.GetValues<PaidEventRefundProtection>(),
                currencyRiskLimits: [],
                requiresFirstPaidEventReview: false,
                farFutureReviewThresholdDays: null);
        fixture.PaidEventPolicies.GetActiveInstanceAsync(
                Arg.Any<CancellationToken>())
            .Returns(instancePolicy);
        ConfigurationManifestApplyPlan plan =
            Plan(TenantPlan("paid", paidEventPolicy: PaidPolicy()));
        plan = plan with
        {
            Instance = plan.Instance with
            {
                PaidEventPolicy =
                    plan.Instance.PaidEventPolicy! with
                    {
                        ExpectedActivePolicyVersion = null
                    }
            }
        };

        ConfigurationManifestPreflightResult result =
            await fixture.Preflight.EvaluateAsync(
                plan,
                CancellationToken.None);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.BoundPlan.Instance.PaidEventPolicy)
            .IsNotNull();
        await Assert.That(result.BoundPlan.Instance.PaidEventPolicy!
            .ExpectedActivePolicyVersion).IsEqualTo(instancePolicy.VersionNumber);
    }

    [Test]
    public async Task EvaluateAsync_PaidPolicyThatBroadensInstanceCeiling_RejectsSafely()
    {
        var fixture = PaidPolicyPreflightFixture();
        fixture.PaidEventPolicies.GetActiveInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(PaidEventPolicyVersion.CreateDefaultInstance());

        ConfigurationManifestPreflightResult result =
            await fixture.Preflight.EvaluateAsync(
                Plan(TenantPlan("paid", paidEventPolicy: PaidPolicy())),
                CancellationToken.None);

        ConfigurationManifestPreflightError error = result.Errors.Single();
        await Assert.That(error.Code)
            .IsEqualTo("configuration_manifest_paid_policy_broadening");
        await Assert.That(error.Message).DoesNotContain("USD");
    }

    [Test]
    public async Task EvaluateAsync_ValidatesTenantAgainstProposedInstanceStateFirst()
    {
        var fixture = PaidPolicyPreflightFixture();
        fixture.PaidEventPolicies.GetActiveInstanceAsync(
                Arg.Any<CancellationToken>())
            .Returns(PaidEventPolicyVersion.CreateDefaultInstance()
                .CreateRevision(
                    isPaymentsEnabled: true,
                    allowedOrganizerKinds: [ActorTypeEnum.Organization],
                    requiresLocalVerification: false,
                    allowedCurrencyCodes: ["USD"],
                    defaultCurrencyCode: "USD",
                    refundProtections:
                        Enum.GetValues<PaidEventRefundProtection>(),
                    currencyRiskLimits: [],
                    requiresFirstPaidEventReview: false,
                    farFutureReviewThresholdDays: null));
        ConfigurationManifestApplyPlan plan =
            Plan(TenantPlan(
                "paid",
                paidEventPolicy: PaidPolicy(isPaymentsEnabled: true)));
        plan = plan with
        {
            Instance = plan.Instance with
            {
                PaidEventPolicy =
                    new ConfigurationManifestInstancePaidEventPolicyPlan(
                        PaidPolicy(isPaymentsEnabled: false),
                        ExpectedActivePolicyVersion: null)
            }
        };

        ConfigurationManifestPreflightResult result =
            await fixture.Preflight.EvaluateAsync(
                plan,
                CancellationToken.None);

        await Assert.That(result.IsValid).IsFalse();
        ConfigurationManifestPreflightError error =
            result.Errors.Single();
        await Assert.That(error.Code)
            .IsEqualTo(
                ConfigurationManifestApplicationFailureCodes
                    .PaidPolicyBroadening);
        await Assert.That(error.Key)
            .IsEqualTo(
                ConfigurationManifestDocumentKeys.TenantPaidEventPolicy);
    }

    [Test]
    public async Task EvaluateAsync_ValidatesTenantSettingsAgainstProposedInstanceState()
    {
        var fixture = PaidPolicyPreflightFixture();
        PublicationPolicyMutationSnapshot current = new(
            SystemValues:
            [
                SystemValue(
                    EventReportingIntakeSettingDefinitions.IntakeEnabled.Key,
                    "false"),
                SystemValue(EventSettingDefinitions.RequireApproval.Key, "true"),
                SystemValue(EventSettingDefinitions.UserSubmissionEnabled.Key, "true"),
                SystemValue(
                    EventSettingDefinitions.OrganizationSubmissionEnabled.Key,
                    "true"),
                SystemValue(EventSettingDefinitions.GroupSubmissionEnabled.Key, "true")
            ],
            TenantValues: []);
        fixture.PublicationPolicyStore.ReadInstanceSnapshotAsync(
                Arg.Any<CancellationToken>())
            .Returns(current);
        fixture.PublicationPolicyStore.ReadTenantSnapshotAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(current);
        ImmutableArray<ConfigurationManifestSettingWrite> proposedInstance =
        [
            Setting(EventSettingDefinitions.RequireApproval.Key, "false"),
            Setting(EventSettingDefinitions.UserSubmissionEnabled.Key, "false"),
            Setting(
                EventSettingDefinitions.OrganizationSubmissionEnabled.Key,
                "false"),
            Setting(EventSettingDefinitions.GroupSubmissionEnabled.Key, "false")
        ];
        ConfigurationManifestApplyPlan plan = Plan(
            proposedInstance,
            TenantPlan(
                "candidate",
                guarded:
                [
                    Setting(
                        EventSettingDefinitions.OrganizationSubmissionEnabled.Key,
                        "true")
                ]));

        ConfigurationManifestPreflightResult result =
            await fixture.Preflight.EvaluateAsync(
                plan,
                CancellationToken.None);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.ManifestIndex == 0
            && error.Code
                == ReportingIntakePolicyReasonCodes
                    .UnsafePublicationPolicy)).IsTrue();
    }

    [Test]
    public async Task EvaluateAsync_ChangedBootstrappedInstanceSectionFailsBeforeStateReads()
    {
        var fixture = PaidPolicyPreflightFixture();
        ConfigurationManifestApplyPlan plan =
            Plan(TenantPlan("candidate")) with
            {
                BootstrapState = new ConfigurationManifestBootstrapState(
                    new string('c', ConfigurationManifestOperation.DigestLength),
                    Generation: 1)
            };

        ConfigurationManifestPreflightResult result =
            await fixture.Preflight.EvaluateAsync(
                plan,
                CancellationToken.None);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Single().Code)
            .IsEqualTo(
                ConfigurationManifestApplicationFailureCodes
                    .InstanceAlreadyBootstrapped);
        await fixture.Tenants.DidNotReceiveWithAnyArgs()
            .GetBySlugsAsNoTrackingAsync(default!, default);
        await fixture.PublicationPolicyStore.DidNotReceiveWithAnyArgs()
            .ReadInstanceSnapshotAsync(default);
        await fixture.PaidEventPolicies.DidNotReceiveWithAnyArgs()
            .GetActiveInstanceAsync(default);
    }

    [Test]
    public async Task EvaluateAsync_SameSectionUsesFreshDay2SettingAuthority()
    {
        var fixture = PaidPolicyPreflightFixture();
        PublicationPolicyMutationSnapshot current = new(
            SystemValues:
            [
                SystemValue(
                    EventReportingIntakeSettingDefinitions.IntakeEnabled.Key,
                    "true"),
                SystemValue(EventSettingDefinitions.RequireApproval.Key, "false"),
                SystemValue(EventSettingDefinitions.UserSubmissionEnabled.Key, "true"),
                SystemValue(
                    EventSettingDefinitions.OrganizationSubmissionEnabled.Key,
                    "true"),
                SystemValue(EventSettingDefinitions.GroupSubmissionEnabled.Key, "true")
            ],
            TenantValues: []);
        fixture.PublicationPolicyStore.ReadTenantSnapshotAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(current);
        ImmutableArray<ConfigurationManifestSettingWrite> historicalInstance =
        [
            Setting(EventSettingDefinitions.RequireApproval.Key, "false"),
            Setting(EventSettingDefinitions.UserSubmissionEnabled.Key, "false"),
            Setting(
                EventSettingDefinitions.OrganizationSubmissionEnabled.Key,
                "false"),
            Setting(EventSettingDefinitions.GroupSubmissionEnabled.Key, "false")
        ];
        ConfigurationManifestApplyPlan plan = Plan(
            historicalInstance,
            TenantPlan(
                "candidate",
                guarded:
                [
                    Setting(
                        EventSettingDefinitions.OrganizationSubmissionEnabled.Key,
                        "true")
                ]));
        plan = plan with
        {
            BootstrapState = new ConfigurationManifestBootstrapState(
                plan.InstanceSectionDigest,
                Generation: 1)
        };

        ConfigurationManifestPreflightResult result =
            await fixture.Preflight.EvaluateAsync(
                plan,
                CancellationToken.None);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.BoundPlan.Instance.GuardedSettings).IsEmpty();
        await Assert.That(result.BoundPlan.Instance.ChangedSettingKeyNames)
            .IsEmpty();
        await fixture.PublicationPolicyStore.DidNotReceiveWithAnyArgs()
            .ReadInstanceSnapshotAsync(default);
    }

    [Test]
    public async Task EvaluateAsync_SameSectionUsesFreshDay2PaidPolicyRevision()
    {
        var fixture = PaidPolicyPreflightFixture();
        PaidEventPolicyVersion current = PaidEventPolicyVersion
            .CreateDefaultInstance()
            .CreateRevision(
                isPaymentsEnabled: true,
                allowedOrganizerKinds: [ActorTypeEnum.Organization],
                requiresLocalVerification: false,
                allowedCurrencyCodes: ["USD"],
                defaultCurrencyCode: "USD",
                refundProtections:
                    Enum.GetValues<PaidEventRefundProtection>(),
                currencyRiskLimits: [],
                requiresFirstPaidEventReview: false,
                farFutureReviewThresholdDays: null);
        fixture.PaidEventPolicies.GetActiveInstanceAsync(
                Arg.Any<CancellationToken>())
            .Returns(current);
        ConfigurationManifestApplyPlan plan =
            Plan(TenantPlan(
                "candidate",
                paidEventPolicy: PaidPolicy()));
        plan = plan with
        {
            BootstrapState = new ConfigurationManifestBootstrapState(
                plan.InstanceSectionDigest,
                Generation: 1),
            Instance = plan.Instance with
            {
                PaidEventPolicy =
                    new ConfigurationManifestInstancePaidEventPolicyPlan(
                        PaidPolicy(isPaymentsEnabled: false),
                        ExpectedActivePolicyVersion: null)
            }
        };

        ConfigurationManifestPreflightResult result =
            await fixture.Preflight.EvaluateAsync(
                plan,
                CancellationToken.None);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(
                result.BoundPlan.Instance.PaidEventPolicy?.ProposedRevision)
            .IsNull();
        await Assert.That(result.BoundPlan.Instance.PaidEventPolicy
                ?.ExpectedActivePolicyVersion)
            .IsEqualTo(current.VersionNumber);
    }

    private static ConfigurationManifestApplyPlan Plan(
        params ConfigurationManifestTenantPlan[] tenants) =>
        Plan([], tenants);

    private static ConfigurationManifestApplyPlan Plan(
        ImmutableArray<ConfigurationManifestSettingWrite> instanceGuarded,
        params ConfigurationManifestTenantPlan[] tenants) =>
        new(
            Guid.Parse("0198e2a4-5340-7f89-8abc-b8bdf43e0ea1"),
            Guid.Parse("0198e2a4-5340-7f89-8abc-b8bdf43e0ea5"),
            ConfigurationManifestMode.Bootstrap,
            "configuration.islamu.org/v1alpha1",
            "TenantConfigurationList",
            "deployment",
            new string('a', 64),
            new string('b', 64),
            BootstrapState: null,
            OccurredAt,
            new ConfigurationManifestInstancePlan(
                GuardedSettings: instanceGuarded,
                UnguardedSettings: [],
                tenants.Any(tenant => tenant.PaidEventPolicy is not null)
                    ? new ConfigurationManifestInstancePaidEventPolicyPlan(
                        ProposedRevision: null,
                        ExpectedActivePolicyVersion: 1)
                    : null,
                ChangedSettingKeyNames:
                    instanceGuarded.Select(setting => setting.Key)
                        .ToImmutableArray(),
                ChangedDocumentKeyNames: []),
            tenants.ToImmutableArray());

    private static ConfigurationManifestTenantPlan TenantPlan(
        string slug,
        int manifestIndex = 0,
        ImmutableArray<ConfigurationManifestSettingWrite> guarded = default,
        ImmutableArray<ConfigurationManifestSettingWrite> unguarded = default,
        ConfigurationManifestPaidEventPolicyPayloadV1Alpha1? paidEventPolicy = null,
        string brandingJson =
            """{"displayName":"Primary Community","logoUrl":null,"faviconUrl":null,"customCssUrl":null}""") =>
        new(
            manifestIndex,
            Guid.CreateVersion7(),
            slug,
            "Primary Community",
            guarded.IsDefault ? [] : guarded,
            unguarded.IsDefault ? [] : unguarded,
            new ConfigurationManifestDocumentWrite(
                Guid.CreateVersion7(),
                SettingsDocumentKeys.Tenant.Branding,
                TenantBrandingSettingsDocumentDefaults.SchemaVersion,
                TenantBrandingSettingsDocumentDefaults.DefaultsVersion,
                brandingJson),
            paidEventPolicy,
            (guarded.IsDefault ? [] : guarded)
                .Concat(unguarded.IsDefault ? [] : unguarded)
                .Select(setting => setting.Key)
                .Order(StringComparer.Ordinal)
                .ToImmutableArray(),
            [SettingsDocumentKeys.Tenant.Branding]);

    private static ConfigurationManifestPaidEventPolicyPayloadV1Alpha1 PaidPolicy(
        bool isPaymentsEnabled = true) =>
        new()
        {
            IsPaymentsEnabled = isPaymentsEnabled,
            AllowedOrganizerKindIds = [2],
            RequiresLocalVerification = true,
            AllowedCurrencyCodes = ["USD"],
            DefaultCurrencyCode = "USD",
            RefundProtectionIds = [1, 2, 3, 4, 5, 6, 7],
            CurrencyRiskLimits = [],
            RequiresFirstPaidEventReview = true,
            FarFutureReviewThresholdDays = 90
        };

    private static PaidPolicyPreflightTestFixture PaidPolicyPreflightFixture()
    {
        ITenantRepository tenants = Substitute.For<ITenantRepository>();
        ISystemSettingRepository systemSettings = Substitute.For<ISystemSettingRepository>();
        ITenantBrandingSettingsDocumentLockService brandingLocks =
            Substitute.For<ITenantBrandingSettingsDocumentLockService>();
        ICoordinatedSettingMutationStore publicationPolicyStore =
            Substitute.For<ICoordinatedSettingMutationStore>();
        IPaidEventPolicyRepository paidEventPolicies =
            Substitute.For<IPaidEventPolicyRepository>();
        tenants.GetBySlugsAsNoTrackingAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        brandingLocks.GetLockStateAsync(Arg.Any<CancellationToken>())
            .Returns(TenantBrandingSettingsDocumentLockState.AllowAll);
        brandingLocks.ValidateAllowedChanges(
                Arg.Any<BrandingSettings>(),
                Arg.Any<BrandingSettings>(),
                Arg.Any<TenantBrandingSettingsDocumentLockState>())
            .Returns([]);
        return new PaidPolicyPreflightTestFixture(
            new ConfigurationManifestPreflight(
                tenants,
                systemSettings,
                brandingLocks,
                publicationPolicyStore,
            paidEventPolicies),
            paidEventPolicies,
            publicationPolicyStore,
            tenants);
    }

    private sealed record PaidPolicyPreflightTestFixture(
        ConfigurationManifestPreflight Preflight,
        IPaidEventPolicyRepository PaidEventPolicies,
        ICoordinatedSettingMutationStore PublicationPolicyStore,
        ITenantRepository Tenants);

    private static Tenant NewTenant(string slug) => new()
    {
        Id = Guid.CreateVersion7(),
        FullName = slug,
        Slug = slug,
        TenantStatusId = 1,
        TenantStatus = null!
    };

    private static PublicationPolicyMutationSnapshot UnsafeCurrentPolicy() => new(
        SystemValues:
        [
            SystemValue(EventReportingIntakeSettingDefinitions.IntakeEnabled.Key, "false"),
            SystemValue(EventSettingDefinitions.RequireApproval.Key, "false"),
            SystemValue(EventSettingDefinitions.UserSubmissionEnabled.Key, "true"),
            SystemValue(EventSettingDefinitions.OrganizationSubmissionEnabled.Key, "true"),
            SystemValue(EventSettingDefinitions.GroupSubmissionEnabled.Key, "true")
        ],
        TenantValues: []);

    private static PublicationPolicySystemValueSnapshot SystemValue(string key, string value) =>
        new(key, value, IsLocked: false);

    private static ConfigurationManifestSettingWrite Setting(
        string key,
        string jsonValue) =>
        new(key, jsonValue);
}
