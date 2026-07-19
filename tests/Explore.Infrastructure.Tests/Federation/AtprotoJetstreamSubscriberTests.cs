// ABOUTME: Tests the bounded exact-collection Jetstream subscriber with fake stream and fenced store boundaries.
// ABOUTME: Covers valid ingestion, quarantine, replay, failed apply recovery, capability gates, and cancellation.

using System.Runtime.CompilerServices;
using System.Text.Json;
using CarpaNet;
using CarpaNet.Jetstream;
using Explore.Application.Contracts.Persistence;
using Explore.Domain.Federation;
using Explore.Infrastructure.Services.Federation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Tests.Federation;

public sealed class AtprotoJetstreamSubscriberTests
{
    private static readonly DateTime ObservedAt = new(2026, 7, 19, 10, 0, 0, DateTimeKind.Utc);
    private const string AllowedDid = "did:plc:remote-owner";

    [Test]
    public async Task RunSingleLease_UsesExactCollectionsAndAtomicallyAppliesEnabledTenantPresentation()
    {
        Guid tenantId = Guid.CreateVersion7();
        var store = new FakeRuntimeStore([tenantId]);
        var source = new FakeEventSource([EventEnvelope(100)]);
        using var subscriber = CreateSubscriber(store, source);

        bool consumed = await subscriber.RunSingleLeaseAsync(CancellationToken.None);

        await Assert.That(consumed).IsTrue();
        await Assert.That(source.Subscription!.WantedCollections)
            .IsEquivalentTo([AtprotoJetstreamConstants.EventCollection, AtprotoJetstreamConstants.RsvpCollection]);
        await Assert.That(source.Subscription.WantedCollections).DoesNotContain(collection => collection.Contains('*'));
        await Assert.That(source.Subscription.WantedDids).IsEquivalentTo([AllowedDid]);
        await Assert.That(store.Applied).HasSingleItem();
        await Assert.That(store.Applied[0].Record!.Collection).IsEqualTo(AtprotoJetstreamConstants.EventCollection);
        await Assert.That(store.Applied[0].Presentations.Select(value => value.TenantId)).IsEquivalentTo([tenantId]);
        await Assert.That(store.Applied[0].EventProjection!.Name).IsEqualTo("Remote event");
        await Assert.That(store.Applied[0].EventProjection!.AtprotoRecordId)
            .IsEqualTo(store.Applied[0].Record!.Id);
    }

    [Test]
    public async Task RunSingleLease_DisabledCapabilityDoesNotOpenStreamOrMaterialize()
    {
        var store = new FakeRuntimeStore([]);
        var source = new FakeEventSource([EventEnvelope(100)]);
        using var subscriber = CreateSubscriber(store, source);

        bool consumed = await subscriber.RunSingleLeaseAsync(CancellationToken.None);

        await Assert.That(consumed).IsFalse();
        await Assert.That(source.Subscription).IsNull();
        await Assert.That(store.Applied).IsEmpty();
    }

    [Test]
    public async Task RunSingleLease_EnabledCapabilityWithMissingDidAllowlistDoesNotOpenStream()
    {
        var store = new FakeRuntimeStore([Guid.CreateVersion7()]);
        var source = new FakeEventSource([EventEnvelope(100)]);
        using var subscriber = CreateSubscriber(store, source, allowedDids: []);

        bool consumed = await subscriber.RunSingleLeaseAsync(CancellationToken.None);

        await Assert.That(consumed).IsFalse();
        await Assert.That(source.Subscription).IsNull();
        await Assert.That(store.Applied).IsEmpty();
    }

