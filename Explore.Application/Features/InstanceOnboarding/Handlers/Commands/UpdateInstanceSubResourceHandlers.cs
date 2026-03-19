// ABOUTME: Handlers for per-domain instance settings update commands.
// ABOUTME: Each handler validates admin access, then delegates to the corresponding service method.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Instance.Validators;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class UpdateModuleSettingsCommandHandler : IRequestHandler<UpdateModuleSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IInstanceGovernanceSettingService _service;

    public UpdateModuleSettingsCommandHandler(IAdminContext adminContext, IInstanceGovernanceSettingService service)
    {
        _adminContext = adminContext;
        _service = service;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateModuleSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        if (!await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
            return Unauthorized(response);

        await _service.ApplyModuleSettingsAsync(null, request.Settings, request.UserId);
        response.Success = true;
        response.Message = "Module settings updated successfully.";
        return response;
    }

    private static BaseCommandResponse<Guid> Unauthorized(BaseCommandResponse<Guid> r)
    {
        r.Success = false;
        r.Message = "Only instance administrators can update instance governance settings.";
        return r;
    }
}

public class UpdateEventPolicyCommandHandler : IRequestHandler<UpdateEventPolicyCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IInstanceGovernanceSettingService _service;

    public UpdateEventPolicyCommandHandler(IAdminContext adminContext, IInstanceGovernanceSettingService service)
    {
        _adminContext = adminContext;
        _service = service;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventPolicyCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        if (!await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
            return Unauthorized(response);

        await _service.ApplyEventPolicyAsync(request.Settings, request.UserId);
        response.Success = true;
        response.Message = "Event policy updated successfully.";
        return response;
    }

    private static BaseCommandResponse<Guid> Unauthorized(BaseCommandResponse<Guid> r)
    {
        r.Success = false;
        r.Message = "Only instance administrators can update instance governance settings.";
        return r;
    }
}

public class UpdateOrganizationPolicyCommandHandler : IRequestHandler<UpdateOrganizationPolicyCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IInstanceGovernanceSettingService _service;

    public UpdateOrganizationPolicyCommandHandler(IAdminContext adminContext, IInstanceGovernanceSettingService service)
    {
        _adminContext = adminContext;
        _service = service;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateOrganizationPolicyCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        if (!await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
            return Unauthorized(response);

        await _service.ApplyOrganizationPolicyAsync(request.Settings, request.UserId);
        response.Success = true;
        response.Message = "Organization policy updated successfully.";
        return response;
    }

    private static BaseCommandResponse<Guid> Unauthorized(BaseCommandResponse<Guid> r)
    {
        r.Success = false;
        r.Message = "Only instance administrators can update instance governance settings.";
        return r;
    }
}

public class UpdateBrandingSettingsCommandHandler : IRequestHandler<UpdateBrandingSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IInstanceGovernanceSettingService _service;

    public UpdateBrandingSettingsCommandHandler(IAdminContext adminContext, IInstanceGovernanceSettingService service)
    {
        _adminContext = adminContext;
        _service = service;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateBrandingSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        if (!await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
            return Unauthorized(response);

        await _service.ApplyBrandingSettingsAsync(request.Settings, request.UserId);
        response.Success = true;
        response.Message = "Branding settings updated successfully.";
        return response;
    }

    private static BaseCommandResponse<Guid> Unauthorized(BaseCommandResponse<Guid> r)
    {
        r.Success = false;
        r.Message = "Only instance administrators can update instance governance settings.";
        return r;
    }
}

public class UpdateDomainSettingsCommandHandler : IRequestHandler<UpdateDomainSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IInstanceGovernanceSettingService _service;

    public UpdateDomainSettingsCommandHandler(IAdminContext adminContext, IInstanceGovernanceSettingService service)
    {
        _adminContext = adminContext;
        _service = service;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateDomainSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        if (!await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
            return Unauthorized(response);

        await _service.ApplyDomainSettingsAsync(request.Settings, request.UserId);
        response.Success = true;
        response.Message = "Domain settings updated successfully.";
        return response;
    }

    private static BaseCommandResponse<Guid> Unauthorized(BaseCommandResponse<Guid> r)
    {
        r.Success = false;
        r.Message = "Only instance administrators can update instance governance settings.";
        return r;
    }
}

public class UpdateTenantDelegationSettingsCommandHandler : IRequestHandler<UpdateTenantDelegationSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IInstanceGovernanceSettingService _service;

    public UpdateTenantDelegationSettingsCommandHandler(IAdminContext adminContext, IInstanceGovernanceSettingService service)
    {
        _adminContext = adminContext;
        _service = service;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateTenantDelegationSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        if (!await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
            return Unauthorized(response);

        await _service.ApplyTenantDelegationSettingsAsync(request.Settings, request.UserId);
        response.Success = true;
        response.Message = "Tenant delegation settings updated successfully.";
        return response;
    }

    private static BaseCommandResponse<Guid> Unauthorized(BaseCommandResponse<Guid> r)
    {
        r.Success = false;
        r.Message = "Only instance administrators can update instance governance settings.";
        return r;
    }
}

public class UpdateRenderPolicySettingsCommandHandler : IRequestHandler<UpdateRenderPolicySettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IInstanceGovernanceSettingService _service;

    public UpdateRenderPolicySettingsCommandHandler(IAdminContext adminContext, IInstanceGovernanceSettingService service)
    {
        _adminContext = adminContext;
        _service = service;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateRenderPolicySettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        if (!await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
            return Unauthorized(response);

        var validator = new RenderPolicySettingsDtoValidator();
        var validation = await validator.ValidateAsync(request.Settings, cancellationToken);
        if (!validation.IsValid)
        {
            response.Success = false;
            response.Message = "Invalid render policy settings.";
            response.Errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        await _service.ApplyRenderPolicySettingsAsync(request.Settings, request.UserId);
        response.Success = true;
        response.Message = "Render policy settings updated successfully.";
        return response;
    }

    private static BaseCommandResponse<Guid> Unauthorized(BaseCommandResponse<Guid> r)
    {
        r.Success = false;
        r.Message = "Only instance administrators can update instance governance settings.";
        return r;
    }
}
