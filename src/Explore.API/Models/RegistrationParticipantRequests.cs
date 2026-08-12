// ABOUTME: Defines PII-minimal participant and concrete ticket-assignment request bodies.
// ABOUTME: Keeps route-owned event, order, participant, and guest capability values out of JSON payloads.

using Explore.Application.DTOs.RegistrationOrders;

namespace Explore.API.Models;

public sealed record RegistrationParticipantRequest(
    int ParticipantTypeId,
    Guid? GuardianParticipantId,
    ParticipantDetailsDto Details);

public sealed record RegistrationTicketAssignmentsRequest(
    IReadOnlyCollection<TicketParticipantAssignmentInputDto> Assignments);

public sealed record RegistrationCompanyAssignmentCsvRequest(string CsvUtf8, string LineageKey);

public sealed record RegistrationTicketDeferralsRequest(
    IReadOnlyCollection<TicketDeferralInputDto> Assignments,
    DateTime AssignmentDeadline);
