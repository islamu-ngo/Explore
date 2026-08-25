// ABOUTME: Verifies retained Coop callbacks execute and settle through the specialized effect pointer.
// ABOUTME: Covers success ordering, poison quarantine, transient retry, tenant mismatch, and cancellation.

using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Features.RegistrationProviders.Commands;
using Explore.Application.Features.RegistrationSubmissions.Commands;
using Explore.Application.Responses;
using Explore.Application.Services.Webhooks;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Event.Application.UnitTests.Services.Webhooks;

public sealed class IncomingWebhookEffectProcessingServiceTests
{
    private readonly IIncomingWebhookEffectOutboxRepository _pointerRepository =
        Substitute.For<IIncomingWebhookEffectOutboxRepository>();
    private readonly IIncomingWebhookMessageRepository _messageRepository =
        Substitute.For<IIncomingWebhookMessageRepository>();
    private readonly IIncomingWebhookEffectReceiptRepository _receiptRepository =
        Substitute.For<IIncomingWebhookEffectReceiptRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [Test]
    public async Task ProcessAsync_CommandSuccess_CreatesReceiptBeforeCompletingPointer()
    {
        var setup = CreateClaim(CreatePayload());
        ConfigureRepositories(setup);
        _mediator.Send(Arg.Any<ProcessCoopDecisionCallbackCommand>(), Arg.Any<CancellationToken>())
            .Returns(BaseCommandResponse.Success(Guid.CreateVersion7()));

        var result = await CreateService(setup.Now).ProcessAsync(setup.Claim, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(IncomingWebhookClaimExecutionOutcome.Completed);
        await Assert.That(setup.Pointer.Status).IsEqualTo(OutboxMessageStatus.Completed);
        await _receiptRepository.Received(1).AddAsync(
            Arg.Is<IncomingWebhookEffectReceipt>(receipt =>
                receipt.IncomingWebhookMessageId == setup.Message.Id &&
                receipt.EffectKind == setup.Pointer.EffectKind),
            Arg.Any<CancellationToken>());
        await _pointerRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_StaleDispatcherReplay_DoesNotDispatchCommandOrSettlePointer()
    {
        var setup = CreateClaim(CreatePayload());
        _pointerRepository.GetActiveClaimAsync(
                setup.Claim,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns((IncomingWebhookEffectOutbox?)null);

        var result = await CreateService(setup.Now).ProcessAsync(
            setup.Claim,
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(IncomingWebhookClaimExecutionOutcome.LeaseLost);
        await _mediator.DidNotReceive().Send(
            Arg.Any<ProcessCoopDecisionCallbackCommand>(),
            Arg.Any<CancellationToken>());
        await _receiptRepository.DidNotReceive().AddAsync(
            Arg.Any<IncomingWebhookEffectReceipt>(),
            Arg.Any<CancellationToken>());
        await _pointerRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_InvalidJson_DeadLettersWithoutCommand()
    {
        var setup = CreateClaim(Encoding.UTF8.GetBytes("{invalid"));
        ConfigureRepositories(setup);

        var result = await CreateService(setup.Now).ProcessAsync(setup.Claim, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(IncomingWebhookClaimExecutionOutcome.Completed);
        await Assert.That(setup.Pointer.Status).IsEqualTo(OutboxMessageStatus.DeadLettered);
        await Assert.That(setup.Pointer.FailureCategory).IsEqualTo("coop_effect_payload_invalid");
        await _mediator.DidNotReceive().Send(
            Arg.Any<ProcessCoopDecisionCallbackCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_TransientFailure_SchedulesBoundedRetry()
    {
        var setup = CreateClaim(CreatePayload());
        ConfigureRepositories(setup);
        _mediator.Send(Arg.Any<ProcessCoopDecisionCallbackCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<BaseCommandResponse<Guid>>>(_ => throw new TimeoutException("canary-sensitive-text"));

        var result = await CreateService(setup.Now).ProcessAsync(setup.Claim, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(IncomingWebhookClaimExecutionOutcome.Completed);
        await Assert.That(setup.Pointer.Status).IsEqualTo(OutboxMessageStatus.Failed);
        await Assert.That(setup.Pointer.FailureCategory).IsEqualTo("coop_effect_transient_failure");
        await Assert.That(setup.Pointer.SafeDetail).DoesNotContain("canary-sensitive-text");
    }

    [Test]
    public async Task ProcessAsync_RegistrationMalformedAttemptId_DeadLettersWithoutRetry()
    {
        var setup = CreateClaim(
            Encoding.UTF8.GetBytes("""
                {
                  "attemptId": "not-a-guid",
                  "providerSubmissionId": "provider-submission-1",
                  "providerResponseRevision": "revision-1"
                }
                """),
            effectKind: ProcessProviderSubmissionEffectCommandHandler.StableEffectKind,
            provider: "registration-provider",
            providerDecisionId: $"{Guid.CreateVersion7():N}:provider-submission-1");
        ConfigureRepositories(setup);
        _mediator.Send(Arg.Any<ProcessProviderSubmissionEffectCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<ProviderSubmissionEffectResult>>(_ => throw new FormatException("canary-sensitive-text"));

        var result = await CreateService(setup.Now).ProcessAsync(setup.Claim, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(IncomingWebhookClaimExecutionOutcome.Completed);
        await Assert.That(setup.Pointer.Status).IsEqualTo(OutboxMessageStatus.DeadLettered);
        await Assert.That(setup.Pointer.FailureCategory).IsEqualTo("malformed_evidence");
        await Assert.That(setup.Pointer.SafeDetail).DoesNotContain("canary-sensitive-text");
        await Assert.That(setup.Pointer.NextAttemptAt).IsNull();
    }

    [Test]
    public async Task ProcessAsync_ManualImportEffect_ParksWithoutSubmissionCommandOrMalformedEvidence()
    {
        var bindingId = Guid.CreateVersion7();
        var setup = CreateClaim(
            Encoding.UTF8.GetBytes($$"""
                {
                  "bindingId": "{{bindingId}}",
                  "storageReference": "storage:object/123",
                  "sourceReference": "operator-import-1"
                }
                """),
            effectKind: QueueManualRegistrationProviderImportCommandHandler.ManualImportEffectKind,
            provider: "registration-provider",
            providerDecisionId: $"{bindingId:N}:manual:abcdef");
        ConfigureRepositories(setup);

        var result = await CreateService(setup.Now).ProcessAsync(setup.Claim, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(IncomingWebhookClaimExecutionOutcome.Completed);
        await Assert.That(setup.Pointer.Status).IsEqualTo(OutboxMessageStatus.DeadLettered);
        await Assert.That(setup.Pointer.FailureCategory).IsEqualTo("manual_import_pending");
        await Assert.That(setup.Pointer.FailureCategory).IsNotEqualTo("malformed_evidence");
        await _mediator.DidNotReceive().Send(
            Arg.Any<ProcessProviderSubmissionEffectCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_PayloadTenantMismatch_DeadLetters()
    {
        var setup = CreateClaim(CreatePayload(tenantId: Guid.CreateVersion7()), Guid.CreateVersion7());
        ConfigureRepositories(setup);

        var result = await CreateService(setup.Now).ProcessAsync(setup.Claim, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(IncomingWebhookClaimExecutionOutcome.Completed);
        await Assert.That(setup.Pointer.Status).IsEqualTo(OutboxMessageStatus.DeadLettered);
        await Assert.That(setup.Pointer.FailureCategory).IsEqualTo("coop_effect_tenant_mismatch");
        await _mediator.DidNotReceive().Send(
            Arg.Any<ProcessCoopDecisionCallbackCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_CancelledCommand_LeavesClaimForLeaseRecovery()
    {
        var setup = CreateClaim(CreatePayload());
        ConfigureRepositories(setup);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        _mediator.Send(Arg.Any<ProcessCoopDecisionCallbackCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<BaseCommandResponse<Guid>>>(_ => throw new OperationCanceledException(cancellation.Token));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateService(setup.Now).ProcessAsync(setup.Claim, cancellation.Token));

        await Assert.That(setup.Pointer.Status).IsEqualTo(OutboxMessageStatus.Processing);
        await _pointerRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private IncomingWebhookEffectProcessingService CreateService(DateTime now) => new(
        _pointerRepository,
        _messageRepository,
        _receiptRepository,
        new ImmediateUnitOfWork(),
        _mediator,
        Options.Create(new IncomingWebhookProcessingSettings
        {
            MaxAttempts = 3,
            InitialRetryDelaySeconds = 10,
            MaxRetryDelaySeconds = 60
        }),
        new FixedTimeProvider(now));

    private void ConfigureRepositories(Setup setup)
    {
        _pointerRepository.GetActiveClaimAsync(setup.Claim, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(setup.Pointer);
        _pointerRepository.GetByTenantAndIdForUpdateAsync(
                setup.Pointer.TenantId,
                setup.Pointer.Id,
                Arg.Any<CancellationToken>())
            .Returns(setup.Pointer);
        _messageRepository.GetByTenantAndIdForUpdateAsync(
                setup.Pointer.TenantId,
                setup.Message.Id,
                Arg.Any<CancellationToken>())
            .Returns(setup.Message);
    }

    private static Setup CreateClaim(
        byte[] payload,
        Guid? pointerTenantId = null,
        string provider = "coop",
        string providerDecisionId = "provider-decision-1",
        string? effectKind = null)
    {
        var now = new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
        var tenantId = pointerTenantId ?? ReadTenantId(payload) ?? Guid.CreateVersion7();
        string kind = effectKind ?? CoopDecisionIncomingWebhookHandler.StableEffectKind;
        var message = IncomingWebhookMessage.CreateVerified(
            tenantId,
            provider,
            providerDecisionId,
            providerDecisionId,
            kind,
            payload,
            "sha256:" + Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant(),
            "application/json",
            "utf-8",
            null,
            now.AddMinutes(-2),
            now.AddMinutes(-2),
            now.AddDays(14),
            "webhook-retention-v1",
            now.AddDays(30),
            now.AddDays(90),
            now.AddDays(14),
            now.AddDays(30));
        var pointer = IncomingWebhookEffectOutbox.CreatePending(
            tenantId,
            message.Id,
            provider,
            providerDecisionId,
            kind,
            message.PayloadHash,
            now.AddMinutes(-1));
        var leaseToken = Guid.CreateVersion7();
        pointer.Claim("worker", leaseToken, now.AddMinutes(5), now.AddSeconds(-1));
        var claim = new IncomingWebhookEffectClaim(
            pointer.Id,
            pointer.TenantId,
            leaseToken,
            pointer.ProcessingFence,
            pointer.ProcessingGeneration);
        return new Setup(pointer, message, claim, now);
    }

    private static byte[] CreatePayload(Guid? tenantId = null)
    {
        var tenant = tenantId ?? Guid.CreateVersion7();
        return Encoding.UTF8.GetBytes($$"""
            {
              "tenantId": "{{tenant}}",
              "eventId": "{{Guid.CreateVersion7()}}",
              "reportId": "{{Guid.CreateVersion7()}}",
              "caseId": "{{Guid.CreateVersion7()}}",
              "providerDecisionId": "provider-decision-1",
              "action": { "id": "allow" }
            }
            """);
    }

    private static Guid? ReadTenantId(byte[] payload)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(payload);
            return document.RootElement.TryGetProperty("tenantId", out var value) &&
                   Guid.TryParse(value.GetString(), out var tenantId)
                ? tenantId
                : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private sealed record Setup(
        IncomingWebhookEffectOutbox Pointer,
        IncomingWebhookMessage Message,
        IncomingWebhookEffectClaim Claim,
        DateTime Now);

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) =>
            operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
            operation(ct);

        public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
            ExecuteInTransactionAsync(operation, ct);
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
