// ABOUTME: Per-domain instance settings update commands for sub-resource endpoints.
// ABOUTME: Each command targets a single governance domain, replacing the monolithic UpdateInstanceGovernanceSettingsCommand for granular updates.

using Explore.Application.DTOs.Instance;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class UpdateModuleSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required ModuleSettingsDto Settings { get; set; }
}

public class UpdateEventPolicyCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required EventPolicyDto Settings { get; set; }
}

public class UpdateOrganizationPolicyCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required OrganizationPolicyDto Settings { get; set; }
}

public class UpdateBrandingSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required BrandingSettingsDto Settings { get; set; }
}

public class UpdateDomainSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required DomainSettingsDto Settings { get; set; }
}

public class UpdateTenantDelegationSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required TenantDelegationSettingsDto Settings { get; set; }
}

public class UpdateMcpGovernanceSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required McpGovernanceSettingsDto Settings { get; set; }
}

public class UpdateRenderPolicySettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required RenderPolicySettingsDto Settings { get; set; }
}
