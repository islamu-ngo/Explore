// ABOUTME: Handler implementations for provider-neutral registration reconciliation health and queue operations.
// ABOUTME: Reuses existing provider bindings, incoming effect outbox, and submission issues instead of new tables.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services.Registration;
using Explore.Application.DTOs.RegistrationProviders;
using Explore.Application.Features.RegistrationSubmissions.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using FluentValidation;
using MediatR;
using static Explore.Application.Features.RegistrationProviders.Commands.RegistrationProviderManagementHandlerHelpers;

namespace Explore.Application.Features.RegistrationProviders.Commands;

public sealed class GetRegistrationProviderHealthQueryHandler(
    IRegistrationProviderRepository providerRepository,
    IRegistrationProviderRegistry providerRegistry,
    TimeProvider timeProvider)
    : IRequestHandler<GetRegistrationProviderHealthQuery, IReadOnlyList<RegistrationProviderBindingHealthDto>>
{
    public async Task<IReadOnlyList<RegistrationProviderBindingHealthDto>> Handle(GetRegistrationProviderHealthQuery request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyList<RegistrationProviderBinding> bindings = await providerRepository.GetBindingsForEventAsync(request.TenantId, request.EventId, cancellationToken);
        var result = new List<RegistrationProviderBindingHealthDto>(bindings.Count);
        foreach (RegistrationProviderBinding binding in bindings)
        {
            DateTime? lastCallbackAt = await providerRepository.GetLastCallbackAtAsync(request.TenantId, binding.Id, cancellationToken);
            int queueDepth = await providerRepository.CountParkedItemsAsync(request.TenantId, binding.Id, cancellationToken);
            DateTime? oldestPendingAt = await providerRepository.GetOldestPendingItemAtAsync(request.TenantId, binding.Id, cancellationToken);
            int? callbackAge = lastCallbackAt.HasValue ? Math.Max(0, (int)(now - lastCallbackAt.Value).TotalSeconds) : null;
            int lag = oldestPendingAt.HasValue ? Math.Max(0, (int)(now - oldestPendingAt.Value).TotalSeconds) : 0;
            result.Add(new RegistrationProviderBindingHealthDto
            {
                TenantId = request.TenantId,
                EventId = request.EventId,
                BindingId = binding.Id,
                ConnectionId = binding.RegistrationProviderConnectionId,
                ProviderKind = binding.Connection?.ProviderKindId.ToString() ?? "unknown",
                BindingStatus = ((RegistrationProviderBindingStateEnum)binding.StateId).ToString(),
                ConnectionValidity = binding.Connection is null || binding.Connection.IsDeleted ? "invalid" : "valid",
                LastCallbackAt = lastCallbackAt,
                LastCallbackAgeSeconds = callbackAge,
                CallbackAgeClass = callbackAge is null ? "none" : callbackAge <= 3600 ? "fresh" : callbackAge <= 86400 ? "aging" : "stale",
                DriftClass = ((RegistrationProviderDriftClassEnum)binding.DriftClassId).ToString(),
                ReconciliationLagSeconds = lag,
                ReconciliationLagClass = queueDepth > 0 ? "parked" : "current",
                ParkedQueueDepth = queueDepth,
                CapabilityCodes = binding.Capabilities.Where(capability => !capability.IsDeleted).Select(capability => capability.CapabilityCode).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray()
            });
        }

        return result;
    }
}

public sealed class GetRegistrationProviderQueueQueryHandler(IRegistrationProviderRepository providerRepository)
    : IRequestHandler<GetRegistrationProviderQueueQuery, IReadOnlyList<RegistrationProviderParkedQueueItemDto>>
{
    public async Task<IReadOnlyList<RegistrationProviderParkedQueueItemDto>> Handle(GetRegistrationProviderQueueQuery request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        int limit = Math.Clamp(request.Limit, 1, 100);
        IReadOnlyList<RegistrationProviderParkedItem> rows = await providerRepository.GetParkedItemsForEventAsync(request.TenantId, request.EventId, limit, cancellationToken);
        return rows.Select(row => row.Submission is { } submission ? new RegistrationProviderParkedQueueItemDto
        {
            TenantId = submission.Submission.TenantId,
            EventId = submission.Submission.EventId,
            BindingId = submission.Submission.RegistrationProviderBindingId ?? Guid.Empty,
            SubmissionId = submission.Submission.Id,
            Status = ((RegistrationSubmissionStatusEnum)submission.Submission.StatusId).ToString(),
            FailureCategory = "submission_issue",
            IssueCodes = submission.Issues.Select(issue => issue.Code).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            CreatedAt = submission.Submission.CreatedAt,
            ProcessingGeneration = 1
        } : new RegistrationProviderParkedQueueItemDto
        {
            TenantId = row.Effect!.Effect.TenantId,
            EventId = row.Effect.EventId,
            BindingId = row.Effect.BindingId,
            EffectOutboxId = row.Effect.Effect.Id,
            Status = row.Effect.Effect.Status.ToString(),
            FailureCategory = SafeCategory(row.Effect.Effect.FailureCategory),
            IssueCodes = string.IsNullOrWhiteSpace(row.Effect.Effect.FailureCategory) ? [] : [SafeCategory(row.Effect.Effect.FailureCategory)],
            CreatedAt = row.Effect.Effect.CreatedAt,
            NextAttemptAt = row.Effect.Effect.NextAttemptAt,
            ProcessingGeneration = row.Effect.Effect.ProcessingGeneration
        }).ToArray();
    }
}

