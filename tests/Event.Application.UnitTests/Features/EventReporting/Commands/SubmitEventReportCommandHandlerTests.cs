// ABOUTME: Unit tests for authenticated event-report submission command handling.
// ABOUTME: Verifies validation, duplicate prevention, quotas, encrypted evidence, outbox, and transaction behavior.

using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Features.EventReporting;
using Explore.Application.Features.EventReporting.Handlers.Commands;
using Explore.Application.Features.EventReporting.Policies;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventReporting.Commands;

public sealed class SubmitEventReportCommandHandlerTests
{
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IEventReportRepository _eventReportRepository = Substitute.For<IEventReportRepository>();
    private readonly IGenericRepository<EventReportTarget, Guid> _targetRepository = Substitute.For<IGenericRepository<EventReportTarget, Guid>>();
    private readonly IGenericRepository<EventReportEvidence, Guid> _evidenceRepository = Substitute.For<IGenericRepository<EventReportEvidence, Guid>>();
    private readonly IGenericRepository<EventReportCase, Guid> _caseRepository = Substitute.For<IGenericRepository<EventReportCase, Guid>>();
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();
    private readonly INotificationPreferenceResolver _notificationPreferenceResolver = Substitute.For<INotificationPreferenceResolver>();
    private readonly IRecipientNotificationMaterializer _recipientNotificationMaterializer = Substitute.For<IRecipientNotificationMaterializer>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IEventReportingIntakeGuard _intakeGuard = Substitute.For<IEventReportingIntakeGuard>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IPrivacyErasureStateRepository _privacyErasureStateRepository = Substitute.For<IPrivacyErasureStateRepository>();
    private readonly IEventReportEvidenceProtector _evidenceProtector = Substitute.For<IEventReportEvidenceProtector>();
    private readonly BusinessMetrics _metrics = CreateMetrics();
    private readonly EventReportSubmissionOptions _options = new();
    private readonly List<RecipientNotificationMaterialization> _receiptMaterializations = [];
    private bool _transactionActive;
    private bool _receiptMaterializedInsideTransaction;

