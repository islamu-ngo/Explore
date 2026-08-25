// ABOUTME: Defines entity-returning admission ticket reads scoped to authenticated account authority.
// ABOUTME: Accepts only server-resolved tenant and user IDs; email and display references never authorize.

using Explore.Domain;

namespace Explore.Application.Contracts.Admissions;

public interface IAdmissionTicketAccountRepository
{
    Task<IReadOnlyList<AdmissionTicket>> ListCurrentAsync(
        Guid tenantId,
        Guid accountUserId,
        CancellationToken cancellationToken);

    Task<AdmissionTicket?> GetCurrentAsync(
        Guid tenantId,
        Guid accountUserId,
        Guid admissionTicketId,
        CancellationToken cancellationToken);
}
