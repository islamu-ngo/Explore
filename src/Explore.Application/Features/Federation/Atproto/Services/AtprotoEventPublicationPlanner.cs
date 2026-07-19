// ABOUTME: Plans every outbound event lifecycle operation through one network-free governed outbox gate.
// ABOUTME: Requires capability, self-consent, an exact encrypted session, exhaustive valid payload, and existing ownership for mutations.

using System.Security.Cryptography;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Services.Federation;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Federation.Atproto.Services;

public sealed record AtprotoEventPublicationInput(
    Guid TenantId,
    Guid UserId,
    Guid EventId,
    Guid SourceVersion,
    PdsSyncOperation Operation,
    Guid OutboxId,
    DateTime CreatedAtUtc,
    bool RestoreOnly = false);

public sealed record AtprotoRsvpPublicationInput(
    Guid TenantId,
    Guid UserId,
    Guid EventId,
    Guid RegistrationIntentId,
    Guid SourceVersion,
    PdsSyncOperation Operation,
    Guid OutboxId,
    DateTime CreatedAtUtc);

public sealed record AtprotoPublicationPlanningResult(
    bool Enqueued,
    string? ReasonCode,
    PdsSyncOutbox? Outbox)
{
    public static AtprotoPublicationPlanningResult Skipped(string reasonCode) => new(false, reasonCode, null);
    public static AtprotoPublicationPlanningResult Added(PdsSyncOutbox outbox) => new(true, null, outbox);
}

public sealed record AtprotoDeliveryGateResult(bool Allowed, string? ReasonCode)
{
    public static AtprotoDeliveryGateResult Permit() => new(true, null);
    public static AtprotoDeliveryGateResult Deny(string reasonCode) => new(false, reasonCode);
}

public sealed record AtprotoRsvpReconciliationResult(
    Guid? NextIntentId,
    int Examined,
    int Enqueued);

public interface IAtprotoDeliveryGate
{
    Task<AtprotoDeliveryGateResult> CheckDeliveryAsync(
        PdsSyncOutbox outbox,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken);
}

