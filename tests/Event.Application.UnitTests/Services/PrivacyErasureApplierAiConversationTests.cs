// ABOUTME: Unit tests for the AI conversation hard-delete slice of privacy erasure.
// ABOUTME: Verifies the transaction path deletes only the target graph, stays idempotent, and avoids provider work when none exists.

using Explore.Application.Configuration;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Ai;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

[Category("AiConversation")]
public sealed class PrivacyErasureApplierAiConversationTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IGenericRepository<UserPii, Guid> _userPiiRepository = Substitute.For<IGenericRepository<UserPii, Guid>>();
    private readonly IUserAuthenticationTokenRepository _tokenRepository = Substitute.For<IUserAuthenticationTokenRepository>();
    private readonly IUserLocationPrivacyErasureRepository _locationErasureRepository = Substitute.For<IUserLocationPrivacyErasureRepository>();
    private readonly IUserPrivacyErasureRepository _privacyErasureRepository = Substitute.For<IUserPrivacyErasureRepository>();
    private readonly IAiConversationRepository _aiConversationRepository = Substitute.For<IAiConversationRepository>();
    private readonly IPrivacyErasureProviderWorkRepository _providerWorkRepository = Substitute.For<IPrivacyErasureProviderWorkRepository>();
    private readonly IPrivacyErasureProviderLocatorProtector _providerLocatorProtector = Substitute.For<IPrivacyErasureProviderLocatorProtector>();
    private readonly IPrivacyErasureReplayCheckpointRepository _checkpointRepository = Substitute.For<IPrivacyErasureReplayCheckpointRepository>();
    private readonly IPrivacyErasureLedgerRepository _ledgerRepository = Substitute.For<IPrivacyErasureLedgerRepository>();
    private readonly IPrivacyErasureStateRepository _stateRepository = Substitute.For<IPrivacyErasureStateRepository>();
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();
    private readonly ILogger<PrivacyErasureApplier> _logger = Substitute.For<ILogger<PrivacyErasureApplier>>();
    private readonly IOptions<PrivacyErasureOptions> _options = Options.Create(new PrivacyErasureOptions());
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    public PrivacyErasureApplierAiConversationTests()
    {
        _userRepository.GetById(Arg.Any<Guid>()).Returns(Task.FromResult<User?>(null));
        _userPiiRepository.GetById(Arg.Any<Guid>()).Returns(Task.FromResult<UserPii?>(null));
        _tokenRepository.GetByUser(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new List<UserAuthenticationToken>()));
        _locationErasureRepository.GetOwnedPrivateHomesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Location>>(Array.Empty<Location>()));
        _locationErasureRepository.GetEventLocationsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<EventLocation>>(Array.Empty<EventLocation>()));
        _locationErasureRepository.GetUserActorsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Actor>>(Array.Empty<Actor>()));
        _locationErasureRepository.SaveChangesAsync(Arg.Any<IReadOnlyCollection<EventLocationDisclosureAudit>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _privacyErasureRepository.GetProviderCandidatesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<PrivacyErasureProviderCandidate>>(Array.Empty<PrivacyErasureProviderCandidate>()));
        _privacyErasureRepository.EraseProviderBackedLocalUserMetadataAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _privacyErasureRepository.AnonymizeRetainedAuditEvidenceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _privacyErasureRepository.EraseRegistrationAndLocalNotificationsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _privacyErasureRepository.EraseMembershipsAndPreferencesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _aiConversationRepository.HardDeleteUserConversationGraphAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(1);
        _providerWorkRepository.AddMissingAsync(Arg.Any<PrivacyErasureProviderWork[]>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _checkpointRepository.GetLatestAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PrivacyErasureReplayCheckpoint?>(null));
        _stateRepository.HasCoverageAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _stateRepository.AddCoverageAsync(Arg.Any<PrivacyErasurePolicyCoverage>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _stateRepository.GetByIntentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PrivacyErasureSaga?>(null));
        _stateRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _outboxRepository.CreateRange(Arg.Any<IReadOnlyCollection<OutboxMessage>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OutboxMessage>>(Array.Empty<OutboxMessage>()));
    }

    [Test]
    public async Task ApplyInCurrentTransactionAsync_DeletesConversationGraphAndDoesNotMaterializeProviderWorkWhenNoneExist()
    {
        var intent = CreateIntent();
        _ledgerRepository.AppendAsync(intent, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(intent));
        var prepared = CreatePreparedErasure();

        var result = await CreateApplier().ApplyInCurrentTransactionAsync(intent, prepared, CancellationToken.None);

        await Assert.That(result.UserId).IsEqualTo(intent.SubjectId);
        Received.InOrder(() =>
        {
            _privacyErasureRepository.EraseProviderBackedLocalUserMetadataAsync(intent.SubjectId, Arg.Any<CancellationToken>());
            _privacyErasureRepository.AnonymizeRetainedAuditEvidenceAsync(intent.SubjectId, Arg.Any<CancellationToken>());
            _privacyErasureRepository.EraseRegistrationAndLocalNotificationsAsync(intent.SubjectId, Arg.Any<CancellationToken>());
            _privacyErasureRepository.EraseMembershipsAndPreferencesAsync(intent.SubjectId, Arg.Any<CancellationToken>());
            _aiConversationRepository.HardDeleteUserConversationGraphAsync(intent.SubjectId, Arg.Any<CancellationToken>());
        });
        await _providerWorkRepository.Received(1).AddMissingAsync(
            Arg.Is<PrivacyErasureProviderWork[]>(work => work != null && work.Length == 0),
            Arg.Any<CancellationToken>());
        _providerLocatorProtector.DidNotReceive().Protect(Arg.Any<string>(), Arg.Any<TimeSpan>());
        await _privacyErasureRepository.Received(1).GetProviderCandidatesAsync(intent.SubjectId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyInCurrentTransactionAsync_WhenCheckpointAndCoverageMatch_SkipsConversationDeletionAndProviderWork()
    {
        var intent = CreateIntent();
        var current = PrivacyErasureReplayCheckpoint.Start(intent, intent.RecordedAtUtc, Guid.CreateVersion7());
        _checkpointRepository.GetLatestAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<PrivacyErasureReplayCheckpoint?>(current));
        _stateRepository.HasCoverageAsync(intent.IntentId, _options.Value.CurrentPolicyVersion, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateApplier().ApplyInCurrentTransactionAsync(intent, CreatePreparedErasure(), CancellationToken.None);

        await Assert.That(result).IsEqualTo(PrivacyErasureApplier.AppliedErasure.None);
        _aiConversationRepository.DidNotReceive().HardDeleteUserConversationGraphAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _privacyErasureRepository.DidNotReceive().EraseProviderBackedLocalUserMetadataAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _privacyErasureRepository.DidNotReceive().AnonymizeRetainedAuditEvidenceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _privacyErasureRepository.DidNotReceive().EraseRegistrationAndLocalNotificationsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _privacyErasureRepository.DidNotReceive().EraseMembershipsAndPreferencesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _privacyErasureRepository.DidNotReceive().GetProviderCandidatesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _providerWorkRepository.DidNotReceive().AddMissingAsync(Arg.Any<PrivacyErasureProviderWork[]>(), Arg.Any<CancellationToken>());
        _providerLocatorProtector.DidNotReceive().Protect(Arg.Any<string>(), Arg.Any<TimeSpan>());
        await _ledgerRepository.DidNotReceive().AppendAsync(Arg.Any<PrivacyErasureIntent>(), Arg.Any<CancellationToken>());
    }

    private PrivacyErasureApplier CreateApplier()
        => new(
            _userRepository,
            _userPiiRepository,
            _tokenRepository,
            _locationErasureRepository,
            _privacyErasureRepository,
            _aiConversationRepository,
            _providerWorkRepository,
            _providerLocatorProtector,
            _checkpointRepository,
            _ledgerRepository,
            _stateRepository,
            _outboxRepository,
            null!,
            _timeProvider,
            _logger,
            _options);

    private static PrivacyErasureIntent CreateIntent()
    {
        var now = DateTime.UtcNow;
        return PrivacyErasureIntent.Record(
            Guid.CreateVersion7(),
            1,
            PrivacyErasureSubjectKind.User,
            Guid.CreateVersion7(),
            PrivacyErasureReasonCode.AccountDeletion,
            1,
            now.AddMinutes(-1),
            now);
    }

    private static PrivacyErasureApplier.PreparedErasure CreatePreparedErasure()
    {
        var now = DateTime.UtcNow;
        return new PrivacyErasureApplier.PreparedErasure(
            new Dictionary<Guid, Guid>(),
            new Dictionary<Guid, Guid>(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            now);
    }
}