public sealed class PollRegistrationProviderReconciliationCommandHandler(
    IRegistrationProviderRepository providerRepository,
    IRegistrationProviderRegistry providerRegistry,
    BusinessMetrics? metrics = null)
    : IRequestHandler<PollRegistrationProviderReconciliationCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(PollRegistrationProviderReconciliationCommand request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        if (request.BindingId == Guid.Empty || request.SinceUtc == default || request.SinceUtc.Kind != DateTimeKind.Utc)
        {
            metrics?.RecordRegistrationProviderManagementAction("poll_reconciliation", "validation_failed");
            return Failure(request.BindingId, "registration_provider_reconciliation_validation_failed", "Reconciliation checkpoint is invalid.");
        }

        RegistrationProviderBinding? binding = await providerRepository.GetBindingAsync(request.TenantId, request.BindingId, cancellationToken);
        if (binding is null || !await BindingBelongsToEventAsync(providerRepository, request, cancellationToken))
        {
            metrics?.RecordRegistrationProviderManagementAction("poll_reconciliation", "not_found");
            return Failure(request.BindingId, "registration_provider_binding_not_found", "Registration provider binding was not found.");
        }

        RegistrationProviderCapability? capability = binding.Capabilities.FirstOrDefault(capability => !capability.IsDeleted && string.Equals(capability.CapabilityCode, RegistrationProviderCapabilityCodes.Reconciliation, StringComparison.OrdinalIgnoreCase));
        if (capability is null)
        {
            metrics?.RecordRegistrationProviderManagementAction("poll_reconciliation", "unsupported");
            return Failure(request.BindingId, "registration_provider_reconciliation_unsupported", "Registration provider reconciliation is not configured for this binding.");
        }

        var tuple = new RegistrationProviderTuple(capability.ProviderCode, capability.DeploymentKind, capability.ApiVersion, capability.AdapterPolicyVersion, capability.ConformanceEvidenceRevision);
        IRegistrationProviderDescriptor? descriptor = providerRegistry.TryResolve(tuple);
        if (descriptor is not IRegistrationProviderReconciliationProvider reconciler || !descriptor.ProvenCapabilities.Reconciliation)
        {
            metrics?.RecordRegistrationProviderManagementAction("poll_reconciliation", "unknown_capability");
            return Failure(request.BindingId, "registration_provider_reconciliation_unknown", "Registration provider reconciliation capability is not available.");
        }

        RegistrationProviderReconciliationResult result = await reconciler.ReconcileAsync(new RegistrationProviderReconciliationRequest(request.TenantId, request.BindingId, request.SinceUtc), cancellationToken);
        metrics?.RecordRegistrationProviderManagementAction("poll_reconciliation", "accepted");
        return Success(request.BindingId, $"Reconciliation observed {result.ObservedSubmissionCount} bounded provider submissions.");
    }
}

public sealed class QueueManualRegistrationProviderImportCommandHandler(
    IRegistrationProviderRepository providerRepository,
    IIncomingWebhookMessageRepository messageRepository,
    IIncomingWebhookEffectOutboxRepository effectRepository,
    TimeProvider timeProvider,
    BusinessMetrics? metrics = null)
    : IRequestHandler<QueueManualRegistrationProviderImportCommand, BaseCommandResponse<Guid>>
{
    public const string ManualImportEffectKind = "registration.provider_manual_import";

    public async Task<BaseCommandResponse<Guid>> Handle(QueueManualRegistrationProviderImportCommand request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        if (request.BindingId == Guid.Empty || !BoundedReference(request.StorageReference, 300) || !BoundedReference(request.SourceReference, 200))
        {
            metrics?.RecordRegistrationProviderManagementAction("manual_import", "validation_failed");
            return Failure(request.BindingId, "registration_provider_manual_import_validation_failed", "Manual import requires bounded storage/reference metadata.");
        }

        RegistrationProviderBinding? binding = await providerRepository.GetBindingAsync(request.TenantId, request.BindingId, cancellationToken);
        if (binding is null || !await BindingBelongsToEventAsync(providerRepository, request.TenantId, request.EventId, request.BindingId, cancellationToken))
        {
            metrics?.RecordRegistrationProviderManagementAction("manual_import", "not_found");
            return Failure(request.BindingId, "registration_provider_binding_not_found", "Registration provider binding was not found for this event.");
        }

        bool manualSupported = binding.Capabilities.Any(capability => !capability.IsDeleted &&
            string.Equals(capability.CapabilityCode, RegistrationProviderCapabilityCodes.Manual, StringComparison.OrdinalIgnoreCase));
        string payload = JsonSerializer.Serialize(new ManualImportEnvelope(request.BindingId, request.StorageReference.Trim(), request.SourceReference.Trim()));
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        string hash = Sha256Identifier(payloadBytes);
        string providerDecisionId = $"{request.BindingId:N}:manual:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(request.StorageReference.Trim() + "\n" + request.SourceReference.Trim())))[..32]}";
        IncomingWebhookMessage? existing = await messageRepository.GetByProviderMessageIdForUpdateAsync(request.TenantId, "registration-provider", providerDecisionId, cancellationToken);
        if (existing is not null && !string.Equals(existing.PayloadHash, hash, StringComparison.Ordinal))
        {
            metrics?.RecordRegistrationProviderManagementAction("manual_import", "payload_conflict");
            return Failure(request.BindingId, "registration_provider_manual_import_conflict", "Manual import identity already exists with different metadata.");
        }

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        IncomingWebhookMessage message = existing ?? IncomingWebhookMessage.CreateVerified(
            request.TenantId,
            "registration-provider",
            providerDecisionId,
            providerDecisionId,
            ManualImportEffectKind,
            payloadBytes,
            hash,
            "application/json",
            "utf-8",
            "{}",
            now,
            now,
            now.AddDays(7),
            "registration-provider-manual-import-v1",
            now.AddDays(30),
            now.AddDays(30),
            now.AddDays(7),
            now.AddDays(30));
        if (existing is null)
        {
            if (!await messageRepository.TryCreateAsync(message, cancellationToken))
            {
                message = await messageRepository.GetByProviderMessageIdForUpdateAsync(request.TenantId, "registration-provider", providerDecisionId, cancellationToken)
                    ?? message;
            }
        }

        IncomingWebhookEffectOutbox? existingEffect = await effectRepository.GetByProviderIdentityAsync(request.TenantId, "registration-provider", providerDecisionId, ManualImportEffectKind, cancellationToken);
        if (existingEffect is null)
        {
            IncomingWebhookEffectOutbox effect = IncomingWebhookEffectOutbox.CreatePending(request.TenantId, message.Id, "registration-provider", providerDecisionId, ManualImportEffectKind, hash, now);
            if (!manualSupported)
            {
                Guid leaseToken = Guid.CreateVersion7();
                effect.Claim("manual-import", leaseToken, now.AddMinutes(5), now);
                effect.DeadLetter(leaseToken, effect.ProcessingFence, effect.ProcessingGeneration, "MANUAL_IMPORT_UNSUPPORTED", "Manual import is not supported by this provider binding.", now);
            }
            else
            {
                effect.AcknowledgeResolution("manual_import_acknowledged", now);
            }

            await effectRepository.AddAsync(effect, cancellationToken);
            await effectRepository.SaveChangesAsync(cancellationToken);
        }

        metrics?.RecordRegistrationProviderManagementAction("manual_import", manualSupported ? "accepted" : "parked_unsupported");
        return Success(request.BindingId, manualSupported
            ? "Manual import metadata accepted for organizer reconciliation."
            : "Manual import metadata parked because this provider binding does not support manual import.");
    }

    private sealed record ManualImportEnvelope(Guid BindingId, string StorageReference, string SourceReference);
}

