// ABOUTME: Processes one fenced provider-submission webhook effect through Phase 8 registration persistence.
// ABOUTME: Enforces sync-mode storage boundaries without adding provider-specific callback adapters.

using System.Security.Cryptography;
using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.RegistrationSubmissions.Commands;

public sealed record ProcessProviderSubmissionEffectCommand(
    Guid TenantId,
    Guid IncomingWebhookMessageId,
    Guid BindingId,
    string Provider,
    ReadOnlyMemory<byte> PayloadBytes,
    IReadOnlyDictionary<string, string> Headers) : IRequest<ProviderSubmissionEffectResult>;

public sealed record ProviderSubmissionEffectResult(ProviderSubmissionEffectOutcome Outcome, string Code)
{
    public static ProviderSubmissionEffectResult Completed(string code = "completed") => new(ProviderSubmissionEffectOutcome.Completed, code);
    public static ProviderSubmissionEffectResult NeedsReconciliation(string code) => new(ProviderSubmissionEffectOutcome.NeedsReconciliation, code);
}

public enum ProviderSubmissionEffectOutcome
{
    Completed = 1,
    NeedsReconciliation = 2
}

public sealed class ProcessProviderSubmissionEffectCommandValidator : AbstractValidator<ProcessProviderSubmissionEffectCommand>
{
    public ProcessProviderSubmissionEffectCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.IncomingWebhookMessageId).NotEmpty();
        RuleFor(command => command.BindingId).NotEmpty();
        RuleFor(command => command.Provider).NotEmpty().MaximumLength(100);
        RuleFor(command => command.PayloadBytes.Length).InclusiveBetween(1, 256 * 1024);
        RuleFor(command => command.Headers).NotNull();
    }
}

