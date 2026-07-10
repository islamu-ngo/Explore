// ABOUTME: Safe Web Push subscription DTO for authenticated-user subscription status.
// ABOUTME: Omits browser endpoint and key material so API and UI responses never echo secrets.

namespace Explore.Application.DTOs.Notification;

public sealed class WebPushSubscriptionDto
{
    public Guid Id { get; init; }
    public required string DeviceIdentifier { get; init; }
    public DateTime LastSeenAt { get; init; }
    public DateTime? ExpirationTime { get; init; }
}
