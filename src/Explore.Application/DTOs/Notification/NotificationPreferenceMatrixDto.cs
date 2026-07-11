// ABOUTME: DTOs for the authenticated user's notification preference matrix.
// ABOUTME: Carries category, channel, cell, and global mute state for HAL-backed clients.

namespace Explore.Application.DTOs.Notification;

public sealed class NotificationPreferenceMatrixDto
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? GroupId { get; set; }
    public string Scope { get; set; } = "user";
    public IReadOnlyList<NotificationPreferenceCategoryDto> Categories { get; set; } = [];
    public IReadOnlyList<NotificationPreferenceChannelDto> Channels { get; set; } = [];
    public IReadOnlyList<NotificationPreferenceCellDto> Cells { get; set; } = [];
    public NotificationPreferenceMuteDto Mute { get; set; } = new();
}

public sealed class NotificationPreferenceCategoryDto
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
}

public sealed class NotificationPreferenceChannelDto
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public sealed class NotificationPreferenceCellDto
{
    public required string CategoryCode { get; set; }
    public required string ChannelCode { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsEditable { get; set; }
    public bool IsRequired { get; set; }
    public bool IsLocked { get; set; }
    public bool IsMuted { get; set; }
    public required string EffectiveSourceScope { get; set; }
    public string? LockReason { get; set; }
}

public sealed class NotificationPreferenceMuteDto
{
    public bool IsMuted { get; set; }
    public bool IsEditable { get; set; } = true;
    public bool IsLocked { get; set; }
    public string? LockReason { get; set; }
}

public sealed class UpdateNotificationPreferenceMatrixDto
{
    public IReadOnlyList<UpdateNotificationPreferenceCellDto> Cells { get; set; } = [];
}

public sealed class UpdateNotificationPreferenceCellDto
{
    public required string CategoryCode { get; set; }
    public required string ChannelCode { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class SetNotificationPreferenceMuteDto
{
    public bool IsMuted { get; set; }
}
