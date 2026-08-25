// ABOUTME: Maps authenticated-account admission ticket entities to transport DTOs.
// ABOUTME: Resolves tenant and account authority from server contexts rather than request data.

using Explore.Application.Contracts.Admissions;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.AdmissionTickets;
using Explore.Application.Features.AdmissionTickets.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.AdmissionTickets.Handlers;

public sealed class GetCurrentAdmissionTicketsQueryHandler(
    IAdmissionTicketAccountRepository repository,
    ITenantContext tenantContext,
    IUserContext userContext) :
    IRequestHandler<GetCurrentAdmissionTicketsQuery, IReadOnlyList<AdmissionTicketDto>>
{
    public async Task<IReadOnlyList<AdmissionTicketDto>> Handle(
        GetCurrentAdmissionTicketsQuery request,
        CancellationToken cancellationToken)
    {
        Guid accountUserId = userContext.GetRequiredUserId();
        IReadOnlyList<AdmissionTicket> tickets = await repository.ListCurrentAsync(
            tenantContext.TenantId,
            accountUserId,
            cancellationToken);
        return tickets.Select(Map).ToArray();
    }

    internal static AdmissionTicketDto Map(AdmissionTicket ticket) =>
        new(
            ticket.Id,
            ticket.Id,
            ticket.EventId,
            ((AdmissionTicketStatusEnum)ticket.AdmissionTicketStatusId)
                .ToString()
                .ToUpperInvariant(),
            ticket.DisplayReference);
}