public sealed class RetryRegistrationProviderParkedItemCommandHandler(
    IRegistrationProviderRepository providerRepository,
    IIncomingWebhookEffectOutboxRepository effectRepository,
    IIncomingWebhookEffectReceiptRepository receiptRepository,
    TimeProvider timeProvider,
    BusinessMetrics? metrics = null)
    : IRequestHandler<RetryRegistrationProviderParkedItemCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(RetryRegistrationProviderParkedItemCommand request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        if (request.EffectOutboxId is not { } effectId || request.ExpectedProcessingGeneration is not { } generation)
        {
            metrics?.RecordRegistrationProviderManagementAction("retry_parked_item", "no_effect");
            return Failure(request.SubmissionId ?? Guid.Empty, "registration_provider_retry_no_effect", "Only retained effect retries are currently replayable.");
        }

        IncomingWebhookEffectOutbox? pointer = await effectRepository.GetByTenantAndIdForUpdateAsync(request.TenantId, effectId, cancellationToken);
        if (pointer is null || !await ValidateRetainedEffectAsync(providerRepository, receiptRepository, pointer, request.TenantId, request.EventId, cancellationToken))
        {
            metrics?.RecordRegistrationProviderManagementAction("retry_parked_item", "not_found");
            return Failure(effectId, "registration_provider_effect_not_found", "Registration provider effect was not found.");
        }

        if (pointer.Status == OutboxMessageStatus.Pending)
        {
            metrics?.RecordRegistrationProviderManagementAction("retry_parked_item", "already_pending");
            return Success(pointer.Id, "Registration provider effect retry was already pending.");
        }

        if (pointer.Status != OutboxMessageStatus.DeadLettered || pointer.ProcessingGeneration != generation)
        {
            metrics?.RecordRegistrationProviderManagementAction("retry_parked_item", "not_eligible");
            return Failure(pointer.Id, "registration_provider_effect_retry_not_eligible", "Registration provider effect retry is not eligible.");
        }

        pointer.Redrive(generation, timeProvider.GetUtcNow().UtcDateTime);
        await effectRepository.SaveChangesAsync(cancellationToken);
        metrics?.RecordRegistrationProviderManagementAction("retry_parked_item", "scheduled");
        return Success(pointer.Id, "Registration provider effect retry scheduled.");
    }
}

public sealed class ResolveRegistrationProviderQueueItemCommandHandler(
    IRegistrationProviderRepository providerRepository,
    IIncomingWebhookEffectOutboxRepository effectRepository,
    IIncomingWebhookEffectReceiptRepository receiptRepository,
    TimeProvider timeProvider,
    BusinessMetrics? metrics = null)
    : IRequestHandler<ResolveRegistrationProviderQueueItemCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(ResolveRegistrationProviderQueueItemCommand request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        bool hasSubmission = request.SubmissionId is { } submissionId && submissionId != Guid.Empty;
        bool hasEffect = request.EffectOutboxId is { } requestedEffectId && requestedEffectId != Guid.Empty;
        bool valid = hasSubmission != hasEffect && BoundedReference(request.DecisionCode, 100) &&
            (string.IsNullOrWhiteSpace(request.NoteReference) || BoundedReference(request.NoteReference, 300));
        if (!valid)
        {
            metrics?.RecordRegistrationProviderManagementAction("resolve_queue_item", "validation_failed");
            return Failure(request.SubmissionId ?? request.EffectOutboxId ?? Guid.Empty, "registration_provider_resolution_validation_failed", "Resolution decision is invalid.");
        }

        if (hasEffect)
        {
            Guid effectId = request.EffectOutboxId!.Value;
            IncomingWebhookEffectOutbox? effect = await effectRepository.GetByTenantAndIdForUpdateAsync(request.TenantId, effectId, cancellationToken);
            if (effect is null || !await ValidateRetainedEffectAsync(providerRepository, receiptRepository, effect, request.TenantId, request.EventId, cancellationToken))
            {
                metrics?.RecordRegistrationProviderManagementAction("resolve_queue_item", "not_found");
                return Failure(effectId, "registration_provider_queue_item_not_found", "Registration provider queue item was not found.");
            }

            effect.AcknowledgeResolution("organizer_" + SafeCategory(request.DecisionCode), timeProvider.GetUtcNow().UtcDateTime);
            await effectRepository.SaveChangesAsync(cancellationToken);
            metrics?.RecordRegistrationProviderManagementAction("resolve_queue_item", "acknowledged");
            return Success(effect.Id, "Registration provider reconciliation decision acknowledged.");
        }

        RegistrationSubmission? submission = await providerRepository.GetParkedSubmissionAsync(request.TenantId, request.EventId, request.SubmissionId!.Value, cancellationToken);
        if (submission is null)
        {
            metrics?.RecordRegistrationProviderManagementAction("resolve_queue_item", "not_found");
            return Failure(request.SubmissionId.Value, "registration_provider_queue_item_not_found", "Registration provider queue item was not found.");
        }

        await providerRepository.AddSubmissionIssueAsync(RegistrationSubmissionIssue.Create(submission, "RESOLVED_" + SafeCategory(request.DecisionCode), timeProvider.GetUtcNow().UtcDateTime), cancellationToken);
        await providerRepository.SaveChangesAsync(cancellationToken);
        metrics?.RecordRegistrationProviderManagementAction("resolve_queue_item", "acknowledged");
        return Success(submission.Id, "Registration provider reconciliation decision acknowledged.");
    }
}

public sealed class GetRegistrationProviderConnectionsQueryHandler(IRegistrationProviderRepository providerRepository)
    : IRequestHandler<GetRegistrationProviderConnectionsQuery, IReadOnlyList<RegistrationProviderConnectionDto>>
{
    public async Task<IReadOnlyList<RegistrationProviderConnectionDto>> Handle(GetRegistrationProviderConnectionsQuery request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        return [.. (await providerRepository.GetConnectionsAsync(request.TenantId, cancellationToken)).Select(connection => ToConnectionDto(connection, request.EventId))];
    }
}

public sealed class GetRegistrationProviderConnectionQueryHandler(IRegistrationProviderRepository providerRepository)
    : IRequestHandler<GetRegistrationProviderConnectionQuery, RegistrationProviderConnectionDto?>
{
    public async Task<RegistrationProviderConnectionDto?> Handle(GetRegistrationProviderConnectionQuery request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        RegistrationProviderConnection? connection = await providerRepository.GetConnectionAsync(request.TenantId, request.ConnectionId, cancellationToken);
        return connection is null ? null : ToConnectionDto(connection, request.EventId);
    }
}

