// ABOUTME: Verifies fenced PDS delivery processing and its final no-network governance gate.
// ABOUTME: Ensures denied or stale claims never call the PDS and successful delivery settles with URI/CID.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Domain.Federation;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Federation;

public sealed class AtprotoPdsDeliveryProcessorTests
{
    private static readonly DateTime Now = new(2026, 7, 19, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task ProcessAsync_WhenInitialGateDenies_FailsWithoutPdsCall()
    {
        var fixture = Fixture();
        fixture.Gate.CheckDeliveryAsync(
                fixture.Outbox,
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(AtprotoDeliveryGateResult.Deny("consent_missing"));
        fixture.Repository.TryFailAsync(
                fixture.Claim,
                "consent_missing",
                false,
                Arg.Any<DateTime>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        AtprotoPdsClaimResult outcome = await fixture.Processor.ProcessAsync(
            fixture.Claim,
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        await Assert.That(outcome.Outcome).IsEqualTo(AtprotoPdsClaimOutcome.DeliveryFailed);
        await Assert.That(outcome.FailureCode).IsEqualTo("consent_missing");
        await Assert.That(outcome.FailureDisposition).IsEqualTo(AtprotoPdsFailureDisposition.DeadLettered);
        await fixture.Gateway.DidNotReceiveWithAnyArgs().DeliverAsync(default!, default);
    }

    [Test]
    public async Task ProcessAsync_WhenLeaseRenewalLosesFence_DoesNotCallPds()
    {
        var fixture = Fixture();
        fixture.Gate.CheckDeliveryAsync(
                fixture.Outbox,
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(AtprotoDeliveryGateResult.Permit());
        fixture.Repository.TryRenewClaimAsync(
                fixture.Claim,
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        AtprotoPdsClaimResult outcome = await fixture.Processor.ProcessAsync(
            fixture.Claim,
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        await Assert.That(outcome.Outcome).IsEqualTo(AtprotoPdsClaimOutcome.ClaimLost);
        await fixture.Gateway.DidNotReceiveWithAnyArgs().DeliverAsync(default!, default);
    }

    [Test]
    public async Task ProcessAsync_WhenBothGatesPermit_DeliversAndSettlesFencedClaim()
    {
        var fixture = Fixture();
        fixture.Gate.CheckDeliveryAsync(
                fixture.Outbox,
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(AtprotoDeliveryGateResult.Permit(), AtprotoDeliveryGateResult.Permit());
        fixture.Repository.TryRenewClaimAsync(
                fixture.Claim,
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.Gateway.DeliverAsync(
                Arg.Any<AtprotoPdsDeliveryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(AtprotoPdsDeliveryResult.Success(
                "at://did:plc:alice/community.lexicon.calendar.event/key",
                "bafy-settled",
                "bafy-observed-base"));
        fixture.Repository.TrySettleAsync(
                fixture.Claim,
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<string?>())
            .Returns(true);

        AtprotoPdsClaimResult outcome = await fixture.Processor.ProcessAsync(
            fixture.Claim,
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        await Assert.That(outcome.Outcome).IsEqualTo(AtprotoPdsClaimOutcome.Delivered);
        await fixture.Gate.Received(2).CheckDeliveryAsync(
            fixture.Outbox,
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(2).TryRenewClaimAsync(
            fixture.Claim,
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).TrySettleAsync(
            fixture.Claim,
            "at://did:plc:alice/community.lexicon.calendar.event/key",
            "bafy-settled",
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>(),
            "bafy-observed-base");
    }

    [Test]
    public async Task ProcessAsync_WhenPostRemoteRenewalLosesFence_DoesNotSettleStaleClaim()
    {
        var fixture = Fixture();
        fixture.Gate.CheckDeliveryAsync(
                fixture.Outbox,
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(AtprotoDeliveryGateResult.Permit());
        fixture.Repository.TryRenewClaimAsync(
                fixture.Claim,
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true, false);
        fixture.Gateway.DeliverAsync(
                Arg.Any<AtprotoPdsDeliveryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(AtprotoPdsDeliveryResult.Success(
                "at://did:plc:alice/community.lexicon.calendar.event/key",
                "bafy-settled"));

        AtprotoPdsClaimResult outcome = await fixture.Processor.ProcessAsync(
            fixture.Claim,
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        await Assert.That(outcome.Outcome).IsEqualTo(AtprotoPdsClaimOutcome.ClaimLost);
        await fixture.Gateway.Received(1).DeliverAsync(
            Arg.Any<AtprotoPdsDeliveryRequest>(),
            Arg.Any<CancellationToken>());
        await fixture.Repository.DidNotReceiveWithAnyArgs().TrySettleAsync(
            default!, default, default, default, default, default);
    }

    [Test]
    public async Task ProcessAsync_WhenCompensationLineageIsIncomplete_FailsBeforeGateway()
    {
        var fixture = Fixture(PdsSyncOperation.Update);
        fixture.Gate.CheckDeliveryAsync(
                fixture.Outbox,
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(AtprotoDeliveryGateResult.Permit());
        fixture.Repository.TryRenewClaimAsync(
                fixture.Claim,
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        fixture.Repository.GetCompensationEvidenceAsync(
                fixture.Outbox,
                Arg.Any<CancellationToken>())
            .Returns(new PdsSyncCompensationEvidence([], [], IsComplete: false));
        fixture.Repository.TryFailAsync(
                fixture.Claim,
                "record_conflict",
                false,
                Arg.Any<DateTime>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        AtprotoPdsClaimResult outcome = await fixture.Processor.ProcessAsync(
            fixture.Claim,
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        await Assert.That(outcome.FailureCode).IsEqualTo("record_conflict");
        await Assert.That(outcome.FailureDisposition).IsEqualTo(AtprotoPdsFailureDisposition.DeadLettered);
        await fixture.Gateway.DidNotReceiveWithAnyArgs().DeliverAsync(default!, default);
    }

    private static TestFixture Fixture(PdsSyncOperation operation = PdsSyncOperation.Create)
    {
        var repository = Substitute.For<IPdsSyncOutboxRepository>();
        var gate = Substitute.For<IAtprotoDeliveryGate>();
        var gateway = Substitute.For<IAtprotoPdsDeliveryGateway>();
        var outbox = Outbox(operation);
        var claim = new PdsSyncClaim(
            outbox.Id,
            outbox.TenantId,
            outbox.UserId,
            Guid.Parse("0198ab00-0000-7000-8000-000000000005"),
            3);
        repository.GetActiveClaimAsync(
                claim,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(outbox);
        return new TestFixture(
            repository,
            gate,
            gateway,
            outbox,
            claim,
            new AtprotoPdsDeliveryProcessor(
                repository,
                gate,
                gateway,
                new FixedTimeProvider(Now)));
    }

    private static PdsSyncOutbox Outbox(PdsSyncOperation operation) => new()
    {
        Id = Guid.Parse("0198ab00-0000-7000-8000-000000000001"),
        TenantId = Guid.Parse("0198ab00-0000-7000-8000-000000000002"),
        UserId = Guid.Parse("0198ab00-0000-7000-8000-000000000003"),
        Did = "did:plc:alice",
        Collection = AtprotoEventPublicationPlanner.EventCollection,
        RecordKey = "key",
        Operation = operation,
        Payload = operation == PdsSyncOperation.Delete ? null : "{}",
        PayloadHash = "hash",
        IdempotencyKey = "idempotency",
        PdsHost = "https://pds.example/",
        SourceEntityType = AtprotoEventPublicationPlanner.EventSourceType,
        SourceEntityId = Guid.Parse("0198ab00-0000-7000-8000-000000000004"),
        SourceVersion = Guid.Parse("0198ab00-0000-7000-8000-000000000006"),
        Status = PdsSyncStatus.Processing,
        CreatedAt = Now,
        MaxRetries = 10
    };

    private sealed record TestFixture(
        IPdsSyncOutboxRepository Repository,
        IAtprotoDeliveryGate Gate,
        IAtprotoPdsDeliveryGateway Gateway,
        PdsSyncOutbox Outbox,
        PdsSyncClaim Claim,
        AtprotoPdsDeliveryProcessor Processor);

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now);
    }
}
