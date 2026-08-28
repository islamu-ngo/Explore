// ABOUTME: Defines the immutable CQRS query for one private fair-return waitlist resource.
// ABOUTME: Carries route identities and opaque capability while authority remains server-owned.

using Explore.Application.DTOs.Waitlist;
using MediatR;

namespace Explore.Application.Features.Waitlist.Requests.Queries;

public sealed record GetFairReturnWaitlistQuery(
    Guid EventId,
    Guid RegistrationOrderId,
    Guid RegistrationOrderLineId,
    string? CapabilityToken) :
    IRequest<FairReturnWaitlistDto?>;
