// ABOUTME: Maps public recovery commands to uniform request and one-time consume orchestration.
// ABOUTME: Derives tenant authority from server context and never accepts it from transport input.

using Explore.Application.Contracts.Admissions;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.AdmissionTickets;
using Explore.Application.Features.AdmissionTickets.Requests.Commands;
using Explore.Application.Services.Registration;
using MediatR;

namespace Explore.Application.Features.AdmissionTickets.Handlers;

public sealed class RequestAdmissionTicketRecoveryCommandHandler(
    AdmissionRecoveryService recoveryService,
    ITenantContext tenantContext) :
    IRequestHandler<RequestAdmissionTicketRecoveryCommand, AdmissionTicketRecoveryRequestResult>
{
    public async Task<AdmissionTicketRecoveryRequestResult> Handle(
        RequestAdmissionTicketRecoveryCommand request,
        CancellationToken cancellationToken)
    {
        _ = await recoveryService.RequestAsync(
            new AdmissionRecoveryRequest(
                tenantContext.TenantId,
                request.Email,
                AdmissionRecoveryPurpose.TicketRecovery),
            cancellationToken);
        return new AdmissionTicketRecoveryRequestResult(true, true);
    }
}

public sealed class ConsumeAdmissionTicketRecoveryCommandHandler(
    AdmissionRecoveryService recoveryService,
    ITenantContext tenantContext) :
    IRequestHandler<ConsumeAdmissionTicketRecoveryCommand, AdmissionTicketRecoveryConsumeResult>
{
    public async Task<AdmissionTicketRecoveryConsumeResult> Handle(
        ConsumeAdmissionTicketRecoveryCommand request,
        CancellationToken cancellationToken)
    {
        AdmissionRecoveryConsumeResult result = await recoveryService.ConsumeByCapabilityAsync(
            tenantContext.TenantId,
            request.Capability,
            cancellationToken);
        AdmissionRecoveryTicketDocument? document = result.Document;
        if (result.Outcome != AdmissionRecoveryConsumeOutcome.Consumed || document is null)
        {
            return null!;
        }

        return new AdmissionTicketRecoveryConsumeResult(
            result.RecoveryRecordId,
            new AdmissionTicketRecoveryDeliveryDto(
                document.TicketId,
                document.TicketId,
                document.EventId,
                document.StatusCode,
                document.DisplayReference,
                document.ManualCode,
                document.ManualCodeClassificationCode,
                document.QrRepresentation,
                document.PrintModel));
    }
}