public sealed class UpsertRegistrationProviderConnectionCommandHandler(IRegistrationProviderRepository providerRepository, TimeProvider timeProvider)
    : IRequestHandler<UpsertRegistrationProviderConnectionCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(UpsertRegistrationProviderConnectionCommand request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        if (!TryConnectionRequest(request.Request, out RegistrationProviderKindEnum kind, out RegistrationProviderDeploymentKindEnum deployment))
            return Failure(request.ConnectionId ?? Guid.Empty, "registration_provider_connection_validation_failed", "Registration provider connection is invalid.");

        if (request.ConnectionId is { } id)
        {
            RegistrationProviderConnection? connection = await providerRepository.GetConnectionAsync(request.TenantId, id, cancellationToken);
            if (connection is null) return Failure(id, "registration_provider_connection_not_found", "Registration provider connection was not found.");
            connection.Update(request.Request.Name, kind, deployment, request.Request.ApiTokenSecretBindingId, request.Request.WebhookSecretBindingId);
            await providerRepository.SaveChangesAsync(cancellationToken);
            return Success(connection.Id, "Registration provider connection updated.");
        }

        RegistrationProviderConnection created = RegistrationProviderConnection.Create(request.TenantId, request.Request.Name, kind, deployment,
            request.Request.ApiTokenSecretBindingId, request.Request.WebhookSecretBindingId, timeProvider.GetUtcNow().UtcDateTime);
        await providerRepository.AddConnectionAsync(created, cancellationToken);
        await providerRepository.SaveChangesAsync(cancellationToken);
        return Success(created.Id, "Registration provider connection created.");
    }
}

public sealed class ReplaceRegistrationProviderApprovedOriginsCommandHandler(IRegistrationProviderRepository providerRepository, TimeProvider timeProvider)
    : IRequestHandler<ReplaceRegistrationProviderApprovedOriginsCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(ReplaceRegistrationProviderApprovedOriginsCommand request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        RegistrationProviderConnection? connection = await providerRepository.GetConnectionAsync(request.TenantId, request.ConnectionId, cancellationToken);
        if (connection is null) return Failure(request.ConnectionId, "registration_provider_connection_not_found", "Registration provider connection was not found.");
        try
        {
            connection.ReplaceApprovedOrigins(request.Origins, timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (ArgumentException ex)
        {
            return Failure(request.ConnectionId, "registration_provider_origin_validation_failed", ex.Message);
        }

        await providerRepository.SaveChangesAsync(cancellationToken);
        return Success(connection.Id, "Registration provider approved origins replaced.");
    }
}

public sealed class DeleteRegistrationProviderConnectionCommandHandler(IRegistrationProviderRepository providerRepository, TimeProvider timeProvider)
    : IRequestHandler<DeleteRegistrationProviderConnectionCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(DeleteRegistrationProviderConnectionCommand request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        RegistrationProviderConnection? connection = await providerRepository.GetConnectionAsync(request.TenantId, request.ConnectionId, cancellationToken);
        if (connection is null) return Failure(request.ConnectionId, "registration_provider_connection_not_found", "Registration provider connection was not found.");
        if ((await providerRepository.GetBindingsAsync(request.TenantId, cancellationToken)).Any(binding => binding.RegistrationProviderConnectionId == request.ConnectionId))
            return Failure(request.ConnectionId, "registration_provider_connection_in_use", "Registration provider connection still has bindings.");
        connection.Remove(timeProvider.GetUtcNow().UtcDateTime);
        await providerRepository.SaveChangesAsync(cancellationToken);
        return Success(connection.Id, "Registration provider connection deleted.");
    }
}

public sealed class GetRegistrationProviderBindingsQueryHandler(IRegistrationProviderRepository providerRepository)
    : IRequestHandler<GetRegistrationProviderBindingsQuery, IReadOnlyList<RegistrationProviderBindingDto>>
{
    public async Task<IReadOnlyList<RegistrationProviderBindingDto>> Handle(GetRegistrationProviderBindingsQuery request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        return [.. (await providerRepository.GetBindingsForEventAsync(request.TenantId, request.EventId, cancellationToken)).Select(binding => ToBindingDto(binding, request.EventId))];
    }
}

public sealed class GetRegistrationProviderBindingQueryHandler(IRegistrationProviderRepository providerRepository)
    : IRequestHandler<GetRegistrationProviderBindingQuery, RegistrationProviderBindingDto?>
{
    public async Task<RegistrationProviderBindingDto?> Handle(GetRegistrationProviderBindingQuery request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        RegistrationProviderBinding? binding = await providerRepository.GetBindingAsync(request.TenantId, request.BindingId, cancellationToken);
        return binding is null || !await BindingBelongsToEventAsync(providerRepository, request.TenantId, request.EventId, request.BindingId, cancellationToken)
            ? null
            : ToBindingDto(binding, request.EventId);
    }
}

public sealed class CreateRegistrationProviderBindingCommandHandler(IRegistrationProviderRepository providerRepository, TimeProvider timeProvider)
    : IRequestHandler<CreateRegistrationProviderBindingCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(CreateRegistrationProviderBindingCommand request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        if (await providerRepository.GetConnectionAsync(request.TenantId, request.Request.ConnectionId, cancellationToken) is null ||
            !await providerRepository.FormVersionBelongsToEventAsync(request.TenantId, request.EventId, request.Request.FormId, request.Request.FormVersionId, cancellationToken) ||
            !TryBindingRequest(request.Request, out RegistrationProviderPresentationModeEnum presentation, out RegistrationProviderCollectionModeEnum collection, out RegistrationProviderCompletionModeEnum completion, out RegistrationProviderTrustLevelEnum trust))
        {
            return Failure(Guid.Empty, "registration_provider_binding_validation_failed", "Registration provider binding is invalid.");
        }

        RegistrationProviderBinding binding = RegistrationProviderBinding.Create(request.TenantId, request.Request.ConnectionId, request.Request.FormId,
            request.Request.FormVersionId, presentation, collection, completion, trust, timeProvider.GetUtcNow().UtcDateTime);
        await providerRepository.AddBindingAsync(binding, cancellationToken);
        await providerRepository.SaveChangesAsync(cancellationToken);
        return Success(binding.Id, "Registration provider binding created.");
    }
}

