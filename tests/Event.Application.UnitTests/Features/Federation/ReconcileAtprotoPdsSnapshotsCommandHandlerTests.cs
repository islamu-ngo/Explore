// ABOUTME: Verifies governed ATProto PDS recovery orchestration without coupling Application to CarpaNet.
// ABOUTME: Proves disabled and downtime-only modes avoid PDS I/O while Full reconciliation is atomic and fenced.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Handlers.Commands;
using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Features.Federation.Atproto.Requests.Commands;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Application.Features.Federation.Atproto.Validators;
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

    [Test]
    public async Task Handle_DisabledPolicy_PerformsNoSnapshotIo()
    {
        var fixture = new Fixture();
        fixture.SetBackfill(enabled: false, mode: "full", locked: true);

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(Command([Did]), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Disabled);
        await fixture.Gateway.DidNotReceiveWithAnyArgs().FetchAsync(default!, default, default);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileAsync(default!, default);
    }

    [Test]
    public async Task Handle_DowntimeOnlyPolicy_UsesNativeJetstreamReplayWithoutPdsIo()
    {
        var fixture = new Fixture();
        fixture.SetBackfill(enabled: true, mode: "downtime_only", locked: true);

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(Command([Did]), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.DowntimeOnly);
        await fixture.Gateway.DidNotReceiveWithAnyArgs().FetchAsync(default!, default, default);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileAsync(default!, default);
    }

    [Test]
    public async Task Handle_FullPolicyWithGlobalDidScope_FailsClosed()
    {
        var fixture = new Fixture();

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(Command([]), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.ScopeRejected);
        await fixture.Gateway.DidNotReceiveWithAnyArgs().FetchAsync(default!, default, default);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileAsync(default!, default);
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
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileAsync(default!, default);
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
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileAsync(default!, default);
    }

    [Test]
    public async Task Handle_MalformedDid_FailsValidation()
    {
        var fixture = new Fixture();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            fixture.Handler.Handle(Command(["not-a-did"]), CancellationToken.None));

        await fixture.AssertNoPolicyIoAsync();
        await fixture.Gateway.DidNotReceiveWithAnyArgs().FetchAsync(default!, default, default);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileAsync(default!, default);
    }

    [Test]
    public async Task Handle_FullPolicy_NormalizesDidsAndReconcilesOneAtomicBatch()
    {
        var fixture = new Fixture();
        fixture.Gateway.FetchAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(call => CompleteSnapshot(call.ArgAt<string>(0), includeAcceptedItem: call.ArgAt<string>(0) == Did));
        fixture.Repository.TryReconcileAsync(Arg.Any<AtprotoPdsSnapshotApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(true);

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(
            Command([SecondDid, Did, Did]),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Completed);
        await Assert.That(result.AppliedDids).IsEqualTo(2);
        await Assert.That(result.FailedDids).IsEqualTo(0);
        await fixture.Gateway.Received(1).FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>());
        await fixture.Gateway.Received(1).FetchAsync(SecondDid, Arg.Any<long>(), Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).TryReconcileAsync(
            Arg.Is<AtprotoPdsSnapshotApplyRequest>(request =>
                request.ScannedDids.SequenceEqual(new[] { Did, SecondDid })
                && request.Snapshots.Select(snapshot => snapshot.Did).SequenceEqual(new[] { Did, SecondDid })
                && request.Snapshots[0].PresentIdentities.Count == 1
                && request.Snapshots[0].Items.Count == 1
                && request.PresentationTenantIds.SequenceEqual(new[] { fixture.TenantId })),
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
        fixture.Repository.TryReconcileAsync(Arg.Any<AtprotoPdsSnapshotApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(true);

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(Command([Did]), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Completed);
        await fixture.Repository.Received(1).TryReconcileAsync(
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
        fixture.Repository.TryReconcileAsync(Arg.Any<AtprotoPdsSnapshotApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(true);

        AtprotoPdsRecoveryResult first = await fixture.Handler.Handle(Command([Did]), CancellationToken.None);
        fixture.SetTenantPresentation(fixture.TenantId, enabled: false);

        AtprotoPdsRecoveryResult second = await fixture.Handler.Handle(
            Command([Did], first.Fingerprint),
            CancellationToken.None);

        await Assert.That(first.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Completed);
        await Assert.That(second.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Completed);
        await Assert.That(second.Fingerprint).IsNotEqualTo(first.Fingerprint);
        await fixture.Gateway.Received(2).FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>());
        await fixture.Repository.Received(2).TryReconcileAsync(
            Arg.Any<AtprotoPdsSnapshotApplyRequest>(),
            Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).TryReconcileAsync(
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
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileAsync(default!, default);
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
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileAsync(default!, default);
    }

    [Test]
    [Arguments(AtprotoEventPublicationPlanner.EventCollection, false)]
    [Arguments(AtprotoEventPublicationPlanner.RsvpCollection, true)]
    public async Task Handle_ProjectionPresenceDoesNotMatchCollection_WritesNothing(
        string collection,
        bool includeProjection)
    {
        var fixture = new Fixture();
        AtprotoRecord record = Record(Did, collection);
        var identity = new AtprotoPdsSnapshotIdentity(collection, record.RecordKey);
        var snapshot = new AtprotoPdsSnapshot(
            Did,
            [identity],
            [new AtprotoPdsSnapshotItem(record, includeProjection ? Projection(record) : null)]);
        fixture.Gateway.FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(AtprotoPdsSnapshotFetchResult.Complete(snapshot));

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(Command([Did]), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.PartialFailure);
        await Assert.That(result.AppliedDids).IsEqualTo(0);
        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileAsync(default!, default);
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

        await fixture.Repository.DidNotReceiveWithAnyArgs().TryReconcileAsync(default!, default);
    }

    [Test]
    public async Task Handle_RepositoryFenceRejection_RejectsWholeBatch()
    {
        var fixture = new Fixture();
        fixture.Gateway.FetchAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(call => CompleteSnapshot(call.ArgAt<string>(0)));
        fixture.Repository.TryReconcileAsync(Arg.Any<AtprotoPdsSnapshotApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(false);

        AtprotoPdsRecoveryResult result = await fixture.Handler.Handle(
            Command([Did, SecondDid]),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.FenceRejected);
        await Assert.That(result.AppliedDids).IsEqualTo(0);
        await fixture.Gateway.Received(2).FetchAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).TryReconcileAsync(
            Arg.Is<AtprotoPdsSnapshotApplyRequest>(request => request.Snapshots.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_LastSuccessfulFingerprintIsUnchanged_PerformsNoSecondSnapshotIo()
    {
        var fixture = new Fixture();
        fixture.Gateway.FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(CompleteSnapshot(Did));
        fixture.Repository.TryReconcileAsync(Arg.Any<AtprotoPdsSnapshotApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(true);

        AtprotoPdsRecoveryResult first = await fixture.Handler.Handle(Command([Did]), CancellationToken.None);
        AtprotoPdsRecoveryResult second = await fixture.Handler.Handle(
            Command([Did], first.Fingerprint),
            CancellationToken.None);

        await Assert.That(first.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Completed);
        await Assert.That(second.Outcome).IsEqualTo(AtprotoPdsRecoveryOutcome.Unchanged);
        await fixture.Gateway.Received(1).FetchAsync(Did, Arg.Any<long>(), Arg.Any<CancellationToken>());
        await fixture.Repository.Received(1).TryReconcileAsync(
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
    public async Task ResolvePolicy_CurrentMixedModePolicyIncludesAllEnabledTenantsInFullAudience()
    {
        var fixture = new Fixture();
        Guid downtimeOnlyTenantId = Guid.CreateVersion7();
        fixture.SetTenants(fixture.TenantId, downtimeOnlyTenantId);
        fixture.SetBackfill(enabled: true, mode: "downtime_only", locked: false);
        fixture.SetTenantBackfill(fixture.TenantId, enabled: true, mode: "full");

        AtprotoPdsRecoveryPolicy policy = await fixture.PolicyResolver.ResolveAsync(CancellationToken.None);

        await Assert.That(policy.Mode).IsEqualTo(AtprotoPdsRecoveryMode.Full);
        await Assert.That(policy.EffectiveTenantIds)
            .IsEquivalentTo(new[] { fixture.TenantId, downtimeOnlyTenantId });
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
        UpdatedAt = SnapshotStartedAt
    };

    private static AtprotoEventProjection Projection(AtprotoRecord record) => new()
    {
        AtprotoRecordId = record.Id,
        Name = "Recovered event",
        SourceVersion = record.SourceVersion,
        MaterializedAt = SnapshotStartedAt
    };

    private static ReconcileAtprotoPdsSnapshotsCommand Command(
        IReadOnlyCollection<string> allowedDids,
        string? lastCompletedFingerprint = null) => new(
        new AtprotoJetstreamClaim(Guid.CreateVersion7(), "https://jetstream.example", 42, Guid.CreateVersion7(), 1),
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
            Repository = Substitute.For<IAtprotoPdsSnapshotRepository>();
            PolicyResolver = new AtprotoPdsRecoveryPolicyResolver(_tenants, _system, _tenant);
            var presentationResolver = new AtprotoJetstreamTenantPresentationResolver(_tenants, _system, _tenant);
            Handler = new ReconcileAtprotoPdsSnapshotsCommandHandler(
                PolicyResolver,
                presentationResolver,
                Gateway,
                Repository,
                TimeProvider.System);
        }

        public Guid TenantId { get; }
        public IAtprotoPdsSnapshotGateway Gateway { get; }
        public IAtprotoPdsSnapshotRepository Repository { get; }
        public AtprotoPdsRecoveryPolicyResolver PolicyResolver { get; }
        public ReconcileAtprotoPdsSnapshotsCommandHandler Handler { get; }

        public async Task AssertNoPolicyIoAsync()
        {
            await _system.DidNotReceiveWithAnyArgs().GetByKey(default!, default);
            await _tenant.DidNotReceiveWithAnyArgs().GetByKeyAcrossTenants(default!, default);
            await _tenants.DidNotReceiveWithAnyArgs().GetActiveAsNoTrackingAsync(default);
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
