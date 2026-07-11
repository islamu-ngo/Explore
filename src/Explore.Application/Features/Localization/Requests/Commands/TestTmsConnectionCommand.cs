// ABOUTME: MediatR command to test the connection to the configured TMS provider.
// ABOUTME: Returns success/failure indicating whether the TMS is reachable with current settings.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Localization.Requests.Commands;

public class TestTmsConnectionCommand : IRequest<BaseCommandResponse<Guid>>
{
}