public sealed class UpdateRegistrationProviderBindingCommandHandler(IRegistrationProviderRepository providerRepository)
    : IRequestHandler<UpdateRegistrationProviderBindingCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(UpdateRegistrationProviderBindingCommand request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        RegistrationProviderBinding? binding = await providerRepository.GetBindingAsync(request.TenantId, request.BindingId, cancellationToken);
        if (binding is null || !await BindingBelongsToEventAsync(providerRepository, request.TenantId, request.EventId, request.BindingId, cancellationToken))
            return Failure(request.BindingId, "registration_provider_binding_not_found", "Registration provider binding was not found.");
        if (await providerRepository.GetConnectionAsync(request.TenantId, request.Request.ConnectionId, cancellationToken) is null ||
            !await providerRepository.FormVersionBelongsToEventAsync(request.TenantId, request.EventId, request.Request.FormId, request.Request.FormVersionId, cancellationToken) ||
            !TryBindingRequest(request.Request, out RegistrationProviderPresentationModeEnum presentation, out RegistrationProviderCollectionModeEnum collection, out RegistrationProviderCompletionModeEnum completion, out RegistrationProviderTrustLevelEnum trust))
            return Failure(request.BindingId, "registration_provider_binding_validation_failed", "Registration provider binding is invalid.");
        try
        {
            binding.UpdateDraft(request.Request.ConnectionId, request.Request.FormId, request.Request.FormVersionId, presentation, collection, completion, trust);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Failure(request.BindingId, "registration_provider_binding_update_failed", ex.Message);
        }

        await providerRepository.SaveChangesAsync(cancellationToken);
        return Success(binding.Id, "Registration provider binding updated.");
    }
}

public sealed class DeleteRegistrationProviderBindingCommandHandler(IRegistrationProviderRepository providerRepository, TimeProvider timeProvider)
    : IRequestHandler<DeleteRegistrationProviderBindingCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(DeleteRegistrationProviderBindingCommand request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        RegistrationProviderBinding? binding = await providerRepository.GetBindingAsync(request.TenantId, request.BindingId, cancellationToken);
        if (binding is null || !await BindingBelongsToEventAsync(providerRepository, request.TenantId, request.EventId, request.BindingId, cancellationToken))
            return Failure(request.BindingId, "registration_provider_binding_not_found", "Registration provider binding was not found.");
        try
        {
            binding.Remove(timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(request.BindingId, "registration_provider_binding_delete_failed", ex.Message);
        }

        await providerRepository.SaveChangesAsync(cancellationToken);
        return Success(binding.Id, "Registration provider binding deleted.");
    }
}

public sealed class PublishEventRegistrationProviderBindingCommandHandler(IRegistrationProviderRepository providerRepository, IMediator mediator, TimeProvider timeProvider)
    : IRequestHandler<PublishEventRegistrationProviderBindingCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(PublishEventRegistrationProviderBindingCommand request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        RegistrationProviderBinding? binding = await providerRepository.GetBindingAsync(request.TenantId, request.BindingId, cancellationToken);
        if (binding is null || !await BindingBelongsToEventAsync(providerRepository, request.TenantId, request.EventId, request.BindingId, cancellationToken))
            return Failure(request.BindingId, "registration_provider_binding_not_found", "Registration provider binding was not found.");
        return await mediator.Send(new PublishRegistrationProviderBindingCommand(
            request.TenantId,
            request.BindingId,
            ToSchemaDriftClass((RegistrationProviderDriftClassEnum)binding.DriftClassId),
            timeProvider.GetUtcNow().UtcDateTime), cancellationToken);
    }
}

public sealed class ReplaceEventDraftRegistrationProviderMappingsCommandHandler(IRegistrationProviderRepository providerRepository, IMediator mediator)
    : IRequestHandler<ReplaceEventDraftRegistrationProviderMappingsCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(ReplaceEventDraftRegistrationProviderMappingsCommand request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        if (!await BindingBelongsToEventAsync(providerRepository, request.TenantId, request.EventId, request.BindingId, cancellationToken))
            return Failure(request.BindingId, "registration_provider_binding_not_found", "Registration provider binding was not found.");

        return await mediator.Send(new ReplaceDraftRegistrationProviderMappingsCommand(
            request.TenantId,
            request.BindingId,
            [.. request.Request.FieldMappings.Select(mapping => new RegistrationProviderFieldMappingInput(mapping.PlatformFieldKey, mapping.ProviderFieldKey, mapping.IsRequired))],
            [.. request.Request.OptionMappings.Select(mapping => new RegistrationProviderOptionMappingInput(mapping.PlatformFieldKey, mapping.PlatformOptionKey, mapping.ProviderOptionKey))]), cancellationToken);
    }
}

public sealed class GetRegistrationChannelsQueryHandler(IRegistrationProviderRepository providerRepository)
    : IRequestHandler<GetRegistrationChannelsQuery, IReadOnlyList<RegistrationChannelDto>>
{
    public async Task<IReadOnlyList<RegistrationChannelDto>> Handle(GetRegistrationChannelsQuery request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        RegistrationRequirement? requirement = await providerRepository.GetRequirementAsync(request.TenantId, request.EventId, request.WorkflowId, request.RequirementId, cancellationToken);
        return requirement is null ? [] : [.. requirement.Channels.Where(channel => !channel.IsDeleted).OrderBy(channel => channel.Ordinal).Select(ToChannelDto)];
    }
}

public sealed class UpsertRegistrationChannelCommandHandler(IRegistrationProviderRepository providerRepository, TimeProvider timeProvider)
    : IRequestHandler<UpsertRegistrationChannelCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(UpsertRegistrationChannelCommand request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        RegistrationRequirement? requirement = await providerRepository.GetRequirementAsync(request.TenantId, request.EventId, request.WorkflowId, request.RequirementId, cancellationToken);
        if (requirement is null) return Failure(request.RequirementId, "registration_requirement_not_found", "Registration requirement was not found.");
        if (!await ProviderBindingUsableAsync(providerRepository, request.TenantId, request.EventId, request.Request, cancellationToken))
            return Failure(request.Request.RegistrationProviderBindingId ?? Guid.Empty, "registration_channel_provider_binding_invalid", "Provider channel requires a published same-event binding.");

        try
        {
            if (request.ChannelId is { } id)
            {
                RegistrationChannel? channel = await providerRepository.GetChannelAsync(request.TenantId, request.EventId, request.WorkflowId, request.RequirementId, id, cancellationToken);
                if (channel is null) return Failure(id, "registration_channel_not_found", "Registration channel was not found.");
                channel.Update(request.Request.Ordinal, request.Request.IsNative, request.Request.RegistrationProviderBindingId);
                await providerRepository.SaveChangesAsync(cancellationToken);
                return Success(channel.Id, "Registration channel updated.");
            }

            RegistrationChannel? deleted = requirement.Channels.FirstOrDefault(channel => channel.IsDeleted && channel.Ordinal == request.Request.Ordinal);
            if (deleted is not null)
            {
                deleted.Revive(request.Request.Ordinal, request.Request.IsNative, request.Request.RegistrationProviderBindingId);
                await providerRepository.SaveChangesAsync(cancellationToken);
                return Success(deleted.Id, "Registration channel created.");
            }

            RegistrationChannel created = RegistrationChannel.Create(requirement, request.Request.Ordinal, request.Request.IsNative,
                request.Request.RegistrationProviderBindingId, timeProvider.GetUtcNow().UtcDateTime);
            requirement.AddChannel(created);
            await providerRepository.SaveChangesAsync(cancellationToken);
            return Success(created.Id, "Registration channel created.");
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
        {
            return Failure(request.ChannelId ?? Guid.Empty, "registration_channel_validation_failed", ex.Message);
        }
    }
}

