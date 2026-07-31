// ABOUTME: EF Core repository for Basic Dispatch Mode email outbox state, attempts, and receipts.
// ABOUTME: Uses affected-row conditional updates for optimistic claims and durable retry/dead-letter transitions.

using System.Data;
using System.Data.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Explore.Persistence.Repositories;

public class EmailDispatchOutboxRepository : IEmailDispatchOutboxRepository
{
    private const int MaxErrorLength = 2000;
    private const int MaxReceiptFailureLength = 1000;
    private const string SmtpProcessorCode = "smtp";
    private const string ClaimAdvisoryLockName = "email-dispatch-smtp-claim";
    private const string AcceptedSettlementUnknownCategory = "accepted_settlement_unknown";
    private const string AcceptedSettlementUnknownMessage = "SMTP accepted the message, but local settlement is uncertain. Automatic resend is disabled pending reconciliation.";
    private const string ProviderHandoffStarted = "provider_handoff_started";
    private const string ReminderSupersededProviderStatus = "superseded";
    private const string ReminderSupersededMessage = "The reminder was superseded before SMTP provider handoff.";
    private const string ReminderRescheduleSql = """
        WITH candidates AS (
            SELECT intent.tenant_id,
                   intent.id AS notification_intent_id,
                    split_part(intent.safe_payload_reference, ':', 2)::uuid AS registration_order_id,
                   intent.recipient_user_id,
                   outbox.id AS outbox_id
            FROM notification_intents AS intent
            LEFT JOIN email_dispatch_outbox AS outbox
                ON outbox.tenant_id = intent.tenant_id
               AND outbox.notification_intent_id = intent.id
               AND outbox.kind = @reminder_kind
               AND outbox.is_deleted = FALSE
               AND outbox.content_redacted_at IS NULL
               AND outbox.status IN (@pending_status, @retry_status, @processing_status)
               AND (outbox.status <> @processing_status OR (
                   NOT EXISTS (
                       SELECT 1 FROM email_dispatch_attempts AS attempt
                       WHERE attempt.tenant_id = outbox.tenant_id
                         AND attempt.email_dispatch_outbox_id = outbox.id
                         AND attempt.attempt_number = outbox.attempt_count
                         AND attempt.failure_category = @provider_handoff_started)
                   AND NOT EXISTS (
                       SELECT 1 FROM email_dispatch_receipts AS receipt
                       WHERE receipt.tenant_id = outbox.tenant_id
                         AND receipt.email_dispatch_outbox_id = outbox.id
                         AND receipt.status = @processing_receipt_status)))
            WHERE intent.tenant_id = @tenant_id
              AND intent.event_id = @event_id
              AND intent.template_key = 'event.reminder'
              AND intent.is_deleted = FALSE
               AND intent.safe_payload_reference ~ '^registration-order:[0-9a-f]{32}:session:[0-9a-f]{32}$'
               AND (@registration_order_id IS NULL
                    OR split_part(intent.safe_payload_reference, ':', 2)::uuid = @registration_order_id)
              AND (@session_id IS NULL
                   OR right(intent.safe_payload_reference, length(@session_suffix)) = @session_suffix
                   OR EXISTS (
                       SELECT 1
                       FROM event_registrations AS affected_child
                       WHERE affected_child.tenant_id = intent.tenant_id
                          AND affected_child.registration_order_id = split_part(intent.safe_payload_reference, ':', 2)::uuid
                         AND affected_child.event_id = @event_id
                         AND affected_child.user_id = intent.recipient_user_id
                         AND affected_child.event_session_id = @session_id
                         AND affected_child.is_deleted = FALSE
                         AND affected_child.approval_status_id = @approved_status))
            FOR UPDATE OF intent
        ),
        selected AS (
            SELECT candidates.*,
                   eligible.session_id,
                   eligible.session_start,
                   eligible.local_start_date,
                   eligible.local_start_time
            FROM candidates
            LEFT JOIN LATERAL (
                SELECT session.id AS session_id,
                       session.start_time AS session_start,
                       session.local_start_date,
                       session.local_start_time
                FROM registration_orders AS parent
                INNER JOIN event_registrations AS child
                    ON child.tenant_id = parent.tenant_id
                    AND child.registration_order_id = parent.id
                INNER JOIN event_sessions AS session
                    ON session.tenant_id = child.tenant_id
                   AND session.id = child.event_session_id
                   AND session.event_id = parent.event_id
                INNER JOIN events AS event
                    ON event.tenant_id = parent.tenant_id
                   AND event.id = parent.event_id
                WHERE parent.tenant_id = candidates.tenant_id
                   AND parent.id = candidates.registration_order_id
                  AND parent.event_id = @event_id
                   AND parent.account_user_id = candidates.recipient_user_id
                  AND parent.is_deleted = FALSE
                   AND parent.registration_order_status_id = @confirmed_order_status
                   AND child.user_id = parent.account_user_id
                  AND child.event_id = parent.event_id
                  AND child.is_deleted = FALSE
                  AND child.approval_status_id = @approved_status
                  AND session.is_deleted = FALSE
                  AND session.event_session_status_id = @published_session_status
                  AND session.start_time IS NOT NULL
                  AND session.local_start_date IS NOT NULL
                  AND session.local_start_time IS NOT NULL
                  AND session.start_time > @changed_at
                  AND event.is_deleted = FALSE
                  AND event.event_status_id = @published_event_status
                  AND COALESCE(
                      NULLIF(btrim(event.event_time_zone_id), ''),
                      NULLIF(btrim(event.timezone), ''),
                      'UTC') = @time_zone_id
                ORDER BY session.start_time, session.id
                LIMIT 1
            ) AS eligible ON TRUE
        ),
        changed_outbox AS (
            UPDATE email_dispatch_outbox AS outbox
            SET status = CASE WHEN selected.session_id IS NULL THEN @skipped_status ELSE @pending_status END,
                next_attempt_at = CASE
                    WHEN selected.session_id IS NULL THEN NULL
                    ELSE GREATEST(
                        selected.session_start - (@lead_seconds * INTERVAL '1 second'),
                        @changed_at)
                END,
                processing_started_at = NULL,
                processing_lease_token = NULL,
                subject = CASE WHEN selected.session_id IS NULL THEN outbox.subject ELSE 'Reminder: ' || @title END,
                plain_text_body = CASE WHEN selected.session_id IS NULL THEN outbox.plain_text_body ELSE
                    'Assalamu alaykum,' || E'\n\n' ||
                    'This is a reminder that ' || @title || ' starts at ' ||
                    to_char(selected.local_start_date, 'YYYY-MM-DD') || ' ' ||
                    to_char(selected.local_start_time, 'HH24:MI') ||
                    ' [' || @time_zone_id || '] (' ||
                    to_char(selected.session_start AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.US"Z"') || ').' ||
                    E'\n\n' || 'Event Platform' END,
                html_body = CASE WHEN selected.session_id IS NULL THEN outbox.html_body ELSE
                    '<p>Assalamu alaykum,</p><p>This is a reminder that <strong>' || @html_title ||
                    '</strong> starts at ' ||
                    to_char(selected.local_start_date, 'YYYY-MM-DD') || ' ' ||
                    to_char(selected.local_start_time, 'HH24:MI') ||
                    ' [' || @html_time_zone_id || '] (' ||
                    to_char(selected.session_start AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.US"Z"') ||
                    ').</p><p>Event Platform</p>' END,
                correlation_id = CASE WHEN selected.session_id IS NULL THEN outbox.correlation_id ELSE
                    'event-reminder:v2:' || replace(selected.session_id::text, '-', '') || ':' ||
                    (round(extract(epoch FROM selected.session_start) * 10000000)::bigint + 621355968000000000)::text || ':' ||
                    @time_zone_id END,
                last_failure_category = CASE WHEN selected.session_id IS NULL THEN @reason ELSE NULL END,
                last_error = CASE WHEN selected.session_id IS NULL THEN @message ELSE NULL END,
                last_failure_at = CASE WHEN selected.session_id IS NULL THEN @changed_at ELSE NULL END,
                updated_at = @changed_at
            FROM selected
            WHERE outbox.tenant_id = selected.tenant_id AND outbox.id = selected.outbox_id
            RETURNING outbox.tenant_id,
                      outbox.id,
                      outbox.notification_intent_id,
                       outbox.registration_order_id,
                      selected.session_id,
                      selected.session_start
        ),
        changed_intent AS (
            UPDATE notification_intents AS intent
            SET status_id = CASE
                    WHEN selected.session_id IS NULL THEN @resolved_intent_status
                    WHEN selected.outbox_id IS NOT NULL THEN @dispatch_queued_intent_status
                    ELSE intent.status_id END,
                safe_payload_reference = CASE WHEN selected.session_id IS NULL THEN intent.safe_payload_reference ELSE
                     'registration-order:' || replace(selected.registration_order_id::text, '-', '') ||
                    ':session:' || replace(selected.session_id::text, '-', '') END,
                updated_at = @changed_at
            FROM selected
            WHERE intent.tenant_id = selected.tenant_id
              AND intent.id = selected.notification_intent_id
        ),
        changed_email_delivery AS (
            UPDATE notification_deliveries AS delivery
            SET status_id = CASE WHEN changed_outbox.session_id IS NULL THEN @superseded_delivery_status ELSE @queued_delivery_status END,
                provider_status = CASE WHEN changed_outbox.session_id IS NULL THEN @superseded_provider_status ELSE NULL END,
                failure_category = CASE WHEN changed_outbox.session_id IS NULL THEN @reason ELSE NULL END,
                completed_at = CASE WHEN changed_outbox.session_id IS NULL THEN @changed_at ELSE NULL END,
                updated_at = @changed_at
            FROM changed_outbox
            WHERE delivery.tenant_id = changed_outbox.tenant_id
              AND delivery.email_dispatch_outbox_id = changed_outbox.id
              AND delivery.channel_id = @email_channel_id
              AND delivery.status_id IN (@pending_delivery_status, @queued_delivery_status)
            RETURNING delivery.id
        ),
        changed_notification AS (
            UPDATE notifications AS notification
            SET is_deleted = selected.session_id IS NULL,
                deleted_at = CASE WHEN selected.session_id IS NULL THEN @changed_at ELSE NULL END,
                title = CASE WHEN selected.session_id IS NULL THEN notification.title ELSE 'Reminder: ' || @title END,
                body = CASE WHEN selected.session_id IS NULL THEN notification.body ELSE
                    @title || ' starts at ' ||
                    to_char(selected.local_start_date, 'YYYY-MM-DD') || ' ' ||
                    to_char(selected.local_start_time, 'HH24:MI') ||
                    ' [' || @time_zone_id || '] (' ||
                    to_char(selected.session_start AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.US"Z"') || ').' END,
                entity_id = CASE WHEN selected.session_id IS NULL THEN notification.entity_id ELSE selected.session_id::text END,
                updated_at = @changed_at
            FROM notification_deliveries AS delivery
            INNER JOIN selected
                ON selected.tenant_id = delivery.tenant_id
               AND selected.notification_intent_id = delivery.notification_intent_id
            WHERE delivery.channel_id = @in_app_channel_id
              AND delivery.notification_id = notification.id
              AND notification.tenant_id = selected.tenant_id
              AND notification.is_deleted = FALSE
            RETURNING notification.tenant_id, notification.id, notification.notification_intent_id
        ),
        changed_in_app_delivery AS (
            UPDATE notification_deliveries AS delivery
            SET status_id = @superseded_delivery_status,
                provider_status = @superseded_provider_status,
                failure_category = @reason,
                completed_at = @changed_at,
                updated_at = @changed_at
            FROM changed_notification
            INNER JOIN selected
                ON selected.tenant_id = changed_notification.tenant_id
               AND selected.notification_intent_id = changed_notification.notification_intent_id
               AND selected.session_id IS NULL
            WHERE delivery.tenant_id = changed_notification.tenant_id
              AND delivery.notification_id = changed_notification.id
              AND delivery.channel_id = @in_app_channel_id
              AND delivery.status_id IN (@pending_delivery_status, @queued_delivery_status)
            RETURNING delivery.id
        )
        SELECT
            (SELECT COUNT(*)::integer FROM changed_outbox),
            (SELECT COUNT(*)::integer FROM changed_email_delivery),
            (SELECT COUNT(*)::integer FROM changed_notification),
            (SELECT COUNT(*)::integer FROM changed_in_app_delivery);
        """;