    public SubmitEventReportCommandHandlerTests()
    {
        _intakeGuard.ResolveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(EnabledIntakeDecision());
        _eventRepository.IsPubliclyEligibleAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        _unitOfWork
            .ExecuteSerializableAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                _transactionActive = true;
                try
                {
                    return await operation(CancellationToken.None);
                }
                finally
                {
                    _transactionActive = false;
                }
            });

        _eventReportRepository.Create(Arg.Any<EventReport>())
            .Returns(call => call.Arg<EventReport>());
        _targetRepository.Create(Arg.Any<EventReportTarget>())
            .Returns(call => call.Arg<EventReportTarget>());
        _evidenceRepository.Create(Arg.Any<EventReportEvidence>())
            .Returns(call => call.Arg<EventReportEvidence>());
        _caseRepository.Create(Arg.Any<EventReportCase>())
            .Returns(call => call.Arg<EventReportCase>());
        _outboxRepository.Create(Arg.Any<OutboxMessage>())
            .Returns(call => call.Arg<OutboxMessage>());
        _privacyErasureStateRepository.GetBySubjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PrivacyErasureSaga?)null);

        _eventReportRepository.ExistsByReporterAndEventAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<Guid?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        _eventReportRepository.CountByReporterSinceAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<Guid?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(0);
        _eventReportRepository.CountByReporterAndEventSinceAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<Guid?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(0);
        _evidenceProtector.Protect(Arg.Any<string>())
            .Returns(call => "protected:" + call.Arg<string>());
        _userRepository.GetUserWithDetails(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => CreateUser(call.ArgAt<Guid>(0)));
        _notificationPreferenceResolver.ResolveAsync(
                Arg.Any<NotificationPreferenceResolveRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                NotificationPreferenceResolveRequest request = call.ArgAt<NotificationPreferenceResolveRequest>(0);
                return new NotificationPreferenceDecision(
                    request.CategoryCode,
                    request.ChannelCode,
                    true,
                    true,
                    true,
                    false,
                    "Default",
                    null);
            });
        _recipientNotificationMaterializer.MaterializeInCurrentTransactionAsync(
                Arg.Any<RecipientNotificationMaterialization>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                RecipientNotificationMaterialization request = call.ArgAt<RecipientNotificationMaterialization>(0);
                _receiptMaterializedInsideTransaction = _transactionActive;
                _receiptMaterializations.Add(request);
                return new RecipientNotificationMaterializationResult(
                    new NotificationIntent
                    {
                        Id = request.IntentId,
                        TenantId = request.Intent.TenantId!.Value,
                        CategoryId = (int)NotificationCategoryEnum.TrustSafetyReporting,
                        OwnershipTypeId = (int)NotificationOwnershipTypeEnum.IslamuEvent,
                        RecipientKindId = (int)NotificationRecipientKindEnum.Reporter,
                        StatusId = (int)NotificationIntentStatusEnum.DispatchQueued,
                        TemplateKey = request.Intent.TemplateKey!,
                        DeduplicationKey = request.Intent.DeduplicationKey!,
                        RecipientUserId = request.Intent.UserId!.Value
                    },
                    [],
                    null,
                    request.Email);
            });
    }

    [Test]
    public async Task SubmissionOptions_DefineOneBoundedCanonicalSlaDefault()
    {
        var options = new EventReportSubmissionOptions();

        await Assert.That(options.CaseSlaHours).IsEqualTo(48);
        await Assert.That(options.CaseSlaHours).IsEqualTo(EventReportSubmissionOptions.DefaultCaseSlaHours);
        await Assert.That(EventReportSubmissionOptions.MinCaseSlaHours).IsEqualTo(1);
        await Assert.That(EventReportSubmissionOptions.MaxCaseSlaHours).IsEqualTo(720);
        await Assert.That(EventReportSubmissionOptions.IsValidCaseSlaHours(0)).IsFalse();
        await Assert.That(EventReportSubmissionOptions.IsValidCaseSlaHours(1)).IsTrue();
        await Assert.That(EventReportSubmissionOptions.IsValidCaseSlaHours(720)).IsTrue();
        await Assert.That(EventReportSubmissionOptions.IsValidCaseSlaHours(721)).IsFalse();
    }

    [Test]
    public async Task Handle_WhenIntakeIsDisabled_ReturnsIntakeFailureBeforeValidationOrReportOperations()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        _intakeGuard.ResolveAsync(tenantId, CancellationToken.None).Returns(DisabledIntakeDecision());

        var result = await CreateHandler().Handle(CreateCommand(Guid.NewGuid(), reasonCode: "unsupported"), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.IntakeDisabled);
        await _intakeGuard.Received(1).ResolveAsync(tenantId, CancellationToken.None);
        await _privacyErasureStateRepository.DidNotReceive().GetBySubjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await _actorRepository.DidNotReceive().GetActorByUserIdAndTenantId(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().GetUserWithDetails(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventReportRepository.DidNotReceive().ExistsByReporterAndEventAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _eventReportRepository.DidNotReceive().CountByReporterSinceAsync(
            Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _eventReportRepository.DidNotReceive().CountByReporterAndEventSinceAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        _evidenceProtector.DidNotReceive().Protect(Arg.Any<string>());
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().ExecuteSerializableAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
        await _eventReportRepository.DidNotReceive().Create(Arg.Any<EventReport>());
        await _targetRepository.DidNotReceive().Create(Arg.Any<EventReportTarget>());
        await _evidenceRepository.DidNotReceive().Create(Arg.Any<EventReportEvidence>());
        await _caseRepository.DidNotReceive().Create(Arg.Any<EventReportCase>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
        await _notificationPreferenceResolver.DidNotReceive().ResolveAsync(
            Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>());
        await _recipientNotificationMaterializer.DidNotReceive().MaterializeInCurrentTransactionAsync(
            Arg.Any<RecipientNotificationMaterialization>(), Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments("Correction", "event_correction_suggestion")]
    [Arguments("UnsafeExternalLink", "unsafe_external_link")]
    [Arguments("LegalOrCopyright", "legal_or_copyright_complaint")]
    public async Task Handle_RemedyChannelBypassesDisabledGeneralIntakeAndPersistsLocally(
        string channelName,
        string expectedSubcategory)
    {
        PropertyInfo? channelProperty = typeof(SubmitEventReportCommand)
            .GetProperty("SubmissionChannel", BindingFlags.Public | BindingFlags.Instance);
        await Assert.That(channelProperty).IsNotNull();
        if (channelProperty is null)
            return;

        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        Actor actor = CreateActor(tenantId, userId);
        Explore.Domain.Event @event = CreateEvent(
            tenantId,
            actor.Id,
            EventStatusEnum.Published);
        var createdReports = new List<EventReport>();
        var createdMessages = new List<OutboxMessage>();
        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _intakeGuard.ResolveAsync(tenantId, CancellationToken.None)
            .Returns(DisabledIntakeDecision());
        _eventRepository.GetById(@event.Id).Returns(@event);
        _actorRepository.GetActorByUserIdAndTenantId(
                userId,
                tenantId,
                CancellationToken.None)
            .Returns(actor);
        _eventReportRepository.Create(Arg.Do<EventReport>(createdReports.Add))
            .Returns(call => call.Arg<EventReport>());
        _outboxRepository.Create(Arg.Do<OutboxMessage>(createdMessages.Add))
            .Returns(call => call.Arg<OutboxMessage>());
        SubmitEventReportCommand command = CreateCommand(@event.Id);
        channelProperty.SetValue(
            command,
            Enum.Parse(channelProperty.PropertyType, channelName));

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(
            command,
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _intakeGuard.DidNotReceive()
            .ResolveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await Assert.That(createdReports).Count().IsEqualTo(1);
        await Assert.That(createdReports[0].SubcategoryCode)
            .IsEqualTo(expectedSubcategory);
        await Assert.That(createdMessages).Count().IsEqualTo(1);
    }

    [Test]
    [Arguments("event_correction_suggestion")]
    [Arguments("unsafe_external_link")]
    [Arguments("legal_or_copyright_complaint")]
    public async Task Handle_GeneralChannelCannotSpoofReservedRemedySubcategory(
        string reservedSubcategory)
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        Actor actor = CreateActor(tenantId, userId);
        Explore.Domain.Event @event = CreateEvent(
            tenantId,
            actor.Id,
            EventStatusEnum.Published);
        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _eventRepository.GetById(@event.Id).Returns(@event);
        _actorRepository.GetActorByUserIdAndTenantId(
                userId,
                tenantId,
                CancellationToken.None)
            .Returns(actor);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(
            CreateCommand(@event.Id, subcategoryCode: reservedSubcategory),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(
                EventReportReasonCodePolicy.InvalidReasonCodeFailureCode);
        await _eventReportRepository.DidNotReceive()
            .Create(Arg.Any<EventReport>());
    }

    [Test]
    public async Task Handle_WhenTenantIsEmpty_ReturnsTenantUnresolvedWithoutIntakeResolution()
    {
        _tenantContext.TenantId.Returns(Guid.Empty);

        var result = await CreateHandler().Handle(CreateCommand(Guid.NewGuid()), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.TenantUnresolved);
        await _intakeGuard.DidNotReceive().ResolveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_NullRequestReturnsValidationFailure()
    {
        Guid tenantId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(tenantId);
        var command = new SubmitEventReportCommand
        {
            Request = null!
        };

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(
            command,
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(EventReportFailureCodes.ValidationFailed);
        await _intakeGuard.DidNotReceive()
            .ResolveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenIntakeIsEnabled_WritesCanonicalLocalReportGraphAndOutboxInsideTransaction()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actor = CreateActor(tenantId, userId);
        var @event = CreateEvent(tenantId, actor.Id, EventStatusEnum.Published);
        var createdReports = new List<EventReport>();
        var createdTargets = new List<EventReportTarget>();
        var createdEvidence = new List<EventReportEvidence>();
        var createdCases = new List<EventReportCase>();
        var createdMessages = new List<OutboxMessage>();
        bool duplicateReadInsideTransaction = false;
        bool reporterQuotaReadInsideTransaction = false;
        bool eventQuotaReadInsideTransaction = false;

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _eventRepository.GetById(@event.Id).Returns(@event);
        _actorRepository.GetActorByUserIdAndTenantId(userId, tenantId).Returns(actor);
        _eventReportRepository.ExistsByReporterAndEventAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<Guid?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                duplicateReadInsideTransaction = _transactionActive;
                return false;
            });
        _eventReportRepository.CountByReporterSinceAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<Guid?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                reporterQuotaReadInsideTransaction = _transactionActive;
                return 0;
            });
        _eventReportRepository.CountByReporterAndEventSinceAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<Guid?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                eventQuotaReadInsideTransaction = _transactionActive;
                return 0;
            });
        _eventReportRepository.Create(Arg.Do<EventReport>(createdReports.Add))
            .Returns(call => call.Arg<EventReport>());
        _targetRepository.Create(Arg.Do<EventReportTarget>(createdTargets.Add))
            .Returns(call => call.Arg<EventReportTarget>());
        _evidenceRepository.Create(Arg.Do<EventReportEvidence>(createdEvidence.Add))
            .Returns(call => call.Arg<EventReportEvidence>());
        _caseRepository.Create(Arg.Do<EventReportCase>(createdCases.Add))
            .Returns(call => call.Arg<EventReportCase>());
        _outboxRepository.Create(Arg.Do<OutboxMessage>(createdMessages.Add))
            .Returns(call => call.Arg<OutboxMessage>());

        var command = CreateCommand(@event.Id);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(createdReports).Count().IsEqualTo(1);
        await Assert.That(createdTargets).Count().IsEqualTo(1);
        await Assert.That(createdEvidence).Count().IsEqualTo(1);
        await Assert.That(createdCases).Count().IsEqualTo(1);
        await Assert.That(createdMessages).Count().IsEqualTo(1);
        await Assert.That(_receiptMaterializations).Count().IsEqualTo(1);
        await Assert.That(_receiptMaterializedInsideTransaction).IsTrue();
        await Assert.That(duplicateReadInsideTransaction).IsTrue();
        await Assert.That(reporterQuotaReadInsideTransaction).IsTrue();
        await Assert.That(eventQuotaReadInsideTransaction).IsTrue();

        var report = createdReports.Single();
        await Assert.That(report.Id).IsEqualTo(result.Id);
        await Assert.That(report.TenantId).IsEqualTo(tenantId);
        await Assert.That(report.EventId).IsEqualTo(@event.Id);
        await Assert.That(report.ReporterUserId).IsEqualTo(userId);
        await Assert.That(report.ReporterActorId).IsEqualTo(actor.Id);
        await Assert.That(report.ReporterKind).IsEqualTo(EventReporterKind.AuthenticatedUser);
        await Assert.That(report.ReasonCode).IsEqualTo("spam");
        await Assert.That(report.Priority).IsEqualTo(EventReportPriority.Urgent);
        await Assert.That(report.Status).IsEqualTo(EventReportStatus.Submitted);
        await Assert.That(report.ReportCaseUpdatesConsent).IsTrue();
        await Assert.That(report.ReportFollowUpContactConsent).IsFalse();

        var target = createdTargets.Single();
        await Assert.That(target.TenantId).IsEqualTo(tenantId);
        await Assert.That(target.ReportId).IsEqualTo(report.Id);
        await Assert.That(target.TargetKind).IsEqualTo(EventReportTargetKind.Event);
        await Assert.That(target.TargetId).IsEqualTo(@event.Id);

        var evidence = createdEvidence.Single();
        await Assert.That(evidence.TenantId).IsEqualTo(tenantId);
        await Assert.That(evidence.ReportId).IsEqualTo(report.Id);
        await Assert.That(evidence.TextBodyEncrypted).IsEqualTo("protected:Unsafe organizer behavior");
        await Assert.That(evidence.Classification).IsEqualTo(EventReportEvidenceClassification.Sensitive);
        await Assert.That(evidence.CreatedByUserId).IsEqualTo(userId);
        await Assert.That(evidence.RetentionUntil).IsNotNull();

        var reportCase = createdCases.Single();
        await Assert.That(reportCase.TenantId).IsEqualTo(tenantId);
        await Assert.That(reportCase.ReportId).IsEqualTo(report.Id);
        await Assert.That(reportCase.QueueCode).IsEqualTo("default");
        await Assert.That(reportCase.Priority).IsEqualTo(EventReportPriority.Urgent);
        await Assert.That(reportCase.Status).IsEqualTo(EventReportCaseStatus.Open);

        var message = createdMessages.Single();
        await Assert.That(message.EventType).IsEqualTo(EventReportOutboxMessageFactory.EventReportProviderSyncRequestedEventType);
        await Assert.That(message.AggregateId).IsEqualTo(report.Id);
        await Assert.That(message.Status).IsEqualTo(OutboxMessageStatus.Pending);
        await Assert.That(message.Payload).DoesNotContain("Unsafe organizer behavior");
        await Assert.That(message.Payload).DoesNotContain(command.ReporterIpHash!);
        await Assert.That(message.Payload).DoesNotContain(command.ReporterUserAgentHash!);

        var payload = JsonSerializer.Deserialize<EventReportProviderSyncRequested>(message.Payload!);
        await Assert.That(payload).IsNotNull();
        await Assert.That(payload!.TenantId).IsEqualTo(tenantId);
        await Assert.That(payload.ReportId).IsEqualTo(report.Id);
        await Assert.That(payload.EventId).IsEqualTo(@event.Id);
        await Assert.That(payload.CaseId).IsEqualTo(reportCase.Id);
        await Assert.That(payload.CaseConcurrencyStamp).IsEqualTo(reportCase.ConcurrencyStamp);
        await Assert.That(payload.ReasonCode).IsEqualTo("spam");
        await Assert.That(payload.CorrelationId).IsEqualTo("correlation-123");

        RecipientNotificationMaterialization receipt = _receiptMaterializations.Single();
        await Assert.That(receipt.Intent.Category).IsEqualTo(Explore.Application.Notifications.NotificationCategory.TrustSafetyReporting);
        await Assert.That(receipt.Intent.RecipientKind).IsEqualTo(nameof(NotificationRecipientKindEnum.Reporter));
        await Assert.That(receipt.Intent.TemplateKey).IsEqualTo(ReportReceiptNotificationFactory.TemplateKey);
        await Assert.That(receipt.Intent.ReportId).IsEqualTo(report.Id);
        await Assert.That(receipt.Intent.EventId).IsEqualTo(@event.Id);
        await Assert.That(receipt.DeliveryPolicy).IsEqualTo(NotificationDeliveryPolicyEnum.ReportCaseUpdate);
        await Assert.That(receipt.InApp).IsNotNull();
        await Assert.That(receipt.InApp!.IsRequired).IsTrue();
        await Assert.That(receipt.Email).IsNotNull();
        await Assert.That(receipt.Email!.Kind).IsEqualTo(EmailDispatchKind.ReportReceipt);
        await Assert.That(receipt.Email.RecipientEmail).IsEqualTo("reporter@example.test");
        await Assert.That(receipt.Email.Subject).IsEqualTo("We received your event report");
        await Assert.That(receipt.Email.PlainTextBody).Contains("within 48 hours");
        await Assert.That(receipt.ConsentPurpose).IsEqualTo(ReportEmailConsentPurposeCodes.CaseUpdates);
        await Assert.That(receipt.PreferenceCategoryCode).IsEqualTo(NotificationPreferenceCategoryCodes.TrustSafety);
        await Assert.That(receipt.MaterializedAt).IsNotNull();
        await Assert.That(receipt.InAppNotificationId).IsNotNull();
        await Assert.That(receipt.InAppDeliveryId).IsNotNull();
        await Assert.That(receipt.EmailDeliveryId).IsNotNull();
        RecipientNotificationMaterialization replay = new ReportReceiptNotificationFactory().Create(
            report,
            EventReportSubmissionOptions.DefaultCaseSlaHours,
            RecipientEmailAddressResolver.Resolve(CreateUser(userId), userId),
            true,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            receipt.MaterializedAt!.Value);
        await Assert.That(replay.Intent.DeduplicationKey).IsEqualTo(receipt.Intent.DeduplicationKey);

        await _unitOfWork.Received(1)
            .ExecuteSerializableAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
        await _intakeGuard.Received(1).ResolveAsync(tenantId, CancellationToken.None);
    }

    [Test]
    public async Task Handle_WhenReasonCodeIsInvalid_ReturnsValidationFailureBeforeLoadingEvent()
    {
        _tenantContext.TenantId.Returns(Guid.NewGuid());

        var result = await CreateHandler().Handle(CreateCommand(Guid.NewGuid(), reasonCode: "unsupported"), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportReasonCodePolicy.InvalidReasonCodeFailureCode);
        await _eventRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
    }


    [Test]
    public async Task Handle_WhenFenceAlreadyExists_ReturnsMaskedFailureWithoutReadsOrWrites()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actor = CreateActor(tenantId, userId);
        var @event = CreateEvent(tenantId, actor.Id, EventStatusEnum.Published);
        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _privacyErasureStateRepository.GetBySubjectAsync(userId, Arg.Any<CancellationToken>())
            .Returns(CreateFencedSaga(userId));

        var result = await CreateHandler().Handle(CreateCommand(@event.Id), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("privacy_erasure_fenced");
        await Assert.That(result.Errors).IsNull();
        await _eventRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
        await _eventReportRepository.DidNotReceive().Create(Arg.Any<EventReport>());
        await _evidenceRepository.DidNotReceive().Create(Arg.Any<EventReportEvidence>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
        await _recipientNotificationMaterializer.DidNotReceive()
            .MaterializeInCurrentTransactionAsync(Arg.Any<RecipientNotificationMaterialization>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenFencedAndGeneralIntakeDisabled_ReturnsMaskedFence()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _intakeGuard.ResolveAsync(
                tenantId,
                CancellationToken.None)
            .Returns(DisabledIntakeDecision());
        _privacyErasureStateRepository.GetBySubjectAsync(
                userId,
                CancellationToken.None)
            .Returns(CreateFencedSaga(userId));

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(
            CreateCommand(Guid.CreateVersion7()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo("privacy_erasure_fenced");
        await _intakeGuard.DidNotReceive()
            .ResolveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenFenceAppearsDuringIntakeResolution_ReturnsMaskedFence()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _intakeGuard.ResolveAsync(
                tenantId,
                CancellationToken.None)
            .Returns(DisabledIntakeDecision());
        _privacyErasureStateRepository.GetBySubjectAsync(
                userId,
                Arg.Any<CancellationToken>())
            .Returns((PrivacyErasureSaga?)null, CreateFencedSaga(userId));

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(
            CreateCommand(Guid.CreateVersion7()),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo("privacy_erasure_fenced");
        await _privacyErasureStateRepository.Received(2)
            .GetBySubjectAsync(
                userId,
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenFenceAppearsDuringValidationMasksDetailedErrorsAndSkipsEventLookup()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _privacyErasureStateRepository.GetBySubjectAsync(userId, Arg.Any<CancellationToken>())
            .Returns((PrivacyErasureSaga?)null, CreateFencedSaga(userId));

        var result = await CreateHandler().Handle(CreateCommand(Guid.NewGuid(), reasonCode: "unsupported"), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("privacy_erasure_fenced");
        await Assert.That(result.Errors).IsNull();
        await _eventRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenDuplicateExists_ReturnsDuplicateFailureBeforeQuotaChecks()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actor = CreateActor(tenantId, userId);
        var @event = CreateEvent(tenantId, actor.Id, EventStatusEnum.Published);

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _eventRepository.GetById(@event.Id).Returns(@event);
        _actorRepository.GetActorByUserIdAndTenantId(userId, tenantId).Returns(actor);
        _eventReportRepository.ExistsByReporterAndEventAsync(
                tenantId,
                @event.Id,
                userId,
                actor.Id,
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                "spam",
                "organizer",
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateHandler().Handle(CreateCommand(@event.Id), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_report_duplicate");
        await _eventReportRepository.DidNotReceive().CountByReporterSinceAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenReporterHourlyQuotaIsExceeded_ReturnsStructuredQuotaFailure()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actor = CreateActor(tenantId, userId);
        var @event = CreateEvent(tenantId, actor.Id, EventStatusEnum.Published);
        _options.MaxReportsPerUserPerHour = 1;

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _eventRepository.GetById(@event.Id).Returns(@event);
        _actorRepository.GetActorByUserIdAndTenantId(userId, tenantId).Returns(actor);
        _eventReportRepository.CountByReporterSinceAsync(
                tenantId,
                userId,
                actor.Id,
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(1);

        var result = await CreateHandler().Handle(CreateCommand(@event.Id), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.QuotaExceeded);
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await Assert.That(result.QuotaExceeded!.QuotaKey).IsEqualTo("reporting.max_reports_per_user_per_hour");
        await Assert.That(result.QuotaExceeded.Scope).IsEqualTo("event_report_reporter");
        await _eventReportRepository.DidNotReceive().CountByReporterAndEventSinceAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenReporterEventDailyQuotaIsExceeded_ReturnsReporterEventQuotaFailure()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actor = CreateActor(tenantId, userId);
        var @event = CreateEvent(tenantId, actor.Id, EventStatusEnum.Published);
        _options.MaxReportsPerEventPerUserPerDay = 3;

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _eventRepository.GetById(@event.Id).Returns(@event);
        _actorRepository.GetActorByUserIdAndTenantId(userId, tenantId).Returns(actor);
        _eventReportRepository.CountByReporterAndEventSinceAsync(
                tenantId,
                @event.Id,
                userId,
                actor.Id,
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(3);

        var result = await CreateHandler().Handle(CreateCommand(@event.Id), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.QuotaExceeded);
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await Assert.That(result.QuotaExceeded!.QuotaKey).IsEqualTo("reporting.max_reports_per_event_per_user_per_day");
        await Assert.That(result.QuotaExceeded.Scope).IsEqualTo("event_report_reporter_event");
        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenEventIsNotPublished_ReturnsInvalidStatusFailureBeforeReportWrite()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actor = CreateActor(tenantId, userId);
        var @event = CreateEvent(tenantId, actor.Id, EventStatusEnum.Draft);

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await CreateHandler().Handle(CreateCommand(@event.Id), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_report_event_invalid_status");
        await _eventReportRepository.DidNotReceive().Create(Arg.Any<EventReport>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(EventStatusEnum.Published)]
    [Arguments(EventStatusEnum.Draft)]
    public async Task Handle_WhenEventIsNotPubliclyEligible_ReturnsNotFound(
        EventStatusEnum status)
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        Actor actor = CreateActor(tenantId, userId);
        Explore.Domain.Event @event = CreateEvent(
            tenantId,
            actor.Id,
            status);
        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _eventRepository.GetById(@event.Id).Returns(@event);
        _eventRepository.IsPubliclyEligibleAsync(
                tenantId,
                @event.Id,
                Arg.Any<CancellationToken>())
            .Returns(false);
        _actorRepository.GetActorByUserIdAndTenantId(
                userId,
                tenantId,
                CancellationToken.None)
            .Returns(actor);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(
            CreateCommand(@event.Id),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(EventReportFailureCodes.EventNotFound);
        await _eventReportRepository.DidNotReceive()
            .Create(Arg.Any<EventReport>());
    }

    [Test]
    public async Task Handle_WhenEligibilityChangesInsideTransaction_WritesNothing()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        Actor actor = CreateActor(tenantId, userId);
        Explore.Domain.Event @event = CreateEvent(
            tenantId,
            actor.Id,
            EventStatusEnum.Published);
        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _eventRepository.GetById(@event.Id).Returns(@event);
        _eventRepository.IsPubliclyEligibleAsync(
                tenantId,
                @event.Id,
                Arg.Any<CancellationToken>())
            .Returns(true, false);
        _actorRepository.GetActorByUserIdAndTenantId(
                userId,
                tenantId,
                CancellationToken.None)
            .Returns(actor);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(
            CreateCommand(@event.Id),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(EventReportFailureCodes.EventNotFound);
        await _eventRepository.Received(2).IsPubliclyEligibleAsync(
            tenantId,
            @event.Id,
            Arg.Any<CancellationToken>());
        await _eventReportRepository.DidNotReceive()
            .Create(Arg.Any<EventReport>());
        await _outboxRepository.DidNotReceive()
            .Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task Handle_WhenAnonymousReportingIsAllowed_ForcesBothCommunicationConsentsOff()
    {
        var tenantId = Guid.NewGuid();
        var @event = CreateEvent(tenantId, Guid.CreateVersion7(), EventStatusEnum.Published);
        var createdReports = new List<EventReport>();
        _options.RequireAuthenticatedReporter = false;
        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns((Guid?)null);
        _eventRepository.GetById(@event.Id).Returns(@event);
        _eventReportRepository.Create(Arg.Do<EventReport>(createdReports.Add))
            .Returns(call => call.Arg<EventReport>());

        var result = await CreateHandler().Handle(CreateCommand(@event.Id), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        var report = createdReports.Single();
        await Assert.That(report.ReporterKind).IsEqualTo(EventReporterKind.Anonymous);
        await Assert.That(report.ReportCaseUpdatesConsent).IsFalse();
        await Assert.That(report.ReportFollowUpContactConsent).IsFalse();
        await Assert.That(_receiptMaterializations).IsEmpty();
        await _recipientNotificationMaterializer.DidNotReceive()
            .MaterializeInCurrentTransactionAsync(
                Arg.Any<RecipientNotificationMaterialization>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenCaseUpdateConsentIsNotGranted_RecordsOneTypedEmailSkip()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        Explore.Domain.Event @event = ConfigureAcceptedAuthenticatedReport(tenantId, userId, CreateUser(userId));
        SubmitEventReportCommand command = CreateCommand(@event.Id);
        command = command with { Request = command.Request with { ReportCaseUpdatesConsent = false } };

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        RecipientNotificationMaterialization receipt = _receiptMaterializations.Single();
        await Assert.That(receipt.InApp).IsNotNull();
        await Assert.That(receipt.Email).IsNull();
        await Assert.That(receipt.IncludeEmailChannel).IsTrue();
        await Assert.That(receipt.EmailSkipReason).IsEqualTo(ReportReceiptNotificationFactory.ConsentNotGrantedSkipReason);
    }

    [Test]
    [Arguments(false, "reporter@example.test", RecipientEmailAddressResolver.RecipientEmailUnverified)]
    [Arguments(true, "", RecipientEmailAddressResolver.RecipientEmailMissing)]
    public async Task Handle_WhenPersistedEmailIsNotEligible_RecordsOneTypedEmailSkip(
        bool emailVerified,
        string email,
        string expectedSkipReason)
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        Explore.Domain.Event @event = ConfigureAcceptedAuthenticatedReport(
            tenantId,
            userId,
            CreateUser(userId, email, emailVerified));

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(
            CreateCommand(@event.Id),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        RecipientNotificationMaterialization receipt = _receiptMaterializations.Single();
        await Assert.That(receipt.InApp).IsNotNull();
        await Assert.That(receipt.Email).IsNull();
        await Assert.That(receipt.EmailSkipReason).IsEqualTo(expectedSkipReason);
    }

    [Test]
    public async Task Handle_WhenTrustSafetyEmailPreferenceIsDisabled_RecordsOneTypedEmailSkip()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        Explore.Domain.Event @event = ConfigureAcceptedAuthenticatedReport(tenantId, userId, CreateUser(userId));
        _notificationPreferenceResolver.ResolveAsync(
                Arg.Any<NotificationPreferenceResolveRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                NotificationPreferenceResolveRequest request = call.ArgAt<NotificationPreferenceResolveRequest>(0);
                return new NotificationPreferenceDecision(
                    request.CategoryCode,
                    request.ChannelCode,
                    false,
                    false,
                    false,
                    false,
                    "User",
                    null);
            });

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(
            CreateCommand(@event.Id),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        RecipientNotificationMaterialization receipt = _receiptMaterializations.Single();
        await Assert.That(receipt.InApp).IsNotNull();
        await Assert.That(receipt.Email).IsNull();
        await Assert.That(receipt.EmailSkipReason).IsEqualTo(ReportReceiptNotificationFactory.PreferenceDisabledSkipReason);
    }

    [Test]
    public async Task Handle_WhenReceiptMaterializationFails_PropagatesBeforeSuccessfulCompletion()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        Explore.Domain.Event @event = ConfigureAcceptedAuthenticatedReport(tenantId, userId, CreateUser(userId));
        _recipientNotificationMaterializer.MaterializeInCurrentTransactionAsync(
                Arg.Any<RecipientNotificationMaterialization>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<RecipientNotificationMaterializationResult>>(
                _ => throw new InvalidOperationException("receipt graph failure"));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateHandler().Handle(CreateCommand(@event.Id), CancellationToken.None));

        await Assert.That(exception.Message).IsEqualTo("receipt graph failure");
        await _unitOfWork.Received(1)
            .ExecuteSerializableAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_SnapshotsBoundedSlaAndExcludesSensitiveSubmissionDataFromReceiptCopy()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        Explore.Domain.Event @event = ConfigureAcceptedAuthenticatedReport(tenantId, userId, CreateUser(userId));
        _options.CaseSlaHours = EventReportSubmissionOptions.MaxCaseSlaHours + 1;
        SubmitEventReportCommand command = CreateCommand(@event.Id);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        RecipientNotificationMaterialization receipt = _receiptMaterializations.Single();
        string copy = string.Join(
            "\n",
            receipt.InApp!.Title,
            receipt.InApp.Body,
            receipt.Email!.Subject,
            receipt.Email.PlainTextBody,
            receipt.Email.HtmlBody);
        await Assert.That(copy).Contains($"within {EventReportSubmissionOptions.MaxCaseSlaHours} hours");
        await Assert.That(receipt.Intent.SafePayloadReference).Contains(
            $"sla-hours:{EventReportSubmissionOptions.MaxCaseSlaHours}");
        await Assert.That(copy).DoesNotContain(command.Request.ReporterText);
        await Assert.That(copy).DoesNotContain(command.ReporterIpHash!);
        await Assert.That(copy).DoesNotContain(command.ReporterUserAgentHash!);
        await Assert.That(copy).DoesNotContain(@event.Title);
        await Assert.That(copy).DoesNotContain("http");
    }

    private SubmitEventReportCommandHandler CreateHandler()
    {
        return new SubmitEventReportCommandHandler(
            _eventRepository,
            _eventReportRepository,
            _targetRepository,
            _evidenceRepository,
            _caseRepository,
            _actorRepository,
            _userRepository,
            _outboxRepository,
            _notificationPreferenceResolver,
            _recipientNotificationMaterializer,
            new ReportReceiptNotificationFactory(),
            _unitOfWork,
            _tenantContext,
            _intakeGuard,
            _currentUserService,
            _privacyErasureStateRepository,
            _evidenceProtector,
            _metrics,
            Options.Create(_options));
    }

    private static EventReportingIntakeDecision EnabledIntakeDecision() => new(
        TenantResolved: true,
        IntakeEnabled: true,
        ReasonCode: "event_reporting_intake_enabled",
        Message: string.Empty);

    private static EventReportingIntakeDecision DisabledIntakeDecision() => new(
        TenantResolved: true,
        IntakeEnabled: false,
        ReasonCode: EventReportFailureCodes.IntakeDisabled,
        Message: string.Empty);

    private Explore.Domain.Event ConfigureAcceptedAuthenticatedReport(Guid tenantId, Guid userId, User user)
    {
        Actor actor = CreateActor(tenantId, userId);
        Explore.Domain.Event @event = CreateEvent(tenantId, actor.Id, EventStatusEnum.Published);
        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _eventRepository.GetById(@event.Id).Returns(@event);
        _actorRepository.GetActorByUserIdAndTenantId(userId, tenantId).Returns(actor);
        _userRepository.GetUserWithDetails(userId, Arg.Any<CancellationToken>()).Returns(user);
        return @event;
    }

    private static PrivacyErasureSaga CreateFencedSaga(Guid userId)
    {
        DateTime nowUtc = DateTime.UtcNow;
        PrivacyErasureIntent intent = PrivacyErasureIntent.Record(
            Guid.CreateVersion7(),
            1,
            PrivacyErasureSubjectKind.User,
            userId,
            PrivacyErasureReasonCode.AccountDeletion,
            1,
            nowUtc,
            nowUtc);
        return PrivacyErasureSaga.Start(intent, 1, new byte[32], nowUtc.AddMinutes(5), nowUtc);
    }

    private static SubmitEventReportCommand CreateCommand(
        Guid eventId,
        string reasonCode = "spam",
        string? subcategoryCode = "organizer")
    {
        return new SubmitEventReportCommand
        {
            Request = new SubmitEventReportDto
            {
                EventId = eventId,
                ReasonCode = reasonCode,
                SubcategoryCode = subcategoryCode,
                ReporterText = "Unsafe organizer behavior",
                SeverityHint = EventReportSeverityHint.Critical,
                ReportCaseUpdatesConsent = true,
                ReportFollowUpContactConsent = false,
                ReporterLocale = "en"
            },
            ReporterIpHash = "iphash-123",
            ReporterUserAgentHash = "uahash-456",
            CorrelationId = "correlation-123"
        };
    }

    private static Explore.Domain.Event CreateEvent(
        Guid tenantId,
        Guid actorId,
        EventStatusEnum status)
    {
        return new Explore.Domain.Event(status)
        {
            Id = Guid.CreateVersion7(),
            Title = "Reported Event",
            TenantId = tenantId,
            Tenant = null!,
            ActorId = actorId,
            Actor = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Digital,
            EventFormat = null!
        };
    }

    private static Actor CreateActor(Guid tenantId, Guid userId)
    {
        return new Actor
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            Pii = new ActorPii { DisplayName = "Reporter" }
        };
    }

    private static User CreateUser(
        Guid userId,
        string email = "reporter@example.test",
        bool? emailVerified = true)
    {
        var user = new User
        {
            Id = userId,
            EmailVerified = emailVerified,
            Pii = new UserPii
            {
                UserId = userId,
                Email = email,
                FirstName = "Report",
                LastName = "Submitter"
            }
        };
        user.Pii.User = user;
        return user;
    }

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
    }
}
