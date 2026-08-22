// ABOUTME: Verifies governed ATProto PDS recovery orchestration without coupling Application to CarpaNet.
// ABOUTME: Proves disabled and downtime-only modes avoid PDS I/O while Full reconciliation is atomic and fenced.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Handlers.Commands;
using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Features.Federation.Atproto.Requests.Commands;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Application.Features.Federation.Atproto.Validators;
using Explore.Application.Models.Storage;
using Explore.Application.Services.Federation;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Federation;

public sealed class ReconcileAtprotoPdsSnapshotsCommandHandlerTests
{
    private static readonly DateTime SnapshotStartedAt = new(2026, 7, 21, 10, 0, 0, DateTimeKind.Utc);
    private const string Did = "did:plc:recovery-owner";
    private const string SecondDid = "did:plc:second-owner";
    private const long Cursor = 42;
    private const string ThumbnailCid = "bafkreibm6jg3ux5quca3po4nukm4m6xkfxzq4bgxjucfd4g6yuk3z7q7di";

    [Test]
    public async Task SnapshotApplyRequest_DefaultsImportPlansToEmpty()
    {
        var request = new AtprotoPdsSnapshotApplyRequest(
            new AtprotoJetstreamClaim(
                Guid.CreateVersion7(),
                "https://jetstream.example",
                42,
                Guid.CreateVersion7(),
                1),
            [Did],
            [],
            [],
            1,
            SnapshotStartedAt);

        await Assert.That(request.EventImports.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_DisabledPolicy_PerformsNoSnapshotIo()
    {
        var fixture = new Fixture();
        fixture.SetBackfill(enabled: false, mode: "full", locked: true);

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(Command([Did]), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Disabled);
        await fixture.Gateway.DidNotReceiveWithAnyArgs().FetchAsync(default!, default, default);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileWithResultAsync(default!, default);
    }

    [Test]
    public async Task Handle_DowntimeOnlyPolicy_UsesNativeJetstreamReplayWithoutPdsIo()
    {
        var fixture = new Fixture();
        fixture.SetBackfill(enabled: true, mode: "downtime_only", locked: true);

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(Command([Did]), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.DowntimeOnly);
        await fixture.Gateway.DidNotReceiveWithAnyArgs().FetchAsync(default!, default, default);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileWithResultAsync(default!, default);
    }

    [Test]
    public async Task Handle_DowntimeOnlyPolicy_PreservesGenericJetstreamDidScope()
    {
        var fixture = new Fixture();
        fixture.SetBackfill(enabled: true, mode: "downtime_only", locked: true);

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(
            Command(["did:example:jetstream-owner"]),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.DowntimeOnly);
        await fixture.Gateway.DidNotReceiveWithAnyArgs().FetchAsync(default!, default, default);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileWithResultAsync(default!, default);
    }

    [Test]
    public async Task Handle_FullPolicyWithGlobalDidScope_FailsClosed()
    {
        var fixture = new Fixture();

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(Command([]), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.ScopeRejected);
        await fixture.Gateway.DidNotReceiveWithAnyArgs().FetchAsync(default!, default, default);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileWithResultAsync(default!, default);
    }

    [Test]
    public async Task Handle_FullPolicyWithMixedPdsAndUnsupportedDidScope_FailsClosedBeforePresentationIo()
    {
        var fixture = new Fixture();
        const string unsupportedDid = "did:key:unsupported-owner";

        AtprotoPdsRecoveryResult first = await fixture.Handler.Handle(
            Command([unsupportedDid, Did]),
            CancellationToken.None);
        AtprotoPdsRecoveryResult reordered = await fixture.Handler.Handle(
            Command([Did, unsupportedDid]),
            CancellationToken.None);

        await Assert.That(first.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.ScopeRejected);
        await Assert.That(reordered.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.ScopeRejected);
        await Assert.That(reordered.Fingerprint).IsEqualTo(first.Fingerprint);
        await fixture.AssertNoPresentationIoAsync(expectedPolicyResolutions: 2);
        await fixture.Gateway.DidNotReceiveWithAnyArgs().FetchAsync(default!, default, default);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileWithResultAsync(default!, default);
    }

    [Test]
    public async Task Handle_FullPolicyWithOversizedDidScope_ReturnsScopeRejected()
    {
        var fixture = new Fixture();
        string[] allowedDids = Enumerable.Range(0, ReconcileAtprotoPdsSnapshotsCommandHandler.MaximumRecoveryDids + 1)
            .Select(index => $"did:plc:owner-{index}")
            .ToArray();

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(
            Command(allowedDids),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.ScopeRejected);
        await Assert.That(result.AppliedDids).IsEqualTo(0);
        await fixture.AssertNoPolicyIoAsync();
        await fixture.Gateway.DidNotReceiveWithAnyArgs().FetchAsync(default!, default, default);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileWithResultAsync(default!, default);
    }

    [Test]
    public async Task Handle_DidScopeBeyondJetstreamProtocolLimit_FailsValidation()
    {
        var fixture = new Fixture();
        string[] allowedDids = Enumerable.Range(0, ReconcileAtprotoPdsSnapshotsCommandValidator.MaximumProtocolDids + 1)
            .Select(index => $"did:plc:owner-{index}")
            .ToArray();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            fixture.Handler.Handle(Command(allowedDids), CancellationToken.None));

        await fixture.AssertNoPolicyIoAsync();
        await fixture.Gateway.DidNotReceiveWithAnyArgs().FetchAsync(default!, default, default);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileWithResultAsync(default!, default);
    }

    [Test]
    public async Task Handle_MalformedDid_FailsValidation()
    {
        var fixture = new Fixture();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            fixture.Handler.Handle(Command(["not-a-did"]), CancellationToken.None));

        await fixture.AssertNoPolicyIoAsync();
        await fixture.Gateway.DidNotReceiveWithAnyArgs().FetchAsync(default!, default, default);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileWithResultAsync(default!, default);
    }

