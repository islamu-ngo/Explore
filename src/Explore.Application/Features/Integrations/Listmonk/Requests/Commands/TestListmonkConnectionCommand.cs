// ABOUTME: Command for testing Listmonk API reachability through the generated-client boundary.
// ABOUTME: Keeps API controllers thin while Infrastructure owns the concrete Listmonk client.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Integrations.Listmonk.Requests.Commands;

public sealed record TestListmonkConnectionCommand : IRequest<BaseCommandResponse<Guid>>
{
}
