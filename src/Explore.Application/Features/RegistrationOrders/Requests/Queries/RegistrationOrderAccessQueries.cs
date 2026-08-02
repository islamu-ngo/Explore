// ABOUTME: Defines account- and capability-scoped registration-order read contracts.
// ABOUTME: Requires a full order/event/capability tuple for anonymous order visibility.

using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.DTOs.RegistrationSubmissions;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Requests.Queries;

public sealed record GetGuestRegistrationOrderQuery(Guid EventId, Guid OrderId, string? CapabilityToken)
    : IRequest<GuestRegistrationOrderDto?>;

public sealed record GetCurrentRegistrationOrderQuery(Guid OrderId)
    : IRequest<RegistrationOrderDto?>;

public sealed record GetGuestRegistrationOrderParticipantsQuery(
    Guid EventId,
    Guid OrderId,
    string? CapabilityToken) : IRequest<RegistrationOrderParticipantsDto?>;

public sealed record GetAuthenticatedRegistrationOrderParticipantsQuery(Guid EventId, Guid OrderId)
    : IRequest<RegistrationOrderParticipantsDto?>;

public sealed record GetGuestNativeRegistrationRequirementProgressQuery(
    Guid EventId,
    Guid OrderId,
    string? CapabilityToken) : IRequest<NativeRegistrationRequirementProgressCollectionDto?>;

public sealed record GetAuthenticatedNativeRegistrationRequirementProgressQuery(Guid EventId, Guid OrderId)
    : IRequest<NativeRegistrationRequirementProgressCollectionDto?>;
