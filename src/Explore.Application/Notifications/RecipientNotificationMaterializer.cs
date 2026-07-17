// ABOUTME: Builds one recipient's intent, channel decisions, in-app row, and SMTP work as one persistence graph.
// ABOUTME: Owns exact deduplication recovery only after the failed transaction has rolled back.

using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Notifications;

public sealed class RecipientNotificationMaterializer(
    IRecipientNotificationGraphRepository notificationGraphRepository,
    IUnitOfWork unitOfWork) : IRecipientNotificationMaterializer
{
    public async Task<RecipientNotificationMaterializationResult> MaterializeAsync(
        RecipientNotificationMaterialization request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(
                ct => MaterializeGraphAsync(request, ct),
                cancellationToken);
        }
        catch (NotificationIntentDeduplicationConflictException)
        {
            return await unitOfWork.ExecuteInTransactionAsync(
                ct => LoadWinningGraphAsync(request, ct),
                cancellationToken);
        }
    }

    public Task<RecipientNotificationMaterializationResult> MaterializeInCurrentTransactionAsync(
        RecipientNotificationMaterialization request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        return MaterializeGraphAsync(request, cancellationToken);
    }

    private async Task<RecipientNotificationMaterializationResult> MaterializeGraphAsync(
        RecipientNotificationMaterialization request,
        CancellationToken cancellationToken)
    {
        var graph = BuildGraph(request);
        await notificationGraphRepository.CreateGraphAsync(graph.Intent, cancellationToken);
        return graph;
    }

    private async Task<RecipientNotificationMaterializationResult> LoadWinningGraphAsync(
        RecipientNotificationMaterialization request,
        CancellationToken cancellationToken)
    {
        Guid tenantId = request.Intent.TenantId!.Value;
        NotificationIntent? winner = await notificationGraphRepository.GetGraphByTenantAndDeduplicationKeyAsync(
            tenantId,
            request.Intent.DeduplicationKey!,
            cancellationToken);
        if (winner is null)
        {
            throw new InvalidOperationException("The winning notification intent was not visible after exact conflict rollback.");
        }

        RecipientNotificationMaterializationResult expected = BuildGraph(request with { IntentId = winner.Id });
        await notificationGraphRepository.RepairMissingRecipientDeliveryRowsAsync(
            winner,
            expected.Deliveries,
            expected.Notification,
            expected.Email,
            cancellationToken);

        NotificationIntent repaired = await notificationGraphRepository.GetGraphByTenantAndDeduplicationKeyAsync(
                tenantId,
                request.Intent.DeduplicationKey!,
                cancellationToken)
            ?? throw new InvalidOperationException("The repaired notification graph could not be loaded.");
        return ToResult(repaired);
    }

    private static RecipientNotificationMaterializationResult BuildGraph(RecipientNotificationMaterialization request)
    {
        NotificationIntentDraft draft = request.Intent;
        Guid tenantId = draft.TenantId!.Value;
        Guid recipientUserId = draft.UserId!.Value;
        DateTime now = DateTime.UtcNow;

        var intent = new NotificationIntent
        {
            Id = request.IntentId,
            TenantId = tenantId,
            CategoryId = MapCategory(draft.Category),
            OwnershipTypeId = (int)NotificationOwnershipTypeEnum.IslamuEvent,
            RecipientKindId = MapRecipientKind(draft.RecipientKind),
            StatusId = request.Email is null
                ? (int)NotificationIntentStatusEnum.Resolved
                : (int)NotificationIntentStatusEnum.DispatchQueued,
            TemplateKey = draft.TemplateKey!,
            DeduplicationKey = draft.DeduplicationKey!,
            SafePayloadReference = BlankToNull(draft.SafePayloadReference),
            SafePayloadHash = BlankToNull(draft.SafePayloadHash),
            CorrelationId = BlankToNull(draft.CorrelationId),
            RecipientUserId = recipientUserId,
            EventId = draft.EventId,
            ReportId = draft.ReportId,
            ReportDecisionId = draft.ReportDecisionId,
            CreatedAt = now
        };

        Notification? notification = null;
        if (request.InApp is not null)
        {
            RecipientInAppNotificationDraft inApp = request.InApp;
            notification = new Notification
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantId,
                Tenant = null!,
                NotificationIntentId = intent.Id,
                NotificationIntent = intent,
                UserId = recipientUserId,
                User = null!,
                NotificationTypeId = inApp.NotificationTypeId,
                NotificationType = null!,
                Title = inApp.Title.Trim(),
                Body = BlankToNull(inApp.Body),
                DeduplicationKey = $"{draft.DeduplicationKey}:in-app",
                NotificationEntityTypeId = inApp.NotificationEntityTypeId,
                EntityId = BlankToNull(inApp.EntityId),
                NotificationScopeId = inApp.NotificationScopeId,
                NotificationScope = null!,
                NotificationReasonId = inApp.NotificationReasonId,
                CreatedAt = now
            };
            intent.Deliveries.Add(CreateDelivery(
                request,
                NotificationPreferenceChannelEnum.InApp,
                inApp.IsRequired,
                NotificationDeliveryStatusEnum.Delivered,
                now,
                notification,
                null,
                null));
        }

        EmailDispatchOutbox? email = request.Email;
        if (request.IncludeEmailChannel)
        {
            if (email is not null)
            {
                if (email.RecipientUserId != recipientUserId || email.TenantId != tenantId)
                {
                    throw new InvalidOperationException("Email recipient authority must match the notification intent tenant and user.");
                }

                email.Id = email.Id == Guid.Empty ? Guid.CreateVersion7() : email.Id;
                email.NotificationIntentId = intent.Id;
                email.NotificationIntent = intent;
                intent.Deliveries.Add(CreateDelivery(
                    request,
                    NotificationPreferenceChannelEnum.Email,
                    request.EmailRequired,
                    NotificationDeliveryStatusEnum.Queued,
                    now,
                    null,
                    email,
                    null));
            }
            else
            {
                intent.Deliveries.Add(CreateDelivery(
                    request,
                    NotificationPreferenceChannelEnum.Email,
                    request.EmailRequired,
                    NotificationDeliveryStatusEnum.Skipped,
                    now,
                    null,
                    null,
                    string.IsNullOrWhiteSpace(request.EmailSkipReason) ? "email_not_eligible" : request.EmailSkipReason.Trim()));
            }
        }

        return new RecipientNotificationMaterializationResult(intent, intent.Deliveries.ToArray(), notification, email);
    }

    private static NotificationDelivery CreateDelivery(
        RecipientNotificationMaterialization request,
        NotificationPreferenceChannelEnum channel,
        bool isRequired,
        NotificationDeliveryStatusEnum status,
        DateTime now,
        Notification? notification,
        EmailDispatchOutbox? email,
        string? failureCategory)
    {
        return new NotificationDelivery
        {
            Id = Guid.CreateVersion7(),
            TenantId = request.Intent.TenantId!.Value,
            NotificationIntentId = request.IntentId,
            ChannelId = (int)channel,
            DeliveryPolicyId = (int)request.DeliveryPolicy,
            IsRequired = isRequired,
            PolicyVersion = request.PolicyVersion,
            ConsentPurpose = request.ConsentPurpose,
            ConsentVersion = request.ConsentVersion,
            PreferenceCategoryCode = request.PreferenceCategoryCode,
            PreferenceEnabled = channel == NotificationPreferenceChannelEnum.Email
                ? request.EmailPreferenceEnabled
                : null,
            RecipientAddressSource = email?.RecipientAddressSource,
            DisclosureLevel = request.DisclosureLevel,
            TemplateKey = request.Intent.TemplateKey!,
            TemplateVersion = request.TemplateVersion,
            LinkAllowed = request.LinkAllowed,
            NotificationId = notification?.Id,
            Notification = notification,
            EmailDispatchOutboxId = email?.Id,
            EmailDispatchOutbox = email,
            StatusId = (int)status,
            FailureCategory = failureCategory,
            QueuedAt = status == NotificationDeliveryStatusEnum.Queued ? now : null,
            CompletedAt = status is NotificationDeliveryStatusEnum.Delivered or NotificationDeliveryStatusEnum.Skipped ? now : null,
            CreatedAt = now
        };
    }

    private static RecipientNotificationMaterializationResult ToResult(NotificationIntent intent)
    {
        NotificationDelivery[] deliveries = intent.Deliveries.ToArray();
        return new RecipientNotificationMaterializationResult(
            intent,
            deliveries,
            deliveries.Select(delivery => delivery.Notification).FirstOrDefault(value => value is not null),
            deliveries.Select(delivery => delivery.EmailDispatchOutbox).FirstOrDefault(value => value is not null));
    }

    private static void Validate(RecipientNotificationMaterialization request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.IntentId == Guid.Empty
            || request.Intent.TenantId is null
            || request.Intent.TenantId == Guid.Empty
            || request.Intent.UserId is null
            || request.Intent.UserId == Guid.Empty)
        {
            throw new InvalidOperationException("A non-empty intent, tenant, and recipient user id are required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(request.Intent.TemplateKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Intent.DeduplicationKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DisclosureLevel);
    }

    private static int MapCategory(NotificationCategory category) =>
        Enum.TryParse<NotificationCategoryEnum>(category.ToString(), out var mapped)
            ? (int)mapped
            : throw new InvalidOperationException($"Unsupported notification category '{category}'.");

    private static int MapRecipientKind(string? recipientKind) =>
        Enum.TryParse<NotificationRecipientKindEnum>(recipientKind, true, out var mapped)
            ? (int)mapped
            : (int)NotificationRecipientKindEnum.User;

    private static string? BlankToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