public sealed class DeleteRegistrationChannelCommandHandler(IRegistrationProviderRepository providerRepository, TimeProvider timeProvider)
    : IRequestHandler<DeleteRegistrationChannelCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(DeleteRegistrationChannelCommand request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        RegistrationChannel? channel = await providerRepository.GetChannelAsync(request.TenantId, request.EventId, request.WorkflowId, request.RequirementId, request.ChannelId, cancellationToken);
        if (channel is null) return Failure(request.ChannelId, "registration_channel_not_found", "Registration channel was not found.");
        channel.Remove(timeProvider.GetUtcNow().UtcDateTime);
        await providerRepository.SaveChangesAsync(cancellationToken);
        return Success(channel.Id, "Registration channel deleted.");
    }
}

public sealed class GetRegistrationProviderLaunchDescriptorQueryHandler(IRegistrationProviderRepository providerRepository, IRegistrationProviderRegistry providerRegistry)
    : IRequestHandler<GetRegistrationProviderLaunchDescriptorQuery, RegistrationProviderLaunchDescriptorDto>
{
    public async Task<RegistrationProviderLaunchDescriptorDto> Handle(GetRegistrationProviderLaunchDescriptorQuery request, CancellationToken cancellationToken)
    {
        RequireEventScope(request.TenantId, request.EventId);
        RegistrationChannel? channel = await providerRepository.GetChannelAsync(request.TenantId, request.EventId, request.WorkflowId, request.RequirementId, request.ChannelId, cancellationToken);
        if (channel is null || channel.IsDeleted || channel.IsNative || channel.RegistrationProviderBindingId != request.BindingId)
            return Descriptor(request.TenantId, request.EventId, request.WorkflowId, request.RequirementId, request.BindingId, request.ChannelId, "unavailable", false, null, "Provider registration", false, "manual", "channel_not_found");

        RegistrationProviderBinding? binding = await providerRepository.GetBindingAsync(request.TenantId, request.BindingId, cancellationToken);
        if (binding is null || binding.Connection is null || !await BindingBelongsToEventAsync(providerRepository, request.TenantId, request.EventId, request.BindingId, cancellationToken))
            return Descriptor(request.TenantId, request.EventId, request.WorkflowId, request.RequirementId, request.BindingId, request.ChannelId, "unavailable", false, null, "Provider registration", false, "manual", "binding_not_found");
        if (binding.StateId != (int)RegistrationProviderBindingStateEnum.Published || binding.DriftClassId >= (int)RegistrationProviderDriftClassEnum.MappingRequired)
            return Descriptor(request.TenantId, request.EventId, request.WorkflowId, request.RequirementId, binding.Id, request.ChannelId, "unavailable", false, null, "Provider registration", false, "manual", "binding_not_launchable");

        if (!BindingLaunchContractMatchesCapabilities(binding))
            return Descriptor(request.TenantId, request.EventId, request.WorkflowId, request.RequirementId, binding.Id, request.ChannelId, "unavailable", false, null, "Provider registration", false, "manual", "channel_binding_mismatch");

        RegistrationProviderCapability? capability = binding.Capabilities.FirstOrDefault(item => !item.IsDeleted &&
            item.CapabilityCode is RegistrationProviderCapabilityCodes.Redirect or RegistrationProviderCapabilityCodes.Embed);
        if (capability is null) return Descriptor(request.TenantId, request.EventId, request.WorkflowId, request.RequirementId, binding.Id, request.ChannelId, "manual", true, null, "Provider registration", true, "manual", "manual_only");

        var tuple = new RegistrationProviderTuple(capability.ProviderCode, capability.DeploymentKind, capability.ApiVersion, capability.AdapterPolicyVersion, capability.ConformanceEvidenceRevision);
        IRegistrationProviderDescriptor? descriptor = providerRegistry.TryResolve(tuple);
        if (descriptor is not IRegistrationProviderPresentation presentationProvider)
            return Descriptor(request.TenantId, request.EventId, request.WorkflowId, request.RequirementId, binding.Id, request.ChannelId, "manual", true, null, "Provider registration", true, "manual", "presentation_unavailable");
        if (!BindingLaunchContractMatchesCapabilities(binding, descriptor.ProvenCapabilities))
            return Descriptor(request.TenantId, request.EventId, request.WorkflowId, request.RequirementId, binding.Id, request.ChannelId, "unavailable", false, null, "Provider registration", false, "manual", "channel_capability_mismatch");

        RegistrationProviderPresentationResult result = await presentationProvider.GetPresentationAsync(new(request.TenantId, binding.Id), cancellationToken);
        Uri? uri = capability.CapabilityCode == RegistrationProviderCapabilityCodes.Embed && result.EmbedAvailable ? result.EmbedUri : result.RedirectUri;
        string mode = capability.CapabilityCode == RegistrationProviderCapabilityCodes.Embed && result.EmbedAvailable ? "embed" : "redirect";
        // DNS caveat: this descriptor is browser navigation metadata only. The server never fetches or proxies
        // provider URLs; literal localhost/private/metadata host blocking remains the SSRF guard at origin approval.
        if (uri is null || !binding.Connection.IsOriginApproved(uri))
            return Descriptor(request.TenantId, request.EventId, request.WorkflowId, request.RequirementId, binding.Id, request.ChannelId, "manual", true, null, "Provider registration", true, "manual", "origin_not_approved");

        return Descriptor(request.TenantId, request.EventId, request.WorkflowId, request.RequirementId, binding.Id, request.ChannelId, mode, true, uri.ToString(), "Provider registration", mode == "redirect", "manual", "ok");
    }
}

internal static class RegistrationProviderManagementHandlerHelpers
{
    public static void RequireEventScope(Guid tenantId, Guid eventId)
    {
        if (tenantId == Guid.Empty || eventId == Guid.Empty)
        {
            throw new ValidationException("Tenant and event identifiers are required.");
        }
    }

    public static bool BoundedReference(string value, int maxLength) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maxLength && !value.Any(char.IsControl);

