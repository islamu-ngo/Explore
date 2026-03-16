// ABOUTME: Handler for WithdrawContactShareConsentCommand — marks a consent as withdrawn.
// ABOUTME: Delegates to IContactShareConsentService for the actual business logic.

using Explore.Application.Contracts.Services;
using Explore.Application.Features.ContactShareConsents.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.ContactShareConsents.Handlers.Commands;

public class WithdrawContactShareConsentCommandHandler : IRequestHandler<WithdrawContactShareConsentCommand, BaseCommandResponse<Guid>>
{
    private readonly IContactShareConsentService _consentService;
    private readonly ILogger<WithdrawContactShareConsentCommandHandler> _logger;

    public WithdrawContactShareConsentCommandHandler(
        IContactShareConsentService consentService,
        ILogger<WithdrawContactShareConsentCommandHandler> logger)
    {
        _consentService = consentService;
        _logger = logger;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(WithdrawContactShareConsentCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        try
        {
            await _consentService.WithdrawConsent(request.TenantId, request.UserId, request.ConsentId);

            response.Success = true;
            response.Id = request.ConsentId;
            response.Message = "Contact sharing consent withdrawn successfully.";
        }
        catch (KeyNotFoundException ex)
        {
            response.Success = false;
            response.Message = ex.Message;
            response.Errors = [ex.Message];
        }
        catch (UnauthorizedAccessException ex)
        {
            response.Success = false;
            response.Message = ex.Message;
            response.Errors = [ex.Message];
        }

        return response;
    }
}
