// ABOUTME: MediatR notification handler that invalidates the hierarchical settings cache after writes.
// ABOUTME: Ensures read-after-write consistency by evicting stale system settings from IMemoryCache.

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Settings;
using MediatR;

namespace Explore.Application.Notifications.Handlers;

public sealed class SettingCacheInvalidationHandler : INotificationHandler<SettingChangedNotification>
{
    private readonly IHierarchicalSettingsResolver _resolver;

    public SettingCacheInvalidationHandler(IHierarchicalSettingsResolver resolver)
    {
        _resolver = resolver;
    }

    public Task Handle(SettingChangedNotification notification, CancellationToken cancellationToken)
    {
        _resolver.InvalidateCache(SettingScope.Instance);

        if (notification.TenantId.HasValue)
        {
            _resolver.InvalidateCache(SettingScope.Tenant, notification.TenantId.Value);
        }

        return Task.CompletedTask;
    }
}
