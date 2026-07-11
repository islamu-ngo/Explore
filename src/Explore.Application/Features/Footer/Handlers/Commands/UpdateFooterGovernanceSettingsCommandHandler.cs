// ABOUTME: Handles UpdateFooterGovernanceSettingsCommand — persists instance-level footer lock flags.
// ABOUTME: Uses IHierarchicalSettingsResolver.SetValueAsync at Instance scope.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Footer.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using MediatR;

namespace Explore.Application.Features.Footer.Handlers.Commands;

public sealed class UpdateFooterGovernanceSettingsCommandHandler(
    IHierarchicalSettingsResolver settingsResolver)
    : IRequestHandler<UpdateFooterGovernanceSettingsCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateFooterGovernanceSettingsCommand request, CancellationToken cancellationToken)
    {
        var s = request.Settings;
        var userId = request.UserId;

        await settingsResolver.SetValueAsync(
            GovernanceSettingKeys.Footer.LockTenantTemplate,
            SettingValueSerializer.Serialize(s.LockTenantTemplate),
            SettingScope.Instance, Guid.Empty, userId, cancellationToken);

        await settingsResolver.SetValueAsync(
            GovernanceSettingKeys.Footer.LockTenantLinkGroups,
            SettingValueSerializer.Serialize(s.LockTenantLinkGroups),
            SettingScope.Instance, Guid.Empty, userId, cancellationToken);

        await settingsResolver.SetValueAsync(
            GovernanceSettingKeys.Footer.LockTenantSocialLinks,
            SettingValueSerializer.Serialize(s.LockTenantSocialLinks),
            SettingScope.Instance, Guid.Empty, userId, cancellationToken);

        await settingsResolver.SetValueAsync(
            GovernanceSettingKeys.Footer.LockTenantDescription,
            SettingValueSerializer.Serialize(s.LockTenantDescription),
            SettingScope.Instance, Guid.Empty, userId, cancellationToken);

        await settingsResolver.SetValueAsync(
            GovernanceSettingKeys.Footer.LockTenantCopyright,
            SettingValueSerializer.Serialize(s.LockTenantCopyright),
            SettingScope.Instance, Guid.Empty, userId, cancellationToken);

        return new BaseCommandResponse<Guid> { Success = true };
    }
}
