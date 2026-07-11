// ABOUTME: Sub-resource DTO for deployment mode configuration.
// ABOUTME: Replaces the DeploymentMode string property from the monolithic InstanceGovernanceSettingsDto.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Instance;

public class DeploymentModeDto
{
    public DeploymentMode Mode { get; set; } = DeploymentMode.SingleTenant;
}
