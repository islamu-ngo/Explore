// ABOUTME: Verifies optimistic EventLocation policy writes and append-only PII-free audit orchestration.
// ABOUTME: Covers tenant/event fail-closed identity, controlled UTC, atomic persistence, and post-commit eviction.

using Explore.Application.Caching;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventLocations.Handlers.Commands;
using Explore.Application.Features.EventLocations.Requests.Commands;
using Explore.Application.Features.EventLocations.Validators;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace ApplicationUnitTests.Features.EventLocations.Commands;

[Category("EventLocationPrivacy")]
public sealed class UpdateEventLocationPolicyCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 16, 45, 0, TimeSpan.Zero);

    [Test]
    public async Task ValidatorRequiresBothConcurrencyTokensAndTypedPolicyValues()
    {
        var validator = new UpdateEventLocationPolicyCommandValidator();
        var invalid = Command(
            Guid.Empty,
            Guid.Empty,
            Guid.Empty,
            expectedPolicyVersion: 0,
            (EventLocationDisclosureFields)(1 << 20),
            (LocationDisclosureAudienceEnum)999,
            DateTime.SpecifyKind(Now.UtcDateTime, DateTimeKind.Local));

        var result = await validator.ValidateAsync(invalid);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.PropertyName)).Contains(nameof(invalid.EventId));
        await Assert.That(result.Errors.Select(error => error.PropertyName)).Contains(nameof(invalid.EventLocationId));
        await Assert.That(result.Errors.Select(error => error.PropertyName)).Contains(nameof(invalid.ExpectedConcurrencyStamp));
        await Assert.That(result.Errors.Select(error => error.PropertyName)).Contains(nameof(invalid.ExpectedPolicyVersion));
        await Assert.That(result.Errors.Select(error => error.PropertyName)).Contains(nameof(invalid.SelectedFields));
        await Assert.That(result.Errors.Select(error => error.PropertyName)).Contains(nameof(invalid.FullDetailsAudience));
        await Assert.That(result.Errors.Select(error => error.PropertyName)).Contains(nameof(invalid.RevealFullDetailsFromUtc));
    }

    [Test]
    public async Task ValidPolicyWriteCommitsAggregateAndAuditBeforeEvictingProjectionTags()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid actorId = Guid.CreateVersion7();
        EventLocation placement = EventLocation.CreatePhysical(
            tenantId,
            eventId,
            Guid.CreateVersion7(),
            actorId,
            Now.UtcDateTime.AddDays(-1));
        var eventLocations = Substitute.For<IEventLocationRepository>();
        eventLocations.GetForUpdateAsync(placement.Id, Arg.Any<CancellationToken>()).Returns(placement);
        var audits = Substitute.For<IEventLocationDisclosureAuditRepository>();
        EventLocationDisclosureAudit? appended = null;
        audits.AppendAsync(Arg.Any<EventLocationDisclosureAudit>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                EventLocationDisclosureAudit audit = call.ArgAt<EventLocationDisclosureAudit>(0)!;
                appended = audit;
                return Task.FromResult(audit);
            });
        var unitOfWork = new RecordingUnitOfWork();
        var cache = new RecordingHybridCache(() => unitOfWork.Completed);
        var handler = CreateHandler(
            eventLocations,
            audits,
            unitOfWork,
            cache,
            tenantId,
            actorId);
        DateTime revealAt = Now.UtcDateTime.AddHours(6);
        UpdateEventLocationPolicyCommand command = Command(
            eventId,
            placement.Id,
            placement.ConcurrencyStamp,
            placement.PolicyVersion,
            EventLocationDisclosureFields.VenueName
                | EventLocationDisclosureFields.City
                | EventLocationDisclosureFields.StreetAddress,
            LocationDisclosureAudienceEnum.ConfirmedParticipant,
            revealAt,
            needsPrivacyReview: false);

        var result = await handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(placement.Id);
        await Assert.That(placement.PolicyVersion).IsEqualTo(2);
        await Assert.That(placement.NeedsPrivacyReview).IsFalse();
        await Assert.That(placement.LastPolicyActorUserId).IsEqualTo(actorId);
        await Assert.That(placement.LastPolicyChangedAtUtc).IsEqualTo(Now.UtcDateTime);
        await Assert.That(appended).IsNotNull();
        await Assert.That(appended!.PreviousFields).IsEqualTo(EventLocationDisclosureFields.None);
        await Assert.That(appended.NewFields).IsEqualTo(command.SelectedFields);
        await Assert.That(appended.PreviousAudienceId).IsEqualTo((int)LocationDisclosureAudienceEnum.Never);
        await Assert.That(appended.NewAudienceId).IsEqualTo((int)command.FullDetailsAudience);
        await Assert.That(appended.PreviousRevealFullDetailsFromUtc).IsNull();
        await Assert.That(appended.NewRevealFullDetailsFromUtc).IsEqualTo(revealAt);
        await Assert.That(appended.PreviousPolicyVersion).IsEqualTo(1);
        await Assert.That(appended.NewPolicyVersion).IsEqualTo(2);
        await Assert.That(appended.Reason).IsEqualTo(EventLocationDisclosureAuditReasonEnum.OrganizerPolicyChange);
        await Assert.That(appended.OccurredAtUtc).IsEqualTo(Now.UtcDateTime);
        await Assert.That(cache.RemovedTags).IsEquivalentTo([CacheTags.EventLocation(placement.Id)]);
    }

    [Test]
    public async Task CrossEventOrTenantIdentityIsHiddenWithoutMutationOrEviction()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorId = Guid.CreateVersion7();
        EventLocation placement = EventLocation.CreatePhysical(
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            actorId,
            Now.UtcDateTime.AddDays(-1));
        var eventLocations = Substitute.For<IEventLocationRepository>();
        eventLocations.GetForUpdateAsync(placement.Id, Arg.Any<CancellationToken>()).Returns(placement);
        var audits = Substitute.For<IEventLocationDisclosureAuditRepository>();
        var cache = new RecordingHybridCache();
        var handler = CreateHandler(
            eventLocations,
            audits,
            new RecordingUnitOfWork(),
            cache,
            Guid.CreateVersion7(),
            actorId);
        UpdateEventLocationPolicyCommand command = Command(
            Guid.CreateVersion7(),
            placement.Id,
            placement.ConcurrencyStamp,
            placement.PolicyVersion,
            EventLocationDisclosureFields.Country,
            LocationDisclosureAudienceEnum.Never,
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_location_policy_not_found");
        await Assert.That(placement.PolicyVersion).IsEqualTo(1);
        await audits.DidNotReceive().AppendAsync(
            Arg.Any<EventLocationDisclosureAudit>(),
            Arg.Any<CancellationToken>());
        await Assert.That(cache.RemovedTags).IsEmpty();
    }

    [Test]
    [Arguments(true, false)]
    [Arguments(false, true)]
    public async Task StaleConcurrencyStampOrPolicyVersionUsesStableConcurrentUpdateError(
        bool staleStamp,
        bool stalePolicyVersion)
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid actorId = Guid.CreateVersion7();
        EventLocation placement = EventLocation.CreatePhysical(
            tenantId,
            eventId,
            Guid.CreateVersion7(),
            actorId,
            Now.UtcDateTime.AddDays(-1));
        var eventLocations = Substitute.For<IEventLocationRepository>();
        eventLocations.GetForUpdateAsync(placement.Id, Arg.Any<CancellationToken>()).Returns(placement);
        var audits = Substitute.For<IEventLocationDisclosureAuditRepository>();
        var cache = new RecordingHybridCache();
        var handler = CreateHandler(
            eventLocations,
            audits,
            new RecordingUnitOfWork(),
            cache,
            tenantId,
            actorId);
        UpdateEventLocationPolicyCommand command = Command(
            eventId,
            placement.Id,
            staleStamp ? Guid.CreateVersion7() : placement.ConcurrencyStamp,
            stalePolicyVersion ? placement.PolicyVersion + 1 : placement.PolicyVersion,
            EventLocationDisclosureFields.Country,
            LocationDisclosureAudienceEnum.Never,
            null);

        ConcurrencyConflictException? exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            handler.Handle(command, CancellationToken.None));

        await Assert.That(exception!.Code).IsEqualTo(ConcurrencyConflictException.ConcurrentUpdate);
        await Assert.That(exception.EntityType).IsEqualTo(nameof(EventLocation));
        await Assert.That(exception.EntityId).IsEqualTo(placement.Id.ToString("D"));
        await audits.DidNotReceive().AppendAsync(
            Arg.Any<EventLocationDisclosureAudit>(),
            Arg.Any<CancellationToken>());
        await Assert.That(cache.RemovedTags).IsEmpty();
    }

    private static UpdateEventLocationPolicyCommandHandler CreateHandler(
        IEventLocationRepository eventLocations,
        IEventLocationDisclosureAuditRepository audits,
        IUnitOfWork unitOfWork,
        HybridCache cache,
        Guid tenantId,
        Guid actorId)
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        var userContext = Substitute.For<IUserContext>();
        userContext.GetRequiredUserId().Returns(actorId);
        return new(
            eventLocations,
            audits,
            unitOfWork,
            cache,
            tenantContext,
            userContext,
            new FixedTimeProvider(Now));
    }

    private static UpdateEventLocationPolicyCommand Command(
        Guid eventId,
        Guid eventLocationId,
        Guid expectedConcurrencyStamp,
        int expectedPolicyVersion,
        EventLocationDisclosureFields selectedFields,
        LocationDisclosureAudienceEnum audience,
        DateTime? revealAt,
        bool needsPrivacyReview = true) => new()
        {
            EventId = eventId,
            EventLocationId = eventLocationId,
            ExpectedConcurrencyStamp = expectedConcurrencyStamp,
            ExpectedPolicyVersion = expectedPolicyVersion,
            SelectedFields = selectedFields,
            FullDetailsAudience = audience,
            RevealFullDetailsFromUtc = revealAt,
            NeedsPrivacyReview = needsPrivacyReview
        };

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public bool Completed { get; private set; }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default)
        {
            await operation(ct);
            Completed = true;
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
        {
            T result = await operation(ct);
            Completed = true;
            return result;
        }

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => ExecuteInTransactionAsync(operation, ct);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingHybridCache(Func<bool>? transactionCompleted = null) : HybridCache
    {
        public List<string> RemovedTags { get; } = [];

        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) => factory(state, cancellationToken);

        public override ValueTask RemoveAsync(
            string key,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public override ValueTask RemoveByTagAsync(
            string tag,
            CancellationToken cancellationToken = default)
        {
            if (transactionCompleted is not null && !transactionCompleted())
            {
                throw new InvalidOperationException("Cache invalidation ran before transaction commit.");
            }

            RemovedTags.Add(tag);
            return ValueTask.CompletedTask;
        }

        public override ValueTask SetAsync<T>(
            string key,
            T value,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