    public static async Task<bool> BindingBelongsToEventAsync(IRegistrationProviderRepository providerRepository, PollRegistrationProviderReconciliationCommand request, CancellationToken cancellationToken) =>
        await BindingBelongsToEventAsync(providerRepository, request.TenantId, request.EventId, request.BindingId, cancellationToken);

    public static async Task<bool> BindingBelongsToEventAsync(IRegistrationProviderRepository providerRepository, Guid tenantId, Guid eventId, Guid bindingId, CancellationToken cancellationToken) =>
        (await providerRepository.GetBindingsForEventAsync(tenantId, eventId, cancellationToken)).Any(binding => binding.Id == bindingId);

    public static bool TryParseBindingId(string providerDecisionId, out Guid bindingId) =>
        Guid.TryParseExact(providerDecisionId.Split(':', 2)[0], "N", out bindingId);

    public static string SafeCategory(string? value) => string.IsNullOrWhiteSpace(value)
        ? "none"
        : new string(value.Trim().ToLowerInvariant().Where(character => char.IsAsciiLetterOrDigit(character) || character == '_').Take(100).ToArray());

    public static string Sha256Identifier(byte[] bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));

    public static BaseCommandResponse<Guid> Success(Guid id, string message) => new() { Id = id, Success = true, Message = message };

    public static BaseCommandResponse<Guid> Failure(Guid id, string code, string message) => new() { Id = id, Success = false, FailureCode = code, Message = message, Errors = [message] };

    public static RegistrationProviderConnectionDto ToConnectionDto(RegistrationProviderConnection connection, Guid eventId = default) => new()
    {
        Id = connection.Id,
        TenantId = connection.TenantId,
        EventId = eventId,
        Name = connection.Name,
        ProviderKindId = connection.ProviderKindId,
        DeploymentKindId = connection.DeploymentKindId,
        ApiTokenSecretBindingId = connection.ApiTokenSecretBindingId,
        WebhookSecretBindingId = connection.WebhookSecretBindingId,
        ApprovedOrigins = [.. connection.ApprovedOrigins.Where(origin => !origin.IsDeleted).Select(origin => origin.Origin).Order(StringComparer.OrdinalIgnoreCase)]
    };

    public static RegistrationProviderBindingDto ToBindingDto(RegistrationProviderBinding binding, Guid eventId = default)
    {
        Dictionary<Guid, string> fieldKeys = binding.FieldMappings
            .Where(mapping => !mapping.IsDeleted)
            .ToDictionary(mapping => mapping.Id, mapping => mapping.PlatformFieldKey);

        return new()
        {
            Id = binding.Id,
            TenantId = binding.TenantId,
            EventId = eventId,
            ConnectionId = binding.RegistrationProviderConnectionId,
            FormId = binding.RegistrationFormId,
            FormVersionId = binding.RegistrationFormVersionId,
            PresentationModeId = binding.PresentationModeId,
            CollectionModeId = binding.CollectionModeId,
            CompletionModeId = binding.CompletionModeId,
            TrustLevelId = binding.TrustLevelId,
            DriftClassId = binding.DriftClassId,
            StateId = binding.StateId,
            PublishedAt = binding.PublishedAt,
            CapabilityCodes = [.. binding.Capabilities.Where(capability => !capability.IsDeleted).Select(capability => capability.CapabilityCode).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)],
            FieldMappings = [.. binding.FieldMappings
                .Where(mapping => !mapping.IsDeleted)
                .OrderBy(mapping => mapping.PlatformFieldKey, StringComparer.Ordinal)
                .Select(mapping => new RegistrationProviderFieldMappingDto
                {
                    PlatformFieldKey = mapping.PlatformFieldKey,
                    ProviderFieldKey = mapping.ProviderFieldKey,
                    IsRequired = mapping.IsRequired
                })],
            OptionMappings = [.. binding.OptionMappings
                .Where(mapping => !mapping.IsDeleted && fieldKeys.ContainsKey(mapping.RegistrationProviderFieldMappingId))
                .OrderBy(mapping => fieldKeys[mapping.RegistrationProviderFieldMappingId], StringComparer.Ordinal)
                .ThenBy(mapping => mapping.PlatformOptionKey, StringComparer.Ordinal)
                .Select(mapping => new RegistrationProviderOptionMappingDto
                {
                    PlatformFieldKey = fieldKeys[mapping.RegistrationProviderFieldMappingId],
                    PlatformOptionKey = mapping.PlatformOptionKey,
                    ProviderOptionKey = mapping.ProviderOptionKey
                })]
        };
    }

    public static RegistrationChannelDto ToChannelDto(RegistrationChannel channel) => new()
    {
        Id = channel.Id,
        TenantId = channel.TenantId,
        EventId = channel.EventId,
        RegistrationWorkflowId = channel.RegistrationWorkflowId,
        RegistrationRequirementId = channel.RegistrationRequirementId,
        Ordinal = channel.Ordinal,
        IsNative = channel.IsNative,
        RegistrationProviderBindingId = channel.RegistrationProviderBindingId
    };

    public static RegistrationProviderLaunchDescriptorDto Descriptor(Guid tenantId, Guid eventId, Guid workflowId, Guid requirementId, Guid bindingId, Guid channelId, string mode, bool available, string? url, string title, bool openInNewTab, string fallbackMode, string reason) => new()
    {
        BindingId = bindingId,
        ChannelId = channelId,
        TenantId = tenantId,
        EventId = eventId,
        WorkflowId = workflowId,
        RequirementId = requirementId,
        Mode = mode,
        Available = available,
        Url = url,
        Title = title,
        OpenInNewTab = openInNewTab,
        FallbackMode = fallbackMode,
        Reason = reason
    };

    public static bool BindingLaunchContractMatchesCapabilities(RegistrationProviderBinding binding, RegistrationProviderCapabilitySet? effectiveCapabilities = null)
    {
        RegistrationProviderCapabilitySet capabilities = effectiveCapabilities ?? RegistrationProviderCapabilitySet.FromCodes(
            binding.Capabilities.Where(capability => !capability.IsDeleted).Select(capability => capability.CapabilityCode));
        return (RegistrationProviderPresentationModeEnum)binding.PresentationModeId switch
        {
            RegistrationProviderPresentationModeEnum.Redirect => capabilities.Redirect,
            RegistrationProviderPresentationModeEnum.Embed => capabilities.Embed,
            RegistrationProviderPresentationModeEnum.Manual => capabilities.Manual,
            _ => false
        } && (RegistrationProviderCollectionModeEnum)binding.CollectionModeId switch
        {
            RegistrationProviderCollectionModeEnum.ProviderHosted => capabilities.Redirect || capabilities.Embed || capabilities.Manual,
            RegistrationProviderCollectionModeEnum.ProviderApi => capabilities.SubmissionWrite || capabilities.SubmissionSink,
            _ => false
        } && (RegistrationProviderCompletionModeEnum)binding.CompletionModeId switch
        {
            RegistrationProviderCompletionModeEnum.Callback => capabilities.CallbackVerification,
            RegistrationProviderCompletionModeEnum.Polling => capabilities.SubmissionRead && capabilities.Reconciliation,
            RegistrationProviderCompletionModeEnum.Manual => capabilities.Manual,
            _ => false
        } && (RegistrationProviderTrustLevelEnum)binding.TrustLevelId switch
        {
            RegistrationProviderTrustLevelEnum.Untrusted => true,
            RegistrationProviderTrustLevelEnum.CompletionOnly => capabilities.CallbackVerification || capabilities.SubmissionRead || capabilities.Manual,
            RegistrationProviderTrustLevelEnum.SelectedFields or RegistrationProviderTrustLevelEnum.FullCanonical => capabilities.SubmissionRead || capabilities.SubmissionWrite || capabilities.SubmissionSink,
            _ => false
        };
    }

    public static bool TryConnectionRequest(RegistrationProviderConnectionRequestDto request, out RegistrationProviderKindEnum providerKind, out RegistrationProviderDeploymentKindEnum deploymentKind)
    {
        providerKind = (RegistrationProviderKindEnum)request.ProviderKindId;
        deploymentKind = (RegistrationProviderDeploymentKindEnum)request.DeploymentKindId;
        return BoundedReference(request.Name, 120) && Enum.IsDefined(providerKind) && Enum.IsDefined(deploymentKind) &&
            request.ApiTokenSecretBindingId != Guid.Empty && request.WebhookSecretBindingId != Guid.Empty;
    }

    public static RegistrationProviderSchemaDriftClass ToSchemaDriftClass(RegistrationProviderDriftClassEnum driftClass) => driftClass switch
    {
        RegistrationProviderDriftClassEnum.NoDrift => RegistrationProviderSchemaDriftClass.NoDrift,
        RegistrationProviderDriftClassEnum.AdditiveOptionalChange => RegistrationProviderSchemaDriftClass.AdditiveOptionalChange,
        RegistrationProviderDriftClassEnum.LabelOnlyChange => RegistrationProviderSchemaDriftClass.LabelOnlyChange,
        RegistrationProviderDriftClassEnum.MappingRequired => RegistrationProviderSchemaDriftClass.MappingRequired,
        RegistrationProviderDriftClassEnum.RequiredFieldRemoved => RegistrationProviderSchemaDriftClass.RequiredFieldRemoved,
        RegistrationProviderDriftClassEnum.TypeChanged => RegistrationProviderSchemaDriftClass.TypeChanged,
        RegistrationProviderDriftClassEnum.OptionSetChanged => RegistrationProviderSchemaDriftClass.OptionSetChanged,
        _ => RegistrationProviderSchemaDriftClass.UnsupportedChange
    };

    public static bool TryBindingRequest(RegistrationProviderBindingRequestDto request, out RegistrationProviderPresentationModeEnum presentation, out RegistrationProviderCollectionModeEnum collection, out RegistrationProviderCompletionModeEnum completion, out RegistrationProviderTrustLevelEnum trust)
    {
        presentation = (RegistrationProviderPresentationModeEnum)request.PresentationModeId;
        collection = (RegistrationProviderCollectionModeEnum)request.CollectionModeId;
        completion = (RegistrationProviderCompletionModeEnum)request.CompletionModeId;
        trust = (RegistrationProviderTrustLevelEnum)request.TrustLevelId;
        return request.ConnectionId != Guid.Empty && request.FormId != Guid.Empty && request.FormVersionId != Guid.Empty &&
            Enum.IsDefined(presentation) && Enum.IsDefined(collection) && Enum.IsDefined(completion) && Enum.IsDefined(trust);
    }

    public static async Task<bool> ProviderBindingUsableAsync(IRegistrationProviderRepository providerRepository, Guid tenantId, Guid eventId, RegistrationChannelRequestDto request, CancellationToken cancellationToken)
    {
        if (request.IsNative) return request.RegistrationProviderBindingId is null;
        if (request.RegistrationProviderBindingId is not { } bindingId || bindingId == Guid.Empty) return false;
        RegistrationProviderBinding? binding = await providerRepository.GetBindingAsync(tenantId, bindingId, cancellationToken);
        return binding is not null && binding.StateId == (int)RegistrationProviderBindingStateEnum.Published &&
            await BindingBelongsToEventAsync(providerRepository, tenantId, eventId, bindingId, cancellationToken);
    }

    public static async Task<bool> ValidateRetainedEffectAsync(
        IRegistrationProviderRepository providerRepository,
        IIncomingWebhookEffectReceiptRepository receiptRepository,
        IncomingWebhookEffectOutbox pointer,
        Guid tenantId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (pointer.IncomingWebhookMessage is null ||
            pointer.TenantId != tenantId ||
            !TryParseBindingId(pointer.ProviderDecisionId, out Guid bindingId) ||
            !string.Equals(pointer.Provider, "registration-provider", StringComparison.Ordinal) ||
            !IsRegistrationProviderManagementEffect(pointer.EffectKind) ||
            !string.Equals(pointer.IncomingWebhookMessage.Provider, pointer.Provider, StringComparison.Ordinal) ||
            !string.Equals(pointer.IncomingWebhookMessage.ProviderMessageId, pointer.ProviderDecisionId, StringComparison.Ordinal) ||
            !string.Equals(pointer.IncomingWebhookMessage.PayloadHash, pointer.PayloadSha256, StringComparison.Ordinal) ||
            !await BindingBelongsToEventAsync(providerRepository, tenantId, eventId, bindingId, cancellationToken))
        {
            return false;
        }

        IncomingWebhookEffectReceipt? receipt = await receiptRepository.GetByIdentityAsync(tenantId, pointer.IncomingWebhookMessageId, pointer.EffectKind, cancellationToken);
        if (receipt is null)
        {
            return true;
        }

        try
        {
            receipt.EnsureMatches(tenantId, pointer.IncomingWebhookMessageId, pointer.EffectKind, pointer.PayloadSha256, pointer.ProcessingGeneration);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool IsRegistrationProviderManagementEffect(string effectKind) =>
        string.Equals(effectKind, ProcessProviderSubmissionEffectCommandHandler.StableEffectKind, StringComparison.Ordinal) ||
        string.Equals(effectKind, QueueManualRegistrationProviderImportCommandHandler.ManualImportEffectKind, StringComparison.Ordinal);
}
