// ABOUTME: Handlers for per-domain instance settings update commands.
// ABOUTME: Each handler validates admin access, then delegates to the corresponding service method.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Instance.Validators;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Constants;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class UpdateModuleSettingsCommandHandler : IRequestHandler<UpdateModuleSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IInstanceGovernanceSettingService _service;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateModuleSettingsCommandHandler(IAdminContext adminContext, IInstanceGovernanceSettingService service, IUnitOfWork unitOfWork)
    {
        _adminContext = adminContext;
        _service = service;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateModuleSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        if (!await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
            return Unauthorized(response);

        await _unitOfWork.ExecuteInTransactionAsync(ct =>
            _service.ApplyModuleSettingsAsync(null, request.Settings, request.UserId), cancellationToken);
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
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEventPolicyCommandHandler(IAdminContext adminContext, IInstanceGovernanceSettingService service, IUnitOfWork unitOfWork)
    {
        _adminContext = adminContext;
        _service = service;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventPolicyCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        if (!await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
            return Unauthorized(response);

        await _unitOfWork.ExecuteInTransactionAsync(ct =>
            _service.ApplyEventPolicyAsync(request.Settings, request.UserId), cancellationToken);
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
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrganizationPolicyCommandHandler(IAdminContext adminContext, IInstanceGovernanceSettingService service, IUnitOfWork unitOfWork)
    {
        _adminContext = adminContext;
        _service = service;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateOrganizationPolicyCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        if (!await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
            return Unauthorized(response);

        await _unitOfWork.ExecuteInTransactionAsync(ct =>
            _service.ApplyOrganizationPolicyAsync(request.Settings, request.UserId), cancellationToken);
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
    private readonly IDeploymentModeProvider _deploymentModeProvider;
    private readonly ITenantBrandingSettingsDocumentProvisioningService _tenantBrandingProvisioningService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBrandingSettingsCommandHandler(
        IAdminContext adminContext,
        IInstanceGovernanceSettingService service,
        IDeploymentModeProvider deploymentModeProvider,
        ITenantBrandingSettingsDocumentProvisioningService tenantBrandingProvisioningService,
        IUnitOfWork unitOfWork)
    {
        _adminContext = adminContext;
        _service = service;
        _deploymentModeProvider = deploymentModeProvider;
        _tenantBrandingProvisioningService = tenantBrandingProvisioningService;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateBrandingSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        if (!await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
            return Unauthorized(response);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _service.ApplyBrandingSettingsAsync(request.Settings, request.UserId);

            if (await _deploymentModeProvider.IsSingleTenantAsync(ct))
            {
                await _tenantBrandingProvisioningService.EnsureTenantBrandingDocumentAsync(
                    PlatformDefaults.DefaultTenantId,
                    request.Settings.DefaultBrandDisplayName,
                    ct);
            }
        }, cancellationToken);
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
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDomainSettingsCommandHandler(IAdminContext adminContext, IInstanceGovernanceSettingService service, IUnitOfWork unitOfWork)
    {
        _adminContext = adminContext;
        _service = service;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateDomainSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        if (!await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
            return Unauthorized(response);

        await _unitOfWork.ExecuteInTransactionAsync(ct =>
            _service.ApplyDomainSettingsAsync(request.Settings, request.UserId), cancellationToken);
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
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTenantDelegationSettingsCommandHandler(IAdminContext adminContext, IInstanceGovernanceSettingService service, IUnitOfWork unitOfWork)
    {
        _adminContext = adminContext;
        _service = service;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateTenantDelegationSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        if (!await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
            return Unauthorized(response);

        await _unitOfWork.ExecuteInTransactionAsync(ct =>
            _service.ApplyTenantDelegationSettingsAsync(request.Settings, request.UserId), cancellationToken);
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

public class UpdateMcpGovernanceSettingsCommandHandler : IRequestHandler<UpdateMcpGovernanceSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IInstanceGovernanceSettingService _service;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMcpGovernanceSettingsCommandHandler(IAdminContext adminContext, IInstanceGovernanceSettingService service, IUnitOfWork unitOfWork)
    {
        _adminContext = adminContext;
        _service = service;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateMcpGovernanceSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        if (!await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
            return Unauthorized(response);

        await _unitOfWork.ExecuteInTransactionAsync(ct =>
            _service.ApplyMcpGovernanceSettingsAsync(request.Settings, request.UserId), cancellationToken);
        response.Success = true;
        response.Message = "MCP governance settings updated successfully.";
        return response;
    }

    private static BaseCommandResponse<Guid> Unauthorized(BaseCommandResponse<Guid> r)
    {
        r.Success = false;
        r.Message = "Only instance administrators can update instance governance settings.";
        return r;
    }
}

public class UpdateAiAssistantGovernanceSettingsCommandHandler : IRequestHandler<UpdateAiAssistantGovernanceSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IInstanceGovernanceSettingService _service;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAiAssistantGovernanceSettingsCommandHandler(IAdminContext adminContext, IInstanceGovernanceSettingService service, IUnitOfWork unitOfWork)
    {
        _adminContext = adminContext;
        _service = service;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateAiAssistantGovernanceSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        if (!await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
            return Unauthorized(response);

        await _unitOfWork.ExecuteInTransactionAsync(ct =>
            _service.ApplyAiAssistantGovernanceSettingsAsync(request.Settings, request.UserId), cancellationToken);
        response.Success = true;
        response.Message = "AI Assistant governance settings updated successfully.";
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
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRenderPolicySettingsCommandHandler(IAdminContext adminContext, IInstanceGovernanceSettingService service, IUnitOfWork unitOfWork)
    {
        _adminContext = adminContext;
        _service = service;
        _unitOfWork = unitOfWork;
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

        await _unitOfWork.ExecuteInTransactionAsync(ct =>
            _service.ApplyRenderPolicySettingsAsync(request.Settings, request.UserId), cancellationToken);
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