public sealed class AtprotoEventPublicationPlanner(
    AtprotoEventGovernanceResolver governanceResolver,
    IEventRepository eventRepository,
    IEventRegistrationIntentRepository registrationIntentRepository,
    IAtprotoRecordRepository recordRepository,
    IUserAuthenticationTokenRepository sessionRepository,
    IUserExternalLoginRepository externalLoginRepository,
    IAtprotoPublicationPayloadBuilder payloadBuilder,
    IPdsSyncOutboxRepository outboxRepository,
    ILogger<AtprotoEventPublicationPlanner> logger) : IAtprotoDeliveryGate
{
    public const string EventCollection = "community.lexicon.calendar.event";
    public const string EventSourceType = "Event";
    public const string RsvpCollection = "community.lexicon.calendar.rsvp";
    public const string RsvpSourceType = "EventRegistrationIntent";
    private const string EmptyPayloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    private static readonly TimeSpan MaximumRemoteCallWindow = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan CompensationSafetyWindow = TimeSpan.FromSeconds(5);

    private AtprotoPublicationPlanningResult Skipped(
        string sourceEntityType,
        PdsSyncOperation operation,
        string reasonCode)
    {
        logger.LogInformation(
            "ATProto publication planning skipped for {SourceEntityType} {Operation} with bounded reason {ReasonCode}",
            sourceEntityType,
            operation,
            reasonCode);
        return AtprotoPublicationPlanningResult.Skipped(reasonCode);
    }

    private AtprotoPublicationPlanningResult Skipped(
        AtprotoEventPublicationInput request,
        string reasonCode) => Skipped(EventSourceType, request.Operation, reasonCode);

    private AtprotoPublicationPlanningResult Skipped(
        AtprotoRsvpPublicationInput request,
        string reasonCode) => Skipped(RsvpSourceType, request.Operation, reasonCode);

    public async Task<AtprotoPublicationPlanningResult> PlanEventAsync(
        AtprotoEventPublicationInput request,
        CancellationToken cancellationToken)
    {
        Validate(request);

        var ownerUserId = request.UserId;
        var recordKey = request.OutboxId.ToString("N");
        Guid? atprotoRecordId = null;
        string? expectedCid = null;
        var plannedOperation = request.Operation;
        PdsSyncOutbox? unsettledPredecessor = await outboxRepository.GetLatestUnsettledMutationAsync(
            request.TenantId,
            EventSourceType,
            request.EventId,
            EventCollection,
            cancellationToken);
        AtprotoOutboundRecordOwnership? ownership = await recordRepository.GetOwnedRecordForSourceAsync(
            request.TenantId,
            EventSourceType,
            request.EventId,
            cancellationToken);
        if (request.Operation != PdsSyncOperation.Create)
        {
            if (unsettledPredecessor is not null)
            {
                ownerUserId = unsettledPredecessor.UserId;
                recordKey = unsettledPredecessor.RecordKey;
                atprotoRecordId = ownership?.AtprotoRecord?.Id;
            }
            else if (ownership?.AtprotoRecord is { TombstonedAt: null } record)
            {
                ownerUserId = ownership.UserId;
                recordKey = record.RecordKey;
                atprotoRecordId = record.Id;
                expectedCid = record.Cid;
            }
            else
            {
                return Skipped(request, "remote_record_missing");
            }
        }
        else if (request.RestoreOnly)
        {
            if (unsettledPredecessor is not null)
            {
                ownerUserId = unsettledPredecessor.UserId;
                recordKey = unsettledPredecessor.RecordKey;
                atprotoRecordId = ownership?.AtprotoRecord?.Id;
                plannedOperation = PdsSyncOperation.Update;
            }
            else if (ownership?.AtprotoRecord is { TombstonedAt: not null } tombstonedRecord)
            {
                ownerUserId = ownership.UserId;
                recordKey = tombstonedRecord.RecordKey;
                atprotoRecordId = tombstonedRecord.Id;
            }
            else
            {
                return Skipped(request, "restore_ownership_missing");
            }
        }
        else
        {
            if (ownership?.AtprotoRecord is { TombstonedAt: null })
            {
                return Skipped(request, "remote_record_exists");
            }

            if (ownership?.AtprotoRecord is { TombstonedAt: not null } tombstonedRecord)
            {
                ownerUserId = ownership.UserId;
                recordKey = tombstonedRecord.RecordKey;
                atprotoRecordId = tombstonedRecord.Id;
            }
            else if (unsettledPredecessor is not null)
            {
                return Skipped(request, "publication_pending");
            }
        }

        AtprotoEventGovernance governance = await governanceResolver.ResolveAsync(
            request.TenantId,
            ownerUserId,
            cancellationToken);
        if (!governance.EventsEnabled)
        {
            return Skipped(request, "capability_disabled");
        }

        if (!governance.PublishMyEvents)
        {
            return Skipped(request, "consent_missing");
        }

        IReadOnlyList<Explore.Domain.UserAuthenticationToken> sessions =
            await sessionRepository.GetAtprotoSessionsForReadAsync(
                request.TenantId,
                ownerUserId,
                RepositoryBackedAtprotoSession.Provider,
                cancellationToken);
        if (sessions.Count != 1
            || string.IsNullOrWhiteSpace(sessions[0].SubjectDid)
            || !Uri.TryCreate(sessions[0].PdsHost, UriKind.Absolute, out Uri? pdsHost)
            || pdsHost.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(pdsHost.Query)
            || !string.IsNullOrEmpty(pdsHost.Fragment))
        {
            return Skipped(request, "session_missing");
        }

        if (unsettledPredecessor is not null
            && (!string.Equals(sessions[0].SubjectDid, unsettledPredecessor.Did, StringComparison.Ordinal)
                || !SamePds(pdsHost, unsettledPredecessor.PdsHost)))
        {
            return Skipped(request, "session_binding_changed");
        }

        Explore.Domain.UserExternalLogin? login = await externalLoginRepository.GetByProviderAndKey(
            RepositoryBackedAtprotoSession.Provider,
            sessions[0].SubjectDid);
        if (login is null
            || login.TenantId != request.TenantId
            || login.UserId != ownerUserId
            || !string.Equals(login.Provider, RepositoryBackedAtprotoSession.Provider, StringComparison.Ordinal)
            || !string.Equals(login.ProviderKey, sessions[0].SubjectDid, StringComparison.Ordinal))
        {
            return Skipped(request, "account_not_linked");
        }

        AtprotoPublicationPayload? payload = null;
        bool hasGroundedIdentity = ownership?.AtprotoRecord is not null || unsettledPredecessor is not null;
        if (request.Operation != PdsSyncOperation.Delete)
        {
            AtprotoEventPublicationEntityGraph? graph = await eventRepository.GetAtprotoPublicationGraphAsync(
                request.TenantId,
                request.EventId,
                cancellationToken);
            if (graph is null || graph.Event.ConcurrencyStamp != request.SourceVersion)
            {
                if (!hasGroundedIdentity || request.Operation != PdsSyncOperation.Update)
                {
                    return Skipped(request, "source_version_changed");
                }

                Explore.Domain.Event? lifecycle = await eventRepository.GetAtprotoLifecycleStateAsync(
                    request.TenantId,
                    request.EventId,
                    cancellationToken);
                if (lifecycle is null || lifecycle.ConcurrencyStamp != request.SourceVersion)
                {
                    return Skipped(request, "source_version_changed");
                }

                plannedOperation = PdsSyncOperation.Delete;
            }
            else
            {
                AtprotoPublicationPayloadBuildResult payloadResult = await payloadBuilder.BuildEventAsync(
                    graph,
                    new DateTimeOffset(request.CreatedAtUtc),
                    cancellationToken);
                if (!payloadResult.IsValid)
                {
                    if (!hasGroundedIdentity || request.Operation != PdsSyncOperation.Update)
                    {
                        return Skipped(request, payloadResult.FailureCode ?? "payload_invalid");
                    }

                    plannedOperation = PdsSyncOperation.Delete;
                }
                else
                {
                    payload = payloadResult.Payload;
                }

            }
        }

        var outbox = new PdsSyncOutbox
        {
            Id = request.OutboxId,
            TenantId = request.TenantId,
            UserId = ownerUserId,
            Did = sessions[0].SubjectDid,
            Collection = EventCollection,
            RecordKey = recordKey,
            Operation = plannedOperation,
            Payload = payload?.Json,
            PayloadHash = payload?.Sha256 ?? EmptyPayloadHash,
            IdempotencyKey = request.OutboxId.ToString("N"),
            PdsHost = pdsHost.AbsoluteUri,
            SourceEntityType = EventSourceType,
            SourceEntityId = request.EventId,
            SourceVersion = request.SourceVersion,
            AtprotoRecordId = atprotoRecordId,
            ExpectedCid = expectedCid,
            Status = PdsSyncStatus.Pending,
            CreatedAt = request.CreatedAtUtc,
            NextRetryAt = CompensationNotBefore(unsettledPredecessor, request.CreatedAtUtc),
            MaxRetries = 10
        };

        await outboxRepository.SupersedePriorAsync(
            request.TenantId,
            EventSourceType,
            request.EventId,
            request.OutboxId,
            request.CreatedAtUtc,
            cancellationToken);
        await outboxRepository.AddAsync(outbox, cancellationToken);
        return AtprotoPublicationPlanningResult.Added(outbox);
    }

    private static DateTime? CompensationNotBefore(PdsSyncOutbox? priorCreate, DateTime createdAtUtc)
    {
        if (priorCreate is null)
        {
            return null;
        }

        DateTime? notBefore = priorCreate.NextRetryAt > createdAtUtc
            ? priorCreate.NextRetryAt
            : null;
        if (priorCreate.Status != PdsSyncStatus.Processing)
        {
            return notBefore;
        }

        DateTime remoteWindowEnd = createdAtUtc.Add(MaximumRemoteCallWindow);
        DateTime leaseEnd = priorCreate.LeaseExpiresAt is { Kind: DateTimeKind.Utc } value
            ? value
            : remoteWindowEnd;
        DateTime processingSafetyEnd = (leaseEnd > remoteWindowEnd ? leaseEnd : remoteWindowEnd)
            .Add(CompensationSafetyWindow);
        return notBefore > processingSafetyEnd ? notBefore : processingSafetyEnd;
    }

    private static bool SamePds(Uri current, string expected) =>
        Uri.TryCreate(expected, UriKind.Absolute, out Uri? expectedUri)
        && string.Equals(current.Scheme, expectedUri.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(current.Host, expectedUri.Host, StringComparison.OrdinalIgnoreCase)
        && current.Port == expectedUri.Port
        && string.Equals(current.AbsolutePath.TrimEnd('/'), expectedUri.AbsolutePath.TrimEnd('/'), StringComparison.Ordinal);

    public async Task<AtprotoRsvpReconciliationResult> ReconcileMissingRsvpsAsync(
        Guid? afterIntentId,
        int batchSize,
        DateTime observedAtUtc,
        CancellationToken cancellationToken)
    {
        if (observedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Reconciliation time must be UTC.", nameof(observedAtUtc));
        }

        IReadOnlyList<Explore.Domain.EventRegistrationIntent> candidates =
            await registrationIntentRepository.GetAtprotoReconciliationCandidatesAsync(
                afterIntentId,
                batchSize,
                cancellationToken);
        var enqueued = 0;
        foreach (Explore.Domain.EventRegistrationIntent intent in candidates)
        {
            AtprotoPublicationPlanningResult result = await PlanRsvpAsync(
                new AtprotoRsvpPublicationInput(
                    intent.TenantId,
                    intent.UserId,
                    intent.EventId,
                    intent.Id,
                    intent.ConcurrencyStamp,
                    PdsSyncOperation.Create,
                    Guid.CreateVersion7(),
                    observedAtUtc),
                cancellationToken);
            if (result.Enqueued)
            {
                enqueued++;
            }
        }

        Guid? next = candidates.Count == batchSize ? candidates[^1].Id : null;
        return new(next, candidates.Count, enqueued);
    }

    public async Task<AtprotoPublicationPlanningResult> PlanRsvpAsync(
        AtprotoRsvpPublicationInput request,
        CancellationToken cancellationToken)
    {
        Validate(request);

        Explore.Domain.EventRegistrationIntent? requestedIntent =
            await registrationIntentRepository.GetAtprotoLifecycleStateAsync(
                request.TenantId,
                request.RegistrationIntentId,
                cancellationToken);
        if (requestedIntent is null
            || requestedIntent.UserId != request.UserId
            || requestedIntent.EventId != request.EventId
            || requestedIntent.ConcurrencyStamp != request.SourceVersion)
        {
            return Skipped(request, "source_version_changed");
        }

        AtprotoOutboundRecordOwnership? existingRsvp =
            await recordRepository.GetOwnedRsvpForUserEventAsync(
                request.TenantId,
                request.UserId,
                request.EventId,
                RsvpSourceType,
                RsvpCollection,
                cancellationToken);
        var activeCount = await registrationIntentRepository.CountActiveForEventUserAsync(
            request.TenantId,
            request.EventId,
            request.UserId,
            cancellationToken);

        Explore.Domain.EventRegistrationIntent sourceIntent = requestedIntent;
        string recordKey = BuildRsvpRecordKey(request.TenantId, request.UserId, request.EventId);
        Guid? atprotoRecordId = null;
        string? expectedCid = null;
        var plannedOperation = request.Operation;
        PdsSyncOutbox? unsettledPredecessor = await outboxRepository.GetLatestUnsettledRsvpMutationAsync(
            request.TenantId,
            request.UserId,
            request.EventId,
            RsvpSourceType,
            RsvpCollection,
            cancellationToken);
        if (request.Operation == PdsSyncOperation.Create)
        {
            if (requestedIntent.IsDeleted || activeCount == 0)
            {
                return Skipped(request, "registration_inactive");
            }

            if (unsettledPredecessor?.Operation is PdsSyncOperation.Create or PdsSyncOperation.Update
                || unsettledPredecessor is null
                && await outboxRepository.HasActiveRsvpPublicationAsync(
                    request.TenantId,
                    request.UserId,
                    request.EventId,
                    RsvpSourceType,
                    RsvpCollection,
                    cancellationToken))
            {
                return Skipped(request, "publication_pending");
            }

            if (unsettledPredecessor is not null)
            {
                recordKey = unsettledPredecessor.RecordKey;
                atprotoRecordId = existingRsvp?.AtprotoRecord?.Id;
                plannedOperation = PdsSyncOperation.Update;
            }
            else if (existingRsvp?.AtprotoRecord is { TombstonedAt: null })
            {
                return Skipped(request, "remote_record_exists");
            }
            else if (existingRsvp?.AtprotoRecord is { TombstonedAt: not null } tombstonedRsvp)
            {
                recordKey = tombstonedRsvp.RecordKey;
                atprotoRecordId = tombstonedRsvp.Id;
            }
        }
        else
        {
            if (request.Operation != PdsSyncOperation.Delete)
            {
                return Skipped(request, "operation_unsupported");
            }

            if (activeCount > 0)
            {
                return Skipped(request, "active_registration_remains");
            }

            if (unsettledPredecessor is not null)
            {
                recordKey = unsettledPredecessor.RecordKey;
                atprotoRecordId = existingRsvp?.AtprotoRecord?.Id;
            }
            else if (existingRsvp?.AtprotoRecord is { TombstonedAt: null } remoteRsvp)
            {
                sourceIntent = existingRsvp.SourceEntityId == requestedIntent.Id
                    ? requestedIntent
                    : await registrationIntentRepository.GetAtprotoLifecycleStateAsync(
                        request.TenantId,
                        existingRsvp.SourceEntityId,
                        cancellationToken)
                        ?? requestedIntent;
                recordKey = remoteRsvp.RecordKey;
                atprotoRecordId = remoteRsvp.Id;
                expectedCid = remoteRsvp.Cid;
            }
            else
            {
                return Skipped(request, "remote_record_missing");
            }
        }

        var owner = await ResolveRsvpOwnerAsync(
            request.TenantId,
            request.UserId,
            cancellationToken);
        if (owner.FailureCode is not null)
        {
            return Skipped(request, owner.FailureCode);
        }

        if (unsettledPredecessor is not null
            && (!string.Equals(owner.Did, unsettledPredecessor.Did, StringComparison.Ordinal)
                || owner.PdsHost is null
                || !SamePds(owner.PdsHost, unsettledPredecessor.PdsHost)))
        {
            return Skipped(request, "session_binding_changed");
        }

        Explore.Domain.AtprotoRecord? eventRecord = null;
        AtprotoPublicationPayload? payload = null;
        if (request.Operation == PdsSyncOperation.Create)
        {
            AtprotoOutboundRecordOwnership? eventOwnership =
                await recordRepository.GetOwnedRecordForSourceAsync(
                    request.TenantId,
                    EventSourceType,
                    request.EventId,
                    cancellationToken);
            if (eventOwnership?.AtprotoRecord is not
                {
                    TombstonedAt: null,
                    Uri: { Length: > 0 } eventUri,
                    Cid: { Length: > 0 } eventCid
                } settledEventRecord)
            {
                return Skipped(request, "event_record_missing");
            }

            eventRecord = settledEventRecord;
            AtprotoRsvpPublicationPlan plan = AtprotoRsvpPublicationSnapshotFactory.PlanActiveRegistration(
                requestedIntent,
                new(request.TenantId, request.UserId, request.EventId),
                owner.Did!,
                new(eventUri, eventCid));
            if (!plan.IsValid || plan.Snapshot is null)
            {
                return Skipped(request, "projection_invalid");
            }

            AtprotoPublicationPayloadBuildResult payloadResult = payloadBuilder.BuildRsvp(plan.Snapshot);
            if (!payloadResult.IsValid)
            {
                return Skipped(request, payloadResult.FailureCode ?? "payload_invalid");
            }

            payload = payloadResult.Payload;
        }

        if (payload is not null
            && await outboxRepository.HasTerminalRsvpPublicationAttemptAsync(
                request.TenantId,
                request.UserId,
                request.EventId,
                sourceIntent.ConcurrencyStamp,
                plannedOperation,
                payload.Sha256,
                RsvpSourceType,
                RsvpCollection,
                cancellationToken))
        {
            return Skipped(request, "publication_failed");
        }

        var outbox = new PdsSyncOutbox
        {
            Id = request.OutboxId,
            TenantId = request.TenantId,
            UserId = request.UserId,
            Did = owner.Did!,
            Collection = RsvpCollection,
            RecordKey = recordKey,
            Operation = plannedOperation,
            Payload = payload?.Json,
            PayloadHash = payload?.Sha256 ?? EmptyPayloadHash,
            IdempotencyKey = request.OutboxId.ToString("N"),
            PdsHost = owner.PdsHost!.AbsoluteUri,
            SourceEntityType = RsvpSourceType,
            SourceEntityId = sourceIntent.Id,
            SourceVersion = sourceIntent.ConcurrencyStamp,
            AtprotoRecordId = atprotoRecordId,
            DependsOnAtprotoRecordId = eventRecord?.Id,
            DependsOnCid = eventRecord?.Cid,
            ExpectedCid = expectedCid,
            Status = PdsSyncStatus.Pending,
            CreatedAt = request.CreatedAtUtc,
            NextRetryAt = CompensationNotBefore(unsettledPredecessor, request.CreatedAtUtc),
            MaxRetries = 10
        };
        await outboxRepository.SupersedePriorRsvpAsync(
            request.TenantId,
            request.UserId,
            request.EventId,
            RsvpCollection,
            request.OutboxId,
            request.CreatedAtUtc,
            cancellationToken);
        await outboxRepository.AddAsync(outbox, cancellationToken);
        return AtprotoPublicationPlanningResult.Added(outbox);
    }

    public async Task<AtprotoDeliveryGateResult> CheckDeliveryAsync(
        PdsSyncOutbox outbox,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        if ((outbox.SourceEntityType != EventSourceType || outbox.Collection != EventCollection)
            && (outbox.SourceEntityType != RsvpSourceType || outbox.Collection != RsvpCollection)
            || outbox.TenantId == Guid.Empty
            || outbox.UserId == Guid.Empty)
        {
            return AtprotoDeliveryGateResult.Deny("ownership_invalid");
        }

        AtprotoEventGovernance governance = await governanceResolver.ResolveAsync(
            outbox.TenantId,
            outbox.UserId,
            cancellationToken);
        if (!governance.EventsEnabled)
        {
            return AtprotoDeliveryGateResult.Deny("capability_disabled");
        }

        if (!governance.PublishMyEvents)
        {
            return AtprotoDeliveryGateResult.Deny("consent_missing");
        }

        IReadOnlyList<Explore.Domain.UserAuthenticationToken> sessions =
            await sessionRepository.GetAtprotoSessionsForReadAsync(
                outbox.TenantId,
                outbox.UserId,
                RepositoryBackedAtprotoSession.Provider,
                cancellationToken);
        if (sessions.Count != 1
            || !string.Equals(sessions[0].SubjectDid, outbox.Did, StringComparison.Ordinal)
            || !string.Equals(sessions[0].PdsHost, outbox.PdsHost, StringComparison.Ordinal))
        {
            return AtprotoDeliveryGateResult.Deny("session_missing");
        }

        Explore.Domain.UserExternalLogin? login = await externalLoginRepository.GetByProviderAndKey(
            RepositoryBackedAtprotoSession.Provider,
            outbox.Did);
        if (login is null
            || login.TenantId != outbox.TenantId
            || login.UserId != outbox.UserId
            || !string.Equals(login.Provider, RepositoryBackedAtprotoSession.Provider, StringComparison.Ordinal)
            || !string.Equals(login.ProviderKey, outbox.Did, StringComparison.Ordinal))
        {
            return AtprotoDeliveryGateResult.Deny("account_not_linked");
        }

        if (outbox.SourceEntityType == RsvpSourceType)
        {
            return await CheckRsvpDeliveryAsync(outbox, observedAt, cancellationToken);
        }

        Explore.Domain.Event? eventEntity = await eventRepository.GetAtprotoLifecycleStateAsync(
            outbox.TenantId,
            outbox.SourceEntityId,
            cancellationToken);
        if (eventEntity is null || eventEntity.ConcurrencyStamp != outbox.SourceVersion)
        {
            return AtprotoDeliveryGateResult.Deny("source_version_changed");
        }

        if (outbox.Operation == PdsSyncOperation.Delete)
        {
            if (eventEntity.IsDeleted
                || eventEntity.EventStatusId is (int)EventStatusEnum.Moderated
                    or (int)EventStatusEnum.Archived
                    or (int)EventStatusEnum.Cancelled)
            {
                return AtprotoDeliveryGateResult.Permit();
            }

            AtprotoEventPublicationEntityGraph? removalGraph = await eventRepository.GetAtprotoPublicationGraphAsync(
                outbox.TenantId,
                outbox.SourceEntityId,
                cancellationToken);
            if (removalGraph is null)
            {
                return AtprotoDeliveryGateResult.Permit();
            }

            AtprotoPublicationPayloadBuildResult removalProjection = await payloadBuilder.BuildEventAsync(
                removalGraph,
                observedAt,
                cancellationToken);
            return removalProjection.IsValid
                ? AtprotoDeliveryGateResult.Deny("delete_state_invalid")
                : AtprotoDeliveryGateResult.Permit();
        }

        AtprotoEventPublicationEntityGraph? graph = await eventRepository.GetAtprotoPublicationGraphAsync(
            outbox.TenantId,
            outbox.SourceEntityId,
            cancellationToken);
        if (graph is null)
        {
            return AtprotoDeliveryGateResult.Deny("privacy_ineligible");
        }

        AtprotoPublicationPayloadBuildResult currentPayload = await payloadBuilder.BuildEventAsync(
            graph,
            observedAt,
            cancellationToken);
        return currentPayload.IsValid
               && string.Equals(currentPayload.Payload!.Sha256, outbox.PayloadHash, StringComparison.Ordinal)
               && string.Equals(currentPayload.Payload.Json, outbox.Payload, StringComparison.Ordinal)
            ? AtprotoDeliveryGateResult.Permit()
            : AtprotoDeliveryGateResult.Deny("privacy_or_payload_changed");
    }

    private async Task<AtprotoDeliveryGateResult> CheckRsvpDeliveryAsync(
        PdsSyncOutbox outbox,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken)
    {
        Explore.Domain.EventRegistrationIntent? intent =
            await registrationIntentRepository.GetAtprotoLifecycleStateAsync(
                outbox.TenantId,
                outbox.SourceEntityId,
                cancellationToken);
        if (intent is null
            || intent.UserId != outbox.UserId
            || intent.ConcurrencyStamp != outbox.SourceVersion)
        {
            return AtprotoDeliveryGateResult.Deny("source_version_changed");
        }

        var activeCount = await registrationIntentRepository.CountActiveForEventUserAsync(
            outbox.TenantId,
            intent.EventId,
            outbox.UserId,
            cancellationToken);
        if (outbox.Operation == PdsSyncOperation.Delete)
        {
            return activeCount == 0
                ? AtprotoDeliveryGateResult.Permit()
                : AtprotoDeliveryGateResult.Deny("active_registration_remains");
        }

        if (outbox.Operation is not (PdsSyncOperation.Create or PdsSyncOperation.Update)
            || intent.IsDeleted
            || activeCount == 0)
        {
            return AtprotoDeliveryGateResult.Deny("registration_inactive");
        }

        AtprotoOutboundRecordOwnership? eventOwnership =
            await recordRepository.GetOwnedRecordForSourceAsync(
                outbox.TenantId,
                EventSourceType,
                intent.EventId,
                cancellationToken);
        if (eventOwnership?.AtprotoRecord is not
            {
                TombstonedAt: null,
                Uri: { Length: > 0 } eventUri,
                Cid: { Length: > 0 } eventCid
            } eventRecord
            || eventRecord.Id != outbox.DependsOnAtprotoRecordId)
        {
            return AtprotoDeliveryGateResult.Deny("event_record_missing");
        }

        AtprotoEventPublicationEntityGraph? graph = await eventRepository.GetAtprotoPublicationGraphAsync(
            outbox.TenantId,
            intent.EventId,
            cancellationToken);
        if (graph is null
            || !(await payloadBuilder.BuildEventAsync(graph, observedAt, cancellationToken)).IsValid)
        {
            return AtprotoDeliveryGateResult.Deny("privacy_ineligible");
        }

        AtprotoRsvpPublicationPlan plan = AtprotoRsvpPublicationSnapshotFactory.PlanActiveRegistration(
            intent,
            new(outbox.TenantId, outbox.UserId, intent.EventId),
            outbox.Did,
            new(eventUri, eventCid));
        if (!plan.IsValid || plan.Snapshot is null)
        {
            return AtprotoDeliveryGateResult.Deny("projection_invalid");
        }

        AtprotoPublicationPayloadBuildResult currentPayload = payloadBuilder.BuildRsvp(plan.Snapshot);
        return currentPayload.IsValid
               && string.Equals(currentPayload.Payload!.Sha256, outbox.PayloadHash, StringComparison.Ordinal)
               && string.Equals(currentPayload.Payload.Json, outbox.Payload, StringComparison.Ordinal)
            ? AtprotoDeliveryGateResult.Permit()
            : AtprotoDeliveryGateResult.Deny("payload_changed");
    }

    private async Task<(string? Did, Uri? PdsHost, string? FailureCode)> ResolveRsvpOwnerAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        AtprotoEventGovernance governance = await governanceResolver.ResolveAsync(
            tenantId,
            userId,
            cancellationToken);
        if (!governance.EventsEnabled)
        {
            return (null, null, "capability_disabled");
        }

        if (!governance.PublishMyEvents)
        {
            return (null, null, "consent_missing");
        }

        IReadOnlyList<Explore.Domain.UserAuthenticationToken> sessions =
            await sessionRepository.GetAtprotoSessionsForReadAsync(
                tenantId,
                userId,
                RepositoryBackedAtprotoSession.Provider,
                cancellationToken);
        if (sessions.Count != 1
            || string.IsNullOrWhiteSpace(sessions[0].SubjectDid)
            || !Uri.TryCreate(sessions[0].PdsHost, UriKind.Absolute, out Uri? pdsHost)
            || pdsHost.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(pdsHost.Query)
            || !string.IsNullOrEmpty(pdsHost.Fragment))
        {
            return (null, null, "session_missing");
        }

        Explore.Domain.UserExternalLogin? login = await externalLoginRepository.GetByProviderAndKey(
            RepositoryBackedAtprotoSession.Provider,
            sessions[0].SubjectDid);
        if (login is null
            || login.TenantId != tenantId
            || login.UserId != userId
            || !string.Equals(login.Provider, RepositoryBackedAtprotoSession.Provider, StringComparison.Ordinal)
            || !string.Equals(login.ProviderKey, sessions[0].SubjectDid, StringComparison.Ordinal))
        {
            return (null, null, "account_not_linked");
        }

        return (sessions[0].SubjectDid, pdsHost, null);
    }

    private static void Validate(AtprotoEventPublicationInput request)
    {
        if (request.TenantId == Guid.Empty
            || request.EventId == Guid.Empty
            || request.SourceVersion == Guid.Empty
            || request.OutboxId == Guid.Empty
            || request.CreatedAtUtc.Kind != DateTimeKind.Utc
            || request.Operation is < PdsSyncOperation.Create or > PdsSyncOperation.Delete
            || request.Operation == PdsSyncOperation.Create && request.UserId == Guid.Empty
            || request.RestoreOnly && request.Operation != PdsSyncOperation.Create)
        {
            throw new ArgumentException("ATProto event publication request is invalid.", nameof(request));
        }
    }

    private static void Validate(AtprotoRsvpPublicationInput request)
    {
        if (request.TenantId == Guid.Empty
            || request.UserId == Guid.Empty
            || request.EventId == Guid.Empty
            || request.RegistrationIntentId == Guid.Empty
            || request.SourceVersion == Guid.Empty
            || request.OutboxId == Guid.Empty
            || request.CreatedAtUtc.Kind != DateTimeKind.Utc
            || request.Operation is not (PdsSyncOperation.Create or PdsSyncOperation.Delete))
        {
            throw new ArgumentException("ATProto RSVP publication request is invalid.", nameof(request));
        }
    }

    private static string BuildRsvpRecordKey(Guid tenantId, Guid userId, Guid eventId)
    {
        Span<byte> identity = stackalloc byte[48];
        tenantId.TryWriteBytes(identity[..16]);
        userId.TryWriteBytes(identity[16..32]);
        eventId.TryWriteBytes(identity[32..]);
        return Convert.ToHexString(SHA256.HashData(identity)).ToLowerInvariant();
    }
}
