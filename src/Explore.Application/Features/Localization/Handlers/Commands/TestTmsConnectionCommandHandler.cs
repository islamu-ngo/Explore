// ABOUTME: Handler for TestTmsConnectionCommand that verifies TMS provider connectivity.
// ABOUTME: Tests the configured TMS (Tolgee/Weblate) connection and returns success/failure.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Identity;
using Explore.Application.Features.Localization.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Localization.Handlers.Commands;

public class TestTmsConnectionCommandHandler : IRequestHandler<TestTmsConnectionCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly ITranslationManagementProvider _translationProvider;

    public TestTmsConnectionCommandHandler(
        IAdminContext adminContext,
        ITranslationManagementProvider translationProvider)
    {
        _adminContext = adminContext;
        _translationProvider = translationProvider;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(TestTmsConnectionCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var actor = await _adminContext.ResolveUserIdAsync(cancellationToken);
        if (!actor.HasValue || !await _adminContext.IsInstanceAdminAsync(actor.Value, cancellationToken))
        {
            response.Success = false;
            response.Message = "Instance administrator authority is required to test the localization TMS connection.";
            return response;
        }

        var isConnected = await _translationProvider.TestConnectionAsync(cancellationToken);

        if (isConnected)
        {
            response.Success = true;
            response.Message = "TMS connection successful.";
        }
        else
        {
            response.Success = false;
            response.Message = "TMS connection failed. Check provider settings and API credentials.";
        }

        return response;
    }
}
