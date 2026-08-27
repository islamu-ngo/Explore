// ABOUTME: Verifies the canonical paid-policy mutation boundary used by CQRS and manifests.
// ABOUTME: Pins serializable locking, expected-instance revisions, tenant fencing, and atomic writes.

namespace Event.Application.UnitTests.Features.PaidEventPolicies;

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.PaidEventPolicies;
using Explore.Application.Features.PaidEventPolicies;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

[Category("Phase43Ticketing")]
public sealed class PaidEventPolicyMutationBoundaryTests
{
    private static readonly Guid TenantId =
        Guid.Parse("0199464e-e388-7f56-9281-cefabd6a5673");

    [Test]
    public async Task ReviseTenantAsync_UsesSerializableNamedLocksAndPersistsOneRevision()
    {
        IPaidEventPolicyRepository policies = Substitute.For<IPaidEventPolicyRepository>();
        var unitOfWork = new RecordingUnitOfWork();
        var mutationLock = new RecordingSettingMutationLock();
        policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(EnabledInstancePolicy());
        var boundary = new PaidEventPolicyMutationBoundary(
            policies,
            unitOfWork,
            mutationLock);

        PaidEventPolicyMutationResult result = await boundary.ReviseTenantAsync(
            new TenantPaidEventPolicyMutationInput(TenantId, Revision()),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(unitOfWork.SerializableBoundaries).IsEqualTo(1);
        await Assert.That(mutationLock.Keys).IsEquivalentTo(
        [
            PaidEventPolicyMutationLockKeys.Instance,
            PaidEventPolicyMutationLockKeys.ForTenant(TenantId)
        ]);
        await policies.Received(1).AddAsync(
            Arg.Is<PaidEventPolicyVersion>(policy =>
                policy.TenantId == TenantId
                && policy.VersionNumber == 1
                && policy.IsActive),
            Arg.Any<CancellationToken>());
        await policies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReviseTenantInCurrentTransaction_StaleInstanceRevisionWritesNothing()
    {
        IPaidEventPolicyRepository policies = Substitute.For<IPaidEventPolicyRepository>();
        policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(EnabledInstancePolicy());
        var boundary = new PaidEventPolicyMutationBoundary(
            policies,
            new RecordingUnitOfWork(),
            new RecordingSettingMutationLock());

        PaidEventPolicyMutationResult result =
            await boundary.ReviseTenantInCurrentTransactionAsync(
                new TenantPaidEventPolicyMutationInput(
                    TenantId,
                    Revision(),
                    ExpectedInstancePolicyVersion: 1,
                    RequireAbsentTenantPolicy: true),
                CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(PaidEventPolicyMutationFailureCodes.ConcurrencyConflict);
        await policies.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await policies.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Test]
    public async Task ReviseTenantInCurrentTransaction_ExistingTenantPolicyFailsClosed()
    {
        IPaidEventPolicyRepository policies = Substitute.For<IPaidEventPolicyRepository>();
        policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(EnabledInstancePolicy());
        policies.GetActiveTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(PaidEventPolicyVersion.CreateTenant(
                TenantId,
                isPaymentsEnabled: false,
                allowedOrganizerKinds: [ActorTypeEnum.Organization],
                requiresLocalVerification: true,
                allowedCurrencyCodes: ["USD"],
                defaultCurrencyCode: "USD",
                refundProtections: Enum.GetValues<PaidEventRefundProtection>(),
                currencyRiskLimits: [],
                requiresFirstPaidEventReview: true,
                farFutureReviewThresholdDays: 90));
        var boundary = new PaidEventPolicyMutationBoundary(
            policies,
            new RecordingUnitOfWork(),
            new RecordingSettingMutationLock());

        PaidEventPolicyMutationResult result =
            await boundary.ReviseTenantInCurrentTransactionAsync(
                new TenantPaidEventPolicyMutationInput(
                    TenantId,
                    Revision(),
                    ExpectedInstancePolicyVersion: 1,
                    RequireAbsentTenantPolicy: true),
                CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(PaidEventPolicyMutationFailureCodes.ConcurrencyConflict);
        await policies.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await policies.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Test]
    public async Task ReviseInstanceInCurrentTransaction_StaleExpectedVersionWritesNothing()
    {
        IPaidEventPolicyRepository policies = Substitute.For<IPaidEventPolicyRepository>();
        policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(EnabledInstancePolicy());
        var boundary = new PaidEventPolicyMutationBoundary(
            policies,
            new RecordingUnitOfWork(),
            new RecordingSettingMutationLock());

        PaidEventPolicyMutationResult result =
            await boundary.ReviseInstanceInCurrentTransactionAsync(
                new InstancePaidEventPolicyMutationInput(
                    Revision(),
                    ExpectedActivePolicyVersion: 1),
                CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(PaidEventPolicyMutationFailureCodes.ConcurrencyConflict);
        await policies.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await policies.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Test]
    public async Task ReviseInstanceInCurrentTransaction_MatchingVersionCreatesNextRevision()
    {
        IPaidEventPolicyRepository policies = Substitute.For<IPaidEventPolicyRepository>();
        policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(EnabledInstancePolicy());
        var boundary = new PaidEventPolicyMutationBoundary(
            policies,
            new RecordingUnitOfWork(),
            new RecordingSettingMutationLock());

        PaidEventPolicyMutationResult result =
            await boundary.ReviseInstanceInCurrentTransactionAsync(
                new InstancePaidEventPolicyMutationInput(
                    Revision(),
                    ExpectedActivePolicyVersion: 2),
                CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await policies.Received(1).AddAsync(
            Arg.Is<PaidEventPolicyVersion>(policy =>
                policy.TenantId == null
                && policy.VersionNumber == 3
                && policy.IsActive),
            Arg.Any<CancellationToken>());
        await policies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReviseInstanceAsync_UsdOnlyRevisionCannotStrandActiveEurTenant()
    {
        PaidEventPolicyVersion currentInstance = EnabledInstancePolicy(
            ["USD", "EUR"],
            "USD");
        PaidEventPolicyVersion eurTenant = PaidEventPolicyVersion.CreateTenant(
            TenantId,
            isPaymentsEnabled: true,
            allowedOrganizerKinds: [ActorTypeEnum.Organization],
            requiresLocalVerification: true,
            allowedCurrencyCodes: ["EUR"],
            defaultCurrencyCode: "EUR",
            refundProtections: Enum.GetValues<PaidEventRefundProtection>(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: true,
            farFutureReviewThresholdDays: 90);
        PaidEventPolicyVersion usdTenant = PaidEventPolicyVersion.CreateTenant(
            Guid.CreateVersion7(),
            isPaymentsEnabled: true,
            allowedOrganizerKinds: [ActorTypeEnum.Organization],
            requiresLocalVerification: true,
            allowedCurrencyCodes: ["USD"],
            defaultCurrencyCode: "USD",
            refundProtections: Enum.GetValues<PaidEventRefundProtection>(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: true,
            farFutureReviewThresholdDays: 90);
        IPaidEventPolicyRepository policies = Substitute.For<IPaidEventPolicyRepository>();
        policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(currentInstance);
        policies.ListActiveTenantsAsync(
                0,
                IPaidEventPolicyRepository.MaximumActiveTenantPolicyPageSize,
                Arg.Any<CancellationToken>())
            .Returns(Enumerable.Repeat(
                usdTenant,
                IPaidEventPolicyRepository.MaximumActiveTenantPolicyPageSize).ToArray());
        policies.ListActiveTenantsAsync(
                IPaidEventPolicyRepository.MaximumActiveTenantPolicyPageSize,
                IPaidEventPolicyRepository.MaximumActiveTenantPolicyPageSize,
                Arg.Any<CancellationToken>())
            .Returns([eurTenant]);
        var unitOfWork = new RecordingUnitOfWork();
        var mutationLock = new RecordingSettingMutationLock();
        var boundary = new PaidEventPolicyMutationBoundary(
            policies,
            unitOfWork,
            mutationLock);

        PaidEventPolicyMutationResult result = await boundary.ReviseInstanceAsync(
            Revision(),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(PaidEventPolicyMutationFailureCodes.ValidationFailed);
        await Assert.That(currentInstance.IsActive).IsTrue();
        await Assert.That(currentInstance.VersionNumber).IsEqualTo(2);
        await Assert.That(unitOfWork.SerializableBoundaries).IsEqualTo(1);
        await Assert.That(mutationLock.Keys)
            .IsEquivalentTo([PaidEventPolicyMutationLockKeys.Instance]);
        await policies.Received(1).ListActiveTenantsAsync(
            IPaidEventPolicyRepository.MaximumActiveTenantPolicyPageSize,
            IPaidEventPolicyRepository.MaximumActiveTenantPolicyPageSize,
            Arg.Any<CancellationToken>());
        await policies.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await policies.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Test]
    public async Task ReviseTenantInCurrentTransaction_BroadeningKeepsTrackedPolicyActive()
    {
        PaidEventPolicyVersion currentTenantPolicy = PaidEventPolicyVersion.CreateTenant(
            TenantId,
            isPaymentsEnabled: true,
            allowedOrganizerKinds: [ActorTypeEnum.Organization],
            requiresLocalVerification: false,
            allowedCurrencyCodes: ["USD"],
            defaultCurrencyCode: "USD",
            refundProtections: Enum.GetValues<PaidEventRefundProtection>(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: false,
            farFutureReviewThresholdDays: null);
        IPaidEventPolicyRepository policies = Substitute.For<IPaidEventPolicyRepository>();
        policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(EnabledInstancePolicy());
        policies.GetActiveTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(currentTenantPolicy);
        var boundary = new PaidEventPolicyMutationBoundary(
            policies,
            new RecordingUnitOfWork(),
            new RecordingSettingMutationLock());
        RevisePaidEventPolicyDto broadening = Revision() with
        {
            AllowedOrganizerKindIds = [(int)ActorTypeEnum.User]
        };

        PaidEventPolicyMutationResult result =
            await boundary.ReviseTenantInCurrentTransactionAsync(
                new TenantPaidEventPolicyMutationInput(
                    TenantId,
                    broadening,
                    ExpectedInstancePolicyVersion: 2),
                CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(currentTenantPolicy.IsActive).IsTrue();
        await policies.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await policies.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private static PaidEventPolicyVersion EnabledInstancePolicy(
        IReadOnlyCollection<string>? allowedCurrencyCodes = null,
        string defaultCurrencyCode = "USD") =>
        PaidEventPolicyVersion.CreateDefaultInstance().CreateRevision(
            isPaymentsEnabled: true,
            allowedOrganizerKinds: [ActorTypeEnum.Organization],
            requiresLocalVerification: false,
            allowedCurrencyCodes: allowedCurrencyCodes ?? ["USD"],
            defaultCurrencyCode,
            refundProtections: Enum.GetValues<PaidEventRefundProtection>(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: false,
            farFutureReviewThresholdDays: null);

    private static RevisePaidEventPolicyDto Revision() => new()
    {
        IsPaymentsEnabled = true,
        AllowedOrganizerKindIds = [(int)ActorTypeEnum.Organization],
        RequiresLocalVerification = true,
        AllowedCurrencyCodes = ["USD"],
        DefaultCurrencyCode = "USD",
        RefundProtectionIds = Enum.GetValues<PaidEventRefundProtection>()
            .Select(protection => (int)protection)
            .ToArray(),
        CurrencyRiskLimits = [],
        RequiresFirstPaidEventReview = true,
        FarFutureReviewThresholdDays = 90
    };

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int SerializableBoundaries { get; private set; }

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default) =>
            operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) =>
            operation(ct);

        public async Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
        {
            SerializableBoundaries++;
            return await operation(ct);
        }
    }

    private sealed class RecordingSettingMutationLock : ISettingMutationLock
    {
        public IReadOnlyList<string> Keys { get; private set; } = [];

        public Task<T> ExecuteAsync<T>(
            string canonicalSettingKey,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) =>
            ExecuteManyAsync([canonicalSettingKey], operation, cancellationToken);

        public Task<T> ExecuteManyAsync<T>(
            IEnumerable<string> canonicalSettingKeys,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            Keys = canonicalSettingKeys.ToArray();
            return operation(cancellationToken);
        }
    }
}
