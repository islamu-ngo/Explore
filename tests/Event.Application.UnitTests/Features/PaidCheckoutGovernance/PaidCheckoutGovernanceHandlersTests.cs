// ABOUTME: Verifies paid Checkout governance handlers preserve durable scope, state, and audit facts.
// ABOUTME: Distinguishes missing controls from mapped controls and requires unlocked query reads.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Payments;
using Explore.Application.Features.PaidCheckoutGovernance.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using DomainEvent = Explore.Domain.Event;

namespace Event.Application.UnitTests.Features.PaidCheckoutGovernance;

public sealed class PaidCheckoutGovernanceHandlersTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task QueryDistinguishesMissingControlAndMapsEveryDurableAuditFact()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid actorId = Guid.CreateVersion7();
        PaidCheckoutSaleControl control = PaidCheckoutSaleControl.CreateStopped(
            tenantId, eventId, actorId, "operator_stop", UtcNow);
        control.RequestResume(actorId, "resume_requested", UtcNow.AddMinutes(1));
        var repository = Substitute.For<IPaidCheckoutActivationRepository>();
        repository.GetSaleControlAsync(tenantId, eventId, false, Arg.Any<CancellationToken>())
            .Returns(control, (PaidCheckoutSaleControl?)null);
        var handler = new GetPaidCheckoutSaleControlQueryHandler(repository);
        var query = new GetPaidCheckoutSaleControlQuery(tenantId, eventId);

        PaidCheckoutSaleControlDto? mapped = await handler.Handle(query, CancellationToken.None);
        PaidCheckoutSaleControlDto? missing = await handler.Handle(query, CancellationToken.None);

        await Assert.That(mapped).IsNotNull();
        await Assert.That(mapped!.TenantId).IsEqualTo(tenantId);
        await Assert.That(mapped.EventId).IsEqualTo(eventId);
        await Assert.That(mapped.IsStopped).IsTrue();
        await Assert.That(mapped.ResumeReviewPending).IsTrue();
        await Assert.That(mapped.Version).IsEqualTo(2L);
        await Assert.That(mapped.AuditTrail.Select(entry => entry.Sequence)).IsEquivalentTo([1, 2]);
        await Assert.That(mapped.AuditTrail.Select(entry => entry.ActionCode))
            .IsEquivalentTo(["stopped", "resume_requested"]);
        await Assert.That(mapped.AuditTrail.Select(entry => entry.ReasonCode))
            .IsEquivalentTo(["operator_stop", "resume_requested"]);
        await Assert.That(mapped.AuditTrail.Select(entry => entry.OccurredAt))
            .IsEquivalentTo([UtcNow, UtcNow.AddMinutes(1)]);
        await Assert.That(missing).IsNull();
        await repository.Received(2).GetSaleControlAsync(
            tenantId, eventId, false, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StopUsesExactEventScopeLockedSerializablePersistenceAndOperatorAudit()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid actorId = Guid.CreateVersion7();
        PaidCheckoutSaleControl control = PaidCheckoutSaleControl.CreateActive(
            tenantId, eventId, Guid.CreateVersion7(), UtcNow.AddMinutes(-1));
        var repository = Substitute.For<IPaidCheckoutActivationRepository>();
        repository.GetSaleControlAsync(tenantId, eventId, true, Arg.Any<CancellationToken>()).Returns(control);
        var events = Substitute.For<IEventRepository>();
        events.GetAuthorizationTargetByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(Event(tenantId, eventId, Guid.CreateVersion7()));
        var unitOfWork = new RecordingSerializableUnitOfWork();
        var handler = new StopPaidCheckoutSalesCommandHandler(
            repository, events, CurrentUser(actorId), unitOfWork, new FixedTimeProvider(UtcNow));

        BaseCommandResponse<Guid> result = await handler.Handle(
            new StopPaidCheckoutSalesCommand(tenantId, eventId, "operator_stop"), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(control.Id);
        await Assert.That(control.IsStopped).IsTrue();
        await Assert.That(control.Version).IsEqualTo(2L);
        await Assert.That(control.UpdatedBy).IsEqualTo(actorId);
        PaidCheckoutSaleControlAudit audit = control.AuditTrail.Last();
        await Assert.That(audit.Sequence).IsEqualTo(2);
        await Assert.That(audit.ActionCode).IsEqualTo("stopped");
        await Assert.That(audit.ReasonCode).IsEqualTo("operator_stop");
        await Assert.That(audit.ActorUserId).IsEqualTo(actorId);
        await Assert.That(audit.EventId).IsEqualTo(eventId);
        await Assert.That(audit.OccurredAt).IsEqualTo(UtcNow);
        await Assert.That(unitOfWork.SerializableExecutions).IsEqualTo(1);
        await repository.Received(1).GetSaleControlAsync(
            tenantId, eventId, true, Arg.Any<CancellationToken>());
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await repository.DidNotReceive().AddSaleControlAsync(
            Arg.Any<PaidCheckoutSaleControl>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StopRejectsAnonymousEmptyAndCrossTenantScopesBeforeSerializableExecution()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid actorId = Guid.CreateVersion7();
        var repository = Substitute.For<IPaidCheckoutActivationRepository>();
        var events = Substitute.For<IEventRepository>();
        events.GetAuthorizationTargetByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(Event(Guid.CreateVersion7(), eventId, Guid.CreateVersion7()));
        var unitOfWork = new RecordingSerializableUnitOfWork();
        var time = new FixedTimeProvider(UtcNow);

        BaseCommandResponse<Guid> anonymous = await new StopPaidCheckoutSalesCommandHandler(
            repository, events, CurrentUser(null), unitOfWork, time).Handle(
            new StopPaidCheckoutSalesCommand(tenantId, null, "operator_stop"), CancellationToken.None);
        BaseCommandResponse<Guid> emptyTenant = await new StopPaidCheckoutSalesCommandHandler(
            repository, events, CurrentUser(actorId), unitOfWork, time).Handle(
            new StopPaidCheckoutSalesCommand(Guid.Empty, null, "operator_stop"), CancellationToken.None);
        BaseCommandResponse<Guid> emptyEvent = await new StopPaidCheckoutSalesCommandHandler(
            repository, events, CurrentUser(actorId), unitOfWork, time).Handle(
            new StopPaidCheckoutSalesCommand(tenantId, Guid.Empty, "operator_stop"), CancellationToken.None);
        BaseCommandResponse<Guid> crossTenant = await new StopPaidCheckoutSalesCommandHandler(
            repository, events, CurrentUser(actorId), unitOfWork, time).Handle(
            new StopPaidCheckoutSalesCommand(tenantId, eventId, "operator_stop"), CancellationToken.None);

        await Assert.That(anonymous.Success).IsFalse();
        await Assert.That(emptyTenant.Success).IsFalse();
        await Assert.That(emptyEvent.Success).IsFalse();
        await Assert.That(crossTenant.Success).IsFalse();
        await Assert.That(anonymous.FailureCode).IsEqualTo("paid_checkout_governance_invalid");
        await Assert.That(emptyTenant.FailureCode).IsEqualTo("paid_checkout_governance_invalid");
        await Assert.That(emptyEvent.FailureCode).IsEqualTo("paid_checkout_governance_invalid");
        await Assert.That(crossTenant.FailureCode).IsEqualTo("paid_checkout_governance_invalid");
        await Assert.That(unitOfWork.SerializableExecutions).IsEqualTo(0);
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResumeRequestBootstrapsTenantControlAndPersistsPendingOperatorAudit()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorId = Guid.CreateVersion7();
        var repository = Substitute.For<IPaidCheckoutActivationRepository>();
        repository.GetSaleControlAsync(tenantId, null, true, Arg.Any<CancellationToken>())
            .Returns((PaidCheckoutSaleControl?)null);
        PaidCheckoutSaleControl? added = null;
        repository.AddSaleControlAsync(
                Arg.Do<PaidCheckoutSaleControl>(control => added = control), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var events = Substitute.For<IEventRepository>();
        var unitOfWork = new RecordingSerializableUnitOfWork();
        var handler = new RequestPaidCheckoutResumeCommandHandler(
            repository, events, CurrentUser(actorId), unitOfWork, new FixedTimeProvider(UtcNow));

        BaseCommandResponse<Guid> result = await handler.Handle(
            new RequestPaidCheckoutResumeCommand(tenantId, null, "operator_resume"), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(added).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(added!.Id);
        await Assert.That(added.TenantId).IsEqualTo(tenantId);
        await Assert.That(added.EventId).IsNull();
        await Assert.That(added.IsStopped).IsTrue();
        await Assert.That(added.ResumeRequestedBy).IsEqualTo(actorId);
        await Assert.That(added.Version).IsEqualTo(2L);
        await Assert.That(added.CreatedBy).IsEqualTo(actorId);
        await Assert.That(added.UpdatedBy).IsEqualTo(actorId);
        await Assert.That(added.AuditTrail.Select(entry => entry.ActionCode).SequenceEqual(
            ["stopped", "resume_requested"])).IsTrue();
        await Assert.That(added.AuditTrail.Select(entry => entry.ReasonCode).SequenceEqual(
            ["initial_activation_required", "operator_resume"])).IsTrue();
        await Assert.That(added.AuditTrail.All(entry => entry.ActorUserId == actorId)).IsTrue();
        await Assert.That(unitOfWork.SerializableExecutions).IsEqualTo(1);
        await repository.Received(1).AddSaleControlAsync(added, Arg.Any<CancellationToken>());
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await events.DidNotReceive().GetAuthorizationTargetByIdAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResumeReviewAppliesApprovalAndRejectionWithIndependentReviewerAudit()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid requesterId = Guid.CreateVersion7();
        Guid reviewerId = Guid.CreateVersion7();
        PaidCheckoutSaleControl approved = PendingControl(tenantId, null, requesterId);
        Guid rejectedEventId = Guid.CreateVersion7();
        PaidCheckoutSaleControl rejected = PendingControl(tenantId, rejectedEventId, requesterId);
        var repository = Substitute.For<IPaidCheckoutActivationRepository>();
        repository.GetSaleControlAsync(tenantId, null, true, Arg.Any<CancellationToken>()).Returns(approved);
        repository.GetSaleControlAsync(tenantId, rejectedEventId, true, Arg.Any<CancellationToken>()).Returns(rejected);
        var events = Substitute.For<IEventRepository>();
        events.GetAuthorizationTargetByIdAsync(rejectedEventId, Arg.Any<CancellationToken>())
            .Returns(Event(tenantId, rejectedEventId, Guid.CreateVersion7()));
        var unitOfWork = new RecordingSerializableUnitOfWork();
        var handler = new ReviewPaidCheckoutResumeCommandHandler(
            repository, events, CurrentUser(reviewerId), unitOfWork, new FixedTimeProvider(UtcNow));

        BaseCommandResponse<Guid> approvedResult = await handler.Handle(
            new ReviewPaidCheckoutResumeCommand(tenantId, null, true, "independent_approval"),
            CancellationToken.None);
        BaseCommandResponse<Guid> rejectedResult = await handler.Handle(
            new ReviewPaidCheckoutResumeCommand(tenantId, rejectedEventId, false, "independent_rejection"),
            CancellationToken.None);

        await Assert.That(approvedResult.Success).IsTrue();
        await Assert.That(rejectedResult.Success).IsTrue();
        await Assert.That(approvedResult.Id).IsEqualTo(approved.Id);
        await Assert.That(rejectedResult.Id).IsEqualTo(rejected.Id);
        await Assert.That(approved.IsStopped).IsFalse();
        await Assert.That(rejected.IsStopped).IsTrue();
        await Assert.That(approved.ResumeRequestedBy).IsNull();
        await Assert.That(rejected.ResumeRequestedBy).IsNull();
        await Assert.That(approved.ResumeReviewedBy).IsEqualTo(reviewerId);
        await Assert.That(rejected.ResumeReviewedBy).IsEqualTo(reviewerId);
        await Assert.That(approved.UpdatedBy).IsEqualTo(reviewerId);
        await Assert.That(rejected.UpdatedBy).IsEqualTo(reviewerId);
        await Assert.That(approved.Version).IsEqualTo(3L);
        await Assert.That(rejected.Version).IsEqualTo(3L);
        await Assert.That(approved.AuditTrail.Last().ActionCode).IsEqualTo("resume_approved");
        await Assert.That(rejected.AuditTrail.Last().ActionCode).IsEqualTo("resume_rejected");
        await Assert.That(approved.AuditTrail.Last().ActorUserId).IsEqualTo(reviewerId);
        await Assert.That(rejected.AuditTrail.Last().ActorUserId).IsEqualTo(reviewerId);
        await Assert.That(approved.AuditTrail.Last().SubjectUserId).IsEqualTo(requesterId);
        await Assert.That(rejected.AuditTrail.Last().SubjectUserId).IsEqualTo(requesterId);
        await Assert.That(unitOfWork.SerializableExecutions).IsEqualTo(2);
        await repository.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResumeReviewRejectsRequesterAsReviewerWithoutPersistence()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid requesterId = Guid.CreateVersion7();
        PaidCheckoutSaleControl control = PendingControl(tenantId, null, requesterId);
        var repository = Substitute.For<IPaidCheckoutActivationRepository>();
        repository.GetSaleControlAsync(tenantId, null, true, Arg.Any<CancellationToken>()).Returns(control);
        var unitOfWork = new RecordingSerializableUnitOfWork();
        var handler = new ReviewPaidCheckoutResumeCommandHandler(
            repository, Substitute.For<IEventRepository>(), CurrentUser(requesterId), unitOfWork,
            new FixedTimeProvider(UtcNow));

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ReviewPaidCheckoutResumeCommand(tenantId, null, true, "self_approval"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("paid_checkout_resume_review_invalid");
        await Assert.That(control.IsStopped).IsTrue();
        await Assert.That(control.ResumeRequestedBy).IsEqualTo(requesterId);
        await Assert.That(control.Version).IsEqualTo(2L);
        await Assert.That(unitOfWork.SerializableExecutions).IsEqualTo(1);
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReviewRequestPersistsExactOrganizerPolicyAndHighValueAuthority()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid organizerId = Guid.CreateVersion7();
        Guid requesterId = Guid.CreateVersion7();
        PaidEventPolicyVersion policy = EnabledPolicy(highValueThresholdMinor: 5_000);
        var repository = Substitute.For<IPaidCheckoutActivationRepository>();
        PaidCheckoutReviewApproval? added = null;
        repository.AddReviewAsync(
                Arg.Do<PaidCheckoutReviewApproval>(review => added = review), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var policies = Substitute.For<IPaidEventPolicyRepository>();
        policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(policy);
        policies.GetActiveTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns((PaidEventPolicyVersion?)null);
        var events = Substitute.For<IEventRepository>();
        events.GetEventWithDetails(eventId).Returns(Event(tenantId, eventId, organizerId));
        var unitOfWork = new RecordingSerializableUnitOfWork();
        var handler = new RequestPaidCheckoutReviewCommandHandler(
            repository, policies, events, CurrentUser(requesterId), unitOfWork,
            new FixedTimeProvider(UtcNow));

        BaseCommandResponse<Guid> result = await handler.Handle(new RequestPaidCheckoutReviewCommand(
            tenantId, eventId, (int)PaidCheckoutReviewTrigger.HighValue, "EUR", 5_000, "risk_review"),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(added).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(added!.Id);
        await Assert.That(added.TenantId).IsEqualTo(tenantId);
        await Assert.That(added.EventId).IsEqualTo(eventId);
        await Assert.That(added.OrganizerActorId).IsEqualTo(organizerId);
        await Assert.That(added.PaidEventPolicyVersionId).IsEqualTo(policy.Id);
        await Assert.That(added.CurrencyCode).IsEqualTo("EUR");
        await Assert.That(added.Trigger).IsEqualTo(PaidCheckoutReviewTrigger.HighValue);
        await Assert.That(added.MaximumOrderAmountMinor).IsEqualTo(5_000L);
        await Assert.That(added.StatusCode).IsEqualTo("pending");
        await Assert.That(added.RequestedByUserId).IsEqualTo(requesterId);
        await Assert.That(added.RequestedAt).IsEqualTo(UtcNow);
        await Assert.That(unitOfWork.SerializableExecutions).IsEqualTo(1);
        await repository.Received(1).AddReviewAsync(added, Arg.Any<CancellationToken>());
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReviewRequestRejectsUndefinedTriggerBeforeTransactionAndCrossTenantOrganizerInsideTransaction()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid requesterId = Guid.CreateVersion7();
        var repository = Substitute.For<IPaidCheckoutActivationRepository>();
        var policies = Substitute.For<IPaidEventPolicyRepository>();
        var events = Substitute.For<IEventRepository>();
        events.GetEventWithDetails(eventId)
            .Returns(Event(Guid.CreateVersion7(), eventId, Guid.CreateVersion7()));
        var unitOfWork = new RecordingSerializableUnitOfWork();
        var handler = new RequestPaidCheckoutReviewCommandHandler(
            repository, policies, events, CurrentUser(requesterId), unitOfWork,
            new FixedTimeProvider(UtcNow));

        BaseCommandResponse<Guid> undefinedTrigger = await handler.Handle(new RequestPaidCheckoutReviewCommand(
            tenantId, eventId, 999, "EUR", null, "risk_review"), CancellationToken.None);
        BaseCommandResponse<Guid> crossTenant = await handler.Handle(new RequestPaidCheckoutReviewCommand(
            tenantId, eventId, (int)PaidCheckoutReviewTrigger.FirstPaidEvent, "EUR", null, "risk_review"),
            CancellationToken.None);

        await Assert.That(undefinedTrigger.Success).IsFalse();
        await Assert.That(crossTenant.Success).IsFalse();
        await Assert.That(undefinedTrigger.FailureCode).IsEqualTo("paid_checkout_review_invalid");
        await Assert.That(crossTenant.FailureCode).IsEqualTo("paid_checkout_review_invalid");
        await Assert.That(unitOfWork.SerializableExecutions).IsEqualTo(1);
        await policies.DidNotReceive().GetActiveInstanceAsync(Arg.Any<CancellationToken>());
        await repository.DidNotReceive().AddReviewAsync(
            Arg.Any<PaidCheckoutReviewApproval>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReviewDecisionAppliesApprovalAndRejectionUsingLockedTenantQualifiedReads()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid requesterId = Guid.CreateVersion7();
        Guid reviewerId = Guid.CreateVersion7();
        PaidCheckoutReviewApproval approved = PendingReview(tenantId, requesterId, 5_000);
        PaidCheckoutReviewApproval rejected = PendingReview(tenantId, requesterId, 6_000);
        var repository = Substitute.For<IPaidCheckoutActivationRepository>();
        repository.GetReviewAsync(tenantId, approved.Id, true, Arg.Any<CancellationToken>()).Returns(approved);
        repository.GetReviewAsync(tenantId, rejected.Id, true, Arg.Any<CancellationToken>()).Returns(rejected);
        var unitOfWork = new RecordingSerializableUnitOfWork();
        var handler = new DecidePaidCheckoutReviewCommandHandler(
            repository, CurrentUser(reviewerId), unitOfWork, new FixedTimeProvider(UtcNow));

        BaseCommandResponse<Guid> approvedResult = await handler.Handle(
            new DecidePaidCheckoutReviewCommand(tenantId, approved.Id, true, "risk_accepted"),
            CancellationToken.None);
        BaseCommandResponse<Guid> rejectedResult = await handler.Handle(
            new DecidePaidCheckoutReviewCommand(tenantId, rejected.Id, false, "risk_rejected"),
            CancellationToken.None);

        await Assert.That(approvedResult.Success).IsTrue();
        await Assert.That(rejectedResult.Success).IsTrue();
        await Assert.That(approvedResult.Id).IsEqualTo(approved.Id);
        await Assert.That(rejectedResult.Id).IsEqualTo(rejected.Id);
        await Assert.That(approved.StatusCode).IsEqualTo("approved");
        await Assert.That(rejected.StatusCode).IsEqualTo("rejected");
        await Assert.That(approved.ReviewedByUserId).IsEqualTo(reviewerId);
        await Assert.That(rejected.ReviewedByUserId).IsEqualTo(reviewerId);
        await Assert.That(approved.UpdatedBy).IsEqualTo(reviewerId);
        await Assert.That(rejected.UpdatedBy).IsEqualTo(reviewerId);
        await Assert.That(approved.ReviewedAt).IsEqualTo(UtcNow);
        await Assert.That(rejected.ReviewedAt).IsEqualTo(UtcNow);
        await Assert.That(unitOfWork.SerializableExecutions).IsEqualTo(2);
        await repository.Received(1).GetReviewAsync(
            tenantId, approved.Id, true, Arg.Any<CancellationToken>());
        await repository.Received(1).GetReviewAsync(
            tenantId, rejected.Id, true, Arg.Any<CancellationToken>());
        await repository.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReviewDecisionRejectsSelfReviewAndConvertsInvalidReasonWithoutPersistence()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid requesterId = Guid.CreateVersion7();
        Guid reviewerId = Guid.CreateVersion7();
        PaidCheckoutReviewApproval selfReview = PendingReview(tenantId, requesterId, 5_000);
        PaidCheckoutReviewApproval invalidReason = PendingReview(tenantId, requesterId, 6_000);
        var repository = Substitute.For<IPaidCheckoutActivationRepository>();
        repository.GetReviewAsync(tenantId, selfReview.Id, true, Arg.Any<CancellationToken>()).Returns(selfReview);
        repository.GetReviewAsync(tenantId, invalidReason.Id, true, Arg.Any<CancellationToken>()).Returns(invalidReason);
        var unitOfWork = new RecordingSerializableUnitOfWork();

        BaseCommandResponse<Guid> selfResult = await new DecidePaidCheckoutReviewCommandHandler(
            repository, CurrentUser(requesterId), unitOfWork, new FixedTimeProvider(UtcNow)).Handle(
            new DecidePaidCheckoutReviewCommand(tenantId, selfReview.Id, true, "self_approval"),
            CancellationToken.None);
        BaseCommandResponse<Guid> invalidReasonResult = await new DecidePaidCheckoutReviewCommandHandler(
            repository, CurrentUser(reviewerId), unitOfWork, new FixedTimeProvider(UtcNow)).Handle(
            new DecidePaidCheckoutReviewCommand(tenantId, invalidReason.Id, false, string.Empty),
            CancellationToken.None);

        await Assert.That(selfResult.Success).IsFalse();
        await Assert.That(invalidReasonResult.Success).IsFalse();
        await Assert.That(selfResult.FailureCode).IsEqualTo("paid_checkout_review_invalid");
        await Assert.That(invalidReasonResult.FailureCode).IsEqualTo("paid_checkout_review_invalid");
        await Assert.That(selfReview.StatusCode).IsEqualTo("pending");
        await Assert.That(unitOfWork.SerializableExecutions).IsEqualTo(2);
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static PaidCheckoutSaleControl PendingControl(Guid tenantId, Guid? eventId, Guid requesterId)
    {
        PaidCheckoutSaleControl control = PaidCheckoutSaleControl.CreateStopped(
            tenantId, eventId, Guid.CreateVersion7(), "operator_stop", UtcNow.AddMinutes(-2));
        control.RequestResume(requesterId, "operator_resume", UtcNow.AddMinutes(-1));
        return control;
    }

    private static PaidCheckoutReviewApproval PendingReview(
        Guid tenantId,
        Guid requesterId,
        long maximumOrderAmountMinor) => PaidCheckoutReviewApproval.Request(
        tenantId,
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        "EUR",
        PaidCheckoutReviewTrigger.HighValue,
        maximumOrderAmountMinor,
        requesterId,
        "risk_review",
        UtcNow.AddMinutes(-1));

    private static PaidEventPolicyVersion EnabledPolicy(long highValueThresholdMinor)
    {
        PaidEventPolicyVersion initial = PaidEventPolicyVersion.CreateDefaultInstance();
        return initial.CreateRevision(
            true,
            initial.AllowedOrganizerKinds,
            false,
            ["EUR"],
            "EUR",
            initial.RefundProtections,
            [PaidEventPolicyCurrencyRiskLimit.Create(
                "EUR", null, null, null, null, null, highValueThresholdMinor)],
            false,
            null);
    }

    private static DomainEvent Event(Guid tenantId, Guid eventId, Guid organizerId) => new(EventStatusEnum.Published)
    {
        Id = eventId,
        TenantId = tenantId,
        OrganizerActorId = organizerId,
        Title = "Paid event",
        Actor = null!,
        Tenant = null!,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!
    };

    private static ICurrentUserService CurrentUser(Guid? userId)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        currentUser.IsAuthenticated.Returns(userId.HasValue);
        return currentUser;
    }

    private sealed class RecordingSerializableUnitOfWork : IUnitOfWork
    {
        public int SerializableExecutions { get; private set; }

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default) => throw new InvalidOperationException("A serializable transaction is required.");

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => throw new InvalidOperationException("A serializable transaction is required.");

        public async Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
        {
            SerializableExecutions++;
            return await operation(ct);
        }
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
