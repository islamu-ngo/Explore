// ABOUTME: Launches and submits native registration attempts after order-scoped access has been established.
// ABOUTME: Pins current published lineage, keeps bearer tokens redacted, and returns only safe validation issues.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.RegistrationSubmissions;
using Explore.Domain;
using Explore.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.RegistrationSubmissions.Commands;

public sealed record LaunchNativeRegistrationAttemptCommand(
    Guid TenantId,
    Guid EventId,
    Guid OrderId,
    Guid RequirementId,
    Guid ChannelId,
    Guid FormId,
    Guid FormVersionId) : IRequest<NativeRegistrationAttemptResult>;

public sealed record SubmitNativeRegistrationAttemptCommand(
    Guid TenantId,
    Guid EventId,
    Guid OrderId,
    Guid RequirementId,
    Guid AttemptId,
    string? AttemptCapabilityToken,
    string? IdempotencyKey,
    IReadOnlyList<RegistrationSubmissionAnswerInput> Answers) : IRequest<NativeRegistrationSubmissionResult>;

public sealed record NativeRegistrationAttemptResult(
    bool Success,
    Guid AttemptId,
    Guid RequirementId,
    Guid ChannelId,
    Guid FormId,
    Guid FormVersionId,
    DateTime ExpiresAt,
    NativeRegistrationFormDefinitionDto? Form,
    IReadOnlyList<NativeRegistrationAnswerSubjectDto> Subjects,
    NativeRegistrationRequirementProgressDto? Progress,
    bool CanSkip,
    string? AttemptCapabilityToken,
    string? FailureCode = null);

public sealed record NativeRegistrationSubmissionResult(
    bool Success,
    Guid SubmissionId,
    IReadOnlyList<RegistrationSubmissionIssueDto> Issues,
    string? FailureCode = null);

public sealed class LaunchNativeRegistrationAttemptCommandValidator
    : AbstractValidator<LaunchNativeRegistrationAttemptCommand>
{
    public LaunchNativeRegistrationAttemptCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.RequirementId).NotEmpty();
        RuleFor(command => command.ChannelId).NotEmpty();
        RuleFor(command => command.FormId).NotEmpty();
        RuleFor(command => command.FormVersionId).NotEmpty();
    }
}

public sealed class SubmitNativeRegistrationAttemptCommandValidator
    : AbstractValidator<SubmitNativeRegistrationAttemptCommand>
{
    public SubmitNativeRegistrationAttemptCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.RequirementId).NotEmpty();
        RuleFor(command => command.AttemptId).NotEmpty();
        RuleFor(command => command.AttemptCapabilityToken).NotEmpty();
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(128);
        RuleFor(command => command.Answers).NotNull();
    }
}

public sealed class LaunchNativeRegistrationAttemptCommandHandler(
    IRegistrationInventoryRepository inventory,
    IRegistrationFormAuthoringRepository authoring,
    IRegistrationSubmissionRepository submissions,
    IRegistrationParticipantRepository participantRepository,
    IRegistrationFinalizationRepository finalization,
    IGuestCapabilityTokenService capabilities,
    TimeProvider timeProvider)
    : IRequestHandler<LaunchNativeRegistrationAttemptCommand, NativeRegistrationAttemptResult>
{
    public async Task<NativeRegistrationAttemptResult> Handle(
        LaunchNativeRegistrationAttemptCommand request,
        CancellationToken cancellationToken)
    {
        if (!(await new LaunchNativeRegistrationAttemptCommandValidator()
                .ValidateAsync(request, cancellationToken)).IsValid)
        {
            return Missing(request);
        }

        RegistrationOrder? order = await inventory.GetOrderWithLinesAsync(
            request.OrderId, request.TenantId, cancellationToken);
        NativeRegistrationLaunchAuthority? authority = await NativeRegistrationLaunchDescriptorResolver.ResolveAsync(
            authoring, order, request.EventId, cancellationToken);
        RegistrationRequirement? requirement = authority?.Workflow.Requirements.SingleOrDefault(candidate =>
            !candidate.IsDeleted && candidate.Id == request.RequirementId);
        RegistrationChannel? channel = requirement?.Channels.SingleOrDefault(candidate =>
            !candidate.IsDeleted && candidate.IsNative && candidate.Id == request.ChannelId);
        RegistrationFormVersion? version = authority?.Version;
        if (order is null || authority is null ||
            requirement is null || channel is null ||
            version is null || version.RegistrationFormId != request.FormId || version.Id != request.FormVersionId)
        {
            return Missing(request);
        }

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        DateTime expiresAt = order.ExpiresAt is { } orderExpiry && orderExpiry < now.AddHours(1)
            ? orderExpiry
            : now.AddHours(1);
        if (expiresAt <= now)
        {
            return Missing(request);
        }

        IReadOnlyList<RegistrationParticipant> participants = await participantRepository.GetParticipantsByOrderAsync(
            order.Id, request.TenantId, cancellationToken);
        IReadOnlyList<RegistrationTicketAssignment> assignments = await participantRepository
            .GetAssignmentsWithParticipantsByOrderAsync(order.Id, request.TenantId, cancellationToken);
        IReadOnlyList<RegistrationRequirementFulfillment> fulfillments = await finalization.GetFulfillmentsAsync(
            request.TenantId, order.Id, requirement.Id, cancellationToken);
        IReadOnlyList<NativeRegistrationAnswerSubjectDto> subjects = NativeRegistrationAttemptContractBuilder.Subjects(
            order, requirement, participants, assignments, fulfillments);
        if (subjects.Count == 0)
        {
            return Missing(request);
        }

        GuestCapabilityTokenIssue capability = capabilities.Issue();
        RegistrationAttempt attempt = RegistrationAttempt.Create(
            request.TenantId,
            request.EventId,
            request.OrderId,
            authority.Workflow.Id,
            requirement.Id,
            channel.Id,
            version.RegistrationFormId,
            version.Id,
            capability.Hash,
            null,
            null,
            now,
            expiresAt);
        await submissions.PersistAttemptAsync(attempt, cancellationToken);
        return new(true, attempt.Id, requirement.Id, channel.Id, version.RegistrationFormId, version.Id,
            attempt.ExpiresAt, NativeRegistrationAttemptContractBuilder.Form(version), subjects,
            NativeRegistrationAttemptContractBuilder.Progress(subjects), requirement.CanSkip, capability.RawToken);
    }

    private static NativeRegistrationAttemptResult Missing(LaunchNativeRegistrationAttemptCommand request) =>
        new(false, Guid.Empty, request.RequirementId, request.ChannelId, request.FormId, request.FormVersionId,
            default, null, [], null, false, null, "registration_requirement_not_found");
}

