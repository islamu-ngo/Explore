// ABOUTME: Creates durable in-app notifications for attendees when moderation hides or redacts an event.
// ABOUTME: Separates light contextual notifications from generic heavy-redaction fanout for privacy safety.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Services;

public sealed class EventModerationNotificationFanoutService(
    IEventRegistrationIntentRepository registrationIntentRepository,
    IEventModerationRecordRepository moderationRecordRepository,
    INotificationRepository notificationRepository,
    INotificationFanoutRunRepository fanoutRunRepository,
    BusinessMetrics metrics,
    ILogger<EventModerationNotificationFanoutService> logger) : IEventModerationNotificationFanoutService
{
    public const string LightFanoutKind = "event-moderated-light";
    public const string HeavyFanoutKind = "event-moderated-heavy";
    public const string StatusProcessing = "processing";
    public const string StatusCompleted = "completed";
    public const string StatusFailed = "failed";
    public const string OutcomeCompleted = "completed";
    public const string OutcomeDuplicateSkipped = "duplicate_skipped";
    public const string OutcomeFailed = "failed";
    public const string OutcomeNotificationCreated = "notification_created";
    public const string OutcomeProcessing = "processing";
    public const string OutcomeProcessed = "processed";
    public const string OutcomeSkippedCompleted = "skipped_completed";

    private const int BatchSize = 250;

    public async Task FanoutLightModerationAsync(
        EventLightModeratedNotificationFanoutRequested request,
        CancellationToken cancellationToken = default)
    {
        var run = await GetOrCreateRunAsync(request, cancellationToken);
        if (string.Equals(run.Status, StatusCompleted, StringComparison.Ordinal))
        {
            metrics.RecordNotificationFanoutRun(request.TenantId.ToString(), LightFanoutKind, OutcomeSkippedCompleted);
            logger.LogInformation(
                "Skipping completed light moderation notification fanout run {RunId} for moderation record {ModerationRecordId}",
                run.Id,
                request.ModerationRecordId);
            return;
        }

        run.Status = StatusProcessing;
        run.StartedAt ??= DateTime.UtcNow;
        run.FailedAt = null;
        run.LastError = null;
        await fanoutRunRepository.Update(run);
        metrics.RecordNotificationFanoutRun(request.TenantId.ToString(), LightFanoutKind, OutcomeProcessing);

        var processedThisAttempt = 0;
        var createdThisAttempt = 0;
        var duplicateSkippedThisAttempt = 0;

        try
        {
            while (true)
            {
                var attendeeUserIds = await registrationIntentRepository.GetRegisteredUserFanoutBatchAsync(
                    request.TenantId,
                    request.EventId,
                    run.CursorSubscriberTenantUserId,
                    BatchSize,
                    cancellationToken);

                if (attendeeUserIds.Count == 0)
                {
                    break;
                }

                foreach (var userId in attendeeUserIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    run.ProcessedCount++;
                    processedThisAttempt++;
                    run.CursorSubscriberTenantUserId = userId;

                    var deduplicationKey = BuildLightModerationDeduplicationKey(request, userId);
                    var alreadyCreated = await notificationRepository.ExistsByDeduplicationKeyAsync(
                        request.TenantId,
                        userId,
                        deduplicationKey,
                        cancellationToken);

                    if (alreadyCreated)
                    {
                        duplicateSkippedThisAttempt++;
                        continue;
                    }

                    await notificationRepository.Create(CreateLightModerationNotification(request, userId, deduplicationKey));
                    run.CreatedNotificationCount++;
                    createdThisAttempt++;
                }

                await fanoutRunRepository.Update(run);

                if (attendeeUserIds.Count < BatchSize)
                {
                    break;
                }
            }

            run.Status = StatusCompleted;
            run.CompletedAt = DateTime.UtcNow;
            run.FailedAt = null;
            run.LastError = null;
            await fanoutRunRepository.Update(run);
            metrics.RecordNotificationFanoutRun(request.TenantId.ToString(), LightFanoutKind, OutcomeCompleted);
            metrics.RecordNotificationFanoutSubscribers(processedThisAttempt, request.TenantId.ToString(), LightFanoutKind, OutcomeProcessed);
            metrics.RecordNotificationFanoutSubscribers(createdThisAttempt, request.TenantId.ToString(), LightFanoutKind, OutcomeNotificationCreated);
            metrics.RecordNotificationFanoutSubscribers(duplicateSkippedThisAttempt, request.TenantId.ToString(), LightFanoutKind, OutcomeDuplicateSkipped);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            run.Status = StatusFailed;
            run.FailedAt = DateTime.UtcNow;
            run.LastError = ex.Message;
            await fanoutRunRepository.Update(run);
            metrics.RecordNotificationFanoutRun(request.TenantId.ToString(), LightFanoutKind, OutcomeFailed);
            metrics.RecordNotificationFanoutSubscribers(processedThisAttempt, request.TenantId.ToString(), LightFanoutKind, OutcomeProcessed);
            metrics.RecordNotificationFanoutSubscribers(createdThisAttempt, request.TenantId.ToString(), LightFanoutKind, OutcomeNotificationCreated);
            metrics.RecordNotificationFanoutSubscribers(duplicateSkippedThisAttempt, request.TenantId.ToString(), LightFanoutKind, OutcomeDuplicateSkipped);
            logger.LogError(
                ex,
                "Failed light moderation notification fanout run {RunId} for moderation record {ModerationRecordId}",
                run.Id,
                request.ModerationRecordId);
            throw;
        }
    }

    public async Task FanoutHeavyRedactionAsync(
        EventHeavyRedactedNotificationFanoutRequested request,
        CancellationToken cancellationToken = default)
    {
        var moderationRecord = await moderationRecordRepository.GetByIdAsync(
            request.TenantId,
            request.ModerationRecordId,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Moderation record {request.ModerationRecordId} was not found for heavy moderation notification fanout.");

        if (moderationRecord.ActionKind != EventModerationActionKind.HeavyRedacted || !moderationRecord.IsIrreversible)
        {
            throw new InvalidOperationException(
                $"Moderation record {request.ModerationRecordId} is not an irreversible heavy redaction record.");
        }

        var run = await GetOrCreateHeavyRunAsync(request, cancellationToken);
        if (string.Equals(run.Status, StatusCompleted, StringComparison.Ordinal))
        {
            metrics.RecordNotificationFanoutRun(request.TenantId.ToString(), HeavyFanoutKind, OutcomeSkippedCompleted);
            logger.LogInformation(
                "Skipping completed heavy moderation notification fanout run {RunId} for moderation record {ModerationRecordId}",
                run.Id,
                request.ModerationRecordId);
            return;
        }

        run.Status = StatusProcessing;
        run.StartedAt ??= DateTime.UtcNow;
        run.FailedAt = null;
        run.LastError = null;
        await fanoutRunRepository.Update(run);
        metrics.RecordNotificationFanoutRun(request.TenantId.ToString(), HeavyFanoutKind, OutcomeProcessing);

        var processedThisAttempt = 0;
        var createdThisAttempt = 0;
        var duplicateSkippedThisAttempt = 0;

        try
        {
            while (true)
            {
                var attendeeUserIds = await registrationIntentRepository.GetRegisteredUserFanoutBatchAsync(
                    request.TenantId,
                    moderationRecord.EventId,
                    run.CursorSubscriberTenantUserId,
                    BatchSize,
                    cancellationToken);

                if (attendeeUserIds.Count == 0)
                {
                    break;
                }

                foreach (var userId in attendeeUserIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    run.ProcessedCount++;
                    processedThisAttempt++;
                    run.CursorSubscriberTenantUserId = userId;

                    var deduplicationKey = BuildHeavyRedactionDeduplicationKey(request, userId);
                    var alreadyCreated = await notificationRepository.ExistsByDeduplicationKeyAsync(
                        request.TenantId,
                        userId,
                        deduplicationKey,
                        cancellationToken);

                    if (alreadyCreated)
                    {
                        duplicateSkippedThisAttempt++;
                        continue;
                    }

                    await notificationRepository.Create(CreateHeavyRedactionNotification(request, userId, deduplicationKey));
                    run.CreatedNotificationCount++;
                    createdThisAttempt++;
                }

                await fanoutRunRepository.Update(run);

                if (attendeeUserIds.Count < BatchSize)
                {
                    break;
                }
            }

            run.Status = StatusCompleted;
            run.CompletedAt = DateTime.UtcNow;
            run.FailedAt = null;
            run.LastError = null;
            await fanoutRunRepository.Update(run);
            metrics.RecordNotificationFanoutRun(request.TenantId.ToString(), HeavyFanoutKind, OutcomeCompleted);
            metrics.RecordNotificationFanoutSubscribers(processedThisAttempt, request.TenantId.ToString(), HeavyFanoutKind, OutcomeProcessed);
            metrics.RecordNotificationFanoutSubscribers(createdThisAttempt, request.TenantId.ToString(), HeavyFanoutKind, OutcomeNotificationCreated);
            metrics.RecordNotificationFanoutSubscribers(duplicateSkippedThisAttempt, request.TenantId.ToString(), HeavyFanoutKind, OutcomeDuplicateSkipped);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            run.Status = StatusFailed;
            run.FailedAt = DateTime.UtcNow;
            run.LastError = ex.Message;
            await fanoutRunRepository.Update(run);
            metrics.RecordNotificationFanoutRun(request.TenantId.ToString(), HeavyFanoutKind, OutcomeFailed);
            metrics.RecordNotificationFanoutSubscribers(processedThisAttempt, request.TenantId.ToString(), HeavyFanoutKind, OutcomeProcessed);
            metrics.RecordNotificationFanoutSubscribers(createdThisAttempt, request.TenantId.ToString(), HeavyFanoutKind, OutcomeNotificationCreated);
            metrics.RecordNotificationFanoutSubscribers(duplicateSkippedThisAttempt, request.TenantId.ToString(), HeavyFanoutKind, OutcomeDuplicateSkipped);
            logger.LogError(
                ex,
                "Failed heavy moderation notification fanout run {RunId} for moderation record {ModerationRecordId}",
                run.Id,
                request.ModerationRecordId);
            throw;
        }
    }

    private async Task<NotificationFanoutRun> GetOrCreateRunAsync(
        EventLightModeratedNotificationFanoutRequested request,
        CancellationToken cancellationToken)
    {
        var run = await fanoutRunRepository.GetBySourceAsync(
            request.TenantId,
            LightFanoutKind,
            (int)NotificationEntityTypeEnum.Event,
            request.ModerationRecordId,
            request.SourceActorId,
            trackChanges: true,
            cancellationToken);

        if (run is not null)
        {
            return run;
        }

        return await fanoutRunRepository.Create(new NotificationFanoutRun
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Tenant = null!,
            FanoutKind = LightFanoutKind,
            NotificationEntityTypeId = (int)NotificationEntityTypeEnum.Event,
            NotificationEntityType = null!,
            EntityId = request.ModerationRecordId,
            SourceActorId = request.SourceActorId,
            SourceActor = null!,
            Status = StatusProcessing,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid()
        });
    }

    private async Task<NotificationFanoutRun> GetOrCreateHeavyRunAsync(
        EventHeavyRedactedNotificationFanoutRequested request,
        CancellationToken cancellationToken)
    {
        var run = await fanoutRunRepository.GetBySourceAsync(
            request.TenantId,
            HeavyFanoutKind,
            (int)NotificationEntityTypeEnum.Event,
            request.ModerationRecordId,
            request.SourceActorId,
            trackChanges: true,
            cancellationToken);

        if (run is not null)
        {
            return run;
        }

        return await fanoutRunRepository.Create(new NotificationFanoutRun
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Tenant = null!,
            FanoutKind = HeavyFanoutKind,
            NotificationEntityTypeId = (int)NotificationEntityTypeEnum.Event,
            NotificationEntityType = null!,
            EntityId = request.ModerationRecordId,
            SourceActorId = request.SourceActorId,
            SourceActor = null!,
            Status = StatusProcessing,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid()
        });
    }

    private static Notification CreateLightModerationNotification(
        EventLightModeratedNotificationFanoutRequested request,
        Guid userId,
        string deduplicationKey)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Tenant = null!,
            UserId = userId,
            User = null!,
            NotificationTypeId = (int)NotificationTypeEnum.EventUpdated,
            NotificationType = null!,
            Title = $"Event moderated: {request.EventTitle}",
            Body = "This event is temporarily unavailable while it is under moderation.",
            DeduplicationKey = deduplicationKey,
            NotificationEntityTypeId = (int)NotificationEntityTypeEnum.Event,
            EntityId = request.EventId.ToString(),
            NotificationScopeId = (int)ActorTypeEnum.User,
            NotificationScope = null!,
            SourceActorId = request.SourceActorId,
            RecipientContextActorId = null,
            NotificationReasonId = (int)NotificationReasonEnum.System,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string BuildLightModerationDeduplicationKey(
        EventLightModeratedNotificationFanoutRequested request,
        Guid userId)
    {
        return $"event-moderated-light:{request.TenantId:N}:{request.ModerationRecordId:N}:{userId:N}";
    }

    private static Notification CreateHeavyRedactionNotification(
        EventHeavyRedactedNotificationFanoutRequested request,
        Guid userId,
        string deduplicationKey)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Tenant = null!,
            UserId = userId,
            User = null!,
            NotificationTypeId = (int)NotificationTypeEnum.General,
            NotificationType = null!,
            Title = "Event no longer accessible",
            Body = "An event you registered for is no longer accessible after moderation.",
            DeduplicationKey = deduplicationKey,
            NotificationEntityTypeId = null,
            EntityId = null,
            NotificationScopeId = (int)ActorTypeEnum.User,
            NotificationScope = null!,
            SourceActorId = null,
            RecipientContextActorId = null,
            NotificationReasonId = (int)NotificationReasonEnum.System,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string BuildHeavyRedactionDeduplicationKey(
        EventHeavyRedactedNotificationFanoutRequested request,
        Guid userId)
    {
        return $"event-moderated-heavy:{request.TenantId:N}:{request.ModerationRecordId:N}:{userId:N}";
    }
}
