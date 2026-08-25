// ABOUTME: MediatR command for a user to withdraw a previously granted contact-sharing consent.
// ABOUTME: User-facing action; authorised via the user's own identity (no org permission needed).

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ContactShareConsents.Requests.Commands;

public sealed record WithdrawContactShareConsentCommand(
    Guid ConsentId = default,
    Guid UserId = default,
    Guid TenantId = default
) : IRequest<BaseCommandResponse<Guid>>;
