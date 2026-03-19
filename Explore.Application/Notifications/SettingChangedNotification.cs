// ABOUTME: MediatR notification published when a governance or policy setting is changed.
// ABOUTME: Consumed by audit log handler for structured observability of configuration changes.

using Explore.Application.Contracts.Infrastructure;
using MediatR;

namespace Explore.Application.Notifications;

public record SettingChangedNotification(
    string Key,
    string? OldValue,
    string? NewValue,
    SettingSource Scope,
    Guid? TenantId,
    Guid? ActorUserId,
    DateTime ChangedAt) : INotification;
