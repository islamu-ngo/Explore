// ABOUTME: Tests paid-event policy CQRS request authorization metadata and revision behavior.
// ABOUTME: Proves handlers map entity-owned policy facts and reject tenant widening.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.PaidEventPolicies;
using Explore.Application.Features.PaidEventPolicies.Handlers.Commands;
using Explore.Application.Features.PaidEventPolicies.Handlers.Queries;
using Explore.Application.Features.PaidEventPolicies.Requests.Commands;
using Explore.Application.Features.PaidEventPolicies.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.PaidEventPolicies;

[Category("Phase43Ticketing")]
public sealed class PaidEventPolicyHandlersTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly IPaidEventPolicyRepository _policies = Substitute.For<IPaidEventPolicyRepository>();

    [Test]
    public async Task RequestMetadata_UsesSettingResourceIds()
    {
        var instanceQuery = (ISecureRequest)new GetInstancePaidEventPolicyQuery();
        var tenantQuery = (ISecureRequest)new GetTenantPaidEventPolicyQuery(_tenantId);
        var tenantConfigurationQuery = (ISecureRequest)new GetTenantPaidEventPolicyConfigurationQuery(_tenantId);
        var instanceCommand = (ISecureRequest)new ReviseInstancePaidEventPolicyCommand(CreateRevisionDto());
        var tenantCommand = (ISecureRequest)new ReviseTenantPaidEventPolicyCommand(_tenantId, CreateRevisionDto());

        await Assert.That(instanceQuery.ResourceId).IsEqualTo("paid-event-policy");
        await Assert.That(tenantQuery.ResourceId).IsEqualTo($"{_tenantId}:paid-event-policy");
        await Assert.That(tenantConfigurationQuery.ResourceId).IsEqualTo($"{_tenantId}:paid-event-policy");
        await Assert.That(instanceCommand.ResourceId).IsEqualTo("paid-event-policy");
        await Assert.That(tenantCommand.ResourceId).IsEqualTo($"{_tenantId}:paid-event-policy");
    }

    [Test]
    public async Task GetTenantConfiguration_WhenNoTenantOverride_UsesInstancePolicyAsEffectivePolicy()
    {
        PaidEventPolicyVersion instancePolicy = PaidEventPolicyVersion.CreateDefaultInstance().CreateRevision(
            isPaymentsEnabled: true,
            allowedOrganizerKinds: [ActorTypeEnum.Organization],
            requiresLocalVerification: false,
            allowedCurrencyCodes: ["USD"],
            defaultCurrencyCode: "USD",
            refundProtections: RefundProtections(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: false,
            farFutureReviewThresholdDays: null);
        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(instancePolicy);

        TenantPaidEventPolicyConfigurationDto? result = await new GetTenantPaidEventPolicyConfigurationQueryHandler(_policies)
            .Handle(new GetTenantPaidEventPolicyConfigurationQuery(_tenantId), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TenantId).IsEqualTo(_tenantId);
        await Assert.That(result.ActiveTenantOverride).IsNull();
        await Assert.That(result.ActiveInstanceCeiling.Id).IsEqualTo(result.EffectivePolicy.Id);
        await Assert.That(result.EffectivePolicy.TenantId).IsNull();
        await Assert.That(result.EffectivePolicy.AllowedCurrencyCodes.Single()).IsEqualTo("USD");
    }

    [Test]
    public async Task GetTenantConfiguration_WhenTenantOverrideExists_UsesTenantPolicyAsEffectivePolicy()
    {
        PaidEventPolicyVersion instancePolicy = PaidEventPolicyVersion.CreateDefaultInstance().CreateRevision(
            isPaymentsEnabled: true,
            allowedOrganizerKinds: [ActorTypeEnum.Organization, ActorTypeEnum.Group],
            requiresLocalVerification: false,
            allowedCurrencyCodes: ["USD", "EUR"],
            defaultCurrencyCode: "USD",
            refundProtections: RefundProtections(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: false,
            farFutureReviewThresholdDays: null);
        PaidEventPolicyVersion tenantPolicy = PaidEventPolicyVersion.CreateTenant(
            _tenantId,
            isPaymentsEnabled: true,
            allowedOrganizerKinds: [ActorTypeEnum.Organization],
            requiresLocalVerification: true,
            allowedCurrencyCodes: ["EUR"],
            defaultCurrencyCode: "EUR",
            refundProtections: RefundProtections(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: true,
            farFutureReviewThresholdDays: 90);
        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(instancePolicy);
        _policies.GetActiveTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(tenantPolicy);

        TenantPaidEventPolicyConfigurationDto? result = await new GetTenantPaidEventPolicyConfigurationQueryHandler(_policies)
            .Handle(new GetTenantPaidEventPolicyConfigurationQuery(_tenantId), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ActiveTenantOverride).IsNotNull();
        await Assert.That(result.EffectivePolicy.Id).IsEqualTo(result.ActiveTenantOverride!.Id);
        await Assert.That(result.EffectivePolicy.TenantId).IsEqualTo(_tenantId);
        await Assert.That(result.EffectivePolicy.AllowedCurrencyCodes.Single()).IsEqualTo("EUR");
        await Assert.That(result.EffectivePolicy.RequiresFirstPaidEventReview).IsTrue();
    }

    [Test]
    public async Task GetTenantConfiguration_WhenInstancePolicyIsAbsent_ReturnsNullWithoutTenantLookup()
    {
        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns((PaidEventPolicyVersion?)null);

        TenantPaidEventPolicyConfigurationDto? result = await new GetTenantPaidEventPolicyConfigurationQueryHandler(_policies)
            .Handle(new GetTenantPaidEventPolicyConfigurationQuery(_tenantId), CancellationToken.None);

        await Assert.That(result).IsNull();
        await _policies.DidNotReceive().GetActiveTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetTenant_WhenActivePolicyExists_MapsPolicyDto()
    {
        PaidEventPolicyVersion policy = PaidEventPolicyVersion.CreateTenant(
            _tenantId,
            isPaymentsEnabled: true,
            allowedOrganizerKinds: [ActorTypeEnum.Organization],
            requiresLocalVerification: true,
            allowedCurrencyCodes: ["USD"],
            defaultCurrencyCode: "USD",
            refundProtections: RefundProtections(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: true,
            farFutureReviewThresholdDays: 180);
        _policies.GetActiveTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(policy);

        PaidEventPolicyDto? result = await new GetTenantPaidEventPolicyQueryHandler(_policies)
            .Handle(new GetTenantPaidEventPolicyQuery(_tenantId), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TenantId).IsEqualTo(_tenantId);
        await Assert.That(result.IsPaymentsEnabled).IsTrue();
        await Assert.That(result.RequiresLocalVerification).IsTrue();
        await Assert.That(result.AllowedCurrencyCodes.Single()).IsEqualTo("USD");
        await Assert.That(result.FarFutureReviewThresholdDays).IsEqualTo(180);
    }

    [Test]
    public async Task ReviseTenant_WhenTenantAddsInstanceCurrency_ReturnsValidationFailureWithoutSave()
    {
        PaidEventPolicyVersion instancePolicy = PaidEventPolicyVersion.CreateDefaultInstance().CreateRevision(
            isPaymentsEnabled: true,
            allowedOrganizerKinds: [ActorTypeEnum.Organization],
            requiresLocalVerification: false,
            allowedCurrencyCodes: ["USD"],
            defaultCurrencyCode: "USD",
            refundProtections: RefundProtections(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: false,
            farFutureReviewThresholdDays: null);
        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(instancePolicy);
        var unitOfWork = new RecordingUnitOfWork();

        var result = await new ReviseTenantPaidEventPolicyCommandHandler(_policies, unitOfWork).Handle(
            new ReviseTenantPaidEventPolicyCommand(_tenantId, CreateRevisionDto(allowedCurrencyCodes: ["USD", "EUR"])),
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("paid_event_policy_validation_failed");
        await Assert.That(unitOfWork.SerializableBoundaries).IsEqualTo(1);
        await _policies.DidNotReceive().AddAsync(Arg.Any<PaidEventPolicyVersion>(), Arg.Any<CancellationToken>());
        await _policies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReviseInstance_WhenActivePolicyExists_AddsRevisionAndSaves()
    {
        PaidEventPolicyVersion instancePolicy = PaidEventPolicyVersion.CreateDefaultInstance();
        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(instancePolicy);
        var unitOfWork = new RecordingUnitOfWork();

        var result = await new ReviseInstancePaidEventPolicyCommandHandler(_policies, unitOfWork).Handle(
            new ReviseInstancePaidEventPolicyCommand(CreateRevisionDto()),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(unitOfWork.SerializableBoundaries).IsEqualTo(1);
        await _policies.Received(1).AddAsync(Arg.Is<PaidEventPolicyVersion>(policy => policy.VersionNumber == 2 && policy.IsActive), Arg.Any<CancellationToken>());
        await _policies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static RevisePaidEventPolicyDto CreateRevisionDto(IReadOnlyList<string>? allowedCurrencyCodes = null) => new()
    {
        IsPaymentsEnabled = true,
        AllowedOrganizerKindIds = [(int)ActorTypeEnum.Organization],
        RequiresLocalVerification = false,
        AllowedCurrencyCodes = allowedCurrencyCodes ?? ["USD"],
        DefaultCurrencyCode = "USD",
        RefundProtectionIds = RefundProtections().Select(protection => (int)protection).ToArray(),
        CurrencyRiskLimits = [],
        RequiresFirstPaidEventReview = false,
        FarFutureReviewThresholdDays = null
    };

    private static PaidEventRefundProtection[] RefundProtections() => Enum.GetValues<PaidEventRefundProtection>();

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int SerializableBoundaries { get; private set; }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) => operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);

        public async Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
        {
            SerializableBoundaries++;
            return await operation(ct);
        }
    }
}
