// ABOUTME: Proves payment-attempt active-slot and checkout-dispatch effect persistence semantics.
// ABOUTME: Uses SQLite for deterministic duplicate, tenant isolation, terminal release, and lease-fence checks.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Payments;
using Explore.Application.Contracts.Services;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests;

public sealed class RegistrationPaymentAttemptPersistenceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly Guid OtherTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000002");
    private static readonly Guid OrderId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000010");

    [Test]
    public async Task ClaimAsyncCreatesAttemptAndDispatchEffectOncePerTenantOrderAcrossCompositionRevisions()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        RegistrationPaymentAttemptClaim claim = Claim(TenantId, OrderId, "composition-a");

        RegistrationPaymentAttemptClaimOutcome first = await repository.ClaimAsync(claim, CancellationToken.None);
        typeof(PaymentAttempt).GetProperty(nameof(PaymentAttempt.ActiveScopeKey))!
            .SetValue(first.Attempt, $"{TenantId:N}|{OrderId:N}|composition-a");
        await repository.SaveChangesAsync(CancellationToken.None);
        RegistrationPaymentAttemptClaimOutcome duplicate = await repository.ClaimAsync(
            Claim(TenantId, OrderId, "composition-b"), CancellationToken.None);

        await Assert.That(first.Created).IsTrue();
        await Assert.That(duplicate.Created).IsFalse();
        await Assert.That(duplicate.Attempt.Id).IsEqualTo(first.Attempt.Id);
        await Assert.That(duplicate.Attempt.CompositionRevision).IsEqualTo("composition-a");
        await Assert.That(duplicate.DispatchEffect.Id).IsEqualTo(first.DispatchEffect.Id);
        await Assert.That(await context.PaymentAttempts.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.CheckoutDispatchEffects.CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task ClaimAsyncAllowsTenantIsolationAndSafeTerminalRetryButKeepsUnknownActive()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        RegistrationPaymentAttemptClaim primary = Claim(TenantId, OrderId, "composition-a");
        RegistrationPaymentAttemptClaim otherTenant = Claim(OtherTenantId, OrderId, "composition-a");

        RegistrationPaymentAttemptClaimOutcome first = await repository.ClaimAsync(primary, CancellationToken.None);
        _ = await repository.ClaimAsync(otherTenant, CancellationToken.None);
        first.Attempt.MarkFailed("pi_failed", UtcNow.AddSeconds(1), "req-failed");
        await repository.ReleaseActiveSlotAsync(first.Attempt, UtcNow.AddSeconds(1), CancellationToken.None);
        RegistrationPaymentAttemptClaimOutcome retry = await repository.ClaimAsync(Claim(TenantId, OrderId, "composition-b"), CancellationToken.None);
        retry.Attempt.MarkUnknown(UtcNow.AddSeconds(2), "req-timeout");
        await repository.SaveChangesAsync(CancellationToken.None);
        RegistrationPaymentAttemptClaimOutcome unknownDuplicate = await repository.ClaimAsync(Claim(TenantId, OrderId, "composition-b"), CancellationToken.None);

        await Assert.That(retry.Created).IsTrue();
        await Assert.That(unknownDuplicate.Created).IsFalse();
        await Assert.That(await context.PaymentAttempts.CountAsync()).IsEqualTo(3);
    }

    [Test]
    public async Task ClaimDueAsyncUsesLeaseTokenAndFenceForIdempotentCompleteRetryParkAndUnknown()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        RegistrationPaymentAttemptClaimOutcome outcome = await repository.ClaimAsync(Claim(TenantId, OrderId, "composition-a"), CancellationToken.None);

        CheckoutDispatchClaim claim = (await repository.ClaimDueDispatchEffectsAsync("worker-a", 1, UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), CancellationToken.None)).Single();
        IReadOnlyList<CheckoutDispatchClaim> premature = await repository.ClaimDueDispatchEffectsAsync("worker-b", 1, UtcNow.AddSeconds(30), TimeSpan.FromMinutes(1), CancellationToken.None);
        IReadOnlyList<CheckoutDispatchClaim> recovered = await repository.ClaimDueDispatchEffectsAsync("worker-c", 1, UtcNow.AddMinutes(1).AddSeconds(1), TimeSpan.FromMinutes(1), CancellationToken.None);
        bool staleComplete = await repository.CompleteDispatchAsync(claim, UtcNow.AddMinutes(1).AddSeconds(2), CancellationToken.None);
        bool retried = await repository.RetryDispatchAsync(recovered.Single(), UtcNow.AddMinutes(2), UtcNow.AddMinutes(1).AddSeconds(2), CancellationToken.None);

        await Assert.That(outcome.DispatchEffect.Status).IsEqualTo(OutboxMessageStatus.Pending);
        await Assert.That((await context.CheckoutDispatchEffects.AsNoTracking().SingleAsync(value => value.Id == outcome.DispatchEffect.Id)).Status)
            .IsEqualTo(OutboxMessageStatus.Failed);
        await Assert.That(premature).IsEmpty();
        await Assert.That(staleComplete).IsFalse();
        await Assert.That(retried).IsTrue();
        await Assert.That(claim.AttemptCount).IsEqualTo(1);
        await Assert.That(recovered.Single().AttemptCount).IsEqualTo(2);
        CheckoutDispatchEffect effect = await context.CheckoutDispatchEffects.AsNoTracking().SingleAsync();
        await Assert.That(effect.AttemptCount).IsEqualTo(2);
        await Assert.That(effect.ProcessingFence).IsEqualTo(2);
    }

    [Test]
    public async Task ClaimDueAsyncAllowsOnlyOneConcurrentWinnerForOneEffect()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"phase18-claim-race-{Guid.NewGuid():N}.db");
        try
        {
            await using (ExploreDbContext seed = await CreateFileContextAsync(databasePath))
            {
                var seedRepository = new RegistrationPaymentAttemptRepository(seed);
                _ = await seedRepository.ClaimAsync(Claim(TenantId, OrderId, "composition-race"), CancellationToken.None);
            }

            await using ExploreDbContext firstContext = await CreateFileContextAsync(databasePath, ensureCreated: false);
            await using ExploreDbContext secondContext = await CreateFileContextAsync(databasePath, ensureCreated: false);
            var firstRepository = new RegistrationPaymentAttemptRepository(firstContext);
            var secondRepository = new RegistrationPaymentAttemptRepository(secondContext);

            IReadOnlyList<CheckoutDispatchClaim>[] results = await Task.WhenAll(
                firstRepository.ClaimDueDispatchEffectsAsync("worker-a", 1, UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), CancellationToken.None),
                secondRepository.ClaimDueDispatchEffectsAsync("worker-b", 1, UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), CancellationToken.None));

            await Assert.That(results.Sum(result => result.Count)).IsEqualTo(1);
            await Assert.That(results.SelectMany(result => result).Select(claim => claim.EffectId).Distinct()).Count().IsEqualTo(1);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Test]
    public async Task MarkDispatchUnknownAsyncParksEffectUntilExplicitRequeueWithoutCreatingAnotherAttempt()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        RegistrationPaymentAttemptClaimOutcome outcome = await repository.ClaimAsync(Claim(TenantId, OrderId, "composition-unknown"), CancellationToken.None);
        CheckoutDispatchClaim claim = (await repository.ClaimDueDispatchEffectsAsync("worker-a", 1, UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), CancellationToken.None)).Single();

        bool markedUnknown = await repository.MarkDispatchUnknownAsync(claim, UtcNow.AddSeconds(2), CancellationToken.None);
        IReadOnlyList<CheckoutDispatchClaim> reclaimed = await repository.ClaimDueDispatchEffectsAsync("worker-b", 1, UtcNow.AddMinutes(10), TimeSpan.FromMinutes(1), CancellationToken.None);
        RegistrationPaymentAttemptClaimOutcome duplicate = await repository.ClaimAsync(Claim(TenantId, OrderId, "composition-unknown"), CancellationToken.None);
        bool requeued = await repository.RequeueUnknownDispatchAsync(TenantId, claim.EffectId, UtcNow.AddSeconds(2), claim.ProcessingFence, 1, UtcNow.AddMinutes(11), UtcNow.AddMinutes(10), CancellationToken.None);
        IReadOnlyList<CheckoutDispatchClaim> afterRequeue = await repository.ClaimDueDispatchEffectsAsync("worker-c", 1, UtcNow.AddMinutes(11), TimeSpan.FromMinutes(1), CancellationToken.None);

        await Assert.That(markedUnknown).IsTrue();
        await Assert.That(reclaimed).IsEmpty();
        await Assert.That(duplicate.Created).IsFalse();
        await Assert.That(duplicate.Attempt.Id).IsEqualTo(outcome.Attempt.Id);
        await Assert.That(requeued).IsTrue();
        await Assert.That(afterRequeue).HasSingleItem();
        await Assert.That(afterRequeue.Single().ReplayKind).IsEqualTo(CheckoutDispatchReplayKind.UnknownRedrive);
        CheckoutDispatchEffect effect = await context.CheckoutDispatchEffects.AsNoTracking().SingleAsync(value => value.Id == claim.EffectId);
        await Assert.That(effect.Status).IsEqualTo(OutboxMessageStatus.Processing);
        await Assert.That(effect.UnknownAt).IsEqualTo(UtcNow.AddSeconds(2));
    }

    [Test]
    public async Task RequeueUnknownDispatchAsyncRejectsStaleUnknownEpochAfterInterveningDispatch()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        _ = await repository.ClaimAsync(Claim(TenantId, OrderId, "composition-stale-unknown"), CancellationToken.None);
        CheckoutDispatchClaim claimA = (await repository.ClaimDueDispatchEffectsAsync("worker-a", 1, UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), CancellationToken.None)).Single();
        DateTime unknownA = UtcNow.AddSeconds(2);
        await repository.MarkDispatchUnknownAsync(claimA, unknownA, CancellationToken.None);
        await repository.RequeueUnknownDispatchAsync(TenantId, claimA.EffectId, unknownA, claimA.ProcessingFence, 1, UtcNow.AddSeconds(3), UtcNow.AddSeconds(3), CancellationToken.None);
        CheckoutDispatchClaim claimB = (await repository.ClaimDueDispatchEffectsAsync("worker-b", 1, UtcNow.AddSeconds(4), TimeSpan.FromMinutes(1), CancellationToken.None)).Single();
        DateTime unknownB = UtcNow.AddSeconds(5);
        await repository.MarkDispatchUnknownAsync(claimB, unknownB, CancellationToken.None);

        bool staleARequeue = await repository.RequeueUnknownDispatchAsync(TenantId, claimA.EffectId, unknownA, claimA.ProcessingFence, 1, UtcNow.AddSeconds(6), UtcNow.AddSeconds(6), CancellationToken.None);
        CheckoutDispatchEffect parkedB = await context.CheckoutDispatchEffects.AsNoTracking().SingleAsync(value => value.Id == claimA.EffectId);
        bool currentBRequeue = await repository.RequeueUnknownDispatchAsync(TenantId, claimB.EffectId, unknownB, claimB.ProcessingFence, 2, UtcNow.AddSeconds(7), UtcNow.AddSeconds(7), CancellationToken.None);

        await Assert.That(staleARequeue).IsFalse();
        await Assert.That(parkedB.Status).IsEqualTo(OutboxMessageStatus.Unknown);
        await Assert.That(parkedB.UnknownAt).IsEqualTo(unknownB);
        await Assert.That(currentBRequeue).IsTrue();
    }

    [Test]
    public async Task PreHandoffFailureRetriesSameActiveAttemptAndMarksNextClaimAsSafeReplay()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        RegistrationPaymentAttemptClaimOutcome outcome = await repository.ClaimAsync(
            Claim(TenantId, OrderId, "composition-pre-handoff"), CancellationToken.None);
        CheckoutDispatchClaim claim = (await repository.ClaimDueDispatchEffectsAsync(
            "worker-a", 1, UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), CancellationToken.None)).Single();
        await repository.MarkCheckoutDispatchPendingAsync(claim, UtcNow.AddSeconds(2), CancellationToken.None);

        bool retried = await repository.RetryCheckoutDispatchBeforeHandoffAsync(
            claim,
            "checkout_provider_secret_unavailable",
            UtcNow.AddSeconds(30),
            UtcNow.AddSeconds(3),
            CancellationToken.None);
        IReadOnlyList<CheckoutDispatchClaim> premature = await repository.ClaimDueDispatchEffectsAsync(
            "worker-b", 1, UtcNow.AddSeconds(29), TimeSpan.FromMinutes(1), CancellationToken.None);
        CheckoutDispatchClaim replay = (await repository.ClaimDueDispatchEffectsAsync(
            "worker-c", 1, UtcNow.AddSeconds(30), TimeSpan.FromMinutes(1), CancellationToken.None)).Single();

        PaymentAttempt attempt = await context.PaymentAttempts.AsNoTracking().SingleAsync(value => value.Id == outcome.Attempt.Id);
        await Assert.That(retried).IsTrue();
        await Assert.That(premature).IsEmpty();
        await Assert.That(replay.ReplayKind).IsEqualTo(CheckoutDispatchReplayKind.PreHandoffRetry);
        await Assert.That(attempt.PaymentAttemptStatusId).IsEqualTo((int)PaymentAttemptStatusEnum.DispatchPending);
        await Assert.That(attempt.ActiveUniquenessSlot).IsEqualTo(PaymentAttempt.ActiveUniquenessSlotValue);
    }

    [Test]
    public async Task CompleteCheckoutDispatchAsyncBindsSessionAndCompletesOnlyCurrentFence()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        RegistrationPaymentAttemptClaimOutcome outcome = await repository.ClaimAsync(Claim(TenantId, OrderId, "composition-complete"), CancellationToken.None);
        DateTime claimedAt = UtcNow.AddSeconds(1);
        CheckoutDispatchClaim claim = (await repository.ClaimDueDispatchEffectsAsync("worker-a", 1, claimedAt, TimeSpan.FromMinutes(1), CancellationToken.None)).Single();

        bool dispatchPending = await repository.MarkCheckoutDispatchPendingAsync(claim, UtcNow.AddSeconds(2), CancellationToken.None);
        PaymentAttempt? loaded = await repository.GetClaimedAttemptAsync(claim, UtcNow.AddSeconds(2), CancellationToken.None);
        bool completed = await repository.CompleteCheckoutDispatchAsync(claim, "cs_123", "req_create", UtcNow.AddSeconds(3), CancellationToken.None);
        bool stale = await repository.CompleteCheckoutDispatchAsync(claim, "cs_other", "req_stale", UtcNow.AddSeconds(4), CancellationToken.None);

        PaymentAttempt attempt = await context.PaymentAttempts.AsNoTracking().SingleAsync(value => value.Id == outcome.Attempt.Id);
        CheckoutDispatchEffect effect = await context.CheckoutDispatchEffects.AsNoTracking().SingleAsync(value => value.Id == claim.EffectId);
        await Assert.That(loaded!.Id).IsEqualTo(outcome.Attempt.Id);
        await Assert.That(dispatchPending).IsTrue();
        await Assert.That(loaded.PaymentAttemptStatusId).IsEqualTo((int)PaymentAttemptStatusEnum.DispatchPending);
        await Assert.That(completed).IsTrue();
        await Assert.That(stale).IsFalse();
        await Assert.That(attempt.ProviderCheckoutSessionId).IsEqualTo("cs_123");
        await Assert.That(attempt.PaymentAttemptStatusId).IsEqualTo((int)PaymentAttemptStatusEnum.RequiresAction);
        await Assert.That(attempt.LastProviderRequestId).IsEqualTo("req_create");
        await Assert.That(effect.Status).IsEqualTo(OutboxMessageStatus.Completed);
    }

    [Test]
    public async Task MarkCheckoutDispatchUnknownAsyncParksAttemptAndEffectInSameEpoch()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        RegistrationPaymentAttemptClaimOutcome outcome = await repository.ClaimAsync(Claim(TenantId, OrderId, "composition-provider-unknown"), CancellationToken.None);
        CheckoutDispatchClaim claim = (await repository.ClaimDueDispatchEffectsAsync("worker-a", 1, UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), CancellationToken.None)).Single();

        await repository.MarkCheckoutDispatchPendingAsync(claim, UtcNow.AddSeconds(2), CancellationToken.None);
        bool unknown = await repository.MarkCheckoutDispatchUnknownAsync(claim, "req_unknown", UtcNow.AddSeconds(3), CancellationToken.None);
        bool stale = await repository.MarkCheckoutDispatchUnknownAsync(claim, "req_stale", UtcNow.AddSeconds(4), CancellationToken.None);

        PaymentAttempt attempt = await context.PaymentAttempts.AsNoTracking().SingleAsync(value => value.Id == outcome.Attempt.Id);
        CheckoutDispatchEffect effect = await context.CheckoutDispatchEffects.AsNoTracking().SingleAsync(value => value.Id == claim.EffectId);
        await Assert.That(unknown).IsTrue();
        await Assert.That(stale).IsFalse();
        await Assert.That(attempt.PaymentAttemptStatusId).IsEqualTo((int)PaymentAttemptStatusEnum.Unknown);
        await Assert.That(attempt.LastProviderRequestId).IsEqualTo("req_unknown");
        await Assert.That(effect.Status).IsEqualTo(OutboxMessageStatus.Unknown);
        await Assert.That(effect.UnknownAt).IsEqualTo(UtcNow.AddSeconds(3));
    }

    [Test]
    public async Task FailCheckoutDispatchAsyncParksDeterministicRejectionAndReleasesActiveSlot()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        RegistrationPaymentAttemptClaimOutcome outcome = await repository.ClaimAsync(Claim(TenantId, OrderId, "composition-rejected"), CancellationToken.None);
        CheckoutDispatchClaim claim = (await repository.ClaimDueDispatchEffectsAsync("worker-a", 1, UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), CancellationToken.None)).Single();

        await repository.MarkCheckoutDispatchPendingAsync(claim, UtcNow.AddSeconds(2), CancellationToken.None);
        bool failed = await repository.FailCheckoutDispatchAsync(
            claim,
            "checkout_provider_rejected",
            "req_bad",
            UtcNow.AddSeconds(3),
            CancellationToken.None);

        PaymentAttempt attempt = await context.PaymentAttempts.AsNoTracking().SingleAsync(value => value.Id == outcome.Attempt.Id);
        CheckoutDispatchEffect effect = await context.CheckoutDispatchEffects.AsNoTracking().SingleAsync(value => value.Id == claim.EffectId);
        await Assert.That(failed).IsTrue();
        await Assert.That(attempt.PaymentAttemptStatusId).IsEqualTo((int)PaymentAttemptStatusEnum.Failed);
        await Assert.That(attempt.ActiveUniquenessSlot).IsNotEqualTo(PaymentAttempt.ActiveUniquenessSlotValue);
        await Assert.That(effect.Status).IsEqualTo(OutboxMessageStatus.DeadLettered);
        await Assert.That(effect.LastFailureCode).IsEqualTo("checkout_provider_rejected");
    }

    [Test]
    public async Task ClaimServiceUsesRealSerializableUnitOfWorkAndPersistsAuthoritativeOrderFacts()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        var order = CreatePayableOrder(TenantId, OrderId, organizerDirectedMinor: 1_000, platformFeeMinor: 75, platformContributionMinor: 125);
        Guid organizerActorId = Guid.CreateVersion7();
        var inventory = Substitute.For<IRegistrationInventoryRepository>();
        inventory.GetOrderForUpdateWithLinesAsync(OrderId, TenantId, Arg.Any<CancellationToken>()).Returns(order);
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetEventWithDetails(order.EventId).Returns(new Explore.Domain.Event
        {
            Id = order.EventId,
            TenantId = TenantId,
            OrganizerActorId = organizerActorId,
            Title = "Paid event",
            Actor = null!,
            Tenant = null!,
            EventFormat = null!,
            VisibilityType = null!,
            EventStatus = null!
        });
        var connection = OrganizerPaymentProviderConnection.Create(Guid.CreateVersion7(), TenantId, organizerActorId,
            "stripe", "platform-live-eu", "acct_authority", UtcNow);
        connection.ApplyReadiness(OrganizerPaymentProviderReadinessObservation.Create("BE", ChargeCapabilityState.Active, ProviderRequirementsState.Satisfied, ["EUR"], UtcNow, "ready-1"));
        var connections = Substitute.For<IOrganizerPaymentProviderConnectionRepository>();
        connections.GetActiveByScopeAsync(TenantId, connection.OrganizerActorId, "stripe", "platform-live-eu", Arg.Any<CancellationToken>()).Returns(connection);
        var policies = Substitute.For<IPaidEventPolicyRepository>();
        policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(PaidEventPolicyVersion.CreateDefaultInstance());
        var commerce = Substitute.For<IOrganizerPaymentCommerceConfiguration>();
        commerce.ProviderCode.Returns("stripe");
        commerce.ConnectPlatformId.Returns("platform-live-eu");
        var descriptor = Substitute.For<IPaymentProviderDescriptor>();
        descriptor.Describe().Returns(new PaymentProviderDescriptor("stripe", "OrganizerDirect", "2026-07-29.dahlia"));
        var service = new RegistrationPaymentAttemptClaimService(repository, inventory, eventRepository, connections, policies, commerce, descriptor, new EfCoreUnitOfWork(context));

        RegistrationPaymentAttemptClaimResult result = await service.ClaimAsync(new(TenantId, OrderId, UtcNow), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Created).IsTrue();
        PaymentAttempt persisted = await context.PaymentAttempts.AsNoTracking().SingleAsync(value => value.Id == result.Attempt!.Id);
        await Assert.That(persisted.OrganizerAmountMinor).IsEqualTo(1_000);
        await Assert.That(persisted.PlatformFeeMinor).IsEqualTo(75);
        await Assert.That(persisted.PlatformContributionMinor).IsEqualTo(125);
        await Assert.That(persisted.TotalMinor).IsEqualTo(1_125);
        await Assert.That(persisted.ProviderApiRevision).IsEqualTo("2026-07-29.dahlia");
        await Assert.That(persisted.RecipientSnapshot.ExternalAccountId).IsEqualTo("acct_authority");
        await Assert.That(await context.CheckoutDispatchEffects.CountAsync(value => value.PaymentAttemptId == persisted.Id)).IsEqualTo(1);
    }

    [Test]
    public async Task ClaimServiceAfterCutoffMutationReusesActiveAttemptWithPinnedCompositionRevision()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        RegistrationOrder order = CreatePayableOrder(TenantId, OrderId, 1_000, 75, 125);
        typeof(RegistrationOrder).GetProperty(nameof(RegistrationOrder.ExpiresAt))!
            .SetValue(order, UtcNow.AddMinutes(1));
        string pinnedRevision = order.ConcurrencyStamp.ToString("N");
        RegistrationPaymentAttemptClaimOutcome existing = await repository.ClaimAsync(
            Claim(TenantId, OrderId, pinnedRevision), CancellationToken.None);
        var inventory = Substitute.For<IRegistrationInventoryRepository>();
        inventory.GetOrderForUpdateWithLinesAsync(OrderId, TenantId, Arg.Any<CancellationToken>()).Returns(order);
        inventory.GetActiveHoldsForUpdateAsync(OrderId, TenantId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<RegistrationInventoryHold>());
        var service = new RegistrationPaymentAttemptClaimService(
            repository,
            inventory,
            Substitute.For<IEventRepository>(),
            Substitute.For<IOrganizerPaymentProviderConnectionRepository>(),
            Substitute.For<IPaidEventPolicyRepository>(),
            Substitute.For<IOrganizerPaymentCommerceConfiguration>(),
            Substitute.For<IPaymentProviderDescriptor>(),
            new EfCoreUnitOfWork(context));

        RegistrationPaymentAttemptClaimResult result = await service.ClaimAsync(
            new(TenantId, OrderId, UtcNow), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Created).IsFalse();
        await Assert.That(result.Attempt!.Id).IsEqualTo(existing.Attempt.Id);
        await Assert.That(result.Attempt.CompositionRevision).IsEqualTo(pinnedRevision);
        await Assert.That(order.ConcurrencyStamp.ToString("N")).IsNotEqualTo(pinnedRevision);
        await Assert.That(await context.PaymentAttempts.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.CheckoutDispatchEffects.CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task ExplicitTerminalRetriesUseNewestCompositionAttemptAndCreateExactNextReplacement()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        RegistrationOrder order = CreatePayableOrder(TenantId, OrderId, 1_000, 75, 125);
        typeof(RegistrationOrder).GetProperty(nameof(RegistrationOrder.ExpiresAt))!
            .SetValue(order, UtcNow.AddHours(1));
        Guid organizerActorId = Guid.CreateVersion7();
        var inventory = Substitute.For<IRegistrationInventoryRepository>();
        inventory.GetOrderForUpdateWithLinesAsync(OrderId, TenantId, Arg.Any<CancellationToken>()).Returns(order);
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetEventWithDetails(order.EventId).Returns(new Explore.Domain.Event
        {
            Id = order.EventId,
            TenantId = TenantId,
            OrganizerActorId = organizerActorId,
            Title = "Paid event",
            Actor = null!,
            Tenant = null!,
            EventFormat = null!,
            VisibilityType = null!,
            EventStatus = null!
        });
        OrganizerPaymentProviderConnection connection = OrganizerPaymentProviderConnection.Create(
            Guid.CreateVersion7(), TenantId, organizerActorId, "stripe", "platform-live-eu", "acct_authority", UtcNow);
        connection.ApplyReadiness(OrganizerPaymentProviderReadinessObservation.Create(
            "BE", ChargeCapabilityState.Active, ProviderRequirementsState.Satisfied, ["EUR"], UtcNow, "ready-1"));
        var connections = Substitute.For<IOrganizerPaymentProviderConnectionRepository>();
        connections.GetActiveByScopeAsync(
            TenantId, organizerActorId, "stripe", "platform-live-eu", Arg.Any<CancellationToken>()).Returns(connection);
        var policies = Substitute.For<IPaidEventPolicyRepository>();
        policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(PaidEventPolicyVersion.CreateDefaultInstance());
        var commerce = Substitute.For<IOrganizerPaymentCommerceConfiguration>();
        commerce.ProviderCode.Returns("stripe");
        commerce.ConnectPlatformId.Returns("platform-live-eu");
        var descriptor = Substitute.For<IPaymentProviderDescriptor>();
        descriptor.Describe().Returns(new PaymentProviderDescriptor("stripe", "OrganizerDirect", "2026-07-29.dahlia"));
        var service = new RegistrationPaymentAttemptClaimService(
            repository,
            inventory,
            eventRepository,
            connections,
            policies,
            commerce,
            descriptor,
            new EfCoreUnitOfWork(context));

        RegistrationPaymentAttemptClaimResult first = await service.ClaimAsync(
            new(TenantId, OrderId, UtcNow), CancellationToken.None);
        first.Attempt!.MarkDispatchFailed(UtcNow.AddSeconds(1), "req-a-failed");
        await repository.ReleaseActiveSlotAsync(first.Attempt, UtcNow.AddSeconds(1), CancellationToken.None);
        RegistrationPaymentAttemptClaimResult second = await service.ClaimAsync(
            new(TenantId, OrderId, UtcNow.AddSeconds(2), first.Attempt.Id), CancellationToken.None);
        second.Attempt!.MarkDispatchFailed(UtcNow.AddSeconds(3), "req-b-failed");
        await repository.ReleaseActiveSlotAsync(second.Attempt, UtcNow.AddSeconds(3), CancellationToken.None);

        (PaymentAttempt Attempt, CheckoutDispatchEffect DispatchEffect)? newest =
            await repository.GetByOrderCompositionAsync(
                TenantId, OrderId, second.Attempt.CompositionRevision, CancellationToken.None);
        await Assert.That(newest!.Value.Attempt.Id).IsEqualTo(second.Attempt.Id);

        RegistrationPaymentAttemptClaimResult third = await service.ClaimAsync(
            new(TenantId, OrderId, UtcNow.AddSeconds(4), second.Attempt.Id), CancellationToken.None);
        RegistrationPaymentAttemptClaimResult repeated = await service.ClaimAsync(
            new(TenantId, OrderId, UtcNow.AddSeconds(5), second.Attempt.Id), CancellationToken.None);

        await Assert.That(first.Created).IsTrue();
        await Assert.That(second.Created).IsTrue();
        await Assert.That(third.Created).IsTrue();
        await Assert.That(third.Attempt!.Id).IsNotEqualTo(first.Attempt.Id);
        await Assert.That(third.Attempt.Id).IsNotEqualTo(second.Attempt.Id);
        await Assert.That(repeated.Created).IsFalse();
        await Assert.That(repeated.Attempt!.Id).IsEqualTo(third.Attempt.Id);
        PaymentAttempt[] history = await context.PaymentAttempts
            .Where(value => value.TenantId == TenantId && value.RegistrationOrderId == OrderId)
            .OrderBy(value => value.CreatedAt)
            .ThenBy(value => value.Id)
            .ToArrayAsync();
        await Assert.That(history.Length).IsEqualTo(3);
        await Assert.That(history.Select(value => value.ProviderIdempotencyKey).Distinct().Count()).IsEqualTo(3);
        await Assert.That(history.Count(value => value.ActiveUniquenessSlot == PaymentAttempt.ActiveUniquenessSlotValue)).IsEqualTo(1);
        await Assert.That(await context.CheckoutDispatchEffects.CountAsync()).IsEqualTo(3);
    }

    [Test]
    public async Task ClaimAsyncReturnsHistoricalSameCompositionAfterTerminalReleaseInsteadOfUniqueIndexCrash()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        RegistrationPaymentAttemptClaim initial = Claim(TenantId, OrderId, "composition-historical");
        RegistrationPaymentAttemptClaimOutcome created = await repository.ClaimAsync(initial, CancellationToken.None);
        created.Attempt.MarkDispatchFailed(UtcNow.AddSeconds(1), "req_bad");
        await repository.ReleaseActiveSlotAsync(created.Attempt, UtcNow.AddSeconds(1), CancellationToken.None);

        RegistrationPaymentAttemptClaimOutcome repeated = await repository.ClaimAsync(
            Claim(TenantId, OrderId, "composition-historical"), CancellationToken.None);

        await Assert.That(repeated.Created).IsFalse();
        await Assert.That(repeated.Attempt.Id).IsEqualTo(created.Attempt.Id);
        await Assert.That(await context.PaymentAttempts.CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task CheckoutCompletionCreatesOneDurableReconciliationEffectAndRecoversExpiredLease()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        RegistrationPaymentAttemptClaimOutcome outcome = await repository.ClaimAsync(Claim(TenantId, OrderId, "composition-reconcile"), CancellationToken.None);
        CheckoutDispatchClaim dispatch = (await repository.ClaimDueDispatchEffectsAsync(
            "checkout-worker", 1, UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), CancellationToken.None)).Single();
        _ = await repository.MarkCheckoutDispatchPendingAsync(dispatch, UtcNow.AddSeconds(1), CancellationToken.None);

        bool completed = await repository.CompleteCheckoutDispatchAsync(
            dispatch, "cs_reconcile", "req_create", UtcNow.AddSeconds(2), CancellationToken.None);
        PaymentReconciliationClaim first = (await repository.ClaimDueReconciliationsAsync(
            "reconcile-a", 1, UtcNow.AddSeconds(3), TimeSpan.FromMinutes(1), CancellationToken.None)).Single();
        IReadOnlyList<PaymentReconciliationClaim> recovered = await repository.ClaimDueReconciliationsAsync(
            "reconcile-b", 1, UtcNow.AddMinutes(1).AddSeconds(4), TimeSpan.FromMinutes(1), CancellationToken.None);

        await Assert.That(completed).IsTrue();
        PaymentAttempt persistedAttempt = await context.PaymentAttempts.AsNoTracking().SingleAsync(value => value.Id == outcome.Attempt.Id);
        await Assert.That(persistedAttempt.ProviderCheckoutSessionId).IsEqualTo("cs_reconcile");
        await Assert.That(await context.PaymentReconciliationEffects.CountAsync()).IsEqualTo(1);
        await Assert.That(recovered).HasSingleItem();
        await Assert.That(recovered.Single().ProcessingFence).IsEqualTo(first.ProcessingFence + 1);
    }

    [Test]
    public async Task SucceededSettlementAtomicallyCreatesOneIdentifiersOnlyObservationAndRejectsStaleFence()
    {
        await using var context = await CreateContextAsync();
        await context.RegistrationOrders.AddAsync(CreatePayableOrder(TenantId, OrderId, 1_000, 75, 125));
        await context.SaveChangesAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        _ = await repository.ClaimAsync(Claim(TenantId, OrderId, "composition-success"), CancellationToken.None);
        CheckoutDispatchClaim dispatch = (await repository.ClaimDueDispatchEffectsAsync(
            "checkout-worker", 1, UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), CancellationToken.None)).Single();
        _ = await repository.MarkCheckoutDispatchPendingAsync(dispatch, UtcNow.AddSeconds(1), CancellationToken.None);
        _ = await repository.CompleteCheckoutDispatchAsync(dispatch, "cs_success", "req_create", UtcNow.AddSeconds(2), CancellationToken.None);
        PaymentReconciliationClaim claim = (await repository.ClaimDueReconciliationsAsync(
            "reconcile", 1, UtcNow.AddSeconds(3), TimeSpan.FromMinutes(1), CancellationToken.None)).Single();
        var decision = new PaymentReconciliationDecision(
            PaymentReconciliationDisposition.Complete,
            PaymentAttemptStatusEnum.Succeeded,
            "cs_success",
            "pi_success",
            "req_retrieve",
            string.Empty,
            UtcNow.AddSeconds(4));

        bool settled = await repository.SettleReconciliationAsync(claim, decision, CancellationToken.None);
        bool duplicate = await repository.SettleReconciliationAsync(claim, decision with { ObservedAt = UtcNow.AddSeconds(5) }, CancellationToken.None);

        await Assert.That(settled).IsTrue();
        await Assert.That(duplicate).IsFalse();
        PaymentSucceededObservation observation = await context.PaymentSucceededObservations.AsNoTracking().SingleAsync();
        await Assert.That(observation.PaymentAttemptId).IsEqualTo(claim.PaymentAttemptId);
        await Assert.That(observation.ProviderCheckoutSessionId).IsEqualTo("cs_success");
        await Assert.That(observation.ProviderPaymentId).IsEqualTo("pi_success");
        await Assert.That(observation.ProviderRequestId).IsEqualTo("req_retrieve");
        RegistrationFinalizationEffect finalizationEffect = await context.RegistrationFinalizationEffects.AsNoTracking().SingleAsync();
        await Assert.That(finalizationEffect.RegistrationOrderId).IsEqualTo(OrderId);
        await Assert.That(finalizationEffect.Status).IsEqualTo(OutboxMessageStatus.Pending);
        var finalization = new RegistrationFinalizationRepository(context);
        SucceededPaymentLookupResult evidence =
            await finalization.GetSucceededPaymentAsync(TenantId, OrderId, CancellationToken.None);
        await Assert.That(evidence.Status).IsEqualTo(SucceededPaymentLookupStatus.Found);
        await Assert.That((await finalization.GetSucceededPaymentAsync(
            Guid.CreateVersion7(), OrderId, CancellationToken.None)).Status).IsEqualTo(SucceededPaymentLookupStatus.Missing);
    }

    [Test]
    public async Task LateSecondSucceededObservationRequeuesCompletedFinalizationEffect()
    {
        await using var context = await CreateContextAsync();
        RegistrationOrder order = CreatePayableOrder(TenantId, OrderId, 1_000, 75, 125);
        await context.RegistrationOrders.AddAsync(order);
        await context.SaveChangesAsync();
        var payments = new RegistrationPaymentAttemptRepository(context);
        RegistrationPaymentAttemptClaimOutcome first = await payments.ClaimAsync(
            Claim(TenantId, OrderId, "composition-first"), CancellationToken.None);
        first.Attempt.MarkRequiresAction("cs_first", UtcNow.AddSeconds(1), null);
        await payments.EnsureReconciliationDueAsync(first.Attempt, null, UtcNow.AddSeconds(2), CancellationToken.None);
        PaymentReconciliationClaim firstClaim = (await payments.ClaimDueReconciliationsAsync(
            "first-success", 1, UtcNow.AddSeconds(2), TimeSpan.FromMinutes(1), CancellationToken.None)).Single();
        await Assert.That(await payments.SettleReconciliationAsync(
            firstClaim,
            new PaymentReconciliationDecision(
                PaymentReconciliationDisposition.Complete,
                PaymentAttemptStatusEnum.Succeeded,
                "cs_first",
                "pi_first",
                null,
                string.Empty,
                UtcNow.AddSeconds(3)),
            CancellationToken.None)).IsTrue();

        var finalization = new RegistrationFinalizationRepository(context);
        RegistrationFinalizationClaim completedClaim = (await finalization.ClaimDueAsync(
            "finalize-first", 1, UtcNow.AddSeconds(4), TimeSpan.FromMinutes(1), CancellationToken.None)).Single();
        await Assert.That(await finalization.CompleteAsync(
            completedClaim, UtcNow.AddSeconds(5), CancellationToken.None)).IsTrue();
        context.ChangeTracker.Clear();

        PaymentAttempt second = Claim(TenantId, OrderId, "composition-legacy-second").Attempt;
        typeof(PaymentAttempt).GetProperty(nameof(PaymentAttempt.ActiveScopeKey))!
            .SetValue(second, $"{TenantId:N}|{OrderId:N}|composition-legacy-second");
        second.MarkRequiresAction("cs_second", UtcNow.AddSeconds(6), null);
        await context.PaymentAttempts.AddAsync(second);
        await context.SaveChangesAsync();
        await payments.EnsureReconciliationDueAsync(second, null, UtcNow.AddSeconds(7), CancellationToken.None);
        PaymentReconciliationClaim secondClaim = (await payments.ClaimDueReconciliationsAsync(
            "second-success", 1, UtcNow.AddSeconds(7), TimeSpan.FromMinutes(1), CancellationToken.None)).Single();

        await Assert.That(await payments.SettleReconciliationAsync(
            secondClaim,
            new PaymentReconciliationDecision(
                PaymentReconciliationDisposition.Complete,
                PaymentAttemptStatusEnum.Succeeded,
                "cs_second",
                "pi_second",
                null,
                string.Empty,
                UtcNow.AddSeconds(8)),
            CancellationToken.None)).IsTrue();

        RegistrationFinalizationEffect effect = await context.RegistrationFinalizationEffects.AsNoTracking().SingleAsync();
        await Assert.That(effect.Status).IsEqualTo(OutboxMessageStatus.Pending);
        await Assert.That(effect.CompletedAt).IsNull();
        await Assert.That(await context.PaymentSucceededObservations.CountAsync()).IsEqualTo(2);
        await Assert.That((await finalization.GetSucceededPaymentAsync(
            TenantId, OrderId, CancellationToken.None)).Status).IsEqualTo(SucceededPaymentLookupStatus.Conflict);
    }

    [Test]
    public async Task SucceededPaymentLookupReturnsBoundedConflictForTwoAttemptsWithoutDiscardingEvidence()
    {
        await using var context = await CreateContextAsync();
        RegistrationOrder order = CreatePayableOrder(TenantId, OrderId, 1_000, 75, 125);
        PaymentAttempt first = PaymentAttempt.Create(
            Guid.CreateVersion7(), TenantId, OrderId, RecipientSnapshot(TenantId), "OrganizerDirect",
            "2026-08-20.acacia", "composition-conflict-a", 1_000, 75, 125,
            "checkout:conflict:a", UtcNow, UtcNow.AddMinutes(30));
        PaymentAttempt second = PaymentAttempt.Create(
            Guid.CreateVersion7(), TenantId, OrderId, RecipientSnapshot(TenantId), "OrganizerDirect",
            "2026-08-20.acacia", "composition-conflict-b", 1_000, 75, 125,
            "checkout:conflict:b", UtcNow, UtcNow.AddMinutes(30));
        typeof(PaymentAttempt).GetProperty(nameof(PaymentAttempt.ActiveScopeKey))!
            .SetValue(first, $"{TenantId:N}|{OrderId:N}|composition-conflict-a");
        typeof(PaymentAttempt).GetProperty(nameof(PaymentAttempt.ActiveScopeKey))!
            .SetValue(second, $"{TenantId:N}|{OrderId:N}|composition-conflict-b");
        first.MarkSucceededFromCheckout("cs_conflict_a", "pi_conflict_a", UtcNow.AddSeconds(1), null);
        second.MarkSucceededFromCheckout("cs_conflict_b", "pi_conflict_b", UtcNow.AddSeconds(1), null);
        context.AddRange(
            order,
            first,
            second,
            PaymentSucceededObservation.Create(first, null, "cs_conflict_a", "pi_conflict_a", null, UtcNow.AddSeconds(1)),
            PaymentSucceededObservation.Create(second, null, "cs_conflict_b", "pi_conflict_b", null, UtcNow.AddSeconds(1)));
        await context.SaveChangesAsync();
        var repository = new RegistrationFinalizationRepository(context);

        SucceededPaymentLookupResult result = await repository.GetSucceededPaymentAsync(
            TenantId, OrderId, CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo(SucceededPaymentLookupStatus.Conflict);
        await Assert.That(result.Code).IsEqualTo("payment_duplicate_succeeded_observations");
        await Assert.That(await context.PaymentSucceededObservations.CountAsync()).IsEqualTo(2);
        PaymentReconciliationHealth health = await new RegistrationPaymentAttemptRepository(context)
            .GetReconciliationHealthAsync(UtcNow.AddMinutes(1), CancellationToken.None);
        await Assert.That(health.DuplicateSucceededOrders).IsEqualTo(1);
    }

    [Test]
    public async Task StaleSucceededEvidenceAfterNewerProcessingDoesNotCreatePaidObservation()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        RegistrationPaymentAttemptClaimOutcome outcome = await repository.ClaimAsync(
            Claim(TenantId, OrderId, "composition-stale-success"), CancellationToken.None);
        outcome.Attempt.MarkRequiresAction("cs_stale", UtcNow.AddSeconds(1), "req_create");
        await repository.SaveChangesAsync(CancellationToken.None);
        await repository.EnsureReconciliationDueAsync(outcome.Attempt, null, UtcNow.AddSeconds(2), CancellationToken.None);
        PaymentReconciliationClaim claim = (await repository.ClaimDueReconciliationsAsync(
            "reconcile", 1, UtcNow.AddSeconds(2), TimeSpan.FromMinutes(5), CancellationToken.None)).Single();
        PaymentAttempt current = await context.PaymentAttempts.SingleAsync(value => value.Id == outcome.Attempt.Id);
        current.MarkProcessing("cs_stale", "pi_stale", UtcNow.AddSeconds(4), "req_processing");
        await context.SaveChangesAsync();
        var staleSuccess = new PaymentReconciliationDecision(
            PaymentReconciliationDisposition.Complete,
            PaymentAttemptStatusEnum.Succeeded,
            "cs_stale",
            "pi_stale",
            "req_stale_success",
            string.Empty,
            UtcNow.AddSeconds(3));

        bool settled = await repository.SettleReconciliationAsync(claim, staleSuccess, CancellationToken.None);

        await Assert.That(settled).IsTrue();
        await Assert.That(await context.PaymentSucceededObservations.CountAsync()).IsEqualTo(0);
        PaymentAttempt persisted = await context.PaymentAttempts.AsNoTracking().SingleAsync(value => value.Id == outcome.Attempt.Id);
        await Assert.That(persisted.PaymentAttemptStatusId).IsEqualTo((int)PaymentAttemptStatusEnum.Processing);
        PaymentReconciliationEffect effect = await context.PaymentReconciliationEffects.AsNoTracking().SingleAsync();
        await Assert.That(effect.Status).IsEqualTo(OutboxMessageStatus.Failed);
        await Assert.That(effect.NextAttemptAt).IsNotNull();
    }

    [Test]
    public async Task AmbiguousCreateWithoutSessionCreatesDurableReconciliationWork()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        _ = await repository.ClaimAsync(Claim(TenantId, OrderId, "composition-ambiguous"), CancellationToken.None);
        CheckoutDispatchClaim dispatch = (await repository.ClaimDueDispatchEffectsAsync(
            "checkout-worker", 1, UtcNow.AddSeconds(1), TimeSpan.FromMinutes(2), CancellationToken.None)).Single();
        _ = await repository.MarkCheckoutDispatchPendingAsync(dispatch, UtcNow.AddSeconds(1), CancellationToken.None);

        bool unknown = await repository.MarkCheckoutDispatchUnknownAsync(
            dispatch, "req_ambiguous", UtcNow.AddSeconds(2), CancellationToken.None);

        await Assert.That(unknown).IsTrue();
        await Assert.That(await context.PaymentReconciliationEffects.CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task ScheduledDrainsRecoverAmbiguousCreateThroughSameKeyReplayAndAuthoritativeRetrieval()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        _ = await repository.ClaimAsync(Claim(TenantId, OrderId, "composition-drain-recovery"), CancellationToken.None);
        CheckoutDispatchClaim firstDispatch = (await repository.ClaimDueDispatchEffectsAsync(
            "checkout-first", 1, UtcNow.AddSeconds(1), TimeSpan.FromMinutes(2), CancellationToken.None)).Single();
        _ = await repository.MarkCheckoutDispatchPendingAsync(firstDispatch, UtcNow.AddSeconds(1), CancellationToken.None);
        _ = await repository.MarkCheckoutDispatchUnknownAsync(firstDispatch, "req_timeout", UtcNow.AddSeconds(2), CancellationToken.None);
        var checkout = Substitute.For<IHostedCheckoutSessionRetriever>();
        var payment = Substitute.For<IPaymentIntentRetriever>();
        var time = new MutableTimeProvider(UtcNow.AddSeconds(3));
        var reconciliation = new RegistrationPaymentReconciliationService(repository, checkout, payment, time);
        PaymentReconciliationEffect recoveryEffect = await context.PaymentReconciliationEffects.AsNoTracking().SingleAsync();
        await Assert.That(recoveryEffect.CheckoutDispatchEffectId).IsEqualTo(firstDispatch.EffectId);
        await Assert.That(recoveryEffect.CheckoutDispatchUnknownAt).IsEqualTo(UtcNow.AddSeconds(2));
        await Assert.That(recoveryEffect.CheckoutDispatchProcessingFence).IsEqualTo(firstDispatch.ProcessingFence);
        await Assert.That(recoveryEffect.CheckoutDispatchAttemptCount).IsEqualTo(firstDispatch.AttemptCount);
        CheckoutDispatchEffect unknownDispatch = await context.CheckoutDispatchEffects.AsNoTracking().SingleAsync();
        await Assert.That(unknownDispatch.UnknownAt).IsEqualTo(recoveryEffect.CheckoutDispatchUnknownAt);
        await Assert.That(unknownDispatch.ProcessingFence).IsEqualTo(recoveryEffect.CheckoutDispatchProcessingFence);
        await Assert.That(unknownDispatch.AttemptCount).IsEqualTo(recoveryEffect.CheckoutDispatchAttemptCount);
        PaymentReconciliationClaim initialRecoveryClaim = (await repository.ClaimDueReconciliationsAsync(
            "reconcile-inspect", 1, UtcNow.AddSeconds(3), TimeSpan.FromMinutes(2), CancellationToken.None)).Single();
        await Assert.That(initialRecoveryClaim.CheckoutDispatchEffectId).IsEqualTo(firstDispatch.EffectId);
        await Assert.That(initialRecoveryClaim.CheckoutDispatchUnknownAt).IsEqualTo(UtcNow.AddSeconds(2));
        await Assert.That(initialRecoveryClaim.CheckoutDispatchProcessingFence).IsEqualTo(firstDispatch.ProcessingFence);
        await Assert.That(initialRecoveryClaim.CheckoutDispatchAttemptCount).IsEqualTo(firstDispatch.AttemptCount);
        _ = await repository.SettleReconciliationAsync(
            initialRecoveryClaim,
            new PaymentReconciliationDecision(
                PaymentReconciliationDisposition.Retry,
                PaymentAttemptStatusEnum.Unknown,
                null,
                null,
                "req_timeout",
                "payment_reconciliation_interrupted",
                UtcNow.AddSeconds(3),
                UtcNow.AddSeconds(4)),
            CancellationToken.None);
        PaymentReconciliationEffect retriedRecoveryEffect = await context.PaymentReconciliationEffects.AsNoTracking().SingleAsync();
        await Assert.That(retriedRecoveryEffect.CheckoutDispatchEffectId).IsEqualTo(firstDispatch.EffectId);
        await Assert.That(retriedRecoveryEffect.CheckoutDispatchUnknownAt).IsEqualTo(UtcNow.AddSeconds(2));
        CheckoutDispatchEffect stillUnknownDispatch = await context.CheckoutDispatchEffects.AsNoTracking().SingleAsync();
        await Assert.That(stillUnknownDispatch.Status).IsEqualTo(OutboxMessageStatus.Unknown);
        await Assert.That(stillUnknownDispatch.UnknownAt).IsEqualTo(retriedRecoveryEffect.CheckoutDispatchUnknownAt);
        await Assert.That(stillUnknownDispatch.ProcessingFence).IsEqualTo(retriedRecoveryEffect.CheckoutDispatchProcessingFence);
        await Assert.That(stillUnknownDispatch.AttemptCount).IsEqualTo(retriedRecoveryEffect.CheckoutDispatchAttemptCount);
        time.Set(UtcNow.AddSeconds(4));
        PaymentAttempt beforeRecovery = await context.PaymentAttempts.AsNoTracking().SingleAsync();
        await Assert.That(beforeRecovery.ProviderCheckoutSessionId).IsNull();

        RegistrationPaymentReconciliationResult recovery = await reconciliation.ReconcileDueAsync(
            new RegistrationPaymentReconciliationRequest("payment-reconciliation-drain-job"), CancellationToken.None);

        await Assert.That(recovery.Unknown).IsEqualTo(1);
        await Assert.That(recovery.RequeuedDispatches).IsEqualTo(1);
        await checkout.DidNotReceiveWithAnyArgs().RetrieveAsync(default!, default);
        CheckoutDispatchEffect requeued = await context.CheckoutDispatchEffects.AsNoTracking().SingleAsync();
        await Assert.That(requeued.Status).IsEqualTo(OutboxMessageStatus.Failed);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutCreateResult.Succeeded(new HostedCheckoutSession(
                "cs_recovered", null, HostedCheckoutSessionStatus.Open, HostedCheckoutPaymentStatus.Unpaid,
                null, UtcNow.AddMinutes(30), 1_125, "EUR"), "req_replayed"));
        var dispatch = new RegistrationPaymentCheckoutDispatchService(
            repository,
            creator,
            checkout,
            payment,
            Substitute.For<IRegistrationOrderLifecycleService>(),
            time);
        _ = await dispatch.DispatchDueAsync(new RegistrationPaymentCheckoutDispatchRequest(
            "checkout-replay",
            1,
            TimeSpan.FromMinutes(2),
            new Uri("https://events.example.test"),
            new Uri("https://events.example.test/payment/success"),
            new Uri("https://events.example.test/payment/cancel")), CancellationToken.None);
        await creator.Received(1).CreateAsync(
            Arg.Is<HostedCheckoutCreateRequest>(request =>
                request != null && request.ProviderIdempotencyKey.Contains("composition-drain-recovery", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
        checkout.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutRetrieveResult.Succeeded(new HostedCheckoutSession(
                "cs_recovered", null, HostedCheckoutSessionStatus.Complete, HostedCheckoutPaymentStatus.Paid,
                "pi_recovered", UtcNow.AddMinutes(30), 1_125, "EUR"), "req_session"));
        payment.RetrievePaymentIntentAsync(Arg.Any<PaymentIntentRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaymentIntentRetrieveResult.Succeeded(new PaymentIntentObservation(
                "pi_recovered", 1_125, "EUR", 200, PaymentIntentStatus.Succeeded), "req_payment"));
        time.Set(UtcNow.AddSeconds(5));

        RegistrationPaymentReconciliationResult settled = await reconciliation.ReconcileDueAsync(
            new RegistrationPaymentReconciliationRequest("payment-reconciliation-drain-job"), CancellationToken.None);

        await Assert.That(settled.Succeeded).IsEqualTo(1);
        await Assert.That(await context.PaymentSucceededObservations.CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task SucceededSettlementFailureRollsBackAttemptObservationAndEffectTogether()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        RegistrationPaymentAttemptClaimOutcome outcome = await repository.ClaimAsync(
            Claim(TenantId, OrderId, "composition-rollback"), CancellationToken.None);
        outcome.Attempt.MarkRequiresAction("cs_rollback", UtcNow.AddSeconds(1), "req_create");
        await repository.SaveChangesAsync(CancellationToken.None);
        await repository.EnsureReconciliationDueAsync(outcome.Attempt, null, UtcNow.AddSeconds(2), CancellationToken.None);
        PaymentReconciliationClaim claim = (await repository.ClaimDueReconciliationsAsync(
            "reconcile", 1, UtcNow.AddSeconds(2), TimeSpan.FromMinutes(2), CancellationToken.None)).Single();
        var invalid = new PaymentReconciliationDecision(
            PaymentReconciliationDisposition.Complete,
            PaymentAttemptStatusEnum.Succeeded,
            "cs_rollback",
            "pi_rollback",
            new string('r', 121),
            string.Empty,
            UtcNow.AddSeconds(3));

        await Assert.That(() => repository.SettleReconciliationAsync(claim, invalid, CancellationToken.None))
            .Throws<ArgumentException>();

        PaymentAttempt persisted = await context.PaymentAttempts.AsNoTracking().SingleAsync(value => value.Id == outcome.Attempt.Id);
        await Assert.That(persisted.PaymentAttemptStatusId).IsEqualTo((int)PaymentAttemptStatusEnum.RequiresAction);
        await Assert.That(await context.PaymentSucceededObservations.CountAsync()).IsEqualTo(0);
        PaymentReconciliationEffect effect = await context.PaymentReconciliationEffects.AsNoTracking().SingleAsync();
        await Assert.That(effect.Status).IsEqualTo(OutboxMessageStatus.Processing);
    }

    [Test]
    public async Task ReconciliationLeaseExpiringAfterReadRejectsSettlementWithoutPartialMutation()
    {
        await using var context = await CreateContextAsync();
        var repository = new RegistrationPaymentAttemptRepository(context);
        RegistrationPaymentAttemptClaimOutcome outcome = await repository.ClaimAsync(
            Claim(TenantId, OrderId, "composition-expired-reconcile"), CancellationToken.None);
        outcome.Attempt.MarkRequiresAction("cs_expired_lease", UtcNow.AddSeconds(1), "req_create");
        await repository.SaveChangesAsync(CancellationToken.None);
        await repository.EnsureReconciliationDueAsync(outcome.Attempt, null, UtcNow.AddSeconds(2), CancellationToken.None);
        PaymentReconciliationClaim claim = (await repository.ClaimDueReconciliationsAsync(
            "reconcile", 1, UtcNow.AddSeconds(2), TimeSpan.FromSeconds(1), CancellationToken.None)).Single();
        PaymentAttempt? read = await repository.GetReconciliationAttemptAsync(claim, UtcNow.AddMilliseconds(2500), CancellationToken.None);

        bool settled = await repository.SettleReconciliationAsync(
            claim,
            new PaymentReconciliationDecision(
                PaymentReconciliationDisposition.Complete,
                PaymentAttemptStatusEnum.Succeeded,
                "cs_expired_lease",
                "pi_expired_lease",
                "req_late",
                string.Empty,
                UtcNow.AddSeconds(4)),
            CancellationToken.None);

        await Assert.That(read).IsNotNull();
        await Assert.That(settled).IsFalse();
        await Assert.That(await context.PaymentSucceededObservations.CountAsync()).IsEqualTo(0);
    }

    private static RegistrationPaymentAttemptClaim Claim(Guid tenantId, Guid orderId, string compositionRevision)
    {
        PaymentAttempt attempt = PaymentAttempt.Create(
            Guid.CreateVersion7(), tenantId, orderId, RecipientSnapshot(tenantId), "OrganizerDirect", "2026-08-20.acacia", compositionRevision, 1_000, 75, 125,
            $"checkout:{tenantId:N}:{orderId:N}:{compositionRevision}", UtcNow, UtcNow.AddMinutes(30));
        return new(attempt, CheckoutDispatchEffect.Create(attempt, UtcNow));
    }

    private static OrganizerPaymentRecipientSnapshot RecipientSnapshot(Guid tenantId) => OrganizerPaymentRecipientSnapshot.Create(
        tenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "stripe", "platform-live-eu", "acct_123", "BE", "EUR", Guid.CreateVersion7(), null, UtcNow);

    private static RegistrationOrder CreatePayableOrder(Guid tenantId, Guid orderId, long organizerDirectedMinor, long platformFeeMinor, long platformContributionMinor)
    {
        RegistrationOrder order = RegistrationOrder.Create(
            orderId,
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            BookingPartyTypeEnum.Individual,
            Guid.CreateVersion7(),
            RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 1, 1, 1, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
            null,
            null,
            "EUR",
            UtcNow,
            UtcNow.AddMinutes(15));
        typeof(RegistrationOrder).GetProperty(nameof(RegistrationOrder.RegistrationOrderStatusId))!.SetValue(order, (int)RegistrationOrderStatusEnum.AwaitingPayment);
        typeof(RegistrationOrder).GetProperty(nameof(RegistrationOrder.OrganizerDirectedTotalMinorSnapshot))!.SetValue(order, organizerDirectedMinor);
        typeof(RegistrationOrder).GetProperty(nameof(RegistrationOrder.PlatformFeeTotalMinorSnapshot))!.SetValue(order, platformFeeMinor);
        typeof(RegistrationOrder).GetProperty(nameof(RegistrationOrder.PlatformContributionTotalMinorSnapshot))!.SetValue(order, platformContributionMinor);
        typeof(RegistrationOrder).GetProperty(nameof(RegistrationOrder.TotalDueMinorSnapshot))!.SetValue(order, organizerDirectedMinor + platformContributionMinor);
        order.ConcurrencyStamp = Guid.Parse("018e4e5c-7f00-7000-8000-000000000099");
        return order;
    }

    private static async Task<ExploreDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseSqlite("Data Source=:memory:")
            .UseSnakeCaseNamingConvention()
            .Options;
        var context = new ExploreDbContext(options);
        context.EnableTenantFilterBypass("Phase 18.2 SQLite persistence test setup.");
        await context.Database.OpenConnectionAsync();
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task<ExploreDbContext> CreateFileContextAsync(string databasePath, bool ensureCreated = true)
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString())
            .UseSnakeCaseNamingConvention()
            .Options;
        var context = new ExploreDbContext(options);
        context.EnableTenantFilterBypass("Phase 18.2 SQLite persistence race test setup.");
        await context.Database.OpenConnectionAsync();
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
        if (ensureCreated)
        {
            await context.Database.EnsureCreatedAsync();
        }

        return context;
    }

    private sealed class MutableTimeProvider(DateTime now) : TimeProvider
    {
        private DateTime current = now;

        public void Set(DateTime value) => current = value;

        public override DateTimeOffset GetUtcNow() => current;
    }
}
