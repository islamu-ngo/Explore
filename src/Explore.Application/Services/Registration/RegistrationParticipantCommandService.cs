// ABOUTME: Applies participant and concrete ticket-unit mutations under one order-locked transaction.
// ABOUTME: Supports pre-confirm group booking and post-confirm optional or deferred admission amendments.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Registration;

namespace Explore.Application.Services.Registration;

public sealed class RegistrationParticipantCommandService(
    IRegistrationInventoryRepository inventory,
    IRegistrationParticipantRepository participants,
    IEventTicketCatalogRepository catalogs,
    IEventSessionRepository eventSessions,
    ITenantContext tenant,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<BaseCommandResponse<Guid>> AddAsync(
        int participantTypeId,
        Guid orderId,
        Guid? guardianParticipantId,
        ParticipantDetailsDto details,
        CancellationToken cancellationToken)
    {
        Guid participantId = Guid.CreateVersion7();
        Guid participantConcurrency = Guid.CreateVersion7();
        Guid orderConcurrency = Guid.CreateVersion7();
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                RegistrationOrder? order = await inventory.GetOrderForUpdateWithLinesAsync(orderId, tenant.TenantId, token);
                if (order is null)
                {
                    return Missing(orderId);
                }

                ParticipantTypeEnum participantType = NormalizeParticipantType(participantTypeId);
                RegistrationParticipant? guardian = await GetGuardianAsync(guardianParticipantId, order, token);
                EnsureParticipantDetails(participantType, details, required: participantType is ParticipantTypeEnum.Child or ParticipantTypeEnum.Dependent);
                RegistrationParticipant participant = RegistrationParticipant.Create(
                    participantId, order.TenantId, order.Id, null, participantType, guardian);
                participant.ConcurrencyStamp = participantConcurrency;
                if (HasDetails(details))
                {
                    participant.SetPii(RegistrationParticipantPii.Create(
                        participant.Id, order.TenantId, details.DisplayName, details.Email, details.Phone));
                }

                order.BumpConcurrency(orderConcurrency);
                await participants.AddParticipantAsync(participant, token);
                await participants.SaveChangesAsync(token);
                return Success(participant.Id, "Registration participant added.");
            }, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Invalid(orderId, exception.Message);
        }
    }

    public async Task<BaseCommandResponse<Guid>> UpdateAsync(
        Guid orderId,
        Guid participantId,
        int participantTypeId,
        Guid? guardianParticipantId,
        ParticipantDetailsDto details,
        CancellationToken cancellationToken)
    {
        Guid participantConcurrency = Guid.CreateVersion7();
        Guid orderConcurrency = Guid.CreateVersion7();
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                RegistrationOrder? order = await inventory.GetOrderForUpdateWithLinesAsync(orderId, tenant.TenantId, token);
                RegistrationParticipant? participant = order is null
                    ? null
                    : await participants.GetParticipantForUpdateAsync(participantId, orderId, tenant.TenantId, token);
                if (order is null || participant is null)
                {
                    return Missing(participantId);
                }

                ParticipantTypeEnum participantType = NormalizeParticipantType(participantTypeId);
                RegistrationParticipant? guardian = await GetGuardianAsync(guardianParticipantId, order, token);
                EnsureParticipantDetails(participantType, details, required: participantType is ParticipantTypeEnum.Child or ParticipantTypeEnum.Dependent);
                participant.Update(participantType, guardian, participantConcurrency);
                if (participant.Pii is null)
                {
                    if (HasDetails(details))
                    {
                        participant.SetPii(RegistrationParticipantPii.Create(
                            participant.Id, order.TenantId, details.DisplayName, details.Email, details.Phone));
                    }
                }
                else
                {
                    participant.Pii.Update(details.DisplayName, details.Email, details.Phone);
                }

                order.BumpConcurrency(orderConcurrency);
                await participants.SaveChangesAsync(token);
                return Success(participant.Id, "Registration participant updated.");
            }, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Invalid(orderId, exception.Message);
        }
    }

    public Task<BaseCommandResponse<Guid>> AssignAsync(
        Guid orderId,
        IReadOnlyCollection<TicketParticipantAssignmentInputDto> requestedAssignments,
        CancellationToken cancellationToken) => MutateAssignmentsAsync(
        orderId, requestedAssignments, [], null, cancellationToken);

    public Task<BaseCommandResponse<Guid>> DeferAsync(
        Guid orderId,
        IReadOnlyCollection<TicketDeferralInputDto> requestedDeferrals,
        DateTime deadline,
        CancellationToken cancellationToken) => MutateAssignmentsAsync(
        orderId, [], requestedDeferrals, deadline, cancellationToken);

    private async Task<BaseCommandResponse<Guid>> MutateAssignmentsAsync(
        Guid orderId,
        IReadOnlyCollection<TicketParticipantAssignmentInputDto> requestedAssignments,
        IReadOnlyCollection<TicketDeferralInputDto> requestedDeferrals,
        DateTime? deadline,
        CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (deadline.HasValue && (deadline.Value.Kind != DateTimeKind.Utc || deadline <= now))
        {
            return Invalid(orderId, "Assignment deadline must be a future UTC timestamp.");
        }

        var requestedKeys = requestedAssignments.Select(item => (item.RegistrationOrderLineId, item.Ordinal))
            .Concat(requestedDeferrals.Select(item => (item.RegistrationOrderLineId, item.Ordinal))).ToArray();
        if (requestedKeys.Length == 0 || requestedKeys.Distinct().Count() != requestedKeys.Length)
        {
            return Invalid(orderId, "Ticket assignment payload contains duplicate or missing ordinals.");
        }

        if (requestedAssignments.Select(item => item.ParticipantId).Distinct().Count() != requestedAssignments.Count)
        {
            return Invalid(orderId, "A participant cannot occupy more than one ticket ordinal in the same order.");
        }

        RegistrationOrder? initialOrder = await inventory.GetOrderWithLinesAsync(orderId, tenant.TenantId, cancellationToken);
        if (initialOrder is null)
        {
            return Missing(orderId);
        }

        EventTicketCatalogVersion? initialCatalog = await catalogs.GetOrderCatalogAsync(
            initialOrder.TicketCatalogVersionId, initialOrder.EventId, tenant.TenantId, cancellationToken);
        if (initialCatalog is null)
        {
            return Missing(orderId);
        }

        List<EventSession> sessions = await eventSessions.GetSessionsByEvent(initialOrder.EventId);
        Dictionary<(Guid RegistrationOrderLineId, int Ordinal), Guid[]> admissionIds;
        try
        {
            admissionIds = requestedAssignments.ToDictionary(
                item => (item.RegistrationOrderLineId, item.Ordinal),
                item => CreateAdmissionIds(initialOrder, initialCatalog, item.RegistrationOrderLineId, sessions));
        }
        catch (InvalidOperationException)
        {
            return Invalid(orderId, "Ticket assignment line does not belong to this order.");
        }
        Guid[] assignmentIds = Enumerable.Range(0, requestedKeys.Length).Select(_ => Guid.CreateVersion7()).ToArray();
        Guid[] assignmentConcurrency = Enumerable.Range(0, requestedKeys.Length).Select(_ => Guid.CreateVersion7()).ToArray();
        Guid[] admissionConcurrency = Enumerable.Range(0, admissionIds.Values.Sum(ids => ids.Length)).Select(_ => Guid.CreateVersion7()).ToArray();
        Guid orderConcurrency = Guid.CreateVersion7();

        try
        {
            return await unitOfWork.ExecuteSerializableAsync(async token =>
            {
                RegistrationOrder? order = await inventory.GetOrderForUpdateWithLinesAsync(orderId, tenant.TenantId, token);
                if (order is null || order.ConcurrencyStamp != initialOrder.ConcurrencyStamp)
                {
                    return Conflict(orderId);
                }

                EventTicketCatalogVersion? catalog = await catalogs.GetOrderCatalogAsync(
                    order.TicketCatalogVersionId, order.EventId, tenant.TenantId, token);
                if (catalog is null)
                {
                    return Missing(orderId);
                }

                var ticketTypes = catalog.TicketTypes.Where(ticket => !ticket.IsDeleted).ToDictionary(ticket => ticket.Id);
                var lines = order.Lines.ToDictionary(line => line.Id);
                IReadOnlyList<RegistrationTicketAssignment> existing =
                    await participants.GetAssignmentsForUpdateByOrderAsync(order.Id, order.TenantId, token);
                var byKey = existing.ToDictionary(item => (item.RegistrationOrderLineId, item.Ordinal));
                var additions = new List<RegistrationTicketAssignment>();
                bool changed = false;
                int mutationIndex = 0;
                int admissionIndex = 0;

                foreach (TicketParticipantAssignmentInputDto item in requestedAssignments)
                {
                    if (!TryResolveLine(item.RegistrationOrderLineId, item.Ordinal, lines, ticketTypes, out RegistrationOrderLine? line, out EventTicketType? ticket, out string? error))
                    {
                        return Invalid(orderId, error!);
                    }

                    ParticipantDataCollectionModeEnum mode = NormalizeCollectionMode(ticket!.ParticipantDataCollectionModeId);
                    if (mode is ParticipantDataCollectionModeEnum.None or ParticipantDataCollectionModeEnum.LeadBookerOnly)
                    {
                        return Invalid(orderId, "The ticket data-collection mode does not permit participant assignments.");
                    }

                    RegistrationParticipant? participant = await participants.GetParticipantForUpdateAsync(
                        item.ParticipantId, order.Id, order.TenantId, token);
                    if (participant is null || !RegistrationOrderRules.IsParticipantEligibleForTicket(participant) ||
                        ticket.RequiresGuardian && participant.GuardianParticipantId is null)
                    {
                        return Invalid(orderId, "The participant is not eligible for this ticket assignment.");
                    }

                    EnsureParticipantDetails((ParticipantTypeEnum)participant.ParticipantTypeId,
                        new ParticipantDetailsDto(participant.Pii?.DisplayName, participant.Pii?.Email, participant.Pii?.Phone),
                        required: mode == ParticipantDataCollectionModeEnum.PerTicketRequired || ticket.RequiresGuardian);
                    if (existing.Any(assignment => assignment.ParticipantId == participant.Id &&
                        (assignment.RegistrationOrderLineId, assignment.Ordinal) != (item.RegistrationOrderLineId, item.Ordinal)))
                    {
                        return Invalid(orderId, "A participant cannot occupy more than one ticket ordinal in the same order.");
                    }

                    if (byKey.TryGetValue((line!.Id, item.Ordinal), out RegistrationTicketAssignment? assignment))
                    {
                        if (assignment.AssignmentStatusId != (int)AssignmentStatusEnum.Assigned || assignment.ParticipantId != participant.Id)
                        {
                            assignment.Assign(participant, assignmentConcurrency[mutationIndex]);
                            changed = true;
                        }
                    }
                    else
                    {
                        assignment = RegistrationTicketAssignment.CreateAssigned(
                            assignmentIds[mutationIndex], line.Id, item.Ordinal, participant, now);
                        assignment.ConcurrencyStamp = assignmentConcurrency[mutationIndex];
                        additions.Add(assignment);
                        changed = true;
                        byKey[(line.Id, item.Ordinal)] = assignment;
                    }

                    if ((RegistrationOrderStatusEnum)order.RegistrationOrderStatusId == RegistrationOrderStatusEnum.Confirmed)
                    {
                        IReadOnlyList<EventRegistration> admissions = await participants.GetAdmissionsForUpdateAsync(
                            order.Id, line.Id, item.Ordinal, order.TenantId, token);
                        if (admissions.Count > 0)
                        {
                            foreach (EventRegistration admission in admissions.Where(value => value.RegistrationParticipantId != participant.Id))
                            {
                                admission.ReassignParticipant(participant, admissionConcurrency[admissionIndex++]);
                                changed = true;
                            }
                        }
                        else
                        {
                            IReadOnlyList<(TicketTypeEntitlement Entitlement, EventSession Session)> expanded =
                                RegistrationAdmissionMaterializer.Expand(ticket, sessions);
                            Guid[] stableIds = admissionIds[(line.Id, item.Ordinal)];
                            EventRegistration[] materialized = expanded.Select((value, index) =>
                                RegistrationAdmissionMaterializer.Create(
                                    stableIds[index], admissionConcurrency[admissionIndex++], order, line, value.Entitlement,
                                    value.Session, participant, item.Ordinal, now)).ToArray();
                            await inventory.AddEventRegistrationsAsync(materialized, token);
                            changed = materialized.Length > 0 || changed;
                        }
                    }

                    mutationIndex++;
                }

                foreach (TicketDeferralInputDto item in requestedDeferrals)
                {
                    if (!TryResolveLine(item.RegistrationOrderLineId, item.Ordinal, lines, ticketTypes, out RegistrationOrderLine? line, out EventTicketType? ticket, out string? error))
                    {
                        return Invalid(orderId, error!);
                    }

                    if ((RegistrationOrderStatusEnum)order.RegistrationOrderStatusId == RegistrationOrderStatusEnum.Confirmed ||
                        NormalizeCollectionMode(ticket!.ParticipantDataCollectionModeId) != ParticipantDataCollectionModeEnum.DeferredAssignment)
                    {
                        return Invalid(orderId, "Only unconfirmed deferred-assignment tickets can be deferred.");
                    }

                    if (byKey.TryGetValue((line!.Id, item.Ordinal), out RegistrationTicketAssignment? assignment))
                    {
                        if (assignment.AssignmentStatusId != (int)AssignmentStatusEnum.Deferred || assignment.AssignmentDeadline != deadline)
                        {
                            assignment.Defer(deadline!.Value, now, assignmentConcurrency[mutationIndex]);
                            changed = true;
                        }
                    }
                    else
                    {
                        assignment = RegistrationTicketAssignment.Create(
                            assignmentIds[mutationIndex], order.TenantId, order.Id, line.Id, item.Ordinal, null,
                            AssignmentStatusEnum.Deferred, deadline, now);
                        assignment.ConcurrencyStamp = assignmentConcurrency[mutationIndex];
                        additions.Add(assignment);
                        changed = true;
                        byKey[(line.Id, item.Ordinal)] = assignment;
                    }

                    mutationIndex++;
                }

                if (!changed)
                {
                    return Success(order.Id, "Registration ticket assignments already match the request.");
                }

                order.BumpConcurrency(orderConcurrency);
                await participants.AddAssignmentsAsync(additions, token);
                await participants.SaveChangesAsync(token);
                return Success(order.Id, "Registration ticket assignments updated.");
            }, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Invalid(orderId, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Invalid(orderId, exception.Message);
        }
    }

    private async Task<RegistrationParticipant?> GetGuardianAsync(
        Guid? guardianParticipantId,
        RegistrationOrder order,
        CancellationToken cancellationToken) => guardianParticipantId.HasValue
        ? await participants.GetParticipantForUpdateAsync(guardianParticipantId.Value, order.Id, order.TenantId, cancellationToken)
            ?? throw new ArgumentException("Guardian participant was not found in this order.")
        : null;

    private static Guid[] CreateAdmissionIds(
        RegistrationOrder order,
        EventTicketCatalogVersion catalog,
        Guid lineId,
        IReadOnlyCollection<EventSession> sessions)
    {
        RegistrationOrderLine line = order.Lines.Single(value => value.Id == lineId);
        EventTicketType ticket = catalog.TicketTypes.Single(value => value.Id == line.TicketTypeId);
        return RegistrationAdmissionMaterializer.Expand(ticket, sessions).Select(_ => Guid.CreateVersion7()).ToArray();
    }

    private static bool TryResolveLine(
        Guid lineId,
        int ordinal,
        IReadOnlyDictionary<Guid, RegistrationOrderLine> lines,
        IReadOnlyDictionary<Guid, EventTicketType> ticketTypes,
        out RegistrationOrderLine? line,
        out EventTicketType? ticket,
        out string? error)
    {
        ticket = null;
        if (!lines.TryGetValue(lineId, out line) || !ticketTypes.TryGetValue(line.TicketTypeId, out ticket))
        {
            error = "Ticket assignment line does not belong to this order.";
            return false;
        }

        if (ordinal < 1 || ordinal > line.Quantity)
        {
            error = "Ticket assignment ordinal is outside the order-line quantity.";
            return false;
        }

        error = null;
        return true;
    }

    private static ParticipantTypeEnum NormalizeParticipantType(int participantTypeId)
    {
        ParticipantTypeEnum type = (ParticipantTypeEnum)participantTypeId;
        return Enum.IsDefined(type) && type != ParticipantTypeEnum.Unnamed
            ? type
            : throw new ArgumentException("Participant type is invalid.");
    }

    private static ParticipantDataCollectionModeEnum NormalizeCollectionMode(int modeId)
    {
        ParticipantDataCollectionModeEnum mode = (ParticipantDataCollectionModeEnum)modeId;
        return Enum.IsDefined(mode) ? mode : throw new ArgumentException("Participant data-collection mode is invalid.");
    }

    private static void EnsureParticipantDetails(ParticipantTypeEnum type, ParticipantDetailsDto details, bool required)
    {
        ArgumentNullException.ThrowIfNull(details);
        if ((required || type is ParticipantTypeEnum.Child or ParticipantTypeEnum.Dependent) &&
            string.IsNullOrWhiteSpace(details.DisplayName))
        {
            throw new ArgumentException("Required participant details are incomplete.");
        }
    }

    private static bool HasDetails(ParticipantDetailsDto details) =>
        !string.IsNullOrWhiteSpace(details.DisplayName) || !string.IsNullOrWhiteSpace(details.Email) || !string.IsNullOrWhiteSpace(details.Phone);

    private static BaseCommandResponse<Guid> Success(Guid id, string message) => new() { Id = id, Success = true, Message = message };
    private static BaseCommandResponse<Guid> Missing(Guid id) => new() { Id = id, Success = false, Message = "Registration participant resource was not found.", Errors = ["Registration participant resource was not found."] };
    private static BaseCommandResponse<Guid> Invalid(Guid id, string error) => new() { Id = id, Success = false, Message = "Registration participant request is invalid.", Errors = [error] };
    private static BaseCommandResponse<Guid> Conflict(Guid id) => new() { Id = id, Success = false, Message = "Registration order changed while assignments were updated.", Errors = ["Registration order changed while assignments were updated."] };
}