    [Test]
    public async Task EventSource_EmptyDidAllowlistIsRejectedBeforeNetworkAccess()
    {
        var source = new CarpaNetJetstreamEventSource();
        var subscription = new AtprotoJetstreamSubscription(
            new Uri("https://jetstream.example.test"),
            AtprotoJetstreamConstants.Collections,
            WantedDids: [],
            Cursor: null,
            MaxMessageSizeBytes: 2_113_536);
        await using IAsyncEnumerator<JetstreamEvent> enumerator = source
            .SubscribeAsync(subscription, CancellationToken.None)
            .GetAsyncEnumerator();

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await enumerator.MoveNextAsync());
    }

    [Test]
    public async Task RunSingleLease_CapabilityRevokedAfterConnectStopsNewPresentationAndCursor()
    {
        var store = new FakeRuntimeStore([Guid.CreateVersion7()]);
        var source = new FakeEventSource([EventEnvelope(100)])
        {
            BeforeYield = () => store.EnabledTenants = []
        };
        using var subscriber = CreateSubscriber(store, source);

        bool consumed = await subscriber.RunSingleLeaseAsync(CancellationToken.None);

        await Assert.That(consumed).IsFalse();
        await Assert.That(store.Cursor).IsEqualTo(0);
        await Assert.That(store.Applied).IsEmpty();
    }

    [Test]
    public async Task RunSingleLease_FailedApplyLeavesCursorForReplayAndSecondAttemptConverges()
    {
        var store = new FakeRuntimeStore([Guid.CreateVersion7()]) { FailNextApply = true };
        var source = new FakeEventSource([EventEnvelope(100)]);
        using var subscriber = CreateSubscriber(store, source);

        bool first = await subscriber.RunSingleLeaseAsync(CancellationToken.None);
        bool second = await subscriber.RunSingleLeaseAsync(CancellationToken.None);

        await Assert.That(first).IsFalse();
        await Assert.That(second).IsTrue();
        await Assert.That(store.Cursor).IsEqualTo(100);
        await Assert.That(store.Applied).Count().IsEqualTo(2);
    }

    [Test]
    public async Task RunSingleLease_CrashAfterAtomicApplyReplaysWithoutDuplicate()
    {
        var store = new FakeRuntimeStore([Guid.CreateVersion7()]) { ThrowAfterNextApply = true };
        var source = new FakeEventSource([EventEnvelope(100), EventEnvelope(100)]);
        using var subscriber = CreateSubscriber(store, source);

        await Assert.ThrowsAsync<InvalidOperationException>(() => subscriber.RunSingleLeaseAsync(CancellationToken.None));
        bool replayed = await subscriber.RunSingleLeaseAsync(CancellationToken.None);

        await Assert.That(replayed).IsFalse();
        await Assert.That(store.Cursor).IsEqualTo(100);
        await Assert.That(store.Applied).HasSingleItem();
    }

    [Test]
    public async Task HostedSubscriber_StreamFailureReconnectsAndConverges()
    {
        var store = new FakeRuntimeStore([Guid.CreateVersion7()]);
        var source = new FakeEventSource([EventEnvelope(100)]) { FailuresRemaining = 1 };
        using var subscriber = CreateSubscriber(store, source);

        await subscriber.StartAsync(CancellationToken.None);
        try
        {
            for (int attempt = 0; attempt < 100 && store.Cursor == 0; attempt++)
            {
                await Task.Delay(20);
            }

            await Assert.That(store.Cursor).IsEqualTo(100);
            await Assert.That(source.SubscriptionCount).IsGreaterThanOrEqualTo(2);
            await Assert.That(store.Applied).HasSingleItem();
        }
        finally
        {
            await subscriber.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task RunSingleLease_RenewalExceptionCancelsActiveStream()
    {
        var store = new FakeRuntimeStore([Guid.CreateVersion7()])
        {
            RenewalException = new InvalidOperationException("simulated_renewal_failure")
        };
        var source = new FakeEventSource([]) { WaitForCancellation = true };
        using var subscriber = CreateSubscriber(store, source, leaseRenewalSeconds: 1);

        Task<bool> run = subscriber.RunSingleLeaseAsync(CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => run);
        await Assert.That(source.CancellationObserved).IsTrue();
    }

    [Test]
    public async Task Parser_InvalidCidIsQuarantinedWithoutThrowing()
    {
        JetstreamEvent envelope = EventEnvelope(101);
        envelope.Commit!.Cid = "not-a-cid";

        AtprotoJetstreamParsedEnvelope outcome = AtprotoJetstreamEnvelopeParser.Parse(
            envelope,
            100,
            [AllowedDid],
            ObservedAt);

        await Assert.That(outcome.Record).IsNull();
        await Assert.That(outcome.Quarantine!.ReasonCode).IsEqualTo("invalid_record_cid");
    }

    [Test]
    public async Task Parser_EmptyDidAllowlistRejectsOtherwiseValidRecord()
    {
        AtprotoJetstreamParsedEnvelope outcome = AtprotoJetstreamEnvelopeParser.Parse(
            EventEnvelope(101),
            100,
            [],
            ObservedAt);

        await Assert.That(outcome.Record).IsNull();
        await Assert.That(outcome.Quarantine!.ReasonCode).IsEqualTo("did_not_allowed");
    }

    [Test]
    public async Task Parser_QuarantinesWrongCollectionTypeSizeAndShapeWithoutRawPayload()
    {
        string oversized = new('x', AtprotoRecordSizeValidator.MaximumJsonBytes + 1);
        JetstreamEvent[] invalid =
        [
            EventEnvelope(101, collection: "app.bsky.feed.post"),
            EventEnvelope(102, type: AtprotoJetstreamConstants.RsvpCollection),
            EventEnvelope(103, json: JsonSerializer.Serialize(new { type = AtprotoJetstreamConstants.EventCollection, name = oversized, createdAt = "2026-07-19T10:00:00Z" }).Replace("\"type\"", "\"$type\"", StringComparison.Ordinal)),
            EventEnvelope(104, json: "{\"$type\":\"community.lexicon.calendar.event\",\"createdAt\":\"2026-07-19T10:00:00Z\"}"),
            EventEnvelope(105)
        ];

        AtprotoJetstreamParsedEnvelope[] outcomes = invalid
            .Select((value, index) => AtprotoJetstreamEnvelopeParser.Parse(
                value,
                100,
                index == invalid.Length - 1 ? ["did:plc:another-owner"] : [AllowedDid],
                ObservedAt))
            .ToArray();

        await Assert.That(outcomes.All(outcome => outcome.Quarantine is not null)).IsTrue();
        await Assert.That(outcomes.Select(outcome => outcome.Quarantine!.ReasonCode))
            .IsEquivalentTo(["collection_not_allowed", "record_type_mismatch", "record_too_large", "invalid_record_shape", "did_not_allowed"]);
        await Assert.That(outcomes.All(outcome => outcome.Quarantine!.EnvelopeHash.Length == 64)).IsTrue();
        await Assert.That(outcomes[3].EventProjectionInvalidation).IsNotNull();
        await Assert.That(outcomes[3].EventProjectionInvalidation!.SourceVersion).IsEqualTo(104);
    }

    [Test]
    public async Task ParserMaterializesBoundedFieldsAndSelectsFirstSafeHttpsSource()
    {
        string json = """
            {
              "$type":"community.lexicon.calendar.event",
              "name":"Remote gathering",
              "description":"Public description",
              "createdAt":"2026-07-19T10:00:00Z",
              "startsAt":"2026-07-20T10:00:00Z",
              "endsAt":"2026-07-20T11:00:00Z",
              "mode":"community.lexicon.calendar.event#virtual",
              "status":"community.lexicon.calendar.event#scheduled",
              "rsvpExpected":true,
              "uris":[
                {"uri":"http://unsafe.example/event"},
                {"uri":"https://events.example/event"}
              ]
            }
            """;

        AtprotoJetstreamParsedEnvelope outcome = AtprotoJetstreamEnvelopeParser.Parse(
            EventEnvelope(106, json: json),
            105,
            [AllowedDid],
            ObservedAt);

        await Assert.That(outcome.Quarantine).IsNull();
        await Assert.That(outcome.EventProjection!.Name).IsEqualTo("Remote gathering");
        await Assert.That(outcome.EventProjection.Mode).IsEqualTo("virtual");
        await Assert.That(outcome.EventProjection.Status).IsEqualTo("scheduled");
        await Assert.That(outcome.EventProjection.RsvpExpected).IsTrue();
        await Assert.That(outcome.EventProjection.SourceUrl).IsEqualTo("https://events.example/event");
        await Assert.That(outcome.EventProjection.SourceVersion).IsEqualTo(106);
    }

    [Test]
    public async Task Parser_AcceptsRsvpAndBuildsEventTombstoneWithDependencyIdentity()
    {
        string cid = ATCid.FromSha256Hash(new byte[32]).Value;
        string subject = "at://did:plc:event-owner/community.lexicon.calendar.event/3m-event";
        JetstreamEvent rsvp = EventEnvelope(
            101,
            AtprotoJetstreamConstants.RsvpCollection,
            AtprotoJetstreamConstants.RsvpCollection,
            $$"""{"$type":"community.lexicon.calendar.rsvp","subject":{"uri":"{{subject}}","cid":"{{cid}}"},"status":"community.lexicon.calendar.rsvp#interested"}""");
        JetstreamEvent tombstone = EventEnvelope(102, operation: "delete", record: null);

        AtprotoJetstreamParsedEnvelope rsvpOutcome = AtprotoJetstreamEnvelopeParser.Parse(rsvp, 100, [AllowedDid], ObservedAt);
        AtprotoJetstreamParsedEnvelope tombstoneOutcome = AtprotoJetstreamEnvelopeParser.Parse(tombstone, 101, [AllowedDid], ObservedAt);

        await Assert.That(rsvpOutcome.Record!.SubjectUri).IsEqualTo(subject);
        await Assert.That(rsvpOutcome.Record.SubjectCid).IsEqualTo(cid);
        await Assert.That(tombstoneOutcome.Record!.TombstonedAt).IsEqualTo(ObservedAt);
        await Assert.That(tombstoneOutcome.Record.RecordJson).IsNull();
    }

    [Test]
    public async Task RunSingleLease_PropagatesCancellationToStream()
    {
        var store = new FakeRuntimeStore([Guid.CreateVersion7()]);
        var source = new FakeEventSource([]) { WaitForCancellation = true };
        using var subscriber = CreateSubscriber(store, source);
        using var cancellation = new CancellationTokenSource();

        Task<bool> run = subscriber.RunSingleLeaseAsync(cancellation.Token);
        await source.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => run);
        await Assert.That(source.CancellationObserved).IsTrue();
    }

    [Test]
    public async Task RunSingleLease_OutOfRangeCursorIsQuarantinedWithoutBlockingNextLegitimateEnvelope()
    {
        var store = new FakeRuntimeStore([Guid.CreateVersion7()]);
        var source = new FakeEventSource([EventEnvelope(long.MaxValue), EventEnvelope(100)]);
        using var subscriber = CreateSubscriber(store, source);

        bool consumed = await subscriber.RunSingleLeaseAsync(CancellationToken.None);

        await Assert.That(consumed).IsTrue();
        await Assert.That(store.Cursor).IsEqualTo(100);
        await Assert.That(store.Applied).Count().IsEqualTo(2);
        await Assert.That(store.Applied[0].Quarantine!.ReasonCode).IsEqualTo("invalid_cursor");
        await Assert.That(store.Applied[1].Record).IsNotNull();
    }

    [Test]
    public async Task OptionsValidator_RejectsMutableOrUnboundedTransportConfiguration()
    {
        var validator = new AtprotoJetstreamOptionsValidator();
        var invalid = new AtprotoJetstreamOptions
        {
            Endpoint = "http://user:secret@jetstream.example.test/path?cursor=1",
            MaxMessageSizeBytes = int.MaxValue,
            LeaseDurationSeconds = 10,
            LeaseRenewalSeconds = 10,
            AllowedDids = ["not-a-did"]
        };

        ValidateOptionsResult result = validator.Validate(null, invalid);

        await Assert.That(result.Failed).IsTrue();
        await Assert.That(result.Failures).Count().IsGreaterThanOrEqualTo(4);
    }

    [Test]
    public async Task OptionsValidator_AllowsEmptyDormantDidAllowlist()
    {
        var validator = new AtprotoJetstreamOptionsValidator();

        ValidateOptionsResult result = validator.Validate(null, new AtprotoJetstreamOptions());

        await Assert.That(result.Succeeded).IsTrue();
    }

    private static AtprotoJetstreamSubscriber CreateSubscriber(
        IAtprotoJetstreamRuntimeStore store,
        IAtprotoJetstreamEventSource source,
        int leaseRenewalSeconds = 10,
        string[]? allowedDids = null)
        => new(
            store,
            source,
            Options.Create(new AtprotoJetstreamOptions
            {
                Endpoint = "https://jetstream.example.test",
                LeaseDurationSeconds = 30,
                LeaseRenewalSeconds = leaseRenewalSeconds,
                RetryMinimumMilliseconds = 10,
                RetryMaximumMilliseconds = 100,
                AllowedDids = allowedDids ?? [AllowedDid]
            }),
            TimeProvider.System,
            NullLogger<AtprotoJetstreamSubscriber>.Instance);

    private static JetstreamEvent EventEnvelope(
        long cursor,
        string collection = AtprotoJetstreamConstants.EventCollection,
        string? type = AtprotoJetstreamConstants.EventCollection,
        string? json = null,
        string operation = "create",
        JsonElement? record = default)
    {
        JsonElement? payload = operation == "delete"
            ? record
            : record ?? JsonDocument.Parse(json ?? $$"""{"$type":"{{type}}","name":"Remote event","createdAt":"2026-07-19T10:00:00Z"}""").RootElement.Clone();
        return new JetstreamEvent
        {
            Did = "did:plc:remote-owner",
            TimeUs = cursor,
            Kind = "commit",
            Commit = new JetstreamCommit
            {
                Operation = operation,
                Collection = collection,
                Rkey = "3m-remote",
                Cid = operation == "delete" ? null : ATCid.FromSha256Hash(new byte[32]).Value,
                Record = payload
            }
        };
    }

    private sealed class FakeEventSource(IReadOnlyList<JetstreamEvent> events) : IAtprotoJetstreamEventSource
    {
        public AtprotoJetstreamSubscription? Subscription { get; private set; }
        public bool WaitForCancellation { get; init; }
        public bool CancellationObserved { get; private set; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Action? BeforeYield { get; init; }
        public int FailuresRemaining { get; set; }
        public int SubscriptionCount { get; private set; }

        public async IAsyncEnumerable<JetstreamEvent> SubscribeAsync(
            AtprotoJetstreamSubscription subscription,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Subscription = subscription;
            SubscriptionCount++;
            Started.TrySetResult();
            if (FailuresRemaining > 0)
            {
                FailuresRemaining--;
                throw new InvalidOperationException("simulated_stream_failure");
            }

            foreach (JetstreamEvent value in events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                BeforeYield?.Invoke();
                yield return value;
            }

            if (WaitForCancellation)
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    CancellationObserved = true;
                    throw;
                }
            }
        }
    }

    private sealed class FakeRuntimeStore(IReadOnlyList<Guid> enabledTenants) : IAtprotoJetstreamRuntimeStore
    {
        private readonly Guid _stateId = Guid.CreateVersion7();
        public List<AtprotoJetstreamApplyRequest> Applied { get; } = [];
        public bool FailNextApply { get; set; }
        public bool ThrowAfterNextApply { get; set; }
        public Exception? RenewalException { get; set; }
        public long Cursor { get; private set; }
        public IReadOnlyList<Guid> EnabledTenants { get; set; } = enabledTenants;

        public Task<IReadOnlyList<Guid>> ResolveEnabledTenantIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(EnabledTenants);

        public Task<AtprotoJetstreamClaim?> TryClaimAsync(
            string service,
            string owner,
            DateTime claimedAt,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken) =>
            Task.FromResult<AtprotoJetstreamClaim?>(new(_stateId, service, Cursor, Guid.CreateVersion7(), Cursor + 1));

        public Task<bool> TryRenewAsync(
            AtprotoJetstreamClaim claim,
            DateTime observedAt,
            DateTime leaseExpiresAt,
            CancellationToken cancellationToken) => RenewalException is null
                ? Task.FromResult(true)
                : Task.FromException<bool>(RenewalException);

        public Task<bool> TryApplyAndAdvanceAsync(
            AtprotoJetstreamApplyRequest request,
            CancellationToken cancellationToken)
        {
            Applied.Add(request);
            if (FailNextApply)
            {
                FailNextApply = false;
                return Task.FromResult(false);
            }

            if (request.AdvanceCursor)
            {
                Cursor = request.NextCursor;
            }
            if (ThrowAfterNextApply)
            {
                ThrowAfterNextApply = false;
                throw new InvalidOperationException("simulated_crash_after_commit");
            }

            return Task.FromResult(true);
        }
    }
}
