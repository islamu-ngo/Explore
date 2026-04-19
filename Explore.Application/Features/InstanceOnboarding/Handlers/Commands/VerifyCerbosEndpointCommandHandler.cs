// ABOUTME: Handles explicit Cerbos endpoint verification requests from onboarding UI.
// ABOUTME: Validates endpoint format, normalizes bare host:port input, and performs a gRPC health check.

using Explore.Application.Contracts.Services;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Utilities;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class VerifyCerbosEndpointCommandHandler : IRequestHandler<VerifyCerbosEndpointCommand, BaseCommandResponse<Guid>>
{
    private readonly IAuthorizationProviderConfigurationService _configurationService;

    public VerifyCerbosEndpointCommandHandler(IAuthorizationProviderConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(VerifyCerbosEndpointCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var normalizedEndpoint = GrpcEndpointNormalizer.Normalize(request.GrpcEndpoint);

        if (!GrpcEndpointNormalizer.IsValid(normalizedEndpoint))
        {
            response.Success = false;
            response.Message = "Cerbos gRPC endpoint must be a valid URL or host:port value.";
            return response;
        }

        var isReachable = await _configurationService.VerifyCerbosEndpointAsync(normalizedEndpoint, cancellationToken);
        if (!isReachable)
        {
            response.Success = false;
            response.Message = "Cerbos gRPC endpoint could not be verified.";
            response.Errors = ["Ensure the endpoint is reachable and serving the gRPC health service."];
            return response;
        }

        response.Success = true;
        response.Message = "Cerbos gRPC endpoint verified successfully.";
        return response;
    }
}
