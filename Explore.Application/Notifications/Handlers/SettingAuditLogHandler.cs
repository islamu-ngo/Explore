// ABOUTME: MediatR notification handler that writes structured audit log entries for setting changes.
// ABOUTME: Uses Serilog structured logging so entries appear in Loki with queryable Key/Scope/Tenant fields.

using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Notifications.Handlers;

public class SettingAuditLogHandler : INotificationHandler<SettingChangedNotification>
{
    private readonly ILogger<SettingAuditLogHandler> _logger;

    public SettingAuditLogHandler(ILogger<SettingAuditLogHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(SettingChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Setting changed: {SettingKey} Scope={Scope} TenantId={TenantId} Actor={ActorUserId} OldValue={OldValue} NewValue={NewValue}",
            notification.Key,
            notification.Scope,
            notification.TenantId,
            notification.ActorUserId,
            notification.OldValue,
            notification.NewValue);

        return Task.CompletedTask;
    }
}
