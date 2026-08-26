// ABOUTME: Defines exact MediatR contracts for admission recovery and current-account ticket reads.
// ABOUTME: Carries only email, capability, or ticket identity at the transport-to-application boundary.

using Explore.Application.DTOs.AdmissionTickets;
using MediatR;

namespace Explore.Application.Features.AdmissionTickets.Requests.Commands
{
    public sealed record RequestAdmissionTicketRecoveryCommand(string Email) :
        IRequest<AdmissionTicketRecoveryRequestResultDto>;

    public sealed record RedeemAdmissionTicketRecoveryCommand(string Capability) :
        IRequest<AdmissionTicketRecoveryConsumeResultDto>;

    public sealed record ReissueCurrentAdmissionTicketQrCommand(Guid TicketId) :
        IRequest<AdmissionTicketQrDeliveryDto>;

    public sealed record ReissueCurrentAdmissionTicketPrintCommand(Guid TicketId) :
        IRequest<AdmissionTicketPrintDeliveryDto>;
}

namespace Explore.Application.Features.AdmissionTickets.Requests.Queries
{
    public sealed record GetCurrentAdmissionTicketsQuery :
        IRequest<IReadOnlyList<AdmissionTicketDto>>;

    public sealed record GetCurrentAdmissionTicketQuery(Guid TicketId) :
        IRequest<AdmissionTicketDto>;

}
