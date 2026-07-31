// ABOUTME: Declares Application-level creation of a registration order with its reserved inventory holds.
// ABOUTME: Lets authenticated and guest entry points reuse one serializable creation implementation.

using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Application.Responses;

namespace Explore.Application.Contracts.Services;

public interface IRegistrationOrderStarter
{
    Task<BaseCommandResponse<Guid>> StartAsync(
        CreateRegistrationOrderWithHoldCommand request,
        CancellationToken cancellationToken);
}
