// ABOUTME: Defines the immutable CQRS query for one private ticket-transfer resource.
// ABOUTME: Carries resource identities and opaque capability while authority remains server-owned.

using Explore.Application.DTOs.Admissions;
using MediatR;

namespace Explore.Application.Features.Admissions.Requests.Queries;

public sealed record GetTicketTransferQuery(
    Guid EventId,
    Guid AdmissionTicketId,
    Guid AdmissionTicketTransferId,
    string? CapabilityToken) :
    IRequest<TicketTransferDto?>;
