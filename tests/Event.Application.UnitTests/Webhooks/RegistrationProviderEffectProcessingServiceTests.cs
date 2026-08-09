// ABOUTME: Proves registration-provider effect pointers bypass Coop-only validation and reach MediatR.
// ABOUTME: Covers the provider-aware branch in the shared incoming-webhook effect processor.

using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.RegistrationSubmissions.Commands;
using Explore.Application.Services.Webhooks;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Options;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Webhooks;

public sealed class RegistrationProviderEffectProcessingServiceTests
{
    [Test]
    public async Task RegistrationProviderSubmissionEffect_ReachesMediatRWithoutCoopEnvelopeValidation()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid bindingId = Guid.CreateVersion7();
        DateTime now = new(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        byte[] payload = Encoding.UTF8.GetBytes(
            "{\"attemptId\":\"" + Guid.CreateVersion7().ToString("D") + "\",\"providerSubmissionId\":\"s1\",\"providerResponseRevision\":\"r1\"}");
        string hash = "sha256:" + Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        IncomingWebhookMessage message = IncomingWebhookMessage.CreateVerified(
            tenantId, "registration-provider", $"{bindingId:N}:s1", $"{bindingId:N}:s1",
            ProcessProviderSubmissionEffectCommandHandler.StableEffectKind, payload, hash, "application/json", "utf-8",
            "{\"X-Registration-Callback-Provider\":\"external-form\",\"X-Registration-Verification-Receipt\":\"receipt:v1:test\"}",
            now, now, now.AddDays(1), "test", now.AddDays(1), now.AddDays(1), now.AddDays(1), now.AddDays(1));
        IncomingWebhookEffectOutbox pointer = IncomingWebhookEffectOutbox.CreatePending(
            tenantId, message.Id, "registration-provider", $"{bindingId:N}:s1",
            ProcessProviderSubmissionEffectCommandHandler.StableEffectKind, hash, now);
        pointer.Claim("worker", Guid.CreateVersion7(), now.AddMinutes(5), now);
        IncomingWebhookEffectClaim claim = new(pointer.Id, tenantId, pointer.ProcessingLeaseToken!.Value, pointer.ProcessingFence, pointer.ProcessingGeneration);
        IIncomingWebhookEffectOutboxRepository pointers = Substitute.For<IIncomingWebhookEffectOutboxRepository>();
        IIncomingWebhookMessageRepository messages = Substitute.For<IIncomingWebhookMessageRepository>();
        IIncomingWebhookEffectReceiptRepository receipts = Substitute.For<IIncomingWebhookEffectReceiptRepository>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        IMediator mediator = Substitute.For<IMediator>();
        pointers.GetActiveClaimAsync(claim, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(pointer);
        pointers.GetByTenantAndIdForUpdateAsync(tenantId, pointer.Id, Arg.Any<CancellationToken>()).Returns(pointer);
        messages.GetByTenantAndIdForUpdateAsync(tenantId, message.Id, Arg.Any<CancellationToken>()).Returns(message);
        unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<IncomingWebhookClaimExecutionResult>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<IncomingWebhookClaimExecutionResult>>>()(CancellationToken.None));
        mediator.Send(Arg.Any<ProcessProviderSubmissionEffectCommand>(), Arg.Any<CancellationToken>())
            .Returns(ProviderSubmissionEffectResult.Completed());
        var service = new IncomingWebhookEffectProcessingService(
            pointers, messages, receipts, unitOfWork, mediator,
            Options.Create(new IncomingWebhookProcessingSettings()), new FixedTimeProvider(now.AddSeconds(1)));

        IncomingWebhookClaimExecutionResult result = await service.ProcessAsync(claim, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(IncomingWebhookClaimExecutionOutcome.Completed);
        await mediator.Received(1).Send(Arg.Any<ProcessProviderSubmissionEffectCommand>(), Arg.Any<CancellationToken>());
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