public sealed class SubmitNativeRegistrationAttemptCommandHandler(
    IRegistrationInventoryRepository inventory,
    IRegistrationSubmissionRepository submissions,
    IRegistrationFormAuthoringRepository forms,
    IRegistrationSensitiveValueProtector protector,
    IGuestCapabilityTokenService capabilities,
    ISender sender,
    TimeProvider timeProvider)
    : IRequestHandler<SubmitNativeRegistrationAttemptCommand, NativeRegistrationSubmissionResult>
{
    public async Task<NativeRegistrationSubmissionResult> Handle(
        SubmitNativeRegistrationAttemptCommand request,
        CancellationToken cancellationToken)
    {
        if (!(await new SubmitNativeRegistrationAttemptCommandValidator()
                .ValidateAsync(request, cancellationToken)).IsValid)
        {
            return Missing();
        }

        RegistrationAttempt? attempt = await submissions.GetAttemptAsync(
            request.TenantId, request.AttemptId, cancellationToken);
        RegistrationOrder? order = await inventory.GetOrderWithLinesAsync(
            request.OrderId, request.TenantId, cancellationToken);
        if (attempt is null || order is null ||
            order.RegistrationOrderStatusId != (int)RegistrationOrderStatusEnum.AwaitingRequirements ||
            order.RegistrationWorkflowVersionId != attempt.RegistrationWorkflowId ||
            attempt.EventId != request.EventId ||
            attempt.RegistrationOrderId != request.OrderId ||
            attempt.RegistrationRequirementId != request.RequirementId ||
            !capabilities.Matches(request.AttemptCapabilityToken, attempt.CapabilityTokenHash))
        {
            return Missing();
        }

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        Guid expectedStamp = attempt.ConcurrencyStamp;
        RegistrationSubmission submission;
        try
        {
            submission = attempt.SubmitNative(
                RegistrationEvidenceHash.Create(Hash(JsonSerializer.SerializeToUtf8Bytes(request.Answers))),
                now,
                RegistrationTransportIdempotencyHash.Create(Hash(Encoding.UTF8.GetBytes(request.IdempotencyKey!))));
        }
        catch (InvalidOperationException)
        {
            return Missing();
        }

        RegistrationSubmissionNormalizationDraft draft = await NormalizeRegistrationSubmissionCommandHandler.PrepareAsync(
            submission, request.Answers, submissions, forms, protector, timeProvider, cancellationToken);
        RegistrationSubmissionPersistenceResult persisted = await submissions.PersistAcceptedWithNormalizationAsync(
            attempt, submission, expectedStamp, draft.Answers, draft.ConsentRecords, draft.Issues, cancellationToken);
        if (persisted.Submission is null || persisted.Outcome == RegistrationSubmissionPersistenceOutcome.AttemptUnavailable)
        {
            return Missing();
        }

        if (persisted.Outcome == RegistrationSubmissionPersistenceOutcome.Inserted)
        {
            await NormalizeRegistrationSubmissionCommandHandler.RecordFulfillmentAsync(
                persisted.Submission, request.Answers, draft.IsValid, sender, cancellationToken);
        }
        return new(draft.IsValid, persisted.Submission.Id, draft.SafeIssues,
            draft.IsValid ? null : "registration_submission_invalid");
    }

    private static string Hash(byte[] value) => Convert.ToBase64String(SHA256.HashData(value));

    private static NativeRegistrationSubmissionResult Missing() =>
        new(false, Guid.Empty, [], "registration_attempt_not_found");
}
