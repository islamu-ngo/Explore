// ABOUTME: Defines immutable CQRS requests for line-scoped fair-return waitlist reads and writes.
// ABOUTME: Carries route identities and opaque capability only; tenant, user, priority, and policy remain server-owned.

using Explore.Application.DTOs.Waitlist;
using MediatR;

namespace Explore.Application.Features.Waitlist;

public sealed record GetFairReturnWaitlistQuery(
    Guid EventId,
    Guid RegistrationOrderId,
    Guid RegistrationOrderLineId,
    string? CapabilityToken) :
    IRequest<FairReturnWaitlistDto?>;

public sealed record JoinFairReturnWaitlistCommand(
    Guid EventId,
    Guid RegistrationOrderId,
    Guid RegistrationOrderLineId) :
    IRequest<FairReturnWaitlistDto?>;

public sealed record LeaveFairReturnWaitlistCommand(
    Guid EventId,
    Guid RegistrationOrderId,
    Guid RegistrationOrderLineId) :
    IRequest<FairReturnWaitlistDto?>;

public sealed record AcceptFairReturnOfferCommand(
    Guid EventId,
    Guid RegistrationOrderId,
    Guid RegistrationOrderLineId,
    Guid OfferId) :
    IRequest<FairReturnWaitlistDto?>;

public sealed record WithdrawFairReturnSupplyCommand(
    Guid EventId,
    Guid RegistrationOrderId,
    Guid RegistrationOrderLineId,
    Guid SupplyId) :
    IRequest<FairReturnWaitlistDto?>;
