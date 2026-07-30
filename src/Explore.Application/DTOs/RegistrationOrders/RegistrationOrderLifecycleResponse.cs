// ABOUTME: Standard command response enriched with the safe post-transition order state.
// ABOUTME: Enables duplicate lifecycle submissions to return the original durable result.

using Explore.Application.Responses;

namespace Explore.Application.DTOs.RegistrationOrders;

public sealed class RegistrationOrderLifecycleResponse : BaseCommandResponse<Guid>
{
    public RegistrationOrderDto? Order { get; init; }
}
