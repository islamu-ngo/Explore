// ABOUTME: Loads the exact confirmed free-order authority graph and atomically stages ticket delivery intent rows.
// ABOUTME: Replay resolves persisted aggregate identities without regenerating credential material.

using Explore.Application.Contracts.Admissions;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class AdmissionIssuanceRepository(ExploreDbContext dbContext) : IAdmissionIssuanceRepository
{
    public async Task<AdmissionIssuanceContext?> LoadAsync(
        AdmissionIssuanceRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<RegistrationFinalizationEffect> effects = dbContext.RegistrationFinalizationEffects;
        if (dbContext.Database.IsNpgsql() && dbContext.Database.CurrentTransaction is not null)
        {
            effects = dbContext.RegistrationFinalizationEffects.FromSqlInterpolated(
                $"SELECT * FROM registration_finalization_effects WHERE tenant_id = {request.TenantId} AND id = {request.FinalizationEffectId} FOR UPDATE");
        }
        RegistrationFinalizationEffect? effect = await effects.SingleOrDefaultAsync(value =>
                value.TenantId == request.TenantId &&
                value.Id == request.FinalizationEffectId &&
                value.RegistrationOrderId == request.RegistrationOrderId,
                cancellationToken);
        if (effect is null)
        {
            return null;
        }

        RegistrationOrder? order = await dbContext.RegistrationOrders
            .Include(value => value.Pii)
            .Include(value => value.Lines)
                .ThenInclude(value => value.Assignments)
            .Include(value => value.Participants)
            .SingleOrDefaultAsync(value => value.TenantId == request.TenantId && value.Id == request.RegistrationOrderId,
                cancellationToken);
        if (order is null)
        {
            return null;
        }

        EventTicketCatalogVersion catalog = await dbContext.EventTicketCatalogVersions
            .Include(value => value.TicketTypes)
            .SingleAsync(value => value.TenantId == request.TenantId && value.Id == order.TicketCatalogVersionId,
                cancellationToken);
        List<AdmissionTicket> existing = await dbContext.AdmissionTickets
            .Include(value => value.Credentials)
            .Where(value => value.TenantId == request.TenantId && value.RegistrationOrderId == request.RegistrationOrderId)
            .ToListAsync(cancellationToken);
        List<AdmissionDeliveryIntent> existingDeliveryIntents = await dbContext.AdmissionDeliveryIntents
            .Where(value => value.TenantId == request.TenantId && value.FinalizationEffectId == effect.Id)
            .ToListAsync(cancellationToken);
        Dictionary<Guid, RegistrationParticipant> participants = order.Participants.ToDictionary(value => value.Id);
        Dictionary<Guid, EventTicketType> ticketTypes = catalog.TicketTypes.ToDictionary(value => value.Id);
        AdmissionAssignmentFact[] assignments = order.Lines
            .SelectMany(line => line.Assignments.Select(assignment => new AdmissionAssignmentFact(
                line,
                assignment,
                participants[assignment.ParticipantId.GetValueOrDefault()],
                ticketTypes[line.TicketTypeId],
                line.PostDiscountLineSubtotalMinorSnapshot / line.Quantity,
                line.PostDiscountLineSubtotalMinorSnapshot,
                true)))
            .ToArray();
        bool confirmed = order.RegistrationOrderStatusId == (int)RegistrationOrderStatusEnum.Confirmed &&
            order.ConfirmedAt is not null;
        string? deliveryAddress = order.Pii?.Email;
        if (string.IsNullOrWhiteSpace(deliveryAddress) && order.AccountUserId.HasValue)
        {
            deliveryAddress = await dbContext.UserPii
                .Where(value => value.UserId == order.AccountUserId.Value)
                .Select(value => value.Email)
                .SingleOrDefaultAsync(cancellationToken);
        }
        return new AdmissionIssuanceContext(
            order.TenantId,
            order.EventId,
            order.Id,
            effect.Id,
            order.TotalDueMinorSnapshot == 0 ? "ConfirmedFreeOrder" : "Paid",
            false,
            confirmed,
            order,
            catalog,
            assignments,
            existing,
            deliveryAddress ?? string.Empty,
            existingDeliveryIntents);
    }

    public Task<AdmissionIssuanceContext?> ReloadCommittedAsync(
        AdmissionIssuanceRequest request,
        CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        return LoadAsync(request, cancellationToken);
    }

    public async Task<AdmissionIssuanceResult> IssueAndScheduleDeliveryAsync(
        AdmissionIssuancePersistenceRequest request,
        CancellationToken cancellationToken)
    {
        List<AdmissionTicket> existing = await dbContext.AdmissionTickets
            .Include(ticket => ticket.Credentials)
            .Where(ticket => ticket.TenantId == request.TenantId &&
                request.Tickets.Select(candidate => candidate.RegistrationTicketAssignmentId)
                    .Contains(ticket.RegistrationTicketAssignmentId))
            .ToListAsync(cancellationToken);
        HashSet<Guid> existingAssignments = existing
            .Select(ticket => ticket.RegistrationTicketAssignmentId)
            .ToHashSet();
        AdmissionTicket[] issued = request.Tickets
            .Where(ticket => !existingAssignments.Contains(ticket.RegistrationTicketAssignmentId))
            .ToArray();
        AdmissionDeliveryIntent[] intents = request.DeliveryIntents
            .Where(intent => issued.Any(ticket => ticket.Id == intent.AdmissionTicketId))
            .ToArray();
        if (issued.Length > 0)
        {
            await dbContext.AdmissionTickets.AddRangeAsync(issued, cancellationToken);
            await dbContext.AdmissionDeliveryIntents.AddRangeAsync(intents, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        AdmissionTicket[] all = existing.Concat(issued).ToArray();
        return new AdmissionIssuanceResult(
            issued.Length == 0 ? AdmissionIssuanceOutcome.AlreadyIssued : AdmissionIssuanceOutcome.Issued,
            issued.Select(ticket => ticket.Id).ToArray(),
            existing.Select(ticket => ticket.Id).ToArray(),
            all,
            intents.Select(intent => intent.Id).ToArray());
    }
}
