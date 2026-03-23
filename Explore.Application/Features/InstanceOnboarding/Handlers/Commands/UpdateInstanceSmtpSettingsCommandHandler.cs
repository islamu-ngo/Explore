// ABOUTME: Handler for updating the instance SMTP (email delivery) settings.
// ABOUTME: Validates input, persists SMTP config to the infrastructure config store.
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class UpdateInstanceSmtpSettingsCommandHandler : IRequestHandler<UpdateInstanceSmtpSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IInstanceSmtpSettingService _smtpSettingService;
    private readonly ISmtpConfigResolver _smtpConfigResolver;

    public UpdateInstanceSmtpSettingsCommandHandler(
        IAdminContext adminContext,
        IInstanceSmtpSettingService smtpSettingService,
        ISmtpConfigResolver smtpConfigResolver)
    {
        _adminContext = adminContext;
        _smtpSettingService = smtpSettingService;
        _smtpConfigResolver = smtpConfigResolver;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateInstanceSmtpSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken);
        if (!isInstanceAdmin)
        {
            response.Success = false;
            response.Message = "Only instance administrators can update SMTP settings.";
            return response;
        }

        await _smtpSettingService.ApplySettingsAsync(request.Settings);

        _smtpConfigResolver.InvalidateCache();

        response.Success = true;
        response.Message = "SMTP settings updated successfully.";
        return response;
    }
}
