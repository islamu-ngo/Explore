// ABOUTME: Sub-resource DTO for deployment mode configuration.
// ABOUTME: Replaces the DeploymentMode string property from the monolithic InstanceGovernanceSettingsDto.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Instance;

public sealed record DeploymentModeDto
{
    public DeploymentMode Mode { get; init; } = DeploymentMode.SingleTenant;
}