    [Test]
    public async Task Handle_FullPolicy_NormalizesDidsAndReconcilesOneAtomicBatch()
    {
        var fixture = new Fixture();
        fixture.Gateway.FetchAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(call => CompleteSnapshot(call.ArgAt<string>(0), includeAcceptedItem: call.ArgAt<string>(0) == Did));
        fixture.Repository.TryReconcileWithResultAsync(Arg.Any<AtprotoPdsSnapshotApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AtprotoPersistenceApplyResult(true, []));

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(
            Command([SecondDid, Did, Did]),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Completed);
        await Assert.That(result.AppliedDids).IsEqualTo(2);
        await Assert.That(result.FailedDids).IsEqualTo(0);
        await fixture.Gateway.Received(1).FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>());
        await fixture.Gateway.Received(1).FetchAsync(SecondDid, Arg.Any<long>(), Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).TryReconcileWithResultAsync(
            Arg.Is<AtprotoPdsSnapshotApplyRequest>(request =>
                request.ScannedDids.SequenceEqual(new[] { Did, SecondDid })
                && request.Snapshots.Select(snapshot => snapshot.Did).SequenceEqual(new[] { Did, SecondDid })
                && request.Snapshots[0].PresentIdentities.Count == 1
                && request.Snapshots[0].Items.Count == 1
                && request.PresentationTenantIds.SequenceEqual(new[] { fixture.TenantId })),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ConclusiveArchiveProbeWithNoChanges_SkipsAllPdsIo()
    {
        var fixture = new Fixture();
        fixture.ArchiveProbe
            .ResolveChangedDidsAsync(Arg.Any<long>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(AtprotoArchiveChangeScope.NoChanges);

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(
            Command([SecondDid, Did]),
            CancellationToken.None);

        // The whole point: a restart with no sealed calendar activity must not re-fetch or re-verify.
        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Unchanged);
        await fixture.Gateway.DidNotReceiveWithAnyArgs().FetchAsync(default!, default, default);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileWithResultAsync(default!, default);
    }

    [Test]
    public async Task Handle_ConclusiveArchiveProbe_NarrowsFetchAndAbsenceScopeToChangedDids()
    {
        var fixture = new Fixture();
        fixture.ArchiveProbe
            .ResolveChangedDidsAsync(Arg.Any<long>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new AtprotoArchiveChangeScope(true, [Did]));
        fixture.Gateway.FetchAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(call => CompleteSnapshot(call.ArgAt<string>(0), includeAcceptedItem: true));
        fixture.Repository.TryReconcileWithResultAsync(Arg.Any<AtprotoPdsSnapshotApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AtprotoPersistenceApplyResult(true, []));

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(
            Command([SecondDid, Did]),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Completed);
        await Assert.That(result.AppliedDids).IsEqualTo(1);
        await fixture.Gateway.Received(1).FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>());
        await fixture.Gateway.DidNotReceive().FetchAsync(SecondDid, Arg.Any<long>(), Arg.Any<CancellationToken>());
        // ScannedDids must exclude the unfetched DID, otherwise persistence would treat its records as absent.
        await fixture.Repository.Received(1).TryReconcileWithResultAsync(
            Arg.Is<AtprotoPdsSnapshotApplyRequest>(request =>
                request.ScannedDids.SequenceEqual(new[] { Did })
                && request.Snapshots.Count == request.ScannedDids.Count),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_InconclusiveArchiveProbe_KeepsFullConfiguredScope()
    {
        var fixture = new Fixture();
        fixture.ArchiveProbe
            .ResolveChangedDidsAsync(Arg.Any<long>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(AtprotoArchiveChangeScope.Inconclusive);
        fixture.Gateway.FetchAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(call => CompleteSnapshot(call.ArgAt<string>(0), includeAcceptedItem: call.ArgAt<string>(0) == Did));
        fixture.Repository.TryReconcileWithResultAsync(Arg.Any<AtprotoPdsSnapshotApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AtprotoPersistenceApplyResult(true, []));

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(
            Command([SecondDid, Did]),
            CancellationToken.None);

        // An unavailable or unsure archive must never narrow coverage.
        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Completed);
        await Assert.That(result.AppliedDids).IsEqualTo(2);
        await fixture.Gateway.Received(1).FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>());
        await fixture.Gateway.Received(1).FetchAsync(SecondDid, Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ArchiveProbeReceivesConsumerCursorAndConfiguredScope()
    {
        var fixture = new Fixture();
        fixture.ArchiveProbe
            .ResolveChangedDidsAsync(Arg.Any<long>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(AtprotoArchiveChangeScope.NoChanges);

        await fixture.Handler.Handle(Command([SecondDid, Did, Did]), CancellationToken.None);

        await fixture.ArchiveProbe.Received(1).ResolveChangedDidsAsync(
            Cursor,
            Arg.Is<IReadOnlyList<string>>(dids => dids.SequenceEqual(new[] { Did, SecondDid })),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_FullPolicy_AttachesExactImportPlanOnlyForVisibleTenant()
    {
        var fixture = new Fixture();
        Guid hiddenTenantId = Guid.CreateVersion7();
        fixture.SetTenants(fixture.TenantId, hiddenTenantId);
        fixture.SetPresentation(enabled: false, locked: false);
        fixture.SetTenantPresentation(fixture.TenantId, enabled: true);
        fixture.SetTenantPresentation(hiddenTenantId, enabled: false);
        fixture.Gateway.FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(CompleteSnapshot(Did, includeAcceptedItem: true));
        var staged = new FileStorageWriteResult(
            "returned-provider",
            "returned/pds-thumbnail",
            8,
            "image/png",
            new string('a', 64));
        fixture.ThumbnailGateway.FetchAndStageAsync(
                Arg.Any<AtprotoThumbnailBlobCandidate>(),
                fixture.TenantId,
                Arg.Any<CancellationToken>())
            .Returns(staged);
        fixture.Repository.TryReconcileWithResultAsync(
                Arg.Any<AtprotoPdsSnapshotApplyRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new AtprotoPersistenceApplyResult(true, [staged]));

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(
            Command([Did]),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Completed);
        await fixture.Repository.Received(1).TryReconcileWithResultAsync(
            Arg.Is<AtprotoPdsSnapshotApplyRequest>(request =>
                request.EventImports.Count == 1
                && request.EventImports[0].TenantId == fixture.TenantId
                && request.EventImports[0].AtprotoRecordId == request.Snapshots[0].Items[0].Record.Id
                && request.EventImports[0].Did == Did
                && request.EventImports[0].AtUri
                    == $"at://{Did}/{AtprotoEventPublicationPlanner.EventCollection}/event-1"
                && request.EventImports[0].Name == "Recovered event"
                && request.EventImports[0].CreatedAt == new DateTimeOffset(SnapshotStartedAt)
                && request.EventImports[0].Description == "Recovered event description."
                && request.EventImports[0].SourceUrl == "https://events.example/recovered"
                && request.EventImports[0].StartsAt == new DateTimeOffset(SnapshotStartedAt).AddDays(1)
                && request.EventImports[0].EndsAt == new DateTimeOffset(SnapshotStartedAt).AddDays(1).AddHours(2)
                && request.EventImports[0].Mode == "#hybrid"
                && request.EventImports[0].Status == "#scheduled"
                && request.EventImports[0].RsvpExpected == true
                && request.EventImports[0].TimeZoneId == "Europe/Brussels"
                && request.EventImports[0].Thumbnail == new AtprotoThumbnailBlobCandidate(
                    Did,
                    ThumbnailCid,
                    "image/png",
                    8)
                && request.EventImports[0].StagedThumbnail == staged
                && request.EventImports.All(plan => plan.TenantId != hiddenTenantId)),
            Arg.Any<CancellationToken>());
        await fixture.ThumbnailGateway.Received(1).FetchAndStageAsync(
            Arg.Is<AtprotoThumbnailBlobCandidate>(candidate =>
                candidate.Did == Did
                && candidate.Cid == ThumbnailCid
                && candidate.MimeType == "image/png"
                && candidate.Size == 8),
            fixture.TenantId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_FullPolicy_InvalidEventProjectionFailsBeforePersistence()
    {
        var fixture = new Fixture();
        AtprotoRecord record = Record(Did);
        AtprotoEventProjection projection = Projection(record);
        projection.Name = " ";
        projection.CreatedAt = default;
        var identity = new AtprotoPdsSnapshotIdentity(record.Collection, record.RecordKey);
        var snapshot = new AtprotoPdsSnapshot(
            Did,
            [identity],
            [new AtprotoPdsSnapshotItem(record, projection)]);
        fixture.Gateway.FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(AtprotoPdsSnapshotFetchResult.Complete(snapshot));

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            fixture.Handler.Handle(Command([Did]), CancellationToken.None));

        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileWithResultAsync(default!, default);
    }

    [Test]
    public async Task Handle_FullPolicy_SnapshotDeletionAttachesNoImportPlan()
    {
        var fixture = new Fixture();
        fixture.Gateway.FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(AtprotoPdsSnapshotFetchResult.Complete(
                new AtprotoPdsSnapshot(Did, [], [])));
        fixture.Repository.TryReconcileWithResultAsync(
                Arg.Any<AtprotoPdsSnapshotApplyRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new AtprotoPersistenceApplyResult(true, []));

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(
            Command([Did]),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Completed);
        await fixture.Repository.Received(1).TryReconcileWithResultAsync(
            Arg.Is<AtprotoPdsSnapshotApplyRequest>(request => request.EventImports.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_FullPolicy_IntersectsBackfillAndPresentationAudiences()
    {
        var fixture = new Fixture();
        Guid backfillDisabledTenantId = Guid.CreateVersion7();
        fixture.SetTenants(fixture.TenantId, backfillDisabledTenantId);
        fixture.SetBackfill(enabled: false, mode: "full", locked: false);
        fixture.SetTenantBackfill(fixture.TenantId, enabled: true, mode: "full");
        fixture.Gateway.FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(CompleteSnapshot(Did));
        fixture.Repository.TryReconcileWithResultAsync(Arg.Any<AtprotoPdsSnapshotApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AtprotoPersistenceApplyResult(true, []));

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(Command([Did]), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Completed);
        await fixture.Repository.Received(1).TryReconcileWithResultAsync(
            Arg.Is<AtprotoPdsSnapshotApplyRequest>(request =>
                request.PresentationTenantIds.SequenceEqual(new[] { fixture.TenantId })),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_FullPolicy_ExcludesDowntimeOnlyTenantsFromHistoricalPresentation()
    {
        var fixture = new Fixture();
        Guid downtimeOnlyTenantId = Guid.CreateVersion7();
        fixture.SetTenants(fixture.TenantId, downtimeOnlyTenantId);
        fixture.SetBackfill(enabled: true, mode: "downtime_only", locked: false);
        fixture.SetTenantBackfill(fixture.TenantId, enabled: true, mode: "full");
        fixture.Gateway.FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(CompleteSnapshot(Did));
        fixture.Repository.TryReconcileWithResultAsync(Arg.Any<AtprotoPdsSnapshotApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AtprotoPersistenceApplyResult(true, []));

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(Command([Did]), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Completed);
        await fixture.Repository.Received(1).TryReconcileWithResultAsync(
            Arg.Is<AtprotoPdsSnapshotApplyRequest>(request =>
                request.PresentationTenantIds.SequenceEqual(new[] { fixture.TenantId })),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_FullPolicy_PresentationAudienceChangeInvalidatesFingerprint()
    {
        var fixture = new Fixture();
        Guid backfillDisabledTenantId = Guid.CreateVersion7();
        fixture.SetTenants(fixture.TenantId, backfillDisabledTenantId);
        fixture.SetBackfill(enabled: false, mode: "full", locked: false);
        fixture.SetTenantBackfill(fixture.TenantId, enabled: true, mode: "full");
        fixture.SetPresentation(enabled: false, locked: false);
        fixture.SetTenantPresentation(fixture.TenantId, enabled: true);
        fixture.SetTenantPresentation(backfillDisabledTenantId, enabled: true);
        fixture.Gateway.FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(CompleteSnapshot(Did));
        fixture.Repository.TryReconcileWithResultAsync(Arg.Any<AtprotoPdsSnapshotApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AtprotoPersistenceApplyResult(true, []));

        AtprotoPdsRecoveryResult first = await fixture.Handler.Handle(Command([Did]), CancellationToken.None);
        fixture.SetTenantPresentation(fixture.TenantId, enabled: false);

        AtprotoPdsRecoveryResult second = await fixture.Handler.Handle(
            Command([Did], first.Fingerprint),
            CancellationToken.None);

        await Assert.That(first.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Completed);
        await Assert.That(second.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Completed);
        await Assert.That(second.Fingerprint).IsNotEqualTo(first.Fingerprint);
        await fixture.Gateway.Received(2).FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>());
        await fixture.Repository.Received(2).TryReconcileWithResultAsync(
            Arg.Any<AtprotoPdsSnapshotApplyRequest>(),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).TryReconcileWithResultAsync(
            Arg.Is<AtprotoPdsSnapshotApplyRequest>(request => request.PresentationTenantIds.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_AnySnapshotFailure_WritesNothingAcrossAllDids()
    {
        var fixture = new Fixture();
        fixture.Gateway.FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(CompleteSnapshot(Did));
        fixture.Gateway.FetchAsync(SecondDid, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(AtprotoPdsSnapshotFetchResult.Failed("invalid_repository"));

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(
            Command([Did, SecondDid]),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.PartialFailure);
        await Assert.That(result.AppliedDids).IsEqualTo(0);
        await Assert.That(result.FailedDids).IsEqualTo(1);
        await fixture.Gateway.Received(1).FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>());
        await fixture.Gateway.Received(1).FetchAsync(SecondDid, Arg.Any<long>(), Arg.Any<CancellationToken>());
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileWithResultAsync(default!, default);
    }

    [Test]
    public async Task Handle_InvalidCompleteSnapshot_WritesNothing()
    {
        var fixture = new Fixture();
        fixture.Gateway.FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(CompleteSnapshot(SecondDid));

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(Command([Did]), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.PartialFailure);
        await Assert.That(result.AppliedDids).IsEqualTo(0);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileWithResultAsync(default!, default);
    }

    [Test]
    public async Task Handle_ProjectionPresenceDoesNotMatchEventCollection_WritesNothing()
    {
        var fixture = new Fixture();
        AtprotoRecord record = Record(Did, AtprotoEventPublicationPlanner.EventCollection);
        var identity = new AtprotoPdsSnapshotIdentity(AtprotoEventPublicationPlanner.EventCollection, record.RecordKey);
        var snapshot = new AtprotoPdsSnapshot(
            Did,
            [identity],
            [new AtprotoPdsSnapshotItem(record, null)]);
        fixture.Gateway.FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(AtprotoPdsSnapshotFetchResult.Complete(snapshot));

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(Command([Did]), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.PartialFailure);
        await Assert.That(result.AppliedDids).IsEqualTo(0);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileWithResultAsync(default!, default);
    }

    [Test]
    public async Task Handle_CancellationDuringFetch_WritesNothing()
    {
        var fixture = new Fixture();
        fixture.Gateway.FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(CompleteSnapshot(Did));
        fixture.Gateway.FetchAsync(SecondDid, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns<Task<AtprotoPdsSnapshotFetchResult>>(_ => throw new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.Handler.Handle(Command([Did, SecondDid]), CancellationToken.None));

        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileWithResultAsync(default!, default);
    }

    [Test]
    public async Task Handle_RepositoryFenceRejection_RejectsWholeBatch()
    {
        var fixture = new Fixture();
        fixture.Gateway.FetchAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(call => CompleteSnapshot(call.ArgAt<string>(0)));
        fixture.Repository.TryReconcileWithResultAsync(Arg.Any<AtprotoPdsSnapshotApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(AtprotoPersistenceApplyResult.Rejected);

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(
            Command([Did, SecondDid]),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.FenceRejected);
        await Assert.That(result.AppliedDids).IsEqualTo(0);
        await fixture.Gateway.Received(2).FetchAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).TryReconcileWithResultAsync(
            Arg.Is<AtprotoPdsSnapshotApplyRequest>(request => request.Snapshots.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_LastSuccessfulFingerprintIsUnchanged_PerformsNoSecondSnapshotIo()
    {
        var fixture = new Fixture();
        fixture.Gateway.FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(CompleteSnapshot(Did));
        fixture.Repository.TryReconcileWithResultAsync(Arg.Any<AtprotoPdsSnapshotApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AtprotoPersistenceApplyResult(true, []));

        AtprotoPdsRecoveryResult first = await fixture.Handler.Handle(Command([Did]), CancellationToken.None);
        AtprotoPdsRecoveryResult second = await fixture.Handler.Handle(
            Command([Did], first.Fingerprint),
            CancellationToken.None);

        await Assert.That(first.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Completed);
        await Assert.That(second.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Unchanged);
        await fixture.Gateway.Received(1).FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).TryReconcileWithResultAsync(
            Arg.Any<AtprotoPdsSnapshotApplyRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResolvePolicy_InstanceLockControlsTenantAudienceAndFingerprint()
    {
        var fixture = new Fixture();
        Guid secondTenantId = Guid.CreateVersion7();
        fixture.SetTenants(fixture.TenantId, secondTenantId);
        fixture.SetBackfill(enabled: false, mode: "downtime_only", locked: false);
        fixture.SetTenantBackfill(fixture.TenantId, enabled: true, mode: "full");

        AtprotoPdsRecoveryPolicy unlocked = await fixture.PolicyResolver.ResolveAsync(CancellationToken.None);

        await Assert.That(unlocked.IsEnabled).IsTrue();
        await Assert.That(unlocked.Mode).IsEqualTo(AtprotoPdsRecoveryMode.Full);
        await Assert.That(unlocked.EffectiveTenantIds).IsEquivalentTo(new[] { fixture.TenantId });

        fixture.SetBackfill(enabled: true, mode: "downtime_only", locked: true);
        AtprotoPdsRecoveryPolicy locked = await fixture.PolicyResolver.ResolveAsync(CancellationToken.None);

        await Assert.That(locked.Mode).IsEqualTo(AtprotoPdsRecoveryMode.DowntimeOnly);
        await Assert.That(locked.EffectiveTenantIds).IsEquivalentTo(new[] { fixture.TenantId, secondTenantId });
        await Assert.That(locked.AudienceFingerprint).IsNotEqualTo(unlocked.AudienceFingerprint);
    }

    [Test]
    public async Task ResolvePolicy_MixedModes_FullAudienceContainsOnlyFullTenantsAndTracksModeChanges()
    {
        var fixture = new Fixture();
        Guid downtimeOnlyTenantId = Guid.CreateVersion7();
        fixture.SetTenants(fixture.TenantId, downtimeOnlyTenantId);
        fixture.SetBackfill(enabled: true, mode: "downtime_only", locked: false);
        fixture.SetTenantBackfill(fixture.TenantId, enabled: true, mode: "full");

        AtprotoPdsRecoveryPolicy mixed = await fixture.PolicyResolver.ResolveAsync(CancellationToken.None);

        await Assert.That(mixed.Mode).IsEqualTo(AtprotoPdsRecoveryMode.Full);
        await Assert.That(mixed.EffectiveTenantIds).IsEquivalentTo(new[] { fixture.TenantId });

        fixture.SetTenantBackfill(downtimeOnlyTenantId, enabled: true, mode: "full");
        AtprotoPdsRecoveryPolicy allFull = await fixture.PolicyResolver.ResolveAsync(CancellationToken.None);

        await Assert.That(allFull.Mode).IsEqualTo(AtprotoPdsRecoveryMode.Full);
        await Assert.That(allFull.EffectiveTenantIds)
            .IsEquivalentTo(new[] { fixture.TenantId, downtimeOnlyTenantId });
        await Assert.That(allFull.AudienceFingerprint).IsNotEqualTo(mixed.AudienceFingerprint);
    }

    private static AtprotoPdsSnapshotFetchResult CompleteSnapshot(string did, bool includeAcceptedItem = false)
    {
        var identity = new AtprotoPdsSnapshotIdentity(
            "community.lexicon.calendar.event",
            "event-1");
        AtprotoRecord record = Record(did);
        IReadOnlyList<AtprotoPdsSnapshotItem> items = includeAcceptedItem
            ? [new AtprotoPdsSnapshotItem(record, Projection(record))]
            : [];
        return AtprotoPdsSnapshotFetchResult.Complete(new AtprotoPdsSnapshot(did, [identity], items));
    }

    private static AtprotoRecord Record(
        string did,
        string collection = AtprotoEventPublicationPlanner.EventCollection) => new()
        {
            Id = Guid.CreateVersion7(),
            Did = did,
            Collection = collection,
            RecordKey = "event-1",
            Direction = AtprotoRecordDirection.Reconciled,
            Provenance = AtprotoRecordProvenance.Jetstream,
            SourceVersion = 1,
            RecordJson = collection == AtprotoEventPublicationPlanner.EventCollection
                ? $$"""
                    {
                      "timezone": "Europe/Brussels",
                      "media": [
                        {
                          "role": "thumbnail",
                          "content": {
                            "$type": "blob",
                            "ref": { "$link": "{{ThumbnailCid}}" },
                            "mimeType": "image/png",
                            "size": 8
                          }
                        }
                      ]
                    }
                    """
                : null,
            UpdatedAt = SnapshotStartedAt
        };

    private static AtprotoEventProjection Projection(AtprotoRecord record) => new()
    {
        AtprotoRecordId = record.Id,
        Name = "Recovered event",
        Description = "Recovered event description.",
        CreatedAt = new DateTimeOffset(SnapshotStartedAt),
        StartsAt = new DateTimeOffset(SnapshotStartedAt).AddDays(1),
        EndsAt = new DateTimeOffset(SnapshotStartedAt).AddDays(1).AddHours(2),
        Mode = "hybrid",
        Status = "scheduled",
        RsvpExpected = true,
        SourceUrl = "https://events.example/recovered",
        SourceVersion = record.SourceVersion,
        MaterializedAt = SnapshotStartedAt
    };

    private static ReconcileAtprotoPdsSnapshotsCommand Command(
        IReadOnlyCollection<string> allowedDids,
        string? lastCompletedFingerprint = null) => new(
        new AtprotoJetstreamClaim(Guid.CreateVersion7(), "https://jetstream.example", Cursor, Guid.CreateVersion7(), 1),
        allowedDids,
        SnapshotStartedAt,
        lastCompletedFingerprint);

    private sealed class Fixture
    {
        private readonly Dictionary<string, SystemSetting> _systemSettings = [];
        private readonly Dictionary<string, List<TenantSetting>> _tenantSettings = [];
        private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
        private readonly ISystemSettingRepository _system = Substitute.For<ISystemSettingRepository>();
        private readonly ITenantSettingRepository _tenant = Substitute.For<ITenantSettingRepository>();

        public Fixture()
        {
            TenantId = Guid.CreateVersion7();
            SetTenants(TenantId);
            SetSystem(GovernanceSettingKeys.Deployment.Mode, "\"MultiTenant\"", locked: false);
            SetBackfill(enabled: true, mode: "full", locked: true);
            SetSystem(GovernanceSettingKeys.Federation.AtprotoEventsEnabled, "true", locked: true);
            _system.GetByKey(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(call => _systemSettings.GetValueOrDefault(call.ArgAt<string>(0)));
            _tenant.GetByKeyAcrossTenants(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(call => _tenantSettings.GetValueOrDefault(call.ArgAt<string>(0), []));
            Gateway = Substitute.For<IAtprotoPdsSnapshotGateway>();
            ThumbnailGateway = Substitute.For<IAtprotoThumbnailBlobGateway>();
            Repository = Substitute.For<IAtprotoPdsSnapshotRepository>();
            ArchiveProbe = Substitute.For<IAtprotoFederationArchiveProbe>();
            // Default to inconclusive: recovery must behave exactly as it did before the archive probe
            // existed unless a test opts into a conclusive answer.
            ArchiveProbe
                .ResolveChangedDidsAsync(
                    Arg.Any<long>(),
                    Arg.Any<IReadOnlyList<string>>(),
                    Arg.Any<CancellationToken>())
                .Returns(AtprotoArchiveChangeScope.Inconclusive);
            PolicyResolver = new AtprotoPdsRecoveryPolicyResolver(_tenants, _system, _tenant);
            var presentationResolver = new AtprotoJetstreamTenantPresentationResolver(_tenants, _system, _tenant);
            Handler = new ReconcileAtprotoPdsSnapshotsCommandHandler(
                PolicyResolver,
                presentationResolver,
                Gateway,
                ThumbnailGateway,
                Repository,
                ArchiveProbe,
                TimeProvider.System);
        }

        public Guid TenantId { get; }
        public IAtprotoPdsSnapshotGateway Gateway { get; }
        public IAtprotoThumbnailBlobGateway ThumbnailGateway { get; }
        public IAtprotoPdsSnapshotRepository Repository { get; }
        public IAtprotoFederationArchiveProbe ArchiveProbe { get; }
        public AtprotoPdsRecoveryPolicyResolver PolicyResolver { get; }
        public ReconcileAtprotoPdsSnapshotsCommandHandler Handler { get; }

        public async Task AssertNoPolicyIoAsync()
        {
            await _system.DidNotReceiveWithAnyArgs().GetByKey(default!, default);
            await _tenant.DidNotReceiveWithAnyArgs().GetByKeyAcrossTenants(default!, default);
            await _tenants.DidNotReceiveWithAnyArgs().GetActiveAsNoTrackingAsync(default);
        }

        public async Task AssertNoPresentationIoAsync(int expectedPolicyResolutions)
        {
            await _system.DidNotReceive().GetByKey(
                GovernanceSettingKeys.Federation.AtprotoEventsEnabled,
                Arg.Any<CancellationToken>());
            await _tenant.DidNotReceive().GetByKeyAcrossTenants(
                GovernanceSettingKeys.Federation.AtprotoEventsEnabled,
                Arg.Any<CancellationToken>());
            await _system.Received(expectedPolicyResolutions).GetByKey(
                GovernanceSettingKeys.Deployment.Mode,
                Arg.Any<CancellationToken>());
            await _tenants.Received(expectedPolicyResolutions)
                .GetActiveAsNoTrackingAsync(Arg.Any<CancellationToken>());
        }

        public void SetTenants(params Guid[] tenantIds) =>
            _tenants.GetActiveAsNoTrackingAsync(Arg.Any<CancellationToken>())
                .Returns(tenantIds.Select(Tenant).ToArray());

        public void SetBackfill(bool enabled, string mode, bool locked)
        {
            SetSystem(
                GovernanceSettingKeys.Federation.AtprotoEventsBackfillEnabled,
                enabled ? "true" : "false",
                locked);
            SetSystem(
                GovernanceSettingKeys.Federation.AtprotoEventsBackfillMode,
                $"\"{mode}\"",
                locked);
        }

        public void SetTenantBackfill(Guid tenantId, bool enabled, string mode)
        {
            SetTenant(
                tenantId,
                GovernanceSettingKeys.Federation.AtprotoEventsBackfillEnabled,
                enabled ? "true" : "false");
            SetTenant(
                tenantId,
                GovernanceSettingKeys.Federation.AtprotoEventsBackfillMode,
                $"\"{mode}\"");
        }

        public void SetPresentation(bool enabled, bool locked) =>
            SetSystem(
                GovernanceSettingKeys.Federation.AtprotoEventsEnabled,
                enabled ? "true" : "false",
                locked);

        public void SetTenantPresentation(Guid tenantId, bool enabled) =>
            SetTenant(
                tenantId,
                GovernanceSettingKeys.Federation.AtprotoEventsEnabled,
                enabled ? "true" : "false");

        private void SetSystem(string key, string value, bool locked) =>
            _systemSettings[key] = new SystemSetting
            {
                Id = Guid.CreateVersion7(),
                SettingKey = key,
                Value = value,
                IsLocked = locked,
                CreatedAt = DateTime.UtcNow
            };

        private void SetTenant(Guid tenantId, string key, string value)
        {
            if (!_tenantSettings.TryGetValue(key, out List<TenantSetting>? settings))
            {
                settings = [];
                _tenantSettings[key] = settings;
            }

            settings.RemoveAll(setting => setting.TenantId == tenantId);
            settings.Add(new TenantSetting
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantId,
                Tenant = null!,
                SettingKey = key,
                Value = value,
                CreatedAt = DateTime.UtcNow
            });
        }

        private static Tenant Tenant(Guid id) => new()
        {
            Id = id,
            FullName = $"Tenant {id}",
            Slug = id.ToString("N"),
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = new TenantStatus
            {
                Id = (int)TenantStatusEnum.Active,
                MasterCode = "ACTIVE",
                FullName = "Active",
                IsActiveState = true
            },
            CreatedAt = DateTime.UtcNow
        };
    }
}
