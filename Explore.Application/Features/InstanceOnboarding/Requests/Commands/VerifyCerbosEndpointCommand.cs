// ABOUTME: Command contract for verifying a Cerbos gRPC endpoint is reachable.
// ABOUTME: Used during onboarding to validate user-entered or env-detected endpoints before saving.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class VerifyCerbosEndpointCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required string GrpcEndpoint { get; set; }
}
