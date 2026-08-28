// ABOUTME: Declares one exact-resource participant readiness read with an optional guest capability.
// ABOUTME: Excludes tenant, current-user, organizer, and HAL authority from caller-controlled input.

using Explore.Application.DTOs.Admissions;
using MediatR;

namespace Explore.Application.Features.Admissions.Requests.Queries;

public sealed record GetParticipantReadinessQuery(
    Guid EventId,
    Guid RegistrationOrderId,
    Guid ParticipantId,
    Guid RegistrationTicketAssignmentId,
    string? CapabilityToken) :
    IRequest<ParticipantReadinessDto?>;
