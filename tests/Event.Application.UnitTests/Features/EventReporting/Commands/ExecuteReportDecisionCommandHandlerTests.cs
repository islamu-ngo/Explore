// ABOUTME: Focused source coverage for durable report-decision enforcement and notification completion.
// ABOUTME: Specifies exact receipts, completion resume, organizer authority, and false-outcome prevention.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.EventReporting;
using Explore.Application.Features.EventReporting.Handlers.Commands;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventReporting.Commands;

public sealed class ExecuteReportDecisionCommandHandlerTests
{
    private readonly IEventReportRepository _reportRepository = Substitute.For<IEventReportRepository>();
    private readonly IEventReportDecisionExecutionRepository _executionRepository = Substitute.For<IEventReportDecisionExecutionRepository>();
    private readonly IEventModerationRecordRepository _moderationRecordRepository = Substitute.For<IEventModerationRecordRepository>();
    private readonly IEventRoleAssignmentRepository _roleAssignmentRepository = Substitute.For<IEventRoleAssignmentRepository>();
    private readonly ITenantUserRepository _tenantUserRepository = Substitute.For<ITenantUserRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly INotificationPreferenceResolver _preferenceResolver = Substitute.For<INotificationPreferenceResolver>();
    private readonly RecordingMaterializer _materializer = new();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [Test]
    public async Task Handle_WithMalformedCommand_ReturnsValidationFailureBeforeRepositoryAccess()
    {
        var command = new ExecuteReportDecisionCommand();

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.ValidationFailed);
        await _reportRepository.DidNotReceive().GetByIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(EventReportDecisionSource.LocalModerator)]
    [Arguments(EventReportDecisionSource.CoopReviewer)]
    public async Task Handle_FinalOutcome_UsesExecutorOwnedIntentForLocalAndCoopSources(
        EventReportDecisionSource decisionSource)
    {
        Scenario scenario = CreateScenario(
            EventReportDecisionKind.NoViolation,
            decisionSource);
        ConfigureScenario(scenario);
        ConfigureExecutionTransitions(scenario.Decision.Execution);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(
            scenario.Command,
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(scenario.Decision.Execution.State)
            .IsEqualTo(EventReportDecisionExecutionState.Completed);
        RecipientNotificationMaterialization outcome = _materializer.Requests.Single();
        await Assert.That(outcome.Intent.TemplateKey)
            .IsEqualTo(ReportOutcomeNotificationFactory.TemplateKey);
        await Assert.That(outcome.Intent.DeduplicationKey)
            .IsEqualTo($"event-report-decision:{scenario.Decision.Id:N}:reporter-outcome");
        await Assert.That(outcome.DeliveryPolicy)
            .IsEqualTo(NotificationDeliveryPolicyEnum.ReportCaseUpdate);
        await Assert.That(outcome.Email!.Kind).IsEqualTo(EmailDispatchKind.ReportOutcome);
        await Assert.That(outcome.Email.SourceId).IsEqualTo(scenario.Decision.Id);
    }

    [Test]
    public async Task Handle_LightModeration_RequiresExactReceiptBeforeAtomicOutcomeCompletion()
    {
        Scenario scenario = CreateScenario(EventReportDecisionKind.LightModerate);
        EventModerationRecord receipt = EventModerationRecord.CreateLightModeration(
            scenario.TenantId,
            scenario.Report.EventId,
            scenario.ModeratorUserId,
            scenario.Decision.ReasonCode,
            (int)EventStatusEnum.Published,
            "report-receipt",
            DateTimeOffset.UtcNow);
        receipt.LinkSourceReportDecision(scenario.Report.Id, scenario.Decision.Id);

        ConfigureScenario(scenario);
        ConfigureExecutionTransitions(scenario.Decision.Execution);
        _moderationRecordRepository.GetBySourceReportDecisionAsync(
                scenario.TenantId,
                scenario.Report.Id,
                scenario.Decision.Id,
                Arg.Any<CancellationToken>())
            .Returns((EventModerationRecord?)null, receipt);
        _mediator.Send(Arg.Any<ModerateEventCommand>(), Arg.Any<CancellationToken>())
            .Returns(Success(scenario.Report.EventId));

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(scenario.Command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(scenario.Decision.Execution.State).IsEqualTo(EventReportDecisionExecutionState.Completed);
        await Assert.That(scenario.Decision.Execution.EnforcementReceiptId).IsEqualTo(receipt.Id);
        await Assert.That(scenario.Report.Status).IsEqualTo(EventReportStatus.Actioned);
        await Assert.That(scenario.Case.Status).IsEqualTo(EventReportCaseStatus.Closed);
        await Assert.That(_materializer.Requests).Count().IsEqualTo(1);
        await Assert.That(_materializer.Requests[0].Email!.Kind).IsEqualTo(EmailDispatchKind.ReportOutcome);
        await Assert.That(_materializer.Requests[0].Intent.SafePayloadReference).DoesNotContain(scenario.Report.EventId.ToString("D"));
        await _mediator.Received(1).Send(
            Arg.Is<ModerateEventCommand>(command =>
                command.SourceReportId == scenario.Report.Id
                && command.SourceReportDecisionId == scenario.Decision.Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_CompletionPending_ResumesWithoutRepeatingModeration()
    {
        Scenario scenario = CreateScenario(EventReportDecisionKind.LightModerate);
        EventModerationRecord receipt = EventModerationRecord.CreateLightModeration(
            scenario.TenantId,
            scenario.Report.EventId,
            scenario.ModeratorUserId,
            scenario.Decision.ReasonCode,
            (int)EventStatusEnum.Published,
            "report-receipt",
            DateTimeOffset.UtcNow);
        receipt.LinkSourceReportDecision(scenario.Report.Id, scenario.Decision.Id);
        DateTime now = DateTime.UtcNow;
        Guid priorLease = Guid.CreateVersion7();
        scenario.Decision.Execution.ClaimEnforcement(priorLease, now, now.AddMinutes(5));
        scenario.Decision.Execution.RecordEnforcementReceipt(
            priorLease,
            EventReportDecisionEnforcementReceiptKind.LightModeration,
            receipt.Id,
            now.AddSeconds(1));

        ConfigureScenario(scenario);
        ConfigureExecutionTransitions(scenario.Decision.Execution);
        _moderationRecordRepository.GetBySourceReportDecisionAsync(
                scenario.TenantId,
                scenario.Report.Id,
                scenario.Decision.Id,
                Arg.Any<CancellationToken>())
            .Returns(receipt);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(scenario.Command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(scenario.Decision.Execution.State).IsEqualTo(EventReportDecisionExecutionState.Completed);
        await Assert.That(scenario.Decision.Execution.EnforcementReceiptId).IsEqualTo(receipt.Id);
        await _mediator.DidNotReceive().Send(Arg.Any<ModerateEventCommand>(), Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<HeavyRedactEventCommand>(), Arg.Any<CancellationToken>());
        await Assert.That(_materializer.Requests).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Handle_NeedsMoreInfo_CompletesWaitingReporterAndMaterializesNonFinalFollowUpOnce()
    {
        Scenario scenario = CreateScenario(EventReportDecisionKind.NeedsMoreInfo);
        ConfigureScenario(scenario);
        ConfigureExecutionTransitions(scenario.Decision.Execution);

        BaseCommandResponse<Guid> first = await CreateHandler().Handle(scenario.Command, CancellationToken.None);
        BaseCommandResponse<Guid> replay = await CreateHandler().Handle(scenario.Command, CancellationToken.None);

        await Assert.That(first.Success).IsTrue();
        await Assert.That(replay.Success).IsTrue();
        await Assert.That(scenario.Report.Status).IsEqualTo(EventReportStatus.UnderReview);
        await Assert.That(scenario.Case.Status).IsEqualTo(EventReportCaseStatus.WaitingReporter);
        await Assert.That(scenario.Decision.Execution.State).IsEqualTo(EventReportDecisionExecutionState.Completed);
        await Assert.That(_materializer.Requests).Count().IsEqualTo(1);
        RecipientNotificationMaterialization followUp = _materializer.Requests.Single();
        await Assert.That(followUp.Intent.TemplateKey).IsEqualTo(ReportNeedsMoreInformationNotificationFactory.TemplateKey);
        await Assert.That(followUp.DeliveryPolicy).IsEqualTo(NotificationDeliveryPolicyEnum.ReportFollowUpContact);
        await Assert.That(followUp.ConsentPurpose).IsEqualTo(ReportEmailConsentPurposeCodes.FollowUpContact);
        await Assert.That(followUp.InApp!.IsRequired).IsTrue();
        await Assert.That(followUp.LinkAllowed).IsFalse();
        await Assert.That(followUp.Email!.Kind).IsEqualTo(EmailDispatchKind.ReportNeedsMoreInformation);
        await Assert.That(_materializer.Requests.Any(value =>
            value.Intent.TemplateKey == ReportOutcomeNotificationFactory.TemplateKey)).IsFalse();
    }

    [Test]
    [Arguments(RequiredReporterAuthorityLoss.AnonymousReport)]
    [Arguments(RequiredReporterAuthorityLoss.InactiveOrMissingMembership)]
    [Arguments(RequiredReporterAuthorityLoss.MissingUser)]
    [Arguments(RequiredReporterAuthorityLoss.DeletedUser)]
    public async Task Handle_NeedsMoreInfoWithoutRequiredReporterAuthority_LeavesCompletionResumable(
        RequiredReporterAuthorityLoss authorityLoss)
    {
        Scenario scenario = CreateScenario(
            EventReportDecisionKind.NeedsMoreInfo,
            anonymousReporter: authorityLoss == RequiredReporterAuthorityLoss.AnonymousReport);
        ConfigureScenario(scenario);
        ConfigureExecutionTransitions(scenario.Decision.Execution);
        if (scenario.Report.ReporterUserId is { } reporterUserId)
        {
            switch (authorityLoss)
            {
                case RequiredReporterAuthorityLoss.InactiveOrMissingMembership:
                    _tenantUserRepository.IsActiveTenantUserAsync(
                            scenario.TenantId,
                            reporterUserId,
                            Arg.Any<CancellationToken>())
                        .Returns(false);
                    break;
                case RequiredReporterAuthorityLoss.MissingUser:
                    _userRepository.GetUserWithDetails(reporterUserId, Arg.Any<CancellationToken>())
                        .Returns((User?)null);
                    break;
                case RequiredReporterAuthorityLoss.DeletedUser:
                    scenario.Reporter.IsDeleted = true;
                    break;
            }
        }

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(scenario.Command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.DecisionRecipientAuthorityChanged);
        await Assert.That(scenario.Report.Status).IsEqualTo(EventReportStatus.UnderReview);
        await Assert.That(scenario.Case.Status).IsEqualTo(EventReportCaseStatus.DecisionReady);
        await Assert.That(scenario.Decision.Execution.State)
            .IsEqualTo(EventReportDecisionExecutionState.CompletionPending);
        await Assert.That(scenario.Decision.Execution.ProcessingLeaseToken).IsNull();
        await Assert.That(_materializer.Requests).IsEmpty();
    }

    [Test]
    [Arguments(OptionalEmailAuthorityLoss.FollowUpConsentWithdrawn)]
    [Arguments(OptionalEmailAuthorityLoss.EmailUnverified)]
    [Arguments(OptionalEmailAuthorityLoss.EmailMissing)]
    [Arguments(OptionalEmailAuthorityLoss.PreferenceDisabled)]
    public async Task Handle_NeedsMoreInfoWithoutOptionalEmailAuthority_StillMaterializesRequiredInApp(
        OptionalEmailAuthorityLoss authorityLoss)
    {
        Scenario scenario = CreateScenario(EventReportDecisionKind.NeedsMoreInfo);
        ConfigureScenario(scenario);
        ConfigureExecutionTransitions(scenario.Decision.Execution);
        switch (authorityLoss)
        {
            case OptionalEmailAuthorityLoss.FollowUpConsentWithdrawn:
                scenario.Report.ChangeReporterCommunicationConsent(
                    reportCaseUpdatesConsent: true,
                    reportFollowUpContactConsent: false,
                    DateTime.UtcNow);
                break;
            case OptionalEmailAuthorityLoss.EmailUnverified:
                scenario.Reporter.EmailVerified = false;
                break;
            case OptionalEmailAuthorityLoss.EmailMissing:
                scenario.Reporter.Pii.Email = string.Empty;
                break;
            case OptionalEmailAuthorityLoss.PreferenceDisabled:
                _preferenceResolver.ResolveAsync(
                        Arg.Any<NotificationPreferenceResolveRequest>(),
                        Arg.Any<CancellationToken>())
                    .Returns(CreatePreferenceDecision(isEnabled: false));
                break;
        }

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(scenario.Command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(scenario.Case.Status).IsEqualTo(EventReportCaseStatus.WaitingReporter);
        await Assert.That(scenario.Decision.Execution.State).IsEqualTo(EventReportDecisionExecutionState.Completed);
        RecipientNotificationMaterialization followUp = _materializer.Requests.Single();
        await Assert.That(followUp.InApp!.IsRequired).IsTrue();
        await Assert.That(followUp.IncludeEmailChannel).IsTrue();
        await Assert.That(followUp.Email).IsNull();
        await Assert.That(followUp.EmailSkipReason)
            .IsEqualTo(authorityLoss switch
            {
                OptionalEmailAuthorityLoss.FollowUpConsentWithdrawn =>
                    ReportNeedsMoreInformationNotificationFactory.ConsentNotGrantedSkipReason,
                OptionalEmailAuthorityLoss.EmailUnverified =>
                    RecipientEmailAddressResolver.RecipientEmailUnverified,
                OptionalEmailAuthorityLoss.EmailMissing =>
                    RecipientEmailAddressResolver.RecipientEmailMissing,
                OptionalEmailAuthorityLoss.PreferenceDisabled =>
                    ReportNeedsMoreInformationNotificationFactory.PreferenceDisabledSkipReason,
                _ => throw new ArgumentOutOfRangeException(nameof(authorityLoss), authorityLoss, null)
            });
    }

    [Test]
    public async Task Handle_WarnOrganizer_MaterializesRequiredOwnerWarningBeforeActionTakenOutcome()
    {
        Scenario scenario = CreateScenario(EventReportDecisionKind.WarnOrganizer);
        User organizer = CreateVerifiedUser();
        EventRoleAssignment owner = EventRoleAssignment.Create(
            scenario.TenantId,
            scenario.Report.EventId,
            organizer.Id,
            (int)RoleEnum.EventOwner,
            EventRoleAssignmentStatus.Active,
            DateTime.UtcNow.AddMinutes(-1),
            expiresAtUtc: null,
            scenario.ModeratorUserId);
        owner.User = organizer;

        ConfigureScenario(scenario);
        ConfigureExecutionTransitions(scenario.Decision.Execution);
        _roleAssignmentRepository.GetEffectiveOwnersForEventAsync(
                scenario.TenantId,
                scenario.Report.EventId,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns([owner]);
        _tenantUserRepository.IsActiveTenantUserAsync(
                scenario.TenantId,
                organizer.Id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(scenario.Command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(_materializer.Requests).Count().IsEqualTo(2);
        RecipientNotificationMaterialization warning = _materializer.Requests[0];
        await Assert.That(warning.Intent.RecipientKind).IsEqualTo(nameof(NotificationRecipientKindEnum.Organizer));
        await Assert.That(warning.DeliveryPolicy).IsEqualTo(NotificationDeliveryPolicyEnum.ModerationContextOptional);
        await Assert.That(warning.InApp!.IsRequired).IsTrue();
        await Assert.That(warning.InApp.NotificationEntityTypeId).IsNull();
        await Assert.That(warning.InApp.EntityId).IsNull();
        await Assert.That(warning.LinkAllowed).IsFalse();
        await Assert.That(warning.Email!.Kind).IsEqualTo(EmailDispatchKind.OrganizerNotification);
        await Assert.That(_materializer.Requests[1].Email!.Kind).IsEqualTo(EmailDispatchKind.ReportOutcome);
        await _mediator.DidNotReceive().Send(Arg.Any<ModerateEventCommand>(), Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<HeavyRedactEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WarnOrganizerWithoutActiveOwner_DoesNotCompleteOrCreateReporterOutcome()
    {
        Scenario scenario = CreateScenario(EventReportDecisionKind.WarnOrganizer);
        ConfigureScenario(scenario);
        ConfigureExecutionTransitions(scenario.Decision.Execution);
        _roleAssignmentRepository.GetEffectiveOwnersForEventAsync(
                scenario.TenantId,
                scenario.Report.EventId,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(scenario.Command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.DecisionOrganizerUnavailable);
        await Assert.That(scenario.Case.Status).IsEqualTo(EventReportCaseStatus.DecisionReady);
        await Assert.That(scenario.Report.Status).IsEqualTo(EventReportStatus.UnderReview);
        await Assert.That(_materializer.Requests).IsEmpty();
    }

    [Test]
    public async Task Handle_WarnOrganizer_WhenOwnerCohortChangesInsideCompletion_RollsBackBeforeMaterialization()
    {
        Scenario scenario = CreateScenario(EventReportDecisionKind.WarnOrganizer);
        User organizer = CreateVerifiedUser();
        EventRoleAssignment owner = EventRoleAssignment.Create(
            scenario.TenantId,
            scenario.Report.EventId,
            organizer.Id,
            (int)RoleEnum.EventOwner,
            EventRoleAssignmentStatus.Active,
            DateTime.UtcNow.AddMinutes(-1),
            expiresAtUtc: null,
            scenario.ModeratorUserId);
        owner.User = organizer;
        ConfigureScenario(scenario);
        ConfigureExecutionTransitions(scenario.Decision.Execution);
        _tenantUserRepository.IsActiveTenantUserAsync(
                scenario.TenantId,
                organizer.Id,
                Arg.Any<CancellationToken>())
            .Returns(true);
        _roleAssignmentRepository.GetEffectiveOwnersForEventAsync(
                scenario.TenantId,
                scenario.Report.EventId,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns([owner], (IReadOnlyList<EventRoleAssignment>)[]);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(scenario.Command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.DecisionRecipientAuthorityChanged);
        await Assert.That(scenario.Report.Status).IsEqualTo(EventReportStatus.UnderReview);
        await Assert.That(scenario.Case.Status).IsEqualTo(EventReportCaseStatus.DecisionReady);
        await Assert.That(_materializer.Requests).IsEmpty();
    }

    [Test]
    public async Task Handle_ClosedCaseWithoutCompletedExecution_FailsClosed()
    {
        Scenario scenario = CreateScenario(EventReportDecisionKind.NoViolation);
        scenario.Case.Close(DateTime.UtcNow);
        ConfigureScenario(scenario);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(scenario.Command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.DecisionExecutionInvalidState);
        await Assert.That(_materializer.Requests).IsEmpty();
        await _mediator.DidNotReceive().Send(Arg.Any<ModerateEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_SameLeaseAndAlreadyAppliedReceipt_CompletesIdempotently()
    {
        Scenario scenario = CreateScenario(EventReportDecisionKind.NoViolation);
        EventReportDecisionExecution execution = scenario.Decision.Execution;
        ConfigureScenario(scenario);
        _executionRepository.TryClaimEnforcementAsync(
                execution.TenantId,
                execution.DecisionId,
                Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                execution.ClaimEnforcement(call.ArgAt<Guid>(2), call.ArgAt<DateTime>(3), call.ArgAt<DateTime>(4));
                return EventReportDecisionExecutionClaimOutcome.SameLease;
            });
        _executionRepository.TryRecordEnforcementReceiptAsync(
                execution.TenantId,
                execution.DecisionId,
                Arg.Any<Guid>(),
                EventReportDecisionEnforcementReceiptKind.NoAction,
                null,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                execution.RecordEnforcementReceipt(
                    call.ArgAt<Guid>(2),
                    call.ArgAt<EventReportDecisionEnforcementReceiptKind>(3),
                    null,
                    call.ArgAt<DateTime>(5));
                return EventReportDecisionExecutionTransitionOutcome.AlreadyApplied;
            });
        _executionRepository.TryClaimCompletionAsync(
                execution.TenantId,
                execution.DecisionId,
                Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                execution.ClaimCompletion(call.ArgAt<Guid>(2), call.ArgAt<DateTime>(3), call.ArgAt<DateTime>(4));
                return EventReportDecisionExecutionClaimOutcome.Claimed;
            });

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(scenario.Command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(execution.State).IsEqualTo(EventReportDecisionExecutionState.Completed);
        await Assert.That(scenario.Case.Status).IsEqualTo(EventReportCaseStatus.Closed);
        await Assert.That(_materializer.Requests).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Handle_EnforcementClaimUnavailable_ReturnsInProgressWithoutSideEffects()
    {
        Scenario scenario = CreateScenario(EventReportDecisionKind.NoViolation);
        ConfigureScenario(scenario);
        _executionRepository.TryClaimEnforcementAsync(
                scenario.TenantId,
                scenario.Decision.Id,
                Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => EventReportDecisionExecutionClaimOutcome.Unavailable);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(scenario.Command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.DecisionExecutionInProgress);
        await Assert.That(scenario.Decision.Execution.State).IsEqualTo(EventReportDecisionExecutionState.Requested);
        await Assert.That(scenario.Case.Status).IsEqualTo(EventReportCaseStatus.DecisionReady);
        await Assert.That(_materializer.Requests).IsEmpty();
    }

    [Test]
    public async Task Handle_ReceiptConflict_ReturnsInProgressWithoutCompletingDecision()
    {
        Scenario scenario = CreateScenario(EventReportDecisionKind.NoViolation);
        ConfigureScenario(scenario);
        ConfigureExecutionTransitions(scenario.Decision.Execution);
        _executionRepository.TryRecordEnforcementReceiptAsync(
                scenario.TenantId,
                scenario.Decision.Id,
                Arg.Any<Guid>(),
                Arg.Any<EventReportDecisionEnforcementReceiptKind>(),
                Arg.Any<Guid?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => EventReportDecisionExecutionTransitionOutcome.Conflict);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(scenario.Command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.DecisionExecutionInProgress);
        await Assert.That(scenario.Decision.Execution.State).IsEqualTo(EventReportDecisionExecutionState.InProgress);
        await Assert.That(scenario.Case.Status).IsEqualTo(EventReportCaseStatus.DecisionReady);
        await Assert.That(_materializer.Requests).IsEmpty();
    }

    [Test]
    public async Task Handle_CompletionClaimCompleted_ReturnsIdempotentSuccessWithoutDuplicateOutcome()
    {
        Scenario scenario = CreateScenario(EventReportDecisionKind.NoViolation);
        ConfigureScenario(scenario);
        ConfigureExecutionTransitions(scenario.Decision.Execution);
        _executionRepository.TryClaimCompletionAsync(
                scenario.TenantId,
                scenario.Decision.Id,
                Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => EventReportDecisionExecutionClaimOutcome.Completed);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(scenario.Command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event report decision was already executed.");
        await Assert.That(scenario.Decision.Execution.State).IsEqualTo(EventReportDecisionExecutionState.CompletionPending);
        await Assert.That(scenario.Case.Status).IsEqualTo(EventReportCaseStatus.DecisionReady);
        await Assert.That(_materializer.Requests).IsEmpty();
    }

    private void ConfigureScenario(Scenario scenario)
    {
        _tenantContext.TenantId.Returns(scenario.TenantId);
        _currentUserService.UserId.Returns(scenario.ModeratorUserId);
        _tenantUserRepository.IsActiveTenantUserAsync(
                scenario.TenantId,
                scenario.ModeratorUserId,
                Arg.Any<CancellationToken>())
            .Returns(true);
        if (scenario.Report.ReporterUserId is { } reporterUserId)
        {
            _tenantUserRepository.IsActiveTenantUserAsync(
                    scenario.TenantId,
                    reporterUserId,
                    Arg.Any<CancellationToken>())
                .Returns(true);
            _userRepository.GetUserWithDetails(
                    reporterUserId,
                    Arg.Any<CancellationToken>())
                .Returns(scenario.Reporter);
        }
        _reportRepository.GetByIdAsync(
                scenario.TenantId,
                scenario.Report.Id,
                Arg.Any<CancellationToken>())
            .Returns(scenario.Report);
        _reportRepository.GetByIdForUpdateAsync(
                scenario.TenantId,
                scenario.Report.Id,
                Arg.Any<CancellationToken>())
            .Returns(scenario.Report);
        _reportRepository.Update(scenario.Report).Returns(Task.CompletedTask);
        _preferenceResolver.ResolveAsync(
                Arg.Any<NotificationPreferenceResolveRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(CreatePreferenceDecision(isEnabled: true));
        _executionRepository.GetByDecisionIdAsync(
                scenario.TenantId,
                scenario.Decision.Id,
                false,
                Arg.Any<CancellationToken>())
            .Returns(scenario.Decision.Execution);
    }

    private void ConfigureExecutionTransitions(EventReportDecisionExecution execution)
    {
        _executionRepository.TryClaimEnforcementAsync(
                execution.TenantId,
                execution.DecisionId,
                Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                execution.ClaimEnforcement(call.ArgAt<Guid>(2), call.ArgAt<DateTime>(3), call.ArgAt<DateTime>(4));
                return EventReportDecisionExecutionClaimOutcome.Claimed;
            });
        _executionRepository.TryRecordEnforcementReceiptAsync(
                execution.TenantId,
                execution.DecisionId,
                Arg.Any<Guid>(),
                Arg.Any<EventReportDecisionEnforcementReceiptKind>(),
                Arg.Any<Guid?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                execution.RecordEnforcementReceipt(
                    call.ArgAt<Guid>(2),
                    call.ArgAt<EventReportDecisionEnforcementReceiptKind>(3),
                    call.ArgAt<Guid?>(4),
                    call.ArgAt<DateTime>(5));
                return EventReportDecisionExecutionTransitionOutcome.Applied;
            });
        _executionRepository.TryClaimCompletionAsync(
                execution.TenantId,
                execution.DecisionId,
                Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                execution.ClaimCompletion(call.ArgAt<Guid>(2), call.ArgAt<DateTime>(3), call.ArgAt<DateTime>(4));
                return EventReportDecisionExecutionClaimOutcome.Claimed;
            });
        _executionRepository.TryReleaseCompletionClaimAsync(
                execution.TenantId,
                execution.DecisionId,
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                execution.ReleaseCompletionClaim(
                    call.ArgAt<Guid>(2),
                    call.ArgAt<string>(3),
                    call.ArgAt<DateTime>(4));
                return true;
            });
    }

    private ExecuteReportDecisionCommandHandler CreateHandler() => new(
        _reportRepository,
        _executionRepository,
        _moderationRecordRepository,
        _roleAssignmentRepository,
        _tenantUserRepository,
        _userRepository,
        _preferenceResolver,
        _materializer,
        new ReportOutcomeNotificationFactory(),
        new ReportNeedsMoreInformationNotificationFactory(),
        new EventOrganizerWarningNotificationFactory(),
        new InlineUnitOfWork(),
        _tenantContext,
        _currentUserService,
        _mediator);

    private static Scenario CreateScenario(
        EventReportDecisionKind decisionKind,
        EventReportDecisionSource decisionSource = EventReportDecisionSource.LocalModerator,
        bool anonymousReporter = false)
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid moderatorUserId = Guid.CreateVersion7();
        User reporter = CreateVerifiedUser();
        EventReport report = EventReport.Create(
            tenantId,
            Guid.CreateVersion7(),
            anonymousReporter ? null : reporter.Id,
            null,
            anonymousReporter ? EventReporterKind.Anonymous : EventReporterKind.AuthenticatedUser,
            EventReportSourceKind.UserReport,
            "spam",
            null,
            EventReportPriority.Normal,
            null,
            reportCaseUpdatesConsent: !anonymousReporter,
            reportFollowUpContactConsent: !anonymousReporter
                && decisionKind == EventReportDecisionKind.NeedsMoreInfo,
            null,
            null,
            null);
        report.UpdateStatus(EventReportStatus.UnderReview, DateTime.UtcNow);

        EventReportCase reportCase = EventReportCase.Create(
            tenantId,
            report.Id,
            "default",
            EventReportPriority.Normal,
            DateTime.UtcNow.AddDays(1));
        reportCase.Assign(moderatorUserId, DateTime.UtcNow);
        Guid? duplicateGroupId = decisionKind == EventReportDecisionKind.Duplicate
            ? Guid.CreateVersion7()
            : null;
        EventReportDecision decision = EventReportDecision.Create(
            tenantId,
            reportCase.Id,
            report.Id,
            decisionSource,
            decisionKind,
            "policy_violation",
            "safe internal note",
            decisionSource == EventReportDecisionSource.LocalModerator ? moderatorUserId : null,
            decisionSource == EventReportDecisionSource.CoopReviewer ? "coop-convergence-decision" : null,
            providerTargetScope: decisionSource == EventReportDecisionSource.CoopReviewer
                ? EventReportProviderTargetScope.Instance
                : EventReportProviderTargetScope.Local,
            providerTargetId: decisionSource == EventReportDecisionSource.CoopReviewer
                ? "instance"
                : "local",
            duplicateGroupId: duplicateGroupId);
        reportCase.SelectDecision(decision, DateTime.UtcNow);
        report.Cases.Add(reportCase);
        report.Decisions.Add(decision);

        return new Scenario(
            tenantId,
            moderatorUserId,
            reporter,
            report,
            reportCase,
            decision,
            new ExecuteReportDecisionCommand
            {
                EventId = report.EventId,
                ReportId = report.Id,
                CaseId = reportCase.Id,
                DecisionId = decision.Id,
                ExpectedCaseConcurrencyStamp = reportCase.ConcurrencyStamp
            });
    }

    private static User CreateVerifiedUser()
    {
        Guid userId = Guid.CreateVersion7();
        return new User
        {
            Id = userId,
            EmailVerified = true,
            Pii = new UserPii
            {
                UserId = userId,
                Email = $"{userId:N}@example.test",
                FirstName = "Test",
                LastName = "User"
            }
        };
    }

    private static BaseCommandResponse<Guid> Success(Guid id) => new()
    {
        Success = true,
        Id = id,
        Message = "Succeeded"
    };

    private static NotificationPreferenceDecision CreatePreferenceDecision(bool isEnabled) => new(
        NotificationPreferenceCategoryCodes.TrustSafety,
        NotificationPreferenceChannelCodes.Email,
        isEnabled,
        false,
        false,
        false,
        "Default",
        null);

    private sealed record Scenario(
        Guid TenantId,
        Guid ModeratorUserId,
        User Reporter,
        EventReport Report,
        EventReportCase Case,
        EventReportDecision Decision,
        ExecuteReportDecisionCommand Command);

    public enum RequiredReporterAuthorityLoss
    {
        AnonymousReport,
        InactiveOrMissingMembership,
        MissingUser,
        DeletedUser
    }

    public enum OptionalEmailAuthorityLoss
    {
        FollowUpConsentWithdrawn,
        EmailUnverified,
        EmailMissing,
        PreferenceDisabled
    }

    private sealed class RecordingMaterializer : IRecipientNotificationMaterializer
    {
        public List<RecipientNotificationMaterialization> Requests { get; } = [];

        public Task<RecipientNotificationMaterializationResult> MaterializeAsync(
            RecipientNotificationMaterialization request,
            CancellationToken cancellationToken = default) =>
            MaterializeInCurrentTransactionAsync(request, cancellationToken);

        public Task<RecipientNotificationMaterializationResult> MaterializeInCurrentTransactionAsync(
            RecipientNotificationMaterialization request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult<RecipientNotificationMaterializationResult>(null!);
        }
    }

    private sealed class InlineUnitOfWork : IUnitOfWork
    {
        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default) => await operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => operation(ct);

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => operation(ct);
    }
}
