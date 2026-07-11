// ABOUTME: Persists instance-level moderation reporting provider lock flags for tenant delegation.
// ABOUTME: Writes existing governance keys through the hierarchical settings resolver at instance scope.

namespace Explore.Application.Features.EventReporting.Handlers.Commands;

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using MediatR;

public sealed class UpdateReportingProviderLocksCommandHandler(
    IAdminContext adminContext,
    IHierarchicalSettingsResolver settingsResolver)
    : IRequestHandler<UpdateReportingProviderLocksCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateReportingProviderLocksCommand request,
        CancellationToken cancellationToken)
    {
        if (!await adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken))
        {
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Only instance administrators can update moderation reporting provider locks.",
            };
        }

        await SetAsync(
            GovernanceSettingKeys.TenantDelegation.LockReportingProviders,
            request.Locks.LockReportingProviders,
            request.UserId,
            cancellationToken);
        await SetAsync(
            GovernanceSettingKeys.TenantDelegation.LockTenantOspreyProvider,
            request.Locks.LockTenantOspreyProvider,
            request.UserId,
            cancellationToken);
        await SetAsync(
            GovernanceSettingKeys.TenantDelegation.LockTenantCoopProvider,
            request.Locks.LockTenantCoopProvider,
            request.UserId,
            cancellationToken);

        settingsResolver.InvalidateCache(SettingScope.Instance);

        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = Guid.Empty,
            Message = "Moderation reporting provider locks updated successfully.",
        };
    }

    private Task SetAsync(string key, bool value, Guid userId, CancellationToken cancellationToken) =>
        settingsResolver.SetValueAsync(
            key,
            SettingValueSerializer.Serialize(value),
            SettingScope.Instance,
            Guid.Empty,
            userId,
            cancellationToken);
}
