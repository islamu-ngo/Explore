// ABOUTME: Handles UpdateTenantFooterSettingsCommand — writes tenant-scoped footer scalar settings.
// ABOUTME: Reads lock flags first; silently skips any setting locked at instance level.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Footer.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using MediatR;

namespace Explore.Application.Features.Footer.Handlers.Commands;

public sealed class UpdateTenantFooterSettingsCommandHandler(
    IHierarchicalSettingsResolver settingsResolver,
    ITenantContext tenantContext)
    : IRequestHandler<UpdateTenantFooterSettingsCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateTenantFooterSettingsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var userId = request.UserId;

        // Resolve governance locks before applying any tenant overrides
        var lockGroup = await settingsResolver.ResolveGroupAsync<FooterSettingGroup>(
            new SettingContext(), cancellationToken);

        if (request.Enabled.HasValue)
            await settingsResolver.SetValueAsync(
                GovernanceSettingKeys.Footer.Enabled,
                SettingValueSerializer.Serialize(request.Enabled.Value),
                SettingScope.Tenant, tenantId, userId, cancellationToken);

        if (request.Template is not null && !lockGroup.LockTenantTemplate)
            await settingsResolver.SetValueAsync(
                GovernanceSettingKeys.Footer.Template,
                SettingValueSerializer.Serialize(request.Template),
                SettingScope.Tenant, tenantId, userId, cancellationToken);

        if (request.ShowDescription.HasValue && !lockGroup.LockTenantDescription)
            await settingsResolver.SetValueAsync(
                GovernanceSettingKeys.Footer.ShowDescription,
                SettingValueSerializer.Serialize(request.ShowDescription.Value),
                SettingScope.Tenant, tenantId, userId, cancellationToken);

        if (request.DescriptionText is not null && !lockGroup.LockTenantDescription)
            await settingsResolver.SetValueAsync(
                GovernanceSettingKeys.Footer.DescriptionText,
                SettingValueSerializer.Serialize(request.DescriptionText),
                SettingScope.Tenant, tenantId, userId, cancellationToken);

        if (request.ShowSocialLinks.HasValue && !lockGroup.LockTenantSocialLinks)
            await settingsResolver.SetValueAsync(
                GovernanceSettingKeys.Footer.ShowSocialLinks,
                SettingValueSerializer.Serialize(request.ShowSocialLinks.Value),
                SettingScope.Tenant, tenantId, userId, cancellationToken);

        if (request.SocialLinksJson is not null && !lockGroup.LockTenantSocialLinks)
            await settingsResolver.SetValueAsync(
                GovernanceSettingKeys.Footer.SocialLinks,
                request.SocialLinksJson,
                SettingScope.Tenant, tenantId, userId, cancellationToken);

        if (request.CopyrightText is not null && !lockGroup.LockTenantCopyright)
            await settingsResolver.SetValueAsync(
                GovernanceSettingKeys.Footer.CopyrightText,
                SettingValueSerializer.Serialize(request.CopyrightText),
                SettingScope.Tenant, tenantId, userId, cancellationToken);

        if (request.ShowCookieSettingsLink.HasValue)
            await settingsResolver.SetValueAsync(
                GovernanceSettingKeys.Footer.ShowCookieSettingsLink,
                SettingValueSerializer.Serialize(request.ShowCookieSettingsLink.Value),
                SettingScope.Tenant, tenantId, userId, cancellationToken);

        settingsResolver.InvalidateCache(SettingScope.Tenant, tenantId);

        return new BaseCommandResponse<Guid> { Success = true };
    }
}
