// ABOUTME: Creates durable notification rows for subscribers when an event is published.
// ABOUTME: Uses NotificationFanoutRun progress records and deterministic dedup keys for at-least-once safety.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Services;

public sealed class EventPublishedNotificationFanoutService(
    IActorSubscriptionRepository actorSubscriptionRepository,
    INotificationRepository notificationRepository,
    INotificationFanoutRunRepository fanoutRunRepository,
    BusinessMetrics metrics,
    ILogger<EventPublishedNotificationFanoutService> logger) : IEventPublishedNotificationFanoutService
{
    public const string FanoutKind = "event-published";
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

    public async Task FanoutAsync(EventPublishedNotificationFanoutRequested request, CancellationToken cancellationToken = default)
    {
        var run = await GetOrCreateRunAsync(request, cancellationToken);
        if (string.Equals(run.Status, StatusCompleted, StringComparison.Ordinal))
        {
            metrics.RecordNotificationFanoutRun(request.TenantId.ToString(), FanoutKind, OutcomeSkippedCompleted);
            logger.LogInformation(
                "Skipping completed notification fanout run {RunId} for event {EventId}",
                run.Id,
                request.EventId);
            return;
        }

        run.Status = StatusProcessing;
        run.StartedAt ??= DateTime.UtcNow;
        run.FailedAt = null;
        run.LastError = null;
        await fanoutRunRepository.Update(run);
        metrics.RecordNotificationFanoutRun(request.TenantId.ToString(), FanoutKind, OutcomeProcessing);
        logger.LogInformation(
            "Started notification fanout run {RunId} for event {EventId} and tenant {TenantId}",
            run.Id,
            request.EventId,
            request.TenantId);

        var processedThisAttempt = 0;
        var createdThisAttempt = 0;
        var duplicateSkippedThisAttempt = 0;
        try
        {
            while (true)
            {
                var batch = await actorSubscriptionRepository.GetActiveFanoutBatchAsync(
                    request.TenantId,
                    request.SourceActorId,
                    run.CursorSubscriberTenantUserId,
                    BatchSize,
                    cancellationToken);

                if (batch.Count == 0)
                {
                    break;
                }

                foreach (var subscription in batch)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    run.ProcessedCount++;
                    processedThisAttempt++;
                    run.CursorSubscriberTenantUserId = subscription.SubscriberTenantUserId;

                    var deduplicationKey = BuildDeduplicationKey(request, subscription);
                    var alreadyCreated = await notificationRepository.ExistsByDeduplicationKeyAsync(
                        request.TenantId,
                        subscription.SubscriberUserId,
                        deduplicationKey,
                        cancellationToken);

                    if (alreadyCreated)
                    {
                        duplicateSkippedThisAttempt++;
                        continue;
                    }

                    await notificationRepository.Create(CreateNotification(request, subscription, deduplicationKey));
                    run.CreatedNotificationCount++;
                    createdThisAttempt++;
                }

                await fanoutRunRepository.Update(run);

                if (batch.Count < BatchSize)
                {
                    break;
                }
            }

            run.Status = StatusCompleted;
            run.CompletedAt = DateTime.UtcNow;
            run.FailedAt = null;
            run.LastError = null;
            await fanoutRunRepository.Update(run);
            metrics.RecordNotificationFanoutRun(request.TenantId.ToString(), FanoutKind, OutcomeCompleted);
            metrics.RecordNotificationFanoutSubscribers(processedThisAttempt, request.TenantId.ToString(), FanoutKind, OutcomeProcessed);
            metrics.RecordNotificationFanoutSubscribers(createdThisAttempt, request.TenantId.ToString(), FanoutKind, OutcomeNotificationCreated);
            metrics.RecordNotificationFanoutSubscribers(duplicateSkippedThisAttempt, request.TenantId.ToString(), FanoutKind, OutcomeDuplicateSkipped);
            logger.LogInformation(
                "Completed notification fanout run {RunId} for event {EventId}: processed {ProcessedCount}, created {CreatedCount}, duplicate skipped {DuplicateSkippedCount}",
                run.Id,
                request.EventId,
                processedThisAttempt,
                createdThisAttempt,
                duplicateSkippedThisAttempt);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            run.Status = StatusFailed;
            run.FailedAt = DateTime.UtcNow;
            run.LastError = ex.Message;
            await fanoutRunRepository.Update(run);
            metrics.RecordNotificationFanoutRun(request.TenantId.ToString(), FanoutKind, OutcomeFailed);
            metrics.RecordNotificationFanoutSubscribers(processedThisAttempt, request.TenantId.ToString(), FanoutKind, OutcomeProcessed);
            metrics.RecordNotificationFanoutSubscribers(createdThisAttempt, request.TenantId.ToString(), FanoutKind, OutcomeNotificationCreated);
            metrics.RecordNotificationFanoutSubscribers(duplicateSkippedThisAttempt, request.TenantId.ToString(), FanoutKind, OutcomeDuplicateSkipped);
            logger.LogError(
                ex,
                "Failed notification fanout run {RunId} for event {EventId}: processed {ProcessedCount}, created {CreatedCount}, duplicate skipped {DuplicateSkippedCount}",
                run.Id,
                request.EventId,
                processedThisAttempt,
                createdThisAttempt,
                duplicateSkippedThisAttempt);
            throw;
        }
    }

    private async Task<NotificationFanoutRun> GetOrCreateRunAsync(
        EventPublishedNotificationFanoutRequested request,
        CancellationToken cancellationToken)
    {
        var run = await fanoutRunRepository.GetBySourceAsync(
            request.TenantId,
            FanoutKind,
            (int)NotificationEntityTypeEnum.Event,
            request.EventId,
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
            FanoutKind = FanoutKind,
            NotificationEntityTypeId = (int)NotificationEntityTypeEnum.Event,
            NotificationEntityType = null!,
            EntityId = request.EventId,
            SourceActorId = request.SourceActorId,
            SourceActor = null!,
            Status = StatusProcessing,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid()
        });
    }

    private static Notification CreateNotification(
        EventPublishedNotificationFanoutRequested request,
        ActorSubscription subscription,
        string deduplicationKey)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Tenant = null!,
            UserId = subscription.SubscriberUserId,
            User = null!,
            NotificationTypeId = (int)NotificationTypeEnum.EventCreated,
            NotificationType = null!,
            Title = $"New event: {request.EventTitle}",
            Body = "An organization or group you follow published a new event.",
            DeduplicationKey = deduplicationKey,
            NotificationEntityTypeId = (int)NotificationEntityTypeEnum.Event,
            EntityId = request.EventId.ToString(),
            NotificationScopeId = subscription.TargetActorTypeId,
            NotificationScope = null!,
            SourceActorId = request.SourceActorId,
            RecipientContextActorId = subscription.TargetActorId,
            NotificationReasonId = (int)NotificationReasonEnum.Subscription,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string BuildDeduplicationKey(EventPublishedNotificationFanoutRequested request, ActorSubscription subscription)
    {
        return $"event-published:{request.TenantId:N}:{request.EventId:N}:{subscription.SubscriberTenantUserId:N}";
    }
}
