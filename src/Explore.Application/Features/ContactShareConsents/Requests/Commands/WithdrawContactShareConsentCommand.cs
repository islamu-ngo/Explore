// ABOUTME: MediatR command for a user to withdraw a previously granted contact-sharing consent.
// ABOUTME: User-facing action; authorised via the user's own identity (no org permission needed).

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ContactShareConsents.Requests.Commands;

public class WithdrawContactShareConsentCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid ConsentId { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
}
