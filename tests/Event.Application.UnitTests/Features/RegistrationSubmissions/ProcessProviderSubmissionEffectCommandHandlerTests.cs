// ABOUTME: Verifies provider callback effect processing preserves Phase 8 sync-mode boundaries.
// ABOUTME: Uses fakes to prove NONE and COMPLETION_ONLY do not persist canonical answers.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Services.Registration;
using Explore.Application.Features.RegistrationSubmissions.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using MediatR;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.RegistrationSubmissions;

public sealed class ProcessProviderSubmissionEffectCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task NoneSyncMode_CompletesWithoutPersistingSubmissionAnswersOrFulfillment()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.NONE);
        var repositories = CreateRepositories(scope);
        var handler = CreateHandler(scope, repositories);

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.Completed);
        await repositories.Submissions.DidNotReceive().PersistEvidenceOnlyAsync(Arg.Any<RegistrationSubmission>(), Arg.Any<CancellationToken>());
        await repositories.Submissions.DidNotReceive().PersistAcceptedWithNormalizationAsync(
            Arg.Any<RegistrationAttempt>(), Arg.Any<RegistrationSubmission>(), Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<RegistrationAnswer>>(), Arg.Any<IReadOnlyCollection<RegistrationConsentRecord>>(),
            Arg.Any<IReadOnlyCollection<RegistrationSubmissionIssue>>(), Arg.Any<IReadOnlyCollection<RegistrationRequirementFulfillment>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CompletionOnlySyncMode_PersistsFulfillmentAndZeroAnswers()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.COMPLETION_ONLY);
        var repositories = CreateRepositories(scope);
        IReadOnlyCollection<RegistrationAnswer>? answers = null;
        IReadOnlyCollection<RegistrationRequirementFulfillment>? fulfillments = null;
        repositories.Submissions.PersistAcceptedWithNormalizationAsync(
                Arg.Any<RegistrationAttempt>(), Arg.Any<RegistrationSubmission>(), Arg.Any<Guid>(),
                Arg.Do<IReadOnlyCollection<RegistrationAnswer>>(value => answers = value),
                Arg.Any<IReadOnlyCollection<RegistrationConsentRecord>>(),
                Arg.Any<IReadOnlyCollection<RegistrationSubmissionIssue>>(),
                Arg.Do<IReadOnlyCollection<RegistrationRequirementFulfillment>>(value => fulfillments = value),
                Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new RegistrationSubmissionPersistenceResult(
                RegistrationSubmissionPersistenceOutcome.Inserted, call.ArgAt<RegistrationSubmission>(1))));
        var handler = CreateHandler(scope, repositories);

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.Completed);
        await Assert.That(answers).IsNotNull();
        await Assert.That(answers!).IsEmpty();
        await Assert.That(fulfillments).IsNotNull();
        await Assert.That(fulfillments!).HasSingleItem();
    }

    [Test]
    public async Task FullCanonicalSyncMode_WithCompletionOnlyTrust_ParksWithoutCanonicalAnswers()
    {
        ProviderScope scope = CreateScope(
            RegistrationAnswerSyncModeEnum.FULL_CANONICAL,
            RegistrationProviderTrustLevelEnum.CompletionOnly);
        var repositories = CreateRepositories(scope);
        var handler = CreateHandler(scope, repositories);

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.NeedsReconciliation);
        await Assert.That(result.Code).IsEqualTo("BELOW_MINIMUM_TRUST");
        await repositories.Submissions.DidNotReceive().PersistAcceptedWithNormalizationAsync(
            Arg.Any<RegistrationAttempt>(), Arg.Any<RegistrationSubmission>(), Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<RegistrationAnswer>>(), Arg.Any<IReadOnlyCollection<RegistrationConsentRecord>>(),
            Arg.Any<IReadOnlyCollection<RegistrationSubmissionIssue>>(), Arg.Any<IReadOnlyCollection<RegistrationRequirementFulfillment>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(RegistrationAnswerSyncModeEnum.COMPLETION_ONLY, RegistrationProviderTrustLevelEnum.Untrusted)]
    [Arguments(RegistrationAnswerSyncModeEnum.SELECTED_FIELDS, RegistrationProviderTrustLevelEnum.CompletionOnly)]
    [Arguments(RegistrationAnswerSyncModeEnum.FULL_CANONICAL, RegistrationProviderTrustLevelEnum.SelectedFields)]
    public async Task TrustBelowSyncMode_ParksWithBoundedIssueAndNoAutoFinalization(
        RegistrationAnswerSyncModeEnum syncMode,
        RegistrationProviderTrustLevelEnum trustLevel)
    {
        ProviderScope scope = CreateScope(syncMode, trustLevel);
        var repositories = CreateRepositories(scope);
        RegistrationSubmission? parked = null;
        IReadOnlyCollection<RegistrationSubmissionIssue>? issues = null;
        repositories.Submissions.PersistEvidenceOnlyAsync(Arg.Do<RegistrationSubmission>(value => parked = value), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new RegistrationSubmissionPersistenceResult(
                RegistrationSubmissionPersistenceOutcome.Inserted,
                call.ArgAt<RegistrationSubmission>(0))));
        repositories.Submissions.PersistNormalizationAsync(
                Arg.Any<IReadOnlyCollection<RegistrationAnswer>>(),
                Arg.Any<IReadOnlyCollection<RegistrationConsentRecord>>(),
                Arg.Do<IReadOnlyCollection<RegistrationSubmissionIssue>>(value => issues = value),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(scope, repositories);

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.NeedsReconciliation);
        await Assert.That(result.Code).IsEqualTo("BELOW_MINIMUM_TRUST");
        await Assert.That(parked).IsNotNull();
        await Assert.That(parked!.IsFinalizable).IsFalse();
        await Assert.That(parked.FinalizedAt).IsNull();
        await Assert.That(issues).IsNotNull();
        await Assert.That(issues!.Single().Code).IsEqualTo("BELOW_MINIMUM_TRUST");
        await repositories.Submissions.DidNotReceive().PersistAcceptedWithNormalizationAsync(
            Arg.Any<RegistrationAttempt>(), Arg.Any<RegistrationSubmission>(), Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<RegistrationAnswer>>(), Arg.Any<IReadOnlyCollection<RegistrationConsentRecord>>(),
            Arg.Any<IReadOnlyCollection<RegistrationSubmissionIssue>>(), Arg.Any<IReadOnlyCollection<RegistrationRequirementFulfillment>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SelectedFieldsSyncMode_PersistsOnlyMappedProviderAnswers()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.SELECTED_FIELDS, addFieldMapping: true);
        var repositories = CreateRepositories(scope);
        IReadOnlyCollection<RegistrationAnswer>? answers = null;
        repositories.Submissions.PersistAcceptedWithNormalizationAsync(
                Arg.Any<RegistrationAttempt>(), Arg.Any<RegistrationSubmission>(), Arg.Any<Guid>(),
                Arg.Do<IReadOnlyCollection<RegistrationAnswer>>(value => answers = value),
                Arg.Any<IReadOnlyCollection<RegistrationConsentRecord>>(),
                Arg.Any<IReadOnlyCollection<RegistrationSubmissionIssue>>(),
                Arg.Any<IReadOnlyCollection<RegistrationRequirementFulfillment>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new RegistrationSubmissionPersistenceResult(
                RegistrationSubmissionPersistenceOutcome.Inserted, call.ArgAt<RegistrationSubmission>(1))));
        var handler = CreateHandler(scope, repositories);

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.Completed);
        await Assert.That(answers).IsNotNull();
        await Assert.That(answers!).HasSingleItem();
        await Assert.That(answers!.Single().RegistrationFormFieldId).IsEqualTo(scope.FieldId);
    }

    [Test]
    public async Task RawProviderCallback_FetchesSubmissionByReceiptIdAndPersistsMappedAnswers()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.SELECTED_FIELDS, addFieldMapping: true);
        var repositories = CreateRepositories(scope);
        byte[] rawPayload = Encoding.UTF8.GetBytes("{\"type\":\"responseFinished\",\"data\":{\"id\":\"submission-1\"}}");
        IReadOnlyCollection<RegistrationAnswer>? answers = null;
        repositories.Submissions.PersistAcceptedWithNormalizationAsync(
                Arg.Any<RegistrationAttempt>(), Arg.Any<RegistrationSubmission>(), Arg.Any<Guid>(),
                Arg.Do<IReadOnlyCollection<RegistrationAnswer>>(value => answers = value),
                Arg.Any<IReadOnlyCollection<RegistrationConsentRecord>>(),
                Arg.Any<IReadOnlyCollection<RegistrationSubmissionIssue>>(),
                Arg.Any<IReadOnlyCollection<RegistrationRequirementFulfillment>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new RegistrationSubmissionPersistenceResult(
                RegistrationSubmissionPersistenceOutcome.Inserted, call.ArgAt<RegistrationSubmission>(1))));
        var handler = CreateHandler(
            scope,
            repositories,
            new StaticReceiptProtector(scope, HashSha256(rawPayload)),
            new ReaderRegistry(scope));

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope, rawPayload), CancellationToken.None);

        await Assert.That(result.Code).IsEqualTo("inserted");
        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.Completed);
        await Assert.That(answers).IsNotNull();
        await Assert.That(answers!).HasSingleItem();
        await Assert.That(answers!.Single().RegistrationFormFieldId).IsEqualTo(scope.FieldId);
    }

    [Test]
    public async Task RawProviderCallback_TransientReadFailureReturnsRetryableWithoutParkingIssue()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.SELECTED_FIELDS, addFieldMapping: true);
        var repositories = CreateRepositories(scope);
        byte[] rawPayload = Encoding.UTF8.GetBytes("{\"type\":\"responseFinished\",\"data\":{\"id\":\"submission-1\"}}");
        var handler = CreateHandler(
            scope,
            repositories,
            new StaticReceiptProtector(scope, HashSha256(rawPayload)),
            new FailingReaderRegistry(new HttpRequestException("provider unavailable")));

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope, rawPayload), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.Retryable);
        await Assert.That(result.Code).IsEqualTo("SUBMISSION_FETCH_FAILED");
        await repositories.Submissions.DidNotReceive().PersistEvidenceOnlyAsync(Arg.Any<RegistrationSubmission>(), Arg.Any<CancellationToken>());
        await repositories.Submissions.DidNotReceive().PersistNormalizationAsync(
            Arg.Any<IReadOnlyCollection<RegistrationAnswer>>(),
            Arg.Any<IReadOnlyCollection<RegistrationConsentRecord>>(),
            Arg.Any<IReadOnlyCollection<RegistrationSubmissionIssue>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GoogleDelegatedFetchedSubmission_WithValidTokenAndCapabilityPolicy_Completes()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.COMPLETION_ONLY, providerCode: "GOOGLE_FORMS");
        var repositories = CreateRepositories(scope, IdentityAccessModeEnum.CapabilityTokenAllowed);
        IGuestCapabilityTokenService capabilities = Substitute.For<IGuestCapabilityTokenService>();
        capabilities.Matches("raw-token", scope.Attempt.CapabilityTokenHash).Returns(true);
        var handler = CreateHandler(scope, repositories, new StaticReceiptProtector(scope, HashSha256(Encoding.UTF8.GetBytes("{}"))), new GoogleDelegatedReaderRegistry(scope, "raw-token"), capabilities);

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope, Encoding.UTF8.GetBytes("{}")), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.Completed);
    }

    [Test]
    public async Task GoogleDelegatedFetchedSubmission_WithAccountRequiredPolicy_ParksWithoutConsumingAttempt()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.COMPLETION_ONLY, providerCode: "GOOGLE_FORMS");
        var repositories = CreateRepositories(scope, IdentityAccessModeEnum.AccountRequired);
        IGuestCapabilityTokenService capabilities = Substitute.For<IGuestCapabilityTokenService>();
        capabilities.Matches("raw-token", scope.Attempt.CapabilityTokenHash).Returns(true);
        var handler = CreateHandler(scope, repositories, new StaticReceiptProtector(scope, HashSha256(Encoding.UTF8.GetBytes("{}"))), new GoogleDelegatedReaderRegistry(scope, "raw-token"), capabilities);

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope, Encoding.UTF8.GetBytes("{}")), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.NeedsReconciliation);
        await Assert.That(result.Code).IsEqualTo("TOKEN_ONLY_IDENTITY_BELOW_POLICY");
        await Assert.That(scope.Attempt.StatusId).IsEqualTo((int)RegistrationAttemptStatusEnum.Active);
        await repositories.Submissions.DidNotReceive().PersistAcceptedWithNormalizationAsync(
            Arg.Any<RegistrationAttempt>(), Arg.Any<RegistrationSubmission>(), Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<RegistrationAnswer>>(), Arg.Any<IReadOnlyCollection<RegistrationConsentRecord>>(),
            Arg.Any<IReadOnlyCollection<RegistrationSubmissionIssue>>(), Arg.Any<IReadOnlyCollection<RegistrationRequirementFulfillment>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GoogleDelegatedFetchedSubmission_WithMismatchedToken_ParksBeforeConsumingAttempt()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.COMPLETION_ONLY, providerCode: "GOOGLE_FORMS");
        var repositories = CreateRepositories(scope, IdentityAccessModeEnum.CapabilityTokenAllowed);
        IGuestCapabilityTokenService capabilities = Substitute.For<IGuestCapabilityTokenService>();
        capabilities.Matches("wrong-token", scope.Attempt.CapabilityTokenHash).Returns(false);
        var handler = CreateHandler(scope, repositories, new StaticReceiptProtector(scope, HashSha256(Encoding.UTF8.GetBytes("{}"))), new GoogleDelegatedReaderRegistry(scope, "wrong-token"), capabilities);

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope, Encoding.UTF8.GetBytes("{}")), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.NeedsReconciliation);
        await Assert.That(result.Code).IsEqualTo("PROVIDER_CORRELATION_INVALID");
        await Assert.That(scope.Attempt.StatusId).IsEqualTo((int)RegistrationAttemptStatusEnum.Active);
    }

    [Test]
    public async Task GoogleDelegatedFetchedSubmission_WithExpiredOrConsumedAttempt_ParksWithoutFinalizing()
    {
        foreach (bool consumed in new[] { false, true })
        {
            ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.COMPLETION_ONLY, providerCode: "GOOGLE_FORMS");
            if (consumed)
            {
                scope.Attempt.Consume(Now.AddMinutes(1));
            }
            else
            {
                SetPrivateProperty(scope.Attempt, nameof(RegistrationAttempt.ExpiresAt), Now.AddSeconds(-1));
            }

            var repositories = CreateRepositories(scope, IdentityAccessModeEnum.CapabilityTokenAllowed);
            IGuestCapabilityTokenService capabilities = Substitute.For<IGuestCapabilityTokenService>();
            capabilities.Matches("raw-token", scope.Attempt.CapabilityTokenHash).Returns(true);
            var handler = CreateHandler(scope, repositories, new StaticReceiptProtector(scope, HashSha256(Encoding.UTF8.GetBytes("{}"))), new GoogleDelegatedReaderRegistry(scope, "raw-token"), capabilities);

            ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope, Encoding.UTF8.GetBytes("{}")), CancellationToken.None);

            await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.NeedsReconciliation);
            await Assert.That(result.Code).IsEqualTo("STALE_OR_OUT_OF_ORDER");
        }
    }

    [Test]
    public async Task GoogleDelegatedFetchedSubmission_WithDriveFileAnswer_ParksExplicitUnsupportedIssue()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.COMPLETION_ONLY, providerCode: "GOOGLE_FORMS");
        var repositories = CreateRepositories(scope, IdentityAccessModeEnum.CapabilityTokenAllowed);
        var handler = CreateHandler(scope, repositories, new StaticReceiptProtector(scope, HashSha256(Encoding.UTF8.GetBytes("{}"))), new UnsupportedDriveReaderRegistry());

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope, Encoding.UTF8.GetBytes("{}")), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.NeedsReconciliation);
        await Assert.That(result.Code).IsEqualTo("PROVIDER_FILE_UPLOAD_UNSUPPORTED");
    }

    [Test]
    public async Task MirrorOnlySyncMode_WithoutSink_ParksAndDoesNotFinalize()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.MIRROR_ONLY);
        var repositories = CreateRepositories(scope);
        RegistrationSubmission? parked = null;
        repositories.Submissions.PersistEvidenceOnlyAsync(Arg.Do<RegistrationSubmission>(value => parked = value), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new RegistrationSubmissionPersistenceResult(
                RegistrationSubmissionPersistenceOutcome.Inserted,
                call.ArgAt<RegistrationSubmission>(0))));
        var handler = CreateHandler(scope, repositories);

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.NeedsReconciliation);
        await Assert.That(result.Code).IsEqualTo("MIRROR_SINK_UNSUPPORTED");
        await Assert.That(parked).IsNotNull();
        await Assert.That(parked!.IsFinalizable).IsFalse();
        await repositories.Submissions.DidNotReceive().PersistAcceptedWithNormalizationAsync(
            Arg.Any<RegistrationAttempt>(), Arg.Any<RegistrationSubmission>(), Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<RegistrationAnswer>>(), Arg.Any<IReadOnlyCollection<RegistrationConsentRecord>>(),
            Arg.Any<IReadOnlyCollection<RegistrationSubmissionIssue>>(), Arg.Any<IReadOnlyCollection<RegistrationRequirementFulfillment>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task BlockingDrift_ParksAsNeedsReconciliationWithoutAutoFinalization()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.COMPLETION_ONLY);
        SetPrivateProperty(scope.Binding, nameof(RegistrationProviderBinding.DriftClassId), (int)RegistrationProviderDriftClassEnum.MappingRequired);
        var repositories = CreateRepositories(scope);
        RegistrationSubmission? parked = null;
        repositories.Submissions.PersistEvidenceOnlyAsync(Arg.Do<RegistrationSubmission>(value => parked = value), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new RegistrationSubmissionPersistenceResult(
                RegistrationSubmissionPersistenceOutcome.Inserted,
                call.ArgAt<RegistrationSubmission>(0))));
        var handler = CreateHandler(scope, repositories);

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.NeedsReconciliation);
        await Assert.That(result.Code).IsEqualTo("BLOCKING_DRIFT");
        await Assert.That(parked).IsNotNull();
        await Assert.That(parked!.IsFinalizable).IsFalse();
    }

    [Test]
    public async Task SupersededAttemptCallback_IsRetainedAsEvidenceOnlyAndDoesNotFinalize()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.COMPLETION_ONLY);
        scope.Attempt.Supersede(Guid.CreateVersion7(), Now.AddMinutes(1), "restart-with-fallback");
        var repositories = CreateRepositories(scope);
        RegistrationSubmission? retained = null;
        repositories.Submissions.PersistEvidenceOnlyAsync(Arg.Do<RegistrationSubmission>(value => retained = value), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new RegistrationSubmissionPersistenceResult(
                RegistrationSubmissionPersistenceOutcome.Inserted,
                call.ArgAt<RegistrationSubmission>(0))));
        var handler = CreateHandler(scope, repositories);

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.NeedsReconciliation);
        await Assert.That(result.Code).IsEqualTo("STALE_OR_OUT_OF_ORDER");
        await Assert.That(retained).IsNotNull();
        await Assert.That(retained!.IsFinalizable).IsFalse();
        await repositories.Submissions.DidNotReceive().PersistAcceptedWithNormalizationAsync(
            Arg.Any<RegistrationAttempt>(), Arg.Any<RegistrationSubmission>(), Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<RegistrationAnswer>>(), Arg.Any<IReadOnlyCollection<RegistrationConsentRecord>>(),
            Arg.Any<IReadOnlyCollection<RegistrationSubmissionIssue>>(), Arg.Any<IReadOnlyCollection<RegistrationRequirementFulfillment>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReceiptBodyHashMismatch_ParksBeforeAttemptLookup()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.COMPLETION_ONLY);
        var repositories = CreateRepositories(scope);
        var handler = CreateHandler(scope, repositories, new StaticReceiptProtector(scope, "sha256:tampered"));

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.NeedsReconciliation);
        await Assert.That(result.Code).IsEqualTo("UNVERIFIABLE_EVIDENCE");
        await repositories.Submissions.DidNotReceive().GetAttemptAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReceiptTamper_ParksBeforeAttemptLookup()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.COMPLETION_ONLY);
        var repositories = CreateRepositories(scope);
        var handler = CreateHandler(scope, repositories, new TamperedReceiptProtector());

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.NeedsReconciliation);
        await Assert.That(result.Code).IsEqualTo("UNVERIFIABLE_EVIDENCE");
        await repositories.Submissions.DidNotReceive().GetAttemptAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReceiptTupleMismatch_ParksBeforeAttemptLookup()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.COMPLETION_ONLY);
        var repositories = CreateRepositories(scope);
        var handler = CreateHandler(scope, repositories, new StaticReceiptProtector(scope, tupleKey: "external-form|hosted|v2|policy|evidence"));

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.NeedsReconciliation);
        await Assert.That(result.Code).IsEqualTo("UNVERIFIABLE_EVIDENCE");
        await repositories.Submissions.DidNotReceive().GetAttemptAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DurableReplay_AcceptsPreviouslyVerifiedReceiptOlderThanOneDay()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.COMPLETION_ONLY);
        var repositories = CreateRepositories(scope);
        repositories.Submissions.PersistAcceptedWithNormalizationAsync(
                Arg.Any<RegistrationAttempt>(), Arg.Any<RegistrationSubmission>(), Arg.Any<Guid>(),
                Arg.Any<IReadOnlyCollection<RegistrationAnswer>>(), Arg.Any<IReadOnlyCollection<RegistrationConsentRecord>>(),
                Arg.Any<IReadOnlyCollection<RegistrationSubmissionIssue>>(), Arg.Any<IReadOnlyCollection<RegistrationRequirementFulfillment>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new RegistrationSubmissionPersistenceResult(
                RegistrationSubmissionPersistenceOutcome.Inserted, call.ArgAt<RegistrationSubmission>(1))));
        var handler = CreateHandler(scope, repositories, new StaticReceiptProtector(scope, verifiedAt: DateTimeOffset.UtcNow.AddDays(-7)));

        ProviderSubmissionEffectResult result = await handler.Handle(CreateCommand(scope), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.Completed);
    }

    [Test]
    public async Task InternalEnvelopeMissingAttemptId_WithoutReaderParksBeforeAttemptLookup()
    {
        ProviderScope scope = CreateScope(RegistrationAnswerSyncModeEnum.COMPLETION_ONLY);
        var repositories = CreateRepositories(scope);
        byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            providerSubmissionId = "submission-1",
            providerResponseRevision = "revision-1"
        }));
        var handler = CreateHandler(scope, repositories, new StaticReceiptProtector(scope, HashSha256(payload)));
        var command = CreateCommand(scope, payload);

        ProviderSubmissionEffectResult result = await handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(ProviderSubmissionEffectOutcome.NeedsReconciliation);
        await Assert.That(result.Code).IsEqualTo("SUBMISSION_READ_UNSUPPORTED");
        await repositories.Submissions.DidNotReceive().GetAttemptAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static ProcessProviderSubmissionEffectCommand CreateCommand(ProviderScope scope) =>
        CreateCommand(scope, Payload(scope));

    private static ProcessProviderSubmissionEffectCommand CreateCommand(ProviderScope scope, byte[] payload) => new(
        scope.TenantId,
        scope.IncomingWebhookMessageId,
        scope.Binding.Id,
        scope.Binding.Connection!.ProviderCode,
        payload,
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Registration-Verification-Receipt"] = "receipt:v1:test"
        });

    private static ProcessProviderSubmissionEffectCommandHandler CreateHandler(
        ProviderScope scope,
        RepositoryFakes repositories) => new(
        repositories.Providers,
        repositories.Submissions,
        repositories.Inventory,
        repositories.Forms,
        repositories.ParticipationConfigurations,
        repositories.Participants,
            Substitute.For<IRegistrationSensitiveValueProtector>(),
            Substitute.For<ISender>(),
            Substitute.For<IRegistrationProviderRegistry>(),
            new StaticReceiptProtector(scope),
            Substitute.For<IGuestCapabilityTokenService>(),
            new FixedTimeProvider(Now.AddMinutes(1)));

    private static ProcessProviderSubmissionEffectCommandHandler CreateHandler(
        ProviderScope scope,
        RepositoryFakes repositories,
        IRegistrationProviderCallbackReceiptProtector receiptProtector) => new(
        repositories.Providers,
        repositories.Submissions,
        repositories.Inventory,
        repositories.Forms,
        repositories.ParticipationConfigurations,
        repositories.Participants,
            Substitute.For<IRegistrationSensitiveValueProtector>(),
            Substitute.For<ISender>(),
            Substitute.For<IRegistrationProviderRegistry>(),
            receiptProtector,
            Substitute.For<IGuestCapabilityTokenService>(),
            new FixedTimeProvider(Now.AddMinutes(1)));

    private static ProcessProviderSubmissionEffectCommandHandler CreateHandler(
        ProviderScope scope,
        RepositoryFakes repositories,
        IRegistrationProviderCallbackReceiptProtector receiptProtector,
        IRegistrationProviderRegistry providerRegistry) => new(
        repositories.Providers,
        repositories.Submissions,
        repositories.Inventory,
        repositories.Forms,
        repositories.ParticipationConfigurations,
        repositories.Participants,
            Substitute.For<IRegistrationSensitiveValueProtector>(),
            Substitute.For<ISender>(),
            providerRegistry,
            receiptProtector,
            Substitute.For<IGuestCapabilityTokenService>(),
            new FixedTimeProvider(Now.AddMinutes(1)));

    private static ProcessProviderSubmissionEffectCommandHandler CreateHandler(
        ProviderScope scope,
        RepositoryFakes repositories,
        IRegistrationProviderCallbackReceiptProtector receiptProtector,
        IRegistrationProviderRegistry providerRegistry,
        IGuestCapabilityTokenService capabilities) => new(
        repositories.Providers,
        repositories.Submissions,
        repositories.Inventory,
        repositories.Forms,
        repositories.ParticipationConfigurations,
        repositories.Participants,
        Substitute.For<IRegistrationSensitiveValueProtector>(),
        Substitute.For<ISender>(),
        providerRegistry,
        receiptProtector,
        capabilities,
        new FixedTimeProvider(Now.AddMinutes(1)));

    private static RepositoryFakes CreateRepositories(ProviderScope scope, IdentityAccessModeEnum identityAccessMode = IdentityAccessModeEnum.CapabilityTokenAllowed)
    {
        IRegistrationProviderRepository providers = Substitute.For<IRegistrationProviderRepository>();
        IRegistrationSubmissionRepository submissions = Substitute.For<IRegistrationSubmissionRepository>();
        IRegistrationInventoryRepository inventory = Substitute.For<IRegistrationInventoryRepository>();
        IRegistrationFormAuthoringRepository forms = Substitute.For<IRegistrationFormAuthoringRepository>();
        IEventParticipationConfigurationRepository participationConfigurations = Substitute.For<IEventParticipationConfigurationRepository>();
        IRegistrationParticipantRepository participants = Substitute.For<IRegistrationParticipantRepository>();
        providers.GetBindingAsync(scope.TenantId, scope.Binding.Id, Arg.Any<CancellationToken>()).Returns(scope.Binding);
        submissions.GetAttemptAsync(scope.TenantId, scope.Attempt.Id, Arg.Any<CancellationToken>()).Returns(scope.Attempt);
        submissions.GetRequirementAsync(scope.TenantId, scope.Requirement.Id, Arg.Any<CancellationToken>()).Returns(scope.Requirement);
        inventory.GetOrderWithLinesAsync(scope.Order.Id, scope.TenantId, Arg.Any<CancellationToken>()).Returns(scope.Order);
        forms.GetVersionAsync(scope.EventId, scope.Form.Id, scope.Version.Id, Arg.Any<CancellationToken>()).Returns(scope.Version);
        participationConfigurations.GetByEventAndTenantAsync(scope.EventId, scope.TenantId, Arg.Any<CancellationToken>())
            .Returns(EventParticipationConfiguration.Create(
                scope.EventId,
                scope.TenantId,
                (int)ParticipationHandlingModeEnum.PlatformManaged,
                (int)AdvanceRegistrationObligationEnum.Required,
                (int)identityAccessMode,
                identityAccessMode == IdentityAccessModeEnum.AccountRequired ? null : GuestRecoveryPolicyEnum.CapabilityLinkOnly,
                Now));
        participants.GetParticipantsByOrderAsync(scope.Order.Id, scope.TenantId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<RegistrationParticipant>());
        participants.GetAssignmentsWithParticipantsByOrderAsync(scope.Order.Id, scope.TenantId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<RegistrationTicketAssignment>());
        submissions.PersistEvidenceOnlyAsync(Arg.Any<RegistrationSubmission>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new RegistrationSubmissionPersistenceResult(
                RegistrationSubmissionPersistenceOutcome.Inserted,
                call.ArgAt<RegistrationSubmission>(0))));
        submissions.PersistAcceptedWithNormalizationAsync(
                Arg.Any<RegistrationAttempt>(), Arg.Any<RegistrationSubmission>(), Arg.Any<Guid>(),
                Arg.Any<IReadOnlyCollection<RegistrationAnswer>>(), Arg.Any<IReadOnlyCollection<RegistrationConsentRecord>>(),
                Arg.Any<IReadOnlyCollection<RegistrationSubmissionIssue>>(), Arg.Any<IReadOnlyCollection<RegistrationRequirementFulfillment>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new RegistrationSubmissionPersistenceResult(
                RegistrationSubmissionPersistenceOutcome.Inserted,
                call.ArgAt<RegistrationSubmission>(1))));
        return new(providers, submissions, inventory, forms, participationConfigurations, participants);
    }

    private static ProviderScope CreateScope(
        RegistrationAnswerSyncModeEnum syncMode,
        RegistrationProviderTrustLevelEnum trustLevel = RegistrationProviderTrustLevelEnum.FullCanonical,
        bool addFieldMapping = false,
        string providerCode = "external-form")
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        RegistrationWorkflow workflow = RegistrationWorkflow.Create(tenantId, eventId, "ATTENDEE", Now);
        RegistrationRequirement requirement = RegistrationRequirement.Create(
            workflow, 1, RegistrationRequirementCriticalityEnum.Required, false,
            RegistrationRequirementCompletionEffectEnum.BlocksRegistration, syncMode,
            RegistrationRequirementSubjectTypeEnum.AllOrders, null, Now);
        RegistrationProviderConnection connection = RegistrationProviderConnection.Create(
            tenantId, "Provider", RegistrationProviderKindEnum.ExternalForm,
            RegistrationProviderDeploymentKindEnum.HostedSaas, providerCode, "hosted", "v1", "policy", "evidence",
            "https:/" + "/forms.example.org/api", "https:/" + "/forms.example.org", "workspace", null, Guid.CreateVersion7(), Now);
        RegistrationProviderBinding binding = RegistrationProviderBinding.Create(
            tenantId, connection.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            RegistrationProviderPresentationModeEnum.Redirect, RegistrationProviderCollectionModeEnum.ProviderHosted,
            RegistrationProviderCompletionModeEnum.Callback, trustLevel, null, Now);
        typeof(RegistrationProviderBinding).GetProperty(nameof(RegistrationProviderBinding.Connection))!.SetValue(binding, connection);
        binding.AddCapability(RegistrationProviderCapability.Create(binding, providerCode, "hosted", "v1", "policy", "evidence", RegistrationProviderCapabilityCodes.CallbackVerification));
        binding.AddCapability(RegistrationProviderCapability.Create(binding, providerCode, "hosted", "v1", "policy", "evidence", RegistrationProviderCapabilityCodes.SubmissionRead));
        RegistrationChannel channel = RegistrationChannel.Create(requirement, 1, false, binding.Id, Now);
        requirement.AddChannel(channel);
        RegistrationForm form = RegistrationForm.Create(binding.RegistrationFormId, tenantId, eventId, "registration", "provider", "Provider", Now);
        RegistrationFormVersion version = RegistrationFormVersion.Create(binding.RegistrationFormVersionId, form, 1, "en", null, null, Now);
        Guid fieldId = Guid.CreateVersion7();
        RegistrationFormSection section = RegistrationFormSection.Create(Guid.CreateVersion7(), version, 1, "Profile", Now);
        RegistrationFormField field = RegistrationFormField.Create(
            fieldId, section, 1, "profile", "display_name", "Display name", RegistrationFieldTypeEnum.ShortText, 1,
            RegistrationOrganizerVisibilityEnum.AuthorizedOrganizers, false, false, Now);
        version.AddSection(section);
        version.AddField(section, field);
        if (addFieldMapping)
        {
            binding.AddFieldMapping(RegistrationProviderFieldMapping.Create(
                binding, "profile.display_name", "provider.display_name", false));
        }

        binding.Publish(RegistrationEvidenceHash.Create(Hash("mapping")), Now);
        form.AddVersion(version);
        RegistrationOrder order = RegistrationOrder.Create(orderId, tenantId, eventId, Guid.CreateVersion7(), null,
            BookingPartyTypeEnum.Individual, Guid.CreateVersion7(), RegistrationParticipationSnapshot.Create(
                Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired), workflow.Id, null, "EUR", Now, Now.AddHours(1));
        RegistrationAttempt attempt = RegistrationAttempt.Create(
            tenantId, eventId, orderId, workflow.Id, requirement.Id, channel.Id, form.Id, version.Id,
            CapabilityTokenHash.Create(Hash("capability")), binding.Id, binding.PublishedMappingRevisionHash, Now, Now.AddHours(1));
        return new(tenantId, eventId, order, requirement, form, version, binding, attempt, Guid.CreateVersion7(), fieldId);
    }

    private static byte[] Payload(ProviderScope scope) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
    {
        attemptId = scope.Attempt.Id,
        providerSubmissionId = "submission-1",
        providerResponseRevision = "revision-1",
        receivedAt = Now,
        answers = new Dictionary<string, object>
        {
            ["provider.display_name"] = "Ali",
            ["provider.unmapped"] = "drop-me"
        }
    }));

    private static void SetPrivateProperty<T>(object instance, string name, T value) =>
        instance.GetType().GetProperty(name)!.SetValue(instance, value);

    private static string Hash(string value) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string HashSha256(byte[] value) => "sha256:" + Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

    private sealed record ProviderScope(Guid TenantId, Guid EventId, RegistrationOrder Order,
        RegistrationRequirement Requirement, RegistrationForm Form, RegistrationFormVersion Version,
        RegistrationProviderBinding Binding, RegistrationAttempt Attempt, Guid IncomingWebhookMessageId, Guid FieldId);

    private sealed record RepositoryFakes(IRegistrationProviderRepository Providers, IRegistrationSubmissionRepository Submissions,
        IRegistrationInventoryRepository Inventory, IRegistrationFormAuthoringRepository Forms,
        IEventParticipationConfigurationRepository ParticipationConfigurations, IRegistrationParticipantRepository Participants);

    private sealed class StaticReceiptProtector(
        ProviderScope scope,
        string? bodyHash = null,
        string? tupleKey = null,
        DateTimeOffset? verifiedAt = null) : IRegistrationProviderCallbackReceiptProtector
    {
        public string Protect(RegistrationProviderCallbackReceipt receipt) => "receipt:v1:test";

        public RegistrationProviderCallbackReceipt Unprotect(string protectedReceipt) => new(
            scope.TenantId,
            scope.Binding.RegistrationProviderConnectionId,
            scope.Binding.Id,
            scope.Binding.Connection!.ProviderCode,
            tupleKey ?? scope.Binding.Connection.ProviderCode + "|hosted|v1|policy|evidence",
            bodyHash ?? "sha256:" + Convert.ToHexString(SHA256.HashData(Payload(scope))).ToLowerInvariant(),
            "submission-1",
            verifiedAt ?? Now,
            "nonce");
    }

    private sealed class TamperedReceiptProtector : IRegistrationProviderCallbackReceiptProtector
    {
        public string Protect(RegistrationProviderCallbackReceipt receipt) => "receipt:v1:test";

        public RegistrationProviderCallbackReceipt Unprotect(string protectedReceipt) => throw new System.Security.Cryptography.CryptographicException("tampered");
    }

    private sealed class ReaderRegistry(ProviderScope scope) : IRegistrationProviderRegistry, IRegistrationProviderDescriptor, IRegistrationProviderSubmissionReader
    {
        public RegistrationProviderTuple Tuple { get; } = new("external-form", "hosted", "v1", "policy", "evidence");
        public RegistrationProviderCapabilitySet ProvenCapabilities => RegistrationProviderCapabilitySet.None;

        public IRegistrationProviderDescriptor? TryResolve(RegistrationProviderTuple tuple) => this;

        public Task<RegistrationProviderSubmissionReadResult> ReadSubmissionAsync(
            RegistrationProviderSubmissionReadRequest request,
            CancellationToken cancellationToken) => Task.FromResult(new RegistrationProviderSubmissionReadResult(
            "submission-1",
            "revision-1",
            Now,
            scope.Attempt.Id,
            new Dictionary<string, JsonElement>
            {
                ["provider.display_name"] = JsonDocument.Parse("\"Ali\"").RootElement.Clone(),
                ["provider.unmapped"] = JsonDocument.Parse("\"drop-me\"").RootElement.Clone()
            }));
    }

    private sealed class GoogleDelegatedReaderRegistry(ProviderScope scope, string token) : IRegistrationProviderRegistry, IRegistrationProviderDescriptor, IRegistrationProviderSubmissionReader, IRegistrationProviderDelegatedAutomation
    {
        public RegistrationProviderTuple Tuple { get; } = new("GOOGLE_FORMS", "hosted", "v1", "policy", "evidence");
        public RegistrationProviderCapabilitySet ProvenCapabilities => RegistrationProviderCapabilitySet.None;
        public string ConnectorContractVersion => "GOOGLE_FORMS_ENTRY_CORRELATION_V1";
        public string RequiredCorrelationPlatformFieldKey => "system.registration_attempt_token";

        public IRegistrationProviderDescriptor? TryResolve(RegistrationProviderTuple tuple) => this;

        public Task<RegistrationProviderSubmissionReadResult> ReadSubmissionAsync(
            RegistrationProviderSubmissionReadRequest request,
            CancellationToken cancellationToken) => Task.FromResult(new RegistrationProviderSubmissionReadResult(
            "submission-1",
            "revision-1",
            Now,
            scope.Attempt.Id,
            new Dictionary<string, JsonElement>(),
            token));
    }

    private sealed class UnsupportedDriveReaderRegistry : IRegistrationProviderRegistry, IRegistrationProviderDescriptor, IRegistrationProviderSubmissionReader
    {
        public RegistrationProviderTuple Tuple { get; } = new("GOOGLE_FORMS", "hosted", "v1", "policy", "evidence");
        public RegistrationProviderCapabilitySet ProvenCapabilities => RegistrationProviderCapabilitySet.None;

        public IRegistrationProviderDescriptor? TryResolve(RegistrationProviderTuple tuple) => this;

        public Task<RegistrationProviderSubmissionReadResult> ReadSubmissionAsync(
            RegistrationProviderSubmissionReadRequest request,
            CancellationToken cancellationToken) => throw new RegistrationProviderUnsupportedSubmissionException("PROVIDER_FILE_UPLOAD_UNSUPPORTED");
    }

    private sealed class FailingReaderRegistry(Exception exception) : IRegistrationProviderRegistry, IRegistrationProviderDescriptor, IRegistrationProviderSubmissionReader
    {
        public RegistrationProviderTuple Tuple { get; } = new("external-form", "hosted", "v1", "policy", "evidence");
        public RegistrationProviderCapabilitySet ProvenCapabilities => RegistrationProviderCapabilitySet.None;
        public IRegistrationProviderDescriptor? TryResolve(RegistrationProviderTuple tuple) => this;
        public Task<RegistrationProviderSubmissionReadResult> ReadSubmissionAsync(
            RegistrationProviderSubmissionReadRequest request,
            CancellationToken cancellationToken) => Task.FromException<RegistrationProviderSubmissionReadResult>(exception);
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
