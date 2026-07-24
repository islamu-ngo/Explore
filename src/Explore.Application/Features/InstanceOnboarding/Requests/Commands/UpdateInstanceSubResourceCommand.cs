// ABOUTME: Per-domain instance settings partial-update commands for sub-resource endpoints.
// ABOUTME: Each command carries a dedicated presence-aware write contract rather than a read DTO.

using Explore.Application.DTOs.Instance;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class UpdateModuleSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required PatchModuleSettingsDto Patch { get; set; }
}

public class UpdateEventPolicyCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required PatchEventPolicyDto Patch { get; set; }
}

public class UpdateOrganizationPolicyCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required PatchOrganizationPolicyDto Patch { get; set; }
}

public class UpdateBrandingSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required PatchBrandingSettingsDto Patch { get; set; }
}

public class UpdateDomainSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required PatchDomainSettingsDto Patch { get; set; }
}

public class UpdateTenantDelegationSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required PatchTenantDelegationSettingsDto Patch { get; set; }
}

public class UpdateAdminPortalSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required PatchAdminPortalSettingsDto Patch { get; set; }
}

public class UpdateMcpGovernanceSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required PatchMcpGovernanceSettingsDto Patch { get; set; }
}

public class UpdateAiAssistantGovernanceSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required PatchAiAssistantGovernanceSettingsDto Patch { get; set; }
}

public class UpdateRenderPolicySettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required PatchRenderPolicySettingsDto Patch { get; set; }
}