    private readonly ExploreDbContext _dbContext;

    public EmailDispatchOutboxRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmailDispatchOutbox> Create(EmailDispatchOutbox entity, CancellationToken cancellationToken)
    {
        await _dbContext.EmailDispatchOutbox.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<IReadOnlyList<EmailDispatchOutbox>> ClaimPendingBatchAsync(
        EmailDispatchBatchClaimRequest request,
        CancellationToken cancellationToken)
    {
        ValidateBatchClaimRequest(request);
        var claimedIds = await ExecuteClaimTransactionAsync(
            request.LeaseToken,
            request.ClaimedAt,
            request.OptionalReminderBacklogHighWatermark,
            request.OptionalReminderBacklogLowWatermark,
            command => ExecuteBatchClaimAsync(command, request, cancellationToken),
            cancellationToken);

        return await LoadClaimedRowsAsync(claimedIds, cancellationToken);
    }

    public async Task<EmailDispatchOutbox?> TryClaimSpecificAsync(
        EmailDispatchSpecificClaimRequest request,
        CancellationToken cancellationToken)
    {
        ValidateSpecificClaimRequest(request);
        var claimedIds = await ExecuteClaimTransactionAsync(
            request.LeaseToken,
            request.ClaimedAt,
            request.OptionalReminderBacklogHighWatermark,
            request.OptionalReminderBacklogLowWatermark,
            command => ExecuteSpecificClaimAsync(command, request, cancellationToken),
            cancellationToken,
            request.TenantId,
            request.PublishEventId);

        if (claimedIds.Count == 0)
        {
            return null;
        }

        return (await LoadClaimedRowsAsync(claimedIds, cancellationToken)).Single();
    }

    public async Task<EventReminderStateChangeResult> SuppressEventRemindersInCurrentTransactionAsync(
        EventReminderSupersessionRequest request,
        CancellationToken cancellationToken)
    {
        NotificationFanoutPrecedenceLock.EnsureActivePostgresTransaction(_dbContext);
        if (request.TenantId == Guid.Empty
            || request.EventId == Guid.Empty
            || request.SupersededAt.Kind != DateTimeKind.Utc
            || string.IsNullOrWhiteSpace(request.ReasonCode)
            || request.ReasonCode.Length > 200)
        {
            throw new ArgumentException("Reminder supersession requires exact tenant/event authority, a UTC time, and a bounded reason.", nameof(request));
        }

        await NotificationFanoutPrecedenceLock.AcquireAsync(
            _dbContext,
            request.TenantId,
            request.EventId,
            cancellationToken);

        string? sessionSuffix = request.SessionId.HasValue ? $":session:{request.SessionId.Value:N}" : null;
        await using DbCommand command = CreateReminderCommand(
            """
            WITH reminder_intents AS (
                SELECT intent.tenant_id, intent.id AS notification_intent_id
                FROM notification_intents AS intent
                WHERE intent.tenant_id = @tenant_id
                  AND intent.event_id = @event_id
                  AND intent.template_key = 'event.reminder'
                  AND intent.is_deleted = FALSE
                   AND intent.safe_payload_reference ~ '^registration-order:[0-9a-f]{32}:session:[0-9a-f]{32}$'
                   AND (@registration_order_id IS NULL
                        OR split_part(intent.safe_payload_reference, ':', 2)::uuid = @registration_order_id)
                  AND (@session_suffix IS NULL
                       OR right(intent.safe_payload_reference, length(@session_suffix)) = @session_suffix)
            ),
            candidates AS (
                SELECT outbox.tenant_id, outbox.id, outbox.notification_intent_id
                FROM email_dispatch_outbox AS outbox
                INNER JOIN reminder_intents AS intent
                    ON intent.tenant_id = outbox.tenant_id
                   AND intent.notification_intent_id = outbox.notification_intent_id
                WHERE outbox.tenant_id = @tenant_id
                  AND outbox.event_id = @event_id
                  AND outbox.kind = @reminder_kind
                  AND outbox.is_deleted = FALSE
                  AND outbox.content_redacted_at IS NULL
                  AND outbox.status IN (@pending_status, @retry_status, @processing_status)
                  AND (outbox.status <> @processing_status OR (
                      NOT EXISTS (
                          SELECT 1 FROM email_dispatch_attempts AS attempt
                          WHERE attempt.tenant_id = outbox.tenant_id
                            AND attempt.email_dispatch_outbox_id = outbox.id
                            AND attempt.attempt_number = outbox.attempt_count
                            AND attempt.failure_category = @provider_handoff_started)
                      AND NOT EXISTS (
                          SELECT 1 FROM email_dispatch_receipts AS receipt
                          WHERE receipt.tenant_id = outbox.tenant_id
                            AND receipt.email_dispatch_outbox_id = outbox.id
                            AND receipt.status = @processing_receipt_status)))
                FOR UPDATE OF outbox
            ),
            suppressed_outbox AS (
                UPDATE email_dispatch_outbox AS outbox
                SET status = @skipped_status,
                    next_attempt_at = NULL,
                    processing_started_at = NULL,
                    processing_lease_token = NULL,
                    last_failure_category = @reason,
                    last_error = @message,
                    last_failure_at = @changed_at,
                    updated_at = @changed_at
                FROM candidates
                WHERE outbox.tenant_id = candidates.tenant_id AND outbox.id = candidates.id
                RETURNING outbox.tenant_id, outbox.id, outbox.notification_intent_id
            ),
            resolved_intent AS (
                UPDATE notification_intents AS intent
                SET status_id = @resolved_intent_status, updated_at = @changed_at
                FROM reminder_intents
                WHERE intent.tenant_id = reminder_intents.tenant_id
                  AND intent.id = reminder_intents.notification_intent_id
            ),
            superseded_email_delivery AS (
                UPDATE notification_deliveries AS delivery
                SET status_id = @superseded_delivery_status,
                    provider_status = @superseded_provider_status,
                    failure_category = @reason,
                    completed_at = @changed_at,
                    updated_at = @changed_at
                FROM suppressed_outbox
                WHERE delivery.tenant_id = suppressed_outbox.tenant_id
                  AND delivery.email_dispatch_outbox_id = suppressed_outbox.id
                  AND delivery.channel_id = @email_channel_id
                  AND delivery.status_id IN (@pending_delivery_status, @queued_delivery_status)
                RETURNING delivery.id
            ),
            suppressed_notification AS (
                UPDATE notifications AS notification
                SET is_deleted = TRUE, deleted_at = @changed_at, updated_at = @changed_at
                FROM notification_deliveries AS delivery
                INNER JOIN reminder_intents
                    ON reminder_intents.tenant_id = delivery.tenant_id
                   AND reminder_intents.notification_intent_id = delivery.notification_intent_id
                WHERE delivery.channel_id = @in_app_channel_id
                  AND delivery.notification_id = notification.id
                  AND notification.tenant_id = reminder_intents.tenant_id
                  AND notification.is_deleted = FALSE
                RETURNING notification.tenant_id, notification.id
            ),
            superseded_in_app_delivery AS (
                UPDATE notification_deliveries AS delivery
                SET status_id = @superseded_delivery_status,
                    provider_status = @superseded_provider_status,
                    failure_category = @reason,
                    completed_at = @changed_at,
                    updated_at = @changed_at
                FROM suppressed_notification
                WHERE delivery.tenant_id = suppressed_notification.tenant_id
                  AND delivery.notification_id = suppressed_notification.id
                  AND delivery.channel_id = @in_app_channel_id
                  AND delivery.status_id IN (@pending_delivery_status, @queued_delivery_status)
                RETURNING delivery.id
            )
            SELECT
                (SELECT COUNT(*)::integer FROM suppressed_outbox),
                (SELECT COUNT(*)::integer FROM superseded_email_delivery),
                (SELECT COUNT(*)::integer FROM suppressed_notification),
                (SELECT COUNT(*)::integer FROM superseded_in_app_delivery);
            """);
        AddReminderParameters(
            command,
            request.TenantId,
            request.EventId,
            request.RegistrationOrderId,
            request.SessionId,
            sessionSuffix,
            request.SupersededAt,
            request.ReasonCode);
        return await ReadReminderStateChangeAsync(command, cancellationToken);
    }

    public async Task<EventReminderStateChangeResult> RescheduleEventRemindersInCurrentTransactionAsync(
        EventReminderRescheduleRequest request,
        CancellationToken cancellationToken)
    {
        NotificationFanoutPrecedenceLock.EnsureActivePostgresTransaction(_dbContext);
        if (request.TenantId == Guid.Empty
            || request.EventId == Guid.Empty
            || request.ChangedAt.Kind != DateTimeKind.Utc
            || request.LeadTime <= TimeSpan.Zero)
        {
            throw new ArgumentException("Reminder rescheduling requires exact tenant/event authority, positive lead time, and a UTC time.", nameof(request));
        }

        await NotificationFanoutPrecedenceLock.AcquireAsync(_dbContext, request.TenantId, request.EventId, cancellationToken);
        string title = string.IsNullOrWhiteSpace(request.EventTitle) ? "the event" : request.EventTitle.Trim();
        string htmlTitle = System.Net.WebUtility.HtmlEncode(title);
        string timeZoneId = Explore.Domain.Services.Scheduling.ScheduleTimeZoneResolver.NormalizeOrUtc(
            request.EventTimeZoneId);
        string htmlTimeZoneId = System.Net.WebUtility.HtmlEncode(timeZoneId);
        string? sessionSuffix = request.SessionId.HasValue ? $":session:{request.SessionId.Value:N}" : null;
        await using DbCommand command = CreateReminderCommand(ReminderRescheduleSql);
        AddReminderParameters(
            command,
            request.TenantId,
            request.EventId,
            request.RegistrationOrderId,
            request.SessionId,
            sessionSuffix,
            request.ChangedAt,
            "event_reminder_schedule_changed");
        AddParameter(command, "lead_seconds", request.LeadTime.TotalSeconds, DbType.Double);
        AddParameter(command, "title", title, DbType.String);
        AddParameter(command, "html_title", htmlTitle, DbType.String);
        AddParameter(command, "time_zone_id", timeZoneId, DbType.String);
        AddParameter(command, "html_time_zone_id", htmlTimeZoneId, DbType.String);
        AddParameter(command, "published_event_status", (int)EventStatusEnum.Published, DbType.Int32);
        AddParameter(command, "approved_status", (int)ApprovalStatusEnum.Approved, DbType.Int32);
        AddParameter(command, "confirmed_order_status", (int)RegistrationOrderStatusEnum.Confirmed, DbType.Int32);
        AddParameter(command, "published_session_status", (int)EventSessionStatusEnum.Published, DbType.Int32);
        return await ReadReminderStateChangeAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailDispatchOutbox>> GetRabbitMqPublishBatch(
        int batchSize,
        DateTime now,
        DateTime retryAttemptsBefore,
        CancellationToken cancellationToken)
    {
        var pausedTenantIds = _dbContext.EmailDispatchTenantControls
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(control => control.IsPaused)
            .Select(control => control.TenantId);
        var processorPaused = _dbContext.EmailDispatchProcessorStates
            .AsNoTracking()
            .Any(state => state.ProcessorCode == SmtpProcessorCode && state.IsPaused);

        return await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(e => e.ContentRedactedAt == null
                && (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled)
                && (e.NextAttemptAt == null || e.NextAttemptAt <= now)
                && (e.RabbitMqLastPublishAttemptAt == null || e.RabbitMqLastPublishAttemptAt <= retryAttemptsBefore)
                && !processorPaused
                && !pausedTenantIds.Contains(e.TenantId))
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountDueDispatchAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        return ActiveDispatchRows()
            .CountAsync(e => (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled)
                && (e.NextAttemptAt == null || e.NextAttemptAt <= now), cancellationToken);
    }

    public Task<DateTime?> GetOldestDueCreatedAtAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        return ActiveDispatchRows()
            .Where(e => (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled)
                && (e.NextAttemptAt == null || e.NextAttemptAt <= now))
            .MinAsync(e => (DateTime?)e.CreatedAt, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountDueDispatchByTenantAsync(
        DateTime now,
        int tenantLimit,
        CancellationToken cancellationToken)
    {
        var rows = await ActiveDispatchRows()
            .Where(e => (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled)
                && (e.NextAttemptAt == null || e.NextAttemptAt <= now))
            .GroupBy(e => e.TenantId)
            .Select(group => new { TenantId = group.Key, Count = group.Count() })
            .OrderByDescending(row => row.Count)
            .ThenBy(row => row.TenantId)
            .Take(tenantLimit)
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.TenantId, row => row.Count);
    }

    public Task<int> CountRetryScheduledAsync(CancellationToken cancellationToken)
    {
        return ActiveDispatchRows()
            .CountAsync(e => e.Status == EmailDispatchStatus.RetryScheduled, cancellationToken);
    }

    public Task<int> CountStaleProcessingAsync(
        DateTime processingStartedBefore,
        CancellationToken cancellationToken)
    {
        return ActiveDispatchRows()
            .CountAsync(e => e.Status == EmailDispatchStatus.Processing
                && e.ProcessingStartedAt != null
                && e.ProcessingStartedAt <= processingStartedBefore, cancellationToken);
    }

    public Task<int> CountDeadLetteredAsync(CancellationToken cancellationToken)
    {
        return ActiveDispatchRows()
            .CountAsync(e => e.Status == EmailDispatchStatus.DeadLettered, cancellationToken);
    }

    public Task<int> CountUnknownAsync(CancellationToken cancellationToken)
    {
        return ActiveDispatchRows()
            .CountAsync(e => e.Status == EmailDispatchStatus.Unknown, cancellationToken);
    }

    public Task<int> CountParkedAsync(CancellationToken cancellationToken)
    {
        return ActiveDispatchRows()
            .CountAsync(e => e.Status == EmailDispatchStatus.Parked, cancellationToken);
    }

    public Task<bool> IsOptionalReminderDeferralActiveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.EmailDispatchProcessorStates
            .AsNoTracking()
            .AnyAsync(
                state => state.ProcessorCode == SmtpProcessorCode && state.OptionalRemindersDeferred,
                cancellationToken);
    }

    public Task<EmailDispatchProcessorState?> GetProcessorState(CancellationToken cancellationToken)
    {
        return _dbContext.EmailDispatchProcessorStates
            .AsNoTracking()
            .SingleOrDefaultAsync(state => state.ProcessorCode == SmtpProcessorCode, cancellationToken);
    }

    public async Task<EmailDispatchProcessorState> SetProcessorPauseState(
        bool isPaused,
        string? pauseReason,
        Guid? changedBy,
        DateTime changedAt,
        CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            _dbContext.ChangeTracker.Clear();
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await AcquireClaimLockAsync(transaction.GetDbTransaction(), cancellationToken);

            var storedReason = isPaused ? Truncate(pauseReason, 500) : null;
            var pausedAt = isPaused ? changedAt : (DateTime?)null;
            var pausedBy = isPaused ? changedBy : null;
            await _dbContext.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO email_dispatch_processor_states (
                    id, processor_code, is_paused, pause_reason, paused_at, paused_by,
                    optional_reminders_deferred, updated_at, updated_by)
                VALUES (
                    {{Guid.CreateVersion7()}}, {{SmtpProcessorCode}}, {{isPaused}}, {{storedReason}}, {{pausedAt}}, {{pausedBy}},
                    FALSE, {{changedAt}}, {{changedBy}})
                ON CONFLICT (processor_code) DO UPDATE
                SET is_paused = EXCLUDED.is_paused,
                    pause_reason = EXCLUDED.pause_reason,
                    paused_at = EXCLUDED.paused_at,
                    paused_by = EXCLUDED.paused_by,
                    updated_at = EXCLUDED.updated_at,
                    updated_by = EXCLUDED.updated_by
                """, cancellationToken);

            var state = await _dbContext.EmailDispatchProcessorStates
                .AsNoTracking()
                .SingleAsync(value => value.ProcessorCode == SmtpProcessorCode, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return state;
        });
    }

    public async Task<EmailDispatchProcessorState> SetGlobalSmtpRateLimitOverride(
        int? rateLimitPerMinute,
        Guid? changedBy,
        DateTime changedAt,
        CancellationToken cancellationToken)
    {
        if (rateLimitPerMinute is < 1 or > 100000)
        {
            throw new ArgumentOutOfRangeException(nameof(rateLimitPerMinute));
        }

        await _dbContext.Database.ExecuteSqlInterpolatedAsync($$"""
            INSERT INTO email_dispatch_processor_states (
                id, processor_code, is_paused, global_smtp_rate_limit_per_minute_override,
                optional_reminders_deferred, updated_at, updated_by)
            VALUES (
                {{Guid.CreateVersion7()}}, {{SmtpProcessorCode}}, FALSE, {{rateLimitPerMinute}},
                FALSE, {{changedAt}}, {{changedBy}})
            ON CONFLICT (processor_code) DO UPDATE
            SET global_smtp_rate_limit_per_minute_override = EXCLUDED.global_smtp_rate_limit_per_minute_override,
                smtp_available_tokens = NULL,
                smtp_refill_at = NULL,
                updated_at = EXCLUDED.updated_at,
                updated_by = EXCLUDED.updated_by
            """, cancellationToken);

        return await _dbContext.EmailDispatchProcessorStates
            .AsNoTracking()
            .SingleAsync(state => state.ProcessorCode == SmtpProcessorCode, cancellationToken);
    }

    private IQueryable<EmailDispatchOutbox> ActiveDispatchRows()
    {
        var pausedTenantIds = _dbContext.EmailDispatchTenantControls
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(control => control.IsPaused)
            .Select(control => control.TenantId);

        return _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(dispatch => dispatch.ContentRedactedAt == null
                && !pausedTenantIds.Contains(dispatch.TenantId));
    }

    public async Task<IReadOnlyList<EmailDispatchOutbox>> GetStatusRows(
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken)
    {
        return await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .AsNoTracking()
            .Where(dispatch => dispatch.TenantId == tenantId)
            .OrderByDescending(dispatch => dispatch.LastFailureAt ?? dispatch.SentAt ?? dispatch.UnknownAt ?? dispatch.ParkedAt ?? dispatch.CreatedAt)
            .ThenByDescending(dispatch => dispatch.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmailDispatchOutbox?> GetByTenantAndId(
        Guid tenantId,
        Guid outboxId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == outboxId, cancellationToken);
    }

    public async Task<EmailDispatchOutbox?> GetByTenantAndPublishEventId(
        Guid tenantId,
        Guid publishEventId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.PublishEventId == publishEventId, cancellationToken);
    }

    public async Task<bool> IsTenantPaused(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.EmailDispatchTenantControls
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .AsNoTracking()
            .AnyAsync(e => e.TenantId == tenantId && e.IsPaused, cancellationToken);
    }

    public async Task<EmailDispatchTenantControl> SetTenantPauseState(
        Guid tenantId,
        bool isPaused,
        string? pauseReason,
        Guid? changedBy,
        DateTime changedAt,
        CancellationToken cancellationToken)
    {
        var storedReason = isPaused ? Truncate(pauseReason, 500) : null;
        var pausedAt = isPaused ? changedAt : (DateTime?)null;
        var pausedBy = isPaused ? changedBy : null;
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($$"""
            INSERT INTO email_dispatch_tenant_controls (
                id, tenant_id, is_paused, pause_reason, paused_at, paused_by,
                created_at, created_by, updated_at, updated_by)
            VALUES (
                {{Guid.CreateVersion7()}}, {{tenantId}}, {{isPaused}}, {{storedReason}}, {{pausedAt}}, {{pausedBy}},
                {{changedAt}}, {{changedBy}}, {{changedAt}}, {{changedBy}})
            ON CONFLICT (tenant_id) DO UPDATE
            SET is_paused = EXCLUDED.is_paused,
                pause_reason = EXCLUDED.pause_reason,
                paused_at = EXCLUDED.paused_at,
                paused_by = EXCLUDED.paused_by,
                updated_at = EXCLUDED.updated_at,
                updated_by = EXCLUDED.updated_by
            """, cancellationToken);

        return await _dbContext.EmailDispatchTenantControls
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .AsNoTracking()
            .SingleAsync(control => control.TenantId == tenantId, cancellationToken);
    }

    public async Task<bool> TryParkForOperator(
        Guid tenantId,
        Guid outboxId,
        string reason,
        Guid? changedBy,
        DateTime parkedAt,
        CancellationToken cancellationToken)
    {
        var updated = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(e => e.TenantId == tenantId
                && e.Id == outboxId
                && e.ContentRedactedAt == null
                && e.Status != EmailDispatchStatus.Sent
                && e.Status != EmailDispatchStatus.Skipped
                && e.Status != EmailDispatchStatus.Parked
                && e.Status != EmailDispatchStatus.Processing
                && e.Status != EmailDispatchStatus.Unknown)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, EmailDispatchStatus.Parked)
                .SetProperty(e => e.ParkedAt, parkedAt)
                .SetProperty(e => e.NextAttemptAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.LastFailureCategory, "operator_parked")
                .SetProperty(e => e.LastError, Truncate(reason, MaxErrorLength))
                .SetProperty(e => e.LastFailureAt, parkedAt)
                .SetProperty(e => e.UpdatedAt, parkedAt)
                .SetProperty(e => e.UpdatedBy, changedBy), cancellationToken);

        return updated > 0;
    }

    public async Task<bool> TryReplayForOperator(
        Guid tenantId,
        Guid outboxId,
        Guid? changedBy,
        DateTime replayAt,
        CancellationToken cancellationToken)
    {
        var updated = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(e => e.TenantId == tenantId
                && e.Id == outboxId
                && e.ContentRedactedAt == null
                && (e.Status == EmailDispatchStatus.DeadLettered
                    || e.Status == EmailDispatchStatus.Parked
                    || e.Status == EmailDispatchStatus.RetryScheduled))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, EmailDispatchStatus.Pending)
                .SetProperty(e => e.NextAttemptAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.DeadLetteredAt, (DateTime?)null)
                .SetProperty(e => e.ParkedAt, (DateTime?)null)
                .SetProperty(e => e.UnknownAt, (DateTime?)null)
                .SetProperty(e => e.LastFailureCategory, (string?)null)
                .SetProperty(e => e.LastError, (string?)null)
                .SetProperty(e => e.LastFailureAt, (DateTime?)null)
                .SetProperty(e => e.RabbitMqLastPublishedAt, (DateTime?)null)
                .SetProperty(e => e.RabbitMqLastPublishAttemptAt, (DateTime?)null)
                .SetProperty(e => e.RabbitMqPublishAttemptCount, 0)
                .SetProperty(e => e.RabbitMqLastPublishFailureCategory, (string?)null)
                .SetProperty(e => e.UpdatedAt, replayAt)
                .SetProperty(e => e.UpdatedBy, changedBy), cancellationToken);

        if (updated == 0)
        {
            return false;
        }

        await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(receipt => receipt.TenantId == tenantId
                && receipt.EmailDispatchOutboxId == outboxId
                && receipt.Status != EmailDispatchReceiptStatus.Completed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(receipt => receipt.Status, EmailDispatchReceiptStatus.Received)
                .SetProperty(receipt => receipt.ConsumerId, (string?)null)
                .SetProperty(receipt => receipt.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(receipt => receipt.CompletedAt, (DateTime?)null)
                .SetProperty(receipt => receipt.FailedAt, (DateTime?)null)
                .SetProperty(receipt => receipt.FailureCode, (string?)null)
                .SetProperty(receipt => receipt.FailureMessage, (string?)null)
                .SetProperty(receipt => receipt.ProviderMessageId, (string?)null)
                .SetProperty(receipt => receipt.UpdatedAt, replayAt)
                .SetProperty(receipt => receipt.UpdatedBy, changedBy), cancellationToken);

        await _dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(delivery => delivery.TenantId == tenantId
                && delivery.EmailDispatchOutboxId == outboxId
                && delivery.ChannelId == (int)NotificationPreferenceChannelEnum.Email
                && delivery.StatusId != (int)NotificationDeliveryStatusEnum.Delivered)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(delivery => delivery.StatusId, (int)NotificationDeliveryStatusEnum.Queued)
                .SetProperty(delivery => delivery.ProviderMessageId, (string?)null)
                .SetProperty(delivery => delivery.ProviderStatus, "queued")
                .SetProperty(delivery => delivery.FailureCategory, (string?)null)
                .SetProperty(delivery => delivery.CompletedAt, (DateTime?)null)
                .SetProperty(delivery => delivery.UpdatedAt, replayAt)
                .SetProperty(delivery => delivery.UpdatedBy, changedBy), cancellationToken);

        return true;
    }

    public async Task<bool> TryResolveWithoutReplay(
        Guid tenantId,
        Guid outboxId,
        string reason,
        Guid? changedBy,
        DateTime resolvedAt,
        CancellationToken cancellationToken)
    {
        var updated = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(e => e.TenantId == tenantId
                && e.Id == outboxId
                && e.ContentRedactedAt == null
                && (e.Status == EmailDispatchStatus.DeadLettered
                    || e.Status == EmailDispatchStatus.Parked
                    || e.Status == EmailDispatchStatus.Unknown))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, EmailDispatchStatus.Skipped)
                .SetProperty(e => e.NextAttemptAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.LastFailureCategory, "operator_resolved_without_replay")
                .SetProperty(e => e.LastError, Truncate(reason, MaxErrorLength))
                .SetProperty(e => e.LastFailureAt, resolvedAt)
                .SetProperty(e => e.UpdatedAt, resolvedAt)
                .SetProperty(e => e.UpdatedBy, changedBy), cancellationToken);

        if (updated == 0)
        {
            return false;
        }

        await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(receipt => receipt.TenantId == tenantId
                && receipt.EmailDispatchOutboxId == outboxId
                && receipt.Status != EmailDispatchReceiptStatus.Completed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(receipt => receipt.Status, EmailDispatchReceiptStatus.Skipped)
                .SetProperty(receipt => receipt.CompletedAt, (DateTime?)null)
                .SetProperty(receipt => receipt.FailedAt, resolvedAt)
                .SetProperty(receipt => receipt.FailureCode, "operator_resolved_without_replay")
                .SetProperty(receipt => receipt.FailureMessage, Truncate(reason, MaxReceiptFailureLength))
                .SetProperty(receipt => receipt.UpdatedAt, resolvedAt)
                .SetProperty(receipt => receipt.UpdatedBy, changedBy), cancellationToken);

        await _dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(delivery => delivery.TenantId == tenantId
                && delivery.EmailDispatchOutboxId == outboxId
                && delivery.StatusId != (int)NotificationDeliveryStatusEnum.Delivered)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(delivery => delivery.StatusId, (int)NotificationDeliveryStatusEnum.Skipped)
                .SetProperty(delivery => delivery.ProviderStatus, "skipped")
                .SetProperty(delivery => delivery.FailureCategory, "operator_resolved_without_replay")
                .SetProperty(delivery => delivery.CompletedAt, resolvedAt)
                .SetProperty(delivery => delivery.UpdatedAt, resolvedAt)
                .SetProperty(delivery => delivery.UpdatedBy, changedBy), cancellationToken);

        return true;
    }

    public async Task<bool> TryReconcileUnknown(
        Guid tenantId,
        Guid outboxId,
        EmailDispatchUnknownReconciliationOutcome outcome,
        string reason,
        string? providerMessageId,
        Guid? changedBy,
        DateTime reconciledAt,
        CancellationToken cancellationToken)
    {
        var dispatch = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .AsNoTracking()
            .SingleOrDefaultAsync(outbox => outbox.TenantId == tenantId
                && outbox.Id == outboxId
                && outbox.ContentRedactedAt == null
                && outbox.Status == EmailDispatchStatus.Unknown, cancellationToken);
        if (dispatch is null)
        {
            return false;
        }

        var delivered = outcome == EmailDispatchUnknownReconciliationOutcome.Delivered;
        var storedProviderMessageId = delivered ? Truncate(providerMessageId, 500) : null;
        var reconciliationCategory = delivered
            ? "operator_reconciled_delivered"
            : "operator_reconciled_not_delivered";
        var outboxUpdated = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(outbox => outbox.TenantId == tenantId
                && outbox.Id == outboxId
                && outbox.ContentRedactedAt == null
                && outbox.Status == EmailDispatchStatus.Unknown
                && outbox.AttemptCount == dispatch.AttemptCount)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(outbox => outbox.Status, delivered ? EmailDispatchStatus.Sent : EmailDispatchStatus.Pending)
                .SetProperty(outbox => outbox.SentAt, delivered ? reconciledAt : (DateTime?)null)
                .SetProperty(outbox => outbox.UnknownAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProviderMessageId, storedProviderMessageId)
                .SetProperty(outbox => outbox.NextAttemptAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(outbox => outbox.LastFailureCategory, reconciliationCategory)
                .SetProperty(outbox => outbox.LastError, Truncate(reason, MaxErrorLength))
                .SetProperty(outbox => outbox.LastFailureAt, reconciledAt)
                .SetProperty(outbox => outbox.UpdatedAt, reconciledAt)
                .SetProperty(outbox => outbox.UpdatedBy, changedBy), cancellationToken);
        if (outboxUpdated == 0)
        {
            return false;
        }

        var attemptUpdated = await _dbContext.EmailDispatchAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(attempt => attempt.TenantId == tenantId
                && attempt.EmailDispatchOutboxId == outboxId
                && attempt.AttemptNumber == dispatch.AttemptCount
                && attempt.Outcome == EmailDispatchAttemptOutcome.Unknown)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(attempt => attempt.Outcome, delivered
                    ? EmailDispatchAttemptOutcome.Succeeded
                    : EmailDispatchAttemptOutcome.Failed)
                .SetProperty(attempt => attempt.CompletedAt, reconciledAt)
                .SetProperty(attempt => attempt.FailureCategory, reconciliationCategory)
                .SetProperty(attempt => attempt.SanitizedErrorMessage, Truncate(reason, MaxErrorLength))
                .SetProperty(attempt => attempt.ProviderMessageId, storedProviderMessageId)
                .SetProperty(attempt => attempt.UpdatedAt, reconciledAt)
                .SetProperty(attempt => attempt.UpdatedBy, changedBy), cancellationToken);
        EnsureExactlyOne(attemptUpdated, "email dispatch attempt");

        var receiptUpdated = await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(receipt => receipt.TenantId == tenantId
                && receipt.EmailDispatchOutboxId == outboxId
                && receipt.Status == EmailDispatchReceiptStatus.Unknown)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(receipt => receipt.Status, delivered
                    ? EmailDispatchReceiptStatus.Completed
                    : EmailDispatchReceiptStatus.Received)
                .SetProperty(receipt => receipt.ConsumerId, receipt => delivered ? receipt.ConsumerId : null)
                .SetProperty(receipt => receipt.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(receipt => receipt.CompletedAt, delivered ? reconciledAt : (DateTime?)null)
                .SetProperty(receipt => receipt.FailedAt, delivered ? (DateTime?)null : reconciledAt)
                .SetProperty(receipt => receipt.FailureCode, delivered ? null : reconciliationCategory)
                .SetProperty(receipt => receipt.FailureMessage, delivered ? null : Truncate(reason, MaxReceiptFailureLength))
                .SetProperty(receipt => receipt.ProviderMessageId, storedProviderMessageId)
                .SetProperty(receipt => receipt.UpdatedAt, reconciledAt)
                .SetProperty(receipt => receipt.UpdatedBy, changedBy), cancellationToken);
        EnsureExactlyOne(receiptUpdated, "email dispatch receipt");

        var deliveryUpdated = await _dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(delivery => delivery.TenantId == tenantId
                && delivery.EmailDispatchOutboxId == outboxId
                && delivery.ChannelId == (int)NotificationPreferenceChannelEnum.Email
                && delivery.StatusId == (int)NotificationDeliveryStatusEnum.Unknown)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(delivery => delivery.StatusId, delivered
                    ? (int)NotificationDeliveryStatusEnum.Delivered
                    : (int)NotificationDeliveryStatusEnum.Queued)
                .SetProperty(delivery => delivery.ProviderMessageId, storedProviderMessageId)
                .SetProperty(delivery => delivery.ProviderStatus, delivered ? "accepted_reconciled" : "queued")
                .SetProperty(delivery => delivery.FailureCategory, delivered ? null : reconciliationCategory)
                .SetProperty(delivery => delivery.CompletedAt, delivered ? reconciledAt : (DateTime?)null)
                .SetProperty(delivery => delivery.UpdatedAt, reconciledAt)
                .SetProperty(delivery => delivery.UpdatedBy, changedBy), cancellationToken);
        EnsureExactlyOne(deliveryUpdated, "email notification delivery");

        return true;
    }

    public async Task<IReadOnlyList<Guid>> GetRetentionTenantIds(
        DateTime cutoffUtc,
        int maxTenants,
        CancellationToken cancellationToken)
    {
        return await RetentionRedactionEligible(cutoffUtc)
            .GroupBy(outbox => outbox.TenantId)
            .Select(group => new
            {
                TenantId = group.Key,
                OldestCreatedAt = group.Min(outbox => outbox.CreatedAt)
            })
            .OrderBy(row => row.OldestCreatedAt)
            .ThenBy(row => row.TenantId)
            .Take(maxTenants)
            .Select(row => row.TenantId)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountRetentionRedactionEligible(
        Guid tenantId,
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        return RetentionRedactionEligible(cutoffUtc)
            .Where(outbox => outbox.TenantId == tenantId)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .CountAsync(cancellationToken);
    }

    public async Task<int> RedactRetentionEligible(
        Guid tenantId,
        DateTime cutoffUtc,
        DateTime redactedAt,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var rows = await RetentionRedactionEligible(cutoffUtc)
            .Where(outbox => outbox.TenantId == tenantId)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return 0;
        }

        var ids = rows.ToArray();
        await _dbContext.EmailDispatchAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(attempt => ids.Contains(attempt.EmailDispatchOutboxId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(attempt => attempt.Provider, (string?)null)
                .SetProperty(attempt => attempt.SanitizedErrorMessage, (string?)null)
                .SetProperty(attempt => attempt.ProviderMessageId, (string?)null)
                .SetProperty(attempt => attempt.CorrelationId, (string?)null)
                .SetProperty(attempt => attempt.UpdatedAt, redactedAt), cancellationToken);

        await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(receipt => ids.Contains(receipt.EmailDispatchOutboxId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(receipt => receipt.ConsumerId, (string?)null)
                .SetProperty(receipt => receipt.FailureMessage, (string?)null)
                .SetProperty(receipt => receipt.ProviderMessageId, (string?)null)
                .SetProperty(receipt => receipt.UpdatedAt, redactedAt), cancellationToken);

        await _dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(delivery => delivery.TenantId == tenantId
                && delivery.EmailDispatchOutboxId != null
                && ids.Contains(delivery.EmailDispatchOutboxId.Value))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(delivery => delivery.ProviderMessageId, (string?)null)
                .SetProperty(delivery => delivery.ProviderStatus, (string?)null)
                .SetProperty(delivery => delivery.UpdatedAt, redactedAt), cancellationToken);

        var redactedCount = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(outbox => ids.Contains(outbox.Id) && outbox.ContentRedactedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(outbox => outbox.RecipientEmail, string.Empty)
                .SetProperty(outbox => outbox.Subject, string.Empty)
                .SetProperty(outbox => outbox.PlainTextBody, (string?)null)
                .SetProperty(outbox => outbox.HtmlBody, (string?)null)
                .SetProperty(outbox => outbox.ReplyTo, (string?)null)
                .SetProperty(outbox => outbox.LastError, (string?)null)
                .SetProperty(outbox => outbox.ProviderMessageId, (string?)null)
                .SetProperty(outbox => outbox.CorrelationId, (string?)null)
                .SetProperty(outbox => outbox.NextAttemptAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(outbox => outbox.ContentRedactedAt, redactedAt)
                .SetProperty(outbox => outbox.UpdatedAt, redactedAt), cancellationToken);
        return redactedCount;
    }

    public async Task<int> SuppressAndRedactTenant(
        Guid tenantId,
        Guid? changedBy,
        DateTime redactedAt,
        CancellationToken cancellationToken)
    {
        var ids = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .AsNoTracking()
            .Where(outbox => outbox.TenantId == tenantId && outbox.ContentRedactedAt == null)
            .Select(outbox => outbox.Id)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
        {
            return 0;
        }

        await _dbContext.EmailDispatchAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(attempt => attempt.TenantId == tenantId && ids.Contains(attempt.EmailDispatchOutboxId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(attempt => attempt.Provider, (string?)null)
                .SetProperty(attempt => attempt.SanitizedErrorMessage, (string?)null)
                .SetProperty(attempt => attempt.ProviderMessageId, (string?)null)
                .SetProperty(attempt => attempt.CorrelationId, (string?)null)
                .SetProperty(attempt => attempt.UpdatedAt, redactedAt)
                .SetProperty(attempt => attempt.UpdatedBy, changedBy), cancellationToken);

        await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(receipt => receipt.TenantId == tenantId && ids.Contains(receipt.EmailDispatchOutboxId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(receipt => receipt.Status, receipt => receipt.Status == EmailDispatchReceiptStatus.Completed
                    ? EmailDispatchReceiptStatus.Completed
                    : EmailDispatchReceiptStatus.Skipped)
                .SetProperty(receipt => receipt.ConsumerId, (string?)null)
                .SetProperty(receipt => receipt.FailedAt, receipt => receipt.Status == EmailDispatchReceiptStatus.Completed
                    ? receipt.FailedAt
                    : redactedAt)
                .SetProperty(receipt => receipt.FailureCode, receipt => receipt.Status == EmailDispatchReceiptStatus.Completed
                    ? receipt.FailureCode
                    : "tenant_deleted")
                .SetProperty(receipt => receipt.FailureMessage, (string?)null)
                .SetProperty(receipt => receipt.ProviderMessageId, (string?)null)
                .SetProperty(receipt => receipt.UpdatedAt, redactedAt)
                .SetProperty(receipt => receipt.UpdatedBy, changedBy), cancellationToken);

        await _dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(delivery => delivery.TenantId == tenantId
                && delivery.EmailDispatchOutboxId != null
                && ids.Contains(delivery.EmailDispatchOutboxId.Value))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(delivery => delivery.StatusId, delivery => delivery.StatusId == (int)NotificationDeliveryStatusEnum.Delivered
                    ? (int)NotificationDeliveryStatusEnum.Delivered
                    : (int)NotificationDeliveryStatusEnum.Skipped)
                .SetProperty(delivery => delivery.ProviderMessageId, (string?)null)
                .SetProperty(delivery => delivery.ProviderStatus, (string?)null)
                .SetProperty(delivery => delivery.FailureCategory, delivery => delivery.StatusId == (int)NotificationDeliveryStatusEnum.Delivered
                    ? delivery.FailureCategory
                    : "tenant_deleted")
                .SetProperty(delivery => delivery.CompletedAt, delivery => delivery.StatusId == (int)NotificationDeliveryStatusEnum.Delivered
                    ? delivery.CompletedAt
                    : redactedAt)
                .SetProperty(delivery => delivery.UpdatedAt, redactedAt)
                .SetProperty(delivery => delivery.UpdatedBy, changedBy), cancellationToken);

        return await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(outbox => outbox.TenantId == tenantId
                && ids.Contains(outbox.Id)
                && outbox.ContentRedactedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(outbox => outbox.Status, outbox => outbox.Status == EmailDispatchStatus.Pending
                    || outbox.Status == EmailDispatchStatus.RetryScheduled
                    || outbox.Status == EmailDispatchStatus.Processing
                        ? EmailDispatchStatus.Skipped
                        : outbox.Status)
                .SetProperty(outbox => outbox.RecipientEmail, string.Empty)
                .SetProperty(outbox => outbox.Subject, string.Empty)
                .SetProperty(outbox => outbox.PlainTextBody, (string?)null)
                .SetProperty(outbox => outbox.HtmlBody, (string?)null)
                .SetProperty(outbox => outbox.ReplyTo, (string?)null)
                .SetProperty(outbox => outbox.LastFailureCategory, outbox => outbox.Status == EmailDispatchStatus.Pending
                    || outbox.Status == EmailDispatchStatus.RetryScheduled
                    || outbox.Status == EmailDispatchStatus.Processing
                        ? "tenant_deleted"
                        : outbox.LastFailureCategory)
                .SetProperty(outbox => outbox.LastError, (string?)null)
                .SetProperty(outbox => outbox.LastFailureAt, outbox => outbox.Status == EmailDispatchStatus.Pending
                    || outbox.Status == EmailDispatchStatus.RetryScheduled
                    || outbox.Status == EmailDispatchStatus.Processing
                        ? redactedAt
                        : outbox.LastFailureAt)
                .SetProperty(outbox => outbox.ProviderMessageId, (string?)null)
                .SetProperty(outbox => outbox.CorrelationId, (string?)null)
                .SetProperty(outbox => outbox.NextAttemptAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(outbox => outbox.ContentRedactedAt, redactedAt)
                .SetProperty(outbox => outbox.UpdatedAt, redactedAt)
                .SetProperty(outbox => outbox.UpdatedBy, changedBy), cancellationToken);
    }

    private async Task<IReadOnlyList<Guid>> ExecuteClaimTransactionAsync(
        Guid leaseToken,
        DateTime claimedAt,
        int highWatermark,
        int lowWatermark,
        Func<DbTransaction, Task<IReadOnlyList<Guid>>> executeClaim,
        CancellationToken cancellationToken,
        Guid? tenantId = null,
        Guid? publishEventId = null)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var dbTransaction = transaction.GetDbTransaction();
            await AcquireClaimLockAsync(dbTransaction, cancellationToken);
            await EnsureProcessorStateAsync(dbTransaction, claimedAt, cancellationToken);
            if (await IsProcessorPausedAsync(dbTransaction, cancellationToken))
            {
                await transaction.CommitAsync(cancellationToken);
                return [];
            }

            var previousClaim = await FindExistingClaimAsync(
                dbTransaction,
                leaseToken,
                tenantId,
                publishEventId,
                cancellationToken);
            if (previousClaim.Count > 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return previousClaim;
            }

            await UpdateOptionalReminderHysteresisAsync(
                dbTransaction,
                claimedAt,
                highWatermark,
                lowWatermark,
                cancellationToken);
            var claimedIds = await executeClaim(dbTransaction);
            await transaction.CommitAsync(cancellationToken);
            return claimedIds;
        });
    }

    private static async Task AcquireClaimLockAsync(
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            transaction,
            "SELECT pg_advisory_xact_lock(hashtext(@lock_name));");
        AddParameter(command, "lock_name", ClaimAdvisoryLockName, DbType.String);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureProcessorStateAsync(
        DbTransaction transaction,
        DateTime updatedAt,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            transaction,
            """
            INSERT INTO email_dispatch_processor_states
                (id, processor_code, is_paused, optional_reminders_deferred, updated_at)
            VALUES (@id, @processor_code, FALSE, FALSE, @updated_at)
            ON CONFLICT (processor_code) DO NOTHING;
            """);
        AddParameter(command, "id", Guid.CreateVersion7(), DbType.Guid);
        AddParameter(command, "processor_code", SmtpProcessorCode, DbType.String);
        AddParameter(command, "updated_at", updatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> IsProcessorPausedAsync(
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            transaction,
            """
            SELECT is_paused
            FROM email_dispatch_processor_states
            WHERE processor_code = @processor_code;
            """);
        AddParameter(command, "processor_code", SmtpProcessorCode, DbType.String);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    private static async Task<IReadOnlyList<Guid>> FindExistingClaimAsync(
        DbTransaction transaction,
        Guid leaseToken,
        Guid? tenantId,
        Guid? publishEventId,
        CancellationToken cancellationToken)
    {
        var specificPredicate = tenantId.HasValue
            ? " AND tenant_id = @tenant_id AND publish_event_id = @publish_event_id"
            : string.Empty;
        await using var command = CreateCommand(
            transaction,
            $"""
            SELECT id
            FROM email_dispatch_outbox
            WHERE status = @processing_status
              AND processing_lease_token = @lease_token{specificPredicate}
            ORDER BY id;
            """);
        AddParameter(command, "processing_status", (int)EmailDispatchStatus.Processing, DbType.Int32);
        AddParameter(command, "lease_token", leaseToken, DbType.Guid);
        if (tenantId.HasValue)
        {
            AddParameter(command, "tenant_id", tenantId.Value, DbType.Guid);
            AddParameter(command, "publish_event_id", publishEventId!.Value, DbType.Guid);
        }

        return await ReadIdsAsync(command, cancellationToken);
    }

    private static async Task UpdateOptionalReminderHysteresisAsync(
        DbTransaction transaction,
        DateTime claimedAt,
        int highWatermark,
        int lowWatermark,
        CancellationToken cancellationToken)
    {
        await using (var insertCommand = CreateCommand(
            transaction,
            """
            INSERT INTO email_dispatch_processor_states
                (id, processor_code, is_paused, optional_reminders_deferred, updated_at)
            VALUES (@id, @processor_code, FALSE, FALSE, @updated_at)
            ON CONFLICT (processor_code) DO NOTHING;
            """))
        {
            AddParameter(insertCommand, "id", Guid.CreateVersion7(), DbType.Guid);
            AddParameter(insertCommand, "processor_code", SmtpProcessorCode, DbType.String);
            AddParameter(insertCommand, "updated_at", claimedAt);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var updateCommand = CreateCommand(
            transaction,
            """
            WITH core_backlog AS (
                SELECT COUNT(*)::integer AS backlog_count
                FROM email_dispatch_outbox AS outbox
                LEFT JOIN notification_deliveries AS delivery
                  ON delivery.tenant_id = outbox.tenant_id
                 AND delivery.email_dispatch_outbox_id = outbox.id
                WHERE outbox.content_redacted_at IS NULL
                  AND outbox.is_deleted = FALSE
                  AND outbox.status IN (@pending_status, @retry_status)
                  AND (outbox.next_attempt_at IS NULL OR outbox.next_attempt_at <= @claimed_at)
                  AND (outbox.kind <> @reminder_kind OR COALESCE(delivery.is_required, FALSE))
                  AND NOT EXISTS (
                      SELECT 1
                      FROM email_dispatch_tenant_controls AS control
                      WHERE control.tenant_id = outbox.tenant_id
                        AND control.is_paused = TRUE)
            )
            UPDATE email_dispatch_processor_states AS state
            SET optional_reminders_deferred = CASE
                    WHEN core_backlog.backlog_count >= @high_watermark THEN TRUE
                    WHEN core_backlog.backlog_count <= @low_watermark THEN FALSE
                    ELSE state.optional_reminders_deferred
                END,
                updated_at = @claimed_at
            FROM core_backlog
            WHERE state.processor_code = @processor_code;
            """);
        AddClaimStateParameters(updateCommand, claimedAt, highWatermark, lowWatermark);
        await updateCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<Guid>> ExecuteBatchClaimAsync(
        DbTransaction transaction,
        EmailDispatchBatchClaimRequest request,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            transaction,
            """
            WITH active_by_tenant AS (
                SELECT tenant_id, COUNT(*)::integer AS active_count
                FROM email_dispatch_outbox
                WHERE status = @processing_status
                GROUP BY tenant_id
            ),
            ranked AS (
                SELECT outbox.id,
                       outbox.tenant_id,
                       outbox.created_at,
                       CASE
                           WHEN COALESCE(delivery.is_required, FALSE) THEN 0
                           WHEN outbox.kind = @reminder_kind THEN 2
                           ELSE 1
                       END AS priority,
                       ROW_NUMBER() OVER (
                           PARTITION BY outbox.tenant_id
                           ORDER BY
                               CASE
                                   WHEN COALESCE(delivery.is_required, FALSE) THEN 0
                                   WHEN outbox.kind = @reminder_kind THEN 2
                                   ELSE 1
                               END,
                               outbox.created_at,
                               outbox.id) AS tenant_rank
                FROM email_dispatch_outbox AS outbox
                LEFT JOIN notification_deliveries AS delivery
                  ON delivery.tenant_id = outbox.tenant_id
                 AND delivery.email_dispatch_outbox_id = outbox.id
                CROSS JOIN email_dispatch_processor_states AS state
                WHERE state.processor_code = @processor_code
                  AND outbox.content_redacted_at IS NULL
                  AND outbox.is_deleted = FALSE
                  AND outbox.status IN (@pending_status, @retry_status)
                  AND (outbox.next_attempt_at IS NULL OR outbox.next_attempt_at <= @claimed_at)
                  AND (NOT state.optional_reminders_deferred
                       OR outbox.kind <> @reminder_kind
                       OR COALESCE(delivery.is_required, FALSE))
                  AND NOT EXISTS (
                      SELECT 1
                      FROM email_dispatch_tenant_controls AS control
                      WHERE control.tenant_id = outbox.tenant_id
                        AND control.is_paused = TRUE)
            ),
            candidates AS (
                SELECT ranked.id
                FROM ranked
                LEFT JOIN active_by_tenant AS active
                  ON active.tenant_id = ranked.tenant_id
                WHERE ranked.tenant_rank <= LEAST(
                    @max_rows_per_tenant,
                    GREATEST(@tenant_limit - COALESCE(active.active_count, 0), 0))
                ORDER BY ranked.priority, ranked.tenant_rank, ranked.created_at, ranked.id
                LIMIT GREATEST(
                    LEAST(
                        @batch_size,
                        @global_limit - (
                            SELECT COUNT(*)::integer
                            FROM email_dispatch_outbox
                            WHERE status = @processing_status)),
                    0)
            )
            UPDATE email_dispatch_outbox AS outbox
            SET status = @processing_status,
                processing_started_at = @claimed_at,
                processing_lease_token = @lease_token,
                updated_at = @claimed_at
            FROM candidates
            WHERE outbox.id = candidates.id
              AND outbox.status IN (@pending_status, @retry_status)
            RETURNING outbox.id;
            """);
        AddClaimParameters(
            command,
            request.LeaseToken,
            request.ClaimedAt,
            request.GlobalProcessingLimit,
            request.TenantProcessingLimit);
        AddParameter(command, "batch_size", request.BatchSize, DbType.Int32);
        AddParameter(command, "max_rows_per_tenant", request.MaxRowsPerTenant, DbType.Int32);
        return await ReadIdsAsync(command, cancellationToken);
    }

    private static async Task<IReadOnlyList<Guid>> ExecuteSpecificClaimAsync(
        DbTransaction transaction,
        EmailDispatchSpecificClaimRequest request,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            transaction,
            """
            WITH candidate AS (
                SELECT outbox.id
                FROM email_dispatch_outbox AS outbox
                LEFT JOIN notification_deliveries AS delivery
                  ON delivery.tenant_id = outbox.tenant_id
                 AND delivery.email_dispatch_outbox_id = outbox.id
                CROSS JOIN email_dispatch_processor_states AS state
                WHERE state.processor_code = @processor_code
                  AND outbox.tenant_id = @tenant_id
                  AND outbox.publish_event_id = @publish_event_id
                  AND outbox.content_redacted_at IS NULL
                  AND outbox.is_deleted = FALSE
                  AND outbox.status IN (@pending_status, @retry_status)
                  AND (outbox.next_attempt_at IS NULL OR outbox.next_attempt_at <= @claimed_at)
                  AND (NOT state.optional_reminders_deferred
                       OR outbox.kind <> @reminder_kind
                       OR COALESCE(delivery.is_required, FALSE))
                  AND NOT EXISTS (
                      SELECT 1
                      FROM email_dispatch_tenant_controls AS control
                      WHERE control.tenant_id = outbox.tenant_id
                        AND control.is_paused = TRUE)
                  AND (
                      SELECT COUNT(*)
                      FROM email_dispatch_outbox
                      WHERE status = @processing_status) < @global_limit
                  AND (
                      SELECT COUNT(*)
                      FROM email_dispatch_outbox
                      WHERE status = @processing_status
                        AND tenant_id = @tenant_id) < @tenant_limit
            )
            UPDATE email_dispatch_outbox AS outbox
            SET status = @processing_status,
                processing_started_at = @claimed_at,
                processing_lease_token = @lease_token,
                updated_at = @claimed_at
            FROM candidate
            WHERE outbox.id = candidate.id
              AND outbox.status IN (@pending_status, @retry_status)
            RETURNING outbox.id;
            """);
        AddClaimParameters(
            command,
            request.LeaseToken,
            request.ClaimedAt,
            request.GlobalProcessingLimit,
            request.TenantProcessingLimit);
        AddParameter(command, "tenant_id", request.TenantId, DbType.Guid);
        AddParameter(command, "publish_event_id", request.PublishEventId, DbType.Guid);
        return await ReadIdsAsync(command, cancellationToken);
    }

    private async Task<IReadOnlyList<EmailDispatchOutbox>> LoadClaimedRowsAsync(
        IReadOnlyList<Guid> claimedIds,
        CancellationToken cancellationToken)
    {
        if (claimedIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(outbox => claimedIds.Contains(outbox.Id))
            .ToListAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<Guid>> ReadIdsAsync(
        DbCommand command,
        CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetGuid(0));
        }

        return ids;
    }

    private DbCommand CreateReminderCommand(string commandText)
    {
        NotificationFanoutPrecedenceLock.EnsureActivePostgresTransaction(_dbContext);
        return CreateCommand(_dbContext.Database.CurrentTransaction!.GetDbTransaction(), commandText);
    }

    private static async Task<EventReminderStateChangeResult> ReadReminderStateChangeAsync(
        DbCommand command,
        CancellationToken cancellationToken)
    {
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Reminder state change did not return its bounded result.");
        }

        return new EventReminderStateChangeResult(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetInt32(3));
    }

    private static void AddReminderParameters(
        DbCommand command,
        Guid tenantId,
        Guid eventId,
        Guid? registrationOrderId,
        Guid? sessionId,
        string? sessionSuffix,
        DateTime changedAt,
        string reason)
    {
        AddParameter(command, "tenant_id", tenantId, DbType.Guid);
        AddParameter(command, "event_id", eventId, DbType.Guid);
        AddParameter(command, "registration_order_id", registrationOrderId ?? (object)DBNull.Value, DbType.Guid);
        AddParameter(command, "session_id", sessionId ?? (object)DBNull.Value, DbType.Guid);
        AddParameter(command, "session_suffix", sessionSuffix ?? (object)DBNull.Value, DbType.String);
        AddParameter(command, "changed_at", changedAt);
        AddParameter(command, "reason", reason, DbType.String);
        AddParameter(command, "message", ReminderSupersededMessage, DbType.String);
        AddParameter(command, "provider_handoff_started", ProviderHandoffStarted, DbType.String);
        AddParameter(command, "superseded_provider_status", ReminderSupersededProviderStatus, DbType.String);
        AddParameter(command, "reminder_kind", (int)EmailDispatchKind.EventReminder, DbType.Int32);
        AddParameter(command, "pending_status", (int)EmailDispatchStatus.Pending, DbType.Int32);
        AddParameter(command, "retry_status", (int)EmailDispatchStatus.RetryScheduled, DbType.Int32);
        AddParameter(command, "processing_status", (int)EmailDispatchStatus.Processing, DbType.Int32);
        AddParameter(command, "skipped_status", (int)EmailDispatchStatus.Skipped, DbType.Int32);
        AddParameter(command, "processing_receipt_status", (int)EmailDispatchReceiptStatus.Processing, DbType.Int32);
        AddParameter(command, "resolved_intent_status", (int)NotificationIntentStatusEnum.Resolved, DbType.Int32);
        AddParameter(command, "dispatch_queued_intent_status", (int)NotificationIntentStatusEnum.DispatchQueued, DbType.Int32);
        AddParameter(command, "pending_delivery_status", (int)NotificationDeliveryStatusEnum.Pending, DbType.Int32);
        AddParameter(command, "queued_delivery_status", (int)NotificationDeliveryStatusEnum.Queued, DbType.Int32);
        AddParameter(command, "superseded_delivery_status", (int)NotificationDeliveryStatusEnum.Superseded, DbType.Int32);
        AddParameter(command, "email_channel_id", (int)NotificationPreferenceChannelEnum.Email, DbType.Int32);
        AddParameter(command, "in_app_channel_id", (int)NotificationPreferenceChannelEnum.InApp, DbType.Int32);
    }

    private static DbCommand CreateCommand(DbTransaction transaction, string commandText)
    {
        var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        return command;
    }

    private static void AddClaimStateParameters(
        DbCommand command,
        DateTime claimedAt,
        int highWatermark,
        int lowWatermark)
    {
        AddParameter(command, "processor_code", SmtpProcessorCode, DbType.String);
        AddParameter(command, "pending_status", (int)EmailDispatchStatus.Pending, DbType.Int32);
        AddParameter(command, "retry_status", (int)EmailDispatchStatus.RetryScheduled, DbType.Int32);
        AddParameter(command, "reminder_kind", (int)EmailDispatchKind.EventReminder, DbType.Int32);
        AddParameter(command, "claimed_at", claimedAt);
        AddParameter(command, "high_watermark", highWatermark, DbType.Int32);
        AddParameter(command, "low_watermark", lowWatermark, DbType.Int32);
    }

    private static void AddClaimParameters(
        DbCommand command,
        Guid leaseToken,
        DateTime claimedAt,
        int globalLimit,
        int tenantLimit)
    {
        AddParameter(command, "processor_code", SmtpProcessorCode, DbType.String);
        AddParameter(command, "pending_status", (int)EmailDispatchStatus.Pending, DbType.Int32);
        AddParameter(command, "processing_status", (int)EmailDispatchStatus.Processing, DbType.Int32);
        AddParameter(command, "retry_status", (int)EmailDispatchStatus.RetryScheduled, DbType.Int32);
        AddParameter(command, "reminder_kind", (int)EmailDispatchKind.EventReminder, DbType.Int32);
        AddParameter(command, "lease_token", leaseToken, DbType.Guid);
        AddParameter(command, "claimed_at", claimedAt);
        AddParameter(command, "global_limit", globalLimit, DbType.Int32);
        AddParameter(command, "tenant_limit", tenantLimit, DbType.Int32);
    }

    private static void AddParameter(DbCommand command, string name, object value, DbType dbType)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        parameter.DbType = dbType;
        command.Parameters.Add(parameter);
    }

    private static void AddParameter(DbCommand command, string name, DateTime value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static void ValidateBatchClaimRequest(EmailDispatchBatchClaimRequest request)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(request.LeaseToken, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.BatchSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.MaxRowsPerTenant, 1);
        ValidateClaimLimits(
            request.GlobalProcessingLimit,
            request.TenantProcessingLimit,
            request.OptionalReminderBacklogHighWatermark,
            request.OptionalReminderBacklogLowWatermark);
    }

    private static void ValidateSpecificClaimRequest(EmailDispatchSpecificClaimRequest request)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(request.TenantId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(request.PublishEventId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(request.LeaseToken, Guid.Empty);
        ValidateClaimLimits(
            request.GlobalProcessingLimit,
            request.TenantProcessingLimit,
            request.OptionalReminderBacklogHighWatermark,
            request.OptionalReminderBacklogLowWatermark);
    }

    private static void ValidateClaimLimits(
        int globalLimit,
        int tenantLimit,
        int highWatermark,
        int lowWatermark)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(globalLimit, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(tenantLimit, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(lowWatermark);
        if (highWatermark <= lowWatermark)
        {
            throw new ArgumentOutOfRangeException(
                nameof(highWatermark),
                highWatermark,
                "The optional-reminder high watermark must exceed the low watermark.");
        }
    }

    public async Task<EmailDispatchStaleRecoveryResult> RecoverStaleProcessing(
        EmailDispatchStaleRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            _dbContext.ChangeTracker.Clear();
            return await RecoverStaleProcessingTransactionAsync(request, cancellationToken);
        });
    }

    private async Task<EmailDispatchStaleRecoveryResult> RecoverStaleProcessingTransactionAsync(
        EmailDispatchStaleRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var stale = await _dbContext.EmailDispatchOutbox
            .FromSqlInterpolated($$"""
                SELECT *
                FROM email_dispatch_outbox
                WHERE status = {{EmailDispatchStatus.Processing}}
                  AND processing_started_at IS NOT NULL
                  AND processing_started_at <= {{request.ProcessingStartedBefore}}
                ORDER BY processing_started_at, id
                FOR UPDATE SKIP LOCKED
                LIMIT {{request.BatchSize}}
                """)
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .ToListAsync(cancellationToken);
        if (stale.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return new EmailDispatchStaleRecoveryResult(0, 0);
        }

        var staleIds = stale.Select(outbox => outbox.Id).ToArray();
        var attemptFences = await _dbContext.EmailDispatchAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(attempt => staleIds.Contains(attempt.EmailDispatchOutboxId)
                && attempt.FailureCategory == "provider_handoff_started")
            .Select(attempt => new { attempt.EmailDispatchOutboxId, attempt.AttemptNumber })
            .ToListAsync(cancellationToken);
        var receiptFences = await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(receipt => staleIds.Contains(receipt.EmailDispatchOutboxId)
                && receipt.Status == EmailDispatchReceiptStatus.Processing)
            .Select(receipt => receipt.EmailDispatchOutboxId)
            .ToListAsync(cancellationToken);
        var fencedIds = stale
            .Where(outbox => attemptFences.Any(attempt =>
                    attempt.EmailDispatchOutboxId == outbox.Id
                    && attempt.AttemptNumber == outbox.AttemptCount)
                || receiptFences.Contains(outbox.Id))
            .Select(outbox => outbox.Id)
            .ToArray();
        var retryableIds = staleIds.Except(fencedIds).ToArray();

        var retryScheduled = retryableIds.Length == 0
            ? 0
            : await _dbContext.EmailDispatchOutbox
                .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
                .Where(outbox => retryableIds.Contains(outbox.Id)
                    && outbox.Status == EmailDispatchStatus.Processing)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(outbox => outbox.Status, EmailDispatchStatus.RetryScheduled)
                    .SetProperty(outbox => outbox.NextAttemptAt, request.RecoveredAt)
                    .SetProperty(outbox => outbox.ProcessingStartedAt, (DateTime?)null)
                    .SetProperty(outbox => outbox.ProcessingLeaseToken, (Guid?)null)
                    .SetProperty(outbox => outbox.LastFailureCategory, Truncate(request.RetryFailureCategory, 100))
                    .SetProperty(outbox => outbox.LastError, Truncate(request.RetryErrorMessage, MaxErrorLength))
                    .SetProperty(outbox => outbox.LastFailureAt, request.RecoveredAt)
                    .SetProperty(outbox => outbox.UpdatedAt, request.RecoveredAt), cancellationToken);

        var unknown = fencedIds.Length == 0
            ? 0
            : await _dbContext.EmailDispatchOutbox
                .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
                .Where(outbox => fencedIds.Contains(outbox.Id)
                    && outbox.Status == EmailDispatchStatus.Processing)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(outbox => outbox.Status, EmailDispatchStatus.Unknown)
                    .SetProperty(outbox => outbox.UnknownAt, request.RecoveredAt)
                    .SetProperty(outbox => outbox.NextAttemptAt, (DateTime?)null)
                    .SetProperty(outbox => outbox.ProcessingStartedAt, (DateTime?)null)
                    .SetProperty(outbox => outbox.ProcessingLeaseToken, (Guid?)null)
                    .SetProperty(outbox => outbox.LastFailureCategory, Truncate(request.UnknownFailureCategory, 100))
                    .SetProperty(outbox => outbox.LastError, Truncate(request.UnknownErrorMessage, MaxErrorLength))
                    .SetProperty(outbox => outbox.LastFailureAt, request.RecoveredAt)
                    .SetProperty(outbox => outbox.UpdatedAt, request.RecoveredAt), cancellationToken);

        if (unknown > 0)
        {
            foreach (var outbox in stale.Where(outbox => fencedIds.Contains(outbox.Id)))
            {
                await _dbContext.EmailDispatchAttempts
                    .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
                    .Where(attempt => attempt.EmailDispatchOutboxId == outbox.Id
                        && attempt.AttemptNumber == outbox.AttemptCount)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(attempt => attempt.Outcome, EmailDispatchAttemptOutcome.Unknown)
                        .SetProperty(attempt => attempt.CompletedAt, request.RecoveredAt)
                        .SetProperty(attempt => attempt.FailureCategory, Truncate(request.UnknownFailureCategory, 100))
                        .SetProperty(attempt => attempt.SanitizedErrorMessage, Truncate(request.UnknownErrorMessage, MaxErrorLength))
                        .SetProperty(attempt => attempt.ProviderMessageId, (string?)null)
                        .SetProperty(attempt => attempt.UpdatedAt, request.RecoveredAt), cancellationToken);
            }
            await _dbContext.EmailDispatchReceipts
                .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
                .Where(receipt => fencedIds.Contains(receipt.EmailDispatchOutboxId)
                    && receipt.Status == EmailDispatchReceiptStatus.Processing)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(receipt => receipt.Status, EmailDispatchReceiptStatus.Unknown)
                    .SetProperty(receipt => receipt.FailedAt, request.RecoveredAt)
                    .SetProperty(receipt => receipt.FailureCode, Truncate(request.UnknownFailureCategory, 100))
                    .SetProperty(receipt => receipt.FailureMessage, Truncate(request.UnknownErrorMessage, MaxReceiptFailureLength))
                    .SetProperty(receipt => receipt.UpdatedAt, request.RecoveredAt), cancellationToken);
            await _dbContext.NotificationDeliveries
                .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
                .Where(delivery => delivery.EmailDispatchOutboxId != null
                    && fencedIds.Contains(delivery.EmailDispatchOutboxId.Value)
                    && delivery.ChannelId == (int)NotificationPreferenceChannelEnum.Email)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(delivery => delivery.StatusId, (int)NotificationDeliveryStatusEnum.Unknown)
                    .SetProperty(delivery => delivery.ProviderMessageId, (string?)null)
                    .SetProperty(delivery => delivery.ProviderStatus, "unknown")
                    .SetProperty(delivery => delivery.FailureCategory, Truncate(request.UnknownFailureCategory, 100))
                    .SetProperty(delivery => delivery.CompletedAt, request.RecoveredAt)
                    .SetProperty(delivery => delivery.UpdatedAt, request.RecoveredAt), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new EmailDispatchStaleRecoveryResult(retryScheduled, unknown);
    }

    public async Task<EmailDispatchPreHandoffReleaseOutcome> ReleaseClaimBeforeProviderHandoff(
        EmailDispatchPreHandoffRelease request,
        CancellationToken cancellationToken)
    {
        var released = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(outbox => outbox.TenantId == request.TenantId
                && outbox.Id == request.OutboxId
                && outbox.Status == EmailDispatchStatus.Processing
                && outbox.ProcessingLeaseToken == request.ProcessingLeaseToken
                && outbox.AttemptCount == request.AttemptNumber)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(outbox => outbox.Status, EmailDispatchStatus.RetryScheduled)
                .SetProperty(outbox => outbox.NextAttemptAt, request.ReleasedAt)
                .SetProperty(outbox => outbox.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(outbox => outbox.LastFailureCategory, Truncate(request.FailureCategory, 100))
                .SetProperty(outbox => outbox.LastError, Truncate(request.FailureMessage, MaxErrorLength))
                .SetProperty(outbox => outbox.LastFailureAt, request.ReleasedAt)
                .SetProperty(outbox => outbox.UpdatedAt, request.ReleasedAt), cancellationToken);
        if (released == 1)
        {
            return EmailDispatchPreHandoffReleaseOutcome.Released;
        }

        var providerFenceExists = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .AnyAsync(outbox => outbox.TenantId == request.TenantId
                && outbox.Id == request.OutboxId
                && outbox.AttemptCount > request.AttemptNumber, cancellationToken)
            || await _dbContext.EmailDispatchAttempts
                .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
                .AsNoTracking()
                .AnyAsync(attempt => attempt.TenantId == request.TenantId
                    && attempt.EmailDispatchOutboxId == request.OutboxId
                    && attempt.AttemptNumber > request.AttemptNumber
                    && attempt.FailureCategory == "provider_handoff_started", cancellationToken);
        return providerFenceExists
            ? EmailDispatchPreHandoffReleaseOutcome.ProviderHandoffFenced
            : EmailDispatchPreHandoffReleaseOutcome.LostClaim;
    }

    public async Task MarkRabbitMqPublishSucceeded(
        Guid id,
        DateTime publishedAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(e => e.Id == id
                && (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.RabbitMqLastPublishedAt, publishedAt)
                .SetProperty(e => e.RabbitMqLastPublishAttemptAt, publishedAt)
                .SetProperty(e => e.RabbitMqPublishAttemptCount, e => e.RabbitMqPublishAttemptCount + 1)
                .SetProperty(e => e.RabbitMqLastPublishFailureCategory, (string?)null)
                .SetProperty(e => e.UpdatedAt, publishedAt), cancellationToken);
    }

    public async Task MarkRabbitMqPublishFailed(
        Guid id,
        string failureCategory,
        DateTime attemptedAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(e => e.Id == id
                && (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.RabbitMqLastPublishAttemptAt, attemptedAt)
                .SetProperty(e => e.RabbitMqPublishAttemptCount, e => e.RabbitMqPublishAttemptCount + 1)
                .SetProperty(e => e.RabbitMqLastPublishFailureCategory, Truncate(failureCategory, 100))
                .SetProperty(e => e.UpdatedAt, attemptedAt), cancellationToken);
    }

    public async Task SettleProviderAccepted(
        EmailDispatchAcceptedSettlement settlement,
        CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            _dbContext.ChangeTracker.Clear();
            await SettleProviderAcceptedTransactionAsync(settlement, cancellationToken);
        });
    }

    private async Task SettleProviderAcceptedTransactionAsync(
        EmailDispatchAcceptedSettlement settlement,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var outboxUpdated = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(outbox => outbox.TenantId == settlement.TenantId
                && outbox.Id == settlement.OutboxId
                && outbox.Status == EmailDispatchStatus.Processing
                && outbox.ProcessingLeaseToken == settlement.ProcessingLeaseToken
                && outbox.AttemptCount == settlement.AttemptNumber)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(outbox => outbox.Status, EmailDispatchStatus.Sent)
                .SetProperty(outbox => outbox.SentAt, settlement.SettledAt)
                .SetProperty(outbox => outbox.UnknownAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProviderMessageId, Truncate(settlement.ProviderMessageId, 500))
                .SetProperty(outbox => outbox.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(outbox => outbox.NextAttemptAt, (DateTime?)null)
                .SetProperty(outbox => outbox.LastFailureCategory, (string?)null)
                .SetProperty(outbox => outbox.LastError, (string?)null)
                .SetProperty(outbox => outbox.LastFailureAt, (DateTime?)null)
                .SetProperty(outbox => outbox.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(outboxUpdated, "email dispatch outbox claim fence");

        var attemptUpdated = await _dbContext.EmailDispatchAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(attempt => attempt.TenantId == settlement.TenantId
                && attempt.EmailDispatchOutboxId == settlement.OutboxId
                && attempt.AttemptNumber == settlement.AttemptNumber)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(attempt => attempt.Outcome, EmailDispatchAttemptOutcome.Succeeded)
                .SetProperty(attempt => attempt.CompletedAt, settlement.SettledAt)
                .SetProperty(attempt => attempt.FailureCategory, (string?)null)
                .SetProperty(attempt => attempt.SanitizedErrorMessage, (string?)null)
                .SetProperty(attempt => attempt.ProviderMessageId, Truncate(settlement.ProviderMessageId, 500))
                .SetProperty(attempt => attempt.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(attemptUpdated, "email dispatch attempt");

        var receiptUpdated = await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(receipt => receipt.TenantId == settlement.TenantId
                && receipt.EmailDispatchOutboxId == settlement.OutboxId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(receipt => receipt.Status, EmailDispatchReceiptStatus.Completed)
                .SetProperty(receipt => receipt.CompletedAt, settlement.SettledAt)
                .SetProperty(receipt => receipt.FailedAt, (DateTime?)null)
                .SetProperty(receipt => receipt.FailureCode, (string?)null)
                .SetProperty(receipt => receipt.FailureMessage, (string?)null)
                .SetProperty(receipt => receipt.ProviderMessageId, Truncate(settlement.ProviderMessageId, 500))
                .SetProperty(receipt => receipt.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(receiptUpdated, "email dispatch receipt");

        var deliveryUpdated = await _dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(delivery => delivery.TenantId == settlement.TenantId
                && delivery.EmailDispatchOutboxId == settlement.OutboxId
                && delivery.ChannelId == (int)NotificationPreferenceChannelEnum.Email)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(delivery => delivery.StatusId, (int)NotificationDeliveryStatusEnum.Delivered)
                .SetProperty(delivery => delivery.ProviderMessageId, Truncate(settlement.ProviderMessageId, 500))
                .SetProperty(delivery => delivery.ProviderStatus, "accepted")
                .SetProperty(delivery => delivery.FailureCategory, (string?)null)
                .SetProperty(delivery => delivery.CompletedAt, settlement.SettledAt)
                .SetProperty(delivery => delivery.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(deliveryUpdated, "email notification delivery");

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<EmailDispatchFailureSettlementOutcome> SettleProviderFailure(
        EmailDispatchFailureSettlement settlement,
        CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            _dbContext.ChangeTracker.Clear();
            return await SettleProviderFailureTransactionAsync(settlement, cancellationToken);
        });
    }

    private async Task<EmailDispatchFailureSettlementOutcome> SettleProviderFailureTransactionAsync(
        EmailDispatchFailureSettlement settlement,
        CancellationToken cancellationToken)
    {
        var exhausted = settlement.AttemptNumber >= settlement.MaxAttempts;
        var outcome = exhausted
            ? EmailDispatchFailureSettlementOutcome.DeadLettered
            : EmailDispatchFailureSettlementOutcome.RetryScheduled;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var outboxUpdated = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(outbox => outbox.TenantId == settlement.TenantId
                && outbox.Id == settlement.OutboxId
                && outbox.Status == EmailDispatchStatus.Processing
                && outbox.ProcessingLeaseToken == settlement.ProcessingLeaseToken
                && outbox.AttemptCount == settlement.AttemptNumber)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(outbox => outbox.Status, exhausted
                    ? EmailDispatchStatus.DeadLettered
                    : EmailDispatchStatus.RetryScheduled)
                .SetProperty(outbox => outbox.DeadLetteredAt, exhausted ? settlement.SettledAt : (DateTime?)null)
                .SetProperty(outbox => outbox.NextAttemptAt, exhausted
                    ? (DateTime?)null
                    : settlement.SettledAt.Add(settlement.RetryDelay))
                .SetProperty(outbox => outbox.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(outbox => outbox.LastFailureCategory, Truncate(settlement.FailureCategory, 100))
                .SetProperty(outbox => outbox.LastError, Truncate(settlement.FailureMessage, MaxErrorLength))
                .SetProperty(outbox => outbox.LastFailureAt, settlement.SettledAt)
                .SetProperty(outbox => outbox.UpdatedAt, settlement.SettledAt), cancellationToken);
        if (outboxUpdated == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return EmailDispatchFailureSettlementOutcome.StaleClaim;
        }

        var attemptUpdated = await _dbContext.EmailDispatchAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(attempt => attempt.TenantId == settlement.TenantId
                && attempt.EmailDispatchOutboxId == settlement.OutboxId
                && attempt.AttemptNumber == settlement.AttemptNumber)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(attempt => attempt.Outcome, EmailDispatchAttemptOutcome.Failed)
                .SetProperty(attempt => attempt.CompletedAt, settlement.SettledAt)
                .SetProperty(attempt => attempt.FailureCategory, Truncate(settlement.FailureCategory, 100))
                .SetProperty(attempt => attempt.SanitizedErrorMessage, Truncate(settlement.FailureMessage, MaxErrorLength))
                .SetProperty(attempt => attempt.ProviderMessageId, (string?)null)
                .SetProperty(attempt => attempt.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(attemptUpdated, "email dispatch attempt");

        var receiptUpdated = await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(receipt => receipt.TenantId == settlement.TenantId
                && receipt.EmailDispatchOutboxId == settlement.OutboxId
                && receipt.Status == EmailDispatchReceiptStatus.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(receipt => receipt.Status, EmailDispatchReceiptStatus.Failed)
                .SetProperty(receipt => receipt.CompletedAt, (DateTime?)null)
                .SetProperty(receipt => receipt.FailedAt, settlement.SettledAt)
                .SetProperty(receipt => receipt.FailureCode, Truncate(settlement.FailureCategory, 100))
                .SetProperty(receipt => receipt.FailureMessage, Truncate(settlement.FailureMessage, MaxReceiptFailureLength))
                .SetProperty(receipt => receipt.ProviderMessageId, (string?)null)
                .SetProperty(receipt => receipt.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(receiptUpdated, "email dispatch receipt");

        var deliveryUpdated = await _dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(delivery => delivery.TenantId == settlement.TenantId
                && delivery.EmailDispatchOutboxId == settlement.OutboxId
                && delivery.ChannelId == (int)NotificationPreferenceChannelEnum.Email)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(delivery => delivery.StatusId, exhausted
                    ? (int)NotificationDeliveryStatusEnum.DeadLettered
                    : (int)NotificationDeliveryStatusEnum.Queued)
                .SetProperty(delivery => delivery.ProviderMessageId, (string?)null)
                .SetProperty(delivery => delivery.ProviderStatus, exhausted ? "dead_lettered" : "retry_scheduled")
                .SetProperty(delivery => delivery.FailureCategory, Truncate(settlement.FailureCategory, 100))
                .SetProperty(delivery => delivery.CompletedAt, exhausted ? settlement.SettledAt : (DateTime?)null)
                .SetProperty(delivery => delivery.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(deliveryUpdated, "email notification delivery");

        await transaction.CommitAsync(cancellationToken);
        return outcome;
    }

    public async Task<EmailDispatchAcceptedReconciliationOutcome> ReconcileProviderAccepted(
        EmailDispatchAcceptedSettlement settlement,
        CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            _dbContext.ChangeTracker.Clear();
            return await ReconcileProviderAcceptedTransactionAsync(settlement, cancellationToken);
        });
    }

    private async Task<EmailDispatchAcceptedReconciliationOutcome> ReconcileProviderAcceptedTransactionAsync(
        EmailDispatchAcceptedSettlement settlement,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var providerMessageId = Truncate(settlement.ProviderMessageId, 500);

        var outboxSent = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .AnyAsync(outbox => outbox.TenantId == settlement.TenantId
                && outbox.Id == settlement.OutboxId
                && outbox.Status == EmailDispatchStatus.Sent
                && (providerMessageId == null || outbox.ProviderMessageId == providerMessageId), cancellationToken);
        var attemptSent = await _dbContext.EmailDispatchAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .AnyAsync(attempt => attempt.TenantId == settlement.TenantId
                && attempt.EmailDispatchOutboxId == settlement.OutboxId
                && attempt.AttemptNumber == settlement.AttemptNumber
                && attempt.Outcome == EmailDispatchAttemptOutcome.Succeeded
                && (providerMessageId == null || attempt.ProviderMessageId == providerMessageId), cancellationToken);
        var receiptSent = await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .AnyAsync(receipt => receipt.TenantId == settlement.TenantId
                && receipt.EmailDispatchOutboxId == settlement.OutboxId
                && receipt.Status == EmailDispatchReceiptStatus.Completed
                && (providerMessageId == null || receipt.ProviderMessageId == providerMessageId), cancellationToken);
        var deliverySent = await _dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .AnyAsync(delivery => delivery.TenantId == settlement.TenantId
                && delivery.EmailDispatchOutboxId == settlement.OutboxId
                && delivery.ChannelId == (int)NotificationPreferenceChannelEnum.Email
                && delivery.StatusId == (int)NotificationDeliveryStatusEnum.Delivered
                && (providerMessageId == null || delivery.ProviderMessageId == providerMessageId), cancellationToken);

        if (outboxSent && attemptSent && receiptSent && deliverySent)
        {
            await transaction.CommitAsync(cancellationToken);
            return EmailDispatchAcceptedReconciliationOutcome.Sent;
        }

        var alreadyUnknown = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .AnyAsync(outbox => outbox.TenantId == settlement.TenantId
                && outbox.Id == settlement.OutboxId
                && outbox.Status == EmailDispatchStatus.Unknown
                && outbox.AttemptCount == settlement.AttemptNumber, cancellationToken);
        if (alreadyUnknown)
        {
            await transaction.CommitAsync(cancellationToken);
            return EmailDispatchAcceptedReconciliationOutcome.Unknown;
        }

        var outboxUpdated = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(outbox => outbox.TenantId == settlement.TenantId
                && outbox.Id == settlement.OutboxId
                && outbox.Status == EmailDispatchStatus.Processing
                && outbox.ProcessingLeaseToken == settlement.ProcessingLeaseToken
                && outbox.AttemptCount == settlement.AttemptNumber)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(outbox => outbox.Status, EmailDispatchStatus.Unknown)
                .SetProperty(outbox => outbox.SentAt, (DateTime?)null)
                .SetProperty(outbox => outbox.UnknownAt, settlement.SettledAt)
                .SetProperty(outbox => outbox.ProviderMessageId, (string?)null)
                .SetProperty(outbox => outbox.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(outbox => outbox.NextAttemptAt, (DateTime?)null)
                .SetProperty(outbox => outbox.LastFailureCategory, AcceptedSettlementUnknownCategory)
                .SetProperty(outbox => outbox.LastError, AcceptedSettlementUnknownMessage)
                .SetProperty(outbox => outbox.LastFailureAt, settlement.SettledAt)
                .SetProperty(outbox => outbox.UpdatedAt, settlement.SettledAt), cancellationToken);
        if (outboxUpdated == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return EmailDispatchAcceptedReconciliationOutcome.StaleClaim;
        }

        var attemptUpdated = await _dbContext.EmailDispatchAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(attempt => attempt.TenantId == settlement.TenantId
                && attempt.EmailDispatchOutboxId == settlement.OutboxId
                && attempt.AttemptNumber == settlement.AttemptNumber)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(attempt => attempt.Outcome, EmailDispatchAttemptOutcome.Unknown)
                .SetProperty(attempt => attempt.CompletedAt, settlement.SettledAt)
                .SetProperty(attempt => attempt.FailureCategory, AcceptedSettlementUnknownCategory)
                .SetProperty(attempt => attempt.SanitizedErrorMessage, AcceptedSettlementUnknownMessage)
                .SetProperty(attempt => attempt.ProviderMessageId, (string?)null)
                .SetProperty(attempt => attempt.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(attemptUpdated, "email dispatch attempt");

        var receiptUpdated = await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(receipt => receipt.TenantId == settlement.TenantId
                && receipt.EmailDispatchOutboxId == settlement.OutboxId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(receipt => receipt.Status, EmailDispatchReceiptStatus.Unknown)
                .SetProperty(receipt => receipt.CompletedAt, (DateTime?)null)
                .SetProperty(receipt => receipt.FailedAt, settlement.SettledAt)
                .SetProperty(receipt => receipt.FailureCode, AcceptedSettlementUnknownCategory)
                .SetProperty(receipt => receipt.FailureMessage, AcceptedSettlementUnknownMessage)
                .SetProperty(receipt => receipt.ProviderMessageId, (string?)null)
                .SetProperty(receipt => receipt.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(receiptUpdated, "email dispatch receipt");

        var deliveryUpdated = await _dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(delivery => delivery.TenantId == settlement.TenantId
                && delivery.EmailDispatchOutboxId == settlement.OutboxId
                && delivery.ChannelId == (int)NotificationPreferenceChannelEnum.Email)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(delivery => delivery.StatusId, (int)NotificationDeliveryStatusEnum.Unknown)
                .SetProperty(delivery => delivery.ProviderMessageId, (string?)null)
                .SetProperty(delivery => delivery.ProviderStatus, "unknown")
                .SetProperty(delivery => delivery.FailureCategory, AcceptedSettlementUnknownCategory)
                .SetProperty(delivery => delivery.CompletedAt, settlement.SettledAt)
                .SetProperty(delivery => delivery.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(deliveryUpdated, "email notification delivery");

        await transaction.CommitAsync(cancellationToken);
        return EmailDispatchAcceptedReconciliationOutcome.Unknown;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return value is null || value.Length <= maxLength ? value : value[..maxLength];
    }

    private IQueryable<EmailDispatchOutbox> RetentionRedactionEligible(DateTime cutoffUtc)
    {
        return _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(outbox => outbox.ContentRedactedAt == null
                && ((outbox.Status == EmailDispatchStatus.Sent && outbox.SentAt <= cutoffUtc)
                    || (outbox.Status == EmailDispatchStatus.Skipped && outbox.LastFailureAt <= cutoffUtc)));
    }

    private static void EnsureExactlyOne(int affectedRows, string ledger)
    {
        if (affectedRows != 1)
        {
            throw new InvalidOperationException($"Accepted SMTP settlement expected one {ledger} row.");
        }
    }
}
