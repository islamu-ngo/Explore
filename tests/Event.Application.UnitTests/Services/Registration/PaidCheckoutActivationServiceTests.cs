// ABOUTME: Verifies persisted sale controls, conservative ceiling exposure, and review approvals form one activation result.
// ABOUTME: Proves configured ceilings allow Checkout below limits and direct evaluations fail closed on current facts.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class PaidCheckoutActivationServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly Guid _organizerId = Guid.CreateVersion7();
    private readonly IPaidCheckoutActivationRepository _repository = Substitute.For<IPaidCheckoutActivationRepository>();
    private readonly IPaidEventPolicyRepository _policies = Substitute.For<IPaidEventPolicyRepository>();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();

    [Test]
    public async Task ConfiguredCeilingAllowsBelowLimitAndBlocksOnlyConservativeWouldExceed()
    {
        PaidEventPolicyVersion policy = Policy(PaidEventPolicyCurrencyRiskLimit.Create(
            "EUR", 10_000, 10, 50_000, 100, 30, null));
        Configure(policy, new PaidCheckoutReservedExposure("EUR", 8_000, 8, 40_000, 80));
        PaidCheckoutActivationService service = Service();

        PaidCheckoutActivationResult below = await service.EvaluateAsync(
            new(_tenantId, _eventId, "EUR", 1_000, Now), CancellationToken.None);
        PaidCheckoutActivationResult exceeded = await service.EvaluateAsync(
            new(_tenantId, _eventId, "EUR", 3_000, Now), CancellationToken.None);

        await Assert.That(below.IsActive).IsTrue();
        await Assert.That(exceeded.FailureCode).IsEqualTo("payment_ceiling_exceeded");
        await _repository.Received().GetReservedExposureAsync(
            _tenantId, _eventId, _organizerId, "EUR", Now.AddDays(-30), null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MissingOrStoppedDurableControlFailsClosedWhileApprovedReviewActivates()
    {
        PaidEventPolicyVersion policy = Policy(PaidEventPolicyCurrencyRiskLimit.Create(
            "EUR", null, null, null, null, null, 5_000), firstReview: true);
        Configure(policy, new PaidCheckoutReservedExposure("EUR", 0, 0, 0, 0));
        _repository.GetSaleControlAsync(_tenantId, null, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns((PaidCheckoutSaleControl?)null);
        PaidCheckoutActivationResult absent = await Service().EvaluateAsync(
            new(_tenantId, _eventId, "EUR", 6_000, Now), CancellationToken.None);

        PaidCheckoutSaleControl global = PaidCheckoutSaleControl.CreateActive(_tenantId, null, Guid.CreateVersion7(), Now);
        _repository.GetSaleControlAsync(_tenantId, null, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(global);
        _repository.HasPriorSucceededPaymentAsync(_tenantId, _organizerId, Arg.Any<CancellationToken>()).Returns(false);
        _repository.HasApprovalAsync(_tenantId, _eventId, _organizerId, policy.Id, "EUR",
            PaidCheckoutReviewTrigger.FirstPaidEvent, 6_000, Arg.Any<CancellationToken>()).Returns(true);
        _repository.HasApprovalAsync(_tenantId, _eventId, _organizerId, policy.Id, "EUR",
            PaidCheckoutReviewTrigger.HighValue, 6_000, Arg.Any<CancellationToken>()).Returns(true);
        PaidCheckoutActivationResult approved = await Service().EvaluateAsync(
            new(_tenantId, _eventId, "EUR", 6_000, Now), CancellationToken.None);

        await Assert.That(absent.FailureCode).IsEqualTo("paid_sale_control_uninitialized");
        await Assert.That(approved.IsActive).IsTrue();
    }

    [Test]
    public async Task OperatorAndEitherDurableStopFailClosed()
    {
        PaidCheckoutActivationResult operatorInactive = await Service(Governance(false, true))
            .EvaluateSaleControlAsync(_tenantId, _eventId, CancellationToken.None);

        await Assert.That(operatorInactive.FailureCode).IsEqualTo("payment_operator_inactive");
        await _repository.DidNotReceive().GetSaleControlAsync(
            Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());

        PaidCheckoutSaleControl globalStopped = PaidCheckoutSaleControl.CreateStopped(
            _tenantId, null, Guid.CreateVersion7(), "operator_stop", Now);
        _repository.GetSaleControlAsync(_tenantId, null, false, Arg.Any<CancellationToken>())
            .Returns(globalStopped);
        _repository.GetSaleControlAsync(_tenantId, _eventId, false, Arg.Any<CancellationToken>())
            .Returns((PaidCheckoutSaleControl?)null);

        PaidCheckoutActivationResult tenantStopped = await Service()
            .EvaluateSaleControlAsync(_tenantId, _eventId, CancellationToken.None);

        await Assert.That(tenantStopped.FailureCode).IsEqualTo("paid_sale_stopped");

        PaidCheckoutSaleControl globalActive = PaidCheckoutSaleControl.CreateActive(
            _tenantId, null, Guid.CreateVersion7(), Now);
        PaidCheckoutSaleControl eventStopped = PaidCheckoutSaleControl.CreateStopped(
            _tenantId, _eventId, Guid.CreateVersion7(), "event_stop", Now);
        _repository.GetSaleControlAsync(_tenantId, null, false, Arg.Any<CancellationToken>())
            .Returns(globalActive);
        _repository.GetSaleControlAsync(_tenantId, _eventId, false, Arg.Any<CancellationToken>())
            .Returns(eventStopped);

        PaidCheckoutActivationResult exactEventStopped = await Service()
            .EvaluateSaleControlAsync(_tenantId, _eventId, CancellationToken.None);

        await Assert.That(exactEventStopped.FailureCode).IsEqualTo("paid_sale_stopped");
    }

    [Test]
    public async Task EmptyEventAndNonUtcEvaluationFactsFailBeforeAuthorityReads()
    {
        PaidCheckoutActivationService service = Service();

        PaidCheckoutActivationResult emptyEvent = await service.EvaluateAsync(
            new(_tenantId, Guid.Empty, "EUR", 1_000, Now), CancellationToken.None);
        PaidCheckoutActivationResult nonUtc = await service.EvaluateAsync(
            new(_tenantId, _eventId, "EUR", 1_000, DateTime.SpecifyKind(Now, DateTimeKind.Local)),
            CancellationToken.None);

        await Assert.That(emptyEvent.FailureCode).IsEqualTo("payment_activation_invalid");
        await Assert.That(nonUtc.FailureCode).IsEqualTo("payment_activation_invalid");
        await _events.DidNotReceive().GetEventWithDetailsAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DisabledInstancePolicyFailsClosed()
    {
        PaidEventPolicyVersion disabled = PaidEventPolicyVersion.CreateDefaultInstance();
        Configure(disabled, new PaidCheckoutReservedExposure("EUR", 0, 0, 0, 0));

        PaidCheckoutActivationResult result = await Service().EvaluateAsync(
            new(_tenantId, _eventId, "EUR", 1_000, Now), CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("payment_policy_unavailable");
    }

    [Test]
    public async Task InvalidTenantBroadeningFailsBeforeExposureLookup()
    {
        PaidEventPolicyVersion instance = PolicyWithoutLimits();
        PaidEventPolicyVersion tenant = PaidEventPolicyVersion.CreateTenant(
            _tenantId,
            true,
            instance.AllowedOrganizerKinds,
            false,
            ["USD"],
            "USD",
            instance.RefundProtections,
            [],
            false,
            null);
        Configure(instance, new PaidCheckoutReservedExposure("EUR", 0, 0, 0, 0));
        _policies.GetActiveTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        PaidCheckoutActivationResult result = await Service().EvaluateAsync(
            new(_tenantId, _eventId, "EUR", 1_000, Now), CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("payment_policy_invalid");
        await _repository.DidNotReceive().GetReservedExposureAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(),
            Arg.Any<DateTime?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ValidTenantRevisionAndMissingRiskRowRemainAuthoritative()
    {
        PaidEventPolicyVersion instance = PolicyWithoutLimits();
        PaidEventPolicyVersion tenant = PaidEventPolicyVersion.CreateTenant(
            _tenantId,
            true,
            instance.AllowedOrganizerKinds,
            false,
            ["EUR"],
            "EUR",
            instance.RefundProtections,
            [],
            false,
            null);
        Configure(instance, new PaidCheckoutReservedExposure("EUR", 0, 0, 0, 0));
        _policies.GetActiveTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        PaidCheckoutActivationResult result = await Service().EvaluateAsync(
            new(_tenantId, _eventId, "EUR", 1_000, Now), CancellationToken.None);

        await Assert.That(result.IsActive).IsTrue();
        await Assert.That(result.OrganizerActorId).IsEqualTo(_organizerId);
        await Assert.That(result.EffectivePolicyVersionId).IsEqualTo(tenant.Id);
        await Assert.That(result.ReservedExposure).IsEqualTo(new PaidCheckoutReservedExposure("EUR", 0, 0, 0, 0));
        await _repository.DidNotReceive().GetReservedExposureAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(),
            Arg.Any<DateTime?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PaymentsDisabledTenantPolicyFailsClosed()
    {
        PaidEventPolicyVersion instance = PolicyWithoutLimits();
        PaidEventPolicyVersion tenant = PaidEventPolicyVersion.CreateTenant(
            _tenantId,
            false,
            instance.AllowedOrganizerKinds,
            false,
            ["EUR"],
            "EUR",
            instance.RefundProtections,
            [],
            false,
            null);
        Configure(instance, new PaidCheckoutReservedExposure("EUR", 0, 0, 0, 0));
        _policies.GetActiveTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        PaidCheckoutActivationResult result = await Service().EvaluateAsync(
            new(_tenantId, _eventId, "EUR", 1_000, Now), CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("payment_policy_unavailable");
    }

    [Test]
    public async Task PriorSucceededPaymentBypassesFirstEventReview()
    {
        PaidEventPolicyVersion policy = PolicyWithoutLimits(firstReview: true);
        Configure(policy, new PaidCheckoutReservedExposure("EUR", 0, 0, 0, 0));

        PaidCheckoutActivationResult result = await Service().EvaluateAsync(
            new(_tenantId, _eventId, "EUR", 1_000, Now), CancellationToken.None);

        await Assert.That(result.IsActive).IsTrue();
        await _repository.DidNotReceive().HasApprovalAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(),
            PaidCheckoutReviewTrigger.FirstPaidEvent, Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HighValueReviewAppliesAtExactThreshold()
    {
        PaidEventPolicyVersion policy = Policy(PaidEventPolicyCurrencyRiskLimit.Create(
            "EUR", null, null, null, null, null, 5_000));
        Configure(policy, new PaidCheckoutReservedExposure("EUR", 0, 0, 0, 0));
        _repository.HasApprovalAsync(
            _tenantId, _eventId, _organizerId, policy.Id, "EUR",
            PaidCheckoutReviewTrigger.HighValue, 5_000, Arg.Any<CancellationToken>()).Returns(false);

        PaidCheckoutActivationResult result = await Service().EvaluateAsync(
            new(_tenantId, _eventId, "EUR", 5_000, Now), CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("payment_review_required");
        await _repository.Received().HasApprovalAsync(
            _tenantId, _eventId, _organizerId, policy.Id, "EUR",
            PaidCheckoutReviewTrigger.HighValue, 5_000, Arg.Any<CancellationToken>());
    }

    private PaidCheckoutActivationService Service(IPaidCheckoutGovernance? governance = null) =>
        new(_repository, _policies, _events, governance ?? Governance());

    private void Configure(PaidEventPolicyVersion policy, PaidCheckoutReservedExposure exposure)
    {
        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(policy);
        _events.GetEventWithDetailsAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(new DomainEvent(EventStatusEnum.Published)
        {
            Id = _eventId, TenantId = _tenantId, OrganizerActorId = _organizerId, Title = "Paid event",
            Actor = null!, Tenant = null!, VisibilityType = null!, EventStatus = null!, EventFormat = null!
        });
        _repository.GetSaleControlAsync(_tenantId, null, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            PaidCheckoutSaleControl.CreateActive(_tenantId, null, Guid.CreateVersion7(), Now));
        _repository.GetSaleControlAsync(_tenantId, _eventId, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns((PaidCheckoutSaleControl?)null);
        _repository.GetReservedExposureAsync(_tenantId, _eventId, _organizerId, "EUR", Arg.Any<DateTime?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(exposure);
        _repository.HasPriorSucceededPaymentAsync(_tenantId, _organizerId, Arg.Any<CancellationToken>()).Returns(true);
    }

    private static PaidEventPolicyVersion Policy(PaidEventPolicyCurrencyRiskLimit limit, bool firstReview = false)
    {
        PaidEventPolicyVersion disabled = PaidEventPolicyVersion.CreateDefaultInstance();
        return disabled.CreateRevision(true, disabled.AllowedOrganizerKinds, false, ["EUR"], "EUR",
            disabled.RefundProtections, [limit], firstReview, null);
    }

    private static PaidEventPolicyVersion PolicyWithoutLimits(bool firstReview = false)
    {
        PaidEventPolicyVersion disabled = PaidEventPolicyVersion.CreateDefaultInstance();
        return disabled.CreateRevision(true, disabled.AllowedOrganizerKinds, false, ["EUR"], "EUR",
            disabled.RefundProtections, [], firstReview, null);
    }

    private static IPaidCheckoutGovernance Governance(bool configured = true, bool activated = true)
    {
        var governance = Substitute.For<IPaidCheckoutGovernance>();
        governance.IsConfigured.Returns(configured);
        governance.IsActivated.Returns(activated);
        return governance;
    }
}
