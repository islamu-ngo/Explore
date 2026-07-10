// ABOUTME: Handles Listmonk connection tests through an Application infrastructure contract.
// ABOUTME: Returns only success or failure so generated-client details stay in Infrastructure.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Integrations.Listmonk.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Integrations.Listmonk.Handlers.Commands;

public sealed class TestListmonkConnectionCommandHandler(IListmonkConnectionTester connectionTester)
    : IRequestHandler<TestListmonkConnectionCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        TestListmonkConnectionCommand request,
        CancellationToken cancellationToken)
    {
        var connected = await connectionTester.TestConnectionAsync(cancellationToken);
        return new BaseCommandResponse<Guid>
        {
            Success = connected,
            Message = connected
                ? "Listmonk connection successful."
                : "Listmonk connection failed. Check provider settings and API credentials."
        };
    }
}
