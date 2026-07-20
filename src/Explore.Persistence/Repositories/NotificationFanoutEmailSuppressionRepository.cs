// ABOUTME: Suppresses occurrence-linked in-app and email work while transport remains before SMTP handoff.
// ABOUTME: Uses one bounded PostgreSQL statement and preserves provider-fenced and terminal SMTP evidence.

using System.Data;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Explore.Persistence.Repositories;

public sealed class NotificationFanoutEmailSuppressionRepository(ExploreDbContext dbContext)
    : INotificationFanoutEmailSuppressionRepository
{
    private const string ProviderHandoffStarted = "provider_handoff_started";

    public async Task<NotificationFanoutEmailSuppressionResult> SuppressPreHandoffAsync(
        Guid tenantId,
        Guid occurrenceId,
        DateTime suppressedAt,
        CancellationToken cancellationToken = default)
    {
        NotificationFanoutPrecedenceLock.EnsureActivePostgresTransaction(dbContext);
        if (tenantId == Guid.Empty || occurrenceId == Guid.Empty)
        {
            throw new ArgumentException("Fanout email suppression requires non-empty tenant and occurrence identifiers.");
        }

        if (suppressedAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Fanout email suppression time must be UTC.", nameof(suppressedAt));
        }

        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.Transaction = dbContext.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = """
            WITH suppressed_outbox AS (
                UPDATE email_dispatch_outbox AS outbox
                SET status = @skipped_outbox_status,
                    next_attempt_at = NULL,
                    processing_started_at = NULL,
                    processing_lease_token = NULL,
                    last_failure_category = @reason,
                    last_error = @message,
                    last_failure_at = @suppressed_at,
                    updated_at = @suppressed_at
                FROM notification_intents AS intent
                WHERE intent.tenant_id = @tenant_id
                  AND intent.fanout_occurrence_id = @occurrence_id
                  AND intent.is_deleted = FALSE
                  AND outbox.tenant_id = intent.tenant_id
                  AND outbox.notification_intent_id = intent.id
                  AND outbox.is_deleted = FALSE
                  AND outbox.content_redacted_at IS NULL
                  AND outbox.status IN (@pending_outbox_status, @retry_outbox_status, @processing_outbox_status)
                  AND (
                      outbox.status <> @processing_outbox_status
                      OR (
                          NOT EXISTS (
                              SELECT 1
                              FROM email_dispatch_attempts AS attempt
                              WHERE attempt.tenant_id = outbox.tenant_id
                                AND attempt.email_dispatch_outbox_id = outbox.id
                                AND attempt.attempt_number = outbox.attempt_count
                                AND attempt.failure_category = @provider_handoff_started)
                          AND NOT EXISTS (
                              SELECT 1
                              FROM email_dispatch_receipts AS receipt
                              WHERE receipt.tenant_id = outbox.tenant_id
                                AND receipt.email_dispatch_outbox_id = outbox.id
                                AND receipt.status = @processing_receipt_status)))
                RETURNING outbox.tenant_id, outbox.id
            ),
            superseded_delivery AS (
                UPDATE notification_deliveries AS delivery
                SET status_id = @superseded_delivery_status,
                    provider_status = @provider_status,
                    failure_category = @reason,
                    completed_at = @suppressed_at,
                    updated_at = @suppressed_at
                FROM suppressed_outbox AS outbox
                WHERE delivery.tenant_id = outbox.tenant_id
                  AND delivery.email_dispatch_outbox_id = outbox.id
                  AND delivery.channel_id = @email_channel_id
                  AND delivery.status_id IN (@pending_delivery_status, @queued_delivery_status)
                RETURNING delivery.id
            ),
            suppressed_notification AS (
                UPDATE notifications AS notification
                SET is_deleted = TRUE,
                    deleted_at = @suppressed_at,
                    updated_at = @suppressed_at
                FROM notification_deliveries AS delivery
                INNER JOIN notification_intents AS intent
                    ON intent.tenant_id = delivery.tenant_id
                   AND intent.id = delivery.notification_intent_id
                WHERE intent.tenant_id = @tenant_id
                  AND intent.fanout_occurrence_id = @occurrence_id
                  AND intent.is_deleted = FALSE
                  AND delivery.tenant_id = intent.tenant_id
                  AND delivery.channel_id = @in_app_channel_id
                  AND delivery.notification_id = notification.id
                  AND delivery.status_id IN (@pending_delivery_status, @delivered_delivery_status)
                  AND notification.tenant_id = intent.tenant_id
                  AND notification.is_deleted = FALSE
                RETURNING notification.tenant_id, notification.id
            ),
            superseded_in_app_delivery AS (
                UPDATE notification_deliveries AS delivery
                SET status_id = @superseded_delivery_status,
                    provider_status = @provider_status,
                    failure_category = @reason,
                    completed_at = @suppressed_at,
                    updated_at = @suppressed_at
                FROM suppressed_notification AS notification
                WHERE delivery.tenant_id = notification.tenant_id
                  AND delivery.notification_id = notification.id
                  AND delivery.channel_id = @in_app_channel_id
                  AND delivery.status_id = @pending_delivery_status
                RETURNING delivery.id
            )
            SELECT
                (SELECT COUNT(*)::integer FROM suppressed_outbox),
                (SELECT COUNT(*)::integer FROM superseded_delivery),
                (SELECT COUNT(*)::integer FROM suppressed_notification),
                (SELECT COUNT(*)::integer FROM superseded_in_app_delivery);
            """;
        AddParameter(command, "tenant_id", tenantId, DbType.Guid);
        AddParameter(command, "occurrence_id", occurrenceId, DbType.Guid);
        AddParameter(command, "suppressed_at", suppressedAt);
        AddParameter(command, "reason", NotificationFanoutEmailSuppressionReason.Code, DbType.String);
        AddParameter(command, "message", NotificationFanoutEmailSuppressionReason.Message, DbType.String);
        AddParameter(command, "provider_status", NotificationFanoutEmailSuppressionReason.ProviderStatus, DbType.String);
        AddParameter(command, "provider_handoff_started", ProviderHandoffStarted, DbType.String);
        AddParameter(command, "pending_outbox_status", (int)EmailDispatchStatus.Pending, DbType.Int32);
        AddParameter(command, "retry_outbox_status", (int)EmailDispatchStatus.RetryScheduled, DbType.Int32);
        AddParameter(command, "processing_outbox_status", (int)EmailDispatchStatus.Processing, DbType.Int32);
        AddParameter(command, "skipped_outbox_status", (int)EmailDispatchStatus.Skipped, DbType.Int32);
        AddParameter(command, "processing_receipt_status", (int)EmailDispatchReceiptStatus.Processing, DbType.Int32);
        AddParameter(command, "pending_delivery_status", (int)NotificationDeliveryStatusEnum.Pending, DbType.Int32);
        AddParameter(command, "queued_delivery_status", (int)NotificationDeliveryStatusEnum.Queued, DbType.Int32);
        AddParameter(command, "delivered_delivery_status", (int)NotificationDeliveryStatusEnum.Delivered, DbType.Int32);
        AddParameter(command, "superseded_delivery_status", (int)NotificationDeliveryStatusEnum.Superseded, DbType.Int32);
        AddParameter(command, "email_channel_id", (int)NotificationPreferenceChannelEnum.Email, DbType.Int32);
        AddParameter(command, "in_app_channel_id", (int)NotificationPreferenceChannelEnum.InApp, DbType.Int32);

        if (command.Connection!.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Fanout email suppression did not return its bounded result.");
        }

        return new NotificationFanoutEmailSuppressionResult(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetInt32(3));
    }

    private static void AddParameter(
        System.Data.Common.DbCommand command,
        string name,
        object value,
        DbType? dbType = null)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        if (dbType.HasValue)
        {
            parameter.DbType = dbType.Value;
        }

        command.Parameters.Add(parameter);
    }
}
