// ABOUTME: Loads order-owned ticket assignments with participant entities for final admission materialization.
// ABOUTME: Stages only tenant-safe PII-free participant entities for the shared transactional unit of work.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class RegistrationParticipantRepository(ExploreDbContext dbContext) : IRegistrationParticipantRepository
{
    public Task<RegistrationParticipant?> GetParticipantForUpdateAsync(
        Guid participantId,
        Guid registrationOrderId,
        Guid tenantId,
        CancellationToken cancellationToken) =>
        dbContext.RegistrationParticipants
            .Include(participant => participant.Pii)
            .Include(participant => participant.GuardianParticipant)
            .FirstOrDefaultAsync(participant => participant.Id == participantId &&
                participant.RegistrationOrderId == registrationOrderId && participant.TenantId == tenantId, cancellationToken);

    public async Task<IReadOnlyList<RegistrationParticipant>> GetParticipantsByOrderAsync(
        Guid registrationOrderId,
        Guid tenantId,
        CancellationToken cancellationToken) =>
        await dbContext.RegistrationParticipants
            .AsNoTracking()
            .Include(participant => participant.Pii)
            .Where(participant => participant.RegistrationOrderId == registrationOrderId && participant.TenantId == tenantId)
            .OrderBy(participant => participant.CreatedAt)
            .ThenBy(participant => participant.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RegistrationTicketAssignment>> GetAssignmentsWithParticipantsByOrderAsync(
        Guid registrationOrderId,
        Guid tenantId,
        CancellationToken cancellationToken) =>
        await dbContext.RegistrationTicketAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Participant)
            .Where(assignment => assignment.RegistrationOrderId == registrationOrderId && assignment.TenantId == tenantId)
            .OrderBy(assignment => assignment.RegistrationOrderLineId)
            .ThenBy(assignment => assignment.Ordinal)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RegistrationTicketAssignment>> GetAssignmentsForUpdateByOrderAsync(
        Guid registrationOrderId,
        Guid tenantId,
        CancellationToken cancellationToken) =>
        await dbContext.RegistrationTicketAssignments
            .Include(assignment => assignment.Participant)
                .ThenInclude(participant => participant!.Pii)
            .Where(assignment => assignment.RegistrationOrderId == registrationOrderId && assignment.TenantId == tenantId)
            .OrderBy(assignment => assignment.RegistrationOrderLineId)
            .ThenBy(assignment => assignment.Ordinal)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EventRegistration>> GetAdmissionsForUpdateAsync(
        Guid registrationOrderId,
        Guid registrationOrderLineId,
        int ordinal,
        Guid tenantId,
        CancellationToken cancellationToken) =>
        await dbContext.EventRegistrations
            .Where(registration => registration.RegistrationOrderId == registrationOrderId &&
                registration.RegistrationOrderLineId == registrationOrderLineId &&
                registration.EntitlementOrdinal == ordinal && registration.TenantId == tenantId)
            .OrderBy(registration => registration.EventSessionId)
            .ToListAsync(cancellationToken);

    public Task AddParticipantAsync(RegistrationParticipant participant, CancellationToken cancellationToken) =>
        dbContext.RegistrationParticipants.AddAsync(participant, cancellationToken).AsTask();

    public async Task AddAssignmentsAsync(
        IReadOnlyCollection<RegistrationTicketAssignment> assignments,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        await dbContext.RegistrationTicketAssignments.AddRangeAsync(assignments, cancellationToken);
    }

    public async Task AddParticipantsAsync(
        IReadOnlyCollection<RegistrationParticipant> participants,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(participants);
        if (participants.Count == 0)
        {
            return;
        }

        Guid tenantId = participants.First().TenantId;
        Guid orderId = participants.First().RegistrationOrderId;
        if (participants.Any(participant => participant.Id == Guid.Empty ||
                participant.TenantId != tenantId ||
                participant.RegistrationOrderId != orderId ||
                participant.ParticipantTypeId != (int)ParticipantTypeEnum.Unnamed ||
                participant.LinkedUserId is not null ||
                participant.Pii is not null))
        {
            throw new ArgumentException("Placeholder participants must be PII-free unnamed participants from one order.", nameof(participants));
        }

        await dbContext.RegistrationParticipants.AddRangeAsync(participants, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
