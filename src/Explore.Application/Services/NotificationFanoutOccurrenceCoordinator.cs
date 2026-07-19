// ABOUTME: Serializes fanout occurrence precedence, coalescing, replay, and supersession decisions.
// ABOUTME: Runs inside the caller's transaction and emits one stable outbox pointer only for a new winner.

using System.Text.Json;
using System.Text.Json.Nodes;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Services;

public sealed class NotificationFanoutOccurrenceCoordinator(
    INotificationFanoutOccurrenceRepository occurrenceRepository,
    INotificationFanoutEmailSuppressionRepository emailSuppressionRepository,
    IOutboxRepository outboxRepository,
    NotificationFanoutRecipientTemplateFactory templateFactory)
{
    public async Task<NotificationFanoutOccurrenceCoordinationResult> CoordinateInCurrentTransactionAsync(
        NotificationFanoutOccurrenceCandidate candidate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        NotificationFanoutOccurrence incoming = CreateNormalizedOccurrence(candidate);
        ClassifiedOccurrence incomingClassification = ClassifyAndValidate(incoming);

        await occurrenceRepository.AcquireSourceThenEventCoordinationLocksAsync(
            candidate.TenantId,
            candidate.SourceType.Trim(),
            candidate.SourceId,
            candidate.AggregateVersion,
            candidate.EventId,
            cancellationToken);
        if (incoming.SessionId.HasValue
            && !await occurrenceRepository.SessionBelongsToEventForCoordinationAsync(
                incoming.TenantId,
                incoming.EventId,
                incoming.SessionId.Value,
                cancellationToken))
        {
            throw new InvalidOperationException("Fanout occurrence session does not belong to its tenant and event.");
        }

        NotificationFanoutOccurrence? replay = await occurrenceRepository
            .GetBySourceIdentityForCoordinationAsync(
                candidate.TenantId,
                candidate.SourceType.Trim(),
                candidate.SourceId,
                candidate.AggregateVersion,
                cancellationToken);
        if (replay is not null)
        {
            await ValidateSourceReplayAsync(incoming, incomingClassification, replay, cancellationToken);
            NotificationFanoutOccurrence active = await ResolveActiveOccurrenceAsync(replay, cancellationToken);
            return new(
                NotificationFanoutOccurrenceCoordinationOutcome.SourceReplay,
                replay,
                active.Id,
                PointerCreated: false);
        }

        IReadOnlyList<NotificationFanoutOccurrence> pending = await occurrenceRepository
            .GetPendingForEventCoordinationAsync(candidate.TenantId, candidate.EventId, cancellationToken);
        var classifiedPending = pending
            .Select(occurrence => new ClassifiedOccurrenceEntry(occurrence, ClassifyAndValidate(occurrence)))
            .Where(entry => ScopesCompete(incomingClassification, entry.Classification))
            .ToArray();

        ClassifiedOccurrenceEntry? blocker = classifiedPending
            .Where(entry => ExistingBlocksIncoming(entry, incoming, incomingClassification))
            .OrderByDescending(entry => entry.Classification.Priority)
            .ThenBy(entry => entry.Occurrence.OccurredAt)
            .ThenBy(entry => entry.Occurrence.Id)
            .FirstOrDefault();
        if (blocker is not null)
        {
            incoming.Supersede(blocker.Occurrence.Id, BlockingReason(blocker.Classification.Kind), incoming.OccurredAt);
            await occurrenceRepository.Create(incoming);
            return new(
                NotificationFanoutOccurrenceCoordinationOutcome.Superseded,
                incoming,
                blocker.Occurrence.Id,
                PointerCreated: false);
        }

        ClassifiedOccurrenceEntry[] superseded = classifiedPending
            .Where(entry => IncomingSupersedesExisting(incoming, incomingClassification, entry))
            .ToArray();
        NotificationFanoutOccurrence winner = incomingClassification.Kind == NotificationFanoutOccurrenceKind.ImportantUpdate
            ? CreateCoalescedUpdate(incoming, superseded)
            : incoming;

        await occurrenceRepository.Create(winner);
        foreach (ClassifiedOccurrenceEntry loser in superseded)
        {
            loser.Occurrence.Supersede(winner.Id, SupersessionReason(incomingClassification.Kind, loser.Classification.Kind), incoming.OccurredAt);
            if (!await occurrenceRepository.TryPersistSupersessionAsync(loser.Occurrence, cancellationToken))
            {
                throw new InvalidOperationException("A pending fanout occurrence changed after coordination acquired its event lock.");
            }

            await emailSuppressionRepository.SuppressPreHandoffAsync(
                loser.Occurrence.TenantId,
                loser.Occurrence.Id,
                incoming.OccurredAt,
                cancellationToken);
        }

        OutboxMessage pointer = NotificationFanoutOccurrenceOutboxMessageFactory.Create(
            winner,
            candidate.PointerOutboxMessageId);
        await outboxRepository.Create(pointer);
        return new(
            NotificationFanoutOccurrenceCoordinationOutcome.NewlyActive,
            winner,
            winner.Id,
            PointerCreated: true);
    }

    private NotificationFanoutOccurrence CreateNormalizedOccurrence(NotificationFanoutOccurrenceCandidate candidate)
    {
        ValidateCandidate(candidate);
        DateTime occurredAt = AtPostgresPrecision(candidate.OccurredAt);
        DateTime audienceCutoffAt = AtPostgresPrecision(candidate.AudienceCutoffAt);
        DateTime requestedNotBefore = AtPostgresPrecision(candidate.RequestedNotBefore);
        NotificationFanoutOccurrenceKind kind = IdentifyKind(
            candidate.TemplateKey,
            candidate.DeliveryPolicyId,
            candidate.SessionId);
        int priority = Priority(kind);
        DateTime notBefore = kind switch
        {
            NotificationFanoutOccurrenceKind.ImportantUpdate => occurredAt.Add(NotificationFanoutOccurrenceCoordinationPolicy.ImportantUpdateWindow),
            NotificationFanoutOccurrenceKind.Reminder => requestedNotBefore,
            _ => occurredAt
        };
        DateTime? windowEndsAt = kind == NotificationFanoutOccurrenceKind.ImportantUpdate
            ? notBefore
            : null;

        return NotificationFanoutOccurrence.Create(
            candidate.OccurrenceId,
            candidate.TenantId,
            candidate.EventId,
            candidate.SessionId,
            occurredAt,
            audienceCutoffAt,
            candidate.AggregateVersion,
            candidate.ChangeSetJson,
            candidate.SafeBeforeSnapshotJson,
            candidate.SafeAfterSnapshotJson,
            candidate.TemplateKey,
            candidate.TemplateVersion,
            candidate.DeliveryPolicyId,
            candidate.PolicyVersion,
            priority,
            notBefore,
            candidate.SourceType.Trim(),
            candidate.SourceId,
            CoalescingKey(candidate.EventId, candidate.SessionId),
            windowEndsAt);
    }

    private ClassifiedOccurrence ClassifyAndValidate(NotificationFanoutOccurrence occurrence)
    {
        NotificationFanoutOccurrenceKind kind = IdentifyKind(
            occurrence.TemplateKey,
            occurrence.DeliveryPolicyId,
            occurrence.SessionId);
        int priority = Priority(kind);
        if (occurrence.TemplateVersion != NotificationFanoutRecipientTemplateFactory.CurrentTemplateVersion
            || occurrence.PolicyVersion != NotificationFanoutRecipientTemplateFactory.CurrentPolicyVersion
            || occurrence.Priority != priority
            || !string.Equals(occurrence.CoalescingKey, CoalescingKey(occurrence.EventId, occurrence.SessionId), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Fanout occurrence coordination metadata is unsupported.");
        }

        DateTime? expectedWindowEnd = kind == NotificationFanoutOccurrenceKind.ImportantUpdate
            ? occurrence.OccurredAt.Add(NotificationFanoutOccurrenceCoordinationPolicy.ImportantUpdateWindow)
            : null;
        if (kind == NotificationFanoutOccurrenceKind.ImportantUpdate
            && (occurrence.NotBefore != expectedWindowEnd || occurrence.CoalescingWindowEndsAt != expectedWindowEnd)
            || kind == NotificationFanoutOccurrenceKind.Reminder
            && (occurrence.NotBefore < occurrence.OccurredAt || occurrence.CoalescingWindowEndsAt.HasValue)
            || kind is not NotificationFanoutOccurrenceKind.ImportantUpdate and not NotificationFanoutOccurrenceKind.Reminder
            && (occurrence.NotBefore != occurrence.OccurredAt || occurrence.CoalescingWindowEndsAt.HasValue))
        {
            throw new InvalidOperationException("Fanout occurrence timing does not match its coordination policy.");
        }

        NotificationFanoutRecipientTemplate? template = kind is NotificationFanoutOccurrenceKind.ImportantUpdate
            or NotificationFanoutOccurrenceKind.SessionCancellation
            or NotificationFanoutOccurrenceKind.EventCancellation
                ? templateFactory.Parse(occurrence)
                : null;
        if (template is null)
        {
            ValidateJsonObject(occurrence.ChangeSetJson, "change set");
            ValidateJsonObject(occurrence.SafeBeforeSnapshotJson, "before snapshot");
            ValidateJsonObject(occurrence.SafeAfterSnapshotJson, "after snapshot");
        }

        return new(kind, priority, occurrence.SessionId, template);
    }

    private async Task ValidateSourceReplayAsync(
        NotificationFanoutOccurrence incoming,
        ClassifiedOccurrence incomingClassification,
        NotificationFanoutOccurrence replay,
        CancellationToken cancellationToken)
    {
        ClassifiedOccurrence replayClassification = ClassifyAndValidate(replay);
        bool compatible = replay.Id == incoming.Id
            && replay.TenantId == incoming.TenantId
            && replay.EventId == incoming.EventId
            && replay.SessionId == incoming.SessionId
            && replay.OccurredAt == incoming.OccurredAt
            && replay.AudienceCutoffAt == incoming.AudienceCutoffAt
            && replay.AggregateVersion == incoming.AggregateVersion
            && replay.TemplateKey == incoming.TemplateKey
            && replay.TemplateVersion == incoming.TemplateVersion
            && replay.DeliveryPolicyId == incoming.DeliveryPolicyId
            && replay.PolicyVersion == incoming.PolicyVersion
            && replay.Priority == incoming.Priority
            && replay.NotBefore == incoming.NotBefore
            && replay.SourceType == incoming.SourceType
            && replay.SourceId == incoming.SourceId
            && replay.CoalescingKey == incoming.CoalescingKey
            && replay.CoalescingWindowEndsAt == incoming.CoalescingWindowEndsAt
            && JsonEquals(replay.SafeAfterSnapshotJson, incoming.SafeAfterSnapshotJson)
            && replayClassification.Kind == incomingClassification.Kind;
        if (!compatible)
        {
            throw new InvalidOperationException("A fanout occurrence source identity was reused with incompatible immutable content.");
        }

        if (incomingClassification.Kind != NotificationFanoutOccurrenceKind.ImportantUpdate)
        {
            compatible = JsonEquals(replay.ChangeSetJson, incoming.ChangeSetJson)
                && JsonEquals(replay.SafeBeforeSnapshotJson, incoming.SafeBeforeSnapshotJson);
        }
        else
        {
            NotificationFanoutChangeField[] replayFields = replayClassification.Template!.ChangeSet.Fields;
            NotificationFanoutChangeField[] incomingFields = incomingClassification.Template!.ChangeSet.Fields;
            compatible = incomingFields.All(replayFields.Contains);
            if (compatible && !JsonEquals(replay.SafeBeforeSnapshotJson, incoming.SafeBeforeSnapshotJson))
            {
                IReadOnlyList<NotificationFanoutOccurrence> predecessors = await occurrenceRepository
                    .GetDirectPredecessorsForCoordinationAsync(
                        incoming.TenantId,
                        incoming.EventId,
                        replay.Id,
                        cancellationToken);
                compatible = predecessors.Any(predecessor =>
                    predecessor.SessionId == incoming.SessionId
                    && predecessor.TemplateKey == incoming.TemplateKey
                    && JsonEquals(predecessor.SafeAfterSnapshotJson, incoming.SafeBeforeSnapshotJson));
            }
        }

        if (!compatible)
        {
            throw new InvalidOperationException("A fanout occurrence source identity was reused with incompatible immutable content.");
        }
    }

    private async Task<NotificationFanoutOccurrence> ResolveActiveOccurrenceAsync(
        NotificationFanoutOccurrence occurrence,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<Guid>();
        NotificationFanoutOccurrence current = occurrence;
        while (current.State == NotificationFanoutOccurrenceState.Superseded)
        {
            if (!visited.Add(current.Id)
                || !current.SupersededByOccurrenceId.HasValue)
            {
                throw new InvalidOperationException("Fanout occurrence supersession contains a cycle or missing replacement.");
            }

            NotificationFanoutOccurrence replacement = await occurrenceRepository.GetByIdForCoordinationAsync(
                    occurrence.TenantId,
                    current.SupersededByOccurrenceId.Value,
                    cancellationToken)
                ?? throw new InvalidOperationException("Fanout occurrence supersession points to a missing replacement.");
            if (replacement.EventId != occurrence.EventId)
            {
                throw new InvalidOperationException("Fanout occurrence supersession crossed its event boundary.");
            }

            ClassifiedOccurrence currentClassification = ClassifyAndValidate(current);
            ClassifiedOccurrence replacementClassification = ClassifyAndValidate(replacement);
            bool validTransition = ScopesCompete(currentClassification, replacementClassification)
                && replacementClassification.Priority >= currentClassification.Priority
                && (currentClassification.Kind != NotificationFanoutOccurrenceKind.ImportantUpdate
                    || replacementClassification.Priority > currentClassification.Priority
                    || CompareOccurrenceOrder(replacement, current) > 0);
            if (!validTransition)
            {
                throw new InvalidOperationException("Fanout occurrence supersession violates scope or precedence ordering.");
            }

            current = replacement;
        }

        if (!visited.Add(current.Id)
            || current.State != NotificationFanoutOccurrenceState.Pending)
        {
            throw new InvalidOperationException("Fanout occurrence supersession has no active pending winner.");
        }

        return current;
    }

    private NotificationFanoutOccurrence CreateCoalescedUpdate(
        NotificationFanoutOccurrence incoming,
        IReadOnlyList<ClassifiedOccurrenceEntry> superseded)
    {
        ClassifiedOccurrenceEntry? latestUpdate = superseded
            .Where(entry => entry.Classification.Kind == NotificationFanoutOccurrenceKind.ImportantUpdate)
            .OrderByDescending(entry => entry.Occurrence.OccurredAt)
            .ThenByDescending(entry => entry.Occurrence.Id)
            .FirstOrDefault();
        if (latestUpdate is null
            || !latestUpdate.Occurrence.CoalescingWindowEndsAt.HasValue
            || incoming.OccurredAt > latestUpdate.Occurrence.CoalescingWindowEndsAt.Value)
        {
            return incoming;
        }

        NotificationFanoutChangeField[] fields = latestUpdate.Classification.Template!.ChangeSet.Fields
            .Concat(ClassifyAndValidate(incoming).Template!.ChangeSet.Fields)
            .Distinct()
            .OrderBy(field => (int)field)
            .ToArray();
        return NotificationFanoutOccurrence.Create(
            incoming.Id,
            incoming.TenantId,
            incoming.EventId,
            incoming.SessionId,
            incoming.OccurredAt,
            incoming.AudienceCutoffAt,
            incoming.AggregateVersion,
            NotificationFanoutTemplateJson.Serialize(new NotificationFanoutChangeSetV1(fields)),
            latestUpdate.Occurrence.SafeBeforeSnapshotJson,
            incoming.SafeAfterSnapshotJson,
            incoming.TemplateKey,
            incoming.TemplateVersion,
            incoming.DeliveryPolicyId,
            incoming.PolicyVersion,
            incoming.Priority,
            incoming.NotBefore,
            incoming.SourceType,
            incoming.SourceId,
            incoming.CoalescingKey,
            incoming.CoalescingWindowEndsAt);
    }

    private static bool ExistingBlocksIncoming(
        ClassifiedOccurrenceEntry existing,
        NotificationFanoutOccurrence incoming,
        ClassifiedOccurrence incomingClassification)
    {
        if (existing.Classification.Priority > incomingClassification.Priority)
        {
            return true;
        }

        if (existing.Classification.Priority < incomingClassification.Priority)
        {
            return false;
        }

        return incomingClassification.Kind != NotificationFanoutOccurrenceKind.ImportantUpdate
            || CompareOccurrenceOrder(existing.Occurrence, incoming) >= 0;
    }

    private static bool IncomingSupersedesExisting(
        NotificationFanoutOccurrence incoming,
        ClassifiedOccurrence incomingClassification,
        ClassifiedOccurrenceEntry existing)
    {
        if (incomingClassification.Priority > existing.Classification.Priority)
        {
            return true;
        }

        return incomingClassification.Kind == NotificationFanoutOccurrenceKind.ImportantUpdate
            && incomingClassification.Priority == existing.Classification.Priority
            && CompareOccurrenceOrder(incoming, existing.Occurrence) > 0;
    }

    private static bool ScopesCompete(ClassifiedOccurrence left, ClassifiedOccurrence right)
    {
        if (left.Kind == NotificationFanoutOccurrenceKind.HeavyModerationUnavailable
            || right.Kind == NotificationFanoutOccurrenceKind.HeavyModerationUnavailable
            || left.Kind == NotificationFanoutOccurrenceKind.EventCancellation
            || right.Kind == NotificationFanoutOccurrenceKind.EventCancellation)
        {
            return true;
        }

        if (left.Kind == NotificationFanoutOccurrenceKind.SessionCancellation)
        {
            return left.SessionId == right.SessionId;
        }

        if (right.Kind == NotificationFanoutOccurrenceKind.SessionCancellation)
        {
            return right.SessionId == left.SessionId;
        }

        return left.SessionId == right.SessionId;
    }

    private static int CompareOccurrenceOrder(NotificationFanoutOccurrence left, NotificationFanoutOccurrence right)
    {
        int occurredAtComparison = left.OccurredAt.CompareTo(right.OccurredAt);
        return occurredAtComparison != 0 ? occurredAtComparison : left.Id.CompareTo(right.Id);
    }

    private static NotificationFanoutOccurrenceKind IdentifyKind(
        string templateKey,
        int deliveryPolicyId,
        Guid? sessionId) => (templateKey, deliveryPolicyId, sessionId.HasValue) switch
        {
            (NotificationFanoutOccurrenceCoordinationPolicy.HeavyModerationUnavailableTemplateKey,
                (int)NotificationDeliveryPolicyEnum.ModerationAvailabilityRequired, false) =>
                NotificationFanoutOccurrenceKind.HeavyModerationUnavailable,
            (NotificationFanoutRecipientTemplateFactory.EventCancelledTemplateKey,
                (int)NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional, false) =>
                NotificationFanoutOccurrenceKind.EventCancellation,
            (NotificationFanoutRecipientTemplateFactory.SessionCancelledTemplateKey,
                (int)NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional, true) =>
                NotificationFanoutOccurrenceKind.SessionCancellation,
            (NotificationFanoutRecipientTemplateFactory.EventUpdatedTemplateKey,
                (int)NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional, false) =>
                NotificationFanoutOccurrenceKind.ImportantUpdate,
            (NotificationFanoutRecipientTemplateFactory.SessionUpdatedTemplateKey,
                (int)NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional, true) =>
                NotificationFanoutOccurrenceKind.ImportantUpdate,
            (NotificationFanoutOccurrenceCoordinationPolicy.EventReminderTemplateKey,
                (int)NotificationDeliveryPolicyEnum.ReminderOptional, false) =>
                NotificationFanoutOccurrenceKind.Reminder,
            (NotificationFanoutOccurrenceCoordinationPolicy.SessionReminderTemplateKey,
                (int)NotificationDeliveryPolicyEnum.ReminderOptional, true) =>
                NotificationFanoutOccurrenceKind.Reminder,
            _ => throw new InvalidOperationException("Fanout occurrence template, policy, and scope are unsupported.")
        };

    private static int Priority(NotificationFanoutOccurrenceKind kind) => kind switch
    {
        NotificationFanoutOccurrenceKind.Reminder => NotificationFanoutOccurrenceCoordinationPolicy.ReminderPriority,
        NotificationFanoutOccurrenceKind.ImportantUpdate => NotificationFanoutOccurrenceCoordinationPolicy.ImportantUpdatePriority,
        NotificationFanoutOccurrenceKind.SessionCancellation => NotificationFanoutOccurrenceCoordinationPolicy.SessionCancellationPriority,
        NotificationFanoutOccurrenceKind.EventCancellation => NotificationFanoutOccurrenceCoordinationPolicy.EventCancellationPriority,
        NotificationFanoutOccurrenceKind.HeavyModerationUnavailable => NotificationFanoutOccurrenceCoordinationPolicy.HeavyModerationUnavailablePriority,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static string CoalescingKey(Guid eventId, Guid? sessionId) => sessionId.HasValue
        ? $"event:{eventId:N}:session:{sessionId.Value:N}"
        : $"event:{eventId:N}";

    private static string BlockingReason(NotificationFanoutOccurrenceKind kind) => kind switch
    {
        NotificationFanoutOccurrenceKind.HeavyModerationUnavailable => "blocked_by_heavy_moderation",
        NotificationFanoutOccurrenceKind.EventCancellation => "blocked_by_event_cancellation",
        NotificationFanoutOccurrenceKind.SessionCancellation => "blocked_by_session_cancellation",
        NotificationFanoutOccurrenceKind.ImportantUpdate => "blocked_by_newer_update",
        NotificationFanoutOccurrenceKind.Reminder => "duplicate_reminder",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static string SupersessionReason(
        NotificationFanoutOccurrenceKind winner,
        NotificationFanoutOccurrenceKind loser) => winner switch
        {
            NotificationFanoutOccurrenceKind.HeavyModerationUnavailable => "superseded_by_heavy_moderation",
            NotificationFanoutOccurrenceKind.EventCancellation => "superseded_by_event_cancellation",
            NotificationFanoutOccurrenceKind.SessionCancellation => "superseded_by_session_cancellation",
            NotificationFanoutOccurrenceKind.ImportantUpdate when loser == NotificationFanoutOccurrenceKind.ImportantUpdate => "superseded_by_newer_update",
            _ => "superseded_by_higher_precedence"
        };

    private static void ValidateCandidate(NotificationFanoutOccurrenceCandidate candidate)
    {
        if (candidate.OccurrenceId == Guid.Empty
            || candidate.PointerOutboxMessageId == Guid.Empty
            || candidate.TenantId == Guid.Empty
            || candidate.EventId == Guid.Empty
            || candidate.AggregateVersion == Guid.Empty
            || candidate.SourceId == Guid.Empty)
        {
            throw new ArgumentException("Fanout candidate identifiers must be non-empty.", nameof(candidate));
        }

        if (candidate.OccurredAt.Kind != DateTimeKind.Utc
            || candidate.AudienceCutoffAt.Kind != DateTimeKind.Utc
            || candidate.RequestedNotBefore.Kind != DateTimeKind.Utc
            || candidate.AudienceCutoffAt > candidate.OccurredAt
            || candidate.RequestedNotBefore < candidate.OccurredAt)
        {
            throw new ArgumentException("Fanout candidate timestamps must be UTC and cannot schedule before occurrence.", nameof(candidate));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(candidate.SourceType);
    }

    private static void ValidateJsonObject(string json, string field)
    {
        JsonNode? value = JsonNode.Parse(json);
        if (value is not JsonObject)
        {
            throw new JsonException($"Fanout {field} must be a JSON object.");
        }
    }

    private static bool JsonEquals(string left, string right) =>
        JsonNode.DeepEquals(JsonNode.Parse(left), JsonNode.Parse(right));

    private static DateTime AtPostgresPrecision(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerMicrosecond, DateTimeKind.Utc);

    private sealed record ClassifiedOccurrence(
        NotificationFanoutOccurrenceKind Kind,
        int Priority,
        Guid? SessionId,
        NotificationFanoutRecipientTemplate? Template);

    private sealed record ClassifiedOccurrenceEntry(
        NotificationFanoutOccurrence Occurrence,
        ClassifiedOccurrence Classification);
}
