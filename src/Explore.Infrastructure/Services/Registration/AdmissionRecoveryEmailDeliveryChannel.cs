// ABOUTME: Hands recovery capabilities to the verified side-channel email transport.
// ABOUTME: Uses the durable delivery-intent ID as the provider idempotency lineage.

using Explore.Application.Contracts.Admissions;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;

namespace Explore.Infrastructure.Services.Registration;

public sealed class AdmissionRecoveryEmailDeliveryChannel(IEmailService emailService) :
    IAdmissionRecoveryDirectDeliveryChannel
{
    public async Task<AdmissionRecoveryDirectDeliveryResult> DeliverAsync(
        AdmissionRecoveryDirectDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty || request.DeliveryIntentId == Guid.Empty ||
            request.AdmissionTicketId == Guid.Empty || request.RecoveryRequestId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.RecipientAddress) ||
            string.IsNullOrWhiteSpace(request.Capability))
        {
            return new AdmissionRecoveryDirectDeliveryResult(
                AdmissionRecoveryDirectDeliveryOutcome.Ambiguous);
        }

        string idempotencyKey = request.DeliveryIntentId.ToString("N");
        EmailResult result = await emailService.SendAsync(new EmailMessage
        {
            To = request.RecipientAddress,
            Subject = "Recover your admission ticket",
            PlainTextBody =
                $"Recovery request: {request.RecoveryRequestId:N}\n" +
                $"Recovery capability: {request.Capability}",
            CustomHeaders =
            {
                ["X-Admission-Recovery-Idempotency-Key"] = idempotencyKey
            }
        }, cancellationToken);
        return result.Success
            ? new AdmissionRecoveryDirectDeliveryResult(
                AdmissionRecoveryDirectDeliveryOutcome.Accepted,
                $"smtp:{idempotencyKey}")
            : new AdmissionRecoveryDirectDeliveryResult(
                AdmissionRecoveryDirectDeliveryOutcome.Ambiguous);
    }
}