public sealed class ProcessProviderSubmissionEffectCommandHandler(
    IRegistrationProviderRepository providerRepository,
    IRegistrationSubmissionRepository submissionRepository,
    IRegistrationInventoryRepository inventoryRepository,
    IRegistrationFormAuthoringRepository formRepository,
    IRegistrationParticipantRepository participantRepository,
    IRegistrationSensitiveValueProtector protector,
    ISender sender,
    IRegistrationProviderCallbackReceiptProtector receiptProtector,
    TimeProvider timeProvider)
    : IRequestHandler<ProcessProviderSubmissionEffectCommand, ProviderSubmissionEffectResult>
{
    public const string StableEffectKind = "registration.provider_submission";

    public async Task<ProviderSubmissionEffectResult> Handle(
        ProcessProviderSubmissionEffectCommand request,
        CancellationToken cancellationToken)
    {
        await new ProcessProviderSubmissionEffectCommandValidator().ValidateAndThrowAsync(request, cancellationToken);

        RegistrationProviderBinding? binding = await providerRepository.GetBindingAsync(
            request.TenantId, request.BindingId, cancellationToken);
        if (binding is null)
        {
            return ProviderSubmissionEffectResult.NeedsReconciliation("UNKNOWN_BINDING");
        }

        RegistrationProviderTuple tuple = ResolveTuple(binding, request.Provider);
        if (tuple.ProviderCode.Length == 0)
        {
            return await ParkAsync(request, null, "UNKNOWN_TUPLE", cancellationToken);
        }

        ProviderSubmissionEnvelope envelope;
        try
        {
            envelope = ProviderSubmissionEnvelope.Parse(request.PayloadBytes.Span);
        }
        catch (JsonException)
        {
            return ProviderSubmissionEffectResult.NeedsReconciliation("MALFORMED_EVIDENCE");
        }

        if (!TryValidateReceipt(request, binding, tuple, envelope.ProviderSubmissionId))
        {
            return await ParkAsync(request, null, "UNVERIFIABLE_EVIDENCE", cancellationToken);
        }

        RegistrationAttempt? attempt = await submissionRepository.GetAttemptAsync(
            request.TenantId, envelope.AttemptId, cancellationToken);
        if (attempt is null || attempt.RegistrationProviderBindingId != request.BindingId)
        {
            return await ParkAsync(request, attempt, "ATTEMPT_NOT_FOUND", cancellationToken);
        }

        RegistrationRequirement requirement = await submissionRepository.GetRequirementAsync(
            request.TenantId, attempt.RegistrationRequirementId, cancellationToken)
            ?? throw new InvalidOperationException("Pinned registration requirement was not found.");
        DateTime receivedAt = envelope.ReceivedAt ?? timeProvider.GetUtcNow().UtcDateTime;
        RegistrationEvidenceHash evidenceHash = RegistrationEvidenceHash.Create(HashBase64(request.PayloadBytes.Span));
        RegistrationTransportIdempotencyHash transportHash = RegistrationTransportIdempotencyHash.Create(
            HashBase64(request.IncomingWebhookMessageId.ToByteArray()));

        if ((RegistrationAnswerSyncModeEnum)requirement.AnswerSyncModeId == RegistrationAnswerSyncModeEnum.NONE)
        {
            return ProviderSubmissionEffectResult.Completed("sync_none_no_storage");
        }

        if (BindingCannotAutoFinalize(binding))
        {
            return await ParkAsync(request, attempt, "BLOCKING_DRIFT", cancellationToken);
        }

        if ((RegistrationAnswerSyncModeEnum)requirement.AnswerSyncModeId == RegistrationAnswerSyncModeEnum.MIRROR_ONLY)
        {
            if (HasCapability(binding, RegistrationProviderCapabilityCodes.SubmissionSink))
            {
                RegistrationSubmissionPersistenceResult mirrorPersisted = await submissionRepository.PersistEvidenceOnlyAsync(RegistrationSubmission.CreateProviderEvidenceOnly(
                    attempt,
                    evidenceHash,
                    receivedAt,
                    transportHash,
                    envelope.ProviderSubmissionId,
                    envelope.ProviderResponseRevision,
                    envelope.ProviderSubjectId,
                    envelope.ProviderCorrelationId),
                    cancellationToken);
                return mirrorPersisted.Outcome is RegistrationSubmissionPersistenceOutcome.Inserted or RegistrationSubmissionPersistenceOutcome.Existing
                    ? ProviderSubmissionEffectResult.Completed("mirror_only_recorded")
                    : ProviderSubmissionEffectResult.NeedsReconciliation("PERSISTENCE_CONFLICT");
            }

            return await ParkAsync(request, attempt, "MIRROR_SINK_UNSUPPORTED", cancellationToken);
        }

        if (!HasMinimumTrust(binding, (RegistrationAnswerSyncModeEnum)requirement.AnswerSyncModeId))
        {
            return await ParkAsync(request, attempt, "BELOW_MINIMUM_TRUST", cancellationToken);
        }

        RegistrationSubmission submission;
        try
        {
            submission = attempt.SubmitProvider(
                evidenceHash,
                receivedAt,
                transportHash,
                envelope.ProviderSubmissionId,
                envelope.ProviderResponseRevision,
                envelope.ProviderSubjectId,
                envelope.ProviderCorrelationId);
        }
        catch (InvalidOperationException)
        {
            return await ParkAsync(request, attempt, "STALE_OR_OUT_OF_ORDER", cancellationToken);
        }

        RegistrationSubmissionPersistenceResult persisted = (RegistrationAnswerSyncModeEnum)requirement.AnswerSyncModeId switch
        {
            RegistrationAnswerSyncModeEnum.COMPLETION_ONLY => await PersistCompletionOnlyAsync(
                attempt, submission, requirement, cancellationToken),
            RegistrationAnswerSyncModeEnum.SELECTED_FIELDS or RegistrationAnswerSyncModeEnum.FULL_CANONICAL =>
                await PersistCanonicalAsync(attempt, submission, binding, envelope, cancellationToken),
            _ => await PersistEvidenceOnlyAsync(submission, "UNSUPPORTED_SYNC_MODE", cancellationToken)
        };

        return persisted.Outcome is RegistrationSubmissionPersistenceOutcome.Inserted or RegistrationSubmissionPersistenceOutcome.Existing
            ? ProviderSubmissionEffectResult.Completed(persisted.Outcome.ToString().ToLowerInvariant())
            : ProviderSubmissionEffectResult.NeedsReconciliation("PERSISTENCE_CONFLICT");
    }

    private async Task<RegistrationSubmissionPersistenceResult> PersistCompletionOnlyAsync(
        RegistrationAttempt attempt,
        RegistrationSubmission submission,
        RegistrationRequirement requirement,
        CancellationToken cancellationToken)
    {
        RegistrationOrder order = await inventoryRepository.GetOrderWithLinesAsync(
            submission.RegistrationOrderId, submission.TenantId, cancellationToken)
            ?? throw new InvalidOperationException("Registration order was not found.");
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        return await submissionRepository.PersistAcceptedWithNormalizationAsync(
            attempt,
            submission,
            attempt.ConcurrencyStamp,
            [],
            [],
            [],
            [RegistrationRequirementFulfillment.CreateFulfilled(
                order,
                requirement,
                submission,
                RegistrationAnswerSubjectTypeEnum.RegistrationOrder,
                order.Id,
                now)],
            cancellationToken);
    }

    private async Task<RegistrationSubmissionPersistenceResult> PersistCanonicalAsync(
        RegistrationAttempt attempt,
        RegistrationSubmission submission,
        RegistrationProviderBinding binding,
        ProviderSubmissionEnvelope envelope,
        CancellationToken cancellationToken)
    {
        RegistrationOrder order = await inventoryRepository.GetOrderWithLinesAsync(
            submission.RegistrationOrderId, submission.TenantId, cancellationToken)
            ?? throw new InvalidOperationException("Registration order was not found.");
        IReadOnlyList<RegistrationParticipant> participants = await participantRepository.GetParticipantsByOrderAsync(
            order.Id, submission.TenantId, cancellationToken);
        IReadOnlyList<RegistrationTicketAssignment> assignments = await participantRepository
            .GetAssignmentsWithParticipantsByOrderAsync(order.Id, submission.TenantId, cancellationToken);
        IReadOnlyList<RegistrationSubmissionAnswerInput> inputs = await MapAnswersAsync(
            submission, binding, envelope.Answers, cancellationToken);
        RegistrationSubmissionNormalizationDraft draft = await NormalizeRegistrationSubmissionCommandHandler.PrepareAsync(
            submission, inputs, order, participants, assignments, submissionRepository, formRepository,
            protector, timeProvider, cancellationToken);
        return await submissionRepository.PersistAcceptedWithNormalizationAsync(
            attempt,
            submission,
            attempt.ConcurrencyStamp,
            draft.Answers,
            draft.ConsentRecords,
            draft.Issues,
            draft.Fulfillments,
            cancellationToken);
    }

    private async Task<IReadOnlyList<RegistrationSubmissionAnswerInput>> MapAnswersAsync(
        RegistrationSubmission submission,
        RegistrationProviderBinding binding,
        IReadOnlyDictionary<string, JsonElement> providerAnswers,
        CancellationToken cancellationToken)
    {
        RegistrationFormVersion version = await formRepository.GetVersionAsync(
            submission.EventId, submission.RegistrationFormId, submission.RegistrationFormVersionId, cancellationToken)
            ?? throw new InvalidOperationException("Pinned registration form version was not found.");
        Dictionary<string, RegistrationFormField> fields = version.Sections
            .SelectMany(section => section.Fields)
            .Where(field => !field.IsDeleted)
            .ToDictionary(field => field.Namespace + "." + field.Key, StringComparer.OrdinalIgnoreCase);
        List<RegistrationSubmissionAnswerInput> inputs = [];
        foreach (RegistrationProviderFieldMapping mapping in binding.FieldMappings.Where(mapping => !mapping.IsDeleted))
        {
            if (!fields.TryGetValue(mapping.PlatformFieldKey, out RegistrationFormField? field) ||
                !providerAnswers.TryGetValue(mapping.ProviderFieldKey, out JsonElement value))
            {
                continue;
            }

            inputs.Add(new RegistrationSubmissionAnswerInput(
                field.Id,
                RegistrationAnswerSubjectTypeEnum.RegistrationOrder,
                submission.RegistrationOrderId,
                null,
                value.Clone()));
        }

        return inputs;
    }

    private async Task<ProviderSubmissionEffectResult> ParkAsync(
        ProcessProviderSubmissionEffectCommand request,
        RegistrationAttempt? attempt,
        string code,
        CancellationToken cancellationToken)
    {
        if (attempt is null)
        {
            return ProviderSubmissionEffectResult.NeedsReconciliation(code);
        }

        RegistrationSubmission submission = RegistrationSubmission.CreateProviderEvidenceOnly(
            attempt,
            RegistrationEvidenceHash.Create(HashBase64(request.PayloadBytes.Span)),
            timeProvider.GetUtcNow().UtcDateTime,
            RegistrationTransportIdempotencyHash.Create(HashBase64(request.IncomingWebhookMessageId.ToByteArray())),
            "parked-" + request.IncomingWebhookMessageId.ToString("N"),
            "parked",
            null,
            null);
        await PersistEvidenceOnlyAsync(submission, code, cancellationToken);
        return ProviderSubmissionEffectResult.NeedsReconciliation(code);
    }

    private async Task<RegistrationSubmissionPersistenceResult> PersistEvidenceOnlyAsync(
        RegistrationSubmission submission,
        string code,
        CancellationToken cancellationToken)
    {
        RegistrationSubmissionPersistenceResult result = await submissionRepository.PersistEvidenceOnlyAsync(
            submission,
            cancellationToken);
        if (result.Outcome == RegistrationSubmissionPersistenceOutcome.Inserted)
        {
            await submissionRepository.PersistNormalizationAsync(
                [],
                [],
                [RegistrationSubmissionIssue.Create(submission, code, timeProvider.GetUtcNow().UtcDateTime)],
                cancellationToken);
        }

        return result;
    }

    private static RegistrationProviderTuple ResolveTuple(RegistrationProviderBinding binding, string provider)
    {
        RegistrationProviderCapability? capability = binding.Capabilities.FirstOrDefault(capability =>
            !capability.IsDeleted &&
            string.Equals(capability.ProviderCode, provider, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(capability.CapabilityCode, RegistrationProviderCapabilityCodes.CallbackVerification, StringComparison.OrdinalIgnoreCase));
        return capability is null
            ? new RegistrationProviderTuple(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
            : new RegistrationProviderTuple(capability.ProviderCode, capability.DeploymentKind, capability.ApiVersion,
                capability.AdapterPolicyVersion, capability.ConformanceEvidenceRevision);
    }

    private static bool BindingCannotAutoFinalize(RegistrationProviderBinding binding) =>
        binding.DriftClassId >= (int)RegistrationProviderDriftClassEnum.MappingRequired ||
        binding.StateId != (int)RegistrationProviderBindingStateEnum.Published;

    private static bool HasMinimumTrust(RegistrationProviderBinding binding, RegistrationAnswerSyncModeEnum syncMode) => syncMode switch
    {
        RegistrationAnswerSyncModeEnum.COMPLETION_ONLY => binding.TrustLevelId >= (int)RegistrationProviderTrustLevelEnum.CompletionOnly,
        RegistrationAnswerSyncModeEnum.SELECTED_FIELDS => binding.TrustLevelId >= (int)RegistrationProviderTrustLevelEnum.SelectedFields,
        RegistrationAnswerSyncModeEnum.FULL_CANONICAL => binding.TrustLevelId >= (int)RegistrationProviderTrustLevelEnum.FullCanonical,
        RegistrationAnswerSyncModeEnum.MIRROR_ONLY => false,
        RegistrationAnswerSyncModeEnum.NONE => true,
        _ => false
    };

    private static bool HasCapability(RegistrationProviderBinding binding, string capabilityCode) =>
        binding.Capabilities.Any(capability =>
            !capability.IsDeleted &&
            string.Equals(capability.CapabilityCode, capabilityCode, StringComparison.OrdinalIgnoreCase));

    private bool TryValidateReceipt(
        ProcessProviderSubmissionEffectCommand request,
        RegistrationProviderBinding binding,
        RegistrationProviderTuple tuple,
        string providerSubmissionId)
    {
        if (!request.Headers.TryGetValue("X-Registration-Verification-Receipt", out string? protectedReceipt) ||
            string.IsNullOrWhiteSpace(protectedReceipt))
        {
            return false;
        }

        RegistrationProviderCallbackReceipt receipt;
        try
        {
            receipt = receiptProtector.Unprotect(protectedReceipt);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or CryptographicException or JsonException or FormatException)
        {
            return false;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        return receipt.TenantId == request.TenantId &&
               receipt.ConnectionId == binding.RegistrationProviderConnectionId &&
               receipt.BindingId == request.BindingId &&
               string.Equals(receipt.Provider, request.Provider, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(receipt.TupleKey, tuple.Key, StringComparison.Ordinal) &&
               string.Equals(receipt.BodySha256, HashSha256Hex(request.PayloadBytes.Span), StringComparison.Ordinal) &&
               string.Equals(receipt.ProviderSubmissionId, providerSubmissionId, StringComparison.Ordinal) &&
               !string.IsNullOrWhiteSpace(receipt.Nonce) &&
               receipt.VerifiedAt <= now.AddMinutes(5) &&
               receipt.VerifiedAt >= now.AddHours(-24);
    }

    private static string HashBase64(ReadOnlySpan<byte> bytes) => Convert.ToBase64String(SHA256.HashData(bytes));

    private static string HashSha256Hex(ReadOnlySpan<byte> bytes) =>
        "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private sealed record ProviderSubmissionEnvelope(
        Guid AttemptId,
        string ProviderSubmissionId,
        string ProviderResponseRevision,
        DateTime? ReceivedAt,
        string? ProviderSubjectId,
        string? ProviderCorrelationId,
        IReadOnlyDictionary<string, JsonElement> Answers)
    {
        public static ProviderSubmissionEnvelope Parse(ReadOnlySpan<byte> payload)
        {
            using JsonDocument document = JsonDocument.Parse(payload.ToArray());
            JsonElement root = document.RootElement;
            Dictionary<string, JsonElement> answers = new(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("answers", out JsonElement answerObject) && answerObject.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in answerObject.EnumerateObject())
                {
                    answers[property.Name] = property.Value.Clone();
                }
            }

            if (!root.TryGetProperty("attemptId", out JsonElement attemptIdElement) ||
                !attemptIdElement.TryGetGuid(out Guid attemptId))
            {
                throw new JsonException("Provider submission envelope is missing a valid attempt id.");
            }

            return new(
                attemptId,
                Required(root, "providerSubmissionId"),
                Required(root, "providerResponseRevision"),
                root.TryGetProperty("receivedAt", out JsonElement receivedAt) && receivedAt.ValueKind == JsonValueKind.String
                    ? receivedAt.GetDateTime().ToUniversalTime()
                    : null,
                Optional(root, "providerSubjectId"),
                Optional(root, "providerCorrelationId"),
                answers);
        }

        private static string Required(JsonElement root, string name) =>
            Optional(root, name) is { Length: > 0 } value && value.Length <= 200
                ? value
                : throw new JsonException("Provider submission envelope is missing a bounded identity.");

        private static string? Optional(JsonElement root, string name) =>
            root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()?.Trim()
                : null;
    }
}
