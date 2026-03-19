// ABOUTME: MediatR notification emitted when any policy set is created or updated.
// ABOUTME: Handlers can invalidate caches, push audit logs, or fan out to external systems.

using Explore.Domain.Settings;
using MediatR;

namespace Explore.Application.Notifications;

public sealed record PolicyChangedNotification(
    SettingScope Scope,
    Guid? ScopeId,
    string ChangedBy,
    DateTimeOffset ChangedAt) : INotification;
